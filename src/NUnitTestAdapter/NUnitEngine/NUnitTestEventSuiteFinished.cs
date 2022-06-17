using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitTestEventSuiteFinished : INUnitTestEvent
    {
        string ReasonMessage { get; }
        bool HasReason { get; }
        string FailureMessage { get; }
        bool HasFailure { get; }
        string StackTrace { get; }
    }

    /// <summary>
    /// Handles the NUnit 'test-suite' event.
    /// </summary>
    public class NUnitTestEventSuiteFinished : NUnitTestEvent, INUnitTestEventSuiteFinished
    {
        public NUnitTestEventSuiteFinished(INUnitTestEventForXml node) : this(node.Node)
        { }
        public NUnitTestEventSuiteFinished(string testEvent) : this(XmlHelper.CreateXmlNode(testEvent))
        { }

        public NUnitTestEventSuiteFinished(XmlNode node) : base(node)
        {
            if (node.Name != "test-suite")
                throw new NUnitEventWrongTypeException($"Expected 'test-suite', got {node.Name}");
            var failureNode = Node.SelectSingleNode("failure");
            if (failureNode != null)
            {
                FailureMessage = UnicodeEscapeHelper.UnEscapeUnicodeCharacters(failureNode.SelectSingleNode("message")?.InnerText);
                StackTrace = UnicodeEscapeHelper.UnEscapeUnicodeCharacters(failureNode.SelectSingleNode("stack-trace")?.InnerText);
            }
            ReasonMessage = UnicodeEscapeHelper.UnEscapeUnicodeCharacters(Node.SelectSingleNode("reason/message")?.InnerText);
        }

        public string ReasonMessage { get; }

        public bool HasReason => !string.IsNullOrEmpty(ReasonMessage);
        public string FailureMessage { get; } = "";

        public string StackTrace { get; } = "";

        public bool HasFailure => !string.IsNullOrEmpty(FailureMessage);
    }
}