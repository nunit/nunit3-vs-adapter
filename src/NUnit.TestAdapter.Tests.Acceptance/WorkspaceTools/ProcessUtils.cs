using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public static class ProcessUtils
    {
        public static ProcessRunResult Run(string workingDirectory, string fileName, IEnumerable<string> arguments = null)
        {
            if (!Path.IsPathRooted(workingDirectory))
                throw new ArgumentException(nameof(workingDirectory), "Working directory must not be relative.");

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException(nameof(fileName), "File name must be specified.");

            var escapedArguments = arguments is null ? null : EscapeProcessArguments(arguments, alwaysQuote: false);

            using var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    FileName = fileName,
                    Arguments = escapedArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            // This is inherited if the test runner was started by the Visual Studio process.
            // It breaks MSBuild 15’s targets when it tries to build legacy csprojs and vbprojs.
            process.StartInfo.EnvironmentVariables.Remove("VisualStudioVersion");

            var stdout = (StringBuilder)null;
            var stderr = (StringBuilder)null;

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is null) return;

                if (stdout is null)
                    stdout = new StringBuilder();
                else
                    stdout.AppendLine();

                stdout.Append(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is null) return;

                if (stderr is null)
                    stderr = new StringBuilder();
                else
                    stderr.AppendLine();

                stderr.Append(e.Data);
            };

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();

            return new ProcessRunResult(
                fileName,
                escapedArguments,
                process.ExitCode,
                stdout?.ToString(),
                stderr?.ToString());
        }

        private static readonly char[] CharsThatRequireQuoting = { ' ', '"' };
        private static readonly char[] CharsThatRequireEscaping = { '\\', '"' };

        /// <summary>
        /// Escapes arbitrary values so that the process receives the exact string you intend and injection is impossible.
        /// Spec: https://msdn.microsoft.com/en-us/library/bb776391.aspx.
        /// </summary>
        public static string EscapeProcessArguments(IEnumerable<string> literalValues, bool alwaysQuote = false)
        {
            if (literalValues is null) throw new ArgumentNullException(nameof(literalValues));

            using var en = literalValues.GetEnumerator();
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
