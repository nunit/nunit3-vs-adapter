// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Core;
// #define LAUNCHDEBUGGER

namespace NUnit.VisualStudio.TestAdapter
{

    [ExtensionUri(ExecutorUri)]
    public sealed class NUnitTestExecutor : NUnitTestAdapter, ITestExecutor, IDisposable
    {
        ///<summary>
        /// The Uri used to identify the NUnitExecutor
        ///</summary>
        public const string ExecutorUri = "executor://NUnitTestExecutor";

        // The currently executing assembly runner
        private AssemblyRunner currentRunner;

        #region ITestExecutor

        /// <summary>
        /// Called by the Visual Studio IDE to run all tests. Also called by TFS Build
        /// to run either all or selected tests. In the latter case, a filter is provided
        /// as part of the run context.
        /// </summary>
        /// <param name="sources">Sources to be run.</param>
        /// <param name="runContext">Context to use when executing the tests.</param>
        /// <param name="frameworkHandle">Test log to send results and messages through</param>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            TestLog.Initialize(frameworkHandle);
            Info("executing tests", "started");

            try
            {
                // Ensure any channels registered by other adapters are unregistered
                CleanUpRegisteredChannels();

                var tfsfilter = new TfsTestFilter(runContext);
                TestLog.SendDebugMessage("Keepalive:" + runContext.KeepAlive);
                var enableShutdown = (UseVsKeepEngineRunning) ? !runContext.KeepAlive : true;
                if (!tfsfilter.HasTfsFilterValue)
                {
                    frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
                }
                
                foreach (var source in sources)
                {
                    using (currentRunner = new AssemblyRunner(TestLog, source, tfsfilter))
                    {
                        currentRunner.RunAssembly(frameworkHandle);
                    }

                    currentRunner = null;
                }
            }
            catch (Exception ex)
            {
                TestLog.SendErrorMessage("Exception " + ex);
            }
            finally
            {
                Info("executing tests", "finished");
            }

        }

        /// <summary>
        /// Called by the VisualStudio IDE when selected tests are to be run. Never called from TFS Build.
        /// </summary>
        /// <param name="tests">The tests to be run</param>
        /// <param name="runContext">The RunContext</param>
        /// <param name="frameworkHandle">The FrameworkHandle</param>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if LAUNCHDEBUGGER
            Debugger.Launch();
#endif

            TestLog.Initialize(frameworkHandle);
            var enableShutdown = (UseVsKeepEngineRunning) ? !runContext.KeepAlive : true;
            frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
            Debug("executing tests", "EnableShutdown set to " +enableShutdown);
            Info("executing tests", "started");

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            var assemblyGroups = tests.GroupBy(tc => tc.Source);
            foreach (var assemblyGroup in assemblyGroups)
            {
                using (currentRunner = new AssemblyRunner(TestLog, assemblyGroup.Key, assemblyGroup))
                {
                    currentRunner.RunAssembly(frameworkHandle);
                }

                currentRunner = null;
            }

            Info("executing tests", "finished");

        }

        void ITestExecutor.Cancel()
        {
            if (currentRunner != null)
                currentRunner.CancelRun();
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (currentRunner != null)
                {
                    currentRunner.Dispose();
                }
            }
            currentRunner = null;
        }
    }
}
