// ***********************************************************************
// Copyright (c) 2014-2017 Charlie Poole, Terje Sandstrom
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
using System.IO;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter
{
    public class AdapterSettings
    {
        private const string RANDOM_SEED_FILE = "nunit_random_seed.tmp";
        private TestLogger _logger;

        #region Constructor

        public AdapterSettings(TestLogger logger)
        {
            _logger = logger;
        }

        #endregion

        #region Properties - General

        public int MaxCpuCount { get; private set; }

        public string ResultsDirectory { get; private set; }

        public string TargetPlatform { get; private set; }

        public string TargetFrameworkVersion { get; private set; }

        public string TestAdapterPaths { get; private set; }

        public bool CollectSourceInformation { get; private set; }

        public bool DisableAppDomain { get; private set; }

        #endregion

        #region Properties - TestRunParameters

        public IDictionary<string, string> TestProperties { get; private set; }

        #endregion

        #region Properties - NUnit Specific

        public string InternalTraceLevel { get; private set; }

        public string WorkDirectory { get; private set; }

        public int DefaultTimeout { get; private set; }

        public int NumberOfTestWorkers { get; private set; }

        public bool ShadowCopyFiles { get; private set; }

        public int Verbosity { get; private set; }

        public bool UseVsKeepEngineRunning { get; private set; }

        public string BasePath { get; private set; }

        public string PrivateBinPath { get; private set; }

        public int? RandomSeed { get; private set; }
        public bool RandomSeedSpecified { get; private set; }

        public bool InProcDataCollectorsAvailable { get; private set; }

        public bool SynchronousEvents { get; private set; }

        public string DomainUsage { get; set; }

        /// <summary>
        ///  Syntax documentation <see cref="https://github.com/nunit/docs/wiki/Template-Based-Test-Naming"/>
        /// </summary>
        public string DefaultTestNamePattern { get; set; }

        #endregion

        #region Public Methods

        public void Load(IDiscoveryContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context", "Load called with null context");

            Load(context?.RunSettings?.SettingsXml);
        }

        public void Load(string settingsXml)
        {
            if (string.IsNullOrEmpty(settingsXml))
                settingsXml = "<RunSettings />";

            // Visual Studio already gives a good error message if the .runsettings
            // file is poorly formed, so we don't need to do anything more.
            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);

            var runConfiguration = doc.SelectSingleNode("RunSettings/RunConfiguration");
            MaxCpuCount = GetInnerTextAsInt(runConfiguration, "MaxCpuCount", -1);
            ResultsDirectory = GetInnerText(runConfiguration, "ResultsDirectory");
            TargetPlatform = GetInnerText(runConfiguration, "TargetPlatform");
            TargetFrameworkVersion = GetInnerText(runConfiguration, "TargetFrameworkVersion");
            TestAdapterPaths = GetInnerText(runConfiguration, "TestAdapterPaths");
            CollectSourceInformation = GetInnerTextAsBool(runConfiguration, "CollectSourceInformation", true);
            DisableAppDomain = GetInnerTextAsBool(runConfiguration, "DisableAppDomain", false);

            TestProperties = new Dictionary<string, string>();
            foreach (XmlNode node in doc.SelectNodes("RunSettings/TestRunParameters/Parameter"))
            {
                var key = node.GetAttribute("name");
                var value = node.GetAttribute("value");
                if (key != null && value != null)
                    TestProperties.Add(key, value);
            }

            var nunitNode = doc.SelectSingleNode("RunSettings/NUnit");
            InternalTraceLevel = GetInnerText(nunitNode, "InternalTraceLevel", "Off", "Error", "Warning", "Info", "Verbose", "Debug");
            WorkDirectory = GetInnerText(nunitNode, "WorkDirectory");
            DefaultTimeout = GetInnerTextAsInt(nunitNode, "DefaultTimeout", 0);
            NumberOfTestWorkers = GetInnerTextAsInt(nunitNode, "NumberOfTestWorkers", -1);
            ShadowCopyFiles = GetInnerTextAsBool(nunitNode, "ShadowCopyFiles", false);
            Verbosity = GetInnerTextAsInt(nunitNode, "Verbosity", 0);
            UseVsKeepEngineRunning = GetInnerTextAsBool(nunitNode, "UseVsKeepEngineRunning", false);
            BasePath = GetInnerText(nunitNode, "BasePath");
            PrivateBinPath = GetInnerText(nunitNode, "PrivateBinPath");
            RandomSeed = GetInnerTextAsNullableInt(nunitNode, "RandomSeed");
            RandomSeedSpecified = RandomSeed.HasValue;
            if (!RandomSeedSpecified)
                RandomSeed = new Random().Next();
            DefaultTestNamePattern = GetInnerText(nunitNode,"DefaultTestNamePattern");
#if SUPPORT_REGISTRY_SETTINGS
            // Legacy (CTP) registry settings override defaults
            var registry = RegistryCurrentUser.OpenRegistryCurrentUser(@"Software\nunit.org\VSAdapter");
            if (registry.Exist("ShadowCopy") && (registry.Read<int>("ShadowCopy") == 1))
                ShadowCopyFiles = true;
            if (registry.Exist("Verbosity"))
                Verbosity = registry.Read<int>("Verbosity");
            if (registry.Exist("UseVsKeepEngineRunning") && (registry.Read<int>("UseVsKeepEngineRunning") == 1)
                UseVsKeepEngineRunning = true;
#endif

#if DEBUG && VERBOSE
            // Force Verbosity to 1 under Debug
            Verbosity = 1;
#endif

            // If any in proc data collector will be instantiated by the TestPlatform run tests sequentially.
            var inProcDataCollectorNode = doc.SelectSingleNode("RunSettings/InProcDataCollectionRunSettings/InProcDataCollectors");
            InProcDataCollectorsAvailable = inProcDataCollectorNode != null && inProcDataCollectorNode.SelectNodes("InProcDataCollector").Count > 0;
            if (InProcDataCollectorsAvailable)
            {
                NumberOfTestWorkers = 0;
                DomainUsage = "None";
                SynchronousEvents = true;
            }

            // If DisableAppDomain settings is passed from the testplatform, set the DomainUsage to None.
            if(DisableAppDomain)
            {
                DomainUsage = "None";
            }
        }

        public void SaveRandomSeed(string dirname)
        {
            try
            {
                var path = Path.Combine(dirname, RANDOM_SEED_FILE);
                File.WriteAllText(path, RandomSeed.Value.ToString());
            }
            catch (Exception ex)
            {
                _logger.Warning("Failed to save random seed.", ex);
            }
        }

        public void RestoreRandomSeed(string dirname)
        {
            var fullpath = Path.Combine(dirname, RANDOM_SEED_FILE);
            if (!File.Exists(fullpath))
                return;
            try
            {
                string value = File.ReadAllText(fullpath);
                RandomSeed = int.Parse(value);
            }
            catch (Exception ex)
            {
                _logger.Warning("Unable to restore random seed.", ex);
            }
        }

        #endregion

        #region Helper Methods

        private string GetInnerText(XmlNode startNode, string xpath, params string[] validValues)
        {
            if (startNode != null)
            {
                var targetNode = startNode.SelectSingleNode(xpath);
                if (targetNode != null)
                {
                    string val = targetNode.InnerText;

                    if (validValues != null && validValues.Length > 0)
                    {
                        foreach (string valid in validValues)
                            if (string.Compare(valid, val, StringComparison.OrdinalIgnoreCase) == 0)
                                return valid;

                        throw new ArgumentException(string.Format(
                            "Invalid value {0} passed for element {1}.", val, xpath));
                    }

                    return val;
                }
            }

            return null;
        }

        private int GetInnerTextAsInt(XmlNode startNode, string xpath, int defaultValue)
        {
            int? temp = GetInnerTextAsNullableInt(startNode, xpath);

            if (temp == null)
                return defaultValue;

            return temp.Value;
        }

        private int? GetInnerTextAsNullableInt(XmlNode startNode, string xpath)
        {
            string temp = GetInnerText(startNode, xpath);

            if (string.IsNullOrEmpty(temp))
                return null;

            return int.Parse(temp);
        }

        private bool GetInnerTextAsBool(XmlNode startNode, string xpath, bool defaultValue)
        {
            string temp = GetInnerText(startNode, xpath);

            if (string.IsNullOrEmpty(temp))
                return defaultValue;

            return bool.Parse(temp);
        }

        #endregion
    }
}
