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

            foreach (var source in SanitizeSources(sources))
            {
                RunAssembly(source, frameworkHandle, TestFilter.Empty);
            }
        }

        /// <summary>
        /// Called by the TestPlatform when selected tests are to be run.
        /// </summary>
        /// <param name="selectedTests">The tests to be run</param>
        /// <param name="runContext">The RunContext</param>
        /// <param name="frameworkHandle">The FrameworkHandle</param>
        public void RunTests(IEnumerable<TestCase> selectedTests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            var assemblyGroups = selectedTests.GroupBy(tc => tc.Source);

            foreach (var assemblyGroup in assemblyGroups)
            {
                var filter = new SimpleNameFilter();
                foreach (var testCase in assemblyGroup)
                    filter.Add(testCase.FullyQualifiedName);

                RunAssembly(assemblyGroup.Key, frameworkHandle, filter);
            }
        }

        void ITestExecutor.Cancel()
        {
            if (runner != null && runner.Running)
                runner.CancelRun();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs the tests in an assembly under control of a filter
        /// </summary>
        /// <param name="assemblyName">The assembly file name and path</param>
        /// <param name="testLog">The destination for all messages and events</param>
        /// <param name="filter">A test filter controlling what tests are run</param>
        private void RunAssembly(string assemblyName, ITestExecutionRecorder testLog, TestFilter filter)
        {
            try
            {
                this.runner = new TestDomain();
                TestPackage package = new TestPackage(assemblyName);

                if (runner.Load(package))
                {
                    var testCaseMap = CreateTestCaseMap(runner.Test as TestNode);

                    var listener = new NUnitEventListener(testLog, testCaseMap, assemblyName);

                    try
                    {
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
                    testLog.SendMessage(TestMessageLevel.Warning, LoadErrorMessage(assemblyName));
                }
            }
            catch (System.BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                testLog.SendMessage(TestMessageLevel.Warning, LoadErrorMessage(assemblyName));
            }
            catch (Exception e)
            {
                testLog.SendMessage(TestMessageLevel.Error, LoadErrorMessage(assemblyName));
                testLog.SendMessage(TestMessageLevel.Error, e.Message);
            }
        }

        private string LoadErrorMessage(string assemblyName)
        {
            return "Unable to load tests from " + assemblyName;
        }

        private Dictionary<string, NUnit.Core.TestNode> CreateTestCaseMap(TestNode topLevelTest)
        {
            var map = new Dictionary<string, NUnit.Core.TestNode>();
            AddTestCasesToMap(map, topLevelTest);

            return map;
        }

        private void AddTestCasesToMap(Dictionary<string, NUnit.Core.TestNode> map, TestNode test)
        {
            if (test.IsSuite)
                foreach (TestNode child in test.Tests)
                    AddTestCasesToMap(map, child);
            else
                map.Add(test.TestName.UniqueName, test);
        }

        #endregion
    }
}
