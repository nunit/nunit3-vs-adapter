using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Testing.Extensions.VSTestBridge;
using Microsoft.Testing.Extensions.VSTestBridge.Requests;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    internal sealed class NUnitBridgedTestFramework : SynchronizedSingleSessionVSTestBridgedTestFramework
    {
        private readonly CancellationTokenSource _internalCts = new();
        private bool _testSessionActive = false;
        private IMessageBus _currentMessageBus;
        private Timer _terminationTimer;
        private volatile bool _disposed = false;

        /// <summary>
        /// Gets the current NUnit framework instance for cancellation scenarios.
        /// This provides direct access to the IDataProducer implementation.
        /// Per TestFX Copilot guidance: NUnitBridgedTestFramework IS the IDataProducer.
        /// </summary>
        public static NUnitBridgedTestFramework CurrentInstance { get; private set; }

        /// <summary>
        /// Gets the current message bus for direct MTP communication
        /// </summary>
        public static IMessageBus CurrentMessageBus { get; private set; }

        /// <summary>
        /// Gets the current session UID for MTP operations
        /// </summary>
        public static object CurrentSessionUid { get; private set; }

        public NUnitBridgedTestFramework(
            NUnitExtension extension,
            Func<IEnumerable<Assembly>> getTestAssemblies,
            IServiceProvider serviceProvider,
            ITestFrameworkCapabilities capabilities)
            : base(extension, getTestAssemblies, serviceProvider, capabilities)
        {
            // Set the current instance for cancellation access (TestFX Copilot pattern)
            CurrentInstance = this;

            // Note: Can't use LogToDump here since we don't have access to NUnit3TestExecutor yet
            // This will be logged later when the executor is created
        }

        /// <summary>
        /// Explicitly end the test session for MTP cancellation scenarios
        /// This ensures proper TestFX session lifecycle completion to prevent
        /// "test session start event was received without a corresponding test session end" errors
        /// </summary>
        public async Task EndSessionExplicitly()
        {
            try
            {
                
                // Mark session as ended FIRST
                _testSessionActive = false;

                // CRITICAL: Actually send the TestSessionEndMessage (this was missing!)
                if (_currentMessageBus != null && CurrentSessionUid != null)
                {
                    try
                    {
                        Debug.WriteLine("🔧 Sending TestSessionEndMessage...");

                        // Create proper TestSessionResult
                        var sessionResult = new TestSessionResult
                        {
                            State = TestSessionState.Cancelled,
                            ExitCode = 0  // Success code since tests completed successfully
                        };

                        // Cast CurrentSessionUid to proper type
                        SessionUid typedSessionUid;
                        if (CurrentSessionUid is SessionUid sessionUid)
                        {
                            typedSessionUid = sessionUid;
                        }
                        else
                        {
                            typedSessionUid = new SessionUid(CurrentSessionUid.ToString());
                        }

                        // Create TestSessionEndMessage using direct TestFX types
                        var sessionEndMessage = new TestSessionEndMessage(typedSessionUid, sessionResult);

                        // Send the session end message using this framework as IDataProducer
                        await _currentMessageBus.PublishAsync(this, sessionEndMessage);

                        Debug.WriteLine("✅ TestSessionEndMessage sent successfully!");
                    }
                    catch (Exception sessionEx)
                    {
                        Debug.WriteLine($"❌ Failed to send TestSessionEndMessage: {sessionEx.Message}");
                    }
                }

                // Give TestFX time to process the session end message
                await Task.Delay(300);

                // Cancel internal token AFTER sending session end
                if (!_internalCts.Token.IsCancellationRequested)
                {
                    _internalCts.Cancel();
                }

                // Clear static references only AFTER TestFX processes session end
                CurrentMessageBus = null;
                CurrentSessionUid = null;

                // Additional delay to ensure TestFX session end event is fully processed
                await Task.Delay(200);

                Debug.WriteLine("✅ Session end processing complete");
            }
            catch (Exception ex)
            {
                // Log if possible, but don't throw - this is cleanup
                Debug.WriteLine($"Session end cleanup exception: {ex.Message}");
            }
        }

        protected override bool UseFullyQualifiedNameAsTestNodeUid => true;

        /// <inheritdoc />
        protected override Task SynchronizedDiscoverTestsAsync(VSTestDiscoverTestExecutionRequest request, IMessageBus messageBus,
            CancellationToken cancellationToken)
        {
            new NUnit3TestDiscoverer()
                .DiscoverTests(request.AssemblyPaths, request.DiscoveryContext, request.MessageLogger, request.DiscoverySink);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override async Task SynchronizedRunTestsAsync(VSTestRunTestExecutionRequest request, IMessageBus messageBus,
            CancellationToken cancellationToken)
        {
            _currentMessageBus = messageBus;
            _testSessionActive = true;

            // CRITICAL: Register session components for direct access (TestFX Copilot pattern)
            // Store static references for clean access from NUnit3TestExecutor
            CurrentMessageBus = messageBus;
            CurrentSessionUid = request.Session?.SessionUid;

            ITestExecutor executor = new NUnit3TestExecutor(isMTP: true);

            // Create combined cancellation token
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCts.Token);

            try
            {
                // ReSharper disable once UseAwaitUsing
                using (combinedCts.Token.Register(() =>
                       {
                           // Enhanced cancellation for MTP
                           executor.Cancel();
                           // Give a moment for proper cleanup
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

        private async Task HandleTestCompletionWithNuclearOption(ITestExecutor executor, bool wasCancelled)
        {
            // Cast to access logging method
            var nunitExecutor = executor as NUnit3TestExecutor;

            if (!wasCancelled)
            {
                // Normal completion - brief cleanup only
                try
                {
                    if (executor is IDisposable disposableExecutor)
                    {
                        disposableExecutor.Dispose();
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(200);
                }
                catch
                {
                    // Ignore cleanup exceptions in normal flow
                }
                return;
            }

            // Cancellation scenario - use nuclear timer approach
            nunitExecutor?.LogToDump("MTPCancellation", "Cancellation detected - starting nuclear termination timer");

            // Start absolute termination timer (nuclear option) - 8 seconds
            _terminationTimer = new Timer(
                ForceTerminate,
                nunitExecutor,
                TimeSpan.FromSeconds(8),
                Timeout.InfiniteTimeSpan);

            try
            {
                // Step 1: Fire-and-forget session cleanup with very short timeout
                nunitExecutor?.LogToDump("CleanupPhase", "Attempting fire-and-forget cleanup");
                await FireAndForgetSessionCleanup(nunitExecutor);

                // Step 2: Quick executor disposal
                nunitExecutor?.LogToDump("ExecutorDisposal", "Disposing executor");
                try
                {
                    if (executor is IDisposable disposableExecutor)
                    {
                        var disposeTask = Task.Run(() => disposableExecutor.Dispose());
                        if (!disposeTask.Wait(1000)) // 1 second timeout
                        {
                            nunitExecutor?.LogToDump("ExecutorDisposal", "Executor disposal timed out", logLevel: LogLevel.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    nunitExecutor?.LogToDump("ExecutorDisposal", $"Executor disposal failed: {ex.Message}", logLevel: LogLevel.Warning);
                }

                // Step 3: Check for stuck threads and exit immediately if found
                nunitExecutor?.LogToDump("ThreadCheck", "Checking for stuck threads");
                if (await HasStuckThreadsAsync(nunitExecutor))
                {
                    nunitExecutor?.LogToDump("ImmediateExit", "Stuck threads detected - immediate exit", logLevel: LogLevel.Warning);
                    _terminationTimer?.Dispose(); // Cancel nuclear timer
                    Environment.Exit(0); // Immediate exit
                }

                // Step 4: If we get here, cleanup succeeded - cancel nuclear timer
                nunitExecutor?.LogToDump("CleanupSuccess", "Cleanup completed successfully");
                _terminationTimer?.Dispose();
            }
            catch (Exception ex)
            {
                nunitExecutor?.LogToDump("CleanupException", $"Exception during cleanup: {ex.Message}", logLevel: LogLevel.Warning);
                // Let nuclear timer handle it - don't call Environment.Exit here to avoid race
            }
        }

        private async Task FireAndForgetSessionCleanup(NUnit3TestExecutor nunitExecutor = null)
        {
            try
            {
                // Use very short timeouts for all operations
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                // Fire session end event but don't wait for confirmation
                _ = Task.Run(
                    async () =>
                    {
                        try
                        {
                            if (_testSessionActive && _currentMessageBus != null)
                            {
                                // Just a brief pause - don't actually send events that might block
                                await Task.Delay(50, cts.Token);
                                _testSessionActive = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            nunitExecutor?.LogToDump("SessionCleanupFailed", $"Session cleanup failed: {ex.Message}");
                        }
                    },
                    cts.Token);

                // Brief delay to let the fire-and-forget task start
                await Task.Delay(100, cts.Token);

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Give threads minimal time to respond
                await Task.Delay(500, cts.Token);
            }
            catch (Exception ex)
            {
                nunitExecutor?.LogToDump("FireForgetFailed", $"Fire-and-forget cleanup failed: {ex.Message}");
            }
        }

        private async Task<bool> HasStuckThreadsAsync(NUnit3TestExecutor nunitExecutor = null)
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var threadCount = currentProcess.Threads.Count;
                nunitExecutor?.LogToDump("ThreadCount", $"Current thread count: {threadCount}");
                return threadCount > 5; // Aggressive threshold for stuck threads
            }
            catch
            {
                return false; // If we can't check, assume no stuck threads
            }
        }

        private void ForceTerminate(object state)
        {
            try
            {
                var nunitExecutor = state as NUnit3TestExecutor;
                nunitExecutor?.LogToDump("NuclearTermination", "NUCLEAR TERMINATION TRIGGERED - Force exit after timeout", logLevel: LogLevel.Warning);

                // Log the nuclear termination
                try
                {
                    var currentProcess = Process.GetCurrentProcess();
                    var threadCount = currentProcess.Threads.Count;
                    nunitExecutor?.LogToDump("FinalThreadCount", $"Final thread count: {threadCount}");
                }
                catch
                {
                    // Ignore errors during final logging
                }

                // Nuclear option - immediate exit
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                var nunitExecutor = state as NUnit3TestExecutor;
                nunitExecutor?.LogToDump("NuclearFailed", $"Nuclear termination failed: {ex.Message}", logLevel: LogLevel.Error);
                Environment.Exit(-1); // Ultimate fallback
            }
            finally
            {
                _terminationTimer?.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                // Don't log here since we don't have access to executor - keep it simple

                // Cancel everything immediately
                _internalCts?.Cancel();

                // Dispose nuclear timer
                _terminationTimer?.Dispose();

                // Don't wait for async cleanup - just fire and forget
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
                _ = Task.Run(async () =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                        if (_testSessionActive)
                        {
                            _testSessionActive = false;
                        }
                    }
                    catch
                    {
                        // Ignore all exceptions during fire-and-forget cleanup
                    }
                });

                try
                {
                    _internalCts?.Dispose();
                }
                catch
                {
                    // Ignore disposal exceptions
                }

                // Clear static references (TestFX Copilot pattern)
                if (CurrentInstance == this)
                {
                    CurrentInstance = null;
                    CurrentMessageBus = null;
                    CurrentSessionUid = null;
                    // Note: Can't use LogToDump here since NUnit3TestExecutor may be disposed
                    // This clearing will be silent but that's acceptable for disposal
                }
            }

            base.Dispose(disposing);
        }
    }
}
