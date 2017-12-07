using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public TestFilter ConvertTfsFilterToNUnitFilter(TfsTestFilter tfsFilter, List<TestCase> loadedTestCases)
        {
            var filteredTestCases = tfsFilter.CheckFilter(loadedTestCases);
            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
            //TestLog.Info(string.Format("TFS Filter detected: LoadedTestCases {0}, Filterered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
            return MakeTestFilter(testCases);
        }

        public TestFilter MakeTestFilter(IEnumerable<TestCase> testCases)
        {
            if (testCases.Count() == 0)
                return NoTestsFound;

            ITestFilterBuilder filterBuilder = _filterService.GetTestFilterBuilder();

            foreach (TestCase testCase in testCases)
            {
                filterBuilder.AddTest(testCase.FullyQualifiedName);
            }

            return filterBuilder.GetFilter();
        }
    }
}
