// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, 2026 Terje Sandstrom
// ***********************************************************************

using System;
using System.IO;

namespace NUnit.VisualStudio.TestAdapter;

internal static class StackTraceFilter
{
    public static string Apply(string stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
            return stackTrace;

        var result = new System.Text.StringBuilder();
        using var reader = new StringReader(stackTrace);
        while (reader.ReadLine() is { } line)
        {
            if (line.Contains("System.Runtime.", StringComparison.Ordinal) ||
                line.Contains("NUnit.Framework.Internal.", StringComparison.Ordinal) ||
                line.Contains(".MoveNext()", StringComparison.Ordinal) ||
                line.Contains("<>d__", StringComparison.Ordinal))
                continue;
            result.AppendLine(line);
        }

        return result.ToString().TrimEnd();
    }
}
