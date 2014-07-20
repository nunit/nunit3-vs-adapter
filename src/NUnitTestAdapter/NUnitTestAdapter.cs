// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using NUnit.Util;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitTestAdapter is the common base for the
    /// NUnit discoverer and executor classes.
    /// </summary>
    public abstract class NUnitTestAdapter
    {
        // Our logger used to display messages
        protected TestLogger TestLog;
        // The adapter version
        private readonly string adapterVersion;

        protected bool UseVsKeepEngineRunning { get; private set; }
        protected bool UseShallowCopy { get; private set; }

        protected int Verbosity { get; private set; }


        protected bool RegistryFailure { get; set; }
        protected string ErrorMsg
        {
            get; set;
        }

        #region Constructor

        /// <summary>
        /// The common constructor initializes NUnit services 
        /// needed to load and run tests and sets some properties.
        /// </summary>
        protected NUnitTestAdapter()
        {
            ServiceManager.Services.AddService(new DomainManager());
            ServiceManager.Services.AddService(new ProjectService());

            ServiceManager.Services.InitializeServices();
            Verbosity = 0;
            RegistryFailure = false;
            adapterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            try
            {
                var registry = RegistryCurrentUser.OpenRegistryCurrentUser(@"Software\nunit.org\VSAdapter");
                UseVsKeepEngineRunning = registry.Exist("UseVsKeepEngineRunning") && (registry.Read<int>("UseVsKeepEngineRunning") == 1);
                UseShallowCopy = registry.Exist("UseShallowCopy") && (registry.Read<int>("UseShallowCopy") == 1);
                Verbosity = (registry.Exist("Verbosity")) ? registry.Read<int>("Verbosity") : 0;
            }
            catch (Exception e)
            {
                RegistryFailure = true;
                ErrorMsg = e.ToString();
            }

            TestLog = new TestLogger(Verbosity);
        }

        #endregion

        #region Protected Helper Methods

        protected void Info(string method, string function)
        {
            var msg = string.Format("NUnit {0} {1} is {2}", adapterVersion, method, function);
            TestLog.SendInformationalMessage(msg);
        }

        protected void Debug(string method, string function)
        {
#if DEBUG
            var msg = string.Format("NUnit {0} {1} is {2}", adapterVersion, method, function);
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
