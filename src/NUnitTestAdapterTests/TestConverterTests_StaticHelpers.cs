// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [Category("TestConverter")]
    public class TestConverterTestsStaticHelpers
    {
        [TestCase("<test-case result='Failed' label='Cancelled'/>", ExpectedResult = TestOutcome.Failed)]
        [TestCase("<test-case result='Failed' label='Error'/>", ExpectedResult = TestOutcome.Failed)]
        [TestCase("<test-case result='Failed'/>", ExpectedResult = TestOutcome.Failed)]
        [TestCase("<test-case result='Skipped' label='Ignored'/>", ExpectedResult = TestOutcome.Skipped)]
        [TestCase("<test-case result='Inconclusive'/>", ExpectedResult = TestOutcome.None)]
        [TestCase("<test-case result='Failed' label='NotRunnable'/>", ExpectedResult = TestOutcome.Failed)]
        [TestCase("<test-case result='Skipped'/>", ExpectedResult = TestOutcome.Skipped)]
        [TestCase("<test-case result='Passed'/>", ExpectedResult = TestOutcome.Passed)]
        public TestOutcome ResultStateToTestOutcome(string result)
        {
            var resultNode = XmlHelper.CreateXmlNode(result);
            return TestConverter.GetTestOutcome(resultNode);
        }
    }
}
