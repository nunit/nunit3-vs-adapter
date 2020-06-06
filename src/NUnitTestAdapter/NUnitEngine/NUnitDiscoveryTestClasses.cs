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


// ReSharper disable InconsistentNaming

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    /// <summary>
    /// These classes are those found during the discovery in the execution phase.
    /// </summary>
    public class NUnitDiscoverySuiteBase : INUnitTestCasePropertyInfo
    {
        public string Id { get; }
        public string Name { get; }
        public string FullName { get; }
        public int TestCaseCount { get; }

        protected NUnitDiscoverySuiteBase(string id, string name, string fullname, int count)
        {
            Id = id;
            Name = name;
            FullName = fullname;
            TestCaseCount = count;
        }

        protected NUnitDiscoverySuiteBase(NUnitDiscoverySuiteBase other) : this(other.Id, other.Name, other.FullName,
            other.TestCaseCount)
        {
            RunState = other.RunState;
            foreach (var prop in other.NUnitTestDiscoveryProperties.Properties)
            {
                NUnitTestDiscoveryProperties.Add(prop);
            }
        }
        public RunStateEnum RunState { get; set; }
        public virtual NUnitDiscoverySuiteBase Parent { get; set; }

        public NUnitTestDiscoveryProperties NUnitTestDiscoveryProperties { get; } = new NUnitTestDiscoveryProperties();

        public NUnitDiscoverySuiteBase(string id, string name, string fullname, int count, RunStateEnum runstate)
        : this(id, name, fullname, count)
        {
            RunState = runstate;
        }

        public virtual bool IsExplicit => RunState == RunStateEnum.Explicit;
        public virtual bool IsParameterizedMethod => false;
        public IEnumerable<NUnitProperty> Properties => NUnitTestDiscoveryProperties.Properties;

        public virtual void AddToAllTestCases(NUnitDiscoveryTestCase  tc)
        {
            Parent.AddToAllTestCases(tc);
        }
    }


    public class NUnitDiscoveryTestRun : NUnitDiscoverySuiteBase
    {
        public NUnitDiscoveryTestRun(NUnitDiscoverySuiteBase baseProps) : base(baseProps)
        {
        }

        public NUnitDiscoveryTestAssembly TestAssembly { get; set; }
    }

    public class NUnitTestDiscoveryProperties
    {
        private List<NUnitProperty> TheProperties { get; } = new List<NUnitProperty>();
        public IEnumerable<NUnitProperty> Properties => TheProperties;

        public void Add(NUnitProperty p) => TheProperties.Add(p);

        public bool AllInternal => TheProperties.All(o => o.IsInternal);
    }

    public sealed class NUnitDiscoveryTestSuite : NUnitDiscoveryCanHaveTestFixture
    {
        private readonly List<NUnitDiscoveryTestSuite> testSuites = new List<NUnitDiscoveryTestSuite>();

        private readonly List<NUnitDiscoveryGenericFixture> genericFixtures = new List<NUnitDiscoveryGenericFixture>();
        private readonly List<NUnitDiscoverySetUpFixture> setUpFixtures = new List<NUnitDiscoverySetUpFixture>();
        private readonly List<NUnitDiscoveryParameterizedTestFixture> parameterizedFixtures = new List<NUnitDiscoveryParameterizedTestFixture>();
        public NUnitDiscoveryTestSuite(NUnitDiscoverySuiteBase theBase, NUnitDiscoverySuiteBase parent) : base(theBase)
        {
            Parent = parent;
        }

        public IEnumerable<NUnitDiscoveryTestSuite> TestSuites => testSuites;
        public IEnumerable<NUnitDiscoverySetUpFixture> SetUpFixtures => setUpFixtures;
        public IEnumerable<NUnitDiscoveryParameterizedTestFixture> ParameterizedFixtures => parameterizedFixtures;

        public IEnumerable<NUnitDiscoveryGenericFixture> GenericFixtures => genericFixtures;

        public new bool IsExplicit =>
                RunState == RunStateEnum.Explicit || (
                AreFixturesExplicit &&
                testSuites.All(o => o.IsExplicit) &&
                genericFixtures.All(o => o.IsExplicit) &&
                setUpFixtures.All(o => o.IsExplicit) &&
                parameterizedFixtures.All(o => o.IsExplicit));

        public override int NoOfActualTestCases => base.NoOfActualTestCases
                                              + testSuites.Sum(o => o.NoOfActualTestCases)
                                              + genericFixtures.Sum(o => o.NoOfActualTestCases)
                                              + setUpFixtures.Sum(o => o.NoOfActualTestCases)
                                              + parameterizedFixtures.Sum(o => o.NoOfActualTestCases);

        public void AddTestSuite(NUnitDiscoveryTestSuite ts)
        {
            ts.Parent = this;
            testSuites.Add(ts);
        }

        public void AddTestGenericFixture(NUnitDiscoveryGenericFixture ts)
        {
            ts.Parent = this;
            genericFixtures.Add(ts);
        }
        public void AddSetUpFixture(NUnitDiscoverySetUpFixture ts)
        {
            ts.Parent = this;
            setUpFixtures.Add(ts);
        }
        public void AddParametrizedFixture(NUnitDiscoveryParameterizedTestFixture ts)
        {
            ts.Parent = this;
            parameterizedFixtures.Add(ts);
        }
        public NUnitDiscoveryTestAssembly ParentAssembly { get; set; }
    }



    public class NUnitDiscoveryTestAssembly : NUnitDiscoverySuiteBase
    {
        public NUnitDiscoveryTestAssembly(NUnitDiscoverySuiteBase theBase) : base(theBase)
        {
        }

        private readonly List<NUnitDiscoveryTestSuite> testSuites = new List<NUnitDiscoveryTestSuite>();
        public IEnumerable<NUnitDiscoveryTestSuite> TestSuites => testSuites;

        public new bool IsExplicit =>
            RunState == RunStateEnum.Explicit || testSuites.All(o => o.IsExplicit);

        private readonly List<NUnitDiscoveryTestCase> allTestCases = new List<NUnitDiscoveryTestCase>();

        public IEnumerable<NUnitDiscoveryTestCase> AllTestCases => allTestCases;

        public void AddTestSuite(NUnitDiscoveryTestSuite ts)
        {
            ts.ParentAssembly = this;
            testSuites.Add(ts);
        }

        public override void AddToAllTestCases(NUnitDiscoveryTestCase tc)
        {
            allTestCases.Add(tc);
        }
    }

    public sealed class NUnitDiscoveryTestFixture : NUnitDiscoveryCanHaveTestCases
    {
        private readonly List<NUnitDiscoveryParameterizedMethod> parametrizedMethods = new List<NUnitDiscoveryParameterizedMethod>();
        public IEnumerable<NUnitDiscoveryParameterizedMethod> ParametrizedMethods => parametrizedMethods;

        private readonly List<NUnitDiscoveryTheory> theories = new List<NUnitDiscoveryTheory>();
        public IEnumerable<NUnitDiscoveryTheory> Theories => theories;
        public string ClassName { get; set; }

        public override int NoOfActualTestCases =>
            base.NoOfActualTestCases
            + parametrizedMethods.Sum(o => o.NoOfActualTestCases)
            + theories.Sum(o => o.NoOfActualTestCases);
        public NUnitDiscoveryTestFixture(NUnitDiscoverySuiteBase theBase, string classname, NUnitDiscoveryCanHaveTestFixture parent) : base(theBase)
        {
            Parent = parent;
            ClassName = classname;
        }

        public void AddParametrizedMethod(NUnitDiscoveryParameterizedMethod ts)
        {
            ts.Parent = this;
            parametrizedMethods.Add(ts);
        }


        public void AddTheory(NUnitDiscoveryTheory tc)
        {
            tc.Parent = this;
            theories.Add(tc);
        }
    }

    public sealed class NUnitDiscoveryTestCase : NUnitDiscoverySuiteBase
    {
        public string ClassName { get; set; }
        public string MethodName { get; set; }
        public long Seed { get; set; }

        public NUnitDiscoveryTestCase(NUnitDiscoverySuiteBase theBase, NUnitDiscoveryCanHaveTestCases parent, string methodname, long seed) : base(theBase)
        {
            Parent = parent;
            MethodName = methodname;
            Seed = seed;
        }
    }

    public sealed class NUnitDiscoveryParameterizedMethod : NUnitDiscoveryCanHaveTestCases
    {
        public string ClassName { get; set; }
        public NUnitDiscoveryParameterizedMethod(NUnitDiscoverySuiteBase theBase, string classname, NUnitDiscoveryTestFixture parent) : base(theBase)
        {
            Parent = parent;
            ClassName = classname;
        }

        public override bool IsParameterizedMethod => true;
    }

    public abstract class NUnitDiscoveryCanHaveTestCases : NUnitDiscoverySuiteBase
    {
        private readonly List<NUnitDiscoveryTestCase> testCases = new List<NUnitDiscoveryTestCase>();

        public IEnumerable<NUnitDiscoveryTestCase> TestCases => testCases;
        public virtual int NoOfActualTestCases => testCases.Count;

        public override bool IsExplicit =>
            RunState == RunStateEnum.Explicit || testCases.All(o => o.IsExplicit);

        public void AddTestCase(NUnitDiscoveryTestCase tc)
        {
            tc.Parent = this;
            testCases.Add(tc);
            Parent.AddToAllTestCases(tc);
        }

        protected NUnitDiscoveryCanHaveTestCases(NUnitDiscoverySuiteBase theBase) : base(theBase)
        {
        }
    }

    public abstract class NUnitDiscoveryCanHaveTestFixture : NUnitDiscoverySuiteBase
    {
        private readonly List<NUnitDiscoveryTestFixture> testFixtures = new List<NUnitDiscoveryTestFixture>();

        public IEnumerable<NUnitDiscoveryTestFixture> TestFixtures => testFixtures;

        protected bool AreFixturesExplicit => testFixtures.All(o => o.IsExplicit);
        public override bool IsExplicit =>
            base.IsExplicit || AreFixturesExplicit;

        public virtual int NoOfActualTestCases => testFixtures.Sum(o => o.NoOfActualTestCases);

        public void AddTestFixture(NUnitDiscoveryTestFixture tf)
        {
            tf.Parent = this;
            testFixtures.Add(tf);
        }
        protected NUnitDiscoveryCanHaveTestFixture(NUnitDiscoverySuiteBase theBase) : base(theBase)
        {
        }
    }




    public class NUnitDiscoveryGenericFixture : NUnitDiscoveryCanHaveTestFixture
    {
        public NUnitDiscoveryGenericFixture(NUnitDiscoverySuiteBase theBase) : base(theBase)
        {
        }
    }

    public sealed class NUnitDiscoveryParameterizedTestFixture : NUnitDiscoveryCanHaveTestFixture
    {
        public NUnitDiscoveryParameterizedTestFixture(NUnitDiscoverySuiteBase theBase, NUnitDiscoveryCanHaveTestFixture parent) : base(theBase)
        {
            Parent = parent;
        }
    }

    public sealed class NUnitDiscoverySetUpFixture : NUnitDiscoveryCanHaveTestFixture
    {
        public string ClassName { get; }
        public NUnitDiscoverySetUpFixture(NUnitDiscoverySuiteBase theBase, string classname, NUnitDiscoveryCanHaveTestFixture parent) : base(theBase)
        {
            Parent = parent;
            ClassName = classname;
        }
    }

    public sealed class NUnitDiscoveryTheory : NUnitDiscoveryCanHaveTestCases
    {
        public string ClassName { get; set; }
        // public IEnumerable<NUnitDiscoveryTestCase> TestCases { get; } = new List<NUnitDiscoveryTestCase>();

        public NUnitDiscoveryTheory(NUnitDiscoverySuiteBase theBase, string classname, NUnitDiscoveryTestFixture parent) : base(theBase)
        {
            Parent = parent;
            ClassName = classname;
        }
    }
}
