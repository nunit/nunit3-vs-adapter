// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, 2026 Terje Sandstrom
// ***********************************************************************

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter;

/// <summary>
/// Decides which messages the adapter sends to the console, from the resolved settings.
/// AgentMode only sets defaults in AdapterSettings; explicit settings arrive here already applied.
/// The VSTest console logger prints each failed test with its message, stack trace and output
/// from the test result, so ShowStdout/ShowStderr=Failed means: only in the test result.
/// </summary>
public sealed class TestOutputPolicy(IAdapterSettings settings)
{
    public bool ShowsConsoleOutput => settings.ShowStdout == CapturedOutputMode.All;

    /// <summary>
    /// With ShowStdout=All every reason is sent (the classic behavior).
    /// With Failed only reasons of failed tests, for example a warning mapped to Failed,
    /// because the warning text is not in the failure message the logger prints.
    /// </summary>
    public bool ShowsReason(TestOutcome? outcome) => settings.ShowStdout switch
    {
        CapturedOutputMode.All => true,
        CapturedOutputMode.Failed => outcome == TestOutcome.Failed,
        _ => false
    };

    public bool ShowsLiveOutput(bool isErrorStream) =>
        ModeFor(isErrorStream) == CapturedOutputMode.All;

    /// <summary>
    /// Output is buffered per test, and attached to its result, unless it is turned off.
    /// </summary>
    public bool CollectsOutput(bool isErrorStream) =>
        ModeFor(isErrorStream) != CapturedOutputMode.None;

    public TestMessageLevel ConsoleOutputLevel =>
        settings.ConsoleOut >= 2 ? TestMessageLevel.Warning : TestMessageLevel.Informational;

    public string StackTraceFor(string stackTrace, bool isParentFailure)
    {
        bool include = isParentFailure ? settings.IncludeStackTraceForSuites : settings.IncludeStackTrace;
        if (!include)
            return null;
        return settings.StackTrace switch
        {
            StackTraceMode.None => null,
            StackTraceMode.Filtered => StackTraceFilter.Apply(stackTrace),
            _ => stackTrace
        };
    }

    private CapturedOutputMode ModeFor(bool isErrorStream) =>
        isErrorStream ? settings.ShowStderr : settings.ShowStdout;
}
