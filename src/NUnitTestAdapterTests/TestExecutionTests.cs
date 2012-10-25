// ****************************************************************
// Copyright (c) 2012 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using NUnit.Tests.Assemblies;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class TestExecutionTests
    {
        static readonly string mockAssemblyPath = Path.GetFullPath("mock-assembly.dll");
        static readonly IRunContext context = new FakeRunContext();

        private List<TestCase> testCases;
        private List<TestResult> testResults;
        private ResultSummary summary;
        private FakeFrameworkHandle testLog;

        [TestFixtureSetUp]
        public void LoadMockassembly()
        {
            // Sanity check to be sure we have the correct version of mock-assembly.dll
            Assert.That(MockAssembly.Tests, Is.EqualTo(31),
                "The reference to mock-assembly.dll appears to be the wrong version");

            testCases = new List<TestCase>();
            testResults = new List<TestResult>();
            testLog = new FakeFrameworkHandle();

            // Load the NUnit mock-assembly.dll once for this test, saving
            // the list of test cases sent to the discovery sink
            ((ITestExecutor)new NUnitTestExecutor()).RunTests(
                new[] { mockAssemblyPath }, 
                context, 
                testLog);

            this.summary = new ResultSummary(testResults);
        }

        [Test]
        [Category("TestExecution")]
        public void CorrectNumberOfTestCasesWereStarted()
        {
            var eventType = FakeFrameworkHandle.EventType.RecordStart;
            Assert.That(
                testLog.Events.FindAll(e => e.EventType == eventType).Count,
                Is.EqualTo(MockAssembly.ResultCount));
        }

        [Test]
        [Category("TestExecution")]
        public void CorrectNumberOfTestCasesWereEnded()
        {
            var eventType = FakeFrameworkHandle.EventType.RecordEnd;
            Assert.That(
                testLog.Events.FindAll(e => e.EventType == eventType).Count,
                Is.EqualTo(MockAssembly.ResultCount));
        }

        [Test]
        [Category("TestExecution")]
        public void CorrectNumberOfResultsWereReceived()
        {
            var eventType = FakeFrameworkHandle.EventType.RecordResult;
            Assert.That(
                testLog.Events.FindAll(e => e.EventType == eventType).Count,
                Is.EqualTo(MockAssembly.ResultCount));
        }

        TestCaseData[] outcomes = new TestCaseData[] {
            // NOTE: One inconclusive test is reported as None
            new TestCaseData(TestOutcome.Passed).Returns(MockAssembly.TestsRun - MockAssembly.Errors - MockAssembly.Failures - 1).SetCategory("TestExecution"),
            new TestCaseData(TestOutcome.Failed).Returns(MockAssembly.Errors + MockAssembly.Failures + MockAssembly.NotRunnable).SetCategory("TestExecution"),
            new TestCaseData(TestOutcome.Skipped).Returns(MockAssembly.NotRun - MockAssembly.Explicit - MockAssembly.NotRunnable).SetCategory("TestExecution"),
            new TestCaseData(TestOutcome.None).Returns(1).SetCategory("TestExecution"),
            new TestCaseData(TestOutcome.NotFound).Returns(0).SetCategory("TestExecution")
        };

        [TestCaseSource("outcomes")]
        public int TestOutcomeTotalsAreCorrect(TestOutcome outcome)
        {
            return testLog.Events
                .FindAll(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult)
                .ConvertAll(e => e.TestResult)
                .FindAll(r => r.Outcome == outcome)
                .Count;
        }

        [TestCase("MockTest3", TestOutcome.Passed, "Succeeded!", true, Category = "TestExecution")]
        [TestCase("FailingTest", TestOutcome.Failed, "Intentional failure", true, Category = "TestExecution")]
        [TestCase("TestWithException", TestOutcome.Failed, "System.ApplicationException : Intentional Exception", true, Category = "TestExecution")]
        // NOTE: Should Inconclusive be reported as TestOutcome.None?
        [TestCase("InconclusiveTest", TestOutcome.None, "No valid data", false, Category = "TestExecution")]
        [TestCase("MockTest4", TestOutcome.Skipped, "ignoring this test method for now", false, Category = "TestExecution")]
        // NOTE: Should this be failed?
        [TestCase("NotRunnableTest", TestOutcome.Failed, "No arguments were provided", false, Category = "TestExecution")]
        public void TestResultIsReportedCorrectly(string name, TestOutcome outcome, string message, bool hasStackTrace)
        {
            var testResult = testLog.Events
                .FindAll(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult)
                .ConvertAll(e => e.TestResult)
                .Find(r => r.TestCase.DisplayName == name);

            Assert.NotNull(testResult, "Unable to find result for method: " + name);
            Assert.That(testResult.Outcome, Is.EqualTo(outcome));
            Assert.That(testResult.ErrorMessage, Is.EqualTo(message));
            if (hasStackTrace)
                Assert.NotNull(testResult.ErrorStackTrace);
        }

        [Test]
        [Category("TestExecution")]
        public void ExplicitTestDoesNotShowUpInResults()
        {
            Assert.Null(testResults.Find(r => r.TestCase.DisplayName == "ExplicitlyRunTest"));
        }

        #region Nested ResultSummary Helper Class

        private class ResultSummary
        {
            private Dictionary<TestOutcome, int> summary;

            public ResultSummary(List<TestResult> results)
            {
                this.summary = new Dictionary<TestOutcome, int>();
                
                foreach(TestResult result in results)
                {
                    var outcome = result.Outcome;
                    summary[outcome] = GetCount(outcome) + 1;
                }
            }

            public int GetCount(TestOutcome outcome)
            {
                return summary.ContainsKey(outcome)
                    ? summary[outcome]
                    : 0;
            }
        }

        #endregion
    }
}
