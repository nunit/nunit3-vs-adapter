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
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NUnitTestFilterBuilder
    {
        private readonly ITestFilterService _filterService;

        // ReSharper disable once StringLiteralTypo
        public static readonly TestFilter NoTestsFound = new ("<notestsfound/>");
        private readonly IAdapterSettings settings;

        public NUnitTestFilterBuilder(ITestFilterService filterService, IAdapterSettings settings)
        {
            this.settings = settings;
            _filterService = filterService ?? throw new NUnitEngineException("TestFilterService is not available. Engine in use is incorrect version.");
        }

        public TestFilter ConvertTfsFilterToNUnitFilter(IVsTestFilter vsFilter, IList<TestCase> loadedTestCases)
        {
            var filteredTestCases = vsFilter.CheckFilter(loadedTestCases);
            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
            // TestLog.Info(string.Format("TFS Filter detected: LoadedTestCases {0}, Filtered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
            return testCases.Any() ? FilterByList(testCases) : NoTestsFound;
        }


        public TestFilter ConvertVsTestFilterToNUnitFilter(IVsTestFilter vsFilter, IDiscoveryConverter discovery)
        {
            if (settings.DiscoveryMethod == DiscoveryMethod.Legacy)
                return ConvertTfsFilterToNUnitFilter(vsFilter, discovery.LoadedTestCases);
            if (!settings.UseNUnitFilter)
                return ConvertTfsFilterToNUnitFilter(vsFilter, discovery);
            var result = ConvertVsTestFilterToNUnitFilter(vsFilter);
            return result ?? ConvertTfsFilterToNUnitFilter(vsFilter, discovery);
        }

        /// <summary>
        /// Used when running from command line, mode Non-Ide,  e.g. 'dotnet test --filter xxxxx'.  Reads the TfsTestCaseFilterExpression.
        /// </summary>
        public TestFilter ConvertVsTestFilterToNUnitFilter(IVsTestFilter vsFilter)
        {
            if (string.IsNullOrEmpty(vsFilter?.TfsTestCaseFilterExpression?.TestCaseFilterValue))
                return null;
            var parser = new TestFilterParser();
            var filter = parser.Parse(vsFilter.TfsTestCaseFilterExpression.TestCaseFilterValue);
            var tf = new TestFilter(filter);
            return tf;
        }


        public TestFilter ConvertTfsFilterToNUnitFilter(IVsTestFilter vsFilter, IDiscoveryConverter discovery)
        {
            var filteredTestCases = vsFilter.CheckFilter(discovery.LoadedTestCases).ToList();
            var explicitCases = discovery.GetExplicitTestCases(filteredTestCases).ToList();
            bool isExplicit = filteredTestCases.Count == explicitCases.Count;
            var tcs = isExplicit ? filteredTestCases : filteredTestCases.Except(explicitCases);
            var testCases = tcs as TestCase[] ?? tcs.ToArray();
            // TestLog.Info(string.Format("TFS Filter detected: LoadedTestCases {0}, Filtered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
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
            return filterBuilder.GetFilter();
        }

        public TestFilter FilterByList(IEnumerable<TestCase> testCases)
        {
            if (testCases.Count() > settings.AssemblySelectLimit)
            {
                // Need to log that filter has been set to empty due to AssemblySelectLimit
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
}
