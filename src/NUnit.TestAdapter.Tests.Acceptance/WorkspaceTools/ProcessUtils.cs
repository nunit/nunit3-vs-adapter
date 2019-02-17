using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public static class ProcessUtils
    {
        public static void Run(string workingDirectory, string fileName, IEnumerable<string> arguments = null)
        {
            if (!Path.IsPathRooted(workingDirectory))
                throw new ArgumentException(nameof(workingDirectory), "Working directory must not be relative.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(nameof(fileName), "File name must be specified.");

            using (var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    FileName = fileName,
                    Arguments = arguments is null ? null : EscapeProcessArguments(arguments, alwaysQuote: false),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            })
            {
                var output = (StringBuilder)null;
                var error = (StringBuilder)null;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data is null) return;

                    if (output is null)
                        output = new StringBuilder();
                    else
                        output.AppendLine();

                    output.Append(e.Data);
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data is null) return;

                    if (error is null)
                        error = new StringBuilder();
                    else
                        error.AppendLine();

                    error.Append(e.Data);
                };

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                if (process.ExitCode != 0 || error != null)
                {
                    throw new ProcessErrorException(
                        Path.GetFileName(fileName),
                        process.ExitCode,
                        output?.ToString(),
                        error?.ToString());
                }
            }
        }

        private static readonly char[] CharsThatRequireQuoting = { ' ', '"' };
        private static readonly char[] CharsThatRequireEscaping = { '\\', '"' };

        /// <summary>
        /// Escapes arbitrary values so that the process receives the exact string you intend and injection is impossible.
        /// Spec: https://msdn.microsoft.com/en-us/library/bb776391.aspx
        /// </summary>
        public static string EscapeProcessArguments(IEnumerable<string> literalValues, bool alwaysQuote = false)
        {
            if (literalValues is null) throw new ArgumentNullException(nameof(literalValues));

            using (var en = literalValues.GetEnumerator())
            {
                if (!en.MoveNext()) return string.Empty;

                var builder = new StringBuilder();

                while (true)
                {
                    EscapeProcessArgument(builder, en.Current, alwaysQuote);
                    if (!en.MoveNext()) break;
                    builder.Append(' ');
                }

                return builder.ToString();
            }
        }

        private static void EscapeProcessArgument(StringBuilder builder, string literalValue, bool alwaysQuote)
        {
            if (string.IsNullOrEmpty(literalValue))
            {
                builder.Append("\"\"");
                return;
            }

            if (literalValue.IndexOfAny(CharsThatRequireQuoting) == -1) // Happy path
            {
                if (!alwaysQuote)
                {
                    builder.Append(literalValue);
                    return;
                }
                if (literalValue[literalValue.Length - 1] != '\\')
                {
                    builder.Append('"').Append(literalValue).Append('"');
                    return;
                }
            }

            builder.Append('"');

            var nextPosition = 0;
            while (true)
            {
                var nextEscapeChar = literalValue.IndexOfAny(CharsThatRequireEscaping, nextPosition);
                if (nextEscapeChar == -1) break;

                builder.Append(literalValue, nextPosition, nextEscapeChar - nextPosition);
                nextPosition = nextEscapeChar + 1;

                switch (literalValue[nextEscapeChar])
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        var numBackslashes = 1;
                        while (nextPosition < literalValue.Length && literalValue[nextPosition] == '\\')
                        {
                            numBackslashes++;
                            nextPosition++;
                        }
                        if (nextPosition == literalValue.Length || literalValue[nextPosition] == '"')
                            numBackslashes <<= 1;

                        for (; numBackslashes != 0; numBackslashes--)
                            builder.Append('\\');
                        break;
                }
            }

            builder.Append(literalValue, nextPosition, literalValue.Length - nextPosition);
            builder.Append('"');
        }
    }
}
