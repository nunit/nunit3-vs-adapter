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
    public class TestConverterTests
    {
        private static readonly string THIS_ASSEMBLY_PATH = 
            Path.GetFullPath("NUnit.VisualStudio.TestAdapter.Tests.dll");
        private static readonly string THIS_CODE_FILE = 
            Path.GetFullPath(@"..\..\TestConverterTests.cs");
        
        // NOTE: If the location of the FakeTestCase method in the 
        // file changes, update the value of FAKE_LINE_NUMBER.
        private static readonly int FAKE_LINE_NUMBER = 30;

        private NUnit.Core.ITest fakeNUnitTest;
        private NUnit.Core.TestResult fakeNUnitResult;
        private TestConverter testConverter;

        private void FakeTestCase()
        {  // FAKE_LINE_NUMBER SHOULD BE THIS LINE
        }

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = this.GetType().GetMethod("FakeTestCase", BindingFlags.Instance | BindingFlags.NonPublic);
            fakeNUnitTest = new NUnit.Core.NUnitTestMethod(fakeTestMethod);
            fakeNUnitResult = new NUnit.Core.TestResult(fakeNUnitTest);

            var map = new Dictionary<string, NUnit.Core.TestNode>();
            map.Add(fakeNUnitTest.TestName.UniqueName, new NUnit.Core.TestNode(fakeNUnitTest));

            testConverter = new TestConverter(THIS_ASSEMBLY_PATH, map);
        }

        [Test]
        [Category("TestConverter")]
        public void CanMakeTestCaseFromTest()
        {
            var testCase = testConverter.ConvertTestCase(fakeNUnitTest);

            Assert.That(testCase.FullyQualifiedName, Is.StringMatching(@"^\[.*\]NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase$"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));

            Assert.That(testCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));
        }

        [Test]
        [Category("TestConverter")]
        public void CanMakeTestCaseFromTestName()
        {
            var testName = fakeNUnitTest.TestName;

            var testCase = testConverter.MakeTestCase(testName);

            Assert.That(testCase.FullyQualifiedName, Is.StringMatching(@"^\[.*\]NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase$"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));

            Assert.Null(testCase.CodeFilePath);
            Assert.That(testCase.LineNumber, Is.EqualTo(0));
        }

        [Test]
        [Category("TestConverter")]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            var testResult = testConverter.ConvertTestResult(fakeNUnitResult);
            var testCase = testResult.TestCase;

            Assert.That(testCase.FullyQualifiedName, Is.StringMatching(@"^\[.*\]NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase$"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));

            Assert.That(testCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));
        }
    }
}
