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

namespace NUnit.VisualStudio.TestAdapter
{
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
            frameworkHandle.EnableShutdownAfterTestRun = true;
            // Ensure any channels registered by other adapters are unregistered
            Info("executing tests","started");

            try
            {
                CleanUpRegisteredChannels();
                var tfsfilter = new TFSTestFilter(runContext);
                isCalledFromTfsBuild = tfsfilter.TfsTestCaseFilterExpression != null;
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
            Logger = frameworkHandle;
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
                this.runner = new TestDomain();
                var package = new TestPackage(assemblyName);
                var testDictionary = new Dictionary<string, TestNode>();
                var converter = new TestConverter(assemblyName, testDictionary);
                if (runner.Load(package))
                {

                    var testCaseMap = CreateTestCaseMap(runner.Test as TestNode, converter);
                    var listener = new NUnitEventListener(testLog, testCaseMap, assemblyName);

                    try
                    {
                        if (isCalledFromTfsBuild)
                        {
                            var testfilter = new TFSTestFilter(runContext);
                            var filteredTestCases = testfilter.CheckFilter(vsTestCases);
                            filter = MakeTestFilter(filteredTestCases);
                        }
                        runner.Run(listener, filter, false, LoggingThreshold.Off);
                    }
                    catch (NullReferenceException)
                    {
                        // this happens during the run when CancelRun is called.
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
            var filter = new NameFilter();

            foreach (TestCase testCase in ptestCases)
                filter.Add(TestName.Parse(testCase.FullyQualifiedName));

            return filter;
        }

        #endregion
    }
}
