using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitTestEventStartTest : INUnitTestEvent
    {
    }

    /// <summary>
    /// Handles the NUnit 'start-test' event.
    /// </summary>
    public class NUnitTestEventStartTest : NUnitTestEvent, INUnitTestEventStartTest
    {
        public NUnitTestEventStartTest(INUnitTestEventForXml node) : this(node.Node)
        { }
        public NUnitTestEventStartTest(string testEvent) : this(XmlHelper.CreateXmlNode(testEvent))
        { }

        public NUnitTestEventStartTest(XmlNode node) : base(node)
        {
            if (node.Name != "start-test")
                throw new NUnitEventWrongTypeException($"Expected 'start-test', got {node.Name}");
        }
    }
}