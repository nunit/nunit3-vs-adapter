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
                foreach (var source in sources) RunAssembly(source, frameworkHandle, TestFilter.Empty, runContext);
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
                RunAssembly(assemblyGroup.Key, frameworkHandle, MakeTestFilter(assemblyGroup), runContext);
            Info("executing tests", "finished");

        }

        void ITestExecutor.Cancel()
        {
            if (runner != null && runner.Running)
                runner.CancelRun();
        }

        #endregion

        #region Private Methods

        // List of test cases used during execution
        private List<TestCase> vsTestCases;

        private void RunAssembly(string assemblyName, ITestExecutionRecorder testLog, TestFilter filter, IRunContext runContext)
        {

            try
            {
#if LAUNCHDEBUGGER
            Debugger.Launch();
#endif
                this.runner = new TestDomain();
                var package = new TestPackage(assemblyName);
                var testDictionary = new Dictionary<string, TestNode>();
                var converter = new TestConverter(assemblyName, testDictionary, isCalledFromTfsBuild);
                if (runner.Load(package))
                {
                    #region DEBUG
#if DEBUG
                    foreach (ITest test in runner.Test.Tests)
                    {
                        this.SendDebugMessage(String.Format("Test:  {0} ", test.TestName.FullName));
                        foreach (ITest testc in test.Tests)
                        {
                            this.SendDebugMessage(String.Format("Test:  {0} ", testc.TestName.FullName));
                            foreach (ITest testm in testc.Tests)
                            {
                                this.SendDebugMessage(String.Format("Test:  {0} {1} ", testm.TestName.FullName, testm.TestName.TestID));

                            }
                        }
                    }
#endif
                    #endregion

                    var testCaseMap = CreateTestCaseMap(runner.Test as TestNode, converter);
                    var listener = new NUnitEventListener(testLog, testCaseMap, assemblyName, isCalledFromTfsBuild);
                    this.SendDebugMessage("TFS Build : " + this.isCalledFromTfsBuild);
                    try
                    {
                        if (isCalledFromTfsBuild)
                        {
                            this.SendDebugMessage("TFS Build - setting up filter");
                            var testfilter = new TFSTestFilter(runContext);
                            var filteredTestCases = testfilter.CheckFilter(vsTestCases);
                            filter = MakeTestFilter(filteredTestCases);
                            this.SendDebugMessage("No of cases found" + vsTestCases.Count() + " after filter = " + filteredTestCases.Count());
                        }
                        runner.Run(listener, filter, false, LoggingThreshold.Off);
                    }
                    catch (NullReferenceException)
                    {
                        // this happens during the run when CancelRun is called.
                        this.SendDebugMessage("Nullref catched");
                    }
                    finally
                    {
                        listener.Dispose();
                        runner.Unload();
                    }
                }
                else
                {
                    NUnitLoadError(assemblyName);
                }
            }
            catch (System.BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                AssemblyNotSupportedWarning(assemblyName);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                DependentAssemblyNotFoundWarning(ex.FileName, assemblyName);
            }
            catch (Exception ex)
            {
                SendErrorMessage("Exception thrown executing tests in " + assemblyName, ex);
            }
        }


        private Dictionary<string, NUnit.Core.TestNode> CreateTestCaseMap(TestNode topLevelTest, TestConverter converter)
        {
            var nunitTestCaseMap = new Dictionary<string, NUnit.Core.TestNode>();
            vsTestCases = new List<TestCase>();
            AddTestCases(nunitTestCaseMap, topLevelTest, converter);

            return nunitTestCaseMap;
        }

        private void AddTestCases(Dictionary<string, NUnit.Core.TestNode> nunitTestCaseMap, TestNode test, TestConverter converter)
        {
            if (test.IsSuite)
                foreach (TestNode child in test.Tests) AddTestCases(nunitTestCaseMap, child, converter);
            else
            {
                vsTestCases.Add(converter.ConvertTestCase(test));
                nunitTestCaseMap.Add(test.TestName.UniqueName, test);

            }
        }

        private TestFilter MakeTestFilter(IEnumerable<TestCase> ptestCases)
        {
            var filter = new SimpleNameFilter();
            foreach (TestCase testCase in ptestCases)
            {
                filter.Add(testCase.FullyQualifiedName);
            }
            return filter;
        }

        #endregion
    }
}
