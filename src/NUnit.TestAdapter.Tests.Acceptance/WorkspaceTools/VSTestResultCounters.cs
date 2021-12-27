using System.Diagnostics;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct VSTestResultCounters
    {
        public int Total { get; }
        public int Executed { get; }
        public int Passed { get; }
        public int Failed { get; }
        public int Error { get; }
        public int Timeout { get; }
        public int Aborted { get; }
        public int Inconclusive { get; }
        public int PassedButRunAborted { get; }
        public int NotRunnable { get; }
        public int NotExecuted { get; }
        public int Disconnected { get; }
        public int Warning { get; }
        public int Completed { get; }
        public int InProgress { get; }
        public int Pending { get; }

        public static VSTestResultCounters CreateEmptyCounters() => new (0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        public VSTestResultCounters(
            int total,
            int executed,
            int passed,
            int failed,
            int error,
            int timeout,
            int aborted,
            int inconclusive,
            int passedButRunAborted,
            int notRunnable,
            int notExecuted,
            int disconnected,
            int warning,
            int completed,
            int inProgress,
            int pending)
        {
            Total = total;
            Executed = executed;
            Passed = passed;
            Failed = failed;
            Error = error;
            Timeout = timeout;
            Aborted = aborted;
            Inconclusive = inconclusive;
            PassedButRunAborted = passedButRunAborted;
            NotRunnable = notRunnable;
            NotExecuted = notExecuted;
            Disconnected = disconnected;
            Warning = warning;
            Completed = completed;
            InProgress = inProgress;
            Pending = pending;
        }

        public override string ToString()
        {
            return "Total: " + Total;
        }
    }
}
