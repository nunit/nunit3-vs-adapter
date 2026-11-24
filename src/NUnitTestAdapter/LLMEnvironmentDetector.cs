// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, 2014-2026 Terje Sandstrom
// ***********************************************************************
// The environment variable list is taken from LLMEnvironmentDetector in Microsoft.Testing.Platform,
// Copyright (c) Microsoft Corporation, MIT license, see THIRD_PARTY_NOTICES.md:
// https://github.com/microsoft/testfx/blob/a684c0aaf1d70f1598ce145edc3a35cd3a49ca8f/src/MTP/Microsoft.Testing.Platform/Helpers/LLMEnvironmentDetector.cs
// Keep the lists in sync with that file.

using System;
using System.Linq;

namespace NUnit.VisualStudio.TestAdapter;

internal static class LlmEnvironmentDetector
{
    /// <summary>
    /// Any of these set (and not "0" or "false") means an agent runs the tests.
    /// </summary>
    private static readonly string[] AgentVariables =
    [
        "CLAUDE_CODE_IS_COWORK",
        "CLAUDECODE", "CLAUDE_CODE", "CLAUDE_CODE_ENTRYPOINT",
        "CURSOR_EDITOR", "CURSOR_AI", "CURSOR_TRACE_ID", "CURSOR_AGENT",
        "GEMINI_CLI",
        "GITHUB_COPILOT_CLI_MODE", "GH_COPILOT_WORKING_DIRECTORY", "COPILOT_CLI", "COPILOT_MODEL", "COPILOT_ALLOW_ALL", "COPILOT_GITHUB_TOKEN",
        "COPILOT_AGENT",
        "CODEX_CLI", "CODEX_SANDBOX", "CODEX_CI", "CODEX_THREAD_ID",
        "AMP_HOME",
        "QWEN_CODE",
        "DROID_CLI",
        "OPENCODE",
        "ZED_ENVIRONMENT", "ZED_TERM",
        "KIMI_CLI",
        "GOOSE_TERMINAL", "GOOSE_PROVIDER",
        "CLINE_TASK_ID",
        "ROO_CODE_TASK_ID",
        "WINDSURF_SESSION",
        "REPL_ID",
        "AUGMENT_AGENT",
        "ANTIGRAVITY_AGENT",
        "AGENT_CLI",
        "AI_AGENT" // Not in MTP as a plain variable: any agent can set it to signal agent mode
    ];

    /// <summary>
    /// These variables mean an agent only with one of the given values (case-insensitive).
    /// </summary>
    private static readonly (string Variable, string Value)[] AgentValues =
    [
        ("OR_APP_NAME", "Aider"),
        ("OR_APP_NAME", "plandex"),
        ("OR_APP_NAME", "OpenHands")
    ];

    public static bool IsAgentEnvironment() => IsAgentEnvironment(Environment.GetEnvironmentVariable);

    internal static bool IsAgentEnvironment(Func<string, string> getVariable) =>
        AgentVariables.Any(variable => IsSet(getVariable(variable))) ||
        AgentValues.Any(rule => string.Equals(getVariable(rule.Variable), rule.Value, StringComparison.OrdinalIgnoreCase));

    private static bool IsSet(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
}
