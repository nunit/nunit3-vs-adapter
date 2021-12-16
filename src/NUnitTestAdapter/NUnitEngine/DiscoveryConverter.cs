// ***********************************************************************
// Copyright (c) 2020-2021 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.VisualStudio.TestAdapter.Internal;



namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface IDiscoveryConverter
    {
        IEnumerable<NUnitDiscoveryTestCase> AllTestCases { get; }
        bool IsExplicitRun { get; }
        IList<TestCase> LoadedTestCases { get; }
        int NoOfLoadedTestCases { get; }

        /// <summary>
        /// Checks if we're running the latest Current DiscoveryMethod.
        /// </summary>
        bool IsDiscoveryMethodCurrent { get; }

        bool NoOfLoadedTestCasesAboveLimit { get; }
        int NoOfExplicitTestCases { get; }
        bool HasExplicitTests { get; }
        IEnumerable<TestCase> GetExplicitTestCases(IEnumerable<TestCase> filteredTestCases);
    }

    public class DiscoveryConverter : IDiscoveryConverter
    {
        private const string ParameterizedFixture = nameof(ParameterizedFixture);
        private const string TestFixture = nameof(TestFixture);
        private const string GenericFixture = nameof(GenericFixture);
        private const string SetUpFixture = nameof(SetUpFixture);
        private const string TestSuite = nameof(TestSuite);

        internal static class NUnitXmlAttributeNames
        {
            public const string Id = "id";
            public const string Type = "type";
            public const string Name = "name";
            public const string Fullname = "fullname";
            public const string Runstate = "runstate";
            public const string Testcasecount = "testcasecount";
            public const string Classname = "classname";
            public const string Methodname = "methodname";
            public const string Seed = "seed";
        }

        private ITestConverterXml converterForXml;
        private ITestConverter converter;

        public ITestConverterCommon TestConverterForXml => converterForXml;

        public ITestConverterCommon TestConverter => converter;

        public NUnitDiscoveryTestRun TestRun { get; private set; }

        /// <summary>
        /// Checks if we're running the latest Current DiscoveryMethod.
        /// </summary>
        public bool IsDiscoveryMethodCurrent => Settings.DiscoveryMethod == DiscoveryMethod.Current;

        public NUnitDiscoveryTestAssembly CurrentTestAssembly => TestRun.TestAssembly;

        public NUnitDiscoveryTestSuite TopLevelTestSuite => CurrentTestAssembly.TestSuites.FirstOrDefault();

        public IEnumerable<NUnitDiscoveryTestCase> AllTestCases => CurrentTestAssembly.AllTestCases;

        public bool IsExplicitRun => CurrentTestAssembly?.IsExplicit ?? false;

        public int NoOfExplicitTestCases => CurrentTestAssembly.NoOfExplicitTestCases;

        public bool HasExplicitTests => NoOfExplicitTestCases > 0;

        private readonly List<TestCase> loadedTestCases = new ();
        public IList<TestCase> LoadedTestCases => loadedTestCases;

        public int NoOfLoadedTestCases => loadedTestCases.Count;

        public string AssemblyPath { get; private set; }

        private IAdapterSettings Settings { get; }
        private ITestLogger TestLog { get; }

        public bool NoOfLoadedTestCasesAboveLimit => NoOfLoadedTestCases > Settings.AssemblySelectLimit;
        public IEnumerable<TestCase> GetExplicitTestCases(IEnumerable<TestCase> filteredTestCases)
        {
            var explicitCases = new List<TestCase>();
            foreach (var tc in filteredTestCases)
            {
                var correspondingNUnitTestCase = AllTestCases.FirstOrDefault(o => o.FullName == tc.FullyQualifiedName);
                if (correspondingNUnitTestCase == null)
                {
                    TestLog.Warning(
                        $"CheckExplicit: Can't locate corresponding NUnit testcase {tc.FullyQualifiedName}");
                    continue;
                }

                if (correspondingNUnitTestCase.IsExplicitReverse)
                    explicitCases.Add(tc);
            }
            return explicitCases;
        }


        public DiscoveryConverter(ITestLogger logger, IAdapterSettings settings)
        {
            Settings = settings;
            TestLog = logger;
        }

        public IList<TestCase> Convert(NUnitResults discoveryResults, string assemblyPath)
        {
            if (discoveryResults == null)
                return new List<TestCase>();
            AssemblyPath = assemblyPath;
            var timing = new TimingLogger(Settings, TestLog);
            if (Settings.DiscoveryMethod != DiscoveryMethod.Legacy)
            {
                TestRun = ConvertXml(discoveryResults);
            }

            var nunitTestCases = discoveryResults.TestCases();

            // As a side effect of calling TestConverter.ConvertTestCase,
            // the converter's cache of all test cases is populated as well.
            // All future calls to convert a test case may now use the cache.

            if (Settings.DiscoveryMethod == DiscoveryMethod.Legacy)
            {
                converterForXml = new TestConverterForXml(TestLog, AssemblyPath, Settings);
                foreach (XmlNode testNode in nunitTestCases)
                    loadedTestCases.Add(converterForXml.ConvertTestCase(new NUnitEventTestCase(testNode)));
                TestLog.Info(
                    $"   NUnit3TestExecutor discovered {loadedTestCases.Count} of {nunitTestCases.Count} NUnit test cases using Legacy discovery mode");
            }
            else
            {
                converter = new TestConverter(TestLog, AssemblyPath, Settings, this);
                var isExplicit = TestRun.IsExplicit;
                var testCases = RunnableTestCases(isExplicit);
                foreach (var testNode in testCases)
                    loadedTestCases.Add(converter.ConvertTestCase(testNode));
                var msg = isExplicit ? "Explicit run" : "Non-Explicit run";
                TestLog.Info(
                    $"   NUnit3TestExecutor discovered {loadedTestCases.Count} of {nunitTestCases.Count} NUnit test cases using Current Discovery mode, {msg}");
            }

            timing.LogTime("Converting test cases ");
            return loadedTestCases;

            IEnumerable<NUnitDiscoveryTestCase> RunnableTestCases(bool isExplicit) =>
                isExplicit
                    ? TestRun.TestAssembly.AllTestCases
                    : TestRun.TestAssembly.RunnableTestCases;
        }

        public NUnitDiscoveryTestRun ConvertXml(NUnitResults discovery)
        {
            var doc = XDocument.Load(new XmlNodeReader(discovery.FullTopNode));
            var testrun = ExtractTestRun(doc);
            var anode = doc.Root.Elements("test-suite");
            var assemblyNode = anode.Single(o => o.Attribute(NUnitXmlAttributeNames.Type).Value == "Assembly");
            var testassembly = ExtractTestAssembly(assemblyNode, testrun);
            ExtractAllFixtures(testassembly, assemblyNode);
            return testrun;
        }

        private static NUnitDiscoveryTestSuite ExtractTestSuite(XElement node, NUnitDiscoverySuiteBase parent)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var ts = new NUnitDiscoveryTestSuite(b, parent);
            return ts;
        }

        private void ExtractAllFixtures(NUnitDiscoveryCanHaveTestFixture parent, XElement node)
        {
            foreach (var child in node.Elements("test-suite"))
            {
                var type = child.Attribute(NUnitXmlAttributeNames.Type)?.Value;
                if (type == null)
                {
                    TestLog.Debug($"ETF1:Don't understand element: {child}");
                    continue;
                }

                var className = child.Attribute(NUnitXmlAttributeNames.Classname)?.Value;
                switch (type)
                {
                    case TestFixture:
                        var tf = ExtractTestFixture(parent, child, className);
                        parent.AddTestFixture(tf);
                        ExtractTestCases(tf, child);
                        ExtractParameterizedMethodsAndTheories(tf, child);
                        break;
                    case GenericFixture:
                        var gtf = ExtractGenericTestFixture(parent, child);
                        parent.AddTestGenericFixture(gtf);
                        ExtractTestFixtures(gtf, child);
                        break;
                    case ParameterizedFixture:
                        var ptf = ExtractParameterizedTestFixture(parent, child);
                        parent.AddParameterizedFixture(ptf);
                        ExtractTestFixtures(ptf, child);
                        break;
                    case SetUpFixture:
                        var stf = ExtractSetUpTestFixture(parent, child, className);
                        parent.AddSetUpFixture(stf);
                        ExtractTestFixtures(stf, child);
                        break;
                    case TestSuite:
                        var ts = ExtractTestSuite(child, parent);
                        parent.AddTestSuite(ts);
                        if (child.HasElements)
                            ExtractAllFixtures(ts, child);
                        break;
                    default:
                        throw new DiscoveryException($"Invalid type found in ExtractAllFixtures for test suite: {type}");
                }
            }
        }

        private void ExtractTestFixtures(NUnitDiscoveryCanHaveTestFixture parent, XElement node)
        {
            foreach (var child in node.Elements().Where(o => o.Name != "properties"))
            {
                var type = child.Attribute(NUnitXmlAttributeNames.Type)?.Value;
                if (type == null)
                {
                    TestLog.Debug($"ETF2:Don't understand element: {child}");
                    continue;
                }
                var className = child.Attribute(NUnitXmlAttributeNames.Classname)?.Value;
                var btf = ExtractSuiteBasePropertiesClass(child);
                switch (type)
                {
                    case TestFixture:
                        var tf = new NUnitDiscoveryTestFixture(btf, className, parent);
                        parent.AddTestFixture(tf);
                        ExtractTestCases(tf, child);
                        ExtractParameterizedMethodsAndTheories(tf, child);
                        break;
                    case TestSuite:
                        var ts = ExtractTestSuite(child, parent);
                        parent.AddTestSuite(ts);
                        if (child.HasElements)
                            ExtractAllFixtures(ts, child);
                        break;
                    case SetUpFixture:
                        var tsf = ExtractSetUpTestFixture(parent, node, className);
                        parent.AddSetUpFixture(tsf);
                        if (child.HasElements)
                        {
                            ExtractTestFixtures(tsf, child);
                        }
                        break;
                    case ParameterizedFixture:
                        var ptf = ExtractParameterizedTestFixture(parent, node);
                        parent.AddParameterizedFixture(ptf);
                        if (child.HasElements)
                        {
                            ExtractTestFixtures(ptf, child);
                        }
                        break;
                    case GenericFixture:
                        var gf = ExtractGenericTestFixture(parent, node);
                        parent.AddTestGenericFixture(gf);
                        if (child.HasElements)
                        {
                            ExtractTestFixtures(gf, child);
                        }
                        break;
                    default:
                        throw new DiscoveryException($"Not a TestFixture, SetUpFixture, ParameterizedFixture, GenericFixture or TestSuite, but {type}");
                }
            }
        }

        private static void ExtractParameterizedMethodsAndTheories(NUnitDiscoveryTestFixture tf, XElement node)
        {
            const string parameterizedMethod = "ParameterizedMethod";
            const string theory = "Theory";
            foreach (var child in node.Elements("test-suite"))
            {
                var type = child.Attribute(NUnitXmlAttributeNames.Type)?.Value;
                if (type != parameterizedMethod && type != "Theory" && type != "GenericMethod")
                    throw new DiscoveryException($"Expected ParameterizedMethod, Theory or GenericMethod, but was {type}");
                var className = child.Attribute(NUnitXmlAttributeNames.Classname)?.Value;
                var btf = ExtractSuiteBasePropertiesClass(child);
                switch (type)
                {
                    case parameterizedMethod:
                        {
                            var tc = new NUnitDiscoveryParameterizedMethod(btf, className, tf);
                            ExtractTestCases(tc, child);
                            tf.AddParameterizedMethod(tc);
                            break;
                        }
                    case theory:
                        {
                            var tc = new NUnitDiscoveryTheory(btf, className, tf);
                            tf.AddTheory(tc);
                            ExtractTestCases(tc, child);
                            break;
                        }
                    default:
                        {
                            var tc = new NUnitDiscoveryGenericMethod(btf, className, tf);
                            tf.AddGenericMethod(tc);
                            ExtractTestCases(tc, child);
                            break;
                        }
                }
            }
        }

        public static IEnumerable<NUnitDiscoveryTestCase> ExtractTestCases(INUnitDiscoveryCanHaveTestCases tf, XElement node)
        {
            foreach (var child in node.Elements("test-case"))
            {
                var tc = ExtractTestCase(tf, child);
                tf.AddTestCase(tc);
            }

            return tf.TestCases;
        }

        /// <summary>
        /// Extracts single test case, made public for testing.
        /// </summary>
        public static NUnitDiscoveryTestCase ExtractTestCase(INUnitDiscoveryCanHaveTestCases tf, XElement child)
        {
            var className = child.Attribute(NUnitXmlAttributeNames.Classname)?.Value;
            var methodName = child.Attribute(NUnitXmlAttributeNames.Methodname)?.Value;
            var seedAtr = child.Attribute(NUnitXmlAttributeNames.Seed)?.Value;
            var seed = seedAtr != null ? long.Parse(seedAtr, CultureInfo.InvariantCulture) : 0;
            var btf = ExtractSuiteBasePropertiesClass(child);
            var tc = new NUnitDiscoveryTestCase(btf, tf, className, methodName, seed);
            return tc;
        }


        public static NUnitDiscoveryTestFixture ExtractTestFixture(INUnitDiscoveryCanHaveTestFixture parent, XElement node,
            string className)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var ts = new NUnitDiscoveryTestFixture(b, className, parent);
            return ts;
        }

        private static NUnitDiscoveryGenericFixture ExtractGenericTestFixture(
            NUnitDiscoveryCanHaveTestFixture parent,
            XElement node)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var ts = new NUnitDiscoveryGenericFixture(b, parent);
            return ts;
        }
        private static NUnitDiscoverySetUpFixture ExtractSetUpTestFixture(
            NUnitDiscoveryCanHaveTestFixture parent,
            XElement node, string className)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var ts = new NUnitDiscoverySetUpFixture(b, className, parent);
            return ts;
        }
        private static NUnitDiscoveryParameterizedTestFixture ExtractParameterizedTestFixture(
            NUnitDiscoveryCanHaveTestFixture parent, XElement node)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var ts = new NUnitDiscoveryParameterizedTestFixture(b, parent);
            return ts;
        }

        private NUnitDiscoveryTestAssembly ExtractTestAssembly(XElement node, NUnitDiscoveryTestRun parent)
        {
            string dType = node.Attribute(NUnitXmlAttributeNames.Type)?.Value;
            if (dType is not "Assembly")
                throw new DiscoveryException("Node is not of type assembly: " + node);
            var aBase = ExtractSuiteBasePropertiesClass(node);
            var assembly = new NUnitDiscoveryTestAssembly(aBase, parent);
            parent.AddTestAssembly(assembly);
            return assembly;
        }

        private static BaseProperties ExtractSuiteBasePropertiesClass(XElement node)
        {
            string dId = node.Attribute(NUnitXmlAttributeNames.Id).Value;
            string dName = node.Attribute(NUnitXmlAttributeNames.Name).Value;
            string dFullname = node.Attribute(NUnitXmlAttributeNames.Fullname).Value;
            var dRunstate = ExtractRunState(node);
            const char apo = '\'';
            var tcs = node.Attribute(NUnitXmlAttributeNames.Testcasecount)?.Value.Trim(apo);
            int dTestcasecount = int.Parse(tcs ?? "1", CultureInfo.InvariantCulture);
            var bp = new BaseProperties(dId, dName, dFullname, dTestcasecount, dRunstate);

            foreach (var propnode in node.Elements("properties").Elements("property"))
            {
                var prop = new NUnitProperty(propnode);
                bp.Properties.Add(prop);
            }
            return bp;
        }


        private NUnitDiscoveryTestRun ExtractTestRun(XDocument node)
        {
            var sb = ExtractSuiteBasePropertiesClass(node.Root);
            var tr = new NUnitDiscoveryTestRun(sb);
            return tr;
        }


        private static RunStateEnum ExtractRunState(XElement node)
        {
            var runState = node.Attribute(NUnitXmlAttributeNames.Runstate)?.Value switch
            {
                "Runnable" => RunStateEnum.Runnable,
                "Explicit" => RunStateEnum.Explicit,
                "NotRunnable" => RunStateEnum.NotRunnable,
                _ => RunStateEnum.NA
            };
            return runState;
        }
    }

    public class BaseProperties
    {
        public BaseProperties(string dId, string dName, string dFullname, int dTestcasecount, RunStateEnum dRunstate)
        {
            Id = dId;
            Name = dName;
            Fullname = dFullname;
            TestCaseCount = dTestcasecount;
            RunState = dRunstate;
        }

        public List<NUnitProperty> Properties { get; } = new ();

        public string Id { get; }
        public string Name { get; }
        public string Fullname { get; }
        public RunStateEnum RunState { get; }

        public int TestCaseCount { get; }
    }
}
