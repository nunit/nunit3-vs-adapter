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

        public string MaxCpuCount { get; private set; }

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

            var doc = new XmlDocument();
            doc.LoadXml(settingsXml);

            var runConfiguration = doc.SelectSingleNode("RunSettings/RunConfiguration");
            MaxCpuCount = GetInnerText(runConfiguration, "MaxCpuCount");
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
            InternalTraceLevel = GetInnerText(nunitNode, "InternalTraceLevel");
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

        private string GetInnerText(XmlNode startNode, string xpath)
        {
            if (startNode != null)
            {
                var targetNode = startNode.SelectSingleNode(xpath);
                if (targetNode != null)
                    return targetNode.InnerText;
            }

            return null;
        }

        private int GetInnerTextAsInt(XmlNode startNode, string xpath, int defaultValue)
        {
            string temp = GetInnerText(startNode, xpath);

            int result;
            if (!string.IsNullOrEmpty(temp) && int.TryParse(temp, out result))
                    return result;

            return defaultValue;
        }

        private bool GetInnerTextAsBool(XmlNode startNode, string xpath)
        {
            string temp = GetInnerText(startNode, xpath);

            bool result;
            if (!String.IsNullOrEmpty(temp) && bool.TryParse(temp, out result))
                return result;

            return false;
        }

        #endregion
    }
}
