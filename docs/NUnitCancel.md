# Handling NUnit Test Cancellation with VSTestBridge

When NUnit tests run in parallel and cancellation is requested, the test process may hang due to how the VSTestBridge handles cancellation propagation to parallel test threads. This document outlines clean approaches to handle this scenario.

## Overview

The VSTestBridge provides compatibility for existing VSTest adapters (like NUnit3TestAdapter) to work with Microsoft Testing Platform (MTP). However, the bridge architecture can lead to cancellation issues when:

- Tests run in parallel (multiple threads)
- Cancellation is requested mid-execution
- The bridge doesn't properly propagate cancellation to all test threads

## Solution Approaches

### 1. Graceful Shutdown with Timeout Pattern

Implement a two-stage shutdown process that attempts graceful cancellation first, then forces termination if needed.

```csharp
public async Task<int> StopAsync(CancellationToken cancellationToken = default)
{
    using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    combinedCts.CancelAfter(TimeSpan.FromSeconds(30)); // Graceful shutdown timeout

    try
    {
        // First: Try graceful cancellation
        await RequestGracefulShutdown(combinedCts.Token);
        
        // Wait for process to exit gracefully
#if NET5_0_OR_GREATER
        if (await _process.WaitForExitAsync(combinedCts.Token))
#else
        var tcs = new TaskCompletionSource<bool>();
        combinedCts.Token.Register(() => tcs.TrySetCanceled());
        
        _process.EnableRaisingEvents = true;
        _process.Exited += (s, e) => tcs.TrySetResult(true);
        
        if (!_process.HasExited && await tcs.Task)
#endif
        {
            return _process.ExitCode;
        }
    }
    catch (OperationCanceledException)
    {
        // Graceful shutdown timed out or was cancelled
    }

    // Second: Force termination if graceful shutdown failed
    try
    {
#if NET5_0_OR_GREATER
        _process.Kill(entireProcessTree: true); // Kill entire process tree for parallel scenarios
        await _process.WaitForExitAsync(CancellationToken.None);
#else
        _process.Kill();
        _process.WaitForExit();
#endif
        return _process.ExitCode;
    }
    catch (Exception ex)
    {
        // Log the force termination
        _logger.LogWarning("Had to force terminate test process: {Exception}", ex);
        return -1;
    }
}
```

### 2. Bridge-Specific Cancellation Handling

Enhance the bridge to properly handle cancellation in parallel test scenarios.

```csharp
// In your bridge implementation
public async Task HandleCancellationAsync(CancellationToken cancellationToken)
{
    // Cancel all active test executions
    await CancelAllActiveTestExecutions(cancellationToken);
    
    // Give threads time to clean up
    try
    {
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // Continue with shutdown even if delay was cancelled
    }
    
    // Signal the bridge to shutdown
    await RequestBridgeShutdown(cancellationToken);
}

private async Task CancelAllActiveTestExecutions(CancellationToken cancellationToken)
{
    var cancellationTasks = _activeTestExecutions.Select(execution => 
        execution.CancelAsync(cancellationToken));
    
    try
    {
        await Task.WhenAll(cancellationTasks);
    }
    catch (Exception ex)
    {
        _logger.LogWarning("Error during test execution cancellation: {Exception}", ex);
    }
}
```

### 3. Process Tree Termination for Parallel Scenarios

Since parallel tests may spawn child processes or threads, use enhanced kill methods for complete cleanup.

