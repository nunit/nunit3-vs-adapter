// ***********************************************************************
// Copyright (c) 2020-2020 Charlie Poole, Terje Sandstrom
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
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public class DiscoveryConverter
    {
#pragma warning disable SA1303 // Const field names should begin with upper-case letter
        private const string id = nameof(id);
        private const string type = nameof(type);
        private const string name = nameof(name);
        private const string fullname = nameof(fullname);
        private const string runstate = nameof(runstate);
        private const string testcasecount = nameof(testcasecount);
        private const string classname = nameof(classname);
        private const string methodname = nameof(methodname);

#pragma warning restore SA1303 // Const field names should begin with upper-case letter

        public NUnitDiscoveryTestRun Convert(NUnitResults discovery)
        {
            var doc = XDocument.Load(new XmlNodeReader(discovery.FullTopNode));
            var testrun = ExtractTestRun(doc);
            var anode = doc.Root.Elements("test-suite");
            var assemblyNode = anode.Single(o => o.Attribute("type").Value == "Assembly");
            var testassembly = ExtractTestAssembly(assemblyNode);
            testassembly.Parent = testrun;
            testrun.TestAssembly = testassembly;
            var node = assemblyNode.Elements("test-suite").Single();
            var type = node.Attribute("test-suite")?.Value;
            if (type != null)
                throw new DiscoveryException();
            var topLevelSuite = ExtractTestSuiteForAssembly(node, testassembly);
            testassembly.AddTestSuite(topLevelSuite);
            ExtractAllFixtures(topLevelSuite, node);
            return testrun;
        }

        private NUnitDiscoveryTestSuite ExtractTestSuiteForAssembly(XElement node, NUnitDiscoverySuiteBase parent)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var ts = new NUnitDiscoveryTestSuite(b, parent);
            return ts;
        }

        private static void ExtractAllFixtures(NUnitDiscoveryTestSuite parent, XElement node)
        {
            foreach (var child in node.Elements())
            {
                var type = child.Attribute("type").Value;
                var className = child.Attribute(classname)?.Value;
                var btf = ExtractSuiteBasePropertiesClass(child);
                switch (type)
                {
                    case "TestFixture":
                        var tf = ExtractTestFixture(parent, child, className);
                        parent.AddTestFixture(tf);
                        ExtractTestCases(tf, child);
                        ExtractParameterizedMethodsAndTheories(tf, child);
                        break;
                    case "GenericFixture":
                        var gtf = ExtractGenericTestFixture(parent, child, className);
                        parent.AddTestGenericFixture(gtf);
                        ExtractTestFixtures(gtf, child);
                        break;
                    case "ParameterizedFixture":
                        var ptf = ExtractParametrizedTestFixture(parent, child, className);
                        parent.AddParametrizedFixture(ptf);
                        ExtractTestFixtures(ptf, child);
                        break;
                    case "SetUpFixture":
                        var stf = ExtractSetUpTestFixture(parent, child, className);
                        parent.AddSetUpFixture(stf);
                        ExtractTestFixtures(stf, child);
                        break;
                    default:
                        throw new DiscoveryException($"Invalid type found in ExtractAllFixtures: {type}");
                }
            }
        }

        private static void ExtractTestFixtures(NUnitDiscoveryCanHaveTestFixture parent, XElement node)
        {
            foreach (var child in node.Elements())
            {
                var type = child.Attribute("type").Value;
                var className = child.Attribute(classname)?.Value;
                var btf = ExtractSuiteBasePropertiesClass(child);
                if (type != "TestFixture")
                    throw new DiscoveryException($"Not a TestFixture, but {type}");
                var tf = new NUnitDiscoveryTestFixture(btf, className, parent);
                parent.AddTestFixture(tf);
                ExtractTestCases(tf, child);
                ExtractParameterizedMethodsAndTheories(tf, child);
            }
        }

        private static void ExtractParameterizedMethodsAndTheories(NUnitDiscoveryTestFixture tf, XElement node)
        {
            const string ParameterizedMethod = nameof(ParameterizedMethod);
            foreach (var child in node.Elements("test-suite"))
            {
                var type = child.Attribute("type")?.Value;
                if (type != ParameterizedMethod && type != "Theory")
                    throw new DiscoveryException($"Expected ParameterizedMethod or Theory, but was {type}");
                var className = child.Attribute(classname)?.Value;
                var btf = ExtractSuiteBasePropertiesClass(child);
                if (type == ParameterizedMethod)
                {
                    var tc = new NUnitDiscoveryParameterizedMethod(btf, className, tf);
                    ExtractTestCases(tc, child);
                    tf.AddParametrizedMethod(tc);
                }
                else
                {
                    var tc = new NUnitDiscoveryTheory(btf, className, tf);
                    tf.AddTheory(tc);
                    ExtractTestCases(tc, child);
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
            var className = child.Attribute(classname)?.Value;
            var methodName = child.Attribute(methodname)?.Value;
            var seedAtr = child.Attribute("seed")?.Value;
            var seed = seedAtr != null ? long.Parse(seedAtr) : 0;
            var btf = ExtractSuiteBasePropertiesClass(child);
            var tc = new NUnitDiscoveryTestCase(btf, tf, methodName, seed) { ClassName = className };
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
            XElement node, string className)
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
        private static NUnitDiscoveryParameterizedTestFixture ExtractParametrizedTestFixture(
            NUnitDiscoveryCanHaveTestFixture parent, XElement node, string className)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var ts = new NUnitDiscoveryParameterizedTestFixture(b, parent);
            return ts;
        }

        private NUnitDiscoveryTestAssembly ExtractTestAssembly(XElement node)
        {
            string d_type = node.Attribute(type).Value;
            if (d_type != "Assembly")
                throw new DiscoveryException("Node is not of type assembly: " + node);
            var a_base = ExtractSuiteBasePropertiesClass(node);
            return new NUnitDiscoveryTestAssembly(a_base);
        }

        private static NUnitDiscoverySuiteBase ExtractSuiteBasePropertiesClass(XElement node)
        {
            string dId = node.Attribute(id).Value;
            string dName = node.Attribute(name).Value;
            string dFullname = node.Attribute(fullname).Value;
            var dRunstate = ExtractRunState(node);
            const char apo = (char)0x22;
            var tcs = node.Attribute(testcasecount)?.Value.Trim(apo);
            int dTestcasecount = int.Parse(tcs ?? "1");
            var bp = new NUnitDiscoverySuiteBase(dId, dName, dFullname, dTestcasecount, dRunstate);

            foreach (var propnode in node.Elements("properties").Elements("property"))
            {
                var prop = new NUnitProperty(
                    propnode.Attribute("name").Value,
                    propnode.Attribute("value").Value);
                bp.NUnitDiscoveryProperties.Add(prop);
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
            var runState = node.Attribute(runstate)?.Value switch
            {
                "Runnable" => RunStateEnum.Runnable,
                "Explicit" => RunStateEnum.Explicit,
                "NotRunnable" => RunStateEnum.NotRunnable,
                _ => RunStateEnum.NA
            };
            return runState;
        }
    }
}