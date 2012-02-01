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
        private static readonly string THIS_ASSEMBLY_PATH = Path.GetFullPath("NUnit.VisualStudio.TestAdapter.Tests.dll");
        private static readonly string THIS_CODE_FILE = Path.GetFullPath(@"..\..\TestConverterTests.cs");
        
        // NOTE: If the location of the FakeTestCase method in the file changes,
        // update the value of FAKE_LINE_NUMBER, FAKE_COLUMN_NUMBER.
        private static readonly int FAKE_LINE_NUMBER = 28;
        private static readonly int FAKE_COLUMN_NUMBER = 9;

        private NUnit.Core.ITest fakeNUnitTest;
        private NUnit.Core.TestResult fakeNUnitResult;

        private void FakeTestCase()
        {
    //  ^----- (FAKE_LINE_NUMBER, FAKE_COLUMN_NUMBER)
        }

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = this.GetType().GetMethod("FakeTestCase", BindingFlags.Instance | BindingFlags.NonPublic);
            fakeNUnitTest = new NUnit.Core.NUnitTestMethod(fakeTestMethod);
            fakeNUnitResult = new NUnit.Core.TestResult(fakeNUnitTest);
        }

        [Test]
        public void CanMakeTestCaseFromTest()
        {
            var testCase = new TestConverter().ConvertTestCase(fakeNUnitTest);

            Assert.That(testCase.Name, Is.EqualTo("NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));

            Assert.That(testCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));
#if DEV10
            Assert.That(testCase.ColumnNumber, Is.EqualTo(FAKE_COLUMN_NUMBER));
#endif
        }

        [Test]
        public void CanMakeTestCaseFromTestName()
        {
            var testName = fakeNUnitTest.TestName;

            var testCase = new TestConverter().ConvertTestName(testName, THIS_ASSEMBLY_PATH);

            Assert.That(testCase.Name, Is.EqualTo("NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));

            Assert.That(testCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));
#if DEV10
            Assert.That(testCase.ColumnNumber, Is.EqualTo(FAKE_COLUMN_NUMBER));
#endif
        }

//        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            var testResult = new TestConverter().ConvertTestResult(fakeNUnitResult);
            var testCase = testResult.TestCase;

            Assert.That(testCase.Name, Is.EqualTo("NUnit.VisualStudio.TestAdapter.Tests.TestConverterTests.FakeTestCase"));
            Assert.That(testCase.DisplayName, Is.EqualTo("FakeTestCase"));
            Assert.That(testCase.Source, Is.SamePath(THIS_ASSEMBLY_PATH));

            Assert.That(testCase.CodeFilePath, Is.EqualTo(THIS_CODE_FILE));
            Assert.That(testCase.LineNumber, Is.EqualTo(FAKE_LINE_NUMBER));
#if DEV10
            Assert.That(testCase.ColumnNumber, Is.EqualTo(FAKE_COLUMN_NUMBER));
#endif
        }
    }
}
