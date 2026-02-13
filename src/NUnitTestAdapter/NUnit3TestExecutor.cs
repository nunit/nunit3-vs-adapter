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
    private volatile bool _disposed = false;

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
                _cancelled = true;
                TestLog.Debug("Process exit - simple MTP cleanup");
                StopRun();
            }
        }
        catch (Exception ex)
        {
            TestLog.Debug($"Error during process exit cleanup: {ex.Message}");
        }
    }

    private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        if (IsMTP && !_cancelled)
        {
            e.Cancel = true; // Prevent immediate termination for graceful shutdown
            _cancelled = true;
            TestLog.Debug("Cancel key pressed - simple MTP cleanup");
            StopRun();
            TestLog.Debug("Cancel key pressed - initiating hard exit after cleanup");
            Environment.Exit(130);
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
        TestLog.Debug($"Cancel requested at {cancelTime}");

        try
        {
            // Set cancellation flag
            _cancelled = true;

            // For MTP scenarios, complete any in-flight tests
            if (IsMTP)
            {
                ReportCancelledTests();
            }

            // Stop NUnit engine gracefully
            NUnitEngineAdapter?.StopRun();

            TestLog.Debug("Cancel completed - engine stopped");
        }
        catch (Exception ex)
        {
            // CRITICAL: Log but NEVER throw from Cancel()
            TestLog.Debug($"Error during cancellation: {ex.Message}");
        }
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
            // Simple cleanup for MTP scenarios
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

            // Normal cleanup for all scenarios
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
        catch (Exception ex)
        {
            TestLog.Debug($"Exception during disposal: {ex.Message}");
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
            // Simple stop for all scenarios
            NUnitEngineAdapter?.StopRun();
            TestLog.Debug("Engine stop completed");
        }
        catch (Exception ex)
        {
            TestLog.Debug($"StopRun exception: {ex.Message}");
        }
    }

    public IDumpXml Dump { get; private set; }

    #region Test Completion Reporting for MTP

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

        TestLog.Debug($"Tracking test: {testCase.DisplayName}");
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
    /// Report cancelled tests via MTP message bus - simple approach without session management
    /// </summary>
    private void ReportCancelledTests()
    {
        if (!IsMTP) return;

        TestCase[] runningTestsCopy;
        lock (_runningTestsLock)
        {
            runningTestsCopy = _runningTests.ToArray();
            _runningTests.Clear();
        }

        if (runningTestsCopy.Length == 0)
        {
            TestLog.Debug("No running tests to report as cancelled");
            return;
        }

        TestLog.Debug($"Reporting {runningTestsCopy.Length} tests as cancelled via message bus");

        try
        {
            var messageBus = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentMessageBus;
            var sessionUid = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentSessionUid;
            var dataProducer = TestingPlatformAdapter.NUnitBridgedTestFramework.CurrentInstance;

            if (messageBus == null || sessionUid == null || dataProducer == null)
            {
                TestLog.Debug("Cannot access MTP message bus - tests will not be reported as cancelled");
                return;
            }

            // Report each test as cancelled
            foreach (var testCase in runningTestsCopy)
            {
                try
                {
                    var testNodeUid = new TestNodeUid(testCase.Id.ToString());
                    var testNode = new TestNode
                    {
                        Uid = testNodeUid,
                        DisplayName = testCase.DisplayName,
                        Properties = new PropertyBag()
                    };

                    var cancelledMessage = new TestNodeUpdateMessage(
                        (Microsoft.Testing.Platform.TestHost.SessionUid)sessionUid,
                        testNode);
                    messageBus.PublishAsync(dataProducer, cancelledMessage);

                    TestLog.Debug($"Reported test as cancelled: {testCase.DisplayName}");
                }
                catch (Exception ex)
                {
                    TestLog.Debug($"Failed to report test as cancelled: {testCase.DisplayName} - {ex.Message}");
                }
            }

            TestLog.Debug("Completed reporting cancelled tests - session end attempted");
        }
        catch (Exception ex)
        {
            TestLog.Debug($"Error reporting cancelled tests: {ex.Message}");
        }
    }

    #endregion

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