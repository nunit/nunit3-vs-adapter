// ***********************************************************************
// Copyright (c) 2020-2021 Charlie Poole, Terje Sandstrom
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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.Internal;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine;

public interface INUnitEngineAdapter
{
    NUnitResults Explore();
    void CloseRunner();
    NUnitResults Explore(TestFilter filter);
    NUnitResults Run(ITestEventListener listener, TestFilter filter);
    void StopRun();
    void SetDump(IDumpXml dumpXml);

    T GetService<T>()
        where T : class;

    void GenerateTestOutput(NUnitResults testResults, string assemblyPath, string testOutputXmlFolder);
    string GetXmlFilePath(string folder, string defaultFileName, string extension);
}

public class NUnitEngineAdapter : INUnitEngineAdapter, IDisposable
{
    private IAdapterSettings settings;
    private ITestLogger logger;
    private IDumpXml dump;
    private TestPackage package;
    private ITestEngine TestEngine { get; set; }
    private ITestRunner Runner { get; set; }

    internal event Action<ITestEngine> InternalEngineCreated;

    public bool EngineEnabled => TestEngine != null;

    public void Initialize(IAdapterSettings adapterSettings)
    {
#if NET462
        var engineX = new TestEngine();
#else
        var engineX = TestEngineActivator.CreateInstance() ?? new TestEngine();
#endif

        InternalEngineCreated?.Invoke(engineX);
        TestEngine = engineX;
        var tmpPath = Path.Combine(Path.GetTempPath(), "NUnit.Engine");
        if (!Directory.Exists(tmpPath))
            Directory.CreateDirectory(tmpPath);
        TestEngine.WorkDirectory = tmpPath;
        TestEngine.InternalTraceLevel = adapterSettings.InternalTraceLevelEnum;
    }

    public void InitializeSettingsAndLogging(IAdapterSettings setting, ITestLogger testLog)
    {
        logger = testLog;
        settings = setting;
    }

    public void SetDump(IDumpXml dumpXml)
    {
        dump = dumpXml;
    }

    public void CreateRunner(TestPackage testPackage)
    {
        LogToDump("EngineLog", "NUnitEngineAdapter.CreateRunner() - starting");
        package = testPackage;
        Runner = TestEngine.GetRunner(package);
        LogToDump("EngineLog", "NUnitEngineAdapter.CreateRunner() - completed");
    }

    public NUnitResults Explore()
        => Explore(TestFilter.Empty);

    public NUnitResults Explore(TestFilter filter)
    {
        LogToDump("EngineLog", "NUnitEngineAdapter.Explore() - starting");
        var timing = new TimingLogger(settings, logger);
        var results = new NUnitResults(Runner.Explore(filter));
        LogToDump("EngineLog", $"NUnitEngineAdapter.Explore() - completed, results: {(results.IsRunnable ? "runnable" : "not runnable")}");
        return LogTiming(filter, timing, results);
    }

    public NUnitResults Run(ITestEventListener listener, TestFilter filter)
    {
        LogToDump("EngineLog", "NUnitEngineAdapter.Run() - starting");
        var timing = new TimingLogger(settings, logger);
        var results = new NUnitResults(Runner.Run(listener, filter));
        LogToDump("EngineLog", "NUnitEngineAdapter.Run() - completed");
        return LogTiming(filter, timing, results);
    }

    private NUnitResults LogTiming(TestFilter filter, TimingLogger timing, NUnitResults results)
    {
        timing.LogTime($"Execution engine run time with filter length {filter.Text.Length}");
        if (filter.Text.Length < 300)
            logger?.Debug($"Filter: {filter.Text}");
        return results;
    }

    public T GetService<T>()
        where T : class
    {
        var service = TestEngine.Services.GetService<T>();
        if (service == null)
        {
            logger?.Warning($"Engine GetService can't create service {typeof(T)}.");
        }
        return service;
    }

    public void StopRun()
    {
        LogToDump("EngineLog", "NUnitEngineAdapter.StopRun() - starting");

        try
        {
            // Enhanced graceful-then-force pattern for better reliability
            var stopTask = Task.Run(() => Runner?.StopRun(true));
            var gracefulTimeout = TimeSpan.FromSeconds(5);

            if (stopTask.Wait(gracefulTimeout))
            {
                LogToDump("EngineLog", "NUnitEngineAdapter.StopRun() - completed gracefully");
            }
            else
            {
                LogToDump("EngineLog", $"NUnitEngineAdapter.StopRun() - TIMEOUT after {gracefulTimeout.TotalSeconds}s, proceeding with force cleanup", LogLevel.Warning);

                // Force cleanup - don't wait for hanging StopRun
                // This allows the adapter to continue with cleanup
            }
        }
        catch (Exception ex)
        {
            LogToDump("EngineLog", $"StopRun exception: {ex}", LogLevel.Warning);

            // Also log any inner exceptions separately for clarity
            if (ex.InnerException != null)
            {
                LogToDump("EngineLog", $"StopRun inner exception: {ex.InnerException}", LogLevel.Warning);
            }
        }
    }

