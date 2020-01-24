using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    /// <summary>
    /// Handles the 'test-output' event.
    /// </summary>
    public class NUnitTestEventTestOutput : NUnitTestEvent
    {
        public enum Streams
        {
            NoIdea,
            Error,
            Progress
        }

        public Streams Stream { get; }
        public string TestId => Node.GetAttribute("testid");

        public string TestName => Node.GetAttribute("testname");


        public NUnitTestEventTestOutput(INUnitTestEvent theEvent) : this(theEvent.Node)
        {
            if (theEvent.Node.Name != "test-output")
                throw new NUnitEventWrongTypeException($"Expected 'test-output', got {theEvent.Node.Name}");
        }
        public NUnitTestEventTestOutput(XmlNode node) : base(node)
        {
            switch (node.GetAttribute("stream"))
            {
                case "Error":
                    Stream = Streams.Error;
                    break;
                case "Progress":
                    Stream = Streams.Progress;
                    break;
                default:
                    Stream = Streams.NoIdea;
                    break;
            }
        }

        /// <summary>
        /// Returns the output information.
        /// </summary>
        public string Content => Node.InnerText;



    }
}