```csharp
private async Task<bool> TryGracefulShutdown(TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);
    
    try
    {
        // Send graceful shutdown signal (if your bridge supports it)
        await SendShutdownSignal(cts.Token);
        
        // Wait for graceful exit
#if NET5_0_OR_GREATER
        await _process.WaitForExitAsync(cts.Token);
#else
        var tcs = new TaskCompletionSource<bool>();
        cts.Token.Register(() => tcs.TrySetCanceled());
        
        _process.EnableRaisingEvents = true;
        _process.Exited += (s, e) => tcs.TrySetResult(true);
        
        if (!_process.HasExited)
        {
            await tcs.Task;
        }
#endif
        return true;
    }
    catch (OperationCanceledException)
    {
        return false;
    }
}

private void ForceTermination()
{
    try
    {
#if NET5_0_OR_GREATER
        // Kill entire process tree - crucial for parallel test scenarios
        _process.Kill(entireProcessTree: true);
#else
        // For older .NET versions, kill the main process
        _process.Kill();
        
        // Optionally kill child processes manually if needed
        KillChildProcesses(_process.Id);
#endif
    }
    catch (Exception ex)
    {
        _logger.LogError("Failed to kill process tree: {Exception}", ex);
        
        // Fallback to single process kill
        try
        {
            _process.Kill();
        }
        catch (Exception fallbackEx)
        {
            _logger.LogError("Fallback process kill also failed: {Exception}", fallbackEx);
        }
    }
}

#if !NET5_0_OR_GREATER
private void KillChildProcesses(int parentId)
{
    try
    {
        var searcher = new ManagementObjectSearcher(
            $"Select * From Win32_Process Where ParentProcessID={parentId}");
        
        foreach (var mo in searcher.Get())
        {
            var childId = Convert.ToInt32(mo["ProcessID"]);
            try
            {
                var childProcess = Process.GetProcessById(childId);
                childProcess.Kill();
                childProcess.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to kill child process {ProcessId}: {Exception}", childId, ex);
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning("Failed to enumerate child processes: {Exception}", ex);
    }
}
#endif
```

### 4. Resource Cleanup in Bridge

Ensure the VSTestBridge properly cleans up parallel test resources during disposal.

```csharp
protected override async ValueTask DisposeAsyncCore()
{
    // Cancel all running operations
    _cancellationTokenSource?.Cancel();
    
    // Clean up parallel test threads
    await CleanupParallelTestThreads();
    
    // Dispose of VSTest framework resources
    _testFramework?.Dispose();
    
    // Clean up process resources
    if (_process != null && !_process.HasExited)
    {
        await StopAsync();
        _process.Dispose();
    }
}

private async Task CleanupParallelTestThreads()
{
    if (_activeTestThreads == null || _activeTestThreads.Count == 0)
        return;

    // Wait for active test threads to complete or timeout
    var cleanupTasks = _activeTestThreads.Select(async thread =>
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            await thread.WaitForCompletion(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Thread didn't respond to cancellation - it will be killed with process
            _logger.LogWarning("Test thread {ThreadId} did not respond to cancellation", thread.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Error waiting for test thread {ThreadId}: {Exception}", thread.Id, ex);
        }
    });
    
    try
    {
        await Task.WhenAll(cleanupTasks);
    }
    catch (Exception ex)
    {
        _logger.LogError("Error during parallel test thread cleanup: {Exception}", ex);
    }
}

// For .NET Framework compatibility
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        // Synchronous cleanup for .NET Framework
        _cancellationTokenSource?.Cancel();
        
        // Clean up threads synchronously
        CleanupParallelTestThreadsSync();
        
        _testFramework?.Dispose();
        
        if (_process != null && !_process.HasExited)
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during synchronous process cleanup: {Exception}", ex);
            }
        }
        
        _process?.Dispose();
    }
    
    base.Dispose(disposing);
}
```

### 5. Decision Matrix: Bridge Enhancement vs Process Kill

| Scenario | Recommended Action | Timeout | Reasoning |
|----------|-------------------|---------|-----------|
| **First cancellation attempt** | Graceful shutdown with timeout | 30 seconds | Allows proper resource cleanup |
| **Timeout after 30s** | Kill process tree (`Kill(true)`) | Immediate | Parallel tests may have child processes |
| **Bridge becomes unresponsive** | Force termination | Immediate | Bridge translation layer may be stuck |
| **Repeated hanging** | Investigate bridge implementation | N/A | May need VSTestBridge fixes |
| **Resource constraints** | Shorter timeout + force kill | 15 seconds | Balance cleanup vs. responsiveness |

## Implementation Guidelines

### Target Framework Considerations

The code examples include conditional compilation for different .NET versions:

- **.NET 5+**: Use `Process.Kill(entireProcessTree: true)` and `WaitForExitAsync()`
- **.NET Framework 4.6.2-4.8**: Use traditional `Process.Kill()` and manual child process cleanup
- **.NET Standard 2.0**: Compatible with both approaches

### Logging and Monitoring

Always log forced terminations for debugging:

