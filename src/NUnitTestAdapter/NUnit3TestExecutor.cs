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

// #define REVERSEENGINEERING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;


using NUnit.VisualStudio.TestAdapter.ExecutionProcesses;
using NUnit.VisualStudio.TestAdapter.Internal;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter;

public interface INUnit3TestExecutor
{
    void StopRun();
    IDumpXml Dump { get; }
    IAdapterSettings Settings { get; }
    IFrameworkHandle FrameworkHandle { get; }
    bool IsCancelled { get; }
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
    private readonly object _dumpLock = new object();
    private bool IsMTP { get; }
    private volatile bool _cancelled = false;

    private RunType RunType { get; set; }

    // Properties set when either of the RunTests methods is called
    public IRunContext RunContext { get; private set; }
    public IFrameworkHandle FrameworkHandle { get; private set; }

    public IVsTestFilter VsTestFilter { get; private set; }

    public ITestLogger Log => TestLog;

    public INUnitEngineAdapter EngineAdapter => NUnitEngineAdapter;

    public string TestOutputXmlFolder { get; set; } = "";

    public bool IsCancelled => _cancelled;

    // NOTE: an earlier version of this code had a FilterBuilder
    // property. This seemed to make sense, because we instantiate
    // it in two different places. However, the existence of an
    // NUnitTestFilterBuilder, containing a reference to an engine
    // service caused our second-level tests of the test executor
    // to throw an exception. So if you consider doing this, beware!

    #endregion

    public NUnit3TestExecutor()
        : this(false)
    {
    }

    internal NUnit3TestExecutor(bool isMTP)
    {
        IsMTP = isMTP;
    }

    #region ITestExecutor Implementation

    /// <summary>
    /// Called by dotnet test, and Azure Devops Pipeline Build
    /// to run either all or selected tests. In the latter case, a filter is provided
    /// as part of the run context.
    /// Also called from MTP - when run from the IDE.
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
#pragma warning disable SYSLIB0012
        var filenames = frames?.Select(x => x.GetMethod()?.DeclaringType?.Assembly.CodeBase).Distinct().ToList();
#pragma warning restore SYSLIB0012
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

        // Create early dump to track setup phase
        var firstSource = sources.FirstOrDefault();
        if (firstSource != null)
        {
            Dump = DumpXml.CreateDump(firstSource, null, Settings);
            Dump?.AddString($"<SetupPhase>Starting execution of {sources.Count()} sources</SetupPhase>\n");
            TestLog.Debug("Early dump created for setup phase tracking");
        }
        else
        {
            TestLog.Debug("No sources found - cannot create early dump");
        }

        bool shouldRunAssemblies = true;

        if (_cancelled)
        {
            TestLog.Debug("Execution cancelled before RunType determination");
            Dump?.AddString("<CancelledAt>Before RunType determination</CancelledAt>\n");
            shouldRunAssemblies = false;
        }

