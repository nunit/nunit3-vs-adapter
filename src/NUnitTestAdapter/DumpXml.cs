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
            return System.IO.Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            System.IO.Directory.CreateDirectory(path);
        }
    }


    public interface IDumpXml
    {
        void AddString(string text);
    }

    public class DumpXml : IDumpXml
    {
        private readonly IFile file;
        private readonly string directory;
        private readonly string filename;
        private StringBuilder txt;

        public DumpXml(string path, IFile file=null)
        {
            this.directory = Path.GetDirectoryName(path);
            this.filename = Path.GetFileName(path);
            this.file = file ?? new File();
            txt = new StringBuilder();
        }

        


        

        public void Dump2File(string path)
        {
            EnsurePathExist(path);
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


        public void AddString(string text)
        {
            txt.Append(text);
        }

    }

#if !NETCOREAPP1_0
    public static class XmlNodeExtension
    {
        public static string AsString(this System.Xml.XmlNode node)
        {
            using (var swriter = new System.IO.StringWriter())
            {
                using (var twriter = new System.Xml.XmlTextWriter(swriter))
                {
                    twriter.Formatting = System.Xml.Formatting.Indented;
                    twriter.Indentation = 3;
                    twriter.QuoteChar = '\'';
                    node.WriteContentTo(twriter);
                }
                return swriter.ToString();
            }
        }
    }
#endif
}
