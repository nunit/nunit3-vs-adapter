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
                var filtervalue = tfsFilter.TfsTestCaseFilterExpression.TestCaseFilterValue;
                var nunitfilter = new VsTest2NUnitFilterConverter(filtervalue);
                var nunitfilterstring = nunitfilter.ToString();
                if (settings?.Verbosity > 4)
                {
                    logger.Info($"Converting filter using TestCaseFilterConverter: {filtervalue} => {nunitfilterstring}");
                }

                try
                {
                    filterBuilder.SelectWhere(nunitfilterstring);
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
