// ***********************************************************************
// Copyright (c) 2014-2019 Charlie Poole, Terje Sandstrom
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

// #define LAUNCHDEBUGGER


using System.Collections.Generic;
using System.Diagnostics;
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
            return MakeTestFilter(testCases,tfsFilter);
        }

        public TestFilter MakeTestFilter(IEnumerable<TestCase> testCases, TfsTestFilter tfsFilter)
        {
            if (!testCases.Any())
                return NoTestsFound;
#if LAUNCHDEBUGGER
            if (!Debugger.IsAttached)
                Debugger.Launch();
#endif
            var filterBuilder = _filterService.GetTestFilterBuilder();
            if (tfsFilter?.TfsTestCaseFilterExpression != null && !tfsFilter.IsEmpty && settings.UseTestCaseFilterConverter)
            {
                var filterValue = tfsFilter.TfsTestCaseFilterExpression.TestCaseFilterValue;
                var nunitFilter = new VsTest2NUnitFilterConverter(filterValue);
                var nunitFilterString = nunitFilter.ToString();
                logger.VerboseInfo($"Converting filter using TestCaseFilterConverter: {filterValue} => {nunitFilterString}");
                

                try
                {
                    filterBuilder.SelectWhere(nunitFilterString);
                }
                catch (NUnit.Engine.TestSelectionParserException e)
                {
                    logger.Error($"Invalid filter expression: {filterValue}");
                    throw;
                }
               
                
            }
            else  // Create name filter
            {
                foreach (var testCase in testCases)
                    filterBuilder.AddTest(testCase.FullyQualifiedName);
            }
            return filterBuilder.GetFilter();
        }
    }
}
