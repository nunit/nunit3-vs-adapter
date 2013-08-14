// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly string THIS_ASSEMBLY_PATH = 
            Path.GetFullPath("NUnit.VisualStudio.TestAdapter.Tests.dll");
        private static readonly string THIS_CODE_FILE = 
            Path.GetFullPath(@"..\..\TestConverterTests.cs");
        
        // NOTE: If the location of the FakeTestCase method in the 
        // file changes, update the value of FAKE_LINE_NUMBER.
        private static readonly int FAKE_LINE_NUMBER = 30;
        private void FakeTestCase() { } // FAKE_LINE_NUMBER SHOULD BE THIS LINE

        private ITest fakeNUnitTest;
        private TestConverter testConverter;

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = this.GetType().GetMethod("FakeTestCase", BindingFlags.Instance | BindingFlags.NonPublic);
            var nunitTest = new NUnitTestMethod(fakeTestMethod);
            nunitTest.Categories.Add("cat1");
            nunitTest.Properties.Add("Priority", "medium");

            var nunitFixture = new TestSuite("FakeNUnitFixture");
            nunitFixture.Categories.Add("super");
            nunitFixture.Add(nunitTest);

            Assert.That(nunitTest.Parent, Is.SameAs(nunitFixture));

            var fixtureNode = new TestNode(nunitFixture);
            fakeNUnitTest = (ITest)fixtureNode.Tests[0];

            testConverter = new TestConverter(new TestLogger(), THIS_ASSEMBLY_PATH);
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
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));

            Assert.That(testCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));

            // Check traits using reflection, since the feature was added
            // in an update to VisualStudio and may not be present.
            if (TraitsFeature.IsSupported)
            {
                var traitList = new List<string>();

                foreach (NTrait trait in testCase.GetTraits())
                    traitList.Add(trait.Name + ":" +  trait.Value);

                Assert.That(traitList, Is.EquivalentTo(new string[] { "Category:super", "Category:cat1", "Priority:medium" }));
            }
        }
    }
}
