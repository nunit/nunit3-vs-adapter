﻿using System;
using System.Collections.Generic;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    partial class IsolatedWorkspace
    {
        private sealed class RunSettings
        {
            private readonly List<string> arguments = new List<string>();

            public string WorkingDirectory { get; }
            public string FileName { get; }

            public RunSettings(string workingDirectory, string fileName)
            {
                WorkingDirectory = workingDirectory;
                FileName = fileName;
            }

            public ProcessRunResult Run(bool throwOnError = true)
            {
                var result = ProcessUtils.Run(WorkingDirectory, FileName, arguments);
                if (throwOnError) result.ThrowIfError();
                return result;
            }

            public RunSettings Add(string argument)
            {
                arguments.Add(argument);
                return this;
            }

            public RunSettings AddRange(IEnumerable<string> arguments)
            {
                if (arguments is null) throw new ArgumentNullException(nameof(arguments));
                this.arguments.AddRange(arguments);
                return this;
            }

            public RunSettings AddIf(bool condition, string argument)
            {
                if (condition) arguments.Add(argument);
                return this;
            }

            public RunSettings AddRangeIf(bool condition, IEnumerable<string> arguments)
            {
                if (arguments is null) throw new ArgumentNullException(nameof(arguments));
                if (condition) this.arguments.AddRange(arguments);
                return this;
            }
        }
    }
}
