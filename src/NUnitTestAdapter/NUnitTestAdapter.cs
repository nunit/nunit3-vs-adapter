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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitTestAdapter is the common base for the
    /// NUnit discoverer and executor classes.
    /// </summary>
    public abstract class NUnitTestAdapter
    {
        #region Properties

        // The adapter version
        private string AdapterVersion { get; set; }

        // Verbosity in effect for message logging
        private int Verbosity { get; set; }

        // True if files should be shadow-copied
        private bool ShadowCopy { get; set; }

        protected ITestEngine TestEngine { get; private set; }

        // Our logger used to display messages
        protected TestLogger TestLog { get; private set; }

        protected bool UseVsKeepEngineRunning { get; private set; }

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
        protected virtual void Initialize(IMessageLogger messageLogger)
        {
            Verbosity = 0; // In case we throw below
            AdapterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            try
            {
                var registry = RegistryCurrentUser.OpenRegistryCurrentUser(@"Software\nunit.org\VSAdapter");
                UseVsKeepEngineRunning = registry.Exist("UseVsKeepEngineRunning") && (registry.Read<int>("UseVsKeepEngineRunning") == 1);
                ShadowCopy = registry.Exist("ShadowCopy") && (registry.Read<int>("ShadowCopy") == 1);
#if DEBUG && VERBOSE
                Verbosity = 1;
#else
                Verbosity = (registry.Exist("Verbosity")) ? registry.Read<int>("Verbosity") : 0;
#endif
            }
            catch (Exception e)
            {
                messageLogger.SendMessage(TestMessageLevel.Error, "Unable to access registry. Default settings will be used");
                messageLogger.SendMessage(TestMessageLevel.Error, e.ToString());
            }

            TestEngine = new TestEngineClass();
            TestLog = new TestLogger(messageLogger, Verbosity);
        }

        protected ITestRunner GetRunnerFor(string assemblyName)
        {
            var package = new TestPackage(assemblyName);

            if (ShadowCopy)
            {
                package.Settings["ShadowCopyFiles"] = "true";
                TestLog.SendDebugMessage("    Setting ShadowCopyFiles to true");
            }

            if (Debugger.IsAttached)
            {
                package.Settings["NumberOfTestWorkers"] = 0;
                TestLog.SendDebugMessage("    Setting NumberOfTestWorkers to zero");
            }

            // Lastest version defaults to a separate process
            package.Settings["ProcessModel"] = "InProcess";
            package.Settings["DomainUsage"] = "Single";

            // Set the work directory to the assembly location
            package.Settings["WorkDirectory"] = Path.GetDirectoryName(assemblyName);

            return TestEngine.GetRunner(package);
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

        private AppDomain _engineDomain;

        protected void Unload()
        {
            if (TestEngine != null)
            {
                TestEngine.Dispose();
                TestEngine = null;
            }
            if (_engineDomain != null)
            {
                AppDomain.Unload(_engineDomain);
                _engineDomain = null;
            }
        }

        #endregion
    }
}
