# How to Properly Handle Cancellation with NUnit in Microsoft Testing Platform Bridge

This document explains the correct approach for handling test cancellation when working with NUnit through the VSTest adapter bridge in Microsoft Testing Platform.

## Overview

When working with the `NUnitBridgedTestFramework`, cancellation flow involves multiple layers:
1. **VSTest Adapter Layer** (`NUnit3TestExecutor`) - Your implementation
2. **Bridge Layer** (`SynchronizedSingleSessionVSTestBridgedTestFramework`) - Handles MTP integration  
3. **Platform Layer** - Manages session lifecycle and messaging

The key insight is that **you should NOT manually manage session lifecycle**. The bridge and platform handle all session start/end messaging automatically.

## The Problem

The error `"A test session start event was received without a corresponding test session end"` occurs when:
- Test processes exit abnormally (e.g., `Environment.Exit()`)
- Cancellation is not handled properly, preventing clean completion
- Session lifecycle is manually managed instead of letting the platform handle it

## The Bridge Flow

Here's what happens in the bridge during cancellation:

```csharp
protected override async Task SynchronizedRunTestsAsync(VSTestRunTestExecutionRequest request, IMessageBus messageBus,
    CancellationToken cancellationToken)
{
    _currentMessageBus = messageBus;
    _testSessionActive = true;

    CurrentMessageBus = messageBus;
    CurrentSessionUid = request.Session?.SessionUid;

    ITestExecutor executor = new NUnit3TestExecutor(isMTP: true);
    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCts.Token);

    try
    {
        using (combinedCts.Token.Register(() =>
        {
            // Enhanced cancellation for MTP
            executor.Cancel();  // <-- Your Cancel() method is called here
            Thread.Sleep(100);
        }))
        {
            executor.RunTests(request.AssemblyPaths, request.RunContext, request.FrameworkHandle);
        }
    }
    finally
    {
        await HandleTestCompletionWithNuclearOption(executor, combinedCts.Token.IsCancellationRequested);
    }
}
```

## ? Correct Implementation

### 1. Proper Cancel() Method Implementation

Your `Cancel()` method should stop the NUnit engine gracefully but **never throw exceptions**:

```csharp
public void Cancel()
{
    try
    {
        // Stop the NUnit engine gracefully
        _nunitEngine?.StopRun(force: false);
        
        // Set cancellation flag for RunTests method
        _cancellationRequested = true;
        
        // Cancel any ongoing operations
        _internalCts?.Cancel();
    }
    catch (Exception ex)
    {
        // Log but don't throw - Cancel should never throw
        Console.WriteLine($"Error during cancellation: {ex}");
    }
}
```

### 2. Proper RunTests Implementation

Your `RunTests` methods should check for cancellation frequently and **return cleanly**:

```csharp
private volatile bool _cancellationRequested;
private readonly CancellationTokenSource _internalCts = new();

public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
{
    try
    {
        var tests = DiscoverTests(sources);
        RunTestsCore(tests, runContext, frameworkHandle);
    }
    catch (OperationCanceledException)
    {
        // Don't log or handle - just let it propagate cleanly
        throw;
    }
    // CRITICAL: Don't catch other exceptions that might prevent proper session cleanup
}

public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
{
    try
    {
        RunTestsCore(tests, runContext, frameworkHandle);
    }
    catch (OperationCanceledException)
    {
        // Let cancellation propagate cleanly to the bridge
        throw;
    }
}

private void RunTestsCore(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
{
    foreach (var testCase in tests)
    {
        // Check multiple cancellation sources
        if (_cancellationRequested || 
            runContext.CancellationToken.IsCancellationRequested ||
            _internalCts.Token.IsCancellationRequested)
        {
            // Report remaining tests as skipped and return cleanly
            frameworkHandle.RecordResult(new TestResult(testCase)
            {
                Outcome = TestOutcome.Skipped,
                ErrorMessage = "Test run was cancelled"
            });
            continue;
        }

        try
        {
            var result = ExecuteTest(testCase);
            frameworkHandle.RecordResult(result);
        }
        catch (OperationCanceledException)
        {
            frameworkHandle.RecordResult(new TestResult(testCase)
            {
                Outcome = TestOutcome.Skipped,
                ErrorMessage = "Test execution was cancelled"
            });
            throw; // Propagate to exit the loop and method
        }
    }
    
    // CRITICAL: Return normally when done - don't throw or exit abnormally
}
```

### 3. Bridge Cleanup Implementation

Your `HandleTestCompletionWithNuclearOption` should clean up resources but let the platform handle session messaging:

