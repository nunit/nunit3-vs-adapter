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
    public interface INUnitDiscoverySuiteBase : INUnitTestCasePropertyInfo
    {
        string Id { get; }
        string Name { get; }
        string FullName { get; }
        int TestCaseCount { get; }
        INUnitDiscoverySuiteBase Parent { get; }
        NUnitDiscoveryProperties NUnitDiscoveryProperties { get; }
        bool IsExplicit { get; }
        bool IsExplicitReverse { get; }

        bool IsParameterizedMethod { get; }
        void AddToAllTestCases(NUnitDiscoveryTestCase tc);
    }

    public abstract class NUnitDiscoverySuiteBase : INUnitDiscoverySuiteBase
    {
        public string Id { get; }
        public string Name { get; }
        public string FullName { get; }
        public int TestCaseCount { get; }
        public RunStateEnum RunState { get; }
        public INUnitDiscoverySuiteBase Parent { get; }


        private NUnitDiscoverySuiteBase(string id, string name, string fullname, int count)
        {
            Id = id;
            Name = name;
            FullName = fullname;
            TestCaseCount = count;
        }

        protected NUnitDiscoverySuiteBase(BaseProperties other)
            : this(other.Id, other.Name, other.Fullname, other.TestCaseCount)
        {
            RunState = other.RunState;
            foreach (var prop in other.Properties)
            {
                NUnitDiscoveryProperties.Add(prop);
            }
        }

        protected NUnitDiscoverySuiteBase(BaseProperties other, INUnitDiscoverySuiteBase parent) : this(other)
        {
            Parent = parent;
        }

        public NUnitDiscoveryProperties NUnitDiscoveryProperties { get; } = new NUnitDiscoveryProperties();

        public abstract bool IsExplicit { get; }
        public virtual bool IsExplicitReverse => RunState == RunStateEnum.Explicit || (Parent?.IsExplicitReverse ?? RunState == RunStateEnum.Explicit);
        public virtual bool IsParameterizedMethod => false;
        public IEnumerable<NUnitProperty> Properties => NUnitDiscoveryProperties.Properties;

        public virtual void AddToAllTestCases(NUnitDiscoveryTestCase tc)
        {
            Parent.AddToAllTestCases(tc);
        }
    }


    public class NUnitDiscoveryTestRun : NUnitDiscoverySuiteBase
    {
        public NUnitDiscoveryTestRun(BaseProperties baseProps) : base(baseProps)
        {
        }

        public NUnitDiscoveryTestAssembly TestAssembly { get; set; }
        public override bool IsExplicit =>
            RunState == RunStateEnum.Explicit || TestAssembly.IsExplicit;

        public void AddTestAssembly(NUnitDiscoveryTestAssembly testAssembly)
        {
            TestAssembly = testAssembly;
        }
    }

    public class NUnitDiscoveryProperties
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
        public NUnitDiscoveryTestSuite(BaseProperties theBase, INUnitDiscoverySuiteBase parent) : base(theBase, parent)
        {
        }

        public IEnumerable<NUnitDiscoveryTestSuite> TestSuites => testSuites;
        public IEnumerable<NUnitDiscoverySetUpFixture> SetUpFixtures => setUpFixtures;
        public IEnumerable<NUnitDiscoveryParameterizedTestFixture> ParameterizedFixtures => parameterizedFixtures;

        public IEnumerable<NUnitDiscoveryGenericFixture> GenericFixtures => genericFixtures;

        public override bool IsExplicit
        {
            get
            {
                var fullList = new List<NUnitDiscoverySuiteBase>();
                fullList.AddRange(TestFixtures.Cast<NUnitDiscoverySuiteBase>());
                fullList.AddRange(TestSuites.Cast<NUnitDiscoverySuiteBase>());
                fullList.AddRange(GenericFixtures.Cast<NUnitDiscoverySuiteBase>());
                fullList.AddRange(SetUpFixtures.Cast<NUnitDiscoverySuiteBase>());
                fullList.AddRange(ParameterizedFixtures.Cast<NUnitDiscoverySuiteBase>());
                return RunState == RunStateEnum.Explicit || fullList.All(o => o.IsExplicit);
            }
        }

        public override int NoOfActualTestCases => base.NoOfActualTestCases
                                                   + testSuites.Sum(o => o.NoOfActualTestCases)
                                                   + genericFixtures.Sum(o => o.NoOfActualTestCases)
                                                   + setUpFixtures.Sum(o => o.NoOfActualTestCases)
                                                   + parameterizedFixtures.Sum(o => o.NoOfActualTestCases);

        public void AddTestSuite(NUnitDiscoveryTestSuite ts)
        {
            testSuites.Add(ts);
        }

        public void AddTestGenericFixture(NUnitDiscoveryGenericFixture ts)
        {
            genericFixtures.Add(ts);
        }
        public void AddSetUpFixture(NUnitDiscoverySetUpFixture ts)
        {
            setUpFixtures.Add(ts);
        }
        public void AddParameterizedFixture(NUnitDiscoveryParameterizedTestFixture ts)
        {
            parameterizedFixtures.Add(ts);
        }
        public NUnitDiscoveryTestAssembly ParentAssembly { get; set; }
    }



    public sealed class NUnitDiscoveryTestAssembly : NUnitDiscoveryCanHaveTestFixture
    {
        public NUnitDiscoveryTestAssembly(BaseProperties theBase, NUnitDiscoveryTestRun parent) : base(theBase, parent)
        { }

        private readonly List<NUnitDiscoveryTestSuite> testSuites = new List<NUnitDiscoveryTestSuite>();
        public IEnumerable<NUnitDiscoveryTestSuite> TestSuites => testSuites;

        public override bool IsExplicit =>
            RunState == RunStateEnum.Explicit || testSuites.AllWithEmptyFalse(o => o.IsExplicit);

        private readonly List<NUnitDiscoveryTestCase> allTestCases = new List<NUnitDiscoveryTestCase>();

        /// <summary>
        /// If all testcases are Explicit, we can run this one.
        /// </summary>
        public IEnumerable<NUnitDiscoveryTestCase> AllTestCases => allTestCases;

        /// <summary>
        /// If there are a mixture of explicit and non-explicit, this one will filter out the explicit ones.
        /// </summary>
        public IEnumerable<NUnitDiscoveryTestCase> RunnableTestCases => allTestCases.Where(c => !c.IsExplicitReverse);

        public void AddTestSuiteToAssembly(NUnitDiscoveryTestSuite ts)
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
        private readonly List<NUnitDiscoveryParameterizedMethod> parameterizedMethods = new List<NUnitDiscoveryParameterizedMethod>();
        public IEnumerable<NUnitDiscoveryParameterizedMethod> ParameterizedMethods => parameterizedMethods;

        private readonly List<NUnitDiscoveryTheory> theories = new List<NUnitDiscoveryTheory>();

        private readonly List<NUnitDiscoveryGenericMethod> genericMethods = new List<NUnitDiscoveryGenericMethod>();
        public IEnumerable<NUnitDiscoveryTheory> Theories => theories;

        public override int NoOfActualTestCases =>
            base.NoOfActualTestCases
            + parameterizedMethods.Sum(o => o.NoOfActualTestCases)
            + theories.Sum(o => o.NoOfActualTestCases)
            + genericMethods.Sum(o => o.NoOfActualTestCases);

        public override bool IsExplicit =>
            base.IsExplicit
            || parameterizedMethods.AllWithEmptyFalse(o => o.IsExplicit)
            || theories.AllWithEmptyFalse(o => o.IsExplicit)
            || genericMethods.AllWithEmptyFalse(o => o.IsExplicit);

        public IEnumerable<NUnitDiscoveryGenericMethod> GenericMethods => genericMethods;

        public NUnitDiscoveryTestFixture(BaseProperties theBase, string classname, INUnitDiscoverySuiteBase parent) : base(theBase, parent, classname)
        {
        }

        public void AddParameterizedMethod(NUnitDiscoveryParameterizedMethod ts)
        {
            parameterizedMethods.Add(ts);
        }


        public void AddTheory(NUnitDiscoveryTheory tc)
        {
            theories.Add(tc);
        }

        public void AddGenericMethod(NUnitDiscoveryGenericMethod tc)
        {
            genericMethods.Add(tc);
        }
    }

    public interface INUnitDiscoveryTestCase : INUnitDiscoverySuiteBase
    {
        string ClassName { get;  }
        string MethodName { get;  }
        long Seed { get; set; }
    }

    public sealed class NUnitDiscoveryTestCase : NUnitDiscoverySuiteBase, INUnitDiscoveryTestCase
    {
        public string ClassName { get; }
        public string MethodName { get; set; }
        public long Seed { get; set; }

        public NUnitDiscoveryTestCase(BaseProperties theBase, INUnitDiscoveryCanHaveTestCases parent,
            string className,
            string methodname, long seed) : base(theBase, parent)
        {
            ClassName = className;
            MethodName = methodname;
            Seed = seed;
        }

        public override bool IsExplicit => RunState == RunStateEnum.Explicit;
    }

    public sealed class NUnitDiscoveryParameterizedMethod : NUnitDiscoveryCanHaveTestCases
    {
        public NUnitDiscoveryParameterizedMethod(BaseProperties theBase, string classname, INUnitDiscoverySuiteBase parent) : base(theBase, parent, classname)
        {
        }

        public override bool IsParameterizedMethod => true;
    }

    public interface INUnitDiscoveryCanHaveTestCases : INUnitDiscoverySuiteBase
    {
        IEnumerable<NUnitDiscoveryTestCase> TestCases { get; }
        int NoOfActualTestCases { get; }
        void AddTestCase(NUnitDiscoveryTestCase tc);
    }

    public abstract class NUnitDiscoveryCanHaveTestCases : NUnitDiscoverySuiteBase, INUnitDiscoveryCanHaveTestCases
    {
        private readonly List<NUnitDiscoveryTestCase> testCases = new List<NUnitDiscoveryTestCase>();

        public IEnumerable<NUnitDiscoveryTestCase> TestCases => testCases;
        public virtual int NoOfActualTestCases => testCases.Count;

        public override bool IsExplicit =>
            RunState == RunStateEnum.Explicit || testCases.AllWithEmptyFalse(o => o.IsExplicit);

        public string ClassName { get; }

        public void AddTestCase(NUnitDiscoveryTestCase tc)
        {
            testCases.Add(tc);
            Parent.AddToAllTestCases(tc);
        }

        protected NUnitDiscoveryCanHaveTestCases(BaseProperties theBase, INUnitDiscoverySuiteBase parent, string classname) : base(theBase, parent)
        {
            ClassName = classname;
        }
    }

    public interface INUnitDiscoveryCanHaveTestFixture : INUnitDiscoverySuiteBase
    {
        IEnumerable<NUnitDiscoveryTestFixture> TestFixtures { get; }
        int NoOfActualTestCases { get; }
        void AddTestFixture(NUnitDiscoveryTestFixture tf);
    }

    public abstract class NUnitDiscoveryCanHaveTestFixture : NUnitDiscoverySuiteBase, INUnitDiscoveryCanHaveTestFixture
    {
        private readonly List<NUnitDiscoveryTestFixture> testFixtures = new List<NUnitDiscoveryTestFixture>();

        public IEnumerable<NUnitDiscoveryTestFixture> TestFixtures => testFixtures;

        public override bool IsExplicit
        {
            get
            {
                if (RunState == RunStateEnum.Explicit)
                    return true;
                if (testFixtures.Any())
                    return testFixtures.All(o => o.IsExplicit);

                return false;
            }
        }

        public virtual int NoOfActualTestCases => testFixtures.Sum(o => o.NoOfActualTestCases);

        public void AddTestFixture(NUnitDiscoveryTestFixture tf)
        {
            testFixtures.Add(tf);
        }
        protected NUnitDiscoveryCanHaveTestFixture(BaseProperties theBase, INUnitDiscoverySuiteBase parent) : base(theBase, parent)
        {
        }
    }

    public sealed class NUnitDiscoveryGenericFixture : NUnitDiscoveryCanHaveTestFixture
    {
        public NUnitDiscoveryGenericFixture(BaseProperties theBase, INUnitDiscoverySuiteBase parent) : base(theBase, parent)
        {
        }
    }

    public sealed class NUnitDiscoveryParameterizedTestFixture : NUnitDiscoveryCanHaveTestFixture
    {
        public NUnitDiscoveryParameterizedTestFixture(BaseProperties theBase, NUnitDiscoveryCanHaveTestFixture parent) : base(theBase, parent)
        {
        }
    }

    public sealed class NUnitDiscoverySetUpFixture : NUnitDiscoveryCanHaveTestFixture
    {
        public string ClassName { get; }
        public NUnitDiscoverySetUpFixture(BaseProperties theBase, string classname, NUnitDiscoveryCanHaveTestFixture parent) : base(theBase, parent)
        {
            ClassName = classname;
        }
    }

    public sealed class NUnitDiscoveryTheory : NUnitDiscoveryCanHaveTestCases
    {
        public NUnitDiscoveryTheory(BaseProperties theBase, string classname, INUnitDiscoverySuiteBase parent) : base(theBase,parent, classname)
        {
        }
    }

    public sealed class NUnitDiscoveryGenericMethod : NUnitDiscoveryCanHaveTestCases
    {
        public NUnitDiscoveryGenericMethod(BaseProperties theBase, string classname, INUnitDiscoverySuiteBase parent) : base(theBase, parent, classname)
        {
        }
    }
}
