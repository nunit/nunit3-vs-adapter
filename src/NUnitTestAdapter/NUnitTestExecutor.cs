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
        /// Called by the test platform to run all tests.
        /// </summary>
        /// <param name="sources">Sources to be run.</param>
        /// <param name="runContext">Context to use when executing the tests.</param>
        /// <param param name="frameworkHandle">Test log to send results and messages through</param>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();
            var tfsfilter = new TFSTestFilter(runContext);
            isCalledFromTfsBuild = tfsfilter.TfsTestCaseFilterExpression != null;
            foreach (var source in sources)
                RunAssembly(source, frameworkHandle, TestFilter.Empty, runContext);
        }

        private void RunAssembly(string assemblyName, ITestExecutionRecorder testLog, TestFilter filter, IRunContext runContext)
        {

            SetLogger(testLog);

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
                            var filteredTestCases = testfilter.CheckFilter(testCases);
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
            catch (Exception ex)
            {
                SendErrorMessage("Exception thrown executing tests in " + assemblyName, ex);
            }
        }


        /// <summary>
        /// Called by the TestPlatform when selected tests are to be run.
        /// This method is called from Visual Studio when you select a group of tests to run.  It is never called from Tfs Build.
        /// </summary>
        /// <param name="selectedTests">The tests to be run</param>
        /// <param name="runContext">The RunContext</param>
        /// <param name="frameworkHandle">The FrameworkHandle</param>
        public void RunTests(IEnumerable<TestCase> selectedTests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();
            isCalledFromTfsBuild = false;
            var assemblyGroups = selectedTests.GroupBy(tc => tc.Source);

            foreach (var assemblyGroup in assemblyGroups)
                RunAssembly(assemblyGroup.Key, frameworkHandle, MakeTestFilter(assemblyGroup), runContext);
        }

        void ITestExecutor.Cancel()
        {
            if (runner != null && runner.Running)
                runner.CancelRun();
        }

        #endregion

        #region Private Methods

        private Dictionary<string, NUnit.Core.TestNode> CreateTestCaseMap(TestNode topLevelTest, TestConverter converter)
        {
            var map = new Dictionary<string, NUnit.Core.TestNode>();
            testCases = new List<TestCase>();
            AddTestCasesToMap(map, topLevelTest, converter);

            return map;
        }


        private List<TestCase> testCases;
        private void AddTestCasesToMap(Dictionary<string, NUnit.Core.TestNode> map, TestNode test, TestConverter converter)
        {
            if (test.IsSuite) 
                foreach (TestNode child in test.Tests) AddTestCasesToMap(map, child, converter);
            else
            {
                testCases.Add(converter.ConvertTestCase(test));
                map.Add(test.TestName.UniqueName, test);
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
