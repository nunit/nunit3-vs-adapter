﻿// ***********************************************************************
// Copyright (c) 2012-2017 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Framework;
using NUnit.Tests;
using NUnit.Tests.Assemblies;
using NUnit.Tests.Singletons;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;
using System.Linq;

namespace NUnit.VisualStudio.TestAdapter.Tests
{

    [Category("TestExecution")]
    public class TestExecutionTests
    {
        private string MockAssemblyPath; 
        static readonly IRunContext Context = new FakeRunContext();

        private FakeFrameworkHandle testLog;
        ResultSummary Summary { get;  set; }    


        [OneTimeSetUp]
        public void LoadMockassembly()
        {
            MockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");

            // Sanity check to be sure we have the correct version of mock-assembly.dll
            Assert.That(MockAssembly.TestsAtRuntime , Is.EqualTo(MockAssembly.Tests),
                "The reference to mock-assembly.dll appears to be the wrong version");
            testLog = new FakeFrameworkHandle();

            // Load the NUnit mock-assembly.dll once for this test, saving
            // the list of test cases sent to the discovery sink
            TestAdapterUtils.CreateExecutor().RunTests(new[] { MockAssemblyPath }, Context, testLog);

            var testResults = testLog.Events
               .Where(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult)
               .Select(e => e.TestResult)
               .ToList();
            this.Summary = new ResultSummary(testResults);
        }

        [Test]
        public void DumpEvents()
        {
            foreach (var ev in testLog.Events)
            {
                TestContext.Write(ev.EventType + ": ");
                if (ev.TestResult != null)
                    TestContext.WriteLine("{0} {1}", ev.TestResult.TestCase.FullyQualifiedName, ev.TestResult.Outcome);
                else if (ev.TestCase != null)
                    TestContext.WriteLine(ev.TestCase.FullyQualifiedName);
                else if (ev.Message.Text != null)
                    TestContext.WriteLine(ev.Message.Text);
                else
                    TestContext.WriteLine();
            }
        }

        [Test]
        public void CorrectNumberOfTestCasesWereStarted()
        {
            const FakeFrameworkHandle.EventType eventType = FakeFrameworkHandle.EventType.RecordStart;
            foreach (var ev in testLog.Events.FindAll(e => e.EventType == eventType))
                Console.WriteLine(ev.TestCase.DisplayName);
            Assert.That(
                testLog.Events.FindAll(e => e.EventType == eventType).Count,
                Is.EqualTo(MockAssembly.ResultCount - BadFixture.Tests - IgnoredFixture.Tests - ExplicitFixture.Tests));
        }

        [Test]
        public void CorrectNumberOfTestCasesWereEnded()
        {
            const FakeFrameworkHandle.EventType eventType = FakeFrameworkHandle.EventType.RecordEnd;
            Assert.That(
                testLog.Events.FindAll(e => e.EventType == eventType).Count,
                Is.EqualTo(MockAssembly.ResultCount));
        }

        [Test]
        public void CorrectNumberOfResultsWereReceived()
        {
            const FakeFrameworkHandle.EventType eventType = FakeFrameworkHandle.EventType.RecordResult;
            Assert.That(
                testLog.Events.FindAll(e => e.EventType == eventType).Count,
                Is.EqualTo(MockAssembly.ResultCount));
        }

        static readonly TestCaseData[] outcomes =
        {
            // NOTE: One inconclusive test is reported as None
            new TestCaseData(TestOutcome.Passed).Returns(MockAssembly.Success),
            new TestCaseData(TestOutcome.Failed).Returns(MockAssembly.ErrorsAndFailures),
            new TestCaseData(TestOutcome.Skipped).Returns(MockAssembly.Ignored),
            new TestCaseData(TestOutcome.None).Returns(MockAssembly.Explicit + 1),
            new TestCaseData(TestOutcome.NotFound).Returns(0)
        };

        [TestCaseSource("outcomes")]
        public int TestOutcomeTotalsAreCorrect(TestOutcome outcome)
        {
            return testLog.Events
                .Count(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult && e.TestResult.Outcome == outcome);
        }

