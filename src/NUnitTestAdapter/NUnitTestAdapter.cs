// ***********************************************************************
// Copyright (c) 2011-2015 Charlie Poole
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
using System.Text;
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
        }

        #endregion

        #region Properties

        public AdapterSettings Settings { get; private set; }

        // The adapter version
        protected string AdapterVersion { get; set; }

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
        // ITestDiscovery or ITestExecutor methods is called. Each 
        // Discover or Execute method must call this method.
        protected void Initialize(IDiscoveryContext context, IMessageLogger messageLogger)
        {
            TestEngine = new TestEngineClass();
            TestLog = new TestLogger(messageLogger);
            Settings = new AdapterSettings(TestLog);

            try
            {
                Settings.Load(context);
                TestLog.Verbosity = Settings.Verbosity;
            }
            catch (Exception e)
            {
                TestLog.Warning("Error initializing RunSettings. Default settings will be used");
                TestLog.Warning(e.ToString());
            }
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
                TestLog.Error("Error: Unable to get runner for this assembly. Check installation, including any extensions.");
                TestLog.Error(ex.GetType().Name + ": " + ex.Message);
                throw;
            }
        }

        private TestPackage CreateTestPackage(string assemblyName)
        {
            var package = new TestPackage(assemblyName);

            if (Settings.ShadowCopyFiles)
            {
                package.Settings[PackageSettings.ShadowCopyFiles] = "true";
                TestLog.Debug("    Setting ShadowCopyFiles to true");
            }

            if (Debugger.IsAttached)
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

            package.Settings[PackageSettings.SynchronousEvents] = Settings.SynchronousEvents;

            int timeout = Settings.DefaultTimeout;
            if (timeout > 0)
                package.Settings[PackageSettings.DefaultTimeout] = timeout;

            if (Settings.InternalTraceLevel != null)
                package.Settings[PackageSettings.InternalTraceLevel] = Settings.InternalTraceLevel;

            if (Settings.BasePath != null)
                package.Settings[PackageSettings.BasePath] = Settings.BasePath;

            if (Settings.PrivateBinPath != null)
                package.Settings[PackageSettings.PrivateBinPath] = Settings.PrivateBinPath;

            if (Settings.RandomSeed.HasValue)
                package.Settings[PackageSettings.RandomSeed] = Settings.RandomSeed;

            if (Settings.TestProperties.Count > 0)
            {
                var sb = new StringBuilder();
                var index = 0;
                foreach(string name in Settings.TestProperties.Keys)
                {
                    if (index++ > 0) sb.Append(";");
                    sb.AppendFormat("{0}={1}", name, Settings.TestProperties[name]);
                }

                package.Settings[PackageSettings.TestParameters] = sb.ToString();
            }

            // Always run one assembly at a time in process in it's own domain
            package.Settings[PackageSettings.ProcessModel] = "InProcess";

            if (Settings.DomainUsage != null)
            {
                package.Settings[PackageSettings.DomainUsage] = Settings.DomainUsage;
            }
            else
            {
                package.Settings[PackageSettings.DomainUsage] = "Single";
            }

            // Force truncation of string arguments to test cases
            package.Settings[PackageSettings.DefaultTestNamePattern] = "{m}{a:40}";

            // Set the work directory to the assembly location unless a setting is provided
            var workDir = Settings.WorkDirectory;
            if (workDir == null)
                workDir = Path.GetDirectoryName(assemblyName);
            else if (!Path.IsPathRooted(workDir))
                workDir = Path.Combine(Path.GetDirectoryName(assemblyName), workDir);
            if (!Directory.Exists(workDir))
                Directory.CreateDirectory(workDir);
            package.Settings[PackageSettings.WorkDirectory] = workDir;

            return package;
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
