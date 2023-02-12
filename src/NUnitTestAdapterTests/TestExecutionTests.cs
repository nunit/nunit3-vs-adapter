// ***********************************************************************
// Copyright (c) 2012-2021 Charlie Poole, 2014-2023 Terje Sandstrom
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
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

using NUnit.Framework;
using NUnit.Tests;
using NUnit.Tests.Assemblies;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    /// <summary>
    /// ResultSummary Helper Class.
    /// </summary>
    public class ResultSummary
    {
        private readonly Dictionary<TestOutcome, int> summary;

        public ResultSummary(IEnumerable<TestResult> results)
        {
            summary = new Dictionary<TestOutcome, int>();

            foreach (TestResult result in results)
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

    [Ignore("These tests needs to rewritten as isolated tests")]
    [Category("TestExecution")]
    public class TestFilteringTests
    {
        private string mockAssemblyPath;
        [OneTimeSetUp]
        public void LoadMockassembly()
        {
            mockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");

            // Sanity check to be sure we have the correct version of mock-assembly.dll
            Assert.That(MockAssembly.TestsAtRuntime, Is.EqualTo(MockAssembly.Tests),
                "The reference to mock-assembly.dll appears to be the wrong version");
        }

        [TestCase("", 38, 5, 5)]
        [TestCase(null, 38, 5, 5)]
        [TestCase("cat == Special", 1)]
        [TestCase("cat == MockCategory", 2)]
        [TestCase("method =~ MockTest?", 5, 1)]
        [TestCase("method =~ MockTest? and cat != MockCategory", 3, 1)]
        [TestCase("namespace == ThisNamespaceDoesNotExist", 0)]
        [TestCase("test==NUnit.Tests.Assemblies.MockTestFixture", MockTestFixture.Tests - MockTestFixture.Explicit, 1, 1, TestName = "{m}_MockTestFixture")]
        [TestCase("test==NUnit.Tests.IgnoredFixture and method == Test2", 1, 1, TestName = "{m}_IgnoredFixture")]
        [TestCase("class==NUnit.Tests.Assemblies.MockTestFixture", MockTestFixture.Tests - MockTestFixture.Explicit, 1, 1)]
        [TestCase("name==MockTestFixture", MockTestFixture.Tests + NUnit.Tests.TestAssembly.MockTestFixture.Tests - MockTestFixture.Explicit, 1, 1)]
        [TestCase("cat==FixtureCategory", MockTestFixture.Tests, 1, 2)]
        [TestCase("cat!=Special", 38 - 1, 5, 4)]
        [TestCase("class==NUnit.Tests.Assemblies.MockTestFixture", MockTestFixture.Tests - MockTestFixture.Explicit, MockTestFixture.Ignored, 1)]
        [TestCase("class==NUnit.Tests.OneOfEach", 2, 1, 0)]
        [TestCase("cat==OneOfEachCat", 3, 1, 1)]
        public void TestsWhereShouldFilter(string filter, int expectedCount, int noOfSkipped = 0, int noOfNone = 0)
        {
            // Create a fake environment.
            var context = new FakeRunContext(new FakeRunSettingsForWhere(filter));
            var fakeFramework = new FakeFrameworkHandle();

            var executor = TestAdapterUtils.CreateExecutor();
            executor.RunTests(new[] { mockAssemblyPath }, context, fakeFramework);

            var completedRuns = fakeFramework.Events.Where(e => e.EventType == FakeFrameworkHandle.EventType.RecordEnd).ToList();
            TestContext.WriteLine(" ");
            foreach (var test in completedRuns)
            {
                TestContext.WriteLine($"{test.TestCase.DisplayName}:{test.TestOutcome}");
            }

            Assert.Multiple(() =>
            {
                Assert.That(completedRuns, Has.Exactly(expectedCount).Items, "Total number wrong");
                Assert.That(completedRuns.Count(o => o.TestOutcome == TestOutcome.Skipped), Is.EqualTo(noOfSkipped), "Skipped number wrong");
                Assert.That(completedRuns.Count(o => o.TestOutcome == TestOutcome.None), Is.EqualTo(noOfNone), "Explicit and inconclusive number wrong");
            });
        }
    }

    [Ignore("These tests needs to rewritten as isolated tests")]
    [Category("TestExecution")]
    public class TestExecutionTests
    {
        private string mockAssemblyPath;
        static readonly IRunContext Context = new FakeRunContext();

        private FakeFrameworkHandle testLog;
        ResultSummary Summary { get; set; }


        [OneTimeSetUp]
        public void LoadMockassembly()
        {
            mockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");

            // Sanity check to be sure we have the correct version of mock-assembly.dll
            Assert.That(MockAssembly.TestsAtRuntime, Is.EqualTo(MockAssembly.Tests),
                "The reference to mock-assembly.dll appears to be the wrong version");
            testLog = new FakeFrameworkHandle();

            // Load the NUnit mock-assembly.dll once for this test, saving
            // the list of test cases sent to the discovery sink
            TestAdapterUtils.CreateExecutor().RunTests(new[] { mockAssemblyPath }, Context, testLog);

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
            Console.WriteLine(" ");
            foreach (var ev in testLog.Events.FindAll(e => e.EventType == eventType))
                Console.WriteLine($"{ev.TestCase.DisplayName}");
            Assert.That(
                testLog.Events.FindAll(e => e.EventType == eventType).Count,
                Is.EqualTo(MockAssembly.ResultCount - BadFixture.Tests - IgnoredFixture.Tests - ExplicitFixture.Tests - MockTestFixture.Explicit - OneOfEach.ExplicitTests));
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

        static readonly TestCaseData[] Outcomes =
        {
            // NOTE: One inconclusive test is reported as None
            new TestCaseData(TestOutcome.Passed).Returns(MockAssembly.Success),
            new TestCaseData(TestOutcome.Failed).Returns(MockAssembly.ErrorsAndFailures),
            new TestCaseData(TestOutcome.Skipped).Returns(MockAssembly.Ignored),
            new TestCaseData(TestOutcome.None).Returns(MockAssembly.Explicit + 1),
            new TestCaseData(TestOutcome.NotFound).Returns(0)
        };

        [TestCaseSource(nameof(Outcomes))]
        public int TestOutcomeTotalsAreCorrect(TestOutcome outcome)
        {
            TestContext.WriteLine(" ");
            TestContext.WriteLine($"Looking for outcome: {outcome}");
            return testLog.Events
                .Count(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult && e.TestResult.Outcome == outcome);
        }

        [TestCase("MockTest3", TestOutcome.Passed, null, false)]
        [TestCase("FailingTest", TestOutcome.Failed, "Intentional failure", true)]
        [TestCase("TestWithException", TestOutcome.Failed, "System.Exception : Intentional Exception", true)]
        // NOTE: Should Inconclusive be reported as TestOutcome.None?
        [TestCase("ExplicitlyRunTest", TestOutcome.None, null, false)]
        [TestCase("InconclusiveTest", TestOutcome.None, "No valid data", false)]
        [TestCase("MockTest4", TestOutcome.Skipped, "ignoring this test method for now", false)]
        // NOTE: Should this be failed?
        [TestCase("NotRunnableTest", TestOutcome.Failed, "No arguments were provided", false)]
        public void TestResultIsReportedCorrectly(string name, TestOutcome outcome, string message, bool hasStackTrace)
        {
            var testResult = GetTestResult(name);

            Assert.That(testResult, Is.Not.Null, "Unable to find result for method: " + name);
            Assert.That(testResult.Outcome, Is.EqualTo(outcome));
            Assert.That(testResult.ErrorMessage, Is.EqualTo(message));
            if (hasStackTrace)
                Assert.That(testResult.ErrorStackTrace, Is.Not.Null, "Unable to find error stacktrace");
        }

        [Test]
        public void AttachmentsShowSupportMultipleFiles()
        {
            var test = GetTestResult(nameof(FixtureWithAttachment.AttachmentTest));
            Assert.That(test, Is.Not.Null, "Could not find test result");

            Assert.That(test.Attachments.Count, Is.EqualTo(1));

            var attachmentSet = test.Attachments[0];
            Assert.That(attachmentSet.Uri.OriginalString, Is.EqualTo(NUnitTestAdapter.ExecutorUri));
            Assert.That(attachmentSet.Attachments.Count, Is.EqualTo(2));

            VerifyAttachment(attachmentSet.Attachments[0], FixtureWithAttachment.Attachment1Name, FixtureWithAttachment.Attachment1Description);
            VerifyAttachment(attachmentSet.Attachments[1], FixtureWithAttachment.Attachment2Name, FixtureWithAttachment.Attachment2Description);
        }

#if NET462
        [Test]
        public void NativeAssemblyProducesWarning()
        {
            // Create a fake environment.
            var context = new FakeRunContext();
            var fakeFramework = new FakeFrameworkHandle();

            // Find the native exaple dll.
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "NativeTests.dll");
            Assert.That(File.Exists(path));

            // Try to execute tests from the dll.
            TestAdapterUtils.CreateExecutor().RunTests(
                new[] { path },
                context,
                fakeFramework);

            // Gather results.
            var testResults = fakeFramework.Events
               .Where(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult)
               .Select(e => e.TestResult)
               .ToList();

            // No tests found.
            Assert.That(testResults.Count, Is.EqualTo(0));

            // A warning about unsupported assebly format provided.
            var messages = fakeFramework.Events
                .Where(e => e.EventType == FakeFrameworkHandle.EventType.SendMessage &&
                   e.Message.Level == Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging.TestMessageLevel.Warning)
                .Select(o => o.Message.Text);
            Assert.That(messages, Has.Some.Contains("Assembly not supported"));
        }
#endif

        private static void VerifyAttachment(UriDataAttachment attachment, string expectedName, string expectedDescription)
        {
            Assert.Multiple(() =>
            {
                Assert.That(attachment.Uri.OriginalString, Does.EndWith(expectedName));
                Assert.That(attachment.Description, Is.EqualTo(expectedDescription));
            });
        }

        /// <summary>
        /// Tries to get the <see cref="TestResult"/> with the specified DisplayName.
        /// </summary>
        /// <param name="displayName">DisplayName to search for.</param>
        /// <returns>The first testresult with the specified DisplayName, or <c>null</c> if none where found.</returns>
        private TestResult GetTestResult(string displayName)
        {
            return testLog.Events
                            .Where(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult && e.TestResult.TestCase.DisplayName == displayName)
                            .Select(e => e.TestResult)
                            .FirstOrDefault();
        }
    }

    [Category("TestExecution")]
    public class TestExecutionTestsForTestOutput
    {
        private string _mockAssemblyPath;
        private string _mockAssemblyFolder;

        private FakeFrameworkHandle testLog;
        ResultSummary Summary { get; set; }


        [OneTimeSetUp]
        public void LoadMockAssembly()
        {
            _mockAssemblyPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");
            _mockAssemblyFolder = Path.GetDirectoryName(_mockAssemblyPath);
            // Sanity check to be sure we have the correct version of mock-assembly.dll
            Assert.That(MockAssembly.TestsAtRuntime, Is.EqualTo(MockAssembly.Tests),
                "The reference to mock-assembly.dll appears to be the wrong version");
            testLog = new FakeFrameworkHandle();

            // Load the NUnit mock-assembly.dll once for this test, saving
            // the list of test cases sent to the discovery sink

            var testResults = testLog.Events
                .Where(e => e.EventType == FakeFrameworkHandle.EventType.RecordResult)
                .Select(e => e.TestResult)
                .ToList();
            Summary = new ResultSummary(testResults);
        }
#if NET462
        [Test]
        public void ThatTestOutputXmlHasBeenCreatedBelowAssemblyFolder()
        {
            var context = new FakeRunContext(new FakeRunSettingsForTestOutput());

            TestAdapterUtils.CreateExecutor().RunTests(new[] { _mockAssemblyPath }, context, testLog);

            var expectedFolder = Path.Combine(_mockAssemblyFolder, "TestResults");
            Assert.That(Directory.Exists(expectedFolder), $"Folder {expectedFolder} not created");
            var expectedFile = Path.Combine(expectedFolder, "mock-assembly.xml");
            Assert.That(File.Exists(expectedFile), $"File {expectedFile} not found");
        }


        [Test]
        public void ThatTestOutputXmlHasBeenAtWorkDirLocation()
        {
            var temp = Path.GetTempPath();
            var context = new FakeRunContext(new FakeRunSettingsForTestOutputAndWorkDir("TestResult", Path.Combine(temp, "NUnit")));

            var executor = TestAdapterUtils.CreateExecutor();
            executor.RunTests(new[] { _mockAssemblyPath }, context, testLog);
            var expectedFolder = Path.Combine(Path.GetTempPath(), "NUnit", "TestResult");
            Assert.That(Directory.Exists(expectedFolder), $"Folder {expectedFolder} not created");
            var expectedFile = Path.Combine(expectedFolder, "mock-assembly.xml");
            Assert.That(File.Exists(expectedFile), $"File {expectedFile} not found");
        }
#endif

    }
}
