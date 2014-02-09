// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

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
        protected TestLogger testLog = new TestLogger();

        // The adapter version
        private string adapterVersion;

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

            adapterVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        #endregion

        #region Protected Helper Methods

        protected void Info(string method, string function)
        {
            var msg = string.Format("NUnit {0} {1} is {2}", adapterVersion, method, function);
            testLog.SendInformationalMessage(msg);
        }

        protected static void CleanUpRegisteredChannels()
        {
            foreach (IChannel chan in ChannelServices.RegisteredChannels)
                ChannelServices.UnregisterChannel(chan);
        }

        #endregion
    }
}
