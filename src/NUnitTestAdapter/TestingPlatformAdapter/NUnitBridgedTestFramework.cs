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
using Microsoft.Testing.Platform.Extensions.TestFramework;
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
        protected override Task SynchronizedDiscoverTestsAsync(
            VSTestDiscoverTestExecutionRequest request,
            IMessageBus messageBus,
            CancellationToken cancellationToken)
        {
            new NUnit3TestDiscoverer()
                .DiscoverTests(request.AssemblyPaths, request.DiscoveryContext, request.MessageLogger,
                    request.DiscoverySink);

            return Task.CompletedTask;
        }

        protected override async Task SynchronizedRunTestsAsync(
        VSTestRunTestExecutionRequest request,
        IMessageBus messageBus,
        CancellationToken cancellationToken)
        {
            // Can't use nunitExecutor logging yet, so use a temporary approach
            LogMessage("SynchronizedRunTestsAsync: ENTRY");

            _currentMessageBus = messageBus;
            _testSessionActive = true;
            CurrentMessageBus = messageBus;
            CurrentSessionUid = request.Session?.SessionUid;

            LogMessage("SynchronizedRunTestsAsync: Creating executor");
            ITestExecutor executor = new NUnit3TestExecutor(isMTP: true);
            var nunitExecutor = executor as NUnit3TestExecutor;

            nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "Creating cancellation token");
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _internalCts.Token);

            try
            {
                nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "Registering cancellation callback");
                using (combinedCts.Token.Register(() =>
                {
                    nunitExecutor?.LogToDump("CANCELLATION CALLBACK", "Starting");
                    executor.Cancel();
                    Thread.Sleep(100);
                    nunitExecutor?.LogToDump("CANCELLATION CALLBACK", "Completed");
                }))
                {
                    nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "Calling executor.RunTests - START");
                    executor.RunTests(request.AssemblyPaths, request.RunContext, request.FrameworkHandle);
                    nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "Calling executor.RunTests - COMPLETED");
                }
                nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "Exited using block");
            }
            finally
            {
                nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "FINALLY block - calling HandleTestCompletion");
                await HandleTestCompletion(executor, combinedCts.Token.IsCancellationRequested);
                nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "FINALLY block - HandleTestCompletion completed");
            }

            nunitExecutor?.LogToDump("SynchronizedRunTestsAsync", "EXIT");
        }

        // Add this helper method to your bridge class for the initial logging before we have nunitExecutor
        private void LogMessage(string message)
        {
            // You can replace this with your preferred logging mechanism
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            // Or if you have another logging method available in the bridge class, use that
        }

        private async Task HandleTestCompletion(ITestExecutor executor, bool wasCancelled)
        {
            _testSessionActive = false;

            // Simple direct exit for cancelled runs to avoid platform session hang
            if (wasCancelled)
            {
                // Give platform a moment to try session cleanup
                await Task.Delay(1000);
                Environment.Exit(0);
            }
        }


        /*
        private async Task HandleTestCompletion(ITestExecutor executor, bool wasCancelled)
        {
            var nunitExecutor = executor as NUnit3TestExecutor;
            try
            {
                nunitExecutor?.LogToDump("HandleTestCompletion", "Starting dispose...");

                // Only dispose the executor
                if (executor is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _testSessionActive = false;

                nunitExecutor?.LogToDump(
                    "HandleTestCompletion",
                    $"Test completion handled - wasCancelled: {wasCancelled}");
            }
            catch (Exception ex)
            {
                nunitExecutor?.LogToDump("HandleTestCompletion", $"Error in test completion: {ex.Message}");
            }

            // NO finally block - NO cleanup - let Dispose() handle everything
            nunitExecutor?.LogToDump("HandleTestCompletion", "EXIT: HandleTestCompletion completed normally");
        }
        */

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
    }
}
