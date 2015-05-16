// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using Fakes;

    [Category("TestConverter")]
    public class TestConverterTests
    {
        private XmlNode fakeTestNode;
        private TestConverter testConverter;

        [SetUp]
        public void SetUp()
        {
            fakeTestNode = FakeTestData.GetTestNode();
            testConverter = new TestConverter(new TestLogger(new MessageLoggerStub(), 0), FakeTestData.AssemblyPath);
        }

        [TearDown]
        public void TearDown()
        {
            testConverter.Dispose();
        }

        [Test]
        public void CanMakeTestCaseFromTest()
        {
            var testCase = testConverter.ConvertTestCase(fakeTestNode);

            CheckTestCase(testCase);
        }

        [Test]
        public void ConvertedTestCaseIsCached()
        {
            testConverter.ConvertTestCase(fakeTestNode);
            var testCase = testConverter.GetCachedTestCase("123");

            CheckTestCase(testCase);
        }

        [Test]
        public void CannotMakeTestResultWhenTestCaseIsNotInCache()
        {
            var fakeResultNode = FakeTestData.GetResultNode();
            var testResult = testConverter.ConvertTestResult(fakeResultNode);
            Assert.Null(testResult);
        }

        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            // This should put the TestCase in the cache
            var cachedTestCase = testConverter.ConvertTestCase(fakeTestNode);
            var fakeResultNode = FakeTestData.GetResultNode();

            var testResult = testConverter.ConvertTestResult(fakeResultNode);
            var testCase = testResult.TestCase;

            Assert.That(testCase, Is.SameAs(cachedTestCase));

            CheckTestCase(testCase);

            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Passed));
            Assert.That(testResult.ErrorMessage, Is.EqualTo("It passed!"));
            Assert.That(testResult.Duration, Is.EqualTo(TimeSpan.FromSeconds(1.234)));
        }

        private void CheckTestCase(TestCase testCase)
        {
            Assert.That(testCase.FullyQualifiedName, Is.EqualTo(FakeTestData.FullyQualifiedName));
            Assert.That(testCase.DisplayName, Is.EqualTo(FakeTestData.DisplayName));
            Assert.That(testCase.Source, Is.SamePath(FakeTestData.AssemblyPath));

            Assert.That(testCase.CodeFilePath, Is.SamePath(FakeTestData.CodeFile));
            Assert.That(testCase.LineNumber, Is.EqualTo(FakeTestData.LineNumber));

            // Check traits using reflection, since the feature was added
            // in an update to VisualStudio and may not be present.
            if (TraitsFeature.IsSupported)
            {
                var traitList = testCase.GetTraits().Select(trait => trait.Name + ":" + trait.Value).ToList();

                Assert.That(traitList, Is.EquivalentTo(new[] { "Category:super", "Category:cat1", "Priority:medium" }));
            }
        }
    }
}
