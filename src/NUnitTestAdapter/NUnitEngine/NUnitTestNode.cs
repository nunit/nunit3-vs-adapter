using System.Collections.Generic;
using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public abstract class NUnitTestNode
    {
        public XmlNode Node { get; protected set; }
        public string Id => Node.GetAttribute("id");
        public string FullName => Node.GetAttribute("fullname");
        public string Name => Node.GetAttribute("name");
        public bool IsNull => Node == null;
        public List<NUnitProperty> Properties { get; } = new List<NUnitProperty>();
        protected NUnitTestNode(XmlNode node)
        {
            Node = node;
            var propertyNodes = Node.SelectNodes("properties/property");
            if (propertyNodes != null)
            {
                foreach (XmlNode prop in propertyNodes)
                {
                    Properties.Add(new NUnitProperty(prop.GetAttribute("name"), prop.GetAttribute("value")));
                }
            }
        }
    }
}