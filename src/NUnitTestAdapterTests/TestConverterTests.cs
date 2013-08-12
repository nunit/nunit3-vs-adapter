// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

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
        private static readonly int FAKE_LINE_NUMBER = 29;
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

            testConverter = new TestConverter(THIS_ASSEMBLY_PATH);
        }

        [Test]
        public void CanMakeTestCaseFromTest()
        {
            var testCase = testConverter.ConvertTestCase(fakeNUnitTest);

            CheckBasicInfo(testCase);

            Assert.That(testCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));

            CheckTraits(testCase);
        }

        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            // This should put the TestCase in the cache
            var cachedTestCase = testConverter.ConvertTestCase(fakeNUnitTest);

            var nunitResult = new NUnitTestResult(fakeNUnitTest);
            var testResult = testConverter.ConvertTestResult(nunitResult);
            var testCase = testResult.TestCase;

            Assert.That(testCase, Is.SameAs(cachedTestCase));

            CheckBasicInfo(testCase);

            Assert.That(testCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));

            CheckTraits(testCase);
        }

        private static void CheckBasicInfo(TestCase testCase)
        {
            Assert.That(testCase.FullyQualifiedName, Is.EqualTo("NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));
        }

        // Check traits using reflection, since the feature was added
        // in an update to VisualStudio and may not be present.
        private static void CheckTraits(TestCase testCase)
        {
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