```csharp
private async Task HandleTestCompletionWithNuclearOption(ITestExecutor executor, bool wasCancelled)
{
    try
    {
        // Clean up executor resources
        if (executor is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        // Mark session as inactive
        _testSessionActive = false;
        
        // Let any pending test results flow through the message bus
        // The message bus will be drained by the platform
        
        // Don't manually send session end - let CloseTestSessionAsync handle it
    }
    catch (Exception ex)
    {
        // Log but ensure session cleanup continues
        Console.WriteLine($"Error in test completion: {ex}");
    }
    finally
    {
        // Ensure cleanup happens
        CurrentMessageBus = null;
        CurrentSessionUid = null;
        _currentMessageBus = null;
    }
}
```

## Session Lifecycle Management

The session lifecycle is managed automatically by the platform through these key components:

### 1. Base Class Session Management

The `SynchronizedSingleSessionVSTestBridgedTestFramework` base class handles session lifecycle:

```csharp
public sealed override Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
{
    if (_sessionUid is not null)
    {
        throw new InvalidOperationException("Session already created");
    }
    _sessionUid = context.SessionUid;
    return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
}

public sealed override async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
{
    // Clear initial count
    _incomingRequestCounter.Signal();

    // Wait for remaining request processing (handles cancellation)
    await _incomingRequestCounter.WaitAsync(context.CancellationToken).ConfigureAwait(false);
    _sessionUid = null;
    return new CloseTestSessionResult { IsSuccess = true };
}
```

### 2. Platform Session Event Handling

Session start/end events are automatically sent by `ITestSessionLifetimeHandler` implementations like `DotnetTestDataConsumer`:

```csharp
public async Task OnTestSessionStartingAsync(ITestSessionContext testSessionContext)
{
    TestSessionEvent sessionStartEvent = new(
        SessionEventTypes.TestSessionStart,
        testSessionContext.SessionUid.Value,
        ExecutionId);
    await _dotnetTestConnection.SendMessageAsync(sessionStartEvent).ConfigureAwait(false);
}

public async Task OnTestSessionFinishingAsync(ITestSessionContext testSessionContext)
{
    TestSessionEvent sessionEndEvent = new(
        SessionEventTypes.TestSessionEnd,
        testSessionContext.SessionUid.Value,
        ExecutionId);
    await _dotnetTestConnection.SendMessageAsync(sessionEndEvent).ConfigureAwait(false);
}
```

## ? What You MUST Do

1. **Return cleanly from RunTests**: Don't exit the process or throw unexpected exceptions
2. **Handle Cancel() gracefully**: Stop your engine but don't throw exceptions from Cancel()
3. **Check cancellation before every operation**: Especially before calling `frameworkHandle.RecordResult()`
4. **Let OperationCanceledException propagate**: Don't suppress it - let it flow to the bridge
5. **Stop trying to record results once cancelled**: The frameworkHandle becomes invalid during cancellation
6. **Dispose resources properly**: In your cleanup methods

## ? What You MUST NOT Do

1. **Don't call Environment.Exit()**: This prevents proper session cleanup
2. **Don't throw from Cancel()**: The Cancel method should never throw exceptions
3. **Don't catch and suppress OperationCanceledException**: Let it propagate to the bridge
4. **Don't try to send session messages directly**: **CORRECTION: Don't send SESSION LIFECYCLE messages. Test result messages via message bus are correct.**
5. **Don't try to record results after cancellation**: The frameworkHandle becomes invalid and will throw
6. **Don't manually manage session lifecycle**: The platform handles this automatically
7. **Don't exit RunTests abnormally**: Always return through normal flow, even when cancelled

## Test Result Handling - CRITICAL

### ? Correct Way to Report Test Results (CORRECTED - Message Bus Approach)

**FOR NUNIT BRIDGE**: Use the message bus directly, not the frameworkHandle. This is the intended pattern:

```csharp
private void RunTestsCore(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
{
    foreach (var testCase in tests)
    {
        // Check cancellation BEFORE starting any test work
        if (_cancellationRequested || 
            runContext.CancellationToken.IsCancellationRequested ||
            _internalCts.Token.IsCancellationRequested)
        {
            // For remaining tests, send cancelled results via message bus
            var cancelledTestNode = testCase.ToTestNode();
            var cancelledResult = TestNodeUpdateMessage.TestNodeSkippedMessage(cancelledTestNode, "Test run was cancelled");
            
            try
            {
                CurrentMessageBus?.PublishAsync(this, new TestNodeUpdateMessage(CurrentSessionUid, cancelledResult));
            }
            catch (Exception ex)
            {
                // If message bus fails, log but continue - session cleanup will handle it
                Console.WriteLine($"Failed to send cancelled test result: {ex.Message}");
            }
            continue;
        }

        try
        {
            // Send test started message
            var testNode = testCase.ToTestNode();
            CurrentMessageBus?.PublishAsync(this, new TestNodeUpdateMessage(CurrentSessionUid, testNode.WithInProgressState()));
            
            var result = ExecuteTest(testCase);
            
            // Check cancellation before sending result
            if (_cancellationRequested || 
                runContext.CancellationToken.IsCancellationRequested ||
                _internalCts.Token.IsCancellationRequested)
            {
                // Send cancelled result and exit
                var cancelledResult = TestNodeUpdateMessage.TestNodeSkippedMessage(testNode, "Test execution was cancelled");
                CurrentMessageBus?.PublishAsync(this, new TestNodeUpdateMessage(CurrentSessionUid, cancelledResult));
                return;
            }
            
            // Send completed result via message bus
            var completedResult = TestNodeUpdateMessage.TestNodeResultMessage.From(result);
            CurrentMessageBus?.PublishAsync(this, new TestNodeUpdateMessage(CurrentSessionUid, completedResult));
            
        }
        catch (OperationCanceledException)
        {
            // Just return cleanly - don't send more messages
            return;
        }
    }
}
```

