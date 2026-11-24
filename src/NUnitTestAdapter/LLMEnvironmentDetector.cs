// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, 2014-2026 Terje Sandstrom
// ***********************************************************************

using System;

namespace NUnit.VisualStudio.TestAdapter;

internal static class LlmEnvironmentDetector
{
    private static readonly string[] AgentVariables =
    [
        "CLAUDECODE",
        "CURSOR_AGENT",
        "GEMINI_CLI",
        "COPILOT_CLI",
        "AI_AGENT",
        "CODEX_CLI",
        "AGENT_CLI"
    ];

    public static bool IsAgentEnvironment()
    {
        foreach (var variable in AgentVariables)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value) &&
                !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
