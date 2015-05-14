// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using System;
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
        // The adapter version
        private string _adapterVersion;

        // Verbosity in effect for message logging
        private int _verbosity;

        // True if files should be shadow-copied
        private bool _shadowCopy;

        #region Properties

        protected TestEngine TestEngine { get; private set; }

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

                return exeName == "vstest.executionengine.exe" || exeName == "vstest.discoveryengine.exe";
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
            _verbosity = 0; // In case we throw below
            _adapterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            try
            {
                var registry = RegistryCurrentUser.OpenRegistryCurrentUser(@"Software\nunit.org\VSAdapter");
                UseVsKeepEngineRunning = registry.Exist("UseVsKeepEngineRunning") && (registry.Read<int>("UseVsKeepEngineRunning") == 1);
                _shadowCopy = registry.Exist("ShadowCopy") && (registry.Read<int>("ShadowCopy") == 1);
                _verbosity = (registry.Exist("Verbosity")) ? registry.Read<int>("Verbosity") : 0;
            }
            catch (Exception e)
            {
                messageLogger.SendMessage(TestMessageLevel.Error, e.ToString());
                // TODO: Shouldn't we rethrow here?
            }

            TestEngine = new TestEngine();
            TestLog = new TestLogger(messageLogger, _verbosity);
        }

        protected ITestRunner GetRunnerFor(string assemblyName)
        {
            var package = new TestPackage(assemblyName);
            if (_shadowCopy)
                package.Settings["ShadowCopyFiles"] = "true";

            return TestEngine.GetRunner(package);
        }

        protected void Info(string method, string function)
        {
            var msg = string.Format("NUnit Adapter {0} {1} is {2}", _adapterVersion, method, function);
            TestLog.SendInformationalMessage(msg);
        }

        protected void Debug(string method, string function)
        {
#if DEBUG
            var msg = string.Format("NUnit Adapter {0} {1} is {2}", _adapterVersion, method, function);
            TestLog.SendDebugMessage(msg);
#endif
        }

        protected static void CleanUpRegisteredChannels()
        {
            foreach (IChannel chan in ChannelServices.RegisteredChannels)
                ChannelServices.UnregisterChannel(chan);
        }

        #endregion
    }
}
