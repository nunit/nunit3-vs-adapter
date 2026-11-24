using System.Collections.Generic;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests;

public class LlmEnvironmentDetectorTests
{
    private static bool DetectsWith(string variable, string value)
    {
        var environment = new Dictionary<string, string> { [variable] = value };
        return LlmEnvironmentDetector.IsAgentEnvironment(name => environment.TryGetValue(name, out var v) ? v : null);
    }

    [TestCase("CLAUDECODE", "1", true)]
    [TestCase("CODEX_SANDBOX", "seatbelt", true)]
    [TestCase("CODEX_THREAD_ID", "abc", true)]
    [TestCase("AI_AGENT", "anything", true)]
    [TestCase("OR_APP_NAME", "aider", true)]
    [TestCase("OR_APP_NAME", "SomethingElse", false)]
    [TestCase("CLAUDECODE", "0", false)]
    [TestCase("CODEX_CI", "false", false)]
    [TestCase("UNRELATED", "1", false)]
    public void DetectsAgentEnvironment(string variable, string value, bool expected) =>
        Assert.That(DetectsWith(variable, value), Is.EqualTo(expected));

    [Test]
    public void EmptyEnvironmentIsNoAgent() =>
        Assert.That(LlmEnvironmentDetector.IsAgentEnvironment(_ => null), Is.False);
}
