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

            if (result.StdErr != null || result.StdOut != null)
            {
                builder.AppendLine().Append(result.StdErr != null ? "Stderr:" : "Stdout:");
                builder.AppendLine().Append(result.StdErr ?? result.StdOut);
            }

            return builder.ToString();
        }
    }
}
