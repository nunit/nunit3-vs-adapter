using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Testing.Extensions.VSTestBridge;
using Microsoft.Testing.Extensions.VSTestBridge.Requests;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Messages;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Engine;
using NUnit.Engine.Internal;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    internal sealed class NUnitBridgedTestFramework : SynchronizedSingleSessionVSTestBridgedTestFramework
    {
        private static readonly object InitializationLock = new();
        private static bool initialized;

        public NUnitBridgedTestFramework(NUnitExtension extension, Func<IEnumerable<Assembly>> getTestAssemblies,
            IServiceProvider serviceProvider, ITestFrameworkCapabilities capabilities)
            : base(extension, getTestAssemblies, serviceProvider, capabilities)
        {
            PatchNUnit3InternalLoggerBug();
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

        private static void PatchNUnit3InternalLoggerBug()
        {
            // NUnit3 will internally call InternalTracer.Initialize(...), and it will check that it is not called multiple times.
            // We call it multiple times in server mode, in indeterminate order (it does not always have to be Discover and then Run),
            // When InternalTracer.Initialize is called with Off, it will not set traceWriter internal field to anything, but when you call it again
            // it will try to write "Initialize was already called" via this writer without null check and will fail with NRE.
            //
            // Patch for this was implemented in NUnit4, but won't be backported to NUnit3 because we are hitting a bit of an edge case:
            // https://github.com/nunit/nunit/issues/4255
            //
            // To fix us, we set the writer to a null writer, so any subsequent calls to Initialize will write to null instead of failing.
            // We also need to do this under a lock, and not rely on the InternalTracer.Initialized, because that might be set to late and we would
            // re-enter the method twice.
            if (initialized)
            {
                return;
            }

            lock (InitializationLock)
            {
                if (initialized)
                {
                    return;
                }

                // TODO: Uncomment this line when InternalsVisibleTo is set up.
                // var nopWriter = new InternalTraceWriter(new StreamWriter(Stream.Null));

                // Initialize log level to be Off (see issue https://github.com/microsoft/testanywhere/issues/1369)
                // because we don't have settings from the request available yet, and the internal tracer is static, so it would set to the
                // level that is set by the first request and always keep it that way.
                //
                // Alternatively we could hook this up to a ILogger, and write the logs via a TextWriter.
                // TODO: Uncomment this line when InternalsVisibleTo is set up.
                // InternalTrace.Initialize(nopWriter, InternalTraceLevel.Off);

                // When we allow the trace level to be set then we need to set the internal writer field only when the level is Off.
                // In that case you will need to do something like this:
                // FieldInfo traceLevelField = typeof(InternalTrace).GetField("traceLevel", BindingFlags.Static | BindingFlags.NonPublic)!;
                // bool isOff = ((InternalTraceLevel)traceLevelField.GetValue(null)!) != InternalTraceLevel.Off;
                // if (isOff)
                // {
                //    FieldInfo traceWriterField = typeof(InternalTrace).GetField("traceWriter", BindingFlags.Static | BindingFlags.NonPublic)!;
                //    traceWriterField.SetValue(null, nopWriter);
                // }

                // TODO: Uncomment these lines when InternalsVisibleTo is set up.
                // FieldInfo traceWriterField = typeof(InternalTrace).GetField("traceWriter", BindingFlags.Static | BindingFlags.NonPublic)!;
                // traceWriterField.SetValue(null, nopWriter);

                initialized = true;
            }
        }
    }
}
