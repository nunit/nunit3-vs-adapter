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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
#if NET462
using System.Runtime.Remoting.Channels;
#endif
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Common;
using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;


namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitTestAdapter is the common base for the
    /// NUnit discoverer and executor classes.
    /// </summary>
    public abstract class NUnitTestAdapter
    {
        #region Constants

        /// <summary>
        /// The Uri used to identify the NUnitExecutor.
        /// </summary>
        public const string ExecutorUri = "executor://NUnit3TestExecutor";

        public const string SettingsName = "NUnitAdapterSettings";

        #endregion

        #region Constructor

        protected NUnitTestAdapter()
        {
#if !NET462
            AdapterVersion = typeof(NUnitTestAdapter).GetTypeInfo().Assembly.GetName().Version.ToString();
#else
            AdapterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif
            NUnitEngineAdapter = new NUnitEngineAdapter();
        }

        #endregion

        #region Properties

        public IAdapterSettings Settings { get; private set; }

        // The adapter version
        protected string AdapterVersion { get; set; }

        private NUnitEngineAdapter nUnitEngineAdapter;

        public NUnitEngineAdapter NUnitEngineAdapter
        {
            get => nUnitEngineAdapter ??= new NUnitEngineAdapter();
            private set => nUnitEngineAdapter = value;
        }

        // Our logger used to display messages
        protected TestLogger TestLog { get; private set; }

        protected string WorkDir { get; private set; }

        private static string entryExeName;

        private static string whoIsCallingUsEntry;

        public static string WhoIsCallingUsEntry
        {
            get
            {
                if (whoIsCallingUsEntry != null)
                    return whoIsCallingUsEntry;
                if (entryExeName == null)
                {
                    var entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null)
                        entryExeName = entryAssembly.Location;
                }
                whoIsCallingUsEntry = entryExeName;
                return whoIsCallingUsEntry;
            }
        }

        public static bool IsRunningUnderIde
        {
            get
            {
                var exe = WhoIsCallingUsEntry;
                return exe != null && (
                    exe.Contains("vstest.executionengine") ||
                    exe.Contains("vstest.discoveryengine") ||
                    exe.Contains("TE.ProcessHost"));
            }
        }

        public List<string> ForbiddenFolders { get; private set; }

        #endregion

        #region Protected Helper Methods

        // The Adapter is constructed using the default constructor.
        // We don't have any info to initialize it until one of the
        // ITestDiscovery or ITestExecutor methods is called. Each
        // Discover or Execute method must call this method.
        protected void Initialize(IDiscoveryContext context, IMessageLogger messageLogger)
        {
            NUnitEngineAdapter.Initialize();
            TestLog = new TestLogger(messageLogger);
            Settings = new AdapterSettings(TestLog);
            NUnitEngineAdapter.InitializeSettingsAndLogging(Settings, TestLog);
            TestLog.InitSettings(Settings);
            try
            {
                Settings.Load(context, TestLog);
                TestLog.Verbosity = Settings.Verbosity;
                InitializeForbiddenFolders();
                SetCurrentWorkingDirectory();
            }
            catch (Exception e)
            {
                TestLog.Warning("Error initializing RunSettings. Default settings will be used");
                TestLog.Warning(e.ToString());
            }
            finally
            {
                TestLog.DebugRunfrom();
            }
        }

        public void InitializeForbiddenFolders()
        {
            ForbiddenFolders = new[]
            {
                Environment.GetEnvironmentVariable("ProgramW6432"),
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("windir"),
            }.Where(o => !string.IsNullOrEmpty(o)).Select(o => o.ToLower() + @"\").ToList();
        }

        private void SetCurrentWorkingDirectory()
        {
            string dir = Directory.GetCurrentDirectory();
            bool foundForbiddenFolder = CheckDirectory(dir);
            if (foundForbiddenFolder)
                Directory.SetCurrentDirectory(Path.GetTempPath());
        }


        /// <summary>
        /// If a directory matches one of the forbidden folders, then we should reroute, so we return true in that case.
        /// </summary>
        public bool CheckDirectory(string dir)
        {
            string checkDir = (dir.EndsWith("\\") ? dir : dir + "\\");
            return ForbiddenFolders.Any(o => checkDir.StartsWith(o, StringComparison.OrdinalIgnoreCase));
        }

        protected TestPackage CreateTestPackage(string assemblyName, IGrouping<string, TestCase> testCases)
        {
            var package = new TestPackage(assemblyName);

            if (Settings.ShadowCopyFiles)
            {
                package.Settings[PackageSettings.ShadowCopyFiles] = true;
                TestLog.Debug("    Setting ShadowCopyFiles to true");
            }

            if (Debugger.IsAttached && !Settings.AllowParallelWithDebugger)
            {
                package.Settings[PackageSettings.NumberOfTestWorkers] = 0;
                TestLog.Debug("    Setting NumberOfTestWorkers to zero for Debugging");
            }
            else
            {
                int workers = Settings.NumberOfTestWorkers;
                if (workers >= 0)
                    package.Settings[PackageSettings.NumberOfTestWorkers] = workers;
            }

            if (Settings.PreFilter && testCases != null)
            {
                var prefilters = new List<string>();

                foreach (var testCase in testCases)
                {
                    int end = testCase.FullyQualifiedName.IndexOfAny(new[] { '(', '<' });
                    prefilters.Add(
                        end > 0 ? testCase.FullyQualifiedName.Substring(0, end) : testCase.FullyQualifiedName);
                }

                package.Settings[PackageSettings.LOAD] = prefilters;
            }

            package.Settings[PackageSettings.SynchronousEvents] = Settings.SynchronousEvents;

            int timeout = Settings.DefaultTimeout;
            if (timeout > 0)
                package.Settings[PackageSettings.DefaultTimeout] = timeout;

            package.Settings[PackageSettings.InternalTraceLevel] = Settings.InternalTraceLevelEnum.ToString();

            if (Settings.BasePath != null)
                package.Settings[PackageSettings.BasePath] = Settings.BasePath;

            if (Settings.PrivateBinPath != null)
                package.Settings[PackageSettings.PrivateBinPath] = Settings.PrivateBinPath;

            if (Settings.RandomSeed.HasValue)
                package.Settings[PackageSettings.RandomSeed] = Settings.RandomSeed;

            if (Settings.TestProperties.Count > 0)
                SetTestParameters(package.Settings, Settings.TestProperties);

            if (Settings.StopOnError)
                package.Settings[PackageSettings.StopOnError] = true;

            if (Settings.SkipNonTestAssemblies)
                package.Settings[PackageSettings.SkipNonTestAssemblies] = true;

            // Always run one assembly at a time in process in its own domain
            package.Settings[PackageSettings.ProcessModel] = "InProcess";

            package.Settings[PackageSettings.DomainUsage] = Settings.DomainUsage ?? "Single";

            if (Settings.DefaultTestNamePattern != null)
            {
                package.Settings[PackageSettings.DefaultTestNamePattern] = Settings.DefaultTestNamePattern;
            }
            else
            {
                // Force truncation of string arguments to test cases
                package.Settings[PackageSettings.DefaultTestNamePattern] = "{m}{a}";
            }

            return SetWorkDir(assemblyName, package);
        }

        private TestPackage SetWorkDir(string assemblyName, TestPackage package)
        {
            // Set the work directory to the assembly location unless a setting is provided
            string workDir = Settings.WorkDirectory;
            if (workDir == null)
                workDir = Path.GetDirectoryName(assemblyName);
            else if (!Path.IsPathRooted(workDir))
                workDir = Path.Combine(Path.GetDirectoryName(assemblyName), workDir);
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);
            package.Settings[PackageSettings.WorkDirectory] = workDir;
            WorkDir = workDir;
            Directory.SetCurrentDirectory(workDir);
            TestLog.Debug($"Workdir set to: {WorkDir}");
            return package;
        }

        /// <summary>
        /// Sets test parameters, handling backwards compatibility.
        /// </summary>
        private static void SetTestParameters(
            IDictionary<string, object> runSettings,
            IDictionary<string, string> testParameters)
        {
            runSettings[PackageSettings.TestParametersDictionary] = testParameters;

            if (testParameters.Count == 0)
                return;
            // Kept for backwards compatibility with old frameworks.
            // Reserializes the way old frameworks understand, even if the parsing above is changed.
            // This reserialization cannot be changed without breaking compatibility with old frameworks.

            var oldFrameworkSerializedParameters = new StringBuilder();
            foreach (var parameter in testParameters)
                oldFrameworkSerializedParameters.Append(parameter.Key).Append('=').Append(parameter.Value).Append(';');

            runSettings[PackageSettings.TestParameters] =
                oldFrameworkSerializedParameters.ToString(0, oldFrameworkSerializedParameters.Length - 1);
        }

        /// <summary>
        /// Ensure any channels registered by other adapters are unregistered.
        /// </summary>
        protected static void CleanUpRegisteredChannels()
        {
#if NET462
            foreach (var chan in ChannelServices.RegisteredChannels)
                ChannelServices.UnregisterChannel(chan);
#endif
        }

        protected void Unload()
        {
            if (NUnitEngineAdapter == null)
                return;
            NUnitEngineAdapter.Dispose();
            NUnitEngineAdapter = null;
        }

        #endregion
    }
}