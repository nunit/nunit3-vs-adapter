using System.IO;

namespace NUnit.VisualStudio.TestAdapter.Dump
{
    public static class XmlNodeExtension
    {
        public static string AsString(this System.Xml.XmlNode node)
        {
            using (var swriter = new StringWriter())
            {
                using (var twriter = new System.Xml.XmlTextWriter(swriter))
                {
                    twriter.Formatting = System.Xml.Formatting.Indented;
                    twriter.Indentation = 3;
                    twriter.QuoteChar = '\'';
                    node.WriteTo(twriter);
                }
                return swriter.ToString();
            }
        }
    }
}