```csharp
private void LogForcedTermination(string reason, Exception exception = null)
{
    var logLevel = exception != null ? LogLevel.Error : LogLevel.Warning;
    _logger.Log(logLevel, "Forced test process termination: {Reason}. Exception: {Exception}", 
        reason, exception?.ToString());
    
    // Optional: Send telemetry for tracking bridge reliability
    _telemetry?.TrackEvent("ForcedProcessTermination", new Dictionary<string, string>
    {
        ["Reason"] = reason,
        ["ProcessId"] = _process?.Id.ToString(),
        ["HasException"] = (exception != null).ToString()
    });
}
```

### Configuration

Consider making timeouts configurable:

```csharp
public class BridgeConfiguration
{
    public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ThreadCleanupTimeout { get; set; } = TimeSpan.FromSeconds(10);
    public bool EnableProcessTreeKill { get; set; } = true;
    public bool LogForcedTerminations { get; set; } = true;
}
```

## Recommendation

**Do not immediately kill the bridge process.** Instead:

1. **Implement the graceful-then-force pattern** with appropriate timeouts
2. **Use `Kill(entireProcessTree: true)`** on .NET 5+ for parallel test scenarios  
3. **Add proper timeout handling** (30-60 seconds for graceful shutdown)
4. **Log forced terminations** for debugging bridge issues
5. **Consider contributing fixes** to VSTestBridge if this becomes a recurring issue

The hanging is typically due to the VSTestBridge not properly handling cancellation propagation to parallel test threads. A cleaner long-term solution involves enhancing the bridge's cancellation support rather than immediately resorting to process termination.

## Testing the Implementation

To verify your cancellation handling:

1. Create parallel NUnit tests that run for extended periods
2. Cancel execution mid-run
3. Monitor process cleanup and resource disposal
4. Verify no orphaned processes remain
5. Check logs for proper graceful vs. forced termination patterns

This approach provides a robust solution that balances clean resource management with the need to handle unresponsive bridge scenarios.

## Handling MTP Session Lifecycle Issues

### The "Missing Test Session End" Problem

When using a "nuclear" exit approach (like `Environment.Exit()` or `Process.Kill()`), you may encounter this exception:

```
System.InvalidOperationException: A test session start event was received without a corresponding test session end.
   at Microsoft.DotNet.Cli.Commands.Test.TestApplication.RunAsync()
```

This occurs because the abrupt process termination bypassed the normal MTP session lifecycle, preventing the bridge from sending the required session end event.

### Required Session Cleanup for NUnit Bridge

The NUnit VSTestBridge must ensure **test session lifecycle completion** before any forced termination:

#### 1. Session End Event Before Exit

```csharp
public async Task HandleForcedShutdownAsync(CancellationToken cancellationToken)
{
    try
    {
        // Critical: Send session end event to MTP before any forced termination
        if (_testSessionActive)
        {
            await SendTestSessionEndEvent(_cancellationToken.IsCancellationRequested 
                ? TestSessionResult.Cancelled 
                : TestSessionResult.Completed);
        }
        
        // Send any pending test completion events
        await FlushPendingTestEvents(TimeSpan.FromSeconds(2));
        
        // Ensure message bus is flushed
        await _messageBus?.FlushAsync(cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogWarning("Error during session cleanup: {Exception}", ex);
    }
}
```

#### 2. Enhanced Bridge Disposal Pattern

```csharp
protected override async ValueTask DisposeAsyncCore()
{
    try
    {
        // Cancel all running operations
        _cancellationTokenSource?.Cancel();
        
        // Critical: Complete test session before disposal
        await HandleForcedShutdownAsync(CancellationToken.None);
        
        // Clean up VSTest framework resources
        _testFramework?.Dispose();
        
        // Clean up process resources
        if (_process?.HasExited == false)
        {
            await StopAsync(TimeSpan.FromSeconds(5));
        }
    }
    catch (Exception ex)
    {
        _logger.LogError("Error during bridge disposal: {Exception}", ex);
    }
    finally
    {
        _process?.Dispose();
    }
}
```

#### 3. Message Bus Completion

