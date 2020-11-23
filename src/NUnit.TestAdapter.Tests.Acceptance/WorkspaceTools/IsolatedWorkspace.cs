using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    [DebuggerDisplay("{Directory,nq}")]
    public sealed partial class IsolatedWorkspace : IDisposable
    {
        private readonly List<string> projectPaths = new List<string>();
        private readonly ToolResolver toolResolver;
        private readonly DirectoryMutex directoryMutex;

        public string Directory => directoryMutex.DirectoryPath;

        public IsolatedWorkspace(DirectoryMutex directoryMutex, ToolResolver toolResolver)
        {
            this.directoryMutex = directoryMutex ?? throw new ArgumentNullException(nameof(toolResolver));
            this.toolResolver = toolResolver ?? throw new ArgumentNullException(nameof(toolResolver));
        }

        public void Dispose() => directoryMutex.Dispose();

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

        public void DotNetRestore()
        {
            ConfigureRun("dotnet")
                .Add("restore")
                .Run();
        }

        public void DotNetBuild(bool noRestore = false)
        {
            ConfigureRun("dotnet")
                .Add("build")
                .AddIf(noRestore, "--no-restore")
                .Run();
        }

        public VSTestResult DotNetTest(bool noBuild = false)
        {
            using var tempTrxFile = new TempFile();

            var result = ConfigureRun("dotnet")
                .Add("test")
                .AddIf(noBuild, "--no-build")
                .Add("--logger").Add("trx;LogFileName=" + tempTrxFile)
                .Run(throwOnError: false);

            if (new FileInfo(tempTrxFile).Length == 0)
                result.ThrowIfError();

            return VSTestResult.Load(result, tempTrxFile);
        }

        public void DotNetVSTest(IEnumerable<string> testAssemblyPaths)
        {
            ConfigureRun("dotnet")
                .Add("vstest")
                .AddRange(testAssemblyPaths)
                .Run();
        }

        public void NuGetRestore(string packagesDirectory = null)
        {
            ConfigureRun(toolResolver.NuGet)
                .Add("restore")
                .AddRangeIf(packagesDirectory != null, new[] { "-PackagesDirectory", packagesDirectory })
                .Run();
        }

        public void MSBuild(string target = null, bool restore = false)
        {
            ConfigureRun(toolResolver.MSBuild)
                .AddIf(target != null, "/t:" + target)
                .AddIf(restore, "/restore")
                .Run();
        }

        public VSTestResult VSTest(string testAssemblyPath)
        {
            using var tempTrxFile = new TempFile();

            var result = ConfigureRun(toolResolver.VSTest)
                .Add(testAssemblyPath)
                .Add("/logger:trx;LogFileName=" + tempTrxFile)
                .Run(throwOnError: false);

            if (new FileInfo(tempTrxFile).Length == 0)
                result.ThrowIfError();

            return VSTestResult.Load(result, tempTrxFile);
        }

        private RunSettings ConfigureRun(string filename) => new RunSettings(Directory, filename);
    }
}