        if (shouldRunAssemblies)
        {
            RunType = GetRunType();
            Dump?.AddString($"<RunType>{RunType}</RunType>\n");

            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled before filter creation");
                Dump?.AddString("<CancelledAt>Before filter creation</CancelledAt>\n");
                shouldRunAssemblies = false;
            }
        }

        if (shouldRunAssemblies)
        {
            var builder = CreateTestFilterBuilder(Dump);
            TestFilter filter = null;
            if (RunType == RunType.CommandLineCurrentNUnit)
            {
                var vsTestFilter = VsTestFilterFactory.CreateVsTestFilter(Settings, runContext);
                filter = builder.ConvertVsTestFilterToNUnitFilter(vsTestFilter);
            }
            else if (RunType == RunType.Ide && IsMTP)
            {
                filter = builder.ConvertVsTestFilterToNUnitFilterForMTP(VsTestFilter);
            }

            filter ??= builder.FilterByWhere(Settings.Where);
            Dump?.AddString($"<Filter>{filter?.Text ?? "null"}</Filter>\n");

            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled before RunAssemblies");
                Dump?.AddString("<CancelledAt>Before RunAssemblies call</CancelledAt>\n");
                shouldRunAssemblies = false;
            }

            if (shouldRunAssemblies)
            {
                TestLog.Debug("About to call RunAssemblies");
                RunAssemblies(sources, filter);
                TestLog.Debug("RunAssemblies completed or stopped");
            }
        }

        // Cleanup and final reporting
        lock (_dumpLock)
        {
            if (_cancelled)
            {
                TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution cancelled");
                Dump?.AddString("<ExecutionResult>Test execution was cancelled</ExecutionResult>\n");
            }
            else
            {
                TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution complete");
                Dump?.AddString("<ExecutionResult>Test execution completed normally</ExecutionResult>\n");
            }

            // Always append to show the complete sequence
            Dump?.AddString($"<FinalCleanup>{DateTime.Now:HH:mm:ss.fff}</FinalCleanup>\n");

            // Dump active threads for debugging
            if (IsMTP)
            {
                DumpActiveThreads();
            }

            Dump?.AppendToExistingDump();
        }
        Unload();
    }

    private void RunAssemblies(IEnumerable<string> sources, TestFilter filter)
    {
        Dump?.AddString($"<RunAssembliesStart>Processing {sources.Count()} assemblies</RunAssembliesStart>\n");

        foreach (string assemblyName in sources)
        {
            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled, stopping assembly processing");
                Dump?.AddString("<AssemblyProcessing>Cancelled before processing remaining assemblies</AssemblyProcessing>\n");
                break;
            }

            try
            {
                Dump?.AddString($"<ProcessingAssembly>{assemblyName}</ProcessingAssembly>\n");
                string assemblyPath = Path.IsPathRooted(assemblyName)
                    ? assemblyName
                    : Path.Combine(Directory.GetCurrentDirectory(), assemblyName);
                RunAssembly(assemblyPath, null, filter, assemblyName);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException) { ex = ex.InnerException; }

                TestLog.Warning("Exception thrown executing tests", ex);
                Dump?.AddString($"<AssemblyException>{ex.Message}</AssemblyException>\n");
            }
        }

        Dump?.AddString("<RunAssembliesEnd>Assembly processing completed</RunAssembliesEnd>\n");
    }

    private RunType GetRunType()
    {
        var runType = !Settings.DesignMode
            ? Settings.DiscoveryMethod == DiscoveryMethod.Legacy
                ? RunType.CommandLineLegacy
                : Settings.UseNUnitFilter
                    ? RunType.CommandLineCurrentNUnit
                    : RunType.CommandLineCurrentVSTest
            : RunType.Ide;
        TestLog.Debug($"Runtype: {runType}");
        return runType;
    }

    /// <summary>
    /// Called by the VisualStudio IDE when all or selected tests are to be run. Never called from Azure Devops Pipeline Build, except (at least 2022, probably also 2019) when 'vstest.console' uses /test: then this is being used.
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
            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled, stopping assembly group processing");
                Dump?.AddString("<AssemblyGroupProcessing>Cancelled before processing remaining assembly groups</AssemblyGroupProcessing>\n");
                break;
            }

            var assemblytiming = new TimingLogger(Settings, TestLog);
            try
            {
                string assemblyName = assemblyGroup.Key;
                string assemblyPath = Path.IsPathRooted(assemblyName)
                    ? assemblyName
                    : Path.Combine(Directory.GetCurrentDirectory(), assemblyName);

                var filterBuilder = CreateTestFilterBuilder(Dump);
                var filter = filterBuilder.FilterByList(assemblyGroup);

                RunAssembly(assemblyPath, assemblyGroup, filter, assemblyName);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException) { ex = ex.InnerException; }

                TestLog.Error("Exception thrown executing tests", ex);
            }

            assemblytiming.LogTime($"Executing {assemblyGroup.Key} time ");
        }

        timing.LogTime("Total execution time");
        if (_cancelled)
        {
            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution cancelled");
            Dump?.AddString("<ExecutionResult>Test execution was cancelled</ExecutionResult>\n");
        }
        else
        {
            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test execution complete");
            Dump?.AddString("<ExecutionResult>Test execution completed normally</ExecutionResult>\n");
        }

        // Dump active threads for MTP scenarios
        if (IsMTP)
        {
            DumpActiveThreads();
        }

        Dump?.DumpForExecution();
        Unload();
    }

    private bool IsInProcDataCollectorsSpecifiedWithMultipleAssemblies(
        IEnumerable<IGrouping<string, TestCase>> assemblyGroups)
        => Settings.InProcDataCollectorsAvailable && assemblyGroups.Count() > 1;

    void ITestExecutor.Cancel()
    {
        var cancelTime = DateTime.Now.ToString("HH:mm:ss.fff");
        TestLog.Debug($"Trace: Cancel - starting cancellation process at {cancelTime}");
        _cancelled = true;

        // Thread-safe access to dump
        lock (_dumpLock)
        {
            if (Dump != null)
            {
                TestLog.Debug("Cancel - adding cancellation message to dump");
                Dump.AddString($"<CancelRequested>{cancelTime}</CancelRequested>\n");
                Dump.AddCancellationMessage();

                // IMMEDIATELY write dump to see if Cancel() is actually called
                Dump.DumpForExecution();
                TestLog.Debug("Cancel - dump written immediately");
            }
            else
            {
                TestLog.Debug("Cancel called but no active dump found");
            }
        }

        var stopTime = DateTime.Now.ToString("HH:mm:ss.fff");
        TestLog.Debug($"About to call StopRun at {stopTime}");
        StopRun();
        TestLog.Debug("StopRun completed");
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        try
        {
            TestLog.Debug($"NUnit3TestExecutor.Dispose() called - IsMTP: {IsMTP}, Cancelled: {_cancelled}");

            // Enhanced disposal for MTP scenarios
            if (IsMTP && _cancelled)
            {
                TestLog.Debug("Performing aggressive MTP cancellation cleanup");

                // Ensure engine is fully stopped and disposed
                try
                {
                    NUnitEngineAdapter?.StopRun();
                    Thread.Sleep(100); // Give it time to stop
                    NUnitEngineAdapter?.CloseRunner();
                    NUnitEngineAdapter?.Dispose();
                    TestLog.Debug("MTP cleanup - engine disposed");
                }
                catch (Exception ex)
                {
                    TestLog.Debug($"Exception during MTP engine cleanup: {ex.Message}");
                }
            }
            else
            {
                // Normal cleanup
                NUnitEngineAdapter?.CloseRunner();
                NUnitEngineAdapter?.Dispose();
            }

            // Dump active threads for debugging
            DumpActiveThreads();

            // Final dump if needed
            if (Dump != null && IsMTP)
            {
                try
                {
                    var disposeTime = DateTime.Now.ToString("HH:mm:ss.fff");
                    Dump.AddString($"<Disposed>{disposeTime} - Executor disposed - MTP: {IsMTP}, Cancelled: {_cancelled}</Disposed>\n");
                    Dump.AppendToExistingDump();
                }
                catch
                {
                    // Ignore dump errors during disposal
                }
            }

            TestLog.Debug("NUnit3TestExecutor disposal completed");
        }
        catch (Exception ex)
        {
            TestLog.Debug($"Exception during executor disposal: {ex.Message}");
        }
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "ProcessThread.StartTime is supported on Windows where this diagnostic tool is used")]
    private void DumpActiveThreads()
    {
        try
        {
            var threadDumpTime = DateTime.Now.ToString("HH:mm:ss.fff");
            var currentProcess = Process.GetCurrentProcess();
            TestLog.Debug($"=== ACTIVE THREADS DUMP at {threadDumpTime} ===");
            TestLog.Debug($"Process: {currentProcess.ProcessName} (PID: {currentProcess.Id})");

            var managedThreads = Process.GetCurrentProcess().Threads;
            TestLog.Debug($"Total OS threads: {managedThreads.Count}");

            foreach (ProcessThread thread in managedThreads)
            {
                try
                {
                    // For both .NET Framework and .NET 8.0 on Windows, StartTime should be available
                    TestLog.Debug($"Thread ID: {thread.Id}, State: {thread.ThreadState}, Start Time: {thread.StartTime}");
                }
                catch
                {
                    // Some thread info might not be accessible
                    TestLog.Debug($"Thread ID: {thread.Id} - Info not accessible");
                }
            }

            // Also dump current managed thread info
            try
            {
                TestLog.Debug($"Current managed thread: {Thread.CurrentThread.ManagedThreadId}, IsBackground: {Thread.CurrentThread.IsBackground}, IsAlive: {Thread.CurrentThread.IsAlive}");
                TestLog.Debug($"Thread state: {Thread.CurrentThread.ThreadState}");
            }
            catch (Exception ex)
            {
                TestLog.Debug($"Error getting managed thread info: {ex.Message}");
            }

            TestLog.Debug($"=== END THREADS DUMP ===");

            // Also add to XML dump if available
            if (Dump != null)
            {
                try
                {
                    Dump.AddString($"<ThreadDiagnostics>{threadDumpTime} - Process: {currentProcess.ProcessName}, OS Threads: {managedThreads.Count}, Current Thread: {Thread.CurrentThread.ManagedThreadId}</ThreadDiagnostics>\n");
                }
                catch
                {
                    // Ignore dump errors
                }
            }
        }
        catch (Exception ex)
        {
            TestLog.Debug($"Exception during thread dump: {ex.Message}");
        }
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

    private void RunAssembly(string assemblyPath, IGrouping<string, TestCase> testCases, TestFilter filter,
        string assemblyName)
    {
        if (_cancelled)
        {
            TestLog.Debug($"Execution cancelled, skipping assembly {assemblyPath}");
            Dump?.AddString($"<AssemblySkipped>Cancelled before processing {assemblyPath}</AssemblySkipped>\n");
            return;
        }

        LogActionAndSelection(assemblyPath, filter);
        RestoreRandomSeed(assemblyPath);

        // Thread-safe dump management - preserve setup dump
        lock (_dumpLock)
        {
            if (Dump == null)
            {
                Dump = DumpXml.CreateDump(assemblyPath, testCases, Settings);
            }
            else
            {
                // Add assembly info to existing dump (preserve setup logging)
                Dump?.AddString($"<AssemblyExecution>{assemblyPath}</AssemblyExecution>\n");
            }
        }

        try
        {
            TestLog.Debug("About to create test package");
            var package = CreateTestPackage(assemblyPath, testCases);

            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled after creating test package");
                Dump?.AddString("<PackageCreated>Cancelled after creating test package</PackageCreated>\n");
                return;
            }

            TestLog.Debug("About to create NUnit engine runner");
            // Pass dump to engine adapter so it can log to dump files
            NUnitEngineAdapter.SetDump(Dump);
            NUnitEngineAdapter.CreateRunner(package);

            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled after creating runner");
                Dump?.AddString("<RunnerCreated>Cancelled after creating runner</RunnerCreated>\n");
                return;
            }

            CreateTestOutputFolder();
            Dump?.StartDiscoveryInExecution(testCases, filter, package);
            TestLog.DebugRunfrom();

            TestLog.Debug("About to call NUnitEngineAdapter.Explore()");
            var discoveryResults = NUnitEngineAdapter.Explore(filter);
            TestLog.Debug("NUnitEngineAdapter.Explore() completed");
            Dump?.AddString(discoveryResults.AsString());

            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled after discovery phase");
                Dump?.AddString("<DiscoveryCompleted>Cancelled after discovery phase</DiscoveryCompleted>\n");
                return;
            }

            if (discoveryResults.IsRunnable)
            {
                var discovery = new DiscoveryConverter(TestLog, Settings);
                discovery.Convert(discoveryResults, assemblyPath);

                if (_cancelled)
                {
                    TestLog.Debug("Execution cancelled after discovery conversion");
                    Dump?.AddString("<DiscoveryConverted>Cancelled after discovery conversion</DiscoveryConverted>\n");
                    return;
                }

                if (!Settings.SkipExecutionWhenNoTests || discovery.AllTestCases.Any())
                {
                    TestLog.Debug("About to start test execution");
                    var ea = ExecutionFactory.Create(this);
                    ea.Run(filter, discovery, this);
                    TestLog.Debug("Test execution completed or stopped");
                }
                else
                {
                    TestLog.InfoNoRunnableTests(discoveryResults, assemblyPath);
                }
            }
            else
            {
                TestLog.InfoNoRunnableTests(discoveryResults, assemblyPath);
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
                ex = ex.InnerException ?? ex;
            TestLog.Warning("   Exception thrown executing tests in " + assemblyPath, ex);
            var tc = new TestCase(assemblyName, new Uri(NUnit3TestExecutor.ExecutorUri), assemblyName)
            {
                DisplayName = assemblyName,
                FullyQualifiedName = assemblyName,
                Id = Guid.NewGuid(),
                CodeFilePath = assemblyPath,
                LineNumber = 0,
            };
            FrameworkHandle.RecordResult(new TestResult(tc)
            {
                Outcome = TestOutcome.Failed,
                ErrorMessage = ex.ToString(),
                ErrorStackTrace = ex.StackTrace,
            });
        }
        finally
        {
            var finalizeTime = DateTime.Now.ToString("HH:mm:ss.fff");
            if (_cancelled)
            {
                TestLog.Debug("Assembly processing completed - execution was cancelled");
                Dump?.AddString($"<ForcedTermination>{finalizeTime}</ForcedTermination>\n");
            }
            else
            {
                TestLog.Debug("Assembly processing completed normally");
            }

            // Only dump if this is a standalone assembly run (not part of sources run)
            // For sources run, the main RunTests method handles the final dump
            if (testCases != null) // This indicates it's a TestCase run, not a Sources run
            {
                Dump?.DumpForExecution();
            }

            try
            {
                TestLog.Debug("About to close NUnit engine runner");
                NUnitEngineAdapter?.CloseRunner();
                TestLog.Debug("NUnit engine runner closed");
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


    private NUnitTestFilterBuilder CreateTestFilterBuilder(IDumpXml dump = null) => new(NUnitEngineAdapter.GetService<ITestFilterService>(), Settings, dump);

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
        var stopStartTime = DateTime.Now.ToString("HH:mm:ss.fff");
        TestLog.Debug($"StopRun called - attempting to stop engine at {stopStartTime}");

        try
        {
            // Use timeout for MTP scenarios to prevent hanging
            if (IsMTP)
            {
                var stopTask = Task.Run(() => NUnitEngineAdapter?.StopRun());
                var timeoutMs = 3000; // 3 second timeout for MTP

                if (stopTask.Wait(timeoutMs))
                {
                    TestLog.Debug("MTP engine stop completed within timeout");
                }
                else
                {
                    TestLog.Warning($"MTP engine stop TIMEOUT after {timeoutMs}ms - continuing anyway");
                }
            }
            else
            {
                // Normal non-MTP scenarios
                NUnitEngineAdapter?.StopRun();
                TestLog.Debug("Engine stop completed");
            }
        }
        catch (Exception ex)
        {
            TestLog.Warning($"Exception during StopRun: {ex.Message}");
        }
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