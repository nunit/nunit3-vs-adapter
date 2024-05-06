using System;
using System.Collections.Generic;
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
    internal sealed class NUnitBridgedTestFramework : SynchronizedSingleSessionVSTestBridgedTestFramework
    {
        public NUnitBridgedTestFramework(NUnitExtension extension, Func<IEnumerable<Assembly>> getTestAssemblies,
            IServiceProvider serviceProvider, ITestFrameworkCapabilities capabilities)
            : base(extension, getTestAssemblies, serviceProvider, capabilities)
        {
        }

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
            ITestExecutor executor = new NUnit3TestExecutor();
            using (cancellationToken.Register(executor.Cancel))
            {
                if (request.VSTestFilter.TestCases is { } testCases)
                {
                    executor.RunTests(testCases, request.RunContext, request.FrameworkHandle);
                }
                else
                {
                    executor.RunTests(request.AssemblyPaths, request.RunContext, request.FrameworkHandle);
                }
            }

            return Task.CompletedTask;
        }
    }
}
