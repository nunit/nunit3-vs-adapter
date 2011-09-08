// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using NUnit.Tests.Assemblies;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class TestExecutionTests : ITestLog
    {
        static readonly string mockAssemblyPath = Path.GetFullPath("mock-assembly.dll");
        static readonly IRunContext context = new MyRunContext();

        private List<TestCase> testCases;
        private List<TestResult> testResults;
        private ResultSummary summary;

        [TestFixtureSetUp]
        public void LoadMockassembly()
        {
            // Sanity check to be sure we have the correct version of mock-assembly.dll
            Assert.That(MockAssembly.Tests, Is.EqualTo(31),
                "The reference to mock-assembly.dll appears to be the wrong version");

            testCases = new List<TestCase>();
            testResults = new List<TestResult>();

            // Load the NUnit mock-assembly.dll once for this test, saving
            // the list of test cases sent to the discovery sink
            ((ITestExecutor)new NUnitTestExecutor()).RunTests(new[] { mockAssemblyPath }, context, this);

            this.summary = new ResultSummary(testResults);
        }

        [Test]
        public void CorrectNumberOfTestCasesWereStarted()
        {

            Assert.That(testCases.Count, Is.EqualTo(MockAssembly.ResultCount));
        }

        [Test]
        public void CorrectNumberOfResultsWereReceived()
        {

            Assert.That(testResults.Count, Is.EqualTo(MockAssembly.ResultCount));
        }

        TestCaseData[] outcomes = new TestCaseData[] {
            // NOTE: One inconclusive test is reported as None
            new TestCaseData(TestOutcome.Passed).Returns(MockAssembly.TestsRun - MockAssembly.ErrorsAndFailures - 1),
            new TestCaseData(TestOutcome.Failed).Returns(MockAssembly.ErrorsAndFailures + MockAssembly.NotRunnable),
            new TestCaseData(TestOutcome.Skipped).Returns(MockAssembly.NotRun - MockAssembly.Explicit - MockAssembly.NotRunnable),
            new TestCaseData(TestOutcome.None).Returns(1),
            new TestCaseData(TestOutcome.NotFound).Returns(0)
        };

        [TestCaseSource("outcomes")]
        public int TestOutcomeTotalsAreCorrect(TestOutcome outcome)
        {
            return summary.GetCount(outcome);
        }

        [TestCase("MockTest3", TestOutcome.Passed, "Succeeded!", true)]
        [TestCase("FailingTest", TestOutcome.Failed, "Intentional failure", true)]
        [TestCase("TestWithException", TestOutcome.Failed, "System.ApplicationException : Intentional Exception", true)]
        // NOTE: Should Inconclusive be reported as TestOutcome.None?
        [TestCase("InconclusiveTest", TestOutcome.None, "No valid data", false)]
        [TestCase("MockTest4", TestOutcome.Skipped, "ignoring this test method for now", false)]
        // NOTE: Should this be failed?
        [TestCase("NotRunnableTest", TestOutcome.Failed, "No arguments were provided", false)]
        public void TestResultIsReportedCorrectly(string name, TestOutcome outcome, string message, bool hasStackTrace)
        {
            var testResult = testResults.Find(r => r.TestCase.DisplayName == name);
            Assert.NotNull(testResult, "Unable to find result for method: " + name);
            Assert.That(testResult.Outcome, Is.EqualTo(outcome));
            Assert.That(testResult.ErrorMessage, Is.EqualTo(message));
            if (hasStackTrace)
                Assert.NotNull(testResult.ErrorStackTrace);
        }

        [Test]
        public void ExplicitTestDoesNotShowUpInResults()
        {
            Assert.Null(testResults.Find(r => r.TestCase.DisplayName == "ExplicitlyRunTest"));
        }

        #region ITestLog Members

        void ITestLog.SendTestCaseStart(TestCase testCase)
        {
            testCases.Add(testCase);
        }

        void ITestLog.SendTestResult(TestResult testResult)
        {
            testResults.Add(testResult);
        }

        #endregion

        #region IMessageLogger Members

        void IMessageLogger.SendMessage(Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel testMessageLevel, string message)
        {
            //throw new NotImplementedException();
        }

        #endregion

        #region Nested MyRunContext Class

        private class MyRunContext : IRunContext
        {
            IRunSettings IRunContext.RunSettings
            {
                get { throw new NotImplementedException(); }
            }
        }

        #endregion

        #region Nested MyRunSettings Class

        private class MyRunSettings : IRunSettings
        {
            ISettingsProvider IRunSettings.GetSettings(string settingsName)
            {
                throw new NotImplementedException();
            }

            T IRunSettings.GetSettings<T>(string settingsName)
            {
                throw new NotImplementedException();
            }

            void IRunSettings.LoadSettingsFile(string path)
            {
                throw new NotImplementedException();
            }

            void IRunSettings.LoadSettingsXml(string settings)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

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
