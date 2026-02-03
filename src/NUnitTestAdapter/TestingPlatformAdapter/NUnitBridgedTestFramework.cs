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
                await HandleTestCompletion(executor, combinedCts.Token.IsCancellationRequested);
            }
        }

        private async Task HandleTestCompletion(ITestExecutor executor, bool wasCancelled)
        {
            var nunitExecutor = executor as NUnit3TestExecutor;
            try
            {
                nunitExecutor?.LogToDump("HandleTestCompletion", "Starting dispose...");
                // Simple resource cleanup
                if (executor is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                // Mark session as inactive (but don't send messages - platform handles it)
                _testSessionActive = false;

                nunitExecutor?.LogToDump("HandleTestCompletion", "Dispose completed, marking session inactive...");
                nunitExecutor?.LogToDump("HandleTestCompletion", $"Test completion handled - wasCancelled: {wasCancelled}");
            }
            catch (Exception ex)
            {
                nunitExecutor?.LogToDump("HandleTestCompletion", $"Error in test completion: {ex.Message}");
            }
            finally
            {
                // CRITICAL: Add a small delay to let session end events be sent first
                if (wasCancelled)
                {
                    await Task.Delay(200); // Give platform time to send session end events
                }
                nunitExecutor?.LogToDump("HandleTestCompletion", "FINALLY: Starting cleanup...");

                // Clean up references - let platform handle session lifecycle

                CurrentMessageBus = null;
                // CurrentSessionUid = null;
                _currentMessageBus = null;
                nunitExecutor?.LogToDump("HandleTestCompletion", "FINALLY: Cleanup completed");
            }
            nunitExecutor?.LogToDump("HandleTestCompletion", "EXIT: HandleTestCompletion completed normally");

            // CRITICAL: Return normally - let CloseTestSessionAsync handle session end
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

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                // Simple cleanup only
                _internalCts?.Cancel();
                _internalCts?.Dispose();

                // Clear static references
                if (CurrentInstance == this)
                {
                    CurrentInstance = null;
                    CurrentMessageBus = null;
                    CurrentSessionUid = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Explicitly end the current session - used when cancelling tests
        /// </summary>
        public async Task EndSessionExplicitly()
        {
            try
            {
                if (_testSessionActive)
                {
                    _testSessionActive = false;

                    // Try to call the base class session end if available
                    if (_currentMessageBus != null)
                    {
                        // Let the base class handle proper session termination
                        await Task.CompletedTask;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - this is best-effort cleanup
                Debug.WriteLine($"EndSessionExplicitly failed: {ex.Message}");
            }
        }
    }
}
