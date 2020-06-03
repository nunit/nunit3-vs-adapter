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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;

// ReSharper disable InconsistentNaming

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{

    /// <summary>
    /// These classes are those found during the discovery in the execution phase.
    /// </summary>

    public class NUnitDiscoverySuiteBase
    {
        public string Id { get; }
        public string Name { get; }
        public string FullName { get; }
        public int TestCaseCount { get; }

        public NUnitDiscoverySuiteBase(string id, string name, string fullname, int count)
        {
            Id = id;
            Name = name;
            FullName = fullname;
            TestCaseCount = count;
        }

        public NUnitDiscoverySuiteBase(NUnitDiscoverySuiteBase other) : this(other.Id, other.Name, other.FullName,
            other.TestCaseCount)
        {

        }
    }

    public class NUnitDiscoverySuiteBaseProperties : NUnitDiscoverySuiteBase
    {
        public NUnitEventTestCase.eRunState Runstate { get; set; }
        public virtual NUnitDiscoverySuiteBaseProperties Parent { get; set; }

        public NUnitTestDiscoveryProperties NUnitTestDiscoveryProperties { get; } = new NUnitTestDiscoveryProperties();

        public NUnitDiscoverySuiteBaseProperties(string id, string name, string fullname, int count, NUnitEventTestCase.eRunState runstate)
        : base(id, name, fullname, count)
        {
            Runstate = runstate;
        }

        public NUnitDiscoverySuiteBaseProperties(NUnitDiscoverySuiteBaseProperties theBase) : base(theBase)
        {
            Runstate = theBase.Runstate;
            foreach (var prop in theBase.NUnitTestDiscoveryProperties.Properties)
            {
                NUnitTestDiscoveryProperties.Add(prop);
            }
        }
    }

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

#pragma warning restore SA1303 // Const field names should begin with upper-case letter

        public NUnitDiscoveryTestRun Convert(NUnitResults discovery, TestConverter converter)
        {
            var doc = XDocument.Load(new XmlNodeReader(discovery.FullTopNode));
            var testrun = ExtractTestRun(doc);
            var anode = doc.Root.Elements("test-suite");
            var assemblyNode = anode.Single(o => o.Attribute("type").Value == "Assembly");
            var testassembly = ExtractTestAssembly(assemblyNode);
            testrun.TestAssembly = testassembly;
            foreach (var node in assemblyNode.Elements())
            {
                var type = node.Attribute("test-suite").Value;
                switch (type)
                {
                    case "test-fixture":
                        CreateTestFixture(node, testassembly);
                        break;
                }
            }

            return testrun;
        }

        private void CreateTestFixture(XElement node, NUnitDiscoveryTestSuite parent)
        {
            var b = ExtractSuiteBasePropertiesClass(node);
            var cn = node.Attribute(classname).Value;
            var tf = new NUnitDiscoveryTestFixture(b, cn) {Parent = parent};
            foreach (var tc in node.Elements("test-case"))
            {

            }

            parent.AddTestFixture(tf);
        }

        private NUnitDiscoveryTestAssembly ExtractTestAssembly(XElement node)
        {
            string d_type = node.Attribute(type).Value;
            if (d_type != "Assembly")
                throw new DiscoveryException("Node is not of type assembly: " + node);
            var a_base = ExtractSuiteBasePropertiesClass(node);
            return new NUnitDiscoveryTestAssembly(a_base);
        }

        private NUnitDiscoverySuiteBaseProperties ExtractSuiteBasePropertiesClass(XElement node)
        {
            string d_id = node.Attribute(id).Value;
            string d_name = node.Attribute(name).Value;
            string d_fullname = node.Attribute(fullname).Value;
            var d_runstate = ExtractRunState(node);
            char apo = (char)0x22;
            var tcs = node.Attribute(testcasecount).Value.Trim(apo);
            int d_testcasecount = int.Parse(tcs);
            var bp = new NUnitDiscoverySuiteBaseProperties(d_id, d_name, d_fullname, d_testcasecount, d_runstate);

            foreach (var propnode in node.Elements("properties").Elements("property"))
            {
                var prop = new NUnitTestDiscoveryProperty(
                    propnode.Attribute("name").Value,
                    propnode.Attribute("value").Value);
                bp.NUnitTestDiscoveryProperties.Add(prop);
            }
            return bp;
        }


        private NUnitDiscoveryTestRun ExtractTestRun(XDocument node)
        {
            var sb = ExtractSuiteBasePropertiesClass(node.Root);
            var tr = new NUnitDiscoveryTestRun(sb);
            return tr;
        }


        private NUnitEventTestCase.eRunState ExtractRunState(XElement node)
        {
            var runState = node.Attribute(runstate)?.ToString() switch
            {
                "Runnable" => NUnitEventTestCase.eRunState.Runnable,
                "Explicit" => NUnitEventTestCase.eRunState.Explicit,
                _ => NUnitEventTestCase.eRunState.NA
            };
            return runState;
        }
    }


    [Serializable]
    public class DiscoveryException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public DiscoveryException()
        {
        }

        public DiscoveryException(string message) : base(message)
        {
        }

        public DiscoveryException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DiscoveryException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    public class NUnitDiscoveryTestRun : NUnitDiscoverySuiteBaseProperties
    {
        public NUnitDiscoveryTestRun(NUnitDiscoverySuiteBaseProperties baseProps) : base(baseProps)
        {
        }

        public IEnumerable<NUnitDiscoveryTestSuite> TestSuites { get; }
        public NUnitDiscoveryTestAssembly TestAssembly { get; set; }
    }

    public class NUnitTestDiscoveryProperties
    {
        private List<NUnitTestDiscoveryProperty> TheProperties { get; } = new List<NUnitTestDiscoveryProperty>();
        public IEnumerable<NUnitTestDiscoveryProperty> Properties => TheProperties;

        public void Add(NUnitTestDiscoveryProperty p) => TheProperties.Add(p);

        public bool AllInternal => TheProperties.All(o => o.IsInternal);
    }

    public class NUnitTestDiscoveryProperty
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public bool IsInternal => Name.StartsWith("_");

        public NUnitTestDiscoveryProperty(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    public class NUnitDiscoveryTestSuiteBase : NUnitDiscoverySuiteBase
    {
        private List<NUnitDiscoveryTestSuite> testSuites = new List<NUnitDiscoveryTestSuite>();
        private List<NUnitDiscoveryTestFixture> testFixtures = new List<NUnitDiscoveryTestFixture>();

        private List<NUnitDiscoveryGenericFixture> genericFixtures = new List<NUnitDiscoveryGenericFixture>();
        public NUnitDiscoveryTestSuiteBase(NUnitDiscoverySuiteBase theBase) : base(theBase)
        {
        }

        public IEnumerable<NUnitDiscoveryTestSuite> TestSuites => testSuites;

        public IEnumerable<NUnitDiscoveryTestFixture> TestFixtures => testFixtures;

        public IEnumerable<NUnitDiscoveryGenericFixture> GenericFixtures => genericFixtures;


        public void AddTestSuite(NUnitDiscoveryTestSuite ts) => testSuites.Add(ts);
        public void AddTestFixture(NUnitDiscoveryTestFixture ts) => testFixtures.Add(ts);
        public void AddTestGenericFixture(NUnitDiscoveryGenericFixture ts) => genericFixtures.Add(ts);
    }

    public class NUnitDiscoveryTestSuite : NUnitDiscoveryTestSuiteBase
    {
        public NUnitDiscoveryTestSuite(NUnitDiscoverySuiteBaseProperties theBase) : base(theBase)
        {
        }
    }

    public class NUnitDiscoveryTestAssembly : NUnitDiscoverySuiteBaseProperties
    {
        public NUnitDiscoveryTestAssembly(NUnitDiscoverySuiteBaseProperties theBase) : base(theBase)
        {
        }

        private List<NUnitDiscoveryTestSuite> testSuites = new List<NUnitDiscoveryTestSuite>();
        public IEnumerable<NUnitDiscoveryTestSuite> TestSuites => testSuites;

        public void AddTestSuite(NUnitDiscoveryTestSuite ts) => testSuites.Add(ts);

    }


    public class NUnitDiscoveryTestFixture : NUnitDiscoverySuiteBaseProperties
    {
        private  List<NUnitDiscoveryTestCase> testCases  = new List<NUnitDiscoveryTestCase>();
        public IEnumerable<NUnitDiscoveryParametrizedMethod> ParametrizedMethods { get; } = new List<NUnitDiscoveryParametrizedMethod>();

        public string ClassName { get; set; }

        public IEnumerable<NUnitDiscoveryTestCase> TestCases => testCases;

        public NUnitDiscoveryTestFixture(NUnitDiscoverySuiteBaseProperties theBase, string classname) : base(theBase)
        {
            ClassName = classname;
        }

        public void AddTestCase(NUnitDiscoveryTestCase ts) => testCases.Add(ts);

    }

    public class NUnitDiscoveryTestCase : NUnitDiscoverySuiteBaseProperties
    {

        public string MethodName { get; set; }
        public long Seed { get; set; }

        public NUnitDiscoveryTestCase(NUnitDiscoverySuiteBaseProperties theBase, string methodname, long seed) : base(theBase)
        {
            MethodName = methodname;
            Seed = seed;
        }
    }

    public class NUnitDiscoveryParametrizedMethod : NUnitDiscoverySuiteBaseProperties
    {
        public string ClassName { get; set; }
        public IEnumerable<NUnitDiscoveryTestCase> TestCases { get; } = new List<NUnitDiscoveryTestCase>();

        public NUnitDiscoveryParametrizedMethod(NUnitDiscoverySuiteBaseProperties theBase, string classname) : base(theBase)
        {
            ClassName = classname;
        }
    }

    public class NUnitDiscoveryGenericFixture : NUnitDiscoverySuiteBaseProperties
    {
        public IEnumerable<NUnitDiscoveryTestFixture> TestFixtures { get; } = new List<NUnitDiscoveryTestFixture>();

        public NUnitDiscoveryGenericFixture(NUnitDiscoverySuiteBaseProperties theBase) : base(theBase)
        {
        }

    }

    public class NUnitDiscoverySetUpFixture : NUnitDiscoverySuiteBaseProperties
    {
        public string ClassName { get; set; }
        public IEnumerable<NUnitDiscoveryTestFixture> TestFixtures { get; } = new List<NUnitDiscoveryTestFixture>();
        public NUnitDiscoverySetUpFixture(NUnitDiscoverySuiteBaseProperties theBase, string classname) : base(theBase)
        {
            ClassName = classname;
        }
    }

    public class NUnitDiscoveryTheory : NUnitDiscoverySuiteBaseProperties
    {
        public string ClassName { get; set; }
        public IEnumerable<NUnitDiscoveryTestCase> TestCases { get; } = new List<NUnitDiscoveryTestCase>();

        public NUnitDiscoveryTheory(NUnitDiscoverySuiteBaseProperties theBase, string classname) : base(theBase)
        {
            ClassName = classname;
        }
    }


}
