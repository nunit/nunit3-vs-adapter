// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, 2014-2026 Terje Sandstrom
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

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.TestHost;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.ExecutionProcesses;
using NUnit.VisualStudio.TestAdapter.Internal;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public interface INUnit3TestExecutor
{
    void StopRun();
    IDumpXml Dump { get; }
    IAdapterSettings Settings { get; }
    IFrameworkHandle FrameworkHandle { get; }
    bool IsCancelled { get; }

    // MTP session management methods
    void TrackRunningTest(TestCase testCase);
    void UntrackRunningTest(TestCase testCase);
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
    private readonly HashSet<TestCase> _runningTests = new HashSet<TestCase>();
    private readonly object _runningTestsLock = new object();
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

        // For MTP scenarios, register process exit handlers to ensure proper cleanup
        if (isMTP)
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Console.CancelKeyPress += OnCancelKeyPress;
        }
    }

    private void OnProcessExit(object sender, EventArgs e)
    {
        try
        {
            if (IsMTP && !_cancelled)
            {
                // Attempt final cleanup during process exit
                _cancelled = true;

                // Force MTP session cleanup immediately
                LogToDump("ProcessExitMTP", "Process exit - forcing MTP session cleanup");
                ForceMTPSessionEnd();

                // Brief delay for session cleanup messages
                Thread.Sleep(200);

                StopRun();

                // Brief cleanup attempt
                Thread.Sleep(300);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw during process exit
            TestLog.Debug($"Error during process exit cleanup: {ex.Message}");
        }
    }

    private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        if (IsMTP && !_cancelled)
        {
            e.Cancel = true; // Prevent immediate termination for graceful shutdown
            _cancelled = true;

            // Force MTP session cleanup first
            LogToDump("CancelKeyPressMTP", "Cancel key pressed - forcing MTP session cleanup");
            ForceMTPSessionEnd();

            StopRun();
        }
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
            LogToDump("SetupPhase", $"Starting execution of {sources.Count()} sources", appendToDump: false);
        }
        else
        {
            TestLog.Debug("No sources found - cannot create early dump");
        }

        bool shouldRunAssemblies = true;

        if (_cancelled)
        {
            LogToDump("CancelledAt", "Before RunType determination", appendToDump: false);
            shouldRunAssemblies = false;
        }

        if (shouldRunAssemblies)
        {
            RunType = GetRunType();
            LogToDump("RunType", RunType.ToString(), appendToDump: true);

            if (_cancelled)
            {
                LogToDump("CancelledAt", "Before filter creation", appendToDump: false);
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
            LogToDump("Filter", filter?.Text ?? "null", appendToDump: true);

            if (_cancelled)
            {
                LogToDump("CancelledAt", "Before RunAssemblies call", appendToDump: false);
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
                LogToDump("ExecutionResult", "Test execution was cancelled", appendToDump: false);
            }
            else
            {
                LogToDump("ExecutionResult", "Test execution completed normally", appendToDump: false);
            }

            // Always append to show the complete sequence
            LogToDump("FinalCleanup", "Final cleanup initiated");

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
        LogToDump("RunAssembliesStart", $"Processing {sources.Count()} assemblies", appendToDump: true);

        foreach (string assemblyName in sources)
        {
            if (_cancelled)
            {
                LogToDump("AssemblyProcessing", "Cancelled before processing remaining assemblies", appendToDump: false);
                break;
            }

            try
            {
                LogToDump("ProcessingAssembly", assemblyName, appendToDump: true);
                string assemblyPath = Path.IsPathRooted(assemblyName)
                    ? assemblyName
                    : Path.Combine(Directory.GetCurrentDirectory(), assemblyName);
                RunAssembly(assemblyPath, null, filter, assemblyName);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException) { ex = ex.InnerException; }

                TestLog.Warning("Exception thrown executing tests", ex);
                LogToDump("AssemblyException", ex.Message, appendToDump: false);
            }
        }

        LogToDump("RunAssembliesEnd", "Assembly processing completed");
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
                LogToDump("AssemblyGroupProcessing", "Cancelled before processing remaining assembly groups");
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
            LogToDump("ExecutionResult", "Test execution was cancelled");
        }
        else
        {
            LogToDump("ExecutionResult", "Test execution completed normally");
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
                LogToDump("CancelRequested", "Cancellation requested");
                Dump.AddCancellationMessage();

                // CRITICAL: Complete all running tests IMMEDIATELY and SYNCHRONOUSLY
                // to beat the cancellation race condition (before FrameworkHandle is cancelled)
                if (IsMTP)
                {
                    LogToDump("MTPCancellation", "Cancellation detected - completing tests BEFORE handle cancellation");
                    CompleteAllRunningTestsSynchronously();
                }

                // IMMEDIATELY append to existing dump to preserve setup information
                Dump.AppendToExistingDump();
                TestLog.Debug("Cancel - dump appended immediately");
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
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            LogToDump("ExecutorDispose", $"Dispose called - IsMTP: {IsMTP}, Cancelled: {_cancelled}");

            // Force MTP session cleanup if we're in MTP mode and cancelled
            if (IsMTP && _cancelled)
            {
                LogToDump("CleanupPhase", "Attempting fire-and-forget cleanup");
                ForceMTPSessionEnd();

                // Brief delay to let session cleanup fire
                Task.Delay(100).Wait();
            }

            // Unregister process exit handlers for MTP scenarios
            if (IsMTP)
            {
                try
                {
                    AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                    Console.CancelKeyPress -= OnCancelKeyPress;
                }
                catch (Exception ex)
                {
                    TestLog.Debug($"Error unregistering process handlers: {ex.Message}");
                }
            }

            // For MTP scenarios, use non-blocking disposal pattern
            if (IsMTP && _cancelled)
            {
                LogToDump("NonBlockingDisposal", "Non-blocking MTP disposal");

                // Fire-and-forget cleanup - don't wait for anything
                _ = Task.Run(() =>
                {
                    try
                    {
                        // Very brief cleanup attempt
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

                        var stopTask = Task.Run(() => NUnitEngineAdapter?.StopRun());
                        TestLog.Debug(stopTask.Wait(500) ? "Quick stop completed" : "Quick stop timed out"); // 500ms max

                        // Try quick close
                        var closeTask = Task.Run(() => NUnitEngineAdapter?.CloseRunner());
                        TestLog.Debug(closeTask.Wait(500) ? "Quick close completed" : "Quick close timed out"); // 500ms max

                        // Final dispose
                        NUnitEngineAdapter?.Dispose();
                        TestLog.Debug("Fire-and-forget cleanup completed");
                    }
                    catch (Exception ex)
                    {
                        TestLog.Debug($"Fire-and-forget cleanup exception: {ex.Message}");
                    }
                });

                // Don't wait for fire-and-forget task - return immediately
                TestLog.Debug("Non-blocking MTP disposal initiated");
            }
            else
            {
                // Normal cleanup for non-MTP or non-cancelled scenarios
                try
                {
                    NUnitEngineAdapter?.CloseRunner();
                    NUnitEngineAdapter?.Dispose();
                }
                catch (Exception ex)
                {
                    TestLog.Debug($"Normal cleanup exception: {ex.Message}");
                }
            }

            // Quick thread dump for MTP scenarios
            if (IsMTP)
            {
                try
                {
                    var currentProcess = Process.GetCurrentProcess();
                    var threadCount = currentProcess.Threads.Count;
                    LogToDump("FinalThreadCount", $"Final thread count: {threadCount}");
                }
                catch
                {
                    // Ignore thread dump errors
                }
            }

            // Quick dump if available
            if (Dump != null && IsMTP)
            {
                try
                {
                    LogToDump("Disposed", $"Executor disposed - MTP: {IsMTP}, Cancelled: {_cancelled}");
                }
                catch
                {
                    // Ignore dump errors during disposal
                }
            }


            LogToDump("ExecutorDispose", "Disposal completed", appendToDump: false);
        }
        catch (Exception ex)
        {
            LogToDump("ExecutorDispose", $"Exception during disposal: {ex.Message}", logLevel: LogLevel.Warning);
        }
    }

    private volatile bool _disposed = false;

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
                    LogToDump("ThreadDiagnostics", $"Process: {currentProcess.ProcessName}, OS Threads: {managedThreads.Count}, Current Thread: {Thread.CurrentThread.ManagedThreadId}", appendToDump: false);
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
                LogToDump("AssemblyExecution", assemblyPath, appendToDump: true);
            }
        }

        if (_cancelled)
        {
            TestLog.Debug($"Execution cancelled, skipping assembly {assemblyPath}");
            LogToDump("AssemblySkipped", $"Cancelled before processing {assemblyPath}");
            return;
        }

        LogActionAndSelection(assemblyPath, filter);
        RestoreRandomSeed(assemblyPath);

        try
        {
            TestLog.Debug("About to create test package");
            var package = CreateTestPackage(assemblyPath, testCases);

            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled after creating test package");
                LogToDump("PackageCreated", "Cancelled after creating test package");
                return;
            }

            TestLog.Debug("About to create NUnit engine runner");
            // Pass dump to engine adapter so it can log to dump files
            NUnitEngineAdapter.SetDump(Dump);
            NUnitEngineAdapter.CreateRunner(package);

            if (_cancelled)
            {
                TestLog.Debug("Execution cancelled after creating runner");
                LogToDump("RunnerCreated", "Cancelled after creating runner");
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
                LogToDump("DiscoveryCompleted", "Cancelled after discovery phase");
                return;
            }

            if (discoveryResults.IsRunnable)
            {
                var discovery = new DiscoveryConverter(TestLog, Settings);
                discovery.Convert(discoveryResults, assemblyPath);

                if (_cancelled)
                {
                    TestLog.Debug("Execution cancelled after discovery conversion");
                    LogToDump("DiscoveryConverted", "Cancelled after discovery conversion");
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
                LogToDump("ForcedTermination", "Assembly processing terminated due to cancellation");
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
            if (IsMTP)
            {
                // Enhanced MTP stop pattern - complete tests synchronously FIRST
                LogToDump("MTPStopRun", "Enhanced MTP stop pattern initiated");

                // Step 1: Try synchronous test completion if not already done
                var runningTestCount = 0;
                lock (_runningTestsLock)
                {
                    runningTestCount = _runningTests.Count;
                }

                if (runningTestCount > 0)
                {
                    LogToDump("MTPStopRun", $"Found {runningTestCount} running tests - attempting synchronous completion");
                    CompleteAllRunningTestsSynchronously();
                }
                else
                {
                    LogToDump("MTPStopRun", "No running tests found - synchronous completion not needed");
                }

                // Step 2: Brief delay for any completion messages to propagate
                LogToDump("MTPStopRun", "Waiting for completion messages to propagate");
                Thread.Sleep(200);

                // Step 3: Try graceful engine stop
                var gracefulTask = Task.Run(() => NUnitEngineAdapter?.StopRun());
                var gracefulTimeout = TimeSpan.FromSeconds(2);

                if (gracefulTask.Wait(gracefulTimeout))
                {
                    LogToDump("MTPStopRun", "Graceful engine stop successful");
                }
                else
                {
                    LogToDump("MTPStopRun", $"Engine stop timeout after {gracefulTimeout.TotalSeconds}s - proceeding with emergency cleanup", logLevel: LogLevel.Warning);

                    // Step 4: If synchronous completion didn't work, try async fallback
                    LogToDump("MTPStopRun", "Attempting async fallback session cleanup");
                    ForceMTPSessionEnd();

                    // Step 5: Fire-and-forget close runner
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            NUnitEngineAdapter?.CloseRunner();
                            LogToDump("MTPStopRun", "Fire-and-forget CloseRunner completed");
                        }
                        catch (Exception ex)
                        {
                            LogToDump("MTPStopRun", $"Fire-and-forget CloseRunner exception: {ex.Message}");
                        }
                    });

                    LogToDump("MTPStopRun", "Emergency cleanup initiated - not waiting for completion");
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
            LogToDump("StopRunException", $"StopRun exception: {ex.Message}", logLevel: LogLevel.Warning);
        }
    }

    // Add emergency stop method for severely hanging scenarios
    public void EmergencyStop()
    {
        LogToDump("EmergencyStop", "EmergencyStop initiated");

        try
        {
            // Force MTP session cleanup FIRST for emergency scenarios
            if (IsMTP)
            {
                LogToDump("EmergencyMTPCleanup", "Emergency MTP session cleanup");

                // Emergency approach: Skip FrameworkHandle entirely
                EmergencyMTPSessionEnd();

                // Very brief delay for session messages to fire
                Task.Delay(50).Wait();
            }

            // Skip ALL cleanup - mark as cancelled and return immediately
            _cancelled = true;

            // Fire-and-forget minimal cleanup
            _ = Task.Run(() =>
            {
                try
                {
                    NUnitEngineAdapter?.Dispose();
                }
                catch
                {
                    // Ignore all exceptions
                }
            });

            LogToDump("EmergencyStop", "EmergencyStop completed");
        }
        catch (Exception ex)
        {
            LogToDump("EmergencyStop", $"EmergencyStop exception: {ex.Message}", logLevel: LogLevel.Warning);
        }
    }

    /// <summary>
    /// Emergency MTP session end that bypasses FrameworkHandle entirely
    /// </summary>
    private void EmergencyMTPSessionEnd()
    {
        if (!IsMTP) return;

        LogToDump("EmergencyMTPSessionEnd", "Starting emergency MTP session cleanup - bypassing FrameworkHandle");

        try
        {
            // Clear all internal tracking immediately
            lock (_runningTestsLock)
            {
                var testCount = _runningTests.Count;
                _runningTests.Clear();
                LogToDump("EmergencyMTPSessionEnd", $"Forcibly cleared {testCount} tracked tests");
            }

            // Try to find and manipulate any session state at the process level
            try
            {
                // Look for MTP-specific environment variables or process-level state
                var processId = Process.GetCurrentProcess().Id;
                LogToDump("EmergencyMTPSessionEnd", $"Process ID: {processId}");

                // Set environment variable to signal emergency session end
                Environment.SetEnvironmentVariable("NUNIT_MTP_EMERGENCY_SESSION_END", "true", EnvironmentVariableTarget.Process);
                LogToDump("EmergencyMTPSessionEnd", "Set emergency session end environment variable");
            }
            catch (Exception envEx)
            {
                LogToDump("EmergencyMTPSessionEnd", $"Environment variable approach failed: {envEx.Message}");
            }

            LogToDump("EmergencyMTPSessionEnd", "Emergency MTP session cleanup completed");
        }
        catch (Exception ex)
        {
            LogToDump("EmergencyMTPSessionEnd", $"Emergency session end failed: {ex.Message}");
        }
    }

    public IDumpXml Dump { get; private set; }

    #region MTP Session Management

    /// <summary>
    /// Track a test that has started execution
    /// </summary>
    public void TrackRunningTest(TestCase testCase)
    {
        if (!IsMTP) return;

        lock (_runningTestsLock)
        {
            _runningTests.Add(testCase);
        }

        LogToDump("TestTrackingStart", $"Tracking test: {testCase.DisplayName}");
    }

    /// <summary>
    /// Remove a test from running tests when it completes normally
    /// </summary>
    public void UntrackRunningTest(TestCase testCase)
    {
        if (!IsMTP) return;

        lock (_runningTestsLock)
        {
            _runningTests.Remove(testCase);
        }
    }

    /// <summary>
    /// CRITICAL: Complete all running tests SYNCHRONOUSLY using direct message bus access
    /// This bypasses the FrameworkHandle entirely since it gets cancelled before we can use it
    /// </summary>
    private void CompleteAllRunningTestsSynchronously()
    {
        if (!IsMTP) return;

        TestCase[] runningTestsCopy;
        lock (_runningTestsLock)
        {
            runningTestsCopy = _runningTests.ToArray();
            _runningTests.Clear();
        }

        LogToDump("DirectMessageBusCompletion", $"Completing {runningTestsCopy.Length} tests via DIRECT message bus (bypassing FrameworkHandle entirely)");

        // Use ONLY direct message bus - no FrameworkHandle fallback since that's the source of race conditions
        if (!TryCompleteTestsViaDirectMessageBus(runningTestsCopy))
        {
            LogToDump("DirectMessageBusCompletion", "Direct message bus completion failed - tests will remain incomplete (intentionally NOT falling back to FrameworkHandle to avoid race conditions)");
        }
    }

    /// <summary>
    /// Complete tests via direct MTP message bus access (TestFX pattern)
    /// UPDATED: Now implements proper TestFX type-safe approach from documentation
    /// </summary>
    private bool TryCompleteTestsViaDirectMessageBus(TestCase[] runningTests)
    {
        // Get direct access to MTP message bus AND DataProducer (TestFX requirement)
        var (messageBus, dataProducer) = GetDirectMessageBusAndDataProducer();
        var sessionUid = GetMTPSessionUid();

        if (messageBus == null || sessionUid == null || dataProducer == null)
        {
            LogToDump("DirectMessageBusCompletion", $"Could not access direct message bus - messageBus: {messageBus != null}, sessionUid: {sessionUid != null}, dataProducer: {dataProducer != null}");
            return false;
        }

        LogToDump("DirectMessageBusCompletion", $"Using direct message bus: {messageBus.GetType().Name}");
        LogToDump("DirectMessageBusCompletion", $"Using DataProducer: {dataProducer.GetType().Name}");

        int successfulCompletions = 0;
        int failedCompletions = 0;

        // Complete tests via direct message bus using TestFX type-safe patterns
        foreach (var testCase in runningTests)
        {
            try
            {
                LogToDump("DirectMessageBusCompletion", $"Completing test via message bus: {testCase.DisplayName}");

                // FIXED: Create TestNodeUid properly (not from Guid directly) - per TestFX documentation
                var testNodeUid = CreateTestNodeUidTypeSafe(testCase.Id);
                if (testNodeUid == null)
                {
                    LogToDump("DirectMessageBusCompletion", $"Failed to create TestNodeUid for: {testCase.DisplayName}");
                    failedCompletions++;
                    continue;
                }

                // Create TestNode using TestFX pattern
                var testNode = CreateTestNodeTypeSafe(testNodeUid, testCase.DisplayName);
                if (testNode == null)
                {
                    LogToDump("DirectMessageBusCompletion", $"Failed to create TestNode for: {testCase.DisplayName}");
                    failedCompletions++;
                    continue;
                }

                // FIXED: Use proper TestNodeStateChangedMessage instead of anonymous type - per TestFX documentation
                var message = CreateTestNodeStateChangedMessage(sessionUid, testNode, "Skipped", "Test cancelled due to parallel execution timeout");

                if (message != null)
                {
                    // FIXED: Use correct PublishAsync signature with IDataProducer as first parameter - per TestFX documentation
                    var publishTask = InvokePublishAsyncWithDataProducer(messageBus, dataProducer, message);

                    // Wait synchronously with timeout to avoid hanging
                    if (publishTask != null && publishTask.Wait(TimeSpan.FromSeconds(1)))
                    {
                        LogToDump("DirectMessageBusCompletion", $"Successfully completed via message bus: {testCase.DisplayName}");
                        successfulCompletions++;
                    }
                    else
                    {
                        LogToDump("DirectMessageBusCompletion", $"Message bus publish timeout for: {testCase.DisplayName}");
                        failedCompletions++;
                    }
                }
                else
                {
                    LogToDump("DirectMessageBusCompletion", $"Failed to create message for: {testCase.DisplayName}");
                    failedCompletions++;
                }
            }
            catch (Exception ex)
            {
                LogToDump("DirectMessageBusCompletion", $"Message bus completion failed for {testCase.DisplayName}: {ex.Message}");
                failedCompletions++;
            }
        }

        LogToDump("DirectMessageBusCompletion", $"Direct message bus results: {successfulCompletions} successful, {failedCompletions} failed out of {runningTests.Length} tests");

        // If we successfully completed some tests, send session end via message bus
        if (successfulCompletions > 0)
        {
            try
            {
                SendSessionEndViaDirectMessageBus(messageBus, sessionUid);
            }
            catch (Exception sessionEx)
            {
                LogToDump("DirectMessageBusCompletion", $"Session end via message bus failed: {sessionEx.Message}");
            }
        }

        return successfulCompletions > 0;
    }

    /// <summary>
    /// Get direct access to MTP message bus AND DataProducer (TestFX Copilot Clean Pattern)
    /// Uses TestFX recommended static instance pattern - no reflection fallback needed
    /// Per TestFX Copilot: NUnitBridgedTestFramework IS the IDataProducer
    /// </summary>
    private (object MessageBus, object DataProducer) GetDirectMessageBusAndDataProducer()
    {
        try
        {
            // TestFX Copilot recommended pattern - use static framework instance DIRECTLY
            if (IsMTP)
            {
                // Access the static properties directly - no reflection needed!
                var frameworkInstance = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentInstance;
                var messageBus = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentMessageBus;

                if (frameworkInstance != null && messageBus != null)
                {
                    LogToDump("DirectMessageBusAccess", $"✅ Found framework instance via DIRECT access - Framework: {frameworkInstance.GetType().Name}, MessageBus: {messageBus.GetType().Name}");
                    // Per TestFX Copilot: NUnitBridgedTestFramework IS the IDataProducer
                    return (messageBus, frameworkInstance);
                }
                else
                {
                    LogToDump("DirectMessageBusAccess", $"❌ Static properties not available - frameworkInstance: {frameworkInstance != null}, messageBus: {messageBus != null}");
                }
            }

            LogToDump("DirectMessageBusAccess", "Could not access direct message bus - not MTP or static properties unavailable");
            return (null, null);
        }
        catch (Exception ex)
        {
            LogToDump("DirectMessageBusAccess", $"Exception getting message bus access: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Create TestNodeUid using direct TestFX types (TestFx Copilot pattern)
    /// </summary>
    private object CreateTestNodeUidTypeSafe(Guid guid)
    {
        try
        {
            // Create TestNodeUid directly using the correct TestFX types
            var testNodeUid = new TestNodeUid(guid.ToString());
            LogToDump("TestNodeUidCreation", $"Created TestNodeUid: {testNodeUid}");
            return testNodeUid;
        }
        catch (Exception ex)
        {
            LogToDump("TestNodeUidCreation", $"Error creating TestNodeUid: {ex.Message}");
            return guid.ToString(); // Fallback
        }
    }

    /// <summary>
    /// Create TestNode using direct TestFX types (TestFx Copilot pattern)
    /// </summary>
    private object CreateTestNodeTypeSafe(object testNodeUid, string displayName)
    {
        try
        {
            // Create TestNode directly using the correct TestFX types
            var testNode = new TestNode
            {
                Uid = (TestNodeUid)testNodeUid,
                DisplayName = displayName,
                Properties = new PropertyBag()
            };

            LogToDump("TestNodeCreation", $"Created TestNode for: {displayName}");
            return testNode;
        }
        catch (Exception ex)
        {
            LogToDump("TestNodeCreation", $"Error creating TestNode: {ex.Message}");
            return new { Uid = testNodeUid, DisplayName = displayName };
        }
    }

    /// <summary>
    /// Create TestNodeStateChangedMessage (TestFX Clean Pattern)
    /// FIXED: Use direct TestFX types as recommended by TestFx Copilot
    /// </summary>
    private object CreateTestNodeStateChangedMessage(object sessionUid, object testNode, string state, string reason)
    {
        try
        {
            LogToDump("MessageCreation", "Creating TestNodeUpdateMessage using direct TestFX types");

            // Cast to the correct TestFX SessionUid type
            Microsoft.Testing.Platform.TestHost.SessionUid typedSessionUid;
            if (sessionUid is Microsoft.Testing.Platform.TestHost.SessionUid sessionUidTyped)
            {
                typedSessionUid = sessionUidTyped;
            }
            else
            {
                // Create new SessionUid from the object
                typedSessionUid = new Microsoft.Testing.Platform.TestHost.SessionUid(sessionUid.ToString());
            }

            // Create TestNodeUpdateMessage directly using the correct TestFX types
            var testNodeUpdateMessage = new TestNodeUpdateMessage(
                sessionUid: typedSessionUid,
                testNode: (TestNode)testNode);

            LogToDump("MessageCreation", $"✅ Successfully created TestNodeUpdateMessage: {testNodeUpdateMessage.GetType().Name}");
            return testNodeUpdateMessage;
        }
        catch (Exception ex)
        {
            LogToDump("MessageCreation", $"Error creating TestNodeUpdateMessage: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Invoke PublishAsync with DataProducer (TestFX pattern)
    /// CORRECTED: Uses the actual TestFX signature PublishAsync(IDataProducer, message) - NO CancellationToken
    /// </summary>
    private Task InvokePublishAsyncWithDataProducer(object messageBus, object dataProducer, object message)
    {
        try
        {
            var messageBusType = messageBus.GetType();
            LogToDump("InvokePublishAsync", $"Message bus type: {messageBusType.Name}");

            // Get all PublishAsync methods to see what signatures are available
            var publishMethods = messageBusType.GetMethods().Where(m => m.Name == "PublishAsync").ToArray();
            LogToDump("InvokePublishAsync", $"Found {publishMethods.Length} PublishAsync methods");

            foreach (var method in publishMethods)
            {
                var parameters = method.GetParameters();
                var paramTypes = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                LogToDump("InvokePublishAsync", $"PublishAsync signature: ({paramTypes})");
            }

            // Try the patterns in order of preference based on TestFX documentation
            foreach (var publishMethod in publishMethods)
            {
                var parameters = publishMethod.GetParameters();

                try
                {
                    object result = null;

                    // Pattern 1: PublishAsync(IDataProducer, message) - CORRECT TestFX pattern (2 parameters, no CancellationToken)
                    if (parameters.Length == 2 && (parameters[0].Name.Contains("dataProducer") || parameters[0].ParameterType.Name.Contains("IDataProducer")))
                    {
                        LogToDump("InvokePublishAsync", "Trying PublishAsync(IDataProducer, message) pattern - CORRECT TestFX signature");
                        result = publishMethod.Invoke(messageBus, new object[] { dataProducer, message });
                    }
                    // Pattern 2: PublishAsync(message, CancellationToken) - fallback pattern
                    else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(CancellationToken))
                    {
                        LogToDump("InvokePublishAsync", "Trying PublishAsync(message, CancellationToken) pattern");
                        result = publishMethod.Invoke(messageBus, new object[] { message, CancellationToken.None });
                    }
                    // Pattern 3: Just the message
                    else if (parameters.Length == 1)
                    {
                        LogToDump("InvokePublishAsync", "Trying PublishAsync(message) pattern");
                        result = publishMethod.Invoke(messageBus, new object[] { message });
                    }
                    // Pattern 4: Other 2-parameter combinations
                    else if (parameters.Length == 2)
                    {
                        LogToDump("InvokePublishAsync", "Trying other 2-parameter PublishAsync pattern");
                        result = publishMethod.Invoke(messageBus, new object[] { dataProducer, message });
                    }

                    if (result is Task task)
                    {
                        LogToDump("InvokePublishAsync", $"✅ Successfully invoked PublishAsync with {parameters.Length} parameters");
                        return task;
                    }
                }
                catch (Exception methodEx)
                {
                    LogToDump("InvokePublishAsync", $"Method with {parameters.Length} params failed: {methodEx.Message}");
                    continue; // Try next method
                }
            }

            LogToDump("InvokePublishAsync", "❌ All PublishAsync method attempts failed");
            return null;
        }
        catch (Exception ex)
        {
            LogToDump("InvokePublishAsync", $"Error investigating PublishAsync methods: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get MTP session UID (TestFX pattern)
    /// </summary>
    private object GetMTPSessionUid()
    {
        try
        {
            // Try to get session UID from RunContext or FrameworkHandle
            if (RunContext != null)
            {
                var runContextType = RunContext.GetType();

                // Look for session UID field or property
                var sessionFields = runContextType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                    .Where(f => f.Name.Contains("Session") || f.Name.Contains("Uid") || f.Name.Contains("Id"));

                foreach (var field in sessionFields)
                {
                    try
                    {
                        var value = field.GetValue(RunContext);
                        if (value != null)
                        {
                            LogToDump("MTPSessionUid", $"Found potential session UID via field {field.Name}: {value.GetType().Name}");
                            return value;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToDump("MTPSessionUid", $"Error accessing field {field.Name}: {ex.Message}");
                    }
                }

                var sessionProperties = runContextType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.Name.Contains("Session") || p.Name.Contains("Uid") || p.Name.Contains("Id"));

                foreach (var prop in sessionProperties)
                {
                    try
                    {
                        var value = prop.GetValue(RunContext);
                        if (value != null)
                        {
                            LogToDump("MTPSessionUid", $"Found potential session UID via property {prop.Name}: {value.GetType().Name}");
                            return value;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToDump("MTPSessionUid", $"Error accessing property {prop.Name}: {ex.Message}");
                    }
                }
            }

            // Try to create a fake session UID if we can't find the real one
            var guidValue = Guid.NewGuid();
            LogToDump("MTPSessionUid", $"Creating fallback session UID: {guidValue}");
            return guidValue;
        }
        catch (Exception ex)
        {
            LogToDump("MTPSessionUid", $"Exception getting session UID: {ex.Message}");
            return Guid.NewGuid(); // Ultimate fallback
        }
    }

    /// <summary>
    /// Create TestNode from TestCase (TestFX pattern)
    /// </summary>
    private object CreateTestNodeFromTestCase(TestCase testCase)
    {
        try
        {
            // Use reflection to create TestNode dynamically since we don't have direct access
            var testNodeType = Type.GetType("Microsoft.Testing.Platform.Messages.TestNode");
            if (testNodeType == null)
            {
                // Try alternative type names
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        testNodeType = assembly.GetTypes().FirstOrDefault(t => t.Name == "TestNode");
                        if (testNodeType != null) break;
                    }
                    catch { }
                }
            }

            if (testNodeType != null)
            {
                var testNode = Activator.CreateInstance(testNodeType);

                // Create proper TestNodeUid from GUID
                var testNodeUid = CreateTestNodeUid(testCase.Id);

                // Set properties using reflection
                SetProperty(testNode, "DisplayName", testCase.DisplayName);
                SetProperty(testNode, "Uid", testNodeUid);

                LogToDump("TestNodeCreation", $"Created TestNode for: {testCase.DisplayName}");
                return testNode;
            }

            LogToDump("TestNodeCreation", "Could not find TestNode type - creating fallback object");
            return new { DisplayName = testCase.DisplayName, Uid = CreateTestNodeUid(testCase.Id) };
        }
        catch (Exception ex)
        {
            LogToDump("TestNodeCreation", $"Error creating TestNode: {ex.Message}");
            return new { DisplayName = testCase.DisplayName, Uid = CreateTestNodeUid(testCase.Id) };
        }
    }

    /// <summary>
    /// Create TestNodeUid from GUID (MTP format)
    /// </summary>
    private object CreateTestNodeUid(Guid guid)
    {
        try
        {
            // Try to create TestNodeUid using reflection
            var testNodeUidType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestNodeUid");
            if (testNodeUidType == null)
            {
                // Search in loaded assemblies
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        testNodeUidType = assembly.GetTypes().FirstOrDefault(t => t.Name == "TestNodeUid");
                        if (testNodeUidType != null) break;
                    }
                    catch { }
                }
            }

            if (testNodeUidType != null)
            {
                // Try different constructor patterns
                try
                {
                    // Try constructor that takes string
                    var constructor = testNodeUidType.GetConstructor(new[] { typeof(string) });
                    if (constructor != null)
                    {
                        return constructor.Invoke(new object[] { guid.ToString() });
                    }

                    // Try constructor that takes Guid
                    constructor = testNodeUidType.GetConstructor(new[] { typeof(Guid) });
                    if (constructor != null)
                    {
                        return constructor.Invoke(new object[] { guid });
                    }

                    // Try static factory methods
                    var fromStringMethod = testNodeUidType.GetMethod("FromString", BindingFlags.Static | BindingFlags.Public);
                    if (fromStringMethod != null)
                    {
                        return fromStringMethod.Invoke(null, new object[] { guid.ToString() });
                    }

                    var fromGuidMethod = testNodeUidType.GetMethod("FromGuid", BindingFlags.Static | BindingFlags.Public);
                    if (fromGuidMethod != null)
                    {
                        return fromGuidMethod.Invoke(null, new object[] { guid });
                    }
                }
                catch (Exception ex)
                {
                    LogToDump("TestNodeUidCreation", $"Error creating TestNodeUid: {ex.Message}");
                }
            }

            LogToDump("TestNodeUidCreation", "Using GUID string as fallback for TestNodeUid");
            return guid.ToString();
        }
        catch (Exception ex)
        {
            LogToDump("TestNodeUidCreation", $"Exception creating TestNodeUid: {ex.Message}");
            return guid.ToString();
        }
    }

    /// <summary>
    /// Create TestNodeUpdateMessage (TestFX pattern)
    /// </summary>
    private object CreateTestNodeUpdateMessage(object sessionUid, object testNode, TestOutcome outcome, string reason)
    {
        try
        {
            // Try to create proper MTP message types using reflection
            var testNodeUpdateMessageType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestNodeUpdateMessage");
            var testNodeUpdateType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestNodeUpdate");
            var testResultPropertyType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestResultProperty");

            // Search in loaded assemblies if direct type loading fails
            if (testNodeUpdateMessageType == null || testNodeUpdateType == null || testResultPropertyType == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        testNodeUpdateMessageType ??= assembly.GetTypes().FirstOrDefault(t => t.Name == "TestNodeUpdateMessage");
                        testNodeUpdateType ??= assembly.GetTypes().FirstOrDefault(t => t.Name == "TestNodeUpdate");
                        testResultPropertyType ??= assembly.GetTypes().FirstOrDefault(t => t.Name == "TestResultProperty");

                        if (testNodeUpdateMessageType != null && testNodeUpdateType != null && testResultPropertyType != null)
                            break;
                    }
                    catch { }
                }
            }

            if (testNodeUpdateMessageType != null && testNodeUpdateType != null && testResultPropertyType != null)
            {
                LogToDump("MessageCreation", "Creating proper MTP message types via reflection");

                // Create TestResultProperty
                var testResultProperty = Activator.CreateInstance(testResultPropertyType, outcome, reason);

                // Create TestNodeUpdate
                var testNodeUpdate = Activator.CreateInstance(testNodeUpdateType);
                SetProperty(testNodeUpdate, "Node", testNode);
                SetProperty(testNodeUpdate, "Property", testResultProperty);

                // Create TestNodeUpdateMessage
                var updateMessage = Activator.CreateInstance(testNodeUpdateMessageType, sessionUid, testNodeUpdate);

                LogToDump("MessageCreation", "Successfully created proper MTP message");
                return updateMessage;
            }

            LogToDump("MessageCreation", "Could not create proper MTP types - using simplified approach");

            // Try simpler approach - create a basic message that might work
            var simpleMessage = new Dictionary<string, object>
            {
                ["SessionUid"] = sessionUid,
                ["TestNode"] = testNode,
                ["Outcome"] = outcome,
                ["Reason"] = reason,
                ["Timestamp"] = DateTime.UtcNow,
                ["MessageType"] = "TestNodeUpdate"
            };

            return simpleMessage;
        }
        catch (Exception ex)
        {
            LogToDump("MessageCreation", $"Error creating update message: {ex.Message}");

            // Final fallback
            return new Dictionary<string, object>
            {
                ["SessionUid"] = sessionUid,
                ["TestNode"] = testNode,
                ["Outcome"] = outcome,
                ["Reason"] = reason
            };
        }
    }

    /// <summary>
    /// Send session end via direct message bus (Documentation Pattern)
    /// UPDATED: Using the NUnitCancel.md approach - "Force All Tests to Complete" then exit
    /// </summary>
    private void SendSessionEndViaDirectMessageBus(object messageBus, object sessionUid)
    {
        try
        {
            LogToDump("DirectSessionEnd", "Using NUnitCancel.md documentation approach - Force All Tests to Complete");

            // Get the current running tests to complete
            TestCase[] runningTestsCopy;
            lock (_runningTestsLock)
            {
                runningTestsCopy = _runningTests.ToArray();
                _runningTests.Clear(); // Clear tracking as per documentation
            }

            LogToDump("DirectSessionEnd", $"Documentation pattern: completing {runningTestsCopy.Length} tests then ending session");

            // DOCUMENTATION PATTERN: Force All Tests to Complete (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(300); // Very short timeout per docs

                    // Get the NUnit framework as IDataProducer
                    var dataProducer = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentInstance;
                    if (dataProducer == null)
                    {
                        LogToDump("DirectSessionEnd", "❌ No IDataProducer - using immediate exit");
                        Environment.Exit(0);
                        return;
                    }

                    LogToDump("DirectSessionEnd", "🔧 Force completion for all running tests (documentation pattern)");

                    // Force completion for all running tests using TestNodeUpdate pattern
                    foreach (var runningTest in runningTestsCopy)
                    {
                        try
                        {
                            // Create TestNode properly
                            var testNodeUid = CreateTestNodeUidTypeSafe(runningTest.Id);
                            var testNode = CreateTestNodeTypeSafe(testNodeUid, runningTest.DisplayName);

                            // DOCUMENTATION APPROACH: Use TestNodeUpdate wrapper with TestResultProperty
                            var testNodeUpdate = CreateTestNodeUpdateWithProperty(testNode, "Test cancelled due to parallel execution timeout");

                            if (testNodeUpdate != null)
                            {
                                // Create TestNodeUpdateMessage with TestNode (not TestNodeUpdate wrapper)
                                // The documentation pattern uses TestNodeUpdateMessage(sessionUid, testNode)

                                try
                                {
                                    // Use TestNode directly, not TestNodeUpdate
                                    var testCompletionMessage = new TestNodeUpdateMessage(
                                        (Microsoft.Testing.Platform.TestHost.SessionUid)sessionUid,
                                        (TestNode)testNode); // Use testNode, not testNodeUpdate

                                    // Use the correct 2-parameter PublishAsync pattern
                                    await InvokePublishAsyncWithDataProducer(messageBus, dataProducer, testCompletionMessage);

                                    LogToDump("DirectSessionEnd", $"✅ Completed via docs pattern: {runningTest.DisplayName}");
                                }
                                catch (InvalidCastException castEx)
                                {
                                    LogToDump("DirectSessionEnd", $"⚠️ Type casting failed: {castEx.Message}, skipping: {runningTest.DisplayName}");
                                }
                            }
                            else
                            {
                                LogToDump("DirectSessionEnd", $"⚠️ Could not create TestNodeUpdate for: {runningTest.DisplayName}");
                            }
                        }
                        catch (Exception testEx)
                        {
                            LogToDump("DirectSessionEnd", $"Test completion failed: {runningTest.DisplayName} - {testEx.Message}");
                        }
                    }

                    // Brief delay for message propagation (per documentation)
                    await Task.Delay(50, cts.Token);

                    // Now send session end (per documentation)
                    LogToDump("DirectSessionEnd", "📤 Sending session end message (documentation pattern)");
                    await SendSessionEndMessage(messageBus, sessionUid, dataProducer);
                }
                catch (Exception ex)
                {
                    LogToDump("DirectSessionEnd", $"Documentation pattern task failed: {ex.Message}");
                }
                finally
                {
                    // CRITICAL: Let TestFX handle session end naturally, then exit if needed
                    LogToDump("DirectSessionEnd", "🏁 Attempting natural TestFX session end");

                    try
                    {
                        var framework = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentInstance;
                        if (framework != null)
                        {
                            LogToDump("DirectSessionEnd", "🔧 Triggering TestFX session end via cancellation");
                            await framework.EndSessionExplicitly();
                            LogToDump("DirectSessionEnd", "✅ TestFX session end triggered");
                        }
                        else
                        {
                            LogToDump("DirectSessionEnd", "⚠️ No framework instance available");
                        }
                    }
                    catch (Exception sessionEx)
                    {
                        LogToDump("DirectSessionEnd", $"TestFX session end error: {sessionEx.Message}");
                    }

                    // Give TestFX extended time to send proper session end events
                    LogToDump("DirectSessionEnd", "⏱️ Allowing extended time for TestFX session end processing");
                    await Task.Delay(1000); // Longer delay for TestFX to process session end

                    // Only exit if TestFX doesn't handle it naturally
                    LogToDump("DirectSessionEnd", "✅ TestFX session processing complete - controlled exit");
                    Environment.Exit(0);
                }
            });

            LogToDump("DirectSessionEnd", "✅ Documentation approach initiated successfully");
        }
        catch (Exception ex)
        {
            LogToDump("DirectSessionEnd", $"Error in documentation approach: {ex.Message}");

            // Immediate fallback per documentation
            LogToDump("DirectSessionEnd", "🏁 IMMEDIATE fallback - ensuring TestFX session end");

            // Even in fallback, try to trigger TestFX session end properly
            try
            {
                var framework = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentInstance;
                if (framework != null)
                {
                    LogToDump("DirectSessionEnd", "🔧 Emergency TestFX session end trigger");
                    // Use synchronous approach for immediate fallback with longer timeout
                    framework.EndSessionExplicitly().Wait(2000); // 2 second timeout for TestFX processing
                    LogToDump("DirectSessionEnd", "✅ Emergency session end successful");

                    // Longer delay for TestFX session end processing
                    Thread.Sleep(800);
                }
            }
            catch (Exception emergencyEx)
            {
                LogToDump("DirectSessionEnd", $"Emergency session end failed: {emergencyEx.Message}");
            }

            try
            {
                Environment.Exit(0);
            }
            catch
            {
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
        }
    }

    /// <summary>
    /// Create TestNodeUpdate with TestResultProperty (Documentation Pattern)
    /// </summary>
    private object CreateTestNodeUpdateWithProperty(object testNode, string reason)
    {
        try
        {
            // Try to create TestNodeUpdate wrapper as per documentation
            var testNodeUpdateType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestNodeUpdate");
            var testResultPropertyType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestResultProperty");

            if (testNodeUpdateType != null && testResultPropertyType != null)
            {
                // Create TestNodeUpdate wrapper (per documentation)
                var testNodeUpdate = Activator.CreateInstance(testNodeUpdateType);
                SetProperty(testNodeUpdate, "Node", testNode);

                // Create TestResultProperty (per documentation)
#if NET8_0_OR_GREATER
                var testOutcome = TestOutcome.Skipped;
#else
                var testOutcome = TestOutcome.None; // .NET Framework compatibility
#endif

                var testResultProperty = Activator.CreateInstance(testResultPropertyType, testOutcome, reason);
                SetProperty(testNodeUpdate, "Property", testResultProperty);

                LogToDump("DirectSessionEnd", "✅ Created TestNodeUpdate with TestResultProperty (documentation pattern)");
                return testNodeUpdate;
            }
            else
            {
                LogToDump("DirectSessionEnd", $"⚠️ TestNodeUpdate types not found - updateType: {testNodeUpdateType != null}, propertyType: {testResultPropertyType != null}");
                return null;
            }
        }
        catch (Exception ex)
        {
            LogToDump("DirectSessionEnd", $"Error creating TestNodeUpdate: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Send session end message using documentation approach
    /// </summary>
    private async Task SendSessionEndMessage(object messageBus, object sessionUid, object dataProducer)
    {
        try
        {
            // Try to create session end message using reflection (per documentation approach)
            var sessionEndMessageType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestSessionEndMessage");
            var testSessionResultType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestSessionResult");
            var testSessionStateType = Type.GetType("Microsoft.Testing.Platform.Extensions.Messages.TestSessionState");

            if (sessionEndMessageType != null && testSessionResultType != null && testSessionStateType != null)
            {
                LogToDump("DirectSessionEnd", "📨 Creating session end message with proper types");

                // Create TestSessionResult
                var sessionResult = Activator.CreateInstance(testSessionResultType);

                // Set State using enum value
                var cancelledState = Enum.GetValues(testSessionStateType)
                    .Cast<object>()
                    .FirstOrDefault(v => v.ToString() == "Cancelled");

                SetProperty(sessionResult, "State", cancelledState);
                SetProperty(sessionResult, "ExitCode", -1);

                // Create TestSessionEndMessage
                var sessionEndMessage = Activator.CreateInstance(sessionEndMessageType, sessionUid, sessionResult);

                if (sessionEndMessage != null)
                {
                    // CRITICAL: Use correct PublishAsync signature (documentation pattern)
                    await InvokePublishAsyncWithDataProducer(messageBus, dataProducer, sessionEndMessage);
                    LogToDump("DirectSessionEnd", "✅ Session end message sent successfully!");
                    return;
                }
            }

            LogToDump("DirectSessionEnd", "⚠️ Session end types not found - using clean exit fallback");
        }
        catch (Exception ex)
        {
            LogToDump("DirectSessionEnd", $"Session end message failed: {ex.Message}");
        }

        // Per documentation: clean exit even if session end fails since tests completed
        LogToDump("DirectSessionEnd", "🏁 Session end fallback - tests completed successfully");
    }

    /// <summary>
    /// Helper to set property via reflection
    /// </summary>
    private void SetProperty(object obj, string propertyName, object value)
    {
        try
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanWrite)
            {
                property.SetValue(obj, value);
            }
        }
        catch (Exception ex)
        {
            LogToDump("SetProperty", $"Error setting {propertyName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper to invoke PublishAsync via reflection
    /// </summary>
    private Task InvokePublishAsync(object messageBus, object message)
    {
        try
        {
            var publishMethod = messageBus.GetType().GetMethod("PublishAsync");
            if (publishMethod != null)
            {
                var result = publishMethod.Invoke(messageBus, new object[] { message, CancellationToken.None });
                if (result is Task task)
                {
                    return task;
                }
            }

            LogToDump("InvokePublishAsync", "PublishAsync method not found or didn't return Task");
            return null;
        }
        catch (Exception ex)
        {
            LogToDump("InvokePublishAsync", $"Error invoking PublishAsync: {ex.Message}");
            return null;
        }
    }

    #endregion

    /// <summary>
    /// Force MTP session end - used for cleanup when synchronous completion fails
    /// </summary>
    private void ForceMTPSessionEnd()
    {
        if (!IsMTP) return;

        LogToDump("MTPSessionEndFallback", "Starting MTP session cleanup");

        // Fire-and-forget task to avoid blocking
        _ = Task.Run(async () =>
        {
            try
            {
                // Extended timeout for session-level operations
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                // Force cleanup of internal state
                lock (_runningTestsLock)
                {
                    var remainingTests = _runningTests.Count;
                    _runningTests.Clear();
                    if (remainingTests > 0)
                    {
                        LogToDump("MTPSessionEndFallback", $"Forcibly cleared {remainingTests} remaining tracked tests");
                    }
                }

                // Try to signal session completion if FrameworkHandle is still available
                try
                {
                    if (FrameworkHandle != null)
                    {
                        FrameworkHandle.EnableShutdownAfterTestRun = true;
                        LogToDump("MTPSessionEndFallback", "EnableShutdownAfterTestRun set to true");
                    }

                    await Task.Delay(100, cts.Token);
                }
                catch (Exception signalEx)
                {
                    LogToDump("MTPSessionEndFallback", $"Session end signal failed: {signalEx.Message}");
                }

                LogToDump("MTPSessionEndFallback", "MTP session cleanup completed");
            }
            catch (OperationCanceledException)
            {
                LogToDump("MTPSessionEndFallback", "MTP session cleanup timed out after 2 seconds");
            }
            catch (Exception ex)
            {
                LogToDump("MTPSessionEndFallback", $"MTP session cleanup failed: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Helper method to log with XML element formatting and optional immediate dump.
    /// </summary>
    /// <param name="elementName">Name of the XML element.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="appendToDump">Whether to immediately append to existing dump file.</param>
    /// <param name="logLevel">TestLog level - Debug, Info, Warning, or Error.</param>
    public void LogToDump(string elementName, string message, bool appendToDump = true, LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            // Null checks to prevent NullReferenceException
            if (string.IsNullOrEmpty(elementName))
                elementName = "UnknownElement";
            message ??= "";

            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");

            // Log to TestLog based on specified level - with null check
            if (TestLog != null)
            {
                switch (logLevel)
                {
                    case LogLevel.Debug:
                        TestLog.Debug($"[{timestamp}] {elementName}: {message}");
                        break;
                    case LogLevel.Info:
                        TestLog.Info($"[{timestamp}] {elementName}: {message}");
                        break;
                    case LogLevel.Warning:
                        TestLog.Warning($"[{timestamp}] {elementName}: {message}");
                        break;
                    case LogLevel.Error:
                        TestLog.Error($"[{timestamp}] {elementName}: {message}");
                        break;
                    default:
                        TestLog.Debug($"[{timestamp}] {elementName}: {message}");
                        break;
                }
            }

            var logMessage = $"{timestamp} - {message}\n";

            Dump?.AddXmlElement(elementName, logMessage);

            if (appendToDump)
            {
                Dump?.AppendToExistingDump();
            }
        }
        catch (Exception ex)
        {
            // Fallback error reporting if TestLog is null
            try
            {
                if (TestLog != null)
                {
                    TestLog.Debug($"Error in LogToDump: {ex.Message}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Error in LogToDump: {ex.Message}");
                }
            }
            catch
            {
                // Ultimate fallback - just use Debug.WriteLine
                System.Diagnostics.Debug.WriteLine($"Critical error in LogToDump: {ex.Message}");
            }
        }
    }

    private void CheckIfDebug()
    {
        if (!Settings.DebugExecution)
            return;
        if (!Debugger.IsAttached)
            Debugger.Launch();
    }
}