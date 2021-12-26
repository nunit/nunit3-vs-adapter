using System;
using System.Collections.Generic;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    partial class IsolatedWorkspace
    {
        private sealed class RunSettings
        {
            private List<string> Arguments { get; } = new();

            public string WorkingDirectory { get; }
            public string FileName { get; }

            public string ArgumentsAsEscapedString => ProcessUtils.EscapeProcessArguments(Arguments);

            public RunSettings(string workingDirectory, string fileName)
            {
                WorkingDirectory = workingDirectory;
                FileName = fileName;
            }

            public ProcessRunResult Run(bool throwOnError = true)
            {
                var result = ProcessUtils.Run(WorkingDirectory, FileName, Arguments);
                if (throwOnError) result.ThrowIfError();
                return result;
            }

            public RunSettings Add(string argument)
            {
                Arguments.Add(argument);
                return this;
            }

            public RunSettings AddRange(IEnumerable<string> arguments)
            {
                if (arguments is null) throw new ArgumentNullException(nameof(arguments));
                Arguments.AddRange(arguments);
                return this;
            }

            public RunSettings AddIf(bool condition, string argument)
            {
                if (condition) Arguments.Add(argument);
                return this;
            }

            public RunSettings AddRangeIf(bool condition, IEnumerable<string> arguments)
            {
                if (arguments is null) throw new ArgumentNullException(nameof(arguments));
                if (condition) Arguments.AddRange(arguments);
                return this;
            }
        }
    }
}
