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
using System.Linq;


// ReSharper disable InconsistentNaming

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine;

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

    public NUnitDiscoveryProperties NUnitDiscoveryProperties { get; } = new();

    public abstract bool IsExplicit { get; }
    public virtual bool IsExplicitReverse => RunState == RunStateEnum.Explicit || (Parent?.IsExplicitReverse ?? RunState == RunStateEnum.Explicit);
    public virtual bool IsParameterizedMethod => false;
    public IEnumerable<NUnitProperty> Properties => NUnitDiscoveryProperties.Properties;

    public virtual void AddToAllTestCases(NUnitDiscoveryTestCase tc)
    {
        Parent.AddToAllTestCases(tc);
    }
}


public class NUnitDiscoveryTestRun(BaseProperties baseProps) : NUnitDiscoverySuiteBase(baseProps)
{
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
    private List<NUnitProperty> TheProperties { get; } = [];
    public IEnumerable<NUnitProperty> Properties => TheProperties;

    public void Add(NUnitProperty p) => TheProperties.Add(p);

    public bool AllInternal => TheProperties.All(o => o.IsInternal);
}

public class NUnitDiscoveryTestSuite(BaseProperties theBase, INUnitDiscoverySuiteBase parent)
    : NUnitDiscoveryCanHaveTestFixture(theBase, parent)
{
    public NUnitDiscoveryTestAssembly ParentAssembly { get; set; }
}



public sealed class NUnitDiscoveryTestAssembly(BaseProperties theBase, NUnitDiscoveryTestRun parent)
    : NUnitDiscoveryTestSuite(theBase, parent)
{
    private readonly List<NUnitDiscoveryTestCase> allTestCases = [];

    /// <summary>
    /// If all testcases are Explicit, we can run this one.
    /// </summary>
    public IEnumerable<NUnitDiscoveryTestCase> AllTestCases => allTestCases;

    /// <summary>
    /// If there are a mixture of explicit and non-explicit, this one will filter out the explicit ones.
    /// </summary>
    public IEnumerable<NUnitDiscoveryTestCase> RunnableTestCases => allTestCases.Where(c => !c.IsExplicitReverse);

    public int NoOfExplicitTestCases => allTestCases.Count(c => c.IsExplicitReverse);

    public void AddTestSuiteToAssembly(NUnitDiscoveryTestSuite ts)
    {
        AddTestSuite(ts);
    }

    public override void AddToAllTestCases(NUnitDiscoveryTestCase tc)
    {
        allTestCases.Add(tc);
    }
}

public sealed class NUnitDiscoveryTestFixture(BaseProperties theBase, string classname, INUnitDiscoverySuiteBase parent)
    : NUnitDiscoveryCanHaveTestCases(theBase, parent, classname)
{
    private readonly List<NUnitDiscoveryParameterizedMethod> parameterizedMethods = [];
    public IEnumerable<NUnitDiscoveryParameterizedMethod> ParameterizedMethods => parameterizedMethods;

    private readonly List<NUnitDiscoveryTheory> theories = [];

    private readonly List<NUnitDiscoveryGenericMethod> genericMethods = [];
    public IEnumerable<NUnitDiscoveryTheory> Theories => theories;

    public override int NoOfActualTestCases =>
        base.NoOfActualTestCases
        + parameterizedMethods.Sum(o => o.NoOfActualTestCases)
        + theories.Sum(o => o.NoOfActualTestCases)
        + genericMethods.Sum(o => o.NoOfActualTestCases);

    public override bool IsExplicit
    {
        get
        {
            if (RunState == RunStateEnum.Explicit)
                return true;
            var all = new List<NUnitDiscoverySuiteBase>();
            all.AddRange(TestCases);
            all.AddRange(parameterizedMethods);
            all.AddRange(theories);
            all.AddRange(genericMethods);
            return all.All(o => o.IsExplicit);
        }
    }

    public IEnumerable<NUnitDiscoveryGenericMethod> GenericMethods => genericMethods;

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
    string ClassName { get; }
    string MethodName { get; }
    long Seed { get; set; }
}

/// <summary>
/// Interface for common properties between event testcase and discoverytestcase.
/// </summary>
public interface INUnitCommonTestCase
{
    string Id { get; }
    string Name { get; }
    string FullName { get; }
    string ClassName { get; }
    string MethodName { get; }
    long Seed { get; }
}

public sealed class NUnitDiscoveryTestCase(
    BaseProperties theBase,
    INUnitDiscoveryCanHaveTestCases parent,
    string className,
    string methodname,
    long seed)
    : NUnitDiscoverySuiteBase(theBase, parent), INUnitDiscoveryTestCase, INUnitCommonTestCase
{
    public string ClassName { get; } = className;
    public string MethodName { get; set; } = methodname;
    public long Seed { get; set; } = seed;

    public override bool IsExplicit => RunState == RunStateEnum.Explicit;
}

