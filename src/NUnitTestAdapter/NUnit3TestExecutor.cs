// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

//#define LAUNCHDEBUGGER

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter
{

    [ExtensionUri(ExecutorUri)]
    public sealed class NUnit3TestExecutor : NUnitTestAdapter, ITestExecutor, IDisposable
    {
        ///<summary>
        /// The Uri used to identify the NUnitExecutor
        ///</summary>
        public const string ExecutorUri = "executor://NUnit3TestExecutor";

        // TFS Filter in effect - may be empty
        private TfsTestFilter _tfsFilter;

        // Fields related to the currently executing assembly
        private ITestRunner _testRunner;
        private TestFilter _nunitFilter = TestFilter.Empty;

        #region ITestExecutor Implementation

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
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            Initialize(frameworkHandle);

            try
            {
                _tfsFilter = new TfsTestFilter(runContext);
                TestLog.SendDebugMessage("Keepalive:" + runContext.KeepAlive);
                var enableShutdown = (UseVsKeepEngineRunning) ? !runContext.KeepAlive : true;
                if (!_tfsFilter.HasTfsFilterValue)
                {
                    if (!(enableShutdown && !runContext.KeepAlive))  // Otherwise causes exception when run as commandline, illegal to enableshutdown when Keepalive is false, might be only VS2012
                        frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
                }

                foreach (var source in sources)
                {
                    var assemblyName = source;
                    if (!Path.IsPathRooted(assemblyName))
                        assemblyName = Path.Combine(Environment.CurrentDirectory, assemblyName);

                    TestLog.SendInformationalMessage("Running all tests in " + assemblyName);

                    RunAssembly(assemblyName, frameworkHandle);
                }
            }
            catch (Exception ex)
            {
                TestLog.SendErrorMessage("Exception thrown executing tests", ex);
            }
            finally
            {
                Info("executing tests", "finished");
                Unload();
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
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            Initialize(frameworkHandle);

            var enableShutdown = (UseVsKeepEngineRunning) ? !runContext.KeepAlive : true;
            frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
            Debug("executing tests", "EnableShutdown set to " + enableShutdown);

            var assemblyGroups = tests.GroupBy(tc => tc.Source);
            foreach (var assemblyGroup in assemblyGroups)
            {
                var assemblyName = assemblyGroup.Key;
                if (Debugger.IsAttached)
                    TestLog.SendInformationalMessage("Debugging selected tests in " + assemblyName);
                else
                    TestLog.SendInformationalMessage("Running selected tests in " + assemblyName);

                _nunitFilter = MakeTestFilter(assemblyGroup);

                RunAssembly(assemblyName, frameworkHandle);
            }

            Info("executing tests", "finished");
            Unload();
        }

        void ITestExecutor.Cancel()
        {
            if (_testRunner != null)
                _testRunner.StopRun(true);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // TODO: Nothing here at the moment. Check what needs disposing, if anything. Otherwise, remove.
        }

        #endregion

        #region Helper Methods

        // The TestExecutor is constructed using the default constructor.
        // We don't have any info to initialize it until one of the
        // ITestExecutor methods is called.
        protected override void Initialize(IMessageLogger messageLogger)
        {
            base.Initialize(messageLogger);

            Info("executing tests", "started");

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();
        }

        private void RunAssembly(string assemblyName, IFrameworkHandle frameworkHandle)
        {
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            _testRunner = GetRunnerFor(assemblyName);

            try
            {
                var loadResult = _testRunner.Explore(TestFilter.Empty);

                if (loadResult.Name == "test-run")
                    loadResult = loadResult.FirstChild;

                if (loadResult.GetAttribute("runstate") == "Runnable")
                {
                    TestLog.SendInformationalMessage(string.Format("Loading tests from {0}", assemblyName));

                    var nunitTestCases = loadResult.SelectNodes("//test-case");

                    using (var testConverter = new TestConverter(TestLog, assemblyName))
                    {
                        var loadedTestCases = new List<TestCase>();

                        // As a side effect of calling TestConverter.ConvertTestCase, 
                        // the converter's cache of all test cases is populated as well. 
                        // All future calls to convert a test case may now use the cache.
                        foreach (XmlNode testNode in nunitTestCases)
                            loadedTestCases.Add(testConverter.ConvertTestCase(testNode));

                        // If we have a TFS Filter, convert it to an nunit filter
                        if (_tfsFilter != null && _tfsFilter.HasTfsFilterValue)
                        {
                            var filteredTestCases = _tfsFilter.CheckFilter(loadedTestCases);
                            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
                            TestLog.SendInformationalMessage(string.Format("TFS Filter detected: LoadedTestCases {0}, Filterered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
                            _nunitFilter = MakeTestFilter(testCases);
                        }

                        using (var listener = new NUnitEventListener(frameworkHandle, testConverter))
                        {
                            try
                            {
                                _testRunner.Run(listener, _nunitFilter);
                            }
                            catch (NullReferenceException)
                            {
                                // this happens during the run when CancelRun is called.
                                TestLog.SendDebugMessage("Nullref caught");
                            }
                        }
                    }
                }
                else
                    TestLog.NUnitLoadError(assemblyName);
            }
            catch (BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                TestLog.AssemblyNotSupportedWarning(assemblyName);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                TestLog.DependentAssemblyNotFoundWarning(ex.FileName, assemblyName);
            }
            catch (Exception ex)
            {
                TestLog.SendErrorMessage("Exception thrown executing tests in " + assemblyName, ex);
            }
            _testRunner.Dispose();
        }

        private static TestFilter MakeTestFilter(IEnumerable<TestCase> testCases)
        {
            var testFilter = new StringBuilder("<filter><tests>");

            foreach (TestCase testCase in testCases)
                testFilter.AppendFormat("<test>{0}</test>", testCase.FullyQualifiedName.Replace("<", "&lt;").Replace(">", "&gt;"));

            testFilter.Append("</tests></filter>");

            return new TestFilter(testFilter.ToString());
        }

        #endregion
    }
}