**KEY INSIGHT**: The NUnit bridge is designed for direct message bus communication, not frameworkHandle usage.

### ? Incorrect Way (What Was Actually Wrong)

**The problem was NOT using the message bus**. The message bus approach is correct for NUnit bridge. The problem was:

```csharp
// ? CORRECT - Send test results via message bus
// CurrentMessageBus?.PublishAsync(this, new TestNodeUpdateMessage(...));

// ? WRONG - Don't manually send SESSION lifecycle events
// await SendSessionEndEvent();
// await messageBus.PublishAsync(this, new SessionEndMessage(...));

// ? WRONG - Don't try to manage session lifecycle yourself  
// CurrentSessionUid = null; // Let the bridge handle this
```

**The real issue**: Trying to manage session lifecycle manually instead of letting the bridge handle it automatically.

### Why This Matters (FINAL CORRECTION)

The NUnit bridge is specifically designed for direct message bus communication:

1. **Bridge exposes message bus**: `CurrentMessageBus` and `CurrentSessionUid` are provided for direct use
2. **Message bus approach works**: This is the intended pattern for sending test results
3. **Session lifecycle is separate**: The bridge handles session start/end automatically
4. **Cancellation timing**: The message bus remains valid longer than frameworkHandle during cancellation

**The key insight**: Use message bus for test results, but never try to manage session lifecycle yourself.

## Key Architecture Points

### Layer Responsibilities

- **Your NUnit3TestExecutor**: Handle test execution, cancellation detection, result reporting
- **Bridge Layer**: Translate between VSTest and MTP, manage session lifecycle
- **Platform Layer**: Handle session messaging, lifecycle events, message bus management

### Cancellation Flow

1. Platform detects cancellation request
2. Bridge receives cancellation and calls `executor.Cancel()`
3. Your executor stops NUnit engine and sets cancellation flags
4. RunTests methods detect cancellation and exit cleanly
5. Bridge cleanup runs in `finally` block
6. Platform automatically calls `CloseTestSessionAsync`
7. Session end events are sent automatically by platform handlers

### Message Flow (FINAL CORRECTION)

**CORRECT** for NUnit Bridge:
```
Your Code: CurrentMessageBus.PublishAsync(TestNodeUpdateMessage) ? Platform ? Session Events  
Bridge: Handles session lifecycle automatically
```

**INCORRECT**:
```
Your Code: Manual Session End Events ? Platform ? CRASH
Your Code: frameworkHandle.RecordResult() ? (Wrong pattern for NUnit bridge)
```

The session end message is sent automatically when `CloseTestSessionAsync` completes successfully, which happens when your `RunTests` methods return normally and you haven't tried to manage session lifecycle manually.

## Debugging Tips

If you're still getting session end errors:

1. **Check for process exits**: Ensure no code calls `Environment.Exit()` or similar
2. **Verify clean returns**: Ensure `RunTests` methods complete normally, even when cancelled
3. **Check exception handling**: Don't catch and hide critical exceptions
4. **Monitor cancellation**: Log when cancellation is detected and how it's handled
5. **Verify disposal**: Ensure `IDisposable` resources are properly cleaned up
6. **Remove manual event sending**: Ensure you're not sending test finished or session events manually
7. **Trace message flow**: Add logging to see when `frameworkHandle.RecordResult()` is called vs when exceptions occur

## Common Crash Scenarios (Fixed)

### Scenario 1: Using frameworkHandle After Cancellation
**Problem**: Your code was trying to call `frameworkHandle.RecordResult()` after cancellation was requested
**Solution**: Check for cancellation before every `RecordResult()` call and exit cleanly once cancelled

### Scenario 2: Session Management
**Problem**: Trying to manually send session end events when tests complete
**Solution**: Let the bridge handle session lifecycle automatically through `CloseTestSessionAsync`

### Scenario 3: Abnormal Exit
**Problem**: Code exits via `Environment.Exit()` or unhandled exceptions
**Solution**: Always return normally from `RunTests`, even when cancelled

The key insight is that **the session end message is sent automatically by the platform when your test execution completes cleanly**. If your code exits abnormally OR corrupts the message flow with manual events, the session cleanup never happens properly.
