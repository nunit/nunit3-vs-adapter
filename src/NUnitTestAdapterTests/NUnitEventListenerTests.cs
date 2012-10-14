// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using NUnit.Core;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NUnitEventListenerTests
    {
        private readonly static string assemblyName = "MyAssembly.dll";
        private readonly static Uri fakeUri = new Uri(NUnitTestExecutor.ExecutorUri);

        private NUnitEventListener listener;
        private TestCase fakeTestCase;
        private NUnit.Core.ITest fakeNUnitTest;
        private FakeTestExecutionRecorder testLog;

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = this.GetType().GetMethod("FakeTestMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            fakeNUnitTest = new NUnitTestMethod(fakeTestMethod);
            fakeTestCase = new TestCase(fakeNUnitTest.TestName.FullName, fakeUri, assemblyName);

            testLog = new FakeTestExecutionRecorder();

            var map = new Dictionary<string, NUnit.Core.TestNode>();

            this.listener = new NUnitEventListener(testLog, map, assemblyName);
        }

        [Test]
        public void TestFinished_CallsRecordResult()
        {
            listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
            Assert.AreEqual(1, testLog.RecordResultCalls);
        }

        [Test]
        public void TestFinished_ResultHasCorrectDisplayName()
        {
            listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
            Assert.AreEqual(fakeNUnitTest.TestName.Name, testLog.LastResult.TestCase.DisplayName);
        }

        [Test]
        public void TestFinished_ResultHasCorrectFullName()
        {
            listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
            Assert.AreEqual(fakeNUnitTest.TestName.FullName, testLog.LastResult.TestCase.FullyQualifiedName);
        }

        [Test]
        public void TestFinished_ResultHasCorrectComputerName()
        {
            listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
            Assert.AreEqual(Environment.MachineName, testLog.LastResult.ComputerName);
        }

        [TestCase(ResultState.Success,      TestOutcome.Passed, null)]
        [TestCase(ResultState.Failure,      TestOutcome.Failed,  "My failure message")]
        [TestCase(ResultState.Error,        TestOutcome.Failed,  "Error!")]
        [TestCase(ResultState.Cancelled,    TestOutcome.None,    null)]
        [TestCase(ResultState.Inconclusive, TestOutcome.None,    null)]
        [TestCase(ResultState.NotRunnable,  TestOutcome.Failed,  "No constructor")]
        [TestCase(ResultState.Skipped,      TestOutcome.Skipped, null)]
        [TestCase(ResultState.Ignored,      TestOutcome.Skipped, "my reason")]
        public void TestFinished_ResultHasCorrectOutcome(ResultState resultState, TestOutcome outcome, string message)
        {
            var nunitResult = new NUnit.Core.TestResult(fakeNUnitTest);
            nunitResult.SetResult(resultState, message, null);
            listener.TestFinished(nunitResult);
            Assert.AreEqual(outcome, testLog.LastResult.Outcome);
            Assert.AreEqual(message, testLog.LastResult.ErrorMessage);
        }

        [Test]
        public void TestFinished_ResultHasCorrectDuration()
        {
            var nunitResult = new NUnit.Core.TestResult(fakeNUnitTest);
            nunitResult.Success();
            nunitResult.Time = 1.234;
            listener.TestFinished(nunitResult);
            Assert.AreEqual(TimeSpan.FromSeconds(1.234), testLog.LastResult.Duration);
        }

        [TestCaseSource("MessageTestSource")]
        public void TestOutput_CallsSendMessageCorrectly(string nunitMessage, string expectedMessage)
        {
            listener.TestOutput(new TestOutput(nunitMessage, TestOutputType.Out));
            Assert.AreEqual(1, testLog.SendMessageCalls);
            Assert.AreEqual(TestMessageLevel.Informational, testLog.LastMessageLevel);
            Assert.AreEqual(expectedMessage, testLog.LastMessage);
        }

        private void FakeTestMethod() { }

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
    }
}
