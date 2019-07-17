using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NUnit.VisualStudio.TestAdapter.Dump
{

    public interface IFile
    {
        void WriteAllText(string path, string txt);
        bool DirectoryExist(string path);

        void CreateDirectory(string path);
    }

    public class File : IFile
    {
        public void WriteAllText(string path, string txt)
        {
            System.IO.File.WriteAllText(path,txt);
        }

        public bool DirectoryExist(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }
    }


    public interface IDumpXml
    {
        void AddString(string text);
        void AddTestEvent(string text);
    }

    public class DumpXml : IDumpXml
    {
        private const string Header = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
        private const string Rootstart = "<NUnitXml>\n";
        private const string Rootend = "\n</NUnitXml>";
        private readonly IFile file;
        private readonly string directory;
        private readonly string filename;
        private StringBuilder txt;

        public DumpXml(string path, IFile file=null)
        {
            directory = Path.GetDirectoryName(path);
            filename = Path.GetFileName(path);
            this.file = file ?? new File();
            txt = new StringBuilder();
            txt.Append(Header);
            txt.Append(Rootstart);
        }

        public void Dump2File(string path)
        {
            EnsurePathExist(path);
            txt.Append(Rootend);
            file.WriteAllText(path,txt.ToString());
            txt = new StringBuilder();
        }

        private void EnsurePathExist(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!file.DirectoryExist(folder))
                file.CreateDirectory(folder);
        }

        public void Dump4Discovery()
        {
            var dumpfolder = Path.Combine(directory, "Dump");
            var path = Path.Combine(dumpfolder, $"D_{filename}.dump");
            Dump2File(path);
        }

        public void Dump4Execution()
        {
            var dumpfolder = Path.Combine(directory, "Dump");
            var path = Path.Combine(dumpfolder, $"E_{filename}.dump");
            Dump2File(path);
        }

        public string RandomName()
        {
            var guid = Guid.NewGuid();
            var res = Convert.ToBase64String(guid.ToByteArray());
            var res2 = Regex.Replace(res, @"[^a-zA-Z0-9]", "");
             return res2+".dump";
        }

        public void AddTestEvent(string text)
        {
            txt.Append("<NUnitTestEvent>\n");
            txt.Append(text);
            txt.Append("\n</NUnitTestEvent>\n");
        }





        public void AddString(string text)
        {
            txt.Append(text);
        }

    }

#if NET35
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
#endif
}
