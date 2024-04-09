using System;
using System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public readonly struct ProcessRunResult(
        string fileName,
        string arguments,
        int exitCode,
        string stdOut,
        string stdErr)
    {
        public string FileName { get; } = fileName ?? throw new ArgumentNullException(nameof(fileName));
        public string Arguments { get; } = arguments;

        public string ProcessName => Path.GetFileName(FileName);
        public int ExitCode { get; } = exitCode;
        public string StdOut { get; } = stdOut ?? string.Empty;
        public string StdErr { get; } = stdErr ?? string.Empty;

        public ProcessRunResult ThrowIfError() =>
            ExitCode == 0 && string.IsNullOrEmpty(StdErr)
                ? this 
                : throw new ProcessErrorException(this);
    }
}
