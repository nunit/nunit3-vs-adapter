using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

/// <summary>
/// Applies <see cref="FixtureDisplayName"/> to every test case and result before the bridge sees it.
/// </summary>
internal sealed class FixtureDisplayNameFrameworkHandle(IFrameworkHandle inner, FixtureDisplayName displayName) : IFrameworkHandle
{
#pragma warning disable CS0618 // Obsolete, but still part of the interface.
    public bool EnableShutdownAfterTestRun
    {
        get => inner.EnableShutdownAfterTestRun;
        set => inner.EnableShutdownAfterTestRun = value;
    }
#pragma warning restore CS0618

    public void RecordStart(TestCase testCase)
    {
        displayName.Apply(testCase);
        inner.RecordStart(testCase);
    }

    public void RecordResult(TestResult testResult)
    {
        displayName.Apply(testResult);
        inner.RecordResult(testResult);
    }

    public void RecordEnd(TestCase testCase, TestOutcome outcome)
    {
        displayName.Apply(testCase);
        inner.RecordEnd(testCase, outcome);
    }

    public void RecordAttachments(IList<AttachmentSet> attachmentSets) => inner.RecordAttachments(attachmentSets);

    public void SendMessage(TestMessageLevel testMessageLevel, string message) => inner.SendMessage(testMessageLevel, message);

    public int LaunchProcessWithDebuggerAttached(string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables)
        => inner.LaunchProcessWithDebuggerAttached(filePath, workingDirectory, arguments, environmentVariables);
}
