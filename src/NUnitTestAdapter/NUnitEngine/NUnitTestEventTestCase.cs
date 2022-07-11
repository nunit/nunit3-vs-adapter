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

        /// <summary>
        /// Complete formatted stacktrace
        /// </summary>
        string FailureStackTrace { get; }
    }


    /// <summary>
    /// Handles the NUnit 'test-case' event.
    /// </summary>
    public class NUnitTestEventTestCase : NUnitTestEvent, INUnitTestEventTestCase
    {
        public NUnitTestEventTestCase(INUnitTestEventForXml node)
            : this(node.Node)
        {
        }

        public NUnitTestEventTestCase(string testEvent)
            : this(XmlHelper.CreateXmlNode(testEvent))
        {
        }

        public NUnitFailure Failure { get; }

        public NUnitTestEventTestCase(XmlNode node)
            : base(node)
        {
            if (node.Name != "test-case")
                throw new NUnitEventWrongTypeException($"Expected 'test-case', got {node.Name}");
            var failureNode = Node.SelectSingleNode("failure");
            if (failureNode != null)
            {
                Failure = new NUnitFailure(
                    failureNode.SelectSingleNode("message")?.InnerText.UnEscapeUnicodeCharacters(),
                    failureNode.SelectSingleNode("stack-trace")?.InnerText.UnEscapeUnicodeCharacters());
            }

            ReasonMessage = Node.SelectSingleNode("reason/message")?.InnerText.UnEscapeUnicodeCharacters();
        }
        public string ReasonMessage { get; }

        public bool HasReason => !string.IsNullOrEmpty(ReasonMessage);
        public bool HasFailure => Failure != null;

        public string FailureStackTrace => $"{Failure?.Stacktrace ?? ""}\n{StackTrace}";


        /// <summary>
        /// Find stacktrace in assertion nodes if not defined.
        /// </summary>
        public string StackTrace
        {
            get
            {
                string stackTrace = string.Empty;
                int i = 1;
                foreach (XmlNode assertionStacktraceNode in Node.SelectNodes("assertions/assertion/stack-trace"))
                {
                    stackTrace += $"{i++}) " + assertionStacktraceNode.InnerText.UnEscapeUnicodeCharacters();
                    stackTrace += "\n";
                }

                return stackTrace;
            }
        }
    }
}