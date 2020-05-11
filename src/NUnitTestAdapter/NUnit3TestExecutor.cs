// ***********************************************************************
// Copyright (c) 2011-2020 Charlie Poole, Terje Sandstrom
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

// #define LAUNCHDEBUGGER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter
{
    public interface INUnit3TestExecutor
    {
        void StopRun();
        IDumpXml Dump { get; }
        IAdapterSettings Settings { get; }
    }


    [ExtensionUri(ExecutorUri)]
    public sealed class NUnit3TestExecutor : NUnitTestAdapter, ITestExecutor, IDisposable, INUnit3TestExecutor
    {
        private DumpXml executionDumpXml;

        public NUnit3TestExecutor()
        {
            EmbeddedAssemblyResolution.EnsureInitialized();
        }

        #region Properties

        // Properties set when either of the RunTests methods is called
        public IRunContext RunContext { get; private set; }
        public IFrameworkHandle FrameworkHandle { get; private set; }
        private TfsTestFilter TfsFilter { get; set; }

        public string TestOutputXmlFolder { get; set; } = "";

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
        /// <param name="frameworkHandle">Test log to send results and messages through.</param>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            Initialize(runContext, frameworkHandle);
            TestLog.Debug("RunTests by IEnumerable<string>");
            InitializeForExecution(runContext, frameworkHandle);

            if (Settings.InProcDataCollectorsAvailable && sources.Count() > 1)
            {
                TestLog.Error("Failed to run tests for multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }

            foreach (string assemblyName in sources)
            {
                try
                {
                    string assemblyPath = Path.IsPathRooted(assemblyName) ? assemblyName : Path.Combine(Directory.GetCurrentDirectory(), assemblyName);
                    var filter = CreateTestFilterBuilder().FilterByWhere(Settings.Where);

                    RunAssembly(assemblyPath, null, filter);
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException) { ex = ex.InnerException; }
                    TestLog.Warning("Exception thrown executing tests", ex);
                }
            }

            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution complete");
            Unload();
        }

        /// <summary>
        /// Called by the VisualStudio IDE when selected tests are to be run. Never called from TFS Build.
        /// </summary>
        /// <param name="tests">The tests to be run.</param>
        /// <param name="runContext">The RunContext.</param>
        /// <param name="frameworkHandle">The FrameworkHandle.</param>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            Initialize(runContext, frameworkHandle);
            TestLog.Debug("RunTests by IEnumerable<TestCase>");
            InitializeForExecution(runContext, frameworkHandle);
            Debug.Assert(NUnitEngineAdapter != null, "NUnitEngineAdapter is null");
            Debug.Assert(NUnitEngineAdapter.EngineEnabled, "NUnitEngineAdapter TestEngine is null");
            var assemblyGroups = tests.GroupBy(tc => tc.Source);
            if (Settings.InProcDataCollectorsAvailable && assemblyGroups.Count() > 1)
            {
                TestLog.Error("Failed to run tests for multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }

            foreach (var assemblyGroup in assemblyGroups)
            {
                try
                {
                    string assemblyName = assemblyGroup.Key;
                    string assemblyPath = Path.IsPathRooted(assemblyName) ? assemblyName : Path.Combine(Directory.GetCurrentDirectory(), assemblyName);

                    var filterBuilder = CreateTestFilterBuilder();
                    var filter = filterBuilder.FilterByList(assemblyGroup);

                    RunAssembly(assemblyPath, assemblyGroup, filter);
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException) { ex = ex.InnerException; }
                    TestLog.Warning("Exception thrown executing tests", ex);
                }
            }

            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution complete");
            Unload();
        }

        void ITestExecutor.Cancel()
        {
            StopRun();
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // TODO: Nothing here at the moment. Check what needs disposing, if anything. Otherwise, remove.
        }

        #endregion

        #region Helper Methods

        public void InitializeForExecution(IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution started");

            RunContext = runContext;
            FrameworkHandle = frameworkHandle;
            TfsFilter = new TfsTestFilter(runContext);

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            TestLog.Debug("KeepAlive: " + runContext.KeepAlive);
            TestLog.Debug("UseVsKeepEngineRunning: " + Settings.UseVsKeepEngineRunning);

            bool enableShutdown = true;
            if (Settings.UseVsKeepEngineRunning)
            {
                enableShutdown = !runContext.KeepAlive;
            }

            if (TfsFilter.IsEmpty)
            {
                if (!(enableShutdown && !runContext.KeepAlive))  // Otherwise causes exception when run as commandline, illegal to enableshutdown when Keepalive is false, might be only VS2012
                    frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
            }

            TestLog.Debug("EnableShutdown: " + enableShutdown);
        }

        private void RunAssembly(string assemblyPath, IGrouping<string, TestCase> testCases, TestFilter filter)
        {
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif

            string actionText = Debugger.IsAttached ? "Debugging " : "Running ";
            string selectionText = filter == null || filter == TestFilter.Empty ? "all" : "selected";
            TestLog.Info(actionText + selectionText + " tests in " + assemblyPath);
            // No need to restore if the seed was in runsettings file
            if (!Settings.RandomSeedSpecified)
                Settings.RestoreRandomSeed(Path.GetDirectoryName(assemblyPath));
            executionDumpXml = null;
            if (Settings.DumpXmlTestResults)
            {
                executionDumpXml = new DumpXml(assemblyPath);
                string runningBy = testCases == null
                    ? "<RunningBy>Sources</RunningBy>"
                    : "<RunningBy>TestCases</RunningBy>";
                executionDumpXml?.AddString($"\n{runningBy}\n");
            }

            try
            {
                var package = CreateTestPackage(assemblyPath, testCases);
                NUnitEngineAdapter.CreateRunner(package);
                CreateTestOutputFolder();
                executionDumpXml?.AddString($"<NUnitDiscoveryInExecution>{assemblyPath}</NUnitExecution>\n\n");
                var discoveryResults = NUnitEngineAdapter.Explore(filter); // _activeRunner.Explore(filter);
                executionDumpXml?.AddString(discoveryResults.AsString());

                if (discoveryResults.IsRunnable)
                {
                    var discovery = new DiscoveryExtensions();
                    var loadedTestCases = discovery.Load(discoveryResults,TestLog, assemblyPath, Settings);

                    // If we have a TFS Filter, convert it to an nunit filter
                    if (TfsFilter != null && !TfsFilter.IsEmpty)
                    {
                        // NOTE This overwrites filter used in call
                        var filterBuilder = CreateTestFilterBuilder();
                        filter = filterBuilder.ConvertTfsFilterToNUnitFilter(TfsFilter, loadedTestCases);
                    }

                    if (filter == NUnitTestFilterBuilder.NoTestsFound)
                    {
                        TestLog.Info("   Skipping assembly - no matching test cases found");
                        return;
                    }
                    executionDumpXml?.AddString($"\n\n<NUnitExecution>{assemblyPath}</NUnitExecution>\n\n");
                    using (var listener = new NUnitEventListener(FrameworkHandle, discovery.TestConverter, this))
                    {
                        try
                        {
                            var results = NUnitEngineAdapter.Run(listener, filter);
                            NUnitEngineAdapter.GenerateTestOutput(results, assemblyPath, this);
                        }
                        catch (NullReferenceException)
                        {
                            // this happens during the run when CancelRun is called.
                            TestLog.Debug("   Null ref caught");
                        }
                    }
                }
                else
                {
                    TestLog.Info(discoveryResults.HasNoNUnitTests
                            ? "   NUnit couldn't find any tests in " + assemblyPath
                            : "   NUnit failed to load " + assemblyPath);
                }
            }
            catch (BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                TestLog.Warning("   Assembly not supported: " + assemblyPath);
            }
            catch (NUnitEngineException e)
            {
                if (e.InnerException is BadImageFormatException)
                {
                    // we skip the native c++ binaries that we don't support.
                    TestLog.Warning("   Assembly not supported: " + assemblyPath);
                }
                throw;
            }
            catch (FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                TestLog.Warning("   Dependent Assembly " + ex.FileName + " of " + assemblyPath + " not found. Can be ignored if not an NUnit project.");
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                    ex = ex.InnerException;
                TestLog.Warning("   Exception thrown executing tests in " + assemblyPath, ex);
            }
            finally
            {
                executionDumpXml?.Dump4Execution();
                try
                {
                    NUnitEngineAdapter?.CloseRunner();
                }
                catch (Exception ex)
                {
                    // can happen if CLR throws CannotUnloadAppDomainException, for example
                    // due to a long-lasting operation in a protected region (catch/finally clause).
                    if (ex is TargetInvocationException) { ex = ex.InnerException; }
                    TestLog.Warning($"   Exception thrown unloading tests from {assemblyPath}", ex);
                }
            }
        }


        private NUnitTestFilterBuilder CreateTestFilterBuilder()
        {
            return new NUnitTestFilterBuilder(NUnitEngineAdapter.GetService<ITestFilterService>());
        }


        private void CreateTestOutputFolder()
        {
            if (!Settings.UseTestOutputXml)
            {
                return;
            }

            string path = Path.IsPathRooted(Settings.TestOutputXml)
                ? Settings.TestOutputXml
                : Path.Combine(WorkDir, Settings.TestOutputXml);
            try
            {
                Directory.CreateDirectory(path);
                TestOutputXmlFolder = path;
                TestLog.Info($"  Test Output folder checked/created : {path} ");
            }
            catch (UnauthorizedAccessException)
            {
                TestLog.Error($"   Failed creating test output folder at {path}");
                throw;
            }
        }

        #endregion

        public void StopRun()
        {
            NUnitEngineAdapter?.StopRun();
        }

        public IDumpXml Dump => executionDumpXml;
    }
}