```csharp
private async Task FlushPendingTestEvents(TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);
    
    try
    {
        // Complete any pending test results
        foreach (var pendingTest in _pendingTestResults)
        {
            await _messageBus.PublishAsync(new TestNodeUpdateMessage(
                pendingTest.SessionUid,
                new TestNodeUpdate
                {
                    Node = pendingTest.TestNode,
                    Property = new TestResultProperty(
                        TestOutcome.NotExecuted,
                        "Test cancelled due to process termination")
                }), cts.Token);
        }
        
        // Ensure message bus processes all messages
        await _messageBus?.CompleteAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Timeout while flushing pending test events");
    }
}
```

#### 4. Process Exit Handler Registration

```csharp
// In bridge initialization
AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
Console.CancelKeyPress += OnCancelKeyPress;

private void OnProcessExit(object sender, EventArgs e)
{
    try
    {
        HandleForcedShutdownAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }
    catch (Exception ex)
    {
        // Log but don't throw during process exit
        Console.Error.WriteLine($"Error during process exit cleanup: {ex}");
    }
}

private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
{
    e.Cancel = true; // Prevent immediate termination
    _cancellationTokenSource.Cancel();
}
```

#### 5. Timeout-Based Session Completion

```csharp
private async Task<bool> TryGracefulSessionEnd(TimeSpan timeout)
{
    using var cts = new CancellationTokenSource(timeout);
    
    try
    {
        await SendTestSessionEndEvent(TestSessionResult.Cancelled);
        await _messageBus?.CompleteAsync(cts.Token);
        return true;
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning("Session end timed out after {Timeout}", timeout);
        return false;
    }
}
```

#### 6. Integration with Graceful Shutdown Pattern

```csharp
public async Task<int> StopAsync(CancellationToken cancellationToken = default)
{
    // Step 1: Complete test session gracefully
    if (await TryGracefulSessionEnd(TimeSpan.FromSeconds(10)))
    {
        return await base.StopAsync(cancellationToken);
    }
    
    // Step 2: Force session completion if graceful failed
    await HandleForcedShutdownAsync(cancellationToken);
    
    // Step 3: Continue with process termination
    return await base.StopAsync(cancellationToken);
}
```

### Key Requirements for NUnit Bridge

1. **Always send session end events** before any process termination
2. **Flush the message bus** to ensure MTP receives all events  
3. **Handle process exit events** to ensure cleanup even during abrupt termination
4. **Use timeouts** to prevent hanging during cleanup
5. **Log cleanup failures** but don't let them prevent termination

### Framework Version Compatibility

The session lifecycle management works across all supported .NET versions, but consider these patterns:

```csharp
#if NET5_0_OR_GREATER
// Use modern async patterns
await _messageBus.CompleteAsync(cancellationToken);
#else  
// Use Task-based async for older frameworks
await _messageBus.CompleteAsync(cancellationToken).ConfigureAwait(false);
#endif
```

This session lifecycle management resolves the `InvalidOperationException` about missing test session end events while maintaining compatibility with the graceful shutdown patterns described earlier.

## Eliminating the "Missing Test Session End" Exception

To specifically eliminate the `InvalidOperationException: A test session start event was received without a corresponding test session end` exception, implement this forced session end pattern:

### Immediate Session End Event

Send the session end event immediately when cancellation is detected, before any cleanup attempts:

```csharp
// Call this immediately when cancellation is detected
public void ForceTestSessionEnd()
{
    if (!_testSessionActive) return;
    
    // Fire and forget - don't wait for completion
    _ = Task.Run(async () =>
    {
        try
        {
            // Use very short timeout
            using var cts = new CancellationTokenSource(500); // 500ms max
            
            // Send session end event directly to message bus
            var sessionEndMessage = new TestSessionEndMessage(
                _sessionUid,
                new TestSessionResult 
                { 
                    State = TestSessionState.Cancelled,
                    ExitCode = -1
                });
            
            await _messageBus.PublishAsync(sessionEndMessage, cts.Token);
            
            // Brief delay to let message propagate
            await Task.Delay(50, cts.Token);
        }
        catch (Exception ex)
        {
            // Log but don't throw - we're in forced termination mode
            Console.Error.WriteLine($"Failed to send session end: {ex.Message}");
        }
    });
    
    // Mark session as ended immediately
    _testSessionActive = false;
}
```

### Integration with Process Termination

Call this before any process kill operations:

