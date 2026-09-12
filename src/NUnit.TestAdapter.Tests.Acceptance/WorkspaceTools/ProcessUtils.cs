using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

public static class ProcessUtils
{
    public static ProcessRunResult Run(string workingDirectory, string fileName, IEnumerable<string> arguments = null)
    {
        if (!Path.IsPathRooted(workingDirectory))
            throw new ArgumentException(nameof(workingDirectory), "Working directory must not be relative.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException(nameof(fileName), "File name must be specified.");

        using var process = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                WorkingDirectory = workingDirectory,
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
        if (arguments is not null)
            foreach (var argument in arguments)
                process.StartInfo.ArgumentList.Add(argument);

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
            arguments is null ? null : string.Join(' ', arguments),
            process.ExitCode,
            stdout?.ToString(),
            stderr?.ToString());
    }
}