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
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public class Discovery
    {
        private ITestConverterXml converterForXml;
        private ITestConverter converter;

        public ITestConverterCommon TestConverterForXml => converterForXml;

        public ITestConverterCommon TestConverter => converter;

        public NUnitDiscoveryTestRun TestRun { get; private set; }

        public NUnitDiscoveryTestAssembly CurrentTestAssembly => TestRun.TestAssembly;

        public NUnitDiscoveryTestSuite TopLevelTestSuite => CurrentTestAssembly.TestSuites.FirstOrDefault();

        public IEnumerable<NUnitDiscoveryTestCase> AllTestCases => CurrentTestAssembly.AllTestCases;

        public bool IsExplicitRun => CurrentTestAssembly?.IsExplicit ?? false;

        public IList<TestCase> Convert(NUnitResults discoveryResults, ITestLogger logger, string assemblyPath, IAdapterSettings settings)
        {
            if (settings.DiscoveryMethod != DiscoveryMethod.Old)
            {
                var discoveryConverter = new DiscoveryConverter();
                TestRun = discoveryConverter.Convert(discoveryResults);
            }

            var nunitTestCases = discoveryResults.TestCases();
            var loadedTestCases = new List<TestCase>();

            // As a side effect of calling TestConverter.ConvertTestCase,
            // the converter's cache of all test cases is populated as well.
            // All future calls to convert a test case may now use the cache.

            if (settings.DiscoveryMethod == DiscoveryMethod.Old)
            {
                converterForXml = new TestConverterForXml(logger, assemblyPath, settings);
                foreach (XmlNode testNode in nunitTestCases)
                    loadedTestCases.Add(converterForXml.ConvertTestCase(new NUnitEventTestCase(testNode)));
                logger.Info(
                    $"   NUnit3TestExecutor discovered {loadedTestCases.Count} of {nunitTestCases.Count} NUnit test cases using Classic mode");
            }
            else
            {
                converter = new TestConverter(logger, assemblyPath, settings);
                var testCases = TestRun.IsExplicit ? TestRun.TestAssembly.AllTestCases : TestRun.TestAssembly.RunnableTestCases;
                foreach (var testNode in testCases)
                    loadedTestCases.Add(converter.ConvertTestCase(testNode));
                logger.Info(
                    $"   NUnit3TestExecutor discovered {loadedTestCases.Count} of {nunitTestCases.Count} NUnit test cases using Modern mode");
            }

            return loadedTestCases;
        }
    }
}