```csharp
public async Task<int> StopAsync(CancellationToken cancellationToken = default)
{
    // FIRST: Force session end immediately
    ForceTestSessionEnd();
    
    // Brief delay to let session end event fire
    await Task.Delay(100, CancellationToken.None);
    
    // THEN: Proceed with process termination
    return await ForceTerminateProcess();
}

private async Task<int> ForceTerminateProcess()
{
    try
    {
#if NET5_0_OR_GREATER
        _process.Kill(entireProcessTree: true);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await _process.WaitForExitAsync(cts.Token);
#else
        _process.Kill();
        if (!_process.WaitForExit(3000))
        {
            Environment.Exit(-1);
        }
#endif
        return _process.ExitCode;
    }
    catch
    {
        Environment.Exit(-1);
        return -1;
    }
}
```

### Static Helper for Emergency Cases

For truly emergency scenarios where the bridge instance might be corrupted:

```csharp
public static void EmergencySessionEnd(IMessageBus messageBus, SessionUid sessionUid)
{
    try
    {
        // Static method that doesn't depend on bridge state
        _ = Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource(300);
            
            var emergencyEndMessage = new TestSessionEndMessage(
                sessionUid,
                new TestSessionResult 
                { 
                    State = TestSessionState.Cancelled,
                    ExitCode = -2 // Emergency termination
                });
            
            await messageBus.PublishAsync(emergencyEndMessage, cts.Token);
        });
        
        // Minimal delay for message to send
        Thread.Sleep(75);
    }
    catch
    {
        // Ultimate fallback - just terminate
    }
}
```

### Process Exit Handler with Session End

Register this during bridge initialization to catch unexpected terminations:

```csharp
private static SessionUid? s_activeSessionUid;
private static IMessageBus? s_messageBus;
private static bool s_sessionEndSent;

// In bridge constructor/initialization
static NUnitBridge()
{
    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
}

private static void OnProcessExit(object sender, EventArgs e)
{
    if (!s_sessionEndSent && s_activeSessionUid.HasValue && s_messageBus != null)
    {
        s_sessionEndSent = true;
        EmergencySessionEnd(s_messageBus, s_activeSessionUid.Value);
    }
}
```

### Framework-Specific Implementations

For different target frameworks in your workspace:

```csharp
#if NET6_0_OR_GREATER
private async Task SendSessionEndEvent(CancellationToken cancellationToken)
{
    var message = new TestSessionEndMessage(_sessionUid, new TestSessionResult 
    { 
        State = TestSessionState.Cancelled 
    });
    
    await _messageBus.PublishAsync(message, cancellationToken);
}
#elif NET5_0
// .NET 5 version
private async Task SendSessionEndEvent(CancellationToken cancellationToken)
{
    var message = new TestSessionEndMessage(_sessionUid, new TestSessionResult 
    { 
        State = TestSessionState.Cancelled 
    });
    
    await _messageBus.PublishAsync(message, cancellationToken).ConfigureAwait(false);
}
#else
// .NET Framework 4.6.2-4.8 version
private Task SendSessionEndEvent(CancellationToken cancellationToken)
{
    var message = new TestSessionEndMessage(_sessionUid, new TestSessionResult 
    { 
        State = TestSessionState.Cancelled 
    });
    
    return _messageBus.PublishAsync(message, cancellationToken);
}
#endif
```

### Key Points for Exception Elimination

1. **Send session end BEFORE any cleanup** - don't wait for other operations
2. **Use very short timeouts** (300-500ms) to prevent hanging
3. **Fire-and-forget pattern** - don't wait for confirmation
4. **Register process exit handler** as ultimate fallback
5. **Mark session as ended immediately** in memory

### Complete Integration Example

```csharp
public async Task HandleCancellation()
{
    _logger.LogWarning("Test cancellation requested - forcing session end");
    
    // Step 1: Force session end immediately
    ForceTestSessionEnd();
    
    // Step 2: Brief delay for message propagation
    await Task.Delay(150, CancellationToken.None);
    
    // Step 3: Kill process tree
#if NET5_0_OR_GREATER
    _process.Kill(entireProcessTree: true);
#else
    _process.Kill();
#endif
    
    // Step 4: Wait briefly, then force exit if needed
    var exitTask = Task.Run(async () =>
    {
        await Task.Delay(2000);
        if (!_process.HasExited)
        {
            Environment.Exit(-1);
        }
    });
}
```

