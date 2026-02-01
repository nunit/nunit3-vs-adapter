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
