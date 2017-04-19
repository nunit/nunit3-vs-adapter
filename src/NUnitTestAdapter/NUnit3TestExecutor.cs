// ***********************************************************************
// Copyright (c) 2011-2015 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

//#define LAUNCHDEBUGGER

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        // Fields related to the currently executing assembly
        private ITestRunner _activeRunner;

        #region Properties

        // Properties set when either of the RunTests methods is called
        public IRunContext RunContext { get; private set; }
        public IFrameworkHandle FrameworkHandle { get; private set; }
        private TfsTestFilter TfsFilter { get; set; }

        // NOTE: an earlier version of this code had a FilterBuilder
        // property. This seemed to make sense, because we instantiate
        // it in two different places. However, the existence of an
        // NUnitTestFilterBuilder, containing a reference to an engine 
        // service caused our second-level tests of the test executor
        // to throw an exception. So if you consider doing this, beware!

        #endregion

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
            Initialize(runContext, frameworkHandle);

            if (Settings.InProcDataCollectorsAvailable && sources.Count() > 1)
            {
                TestLog.Error("Failed to run tests for multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }

            foreach (var source in sources)
            {
                try
                {
                    var assemblyName = source;
                    if (!Path.IsPathRooted(assemblyName))
                        assemblyName = Path.Combine(Environment.CurrentDirectory, assemblyName);

                    TestLog.Info("Running all tests in " + assemblyName);

                    RunAssembly(assemblyName, TestFilter.Empty);
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException)
                        ex = ex.InnerException;
                    TestLog.Warning("Exception thrown executing tests", ex);
                }
            }

            TestLog.Info(string.Format("NUnit Adapter {0}: Test execution complete", AdapterVersion));
            Unload();

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
            Initialize(runContext, frameworkHandle);

            var assemblyGroups = tests.GroupBy(tc => tc.Source);
            if (Settings.InProcDataCollectorsAvailable && assemblyGroups.Count() > 1)
            {
                TestLog.Error("Failed to run tests for multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }

            foreach (var assemblyGroup in assemblyGroups)
            {
                var assemblyName = assemblyGroup.Key;
                if (Debugger.IsAttached)
                    TestLog.Info("Debugging selected tests in " + assemblyName);
                else
                    TestLog.Info("Running selected tests in " + assemblyName);

                var filterBuilder = new NUnitTestFilterBuilder(TestEngine.Services.GetService<ITestFilterService>());
                var filter = filterBuilder.MakeTestFilter(assemblyGroup);

                RunAssembly(assemblyName, filter);
            }

            TestLog.Info(string.Format("NUnit Adapter {0}: Test execution complete", AdapterVersion));
            Unload();
        }

        void ITestExecutor.Cancel()
        {
            if (_activeRunner != null)
                _activeRunner.StopRun(true);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // TODO: Nothing here at the moment. Check what needs disposing, if anything. Otherwise, remove.
        }

        #endregion

        #region Helper Methods

        public void Initialize(IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            base.Initialize(runContext, frameworkHandle);

            TestLog.Info(string.Format("NUnit Adapter {0}: Test execution started", AdapterVersion));

            RunContext = runContext;
            FrameworkHandle = frameworkHandle;
            TfsFilter = new TfsTestFilter(runContext);

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            TestLog.Debug("Keepalive: " + runContext.KeepAlive);
            TestLog.Debug("UseVsKeepEngineRunning: " + Settings.UseVsKeepEngineRunning);

            bool enableShutdown = true;
            if (Settings.UseVsKeepEngineRunning )
            {
                enableShutdown = !runContext.KeepAlive;
            }

            if (TfsFilter.IsEmpty)
            {
                if (!(enableShutdown && !runContext.KeepAlive))  // Otherwise causes exception when run as commandline, illegal to enableshutdown when Keepalive is false, might be only VS2012
                    frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
            }

            TestLog.Debug("EnableShutdown: " + enableShutdown.ToString());
        }

        private void RunAssembly(string assemblyName, TestFilter filter)
        {
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif

            // No need to restore if the seed was in runsettings file
            if (!Settings.RandomSeedSpecified)
                Settings.RestoreRandomSeed(Path.GetDirectoryName(assemblyName));

            _activeRunner = GetRunnerFor(assemblyName);

            try
            {
                var loadResult = _activeRunner.Explore(TestFilter.Empty);

                if (loadResult.Name == "test-run")
                    loadResult = loadResult.FirstChild;

                if (loadResult.GetAttribute("runstate") == "Runnable")
                {
                    var nunitTestCases = loadResult.SelectNodes("//test-case");

                    var testConverter = new TestConverter(TestLog, assemblyName);

                    var loadedTestCases = new List<TestCase>();

                    // As a side effect of calling TestConverter.ConvertTestCase, 
                    // the converter's cache of all test cases is populated as well. 
                    // All future calls to convert a test case may now use the cache.
                    foreach (XmlNode testNode in nunitTestCases)
                        loadedTestCases.Add(testConverter.ConvertTestCase(testNode));

                    TestLog.Info(string.Format("NUnit3TestExecutor converted {0} of {1} NUnit test cases", loadedTestCases.Count, nunitTestCases.Count));

                    // If we have a TFS Filter, convert it to an nunit filter
                    if (TfsFilter != null && !TfsFilter.IsEmpty)
                    {
                        // NOTE This overwrites filter used in call
                        var filterBuilder = new NUnitTestFilterBuilder(TestEngine.Services.GetService<ITestFilterService>());
                        filter = filterBuilder.ConvertTfsFilterToNUnitFilter(TfsFilter, loadedTestCases);
                    }

                    if (filter == NUnitTestFilterBuilder.NoTestsFound)
                    {
                        TestLog.Info("Skipping assembly - no matching test cases found");
                        return;
                    }

                    using (var listener = new NUnitEventListener(FrameworkHandle, testConverter))
                    {
                        try
                        {
                            _activeRunner.Run(listener, filter);
                        }
                        catch (NullReferenceException)
                        {
                            // this happens during the run when CancelRun is called.
                            TestLog.Debug("Nullref caught");
                        }
                    }
                }
                else
                {
                    var msgNode = loadResult.SelectSingleNode("properties/property[@name='_SKIPREASON']");
                    if (msgNode != null && (new[] { "contains no tests", "Has no TestFixtures" }).Any(msgNode.GetAttribute("value").Contains))
                        TestLog.Info("NUnit couldn't find any tests in " + assemblyName);
                    else
                        TestLog.Info("NUnit failed to load " + assemblyName);
                }
            }
            catch (BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                TestLog.Warning("Assembly not supported: " + assemblyName);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                TestLog.Warning("Dependent Assembly " + ex.FileName + " of " + assemblyName + " not found. Can be ignored if not a NUnit project.");
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                    ex = ex.InnerException;
                TestLog.Warning("Exception thrown executing tests in " + assemblyName, ex);
            }

            _activeRunner.Dispose();
            _activeRunner = null;
        }

        #endregion
    }
}