This approach ensures the MTP session lifecycle is completed properly, eliminating the `InvalidOperationException` while maintaining the aggressive termination needed for hanging parallel tests.

## Additional Aggressive Termination Strategies

### The Problem with Graceful Approaches

If the bridge still hangs even after implementing session lifecycle management, the issue is likely that:

1. **Message bus operations are blocking indefinitely** despite cancellation tokens
2. **VSTest framework disposal is hanging** waiting for unresponsive threads
3. **Session end events are waiting for responses** that never come in parallel scenarios

### Fire-and-Forget Session Cleanup

For truly unresponsive scenarios, implement fire-and-forget cleanup with absolute timeouts:

```csharp
public async Task<int> StopAsync(CancellationToken cancellationToken = default)
{
    // Step 1: Attempt fire-and-forget session cleanup
    var cleanupTask = FireAndForgetSessionCleanup();
    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    
    var completedTask = await Task.WhenAny(cleanupTask, timeoutTask);
    
    if (completedTask == timeoutTask)
    {
        _logger.LogWarning("Session cleanup timed out - proceeding with force termination");
    }
    
    // Step 2: Force kill regardless of cleanup status
    return await ForceTerminateProcess();
}

private async Task FireAndForgetSessionCleanup()
{
    try
    {
        // Use very short timeouts for all operations
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        
        // Fire session end event but don't wait for confirmation
        _ = Task.Run(async () =>
        {
            try
            {
                if (_testSessionActive)
                {
                    await SendTestSessionEndEvent(TestSessionResult.Cancelled);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Session end event failed: {Exception}", ex);
            }
        }, cts.Token);
        
        // Brief delay to let the event fire
        await Task.Delay(100, cts.Token);
        
        // Force message bus completion with timeout
        if (_messageBus != null)
        {
            var busTask = _messageBus.CompleteAsync(cts.Token);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            
            await Task.WhenAny(busTask, timeoutTask);
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning("Fire-and-forget cleanup failed: {Exception}", ex);
    }
}
```

### Background Termination Monitor

Implement a background monitor that forcefully terminates if cleanup takes too long:

```csharp
private Timer? _terminationTimer;

public async Task<int> StopAsync(CancellationToken cancellationToken = default)
{
    // Start absolute termination timer (nuclear option)
    _terminationTimer = new Timer(ForceTerminate, null, 
        TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
    
    try
    {
        // Try graceful cleanup with very short timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await AttemptGracefulCleanup(cts.Token);
        
        // If we get here, cancel the nuclear timer
        _terminationTimer?.Dispose();
        
        return await WaitForProcessExit(cancellationToken);
    }
    catch (Exception)
    {
        // Graceful cleanup failed - let nuclear timer handle it
        return -1;
    }
}

private void ForceTerminate(object? state)
{
    try
    {
        _logger.LogWarning("Triggering nuclear process termination");
        
#if NET5_0_OR_GREATER
        _process?.Kill(entireProcessTree: true);
#else
        _process?.Kill();
#endif
        
        // Also force exit the entire application if needed
        if (_process?.HasExited == false)
        {
            Task.Run(async () =>
            {
                await Task.Delay(2000); // Give it 2 seconds
                if (_process?.HasExited == false)
                {
                    Environment.Exit(-1); // Nuclear option
                }
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError("Nuclear termination failed: {Exception}", ex);
        Environment.Exit(-2); // Ultimate fallback
    }
    finally
    {
        _terminationTimer?.Dispose();
    }
}
```

### Immediate Process Kill Pattern

For scenarios where any cleanup attempt causes hanging:

```csharp
public async Task<int> EmergencyStop()
{
    _logger.LogWarning("Emergency stop initiated - skipping all cleanup");
    
    try
    {
        // Skip ALL cleanup - just kill the process immediately
#if NET5_0_OR_GREATER
        _process.Kill(entireProcessTree: true);
        
        // Wait maximum 3 seconds for process death
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await _process.WaitForExitAsync(cts.Token);
#else
        _process.Kill();
        if (!_process.WaitForExit(3000))
        {
            // Process still alive after 3 seconds - force app exit
            Environment.Exit(-1);
        }
#endif
        
        return _process.ExitCode;
    }
    catch (Exception ex)
    {
        _logger.LogError("Emergency stop failed: {Exception}", ex);
        
        // Last resort - terminate entire application
        Environment.Exit(-1);
        return -1; // Never reached
    }
}
```

