using System.Linq;
using System.Xml;
using NUnit.VisualStudio.TestAdapter.Dump;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public class NUnitResults
    {
        public enum SkipReason
        {
            NoNUnitTests,
            LoadFailure
        }


        public XmlNode TopNode { get; }

        public bool IsRunnable { get; }

        public string AsString() => FullTopNode.AsString();

        private XmlNode FullTopNode { get; }
        public NUnitResults(XmlNode results)
        {
            FullTopNode = results;
            // Currently, this will always be the case but it might change
            TopNode = results.Name == "test-run" ? results.FirstChild : results;
            // ReSharper disable once StringLiteralTypo
            IsRunnable = TopNode.GetAttribute("runstate") == "Runnable";
        }


        public SkipReason WhatSkipReason()
        {
            var msgNode = TopNode.SelectSingleNode("properties/property[@name='_SKIPREASON']");
            if (msgNode != null &&
                new[] { "contains no tests", "Has no TestFixtures" }.Any(msgNode.GetAttribute("value")
                    .Contains))
                return SkipReason.NoNUnitTests;
            return SkipReason.LoadFailure;
        }

        public bool HasNoNUnitTests => WhatSkipReason() == SkipReason.NoNUnitTests;

        public XmlNodeList TestCases()
        {
            return TopNode.SelectNodes("//test-case");
        }
    }
}