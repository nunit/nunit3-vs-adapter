// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using NUnit.Util;
using AssemblyHelper = Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.AssemblyHelper;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitTestAdapter is the common base for the
    /// NUnit discoverer and executor classes.
    /// </summary>
    public abstract class NUnitTestAdapter
    {
        // The logger currently in use
        protected IMessageLogger Logger { get; set; }

        #region Constructor

        /// <summary>
        /// The common constructor initializes NUnit services 
        /// needed to load and run tests.
        /// </summary>
        protected NUnitTestAdapter()
        {
            ServiceManager.Services.AddService(new DomainManager());
            ServiceManager.Services.AddService(new ProjectService());

            ServiceManager.Services.InitializeServices();
        }

        #endregion

        #region Helper Methods

        
        protected static void CleanUpRegisteredChannels()
        {
            foreach (IChannel chan in ChannelServices.RegisteredChannels)
                ChannelServices.UnregisterChannel(chan);
        }

        protected void AssemblyNotSupportedWarning(string sourceAssembly)
        {
            SendWarningMessage("Assembly not supported: " + sourceAssembly);
        }

        protected void DependentAssemblyNotFoundWarning(string dependentAssembly, string sourceAssembly)
        {
            SendWarningMessage("Dependent Assembly "+dependentAssembly+" of " + sourceAssembly + " not found. Can be ignored if not a NUnit project.");
        }

        protected void NUnitLoadError(string sourceAssembly)
        {
            SendErrorMessage("NUnit failed to load " + sourceAssembly);
        }

        protected void SendErrorMessage(string message)
        {
            this.Logger.SendMessage(TestMessageLevel.Error, message);
        }

        protected void SendErrorMessage(string message, Exception ex)
        {
            this.Logger.SendMessage(TestMessageLevel.Error, message);
            this.Logger.SendMessage(TestMessageLevel.Error, ex.ToString());
        }

        protected void SendWarningMessage(string message)
        {
            this.Logger.SendMessage(TestMessageLevel.Warning, message);
        }

        protected void SendInformationalMessage(string message)
        {
            this.Logger.SendMessage(TestMessageLevel.Informational, message);
        }

        #endregion

       
    }
}
