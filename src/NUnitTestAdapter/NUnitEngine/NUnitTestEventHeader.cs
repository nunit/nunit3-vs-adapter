using System.Xml;
using NUnit.VisualStudio.TestAdapter.Dump;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public class NUnitTestEventHeader : INUnitTestEvent
    {
        public enum EventType
        {
            NoIdea,
            StartTest,
            TestCase,
            TestSuite,
            TestOutput
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
                default:
                    Type = EventType.NoIdea;
                    break;
            }
        }
    }
}