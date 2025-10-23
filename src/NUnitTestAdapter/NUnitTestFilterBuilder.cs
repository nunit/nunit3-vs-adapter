// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, Terje Sandstrom
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

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter;

public class NUnitTestFilterBuilder(ITestFilterService filterService, IAdapterSettings settings, IDumpXml dump = null)
{
    private readonly ITestFilterService _filterService = filterService ?? throw new NUnitEngineException("TestFilterService is not available. Engine in use is incorrect version.");

    // ReSharper disable once StringLiteralTypo
    public static readonly TestFilter NoTestsFound = new("<notestsfound/>");

    public TestFilter ConvertMsFilterToNUnitFilter(IVsTestFilter vsFilter, IList<TestCase> loadedTestCases)
    {
        var filteredTestCases = vsFilter.CheckFilter(loadedTestCases);
        var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
        // TestLog.Info(string.Format("Microsoft Filter detected: LoadedTestCases {0}, Filtered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
        return testCases.Any() ? FilterByList(testCases) : NoTestsFound;
    }


    public TestFilter ConvertVsTestFilterToNUnitFilter(IVsTestFilter vsFilter, IDiscoveryConverter discovery)
    {
        if (settings.DiscoveryMethod == DiscoveryMethod.Legacy)
            return ConvertMsFilterToNUnitFilter(vsFilter, discovery.LoadedTestCases);
        if (!settings.UseNUnitFilter)
            return ConvertMsFilterToNUnitFilter(vsFilter, discovery);
        var result = ConvertVsTestFilterToNUnitFilter(vsFilter);
        return result ?? ConvertMsFilterToNUnitFilter(vsFilter, discovery);
    }

    /// <summary>
    /// Used when running from command line, mode Non-Ide,  e.g. 'dotnet test --filter xxxxx'.  Reads the MSTestCaseFilterExpression.
    /// </summary>
    public TestFilter ConvertVsTestFilterToNUnitFilter(IVsTestFilter vsFilter)
    {
        if (string.IsNullOrEmpty(vsFilter?.MsTestCaseFilterExpression?.TestCaseFilterValue))
            return null;
        var parser = new TestFilterParser();
        var filter = parser.Parse(vsFilter.MsTestCaseFilterExpression.TestCaseFilterValue);
        var tf = new TestFilter(filter);
        if (settings.ExplicitMode == ExplicitModeEnum.None)
        {
            var tfExplicitNone = new TestFilter("<not><prop name='Explicit'>true</prop></not>");
            var combiner = new TestFilterCombiner(tf, tfExplicitNone);
            return combiner.GetFilter();
        }
        return tf;
    }

    /// <summary>
    /// Used when running from Ide under MTP. Using the Microsoft Test Case Filter expression.
    /// </summary>
    public TestFilter ConvertVsTestFilterToNUnitFilterForMTP(IVsTestFilter vsFilter)
    {
        var msFilter = vsFilter?.MsTestCaseFilterExpression.TestCaseFilterValue;
        dump?.AddString($@"\n\n<MsFilter>\n{msFilter}\n</MsFilter>\n");
        if (string.IsNullOrEmpty(msFilter))
            return null;
        if (FullyQualifiedNameFilterParser.CheckFullyQualifiedNameFilter(msFilter))
        {
            var testCases = FullyQualifiedNameFilterParser.GetFullyQualifiedNames(msFilter);
            var filterBuilder = _filterService.GetTestFilterBuilder();
            dump?.AddString($@"\n\n<ParsedTestCases>\n");
            foreach (var testCase in testCases)
            {
                dump?.AddString($@"{testCase}\n");
                filterBuilder.AddTest(testCase);
            }
            dump?.AddString($@"\n\n</ParsedTestCases>\n");
            return filterBuilder.GetFilter();
        }
        // When we are here, we should have a valid MSTest filter that is not just FullyQualifiedName based.
        // From here we use the standard parser and expect real FQN names, categories , etc.
        var parser = new TestFilterParser();
        var filter = parser.Parse(msFilter);
        var tf = new TestFilter(filter);
        if (settings.ExplicitMode == ExplicitModeEnum.None)
        {
            var tfExplicitNone = new TestFilter("<not><prop name='Explicit'>true</prop></not>");
            var combiner = new TestFilterCombiner(tf, tfExplicitNone);
            return combiner.GetFilter();
        }
        return tf;
    }



    public TestFilter ConvertMsFilterToNUnitFilter(IVsTestFilter vsFilter, IDiscoveryConverter discovery)
    {
        var filteredTestCases = vsFilter.CheckFilter(discovery.LoadedTestCases).ToList();
        var explicitCases = discovery.GetExplicitTestCases(filteredTestCases).ToList();
        bool isExplicit = filteredTestCases.Count == explicitCases.Count && settings.ExplicitMode != ExplicitModeEnum.None;
        var tcs = isExplicit ? filteredTestCases : filteredTestCases.Except(explicitCases);
        var testCases = tcs as TestCase[] ?? tcs.ToArray();
        // TestLog.Info(string.Format("Microsoft Filter detected: LoadedTestCases {0}, Filtered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
        return testCases.Any() ? FilterByList(testCases) : NoTestsFound;
    }

    /// <summary>
    /// Used when a Where statement is added as a runsettings parameter, either in a runsettings file or on the command line from dotnet using the '-- NUnit.Where .....' statement.
    /// </summary>
    public TestFilter FilterByWhere(string where)
    {
        if (string.IsNullOrEmpty(where))
            return TestFilter.Empty;
        var filterBuilder = _filterService.GetTestFilterBuilder();
        filterBuilder.SelectWhere(where);
        if (settings.ExplicitMode == ExplicitModeEnum.None)
        {
            var tfExplicitNone = new TestFilter("<not><prop name='Explicit'>true</prop></not>");
            var combiner = new TestFilterCombiner(filterBuilder.GetFilter(), tfExplicitNone);
            return combiner.GetFilter();
        }
        return filterBuilder.GetFilter();
    }

    public TestFilter FilterByList(IEnumerable<TestCase> testCases)
    {
        if (testCases.Count() > settings.AssemblySelectLimit)
        {
            // Need to log that filter has been set to empty due to AssemblySelectLimit
            if (settings.ExplicitMode == ExplicitModeEnum.None)
            {
                var tfExplicitNone = new TestFilter("<not><prop name='Explicit'>true</prop></not>");
                return tfExplicitNone;
            }
            return TestFilter.Empty;
        }

        var filterBuilder = _filterService.GetTestFilterBuilder();
        foreach (var testCase in testCases)
        {
            filterBuilder.AddTest(testCase.FullyQualifiedName);
        }

        return filterBuilder.GetFilter();
    }
}