public sealed class NUnitDiscoveryParameterizedMethod(
    BaseProperties theBase,
    string classname,
    INUnitDiscoverySuiteBase parent)
    : NUnitDiscoveryCanHaveTestCases(theBase, parent, classname)
{
    public override bool IsParameterizedMethod => true;
}

public interface INUnitDiscoveryCanHaveTestCases : INUnitDiscoverySuiteBase
{
    IEnumerable<NUnitDiscoveryTestCase> TestCases { get; }
    int NoOfActualTestCases { get; }
    void AddTestCase(NUnitDiscoveryTestCase tc);
}

public abstract class NUnitDiscoveryCanHaveTestCases(
    BaseProperties theBase,
    INUnitDiscoverySuiteBase parent,
    string classname)
    : NUnitDiscoverySuiteBase(theBase, parent), INUnitDiscoveryCanHaveTestCases
{
    private readonly List<NUnitDiscoveryTestCase> testCases = [];

    public IEnumerable<NUnitDiscoveryTestCase> TestCases => testCases;
    public virtual int NoOfActualTestCases => testCases.Count;

    public override bool IsExplicit =>
        RunState == RunStateEnum.Explicit || testCases.AllWithEmptyFalse(o => o.IsExplicit);

    public string ClassName { get; } = classname;

    public void AddTestCase(NUnitDiscoveryTestCase tc)
    {
        testCases.Add(tc);
        Parent.AddToAllTestCases(tc);
    }
}

public interface INUnitDiscoveryCanHaveTestFixture : INUnitDiscoverySuiteBase
{
    IEnumerable<NUnitDiscoveryTestFixture> TestFixtures { get; }
    int NoOfActualTestCases { get; }
    void AddTestFixture(NUnitDiscoveryTestFixture tf);
}

public abstract class NUnitDiscoveryCanHaveTestFixture(BaseProperties theBase, INUnitDiscoverySuiteBase parent)
    : NUnitDiscoverySuiteBase(theBase, parent), INUnitDiscoveryCanHaveTestFixture
{
    private readonly List<NUnitDiscoveryTestFixture> testFixtures = [];

    public IEnumerable<NUnitDiscoveryTestFixture> TestFixtures => testFixtures;

    public void AddTestFixture(NUnitDiscoveryTestFixture tf)
    {
        testFixtures.Add(tf);
    }


    private readonly List<NUnitDiscoveryTestSuite> testSuites = [];

    private readonly List<NUnitDiscoveryGenericFixture> genericFixtures = [];
    private readonly List<NUnitDiscoverySetUpFixture> setUpFixtures = [];
    private readonly List<NUnitDiscoveryParameterizedTestFixture> parameterizedFixtures = [];


    public IEnumerable<NUnitDiscoveryTestSuite> TestSuites => testSuites;
    public IEnumerable<NUnitDiscoverySetUpFixture> SetUpFixtures => setUpFixtures;
    public IEnumerable<NUnitDiscoveryParameterizedTestFixture> ParameterizedFixtures => parameterizedFixtures;

    public IEnumerable<NUnitDiscoveryGenericFixture> GenericFixtures => genericFixtures;

    public override bool IsExplicit
    {
        get
        {
            if (RunState == RunStateEnum.Explicit)
                return true;
            var fullList = new List<NUnitDiscoverySuiteBase>();
            fullList.AddRange(TestFixtures);
            fullList.AddRange(TestSuites);
            fullList.AddRange(GenericFixtures);
            fullList.AddRange(SetUpFixtures);
            fullList.AddRange(ParameterizedFixtures);
            return fullList.All(o => o.IsExplicit);
        }
    }

    public int NoOfActualTestCases => testFixtures.Sum(o => o.NoOfActualTestCases)
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
}

public sealed class NUnitDiscoveryGenericFixture(BaseProperties theBase, INUnitDiscoverySuiteBase parent)
    : NUnitDiscoveryCanHaveTestFixture(theBase, parent);

public sealed class NUnitDiscoveryParameterizedTestFixture(
    BaseProperties theBase,
    NUnitDiscoveryCanHaveTestFixture parent)
    : NUnitDiscoveryCanHaveTestFixture(theBase, parent);

public sealed class NUnitDiscoverySetUpFixture(
    BaseProperties theBase,
    string classname,
    NUnitDiscoveryCanHaveTestFixture parent)
    : NUnitDiscoveryCanHaveTestFixture(theBase, parent)
{
    public string ClassName { get; } = classname;
}

public sealed class NUnitDiscoveryTheory(BaseProperties theBase, string classname, INUnitDiscoverySuiteBase parent)
    : NUnitDiscoveryCanHaveTestCases(theBase, parent, classname);

public sealed class NUnitDiscoveryGenericMethod(
    BaseProperties theBase,
    string classname,
    INUnitDiscoverySuiteBase parent)
    : NUnitDiscoveryCanHaveTestCases(theBase, parent, classname);