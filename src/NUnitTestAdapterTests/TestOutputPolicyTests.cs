using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests;

/// <summary>
/// Issue 1515. Uses real AdapterSettings, so the defaults set by AgentMode are part of what is tested.
/// AgentMode is always set explicitly, because Auto depends on the environment the tests run in.
/// </summary>
public class TestOutputPolicyTests
{
    private const string Off = "<AgentMode>Off</AgentMode>";
    private const string On = "<AgentMode>On</AgentMode>";

    private static TestOutputPolicy PolicyFor(string nunitSettings)
    {
        var settings = new AdapterSettings(new TestLogger(new MessageLoggerStub()));
        settings.Load($"<RunSettings><NUnit>{nunitSettings}</NUnit></RunSettings>");
        return new TestOutputPolicy(settings);
    }

    [TestCase(TestOutcome.Passed)]
    [TestCase(TestOutcome.Failed)]
    [TestCase(TestOutcome.Skipped)]
    [TestCase(TestOutcome.None)]
    public void OffShowsEverythingByDefault(TestOutcome outcome)
    {
        var policy = PolicyFor(Off);
        Assert.Multiple(() =>
        {
            Assert.That(policy.ShowsConsoleOutput, Is.True);
            Assert.That(policy.ShowsReason(outcome), Is.True);
            Assert.That(policy.ShowsLiveOutput(isErrorStream: false), Is.True);
            Assert.That(policy.ShowsLiveOutput(isErrorStream: true), Is.True);
            Assert.That(policy.ConsoleOutputLevel, Is.EqualTo(TestMessageLevel.Informational));
            Assert.That(policy.StackTraceFor("trace", isParentFailure: false), Is.EqualTo("trace"));
        });
    }

    [TestCase(TestOutcome.Passed)]
    [TestCase(TestOutcome.Failed)]
    [TestCase(TestOutcome.Skipped)]
    public void OffWithConsoleOutZeroShowsAndCollectsNothing(TestOutcome outcome)
    {
        var policy = PolicyFor(Off + "<ConsoleOut>0</ConsoleOut>");
        Assert.Multiple(() =>
        {
            Assert.That(policy.ShowsConsoleOutput, Is.False);
            Assert.That(policy.ShowsReason(outcome), Is.False);
            Assert.That(policy.CollectsOutput(isErrorStream: false), Is.False);
            Assert.That(policy.CollectsOutput(isErrorStream: true), Is.False);
        });
    }

    [Test]
    public void OffWithConsoleOutTwoSendsWarnings()
    {
        Assert.That(PolicyFor(Off + "<ConsoleOut>2</ConsoleOut>").ConsoleOutputLevel, Is.EqualTo(TestMessageLevel.Warning));
    }

    [Test]
    public void OffHonoursExplicitShowStdoutFailed()
    {
        var policy = PolicyFor(Off + "<ShowStdout>Failed</ShowStdout>");
        Assert.Multiple(() =>
        {
            Assert.That(policy.ShowsConsoleOutput, Is.False);
            Assert.That(policy.ShowsLiveOutput(isErrorStream: false), Is.False);
            Assert.That(policy.CollectsOutput(isErrorStream: false), Is.True);
            Assert.That(policy.ShowsLiveOutput(isErrorStream: true), Is.True);
        });
    }

    [Test]
    public void OnLeavesOutputToTheTestResult()
    {
        var policy = PolicyFor(On);
        Assert.Multiple(() =>
        {
            Assert.That(policy.ShowsConsoleOutput, Is.False);
            Assert.That(policy.ShowsLiveOutput(isErrorStream: false), Is.False);
            Assert.That(policy.ShowsLiveOutput(isErrorStream: true), Is.False);
            Assert.That(policy.CollectsOutput(isErrorStream: false), Is.True);
            Assert.That(policy.CollectsOutput(isErrorStream: true), Is.True);
        });
    }

    [TestCase(TestOutcome.Failed, true)]
    [TestCase(TestOutcome.Skipped, false)]
    [TestCase(TestOutcome.None, false)]
    [TestCase(TestOutcome.Passed, false)]
    public void OnShowsReasonsOfFailedTestsOnly(TestOutcome outcome, bool expected)
    {
        Assert.That(PolicyFor(On).ShowsReason(outcome), Is.EqualTo(expected));
    }

    [Test]
    public void OnFiltersStackTraces()
    {
        const string trace = """
               at Noise.Level2() in Noise.cs:line 25
               at NUnit.Framework.Internal.Commands.TestMethodCommand.Execute(TestExecutionContext context)
            """;
        Assert.That(PolicyFor(On).StackTraceFor(trace, isParentFailure: false), Is.EqualTo("   at Noise.Level2() in Noise.cs:line 25"));
    }

    [TestCase("Full", false, "trace")]
    [TestCase("Full", true, "trace")]
    [TestCase("None", false, null)]
    public void StackTraceFollowsStackTraceMode(string mode, bool isParentFailure, string expected)
    {
        var policy = PolicyFor(Off + $"<StackTrace>{mode}</StackTrace>");
        Assert.That(policy.StackTraceFor("trace", isParentFailure), Is.EqualTo(expected));
    }

    [Test]
    public void StackTraceForSuitesCanBeTurnedOffSeparately()
    {
        var policy = PolicyFor(Off + "<IncludeStackTraceForSuites>false</IncludeStackTraceForSuites>");
        Assert.Multiple(() =>
        {
            Assert.That(policy.StackTraceFor("trace", isParentFailure: true), Is.Null);
            Assert.That(policy.StackTraceFor("trace", isParentFailure: false), Is.EqualTo("trace"));
        });
    }
}
