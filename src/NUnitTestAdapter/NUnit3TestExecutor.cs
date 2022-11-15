// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, Terje Sandstrom
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
#define REVERSEENGINEERING

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
using NUnit.VisualStudio.TestAdapter.Internal;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter
{
    public interface INUnit3TestExecutor
    {
        void StopRun();
        IDumpXml Dump { get; }
        IAdapterSettings Settings { get; }
        IFrameworkHandle FrameworkHandle { get; }
    }

    public enum RunType
    {
        Unknown,
        CommandLineLegacy,
        CommandLineCurrentVSTest,
        CommandLineCurrentNUnit,
        Ide
    }


    [ExtensionUri(ExecutorUri)]
    public sealed class NUnit3TestExecutor : NUnitTestAdapter, ITestExecutor, IDisposable, INUnit3TestExecutor,
        IExecutionContext
    {
        #region Properties

        private RunType RunType { get; set; }

        // Properties set when either of the RunTests methods is called
        public IRunContext RunContext { get; private set; }
        public IFrameworkHandle FrameworkHandle { get; private set; }

        public IVsTestFilter VsTestFilter { get; private set; }

        public ITestLogger Log => TestLog;

        public INUnitEngineAdapter EngineAdapter => NUnitEngineAdapter;

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
        /// Called by dotnet test, and TFS Build
        /// to run either all or selected tests. In the latter case, a filter is provided
        /// as part of the run context.
        /// </summary>
        /// <param name="sources">Sources to be run.</param>
        /// <param name="runContext">Context to use when executing the tests.</param>
        /// <param name="frameworkHandle">Test log to send results and messages through.</param>
        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Initialize(runContext, frameworkHandle);
            CheckIfDebug();
#if REVERSEENGINEERING
            var st = new StackTrace();
            var frames = st.GetFrames();
            var filenames = frames?.Select(x => x.GetMethod()?.DeclaringType?.Assembly.CodeBase).Distinct().ToList();
#endif
            InitializeForExecution(runContext, frameworkHandle);
            TestLog.Debug($"RunTests by IEnumerable<string>,({sources.Count()} entries), called from {WhoIsCallingUsEntry}");

            if (Settings.InProcDataCollectorsAvailable && sources.Count() > 1)
            {
                TestLog.Error(
                    "Failed to run tests for multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }

            SetRunTypeByStrings();
            var builder = CreateTestFilterBuilder();
            TestFilter filter = null;
            if (RunType == RunType.CommandLineCurrentNUnit)
            {
                var vsTestFilter = VsTestFilterFactory.CreateVsTestFilter(Settings, runContext);
                filter = builder.ConvertVsTestFilterToNUnitFilter(vsTestFilter);
            }

            filter ??= builder.FilterByWhere(Settings.Where);

            foreach (string assemblyName in sources)
            {
                try
                {
                    string assemblyPath = Path.IsPathRooted(assemblyName)
                        ? assemblyName
                        : Path.Combine(Directory.GetCurrentDirectory(), assemblyName);
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

        private void SetRunTypeByStrings()
        {
            RunType = !Settings.DesignMode
                ? Settings.DiscoveryMethod == DiscoveryMethod.Legacy
                    ? RunType.CommandLineLegacy
                    : Settings.UseNUnitFilter
                        ? RunType.CommandLineCurrentNUnit
                        : RunType.CommandLineCurrentVSTest
                : RunType.Ide;
            TestLog.Debug($"Runtype: {RunType}");
        }

        /// <summary>
        /// Called by the VisualStudio IDE when all or selected tests are to be run. Never called from TFS Build, except (at least 2022, probably also 2019) when vstest.console uses /test: then this is being used.
        /// </summary>
        /// <param name="tests">The tests to be run.</param>
        /// <param name="runContext">The RunContext.</param>
        /// <param name="frameworkHandle">The FrameworkHandle.</param>
        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            Initialize(runContext, frameworkHandle);
            CheckIfDebug();
            InitializeForExecution(runContext, frameworkHandle);
            RunType = RunType.Ide;
            TestLog.Debug($"RunTests by IEnumerable<TestCase>. RunType = Ide, called from {WhoIsCallingUsEntry}");
            var timing = new TimingLogger(Settings, TestLog);
            Debug.Assert(NUnitEngineAdapter != null, "NUnitEngineAdapter is null");
            Debug.Assert(NUnitEngineAdapter.EngineEnabled, "NUnitEngineAdapter TestEngine is null");
            var assemblyGroups = tests.GroupBy(tc => tc.Source).ToList();
            if (assemblyGroups.Count > 1)
                TestLog.Debug($"Multiple ({assemblyGroups.Count}) assemblies in one test");
            if (IsInProcDataCollectorsSpecifiedWithMultipleAssemblies(assemblyGroups))
            {
                TestLog.Error(
                    "Failed to run tests for multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }

            foreach (var assemblyGroup in assemblyGroups)
            {
                var assemblytiming = new TimingLogger(Settings, TestLog);
                try
                {
                    string assemblyName = assemblyGroup.Key;
                    string assemblyPath = Path.IsPathRooted(assemblyName)
                        ? assemblyName
                        : Path.Combine(Directory.GetCurrentDirectory(), assemblyName);

                    var filterBuilder = CreateTestFilterBuilder();
                    var filter = filterBuilder.FilterByList(assemblyGroup);

                    RunAssembly(assemblyPath, assemblyGroup, filter);
                }
                catch (Exception ex)
                {
                    if (ex is TargetInvocationException) { ex = ex.InnerException; }

                    TestLog.Warning("Exception thrown executing tests", ex);
                }

                assemblytiming.LogTime($"Executing {assemblyGroup.Key} time ");
            }

            timing.LogTime("Total execution time");
            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution complete");
            Unload();
        }

        private bool IsInProcDataCollectorsSpecifiedWithMultipleAssemblies(
            IEnumerable<IGrouping<string, TestCase>> assemblyGroups)
            => Settings.InProcDataCollectorsAvailable && assemblyGroups.Count() > 1;

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
            VsTestFilter = VsTestFilterFactory.CreateVsTestFilter(Settings, runContext);

            CleanUpRegisteredChannels();

            TestLog.Debug("KeepAlive: " + runContext.KeepAlive);
            TestLog.Debug("UseVsKeepEngineRunning: " + Settings.UseVsKeepEngineRunning);

            bool enableShutdown = true;
            if (Settings.UseVsKeepEngineRunning)
            {
                enableShutdown = !runContext.KeepAlive;
            }

            if (VsTestFilter.IsEmpty)
            {
                if (!(enableShutdown &&
                      !runContext
                          .KeepAlive)) // Otherwise causes exception when run as commandline, illegal to enableshutdown when Keepalive is false, might be only VS2012
                    frameworkHandle.EnableShutdownAfterTestRun = enableShutdown;
            }

            TestLog.Debug("EnableShutdown: " + enableShutdown);
        }

        private void RunAssembly(string assemblyPath, IGrouping<string, TestCase> testCases, TestFilter filter)
        {
            LogActionAndSelection(assemblyPath, filter);
            RestoreRandomSeed(assemblyPath);
            Dump = DumpXml.CreateDump(assemblyPath, testCases, Settings);

            try
            {
                var package = CreateTestPackage(assemblyPath, testCases);
                NUnitEngineAdapter.CreateRunner(package);
                CreateTestOutputFolder();
                Dump?.StartDiscoveryInExecution(testCases, filter, package);
                TestLog.DebugRunfrom();
                // var discoveryResults = RunType == RunType.CommandLineCurrentNUnit ? null : NUnitEngineAdapter.Explore(filter);
                var discoveryResults = NUnitEngineAdapter.Explore(filter);
                Dump?.AddString(discoveryResults?.AsString() ?? " No discovery");

                if (discoveryResults?.IsRunnable ?? true)
                {
                    var discovery = new DiscoveryConverter(TestLog, Settings);
                    discovery.Convert(discoveryResults, assemblyPath);
                    if (!Settings.SkipExecutionWhenNoTests || discovery.AllTestCases.Any())
                    {
                        var ea = ExecutionFactory.Create(this);
                        ea.Run(filter, discovery, this);
                    }
                    else
                    {
                        TestLog.InfoNoTests(assemblyPath);
                    }
                }
                else
                {
                    TestLog.InfoNoTests(discoveryResults.HasNoNUnitTests, assemblyPath);
                }
            }
            catch (Exception ex) when (ex is BadImageFormatException || ex.InnerException is BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                TestLog.Warning("   Assembly not supported: " + assemblyPath);
            }
            catch (FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                TestLog.Warning("   Dependent Assembly " + ex.FileName + " of " + assemblyPath +
                                " not found. Can be ignored if not an NUnit project.");
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException)
                    ex = ex.InnerException;
                TestLog.Warning("   Exception thrown executing tests in " + assemblyPath, ex);
            }
            finally
            {
                Dump?.DumpForExecution();
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

        private void LogActionAndSelection(string assemblyPath, TestFilter filter)
        {
            string actionText = Debugger.IsAttached ? "Debugging " : "Running ";
            string selectionText = filter == null || filter == TestFilter.Empty ? "all" : "selected";
            TestLog.Info(actionText + selectionText + " tests in " + assemblyPath);
        }


        private void RestoreRandomSeed(string assemblyPath)
        {
            // No need to restore if the seed was in runsettings file
            if (!Settings.RandomSeedSpecified)
                Settings.RestoreRandomSeed(Path.GetDirectoryName(assemblyPath));
        }


        private NUnitTestFilterBuilder CreateTestFilterBuilder() => new(NUnitEngineAdapter.GetService<ITestFilterService>(), Settings);

        /// <summary>
        /// Must be called after WorkDir have been set.
        /// </summary>
        private void CreateTestOutputFolder()
        {
            if (!Settings.UseTestOutputXml)
            {
                return;
            }
            string path = Settings.SetTestOutputFolder(WorkDir);
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

        public IDumpXml Dump { get; private set; }

        private void CheckIfDebug()
        {
            if (!Settings.DebugExecution)
                return;
            if (!Debugger.IsAttached)
                Debugger.Launch();
        }
    }
}