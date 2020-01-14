using System;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public sealed class ProcessErrorException : Exception
    {
        public ProcessErrorException(ProcessRunResult result)
            : base(BuildMessage(result))
        {
            Result = result;
        }

        public ProcessRunResult Result { get; }

        private static string BuildMessage(ProcessRunResult result)
        {
            var builder = new StringBuilder();
            builder.Append("Process ‘").Append(result.ProcessName);
            builder.Append("’ exited with code ").Append(result.ExitCode).Append('.');
            builder.AppendLine().Append("Executable: ").Append(result.FileName);

            if (!string.IsNullOrWhiteSpace(result.Arguments))
            {
                builder.AppendLine().Append("Arguments: ").Append(result.Arguments);
            }

            var hasStdErr = !string.IsNullOrWhiteSpace(result.StdErr);

            if (hasStdErr || !string.IsNullOrWhiteSpace(result.StdOut))
            {
                builder.AppendLine().Append(hasStdErr ? "Stderr:" : "Stdout:");
                builder.AppendLine().Append(hasStdErr ? result.StdErr : result.StdOut);
            }

            return builder.ToString();
        }
    }
}