### Non-Blocking Disposal Pattern

Ensure disposal never blocks:

```csharp
protected override void Dispose(bool disposing)
{
    if (disposing && !_disposed)
    {
        _disposed = true;
        
        // Cancel everything immediately
        _cancellationTokenSource?.Cancel();
        
        // Don't wait for async cleanup - just fire and forget
        _ = Task.Run(async () =>
        {
            try
            {
                await FireAndForgetSessionCleanup();
            }
            catch
            {
                // Ignore all exceptions during fire-and-forget cleanup
            }
        });
        
        // Force kill process without waiting
        try
        {
#if NET5_0_OR_GREATER
            _process?.Kill(entireProcessTree: true);
#else
            _process?.Kill();
#endif
        }
        catch
        {
            // Ignore kill exceptions
        }
        
        // Dispose resources immediately
        _testFramework?.Dispose();
        _process?.Dispose();
    }
    
    base.Dispose(disposing);
}

// Async disposal that never hangs
protected override async ValueTask DisposeAsyncCore()
{
    if (!_disposed)
    {
        _disposed = true;
        
        // Very short timeout for any async cleanup
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        
        try
        {
            await FireAndForgetSessionCleanup();
        }
        catch (OperationCanceledException)
        {
            // Expected timeout - proceed with force cleanup
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Async disposal cleanup failed: {Exception}", ex);
        }
        
        // Force terminate regardless of cleanup result
        await ForceTerminateProcess();
    }
}
```

### Configuration for Aggressive Mode

Add configuration to control how aggressive the termination should be:

```csharp
public class BridgeConfiguration
{
    public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ThreadCleanupTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public bool EnableProcessTreeKill { get; set; } = true;
    public bool LogForcedTerminations { get; set; } = true;
    
    // New aggressive options
    public bool EnableNuclearTermination { get; set; } = true;
    public TimeSpan NuclearTimeoutSeconds { get; set; } = TimeSpan.FromSeconds(10);
    public bool AllowEnvironmentExit { get; set; } = true;
    public bool SkipSessionCleanupOnTimeout { get; set; } = true;
}
```

### Decision Matrix: Updated for Hanging Scenarios

| Scenario | Recommended Action | Timeout | Nuclear Fallback |
|----------|-------------------|---------|------------------|
| **First cancellation attempt** | Fire-and-forget cleanup | 5 seconds | Nuclear timer |
| **Cleanup hangs** | Skip to process kill | Immediate | Environment.Exit |
| **Process kill hangs** | Environment.Exit | 3 seconds | System termination |
| **Repeated hanging** | Use EmergencyStop() | 1 second | Force app exit |
| **Bridge disposal hangs** | Non-blocking disposal | Immediate | Fire-and-forget |

### Implementation Priority

When the bridge hangs even with graceful approaches:

1. **Use fire-and-forget cleanup** - don't wait for session events
2. **Implement nuclear timer** - absolute fallback after 10 seconds  
3. **Skip cleanup entirely** if any cleanup operation times out
4. **Use EmergencyStop()** for repeated hanging scenarios
5. **Allow Environment.Exit** as ultimate fallback

### Framework-Specific Considerations

```csharp
// .NET 5+ approach with process tree killing
#if NET5_0_OR_GREATER
private async Task<bool> TryKillProcessTree(TimeSpan timeout)
{
    try
    {
        _process.Kill(entireProcessTree: true);
        using var cts = new CancellationTokenSource(timeout);
        await _process.WaitForExitAsync(cts.Token);
        return true;
    }
    catch
    {
        return false;
    }
}
#else
// .NET Framework fallback
private bool TryKillProcessTree(TimeSpan timeout)
{
    try
    {
        _process.Kill();
        return _process.WaitForExit((int)timeout.TotalMilliseconds);
    }
    catch
    {
        return false;
    }
}
#endif
```

The key insight is that **any cleanup operation that can potentially hang should have aggressive timeouts and fallbacks**. If the VSTestBridge message bus or session lifecycle operations are blocking, skip them entirely and proceed directly to process termination.
