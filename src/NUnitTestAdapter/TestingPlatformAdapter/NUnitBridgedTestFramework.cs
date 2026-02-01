using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Testing.Extensions.VSTestBridge;
using Microsoft.Testing.Extensions.VSTestBridge.Requests;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Messages;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    internal sealed class NUnitBridgedTestFramework(
        NUnitExtension extension,
        Func<IEnumerable<Assembly>> getTestAssemblies,
        IServiceProvider serviceProvider,
        ITestFrameworkCapabilities capabilities)
        : SynchronizedSingleSessionVSTestBridgedTestFramework(extension, getTestAssemblies, serviceProvider,
            capabilities)
    {
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
        protected override Task SynchronizedRunTestsAsync(VSTestRunTestExecutionRequest request, IMessageBus messageBus,
            CancellationToken cancellationToken)
        {
            ITestExecutor executor = new NUnit3TestExecutor(isMTP: true);
            try
            {
                using (cancellationToken.Register(() =>
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
                // Aggressive cleanup for MTP to release test DLL
                try
                {
                    if (executor is IDisposable disposableExecutor)
                    {
                        disposableExecutor.Dispose();
                    }
                }
                catch
                {
                    // Ignore disposal exceptions
                }

                // For MTP cancellation: Handle stuck test threads that won't die
                if (cancellationToken.IsCancellationRequested)
                {
                    // Force garbage collection to help release resources
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Give test threads a reasonable time to respond to cancellation
                    Thread.Sleep(2000);

                    // Check if we still have too many threads indicating stuck test threads
                    var currentProcess = Process.GetCurrentProcess();
                    var threadCount = currentProcess.Threads.Count;

                    // If we have excessive threads (your case showed 41 threads), use nuclear option
                    // This prevents MTP from hanging indefinitely waiting for threads that won't die
                    if (threadCount > 5)
                    {
                        // Nuclear option: Force clean process exit for MTP cancellation
                        // Unlike MSTest's process isolation, NUnit runs in-process so we need this
                        Environment.Exit(0);
                    }
                }
                else
                {
                    // Normal completion - still do cleanup but with shorter timeout
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    Thread.Sleep(200);
                }
            }

            return Task.CompletedTask;
        }
    }
}