        [TestCase("NUnit.Tests.Assemblies.MockTestFixture.MockTest3", TestOutcome.Passed, "Succeeded!", false)]
        [TestCase("NUnit.Tests.Assemblies.MockTestFixture.FailingTest", TestOutcome.Failed, "Intentional failure", true)]
        [TestCase("NUnit.Tests.Assemblies.MockTestFixture.TestWithException", TestOutcome.Failed, "System.Exception : Intentional Exception", true)]
        // NOTE: Should Inconclusive be reported as TestOutcome.None?
        [TestCase("NUnit.Tests.Assemblies.MockTestFixture.ExplicitlyRunTest", TestOutcome.None, null, false)]
        [TestCase("NUnit.Tests.Assemblies.MockTestFixture.InconclusiveTest", TestOutcome.None, "No valid data", false)]
        [TestCase("NUnit.Tests.Assemblies.MockTestFixture.MockTest4", TestOutcome.Skipped, "ignoring this test method for now", false)]
        // NOTE: Should this be failed?
        [TestCase("NUnit.Tests.Assemblies.MockTestFixture.NotRunnableTest", TestOutcome.Failed, "No arguments were provided", false)]
        public void TestResultIsReportedCorrectly(string name, TestOutcome outcome, string message, bool hasStackTrace)
        {
            var testResult = GetTestResult(name);

            Assert.NotNull(testResult, "Unable to find result for method: " + name);
            Assert.That(testResult.Outcome, Is.EqualTo(outcome));
            Assert.That(testResult.ErrorMessage, Is.EqualTo(message));
            if (hasStackTrace)
                Assert.NotNull(testResult.ErrorStackTrace);
        }

        [Test]
        public void AttachmentsShowSupportMultipleFiles()
        {
            var test = GetTestResult(typeof(FixtureWithAttachment).FullName + ".AttachmentTest");
            Assert.That(test, Is.Not.Null, "Could not find test result");

            Assert.That(test.Attachments.Count, Is.EqualTo(1));

            var attachmentSet = test.Attachments[0];
            Assert.That(attachmentSet.Uri.OriginalString, Is.EqualTo(NUnitTestAdapter.ExecutorUri));
            Assert.That(attachmentSet.Attachments.Count, Is.EqualTo(2));

            VerifyAttachment(attachmentSet.Attachments[0], FixtureWithAttachment.Attachment1Name, FixtureWithAttachment.Attachment1Description);
            VerifyAttachment(attachmentSet.Attachments[1], FixtureWithAttachment.Attachment2Name, FixtureWithAttachment.Attachment2Description);
        }

        private static void VerifyAttachment(UriDataAttachment attachment, string expectedName, string expectedDescription)
        {
            Assert.Multiple(() =>
            {
                Assert.That(attachment.Uri.OriginalString, Does.EndWith(expectedName));
                Assert.That(attachment.Description, Is.EqualTo(expectedDescription));
            });
        }

        /// <summary>
        /// Tries to get the <see cref="TestResult"/> with the specified DisplayName
        /// </summary>
        /// <param name="fullyQualifiedName">DisplayName to search for</param>
        /// <returns>The first testresult with the specified DisplayName, or <c>null</c> if none where found</returns>
        private TestResult GetTestResult(string fullyQualifiedName)
        {
            return testLog.Events
                            .Where(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult && e.TestResult.TestCase.FullyQualifiedName == fullyQualifiedName)
                            .Select(e => e.TestResult)
                            .FirstOrDefault();
        }

        #region Nested ResultSummary Helper Class

        private class ResultSummary
        {
            private readonly Dictionary<TestOutcome, int> summary;

            public ResultSummary(IEnumerable<TestResult> results)
            {
                summary = new Dictionary<TestOutcome, int>();
                
                foreach(TestResult result in results)
                {
                    var outcome = result.Outcome;
                    summary[outcome] = GetCount(outcome) + 1;
                }
            }

            private int GetCount(TestOutcome outcome)
            {
                return summary.ContainsKey(outcome)
                    ? summary[outcome]
                    : 0;
            }
        }

        #endregion
    }
}
