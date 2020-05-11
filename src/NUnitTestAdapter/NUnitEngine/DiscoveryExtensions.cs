using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public class DiscoveryExtensions
    {
        public TestConverter TestConverter { get; private set; }

        public IList<TestCase> Load(NUnitResults discoveryResults, ITestLogger logger, string assemblyPath, IAdapterSettings settings)
        {
            var nunitTestCases = discoveryResults.TestCases();

            TestConverter = new TestConverter(logger, assemblyPath, settings);
            var loadedTestCases = new List<TestCase>();

            // As a side effect of calling TestConverter.ConvertTestCase,
            // the converter's cache of all test cases is populated as well.
            // All future calls to convert a test case may now use the cache.
            foreach (XmlNode testNode in nunitTestCases)
                loadedTestCases.Add(TestConverter.ConvertTestCase(new NUnitTestCase(testNode)));
            logger.Info($"   NUnit3TestExecutor discovered {loadedTestCases.Count} of {nunitTestCases.Count} NUnit test cases");

            return loadedTestCases;
        }
    }
}
