using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct VSTestResult
    {
        public string Outcome { get; }
        public VSTestResultCounters Counters { get; }
        public IReadOnlyList<string> RunErrors { get; }

        public VSTestResult(string outcome, VSTestResultCounters counters, IReadOnlyList<string> runErrors = null)
        {
            Outcome = outcome;
            Counters = counters;
            RunErrors = runErrors ?? Array.Empty<string>();
        }

        public static VSTestResult Load(string trxFilePath)
        {
            var trx = XDocument.Load(trxFilePath);

            var ns = (XNamespace)"http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

            var resultSummary = trx.Root.Element(ns + "ResultSummary");
            var counters = resultSummary.Element(ns + "Counters");

            return new VSTestResult(
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
                runErrors: resultSummary.Element(ns + "RunInfos")
                    ?.Elements()
                    .Where(runInfo => runInfo.Attribute("outcome")?.Value == "Error")
                    .Select(runInfo => runInfo.Element(ns + "Text")?.Value ?? string.Empty)
                    .ToList());
        }

        public void AssertSinglePassingTest()
        {
            Assert.That(RunErrors, Is.Empty);
            Assert.That(Counters.Total, Is.EqualTo(1), "There should be a single test in the test results.");
            Assert.That(Counters.Passed, Is.EqualTo(1), "There should be a single test passing in the test results.");
        }

        public override string ToString()
        {
            return Outcome + ", " + Counters;
        }
    }
}
