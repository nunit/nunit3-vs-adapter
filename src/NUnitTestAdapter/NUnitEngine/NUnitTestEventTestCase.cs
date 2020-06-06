using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitTestEventTestCase : INUnitTestEvent
    {
        NUnitFailure Failure { get; }
        string ReasonMessage { get; }
        bool HasReason { get; }

        bool HasFailure { get; }

        /// <summary>
        /// Find stacktrace in assertion nodes if not defined.
        /// </summary>
        string StackTrace { get; }
    }



    /// <summary>
    /// Handles the NUnit 'test-case' event.
    /// </summary>
    public class NUnitTestEventTestCase : NUnitTestEvent, INUnitTestEventTestCase
    {
        public NUnitTestEventTestCase(INUnitTestEventForXml node) : this(node.Node)
        {
        }

        public NUnitTestEventTestCase(string testEvent) : this(XmlHelper.CreateXmlNode(testEvent))
        {
        }

        public NUnitFailure Failure { get; }

        public NUnitTestEventTestCase(XmlNode node) : base(node)
        {
            if (node.Name != "test-case")
                throw new NUnitEventWrongTypeException($"Expected 'test-case', got {node.Name}");
            var failureNode = Node.SelectSingleNode("failure");
            if (failureNode != null)
            {
                Failure = new NUnitFailure(
                    failureNode.SelectSingleNode("message")?.InnerText,
                    failureNode.SelectSingleNode("stack-trace")?.InnerText);
            }
            ReasonMessage = Node.SelectSingleNode("reason/message")?.InnerText;
        }

        public string ReasonMessage { get; }

        public bool HasReason => !string.IsNullOrEmpty(ReasonMessage);
        public bool HasFailure => Failure != null;

        /// <summary>
        /// Find stacktrace in assertion nodes if not defined.
        /// </summary>
        public string StackTrace
        {
            get
            {
                string stackTrace = string.Empty;
                foreach (XmlNode assertionStacktraceNode in Node.SelectNodes("assertions/assertion/stack-trace"))
                {
                    stackTrace += assertionStacktraceNode.InnerText;
                }

                return stackTrace;
            }
        }
    }
}