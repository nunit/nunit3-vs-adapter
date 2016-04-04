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
        private XmlNode _runConfiguration;
        private XmlNode _testRunParameters;
        private XmlNode _nunitAdapter;

        #region Properties

        public string MaxCpuCount
        {
            get { return GetInnerText(_runConfiguration, "MaxCpuCount"); }
        }

        public string ResultsDirectory
        {
            get { return GetInnerText(_runConfiguration, "ResultsDirectory"); }
        }

        public string TargetPlatform
        {
            get { return GetInnerText(_runConfiguration, "TargetPlatform"); }
        }

        public string TargetFrameworkVersion
        {
            get { return GetInnerText(_runConfiguration, "TargetFrameworkVersion"); }
        }

        public string TestAdapterPaths
        {
            get { return GetInnerText(_runConfiguration, "TestAdapterPaths"); }
        }

        private Dictionary<string, string> _testProperties;
        public IDictionary<string, string> TestProperties
        {
            get
            {
                if (_testProperties == null)
                {
                    _testProperties = new Dictionary<string, string>();

                    if (_testRunParameters != null)
                    {
                        foreach (XmlNode node in _testRunParameters.SelectNodes("Property"))
                        {
                            var key = node.GetAttribute("name");
                            var value = node.GetAttribute("value");
                            if (key != null && value != null)
                                _testProperties.Add(key, value);
                        }
                    }
                }

                return _testProperties;
            }
        }

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
            _runConfiguration = doc.SelectSingleNode("RunSettings/RunConfiguration");
            _testRunParameters = doc.SelectSingleNode("RunSettings/TestRunParameters");
            _nunitAdapter = doc.SelectSingleNode("RunSettings/NUnit");
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

        #endregion
    }
}