    public void CloseRunner()
    {
        LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - starting");

        if (Runner == null)
        {
            LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - runner is null, returning");
            return;
        }

        try
        {
            // Enhanced close with graceful-then-force pattern
            if (Runner.IsTestRunning)
            {
                LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - test is running, stopping with timeout");

                // Try graceful stop with timeout
                var stopTask = Task.Run(() => Runner.StopRun(true));
                if (!stopTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - StopRun timeout during close", LogLevel.Warning);
                }
            }

            LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - unloading and disposing runner");

            // Unload with timeout
            var unloadTask = Task.Run(() => Runner.Unload());
            if (!unloadTask.Wait(TimeSpan.FromSeconds(5)))
            {
                LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - Unload timeout", LogLevel.Warning);
            }

            // Dispose with timeout
            var disposeTask = Task.Run(() => Runner.Dispose());
            if (!disposeTask.Wait(TimeSpan.FromSeconds(2)))
            {
                LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - Dispose timeout", LogLevel.Warning);
            }
        }
        catch (NUnitEngineUnloadException ex)
        {
            LogToDump("EngineLog", $"Engine encountered NUnitEngineUnloadException: {ex}", LogLevel.Warning);

            if (ex.InnerException != null)
            {
                LogToDump("EngineLog", $"NUnitEngineUnloadException inner exception: {ex.InnerException}", LogLevel.Warning);
            }
        }
        catch (Exception ex)
        {
            LogToDump("EngineLog", $"Unexpected exception during CloseRunner: {ex}", LogLevel.Warning);

            if (ex.InnerException != null)
            {
                LogToDump("EngineLog", $"CloseRunner inner exception: {ex.InnerException}", LogLevel.Warning);
            }
        }
        finally
        {
            Runner = null;
            LogToDump("EngineLog", "NUnitEngineAdapter.CloseRunner() - completed");
        }
    }

    public void Dispose()
    {
        CloseRunner();
        TestEngine?.Dispose();
    }

    /// <summary>
    /// Helper method to log with XML element formatting - similar to NUnit3TestExecutor.LogToDump.
    /// </summary>
    /// <param name="elementName">Name of the XML element.</param>
    /// <param name="message">Message to log.</param>
    /// <param name="logLevel">TestLog level - Debug, Info, Warning, or Error.</param>
    private void LogToDump(string elementName, string message, LogLevel logLevel = LogLevel.Debug)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"<{elementName}>{timestamp} - {message}</{elementName}>\n";

            // Log to TestLog based on specified level
            switch (logLevel)
            {
                case LogLevel.Debug:
                    logger?.Debug($"[{timestamp}] {elementName}: {message}");
                    break;
                case LogLevel.Info:
                    logger?.Info($"[{timestamp}] {elementName}: {message}");
                    break;
                case LogLevel.Warning:
                    logger?.Warning($"[{timestamp}] {elementName}: {message}");
                    break;
                case LogLevel.Error:
                    logger?.Error($"[{timestamp}] {elementName}: {message}");
                    break;
                default:
                    logger?.Debug($"[{timestamp}] {elementName}: {message}");
                    break;
            }

            // Add to dump
            dump?.AddString(logMessage);
        }
        catch (Exception ex)
        {
            // Fallback logging in case of issues with LogToDump itself
            logger?.Warning($"LogToDump failed for {elementName}: {ex.Message}");
        }
    }

    public void GenerateTestOutput(NUnitResults testResults, string assemblyPath, string testOutputXmlFolder)
    {
        if (!settings.UseTestOutputXml)
            return;

        var resultService = GetService<IResultService>();

        using Mutex mutex = new Mutex(false, string.IsNullOrWhiteSpace(assemblyPath) ? nameof(GenerateTestOutput) : Path.GetFileNameWithoutExtension(assemblyPath));
        bool received = false;
        try
        {
            received = mutex.WaitOne();
            string path = GetXmlFilePath(testOutputXmlFolder, GetTestOutputFileName(assemblyPath), "xml");

            // Following null argument should work for nunit3 format. Empty array is OK as well.
            // If you decide to handle other formats in the runsettings, it needs more work.
            var resultWriter = resultService.GetResultWriter("nunit3", null);
            resultWriter.WriteResultFile(testResults.FullTopNode, path);
            logger?.Info($"   Test results written to {path}");
        }
        finally
        {
            if (received)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    public string GetTestOutputFileName(string assemblyPath)
        => string.IsNullOrWhiteSpace(settings.TestOutputXmlFileName)
            ? Path.GetFileNameWithoutExtension(assemblyPath)
            : settings.TestOutputXmlFileName;

    public string GetXmlFilePath(string folder, string defaultFileName, string extension)
    {
        if (!settings.NewOutputXmlFileForEachRun)
        {
            // overwrite the existing file
            return Path.Combine(folder, $"{defaultFileName}.{extension}");
        }
        // allways create a new file
        int i = 1;
        while (true)
        {
            string path = Path.Combine(folder, $"{defaultFileName}.{i++}.{extension}");
            if (!System.IO.File.Exists(path))
                return path;
        }
    }
}