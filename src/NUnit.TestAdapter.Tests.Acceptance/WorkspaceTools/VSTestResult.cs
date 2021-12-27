using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct VSTestResult
    {
        public ProcessRunResult ProcessRunResult { get; }
        public string Outcome { get; }
        public VSTestResultCounters Counters { get; }
        public IReadOnlyList<string> RunErrors { get; }
        public IReadOnlyList<string> RunWarnings { get; }

        public VSTestResult(ProcessRunResult processRunResult)
        {
            ProcessRunResult = processRunResult;
            Outcome = "";
            Counters = VSTestResultCounters.CreateEmptyCounters();
            RunErrors = Array.Empty<string>();
            RunWarnings = Array.Empty<string>();
        }


        public VSTestResult(ProcessRunResult processRunResult, string outcome, VSTestResultCounters counters, IReadOnlyList<string> runErrors = null, IReadOnlyList<string> runWarnings = null)
        {
            ProcessRunResult = processRunResult;
            Outcome = outcome;
            Counters = counters;
            RunErrors = runErrors ?? Array.Empty<string>();
            RunWarnings = runWarnings ?? Array.Empty<string>();
        }

        public static VSTestResult Load(ProcessRunResult processRunResult, string trxFilePath)
        {
            var trx = XDocument.Load(trxFilePath);

            var ns = (XNamespace)"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            var resultSummary = trx.Root.Element(ns + "ResultSummary");
            var counters = resultSummary.Element(ns + "Counters");

            var runInfos = resultSummary.Element(ns + "RunInfos")?.Elements().Select(runInfo => (
                Outcome: runInfo.Attribute("outcome")?.Value,
                Text: runInfo.Element(ns + "Text")?.Value ?? string.Empty));

            return new VSTestResult(
                processRunResult,
                (string)resultSummary.Attribute("outcome"),
                new VSTestResultCounters(
                    (int)counters.Attribute("total"),
                    (int)counters.Attribute("executed"),
                    (int)counters.Attribute("passed"),
                    (int)counters.Attribute("failed"),
                    (int)counters.Attribute("error"),
                    (int)counters.Attribute("timeout"),
                    (int)counters.Attribute("aborted"),
                    (int)counters.Attribute("inconclusive"),
                    (int)counters.Attribute("passedButRunAborted"),
                    (int)counters.Attribute("notRunnable"),
                    (int)counters.Attribute("notExecuted"),
                    (int)counters.Attribute("disconnected"),
                    (int)counters.Attribute("warning"),
                    (int)counters.Attribute("completed"),
                    (int)counters.Attribute("inProgress"),
                    (int)counters.Attribute("pending")),
                runErrors: runInfos?.Where(i => i.Outcome == "Error").Select(i => i.Text).ToList(),
                runWarnings: runInfos?.Where(i => i.Outcome == "Warning").Select(i => i.Text).ToList());
        }

        public void AssertSinglePassingTest()
        {
            Assert.That(RunErrors, Is.Empty);

            if (RunWarnings.Any())
                Assert.Fail("Unexpected VSTest warnings. Standard output:" + Environment.NewLine + ProcessRunResult.StdOut);

            Assert.That(Counters.Total, Is.EqualTo(1), "There should be a single test in the test results.");
            Assert.That(Counters.Passed, Is.EqualTo(1), "There should be a single test passing in the test results.");
        }

        public override string ToString()
        {
            return Outcome + ", " + Counters;
        }
    }
}
