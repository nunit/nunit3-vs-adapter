// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

//#define VERBOSE

// We use an alias so that we don't accidentally make
// references to engine internals, except for creating
// the engine object in the Initialize method.
extern alias ENG;
using TestEngineClass = ENG::NUnit.Engine.TestEngine;

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Common;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitTestAdapter is the common base for the
    /// NUnit discoverer and executor classes.
    /// </summary>
    public abstract class NUnitTestAdapter
    {
        #region Constants

        ///<summary>
        /// The Uri used to identify the NUnitExecutor
        ///</summary>
        public const string ExecutorUri = "executor://NUnit3TestExecutor";

        public const string SettingsName = "NUnitAdapterSettings";

        #endregion

        #region Constructor

        public NUnitTestAdapter()
        {
            AdapterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Settings = new AdapterSettings();
        }

        #endregion

        #region Properties

        public AdapterSettings Settings { get; private set; }

        // The adapter version
        private string AdapterVersion { get; set; }

        protected ITestEngine TestEngine { get; private set; }

        // Our logger used to display messages
        protected TestLogger TestLog { get; private set; }

        private static string exeName;

        public static bool IsRunningUnderIDE
        {
            get
            {
                if (exeName == null)
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null)
                        exeName = entryAssembly.Location;

                }

                return exeName != null && (
                       exeName.Contains("vstest.executionengine") ||
                       exeName.Contains("vstest.discoveryengine") ||
                       exeName.Contains("TE.ProcessHost"));
            }
        }

        #endregion

        #region Protected Helper Methods

        // The Adapter is constructed using the default constructor.
        // We don't have any info to initialize it until one of the
        // ITestDiscovery or ITestExecutor methods is called. The
        // each Discover or Execute method must call this method.
        protected virtual void Initialize(IDiscoveryContext context, IMessageLogger messageLogger)
        {
            try
            {
                Settings.Load(context);
            }
            catch (Exception e)
            {
                messageLogger.SendMessage(TestMessageLevel.Error, "Error initializing RunSettings. Default settings will be used");
                messageLogger.SendMessage(TestMessageLevel.Error, e.ToString());
            }

            TestEngine = new TestEngineClass();
            TestLog = new TestLogger(messageLogger, Settings.Verbosity);
        }

        protected ITestRunner GetRunnerFor(string assemblyName)
        {
            var package = CreateTestPackage(assemblyName);

            try
            {
                return TestEngine.GetRunner(package);
            }
            catch(Exception ex)
            {
                TestLog.SendErrorMessage("Error: Unable to get runner for this assembly. Check installation, including any extensions.");
                TestLog.SendErrorMessage(ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        private TestPackage CreateTestPackage(string assemblyName)
        {
            var package = new TestPackage(assemblyName);

            if (Settings.ShadowCopyFiles)
            {
                package.Settings[PackageSettings.ShadowCopyFiles] = "true";
                TestLog.SendDebugMessage("    Setting ShadowCopyFiles to true");
            }

            if (Debugger.IsAttached)
            {
                package.Settings[PackageSettings.NumberOfTestWorkers] = 0;
                TestLog.SendDebugMessage("    Setting NumberOfTestWorkers to zero for Debugging");
            }
            else
            {
                int workers = Settings.NumberOfTestWorkers;
                if (workers >= 0)
                    package.Settings[PackageSettings.NumberOfTestWorkers] = workers;
            }

            int timeout = Settings.DefaultTimeout;
            if (timeout > 0)
                package.Settings[PackageSettings.DefaultTimeout] = timeout;

            if (Settings.InternalTraceLevel != null)
                package.Settings[PackageSettings.InternalTraceLevel] = Settings.InternalTraceLevel;

            if (Settings.BasePath != null)
                package.Settings[PackageSettings.BasePath] = Settings.BasePath;

            if (Settings.PrivateBinPath != null)
                package.Settings[PackageSettings.PrivateBinPath] = Settings.PrivateBinPath;

            if (Settings.RandomSeed != -1)
                package.Settings[PackageSettings.RandomSeed] = Settings.RandomSeed;

            // Always run one assembly at a time in process in it's own domain
            package.Settings[PackageSettings.ProcessModel] = "InProcess";
            package.Settings[PackageSettings.DomainUsage] = "Single";

            // Set the work directory to the assembly location unless a setting is provided
            var workDir = Settings.WorkDirectory;
            if (workDir == null)
                workDir = Path.GetDirectoryName(assemblyName);
            else if (!Path.IsPathRooted(workDir))
                workDir = Path.Combine(Path.GetDirectoryName(assemblyName), workDir);
            package.Settings[PackageSettings.WorkDirectory] = workDir;

            return package;
        }

        protected void Info(string method, string function)
        {
            var msg = string.Format("NUnit Adapter {0} {1} is {2}", AdapterVersion, method, function);
            TestLog.SendInformationalMessage(msg);
        }

        protected void Debug(string method, string function)
        {
#if DEBUG
            var msg = string.Format("NUnit Adapter {0} {1} is {2}", AdapterVersion, method, function);
            TestLog.SendDebugMessage(msg);
#endif
        }

        protected static void CleanUpRegisteredChannels()
        {
            foreach (IChannel chan in ChannelServices.RegisteredChannels)
                ChannelServices.UnregisterChannel(chan);
        }

        protected void Unload()
        {
            if (TestEngine != null)
            {
                TestEngine.Dispose();
                TestEngine = null;
            }
        }

        #endregion
    }
}
