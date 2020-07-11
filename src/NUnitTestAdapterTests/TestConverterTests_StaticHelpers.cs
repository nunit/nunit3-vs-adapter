// ***********************************************************************
// Copyright (c) 2011-2015 Charlie Poole, Terje Sandstrom
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

using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NUnit.Engine;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [Category("TestConverter")]
    public class TestConverterTestsGetTestOutcome
    {
        [TestCase("<test-case result='Failed' label='Cancelled'/>", TestOutcome.Failed)]
        [TestCase("<test-case result='Failed' label='Error'/>", TestOutcome.Failed)]
        [TestCase("<test-case result='Failed'/>", TestOutcome.Failed)]
        [TestCase("<test-case result='Skipped' label='Ignored'/>", TestOutcome.Skipped)]
        [TestCase("<test-case result='Inconclusive'/>", TestOutcome.None)]
        [TestCase("<test-case result='Failed' label='NotRunnable'/>", TestOutcome.Failed)]
        [TestCase("<test-case result='Skipped'/>", TestOutcome.None)]
        [TestCase("<test-case result='Passed'/>", TestOutcome.Passed)]
        [TestCase("<test-case result='Warning'/>", TestOutcome.Skipped)]
        public void ResultStateToTestOutcome(string result, TestOutcome expected)
        {
            var resultNode = new NUnitTestEventTestCase(XmlHelper.CreateXmlNode(result));
            var logger = Substitute.For<ITestLogger>();
            var settings = Substitute.For<IAdapterSettings>();
            settings.MapWarningTo.Returns(TestOutcome.Skipped);

            var converter = new TestConverterForXml(logger, "whatever", settings);

            var res = converter.GetTestOutcome(resultNode);

            Assert.That(res, Is.EqualTo(expected), $"In: {result}, out: {res.ToString()} expected: {expected.ToString()} ");
        }
    }
}
