using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter
{
    public class NUnitTestFilterBuilder
    {
        private readonly ITestFilterService _filterService;

        public static readonly TestFilter NoTestsFound = new TestFilter("<notestsfound/>");
        private readonly IAdapterSettings settings;
        private readonly ITestLogger logger;

        public NUnitTestFilterBuilder(ITestFilterService filterService,IAdapterSettings settings, ITestLogger log)
        {
            logger = log;
            this.settings = settings;
            _filterService = filterService ?? throw new NUnitEngineException("TestFilterService is not available. Engine in use is incorrect version.");
        }

        public TestFilter ConvertTfsFilterToNUnitFilter(TfsTestFilter tfsFilter, List<TestCase> loadedTestCases)
        {
            var filteredTestCases = tfsFilter.CheckFilter(loadedTestCases);
            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
            //TestLog.Info(string.Format("TFS Filter detected: LoadedTestCases {0}, Filterered Test Cases {1}", loadedTestCases.Count, testCases.Count()));
            return MakeTestFilter(testCases,tfsFilter);
        }

        public TestFilter MakeTestFilter(IEnumerable<TestCase> testCases, TfsTestFilter tfsFilter)
        {
            if (!testCases.Any())
                return NoTestsFound;

            var filterBuilder = _filterService.GetTestFilterBuilder();
            if (tfsFilter!=null && settings.UseTestCaseFilterConverter)
            {
                var filterValue = tfsFilter.TfsTestCaseFilterExpression.TestCaseFilterValue;
                var nunitFilter = new VsTest2NUnitFilterConverter(filterValue);
                var nunitFilterString = nunitFilter.ToString();
                if (settings?.Verbosity > 4)
                {
                    logger.Info($"Converting filter using TestCaseFilterConverter: {filterValue} => {nunitFilterString}");
                }

                try
                {
                    filterBuilder.SelectWhere(nunitFilterString);
                }
                catch (NUnit.Engine.TestSelectionParserException e)
                {
                    logger.Error("Invalid filter expression");
                    throw;
                }
               
                
            }
            else
            {
                foreach (var testCase in testCases)
                    filterBuilder.AddTest(testCase.FullyQualifiedName);
            }
            return filterBuilder.GetFilter();
        }
    }
}
