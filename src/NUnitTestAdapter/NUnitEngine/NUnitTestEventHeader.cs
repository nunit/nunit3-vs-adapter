using System.Xml;
using NUnit.VisualStudio.TestAdapter.Dump;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public class NUnitTestEventHeader : INUnitTestEvent
    {
        public enum EventType
        {
            NoIdea,
            StartTest,  // Match: A test was started
            TestCase,   // Match: A test was finished
            TestSuite,  // Match: A suite was finished
            TestOutput, // Match: Test output, not part of test results, but should be added to it
            StartRun,  // Currently not used
            StartSuite // Currently not used
        }
        public XmlNode Node { get; }
        public string AsString() => Node.AsString();
        public string FullName => Node.GetAttribute("fullname");
        public string Name => Node.GetAttribute("name");
        public EventType Type { get; }
        public NUnitTestEventHeader(string sNode)
        {
            Node = XmlHelper.CreateXmlNode(sNode);
            switch (Node.Name)
            {
                case "start-test":
                    Type = EventType.StartTest;
                    break;
                case "test-case":
                    Type = EventType.TestCase;
                    break;
                case "test-suite":
                    Type = EventType.TestSuite;
                    break;
                case "test-output":
                    Type = EventType.TestOutput;
                    break;
                case "start-run":
                    Type = EventType.StartRun;
                    break;
                case "start-suite":
                    Type = EventType.StartSuite;
                    break;
                default:
                    Type = EventType.NoIdea;
                    break;
            }
        }
    }
}