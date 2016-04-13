using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter
{
    public class AdapterSettings
    {
        #region Properties - General

        public int MaxCpuCount { get; private set; }

        public string ResultsDirectory { get; private set; }

        public string TargetPlatform { get; private set; }

        public string TargetFrameworkVersion { get; private set; }

        public string TestAdapterPaths { get; private set; }

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

        public int RandomSeed { get; private set; }

        #endregion

        #region Public Methods

        public void Load(IDiscoveryContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context", "Load called with null context");

            Load(context.RunSettings.SettingsXml);
        }

        public void Load(string settingsXml)
        {
            if (settingsXml == null)
                throw new ArgumentNullException("settingsXml", "Load called with null XML string");
            if (settingsXml == string.Empty)
                throw new ArgumentException("settingsXml", "Load called with empty XML string");

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
            ShadowCopyFiles = GetInnerTextAsBool(nunitNode, "ShadowCopyFiles");
            Verbosity = GetInnerTextAsInt(nunitNode, "Verbosity", 0);
            UseVsKeepEngineRunning = GetInnerTextAsBool(nunitNode, "UseVsKeepEngineRunning");
            BasePath = GetInnerText(nunitNode, "BasePath");
            PrivateBinPath = GetInnerText(nunitNode, "PrivateBinPath");
            RandomSeed = GetInnerTextAsInt(nunitNode, "RandomSeed", -1);
            
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
            string temp = GetInnerText(startNode, xpath);

            if (string.IsNullOrEmpty(temp))
                return defaultValue;

            return int.Parse(temp);
        }

        private bool GetInnerTextAsBool(XmlNode startNode, string xpath)
        {
            string temp = GetInnerText(startNode, xpath);

            if (string.IsNullOrEmpty(temp))
                return false;

            return bool.Parse(temp);
        }

        #endregion
    }
}
