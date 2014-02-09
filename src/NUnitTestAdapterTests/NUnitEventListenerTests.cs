// ****************************************************************
// Copyright (c) 2012 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Core;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

using VSTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using NUnitTestResult = NUnit.Core.TestResult;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NUnitEventListenerTests
    {
        private readonly static Uri EXECUTOR_URI = new Uri(NUnitTestExecutor.ExecutorUri);
        private static readonly string THIS_ASSEMBLY_PATH =
            Path.GetFullPath("NUnit.VisualStudio.TestAdapter.Tests.dll");
        private static readonly string THIS_CODE_FILE =
            Path.GetFullPath(@"..\..\NUnitEventListenerTests.cs");

        private static readonly int LINE_NUMBER = 29; // Must be number of the following line
        private void FakeTestMethod() { }

        private ITest fakeNUnitTest;
        private NUnitTestResult fakeNUnitResult;

        private NUnitEventListener listener;
        private FakeFrameworkHandle testLog;
        private TestConverter testConverter;

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = this.GetType().GetMethod("FakeTestMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            fakeNUnitTest = new NUnitTestMethod(fakeTestMethod);

            fakeNUnitResult = new NUnitTestResult(fakeNUnitTest);
            fakeNUnitResult.SetResult(ResultState.Success, "It passed!", null);
            fakeNUnitResult.Time = 1.234;

            testLog = new FakeFrameworkHandle();

            testConverter = new TestConverter(new TestLogger(), THIS_ASSEMBLY_PATH);

            testConverter.ConvertTestCase(fakeNUnitTest);
            Assert.NotNull(testConverter.GetCachedTestCase(fakeNUnitTest.TestName.UniqueName));
            
            this.listener = new NUnitEventListener(testLog, testConverter);
        }

        #region TestStarted Tests

        [Test]
        public void TestStarted_CallsRecordStartCorrectly()
        {
            listener.TestStarted(fakeNUnitTest.TestName);
            Assert.That(testLog.Events.Count, Is.EqualTo(1));
            Assert.That(
                testLog.Events[0].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordStart));

            VerifyTestCase(testLog.Events[0].TestCase);
        }

        #endregion

        #region TestFinished Tests

        [Test]
        public void TestFinished_CallsRecordEnd_Then_RecordResult()
        {
            listener.TestFinished(fakeNUnitResult);
            Assert.AreEqual(2, testLog.Events.Count);
            Assert.AreEqual(
                FakeFrameworkHandle.EventType.RecordEnd,
                testLog.Events[0].EventType);
            Assert.AreEqual(
                FakeFrameworkHandle.EventType.RecordResult,
                testLog.Events[1].EventType);
        }

        [Test]
        public void TestFinished_CallsRecordEndCorrectly()
        {
            listener.TestFinished(fakeNUnitResult);
            Assume.That(testLog.Events.Count, Is.EqualTo(2));
            Assume.That(
                testLog.Events[0].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordEnd));

            VerifyTestCase(testLog.Events[0].TestCase);
            Assert.AreEqual(TestOutcome.Passed, testLog.Events[0].TestOutcome);
        }

        [Test]
        public void TestFinished_CallsRecordResultCorrectly()
        {
            listener.TestFinished(fakeNUnitResult);
            Assume.That(testLog.Events.Count, Is.EqualTo(2));
            Assume.That(
                testLog.Events[1].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordResult));
            
            VerifyTestResult(testLog.Events[1].TestResult);
        }

        [TestCase(ResultState.Success, TestOutcome.Passed, null)]
        [TestCase(ResultState.Failure, TestOutcome.Failed, "My failure message")]
        [TestCase(ResultState.Error, TestOutcome.Failed, "Error!")]
        [TestCase(ResultState.Cancelled, TestOutcome.None, null)]
        [TestCase(ResultState.Inconclusive, TestOutcome.None, null)]
        [TestCase(ResultState.NotRunnable, TestOutcome.Failed, "No constructor")]
        [TestCase(ResultState.Skipped, TestOutcome.Skipped, null)]
        [TestCase(ResultState.Ignored, TestOutcome.Skipped, "my reason")]
        public void TestFinished_OutcomesAreCorrectlyTranslated(ResultState resultState, TestOutcome outcome, string message)
        {
            fakeNUnitResult.SetResult(resultState, message, null);
            listener.TestFinished(fakeNUnitResult);
            Assume.That(testLog.Events.Count, Is.EqualTo(2));
            Assume.That(
                testLog.Events[0].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordEnd));
            Assume.That(
                testLog.Events[1].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordResult));

            Assert.AreEqual(outcome, testLog.Events[0].TestOutcome);
            Assert.AreEqual(outcome, testLog.Events[1].TestResult.Outcome);
            Assert.AreEqual(message, testLog.Events[1].TestResult.ErrorMessage);
        }

        #endregion

        #region TestOutput Tests

        [TestCaseSource("MessageTestSource")]
        public void TestOutput_CallsSendMessageCorrectly(string nunitMessage, string expectedMessage)
        {
            listener.TestOutput(new TestOutput(nunitMessage, TestOutputType.Out));
            Assert.AreEqual(1, testLog.Events.Count);

            Assert.AreEqual(TestMessageLevel.Informational, testLog.Events[0].Message.Level);
            Assert.AreEqual(expectedMessage, testLog.Events[0].Message.Text);
        }

        private static readonly string NL = Environment.NewLine;
        private static readonly string MESSAGE = "MESSAGE";
        private static readonly string LINE1 = "LINE#1";
        private static readonly string LINE2 = "\tLINE#2";
        private TestCaseData[] MessageTestSource = 
        {
            new TestCaseData(MESSAGE, MESSAGE),
            new TestCaseData(MESSAGE + NL, MESSAGE),
            new TestCaseData(MESSAGE + "\r\n", MESSAGE),
            new TestCaseData(MESSAGE + "\n", MESSAGE),
            new TestCaseData(MESSAGE + "\r", MESSAGE),
            new TestCaseData(LINE1 + NL + LINE2, LINE1 + NL + LINE2),
            new TestCaseData(LINE1 +"\r\n" + LINE2, LINE1 +"\r\n" + LINE2),
            new TestCaseData(LINE1 +"\n" + LINE2, LINE1 + "\n" + LINE2),
            new TestCaseData(LINE1 +"\r" + LINE2, LINE1 + "\r" + LINE2),
            new TestCaseData(LINE1 +"\r\n" + LINE2 + "\r\n", LINE1 +"\r\n" + LINE2),
            new TestCaseData(MESSAGE + NL + NL, MESSAGE + NL),
            new TestCaseData(MESSAGE + "\r\n\r\n", MESSAGE + "\r\n"),
            new TestCaseData(MESSAGE + "\n\n", MESSAGE + "\n"),
            new TestCaseData(MESSAGE + "\r\r", MESSAGE + "\r"),
        };

        #endregion

        #region Helper Methods

        private void VerifyTestCase(TestCase ourCase)
        {
            Assert.NotNull(ourCase, "TestCase not set");
            Assert.That(ourCase.DisplayName, Is.EqualTo(fakeNUnitTest.TestName.Name));
            Assert.That(ourCase.FullyQualifiedName, Is.EqualTo(fakeNUnitTest.TestName.FullName));
            Assert.That(ourCase.Source, Is.EqualTo(THIS_ASSEMBLY_PATH));
            Assert.That(ourCase.CodeFilePath, Is.SamePath(THIS_CODE_FILE));
            Assert.That(ourCase.LineNumber, Is.EqualTo(LINE_NUMBER));
        }

        private void VerifyTestResult(VSTestResult ourResult)
        {
            Assert.NotNull(ourResult, "TestResult not set");
            VerifyTestCase(ourResult.TestCase);

            Assert.AreEqual(Environment.MachineName, ourResult.ComputerName);
            Assert.AreEqual(TestOutcome.Passed, ourResult.Outcome);
            Assert.AreEqual("It passed!", ourResult.ErrorMessage);
            Assert.AreEqual(TimeSpan.FromSeconds(1.234), ourResult.Duration);
        }

        #endregion
    }
}
