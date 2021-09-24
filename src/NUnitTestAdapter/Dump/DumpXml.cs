using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Engine;

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
            System.IO.File.WriteAllText(path, txt);
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
        void StartDiscoveryInExecution(IGrouping<string, TestCase> testCases, TestFilter filter, TestPackage package);
        void DumpForExecution();
        void DumpVSInputFilter(TestFilter filter, string info);
        void StartExecution(TestFilter filter, string atExecution);
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
        private static string assemblyPath;

        public DumpXml(string path, IFile file = null)
        {
            assemblyPath = path;
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
            file.WriteAllText(path, txt.ToString());
            txt = new StringBuilder();
        }

        private void EnsurePathExist(string path)
        {
            var folder = Path.GetDirectoryName(path);
            if (!file.DirectoryExist(folder))
                file.CreateDirectory(folder);
        }

        public void DumpForDiscovery()
        {
            var dumpfolder = Path.Combine(directory, "Dump");
            var path = Path.Combine(dumpfolder, $"D_{filename}.dump");
            Dump2File(path);
        }

        public void DumpForExecution()
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
            return res2 + ".dump";
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

        public void DumpVSInputFilter(TestFilter filter, string info)
        {
            AddString($"<TestFilter>\n {info}  {filter.Text}\n</TestFilter>\n\n");
        }

        public void DumpVSInput(IEnumerable<TestCase> testCases)
        {
            AddString("<VS_Input_TestCases>   (DisplayName : FQN : Id)\n");
            foreach (var tc in testCases)
            {
                AddString($"   {tc.DisplayName} : {tc.FullyQualifiedName} : {tc.Id}\n");
            }
            AddString("</VS_Input_TestCases>\n");
        }

        public void DumpVSInput2NUnit(TestPackage package)
        {
            AddString($"\n<TestPackage>: {package.Name}\n\n");
            foreach (var tc in package.SubPackages)
            {
                DumpVSInput2NUnit(tc);
            }
            AddString("\n</TestPackage>\n\n");
        }

        private void DumpFromVSInput(IGrouping<string, TestCase> testCases, TestFilter filter, TestPackage package)
        {
            AddString("<VSTest input/>\n\n");
            if (testCases != null)
                DumpVSInput(testCases);
            DumpVSInput2NUnit(package);
            DumpVSInputFilter(filter, "");
            AddString("</VSTest input/>\n\n");
        }

        public void StartDiscoveryInExecution(IGrouping<string, TestCase> testCases, TestFilter filter, TestPackage package)
        {
            DumpFromVSInput(testCases, filter, package);
            AddString($"<NUnitDiscoveryInExecution>{assemblyPath}</NUnitDiscoveryInExecution>\n\n");
        }

        public void StartExecution(TestFilter filter, string atExecution)
        {
            DumpVSInputFilter(filter, atExecution);
            AddString($"\n\n<NUnitExecution>{assemblyPath}</NUnitExecution>\n\n");
        }

        public static IDumpXml CreateDump(string path, IGrouping<string, TestCase> testCases, IAdapterSettings settings)
        {
            if (!settings.DumpXmlTestResults)
                return null;
            var executionDumpXml = new DumpXml(path);
            string runningBy = testCases == null
                ? "<RunningBy>Sources</RunningBy>"
                : "<RunningBy>TestCases</RunningBy>";
            executionDumpXml.AddString($"\n{runningBy}\n");
            return executionDumpXml;
        }
    }
}
