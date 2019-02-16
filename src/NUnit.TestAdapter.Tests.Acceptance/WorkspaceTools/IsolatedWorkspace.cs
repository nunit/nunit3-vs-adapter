using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    [DebuggerDisplay("{Directory,nq}")]
    public sealed class IsolatedWorkspace
    {
        private readonly List<string> projectPaths = new List<string>();

        public string Directory { get; }

        public IsolatedWorkspace(string directory)
        {
            Directory = directory;
        }

        public IsolatedWorkspace AddProject(string path, string contents)
        {
            AddFile(path, contents);
            projectPaths.Add(path);
            return this;
        }

        public IsolatedWorkspace AddFile(string path, string contents)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path must be specified.", nameof(path));

            if (Path.IsPathRooted(path))
                throw new ArgumentException("File path must not be rooted.", nameof(path));

            File.WriteAllText(Path.Combine(Directory, path), Utils.RemoveIndent(contents));
            return this;
        }
    }
}
