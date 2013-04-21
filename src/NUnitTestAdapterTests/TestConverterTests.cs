// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using System.IO;

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
        private static readonly int FAKE_LINE_NUMBER = 31;

        private NUnit.Core.TestSuite fakeNUnitFixture;
        private NUnit.Core.Test fakeNUnitTest;
        private NUnit.Core.TestResult fakeNUnitResult;
        private TestConverter testConverter;

        private void FakeTestCase() { } // FAKE_LINE_NUMBER SHOULD BE THIS LINE

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = this.GetType().GetMethod("FakeTestCase", BindingFlags.Instance | BindingFlags.NonPublic);
            fakeNUnitTest = new NUnit.Core.NUnitTestMethod(fakeTestMethod);
            fakeNUnitTest.Categories.Add("cat1");
            fakeNUnitTest.Properties.Add("Priority", "medium");

            fakeNUnitFixture = new NUnit.Core.TestSuite("FakeNUnitFixture");
            fakeNUnitFixture.Categories.Add("super");
            fakeNUnitFixture.Add(fakeNUnitTest);
            Assert.That(fakeNUnitTest.Parent, Is.SameAs(fakeNUnitFixture));

            fakeNUnitResult = new NUnit.Core.TestResult(fakeNUnitTest);

            var map = new Dictionary<string, NUnit.Core.TestNode>();
            var fixtureNode = new NUnit.Core.TestNode(fakeNUnitFixture);
            var testNode = (NUnit.Core.TestNode)fixtureNode.Tests[0];
            map.Add(fakeNUnitTest.TestName.UniqueName, testNode);

            testConverter = new TestConverter(THIS_ASSEMBLY_PATH, map);
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
        public void CanMakeTestCaseFromTestName()
        {
            var testName = fakeNUnitTest.TestName;

            var testCase = testConverter.MakeTestCaseFromTestName(testName);

            CheckBasicInfo(testCase);

            Assert.Null(testCase.CodeFilePath);
            Assert.That(testCase.LineNumber, Is.EqualTo(0));
        }

        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            var testResult = testConverter.ConvertTestResult(fakeNUnitResult);
            var testCase = testResult.TestCase;

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
