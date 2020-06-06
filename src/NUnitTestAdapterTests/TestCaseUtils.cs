// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
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

using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    /// <summary>
    /// Helper methods for testing against test cases.
    /// </summary>
    internal static class TestCaseUtils
    {
        /// <summary>
        /// Knows how to convert an entire XML fragment.
        /// </summary>
        public static IReadOnlyList<TestCase> ConvertTestCases(this TestConverterForXml testConverterForXml, string xml)
        {
            if (testConverterForXml == null) throw new ArgumentNullException(nameof(testConverterForXml));

            var fragment = new XmlDocument().CreateDocumentFragment();
            fragment.InnerXml = xml;
            var testCaseNodes = fragment.SelectNodes("//test-case");

            var testCases = new TestCase[testCaseNodes.Count];

            for (var i = 0; i < testCases.Length; i++)
                testCases[i] = testConverterForXml.ConvertTestCase(new NUnitEventTestCase(testCaseNodes[i]));

            return testCases;
        }

        public static IReadOnlyCollection<TestCase> ConvertTestCases(string xml)
        {
            var settings = Substitute.For<IAdapterSettings>();
            settings.CollectSourceInformation.Returns(false);
            using (var testConverter = new TestConverterForXml(
                new TestLogger(new MessageLoggerStub()),
                FakeTestData.AssemblyPath,
                settings))
            {
                return testConverter.ConvertTestCases(xml);
            }
        }
    }
}
