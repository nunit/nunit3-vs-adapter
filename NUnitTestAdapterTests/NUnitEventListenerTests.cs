// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NUnit.Core;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NUnitEventListenerTests : ITestLog
    {
        private readonly static string assemblyName = "MyAssembly.dll";
        private readonly static Uri fakeUri = new Uri(NUnitTestExecutor.ExecutorUri);

        private NUnitEventListener listener;
        private TestCase fakeTestCase;
        private NUnit.Core.ITest fakeNUnitTest;

        private int callCount;
        private TestResult testResult;

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod = this.GetType().GetMethod("FakeTestMethod", BindingFlags.Instance | BindingFlags.NonPublic);
            fakeNUnitTest = new NUnitTestMethod(fakeTestMethod);

            fakeTestCase = new TestCase(fakeNUnitTest.TestName.FullName, fakeUri);

            var map = new Dictionary<string, TestCase>();
            map.Add(fakeTestCase.Name, fakeTestCase);
            this.listener = new NUnitEventListener(this, map, assemblyName);
            this.callCount = 0;
        }

        [Test]
        public void TestFinishedCallsTestLog()
        {
            listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
            Assert.AreEqual(1, callCount);
        }

        //[Test]
        //public void TestFinishedDoesNothingIfNameIsNotInMap()
        //{
        //    fakeNUnitTest.TestName.FullName = "This.Is.Not.In.Log";
        //    listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
        //    Assert.AreEqual(0, callCount);
        //}

        [Test]
        public void TestResultHasCorrectTestName()
        {
            listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
            Assert.AreEqual(fakeNUnitTest.TestName.FullName, testResult.TestCase.DisplayName);
        }

        [Test]
        public void TestResultHasCorrectComputerName()
        {
            listener.TestFinished(new NUnit.Core.TestResult(fakeNUnitTest));
            Assert.AreEqual(Environment.MachineName, testResult.ComputerName);
        }

        [TestCase(ResultState.Success,      TestOutcome.Passed, null)]
        [TestCase(ResultState.Failure,      TestOutcome.Failed,  "My failure message")]
        [TestCase(ResultState.Error,        TestOutcome.Failed,  "Error!")]
        [TestCase(ResultState.Cancelled,    TestOutcome.None,    null)]
        [TestCase(ResultState.Inconclusive, TestOutcome.None,    null)]
        [TestCase(ResultState.NotRunnable,  TestOutcome.Failed,  "No constructor")]
        [TestCase(ResultState.Skipped,      TestOutcome.Skipped, null)]
        [TestCase(ResultState.Ignored,      TestOutcome.Skipped, "my reason")]
        public void TestResultHasCorrectOutcome(ResultState resultState, TestOutcome outcome, string message)
        {
            var nunitResult = new NUnit.Core.TestResult(fakeNUnitTest);
            nunitResult.SetResult(resultState, message, null);
            listener.TestFinished(nunitResult);
            Assert.AreEqual(outcome, testResult.Outcome);
            Assert.AreEqual(message, testResult.ErrorMessage);
        }

        [Test]
        public void TestResultHasCorrectDuration()
        {
            var nunitResult = new NUnit.Core.TestResult(fakeNUnitTest);
            nunitResult.Success();
            nunitResult.Time = 1.234;
            listener.TestFinished(nunitResult);
            Assert.AreEqual(TimeSpan.FromSeconds(1.234), testResult.Duration);
        }

        private void FakeTestMethod() { }

        #region ITestLog Members

        public void SendTestCaseStart(TestCase testCase)
        {
            throw new NotImplementedException();
        }

        public void SendTestResult(Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult testResult)
        {
            this.callCount++;
            this.testResult = testResult;
        }

        public void SendMessage(Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel testMessageLevel, string message)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
