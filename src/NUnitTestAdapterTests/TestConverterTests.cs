// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using NUnit.Core;
using NUnit.Framework;

using NUnitTestResult = NUnit.Core.TestResult;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [Category("TestConverter")]
    public class TestConverterTests
    {
        private static readonly string ThisAssemblyPath = 
            Path.GetFullPath("NUnit.VisualStudio.TestAdapter.Tests.dll");
        private static readonly string ThisCodeFile = 
            Path.GetFullPath(@"..\..\TestConverterTests.cs");
        
        // NOTE: If the location of the FakeTestCase method in the 
        // file changes, update the value of FAKE_LINE_NUMBER.
        private const int FakeLineNumber = 31;
// ReSharper disable once UnusedMember.Local
        private void FakeTestCase() { } // FAKE_LINE_NUMBER SHOULD BE THIS LINE

        private ITest fakeNUnitTest;
        private TestConverter testConverter;

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = GetType().GetMethod("FakeTestCase", BindingFlags.Instance | BindingFlags.NonPublic);
            var nunitTest = new NUnitTestMethod(fakeTestMethod);
            nunitTest.Categories.Add("cat1");
            nunitTest.Properties.Add("Priority", "medium");

            var nunitFixture = new TestSuite("FakeNUnitFixture");
            nunitFixture.Categories.Add("super");
            nunitFixture.Add(nunitTest);

            Assert.That(nunitTest.Parent, Is.SameAs(nunitFixture));

            var fixtureNode = new TestNode(nunitFixture);
            fakeNUnitTest = (ITest)fixtureNode.Tests[0];

            testConverter = new TestConverter(new TestLogger(), ThisAssemblyPath);
        }

        [TearDown]
        public void TearDown()
        {
            testConverter.Dispose();
        }

        [Test]
        public void CanMakeTestCaseFromTest()
        {
            var testCase = testConverter.ConvertTestCase(fakeNUnitTest);

            CheckTestCase(testCase);
        }

        [Test]
        public void ConvertedTestCaseIsCached()
        {
            testConverter.ConvertTestCase(fakeNUnitTest);
            var testCase = testConverter.GetCachedTestCase(fakeNUnitTest.TestName.UniqueName);

            CheckTestCase(testCase);
        }

        [Test]
        public void CannotMakeTestResultWhenTestCaseIsNotInCache()
        {
            var nunitResult = new NUnitTestResult(fakeNUnitTest);
            var testResult = testConverter.ConvertTestResult(nunitResult);
            Assert.Null(testResult);
        }

        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            // This should put the TestCase in the cache
            var cachedTestCase = testConverter.ConvertTestCase(fakeNUnitTest);

            var nunitResult = new NUnitTestResult(fakeNUnitTest);
            nunitResult.SetResult(ResultState.Success, "It passed!", null);
            nunitResult.Time = 1.234;
            
            var testResult = testConverter.ConvertTestResult(nunitResult);
            var testCase = testResult.TestCase;

            Assert.That(testCase, Is.SameAs(cachedTestCase));

            CheckTestCase(testCase);

            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Passed));
            Assert.That(testResult.ErrorMessage, Is.EqualTo("It passed!"));
            Assert.That(testResult.Duration, Is.EqualTo(TimeSpan.FromSeconds(1.234)));
        }

        private static void CheckTestCase(TestCase testCase)
        {
            Assert.That(testCase.FullyQualifiedName, Is.EqualTo("NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(ThisAssemblyPath));

            Assert.That(testCase.CodeFilePath, Is.SamePath(ThisCodeFile));
            Assert.That(testCase.LineNumber, Is.EqualTo(FakeLineNumber));

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
