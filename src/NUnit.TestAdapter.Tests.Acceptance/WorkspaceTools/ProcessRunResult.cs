using System;
using System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public readonly struct ProcessRunResult
    {
        public ProcessRunResult(string fileName, string arguments, int exitCode, string stdOut, string stdErr)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Arguments = arguments;
            ExitCode = exitCode;
            StdOut = stdOut ?? string.Empty;
            StdErr = stdErr ?? string.Empty;
        }

        public string FileName { get; }
        public string Arguments { get; }

        public string ProcessName => Path.GetFileName(FileName);
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdErr { get; }

        public ProcessRunResult ThrowIfError()
        {
            if (ExitCode == 0 && string.IsNullOrEmpty(StdErr)) return this;

            throw new ProcessErrorException(this);
        }
    }
}
