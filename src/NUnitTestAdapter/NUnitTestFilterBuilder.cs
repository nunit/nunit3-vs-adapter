using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NUnitTestFilterBuilder
    {
        private ITestFilterService _filterService;

        public static readonly TestFilter NoTestsFound = new TestFilter("<notestsfound/>");

        public NUnitTestFilterBuilder(ITestFilterService filterService)
        {
            if (filterService == null)
                throw new NUnitEngineException("TestFilterService is not available. Engine in use is incorrect version.");

            _filterService = filterService;
        }

        public TestFilter ConvertTfsFilterToNUnitFilter(ITfsTestFilter tfsFilter, IList<TestCase> loadedTestCases)
        {
            var filteredTestCases = tfsFilter.CheckFilter(loadedTestCases);
            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
            //TestLog.Info(string.Format("TFS Filter detected: LoadedTestCases {0}, Filtered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
            return testCases.Any() ? FilterByList(testCases) : NoTestsFound;
        }

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
            var filterBuilder = _filterService.GetTestFilterBuilder();

            foreach (var testCase in testCases)
            {
                filterBuilder.AddTest(testCase.FullyQualifiedName);
            }

            return filterBuilder.GetFilter();
        }
    }
}
