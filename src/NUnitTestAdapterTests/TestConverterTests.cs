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
        private static readonly int FAKE_LINE_NUMBER = 32;

        private NUnit.Core.TestSuite fakeNUnitFixture;
        private NUnit.Core.Test fakeNUnitTest;
        private NUnit.Core.TestResult fakeNUnitResult;
        private TestConverter testConverter;

        private void FakeTestCase()
        {  // FAKE_LINE_NUMBER SHOULD BE THIS LINE
        }

        private static PropertyInfo traitsProperty;
        private static PropertyInfo nameProperty;
        private static PropertyInfo valueProperty;

        static TestConverterTests()
        {
            traitsProperty = typeof(TestCase).GetProperty("Traits");

            var traitType = Type.GetType("Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait,Microsoft.VisualStudio.TestPlatform.ObjectModel");
            nameProperty = traitType.GetProperty("Name");
            valueProperty = traitType.GetProperty("Value");
        }

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

            var testCase = testConverter.MakeTestCase(testName);

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
            Assert.That(testCase.FullyQualifiedName, Is.StringMatching(@"^\[.*\]NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase$"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));
        }

        private static void CheckTraits(TestCase testCase)
        {
            if (traitsProperty != null)
            {
                var traitsCollection = traitsProperty.GetValue(testCase, new object[0]) as System.Collections.IEnumerable;
                var traits = new List<string>();

                foreach (object trait in traitsCollection)
                {
                    string name = nameProperty.GetValue(trait) as string;
                    string value = valueProperty.GetValue(trait) as string;
                    traits.Add(string.Format(name + ":" + value));
                }
                Assert.That(traits, Is.EquivalentTo(new string[] { "Category:super", "Category:cat1", "Priority:medium" }));
            }
        }
    }
}
