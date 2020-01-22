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

    public class NUnitTestCase
    {
        public XmlNode Node { get; }

        public bool IsNull => Node == null;

        public bool IsTestCase => !IsNull && Node.Name == "test-case";

        public bool IsParameterizedMethod => Type == "ParameterizedMethod";

        public string Id => Node.GetAttribute("id");

        public string FullName => Node.GetAttribute("fullname");

        public string Type => Node.GetAttribute("type");

        public string Name => Node.GetAttribute("name");

        public string ClassName => Node.GetAttribute("classname");

        public string MethodName => Node.GetAttribute("methodname");

        public NUnitTestCase(XmlNode testCase)
        {
            Node = testCase;
        }

        public NUnitTestCase Parent() => new NUnitTestCase(Node.ParentNode);
    }
}