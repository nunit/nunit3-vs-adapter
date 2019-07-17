using System;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public sealed class ProcessErrorException : Exception
    {
        public ProcessErrorException(string processName, int exitCode, string stdOut, string stdErr)
            : base(BuildMessage(processName, exitCode, stdOut, stdErr))
        {
            ProcessName = processName;
            ExitCode = exitCode;
            StdOut = stdOut;
            StdErr = stdErr;
        }

        public string ProcessName { get; }
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdErr { get; }

        private static string BuildMessage(string processName, int exitCode, string stdOut, string stdErr)
        {
            var builder = new StringBuilder();
            builder.Append("Process ‘").Append(processName);
            builder.Append("’ exited with code ").Append(exitCode).Append('.');

            if (stdErr != null || stdOut != null)
            {
                builder.AppendLine(stdErr != null ? " Stderr:" : " Stdout:");
                builder.Append(stdErr ?? stdOut);
            }

            return builder.ToString();
        }
    }
}
