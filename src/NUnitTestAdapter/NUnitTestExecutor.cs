// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Core;
using NUnit.Core.Filters;
using NUnit.Util;
// #define LAUNCHDEBUGGER

namespace NUnit.VisualStudio.TestAdapter
{
    using System.Diagnostics;

    [ExtensionUri(NUnitTestExecutor.ExecutorUri)]
    public sealed class NUnitTestExecutor : NUnitTestAdapter, ITestExecutor
    {
        ///<summary>
        /// The Uri used to identify the NUnitExecutor
        ///</summary>
        public const string ExecutorUri = "executor://NUnitTestExecutor";

        /// <summary>
        /// The current NUnit TestRunner instance
        /// </summary>
        private TestRunner runner;

        #region ITestExecutor

        private bool isCalledFromTfsBuild;
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
            Logger = frameworkHandle;
            Info("executing tests", "started");
            try
            {
                // Ensure any channels registered by other adapters are unregistered
                CleanUpRegisteredChannels();
                var tfsfilter = new TFSTestFilter(runContext);
                isCalledFromTfsBuild = tfsfilter.TfsTestCaseFilterExpression != null;
                this.SendDebugMessage("Keepalive:" + runContext.KeepAlive);
                if (!isCalledFromTfsBuild && runContext.KeepAlive)
                    frameworkHandle.EnableShutdownAfterTestRun = true;
                foreach (var source in sources)
                {
                    using (var filter = (isCalledFromTfsBuild) ? new TfsAssemblyFilter(source, runContext) : new AssemblyFilter(source))
                    {
                        this.RunAssembly(frameworkHandle, filter);
                    }
                }
            }
            catch (Exception ex)
            {
                SendErrorMessage("Exception " + ex);
            }
            finally
            {
                Info("executing tests", "finished");
            }

        }

        /// <summary>
        /// Called by the VisualStudio IDE when selected tests are to be run. Never called from TFS Build.
        /// </summary>
        /// <param name="selectedTests">The tests to be run</param>
        /// <param name="runContext">The RunContext</param>
        /// <param name="frameworkHandle">The FrameworkHandle</param>
        public void RunTests(IEnumerable<TestCase> selectedTests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if LAUNCHDEBUGGER
            Debugger.Launch();
#endif

            Logger = frameworkHandle;
            if (runContext.KeepAlive)
                frameworkHandle.EnableShutdownAfterTestRun = true;
            Info("executing tests", "started");

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();
            isCalledFromTfsBuild = false;
            var assemblyGroups = selectedTests.GroupBy(tc => tc.Source);
            foreach (var assemblyGroup in assemblyGroups)
            {
                using (var filter = AssemblyFilter.Create(assemblyGroup.Key, assemblyGroup))
                {
                    this.RunAssembly(frameworkHandle, filter);
                }

            }
            Info("executing tests", "finished");

        }

        void ITestExecutor.Cancel()
        {
            if (runner != null && runner.Running)
                runner.CancelRun();
        }

        #endregion

        #region Private Methods



        private void RunAssembly(ITestExecutionRecorder testLog, AssemblyFilter filter)
        {

            try
            {
#if LAUNCHDEBUGGER
            Debugger.Launch();
#endif
                this.runner = new TestDomain();
                var package = new TestPackage(filter.AssemblyName);
                if (runner.Load(package))
                {
                    filter.AddTestCases(runner.Test as TestNode);
                    var listener = new NUnitEventListener(testLog, filter);
                    try
                    {
                        filter.ProcessTfsFilter();
                        runner.Run(listener, filter.NUnitFilter, true, LoggingThreshold.Off);
                    }
                    catch (NullReferenceException)
                    {
                        // this happens during the run when CancelRun is called.
                        this.SendDebugMessage("Nullref catched");
                    }
                    finally
                    {
                        runner.Unload();
                    }
                }
                else
                {
                    NUnitLoadError(filter.AssemblyName);
                }
            }
            catch (System.BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                AssemblyNotSupportedWarning(filter.AssemblyName);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                DependentAssemblyNotFoundWarning(ex.FileName, filter.AssemblyName);
            }
            catch (Exception ex)
            {
                SendErrorMessage("Exception thrown executing tests in " + filter.AssemblyName, ex);
            }
        }




        

        #endregion
    }
}
