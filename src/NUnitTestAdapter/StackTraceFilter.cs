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
            // MoveNext frames stay: on .NET Framework the throw location in an async method
            // is shown as <Method>d__0.MoveNext(), and handwritten enumerators have them too.
            if (line.Contains("System.Runtime.", StringComparison.Ordinal) ||
                line.Contains("NUnit.Framework.Internal.", StringComparison.Ordinal))
                continue;
            result.AppendLine(line);
        }

        return result.ToString().TrimEnd();
    }
}
