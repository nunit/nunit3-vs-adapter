using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    [DebuggerDisplay("{Directory,nq}")]
    public sealed partial class IsolatedWorkspace : IDisposable
    {
        private readonly List<string> projectPaths = new();
        private readonly ToolResolver toolResolver;
        private readonly DirectoryMutex directoryMutex;

        public bool DumpTestExecution { get; set; } = false;

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

        /// <summary>
        /// Runs dotnet test.
        /// </summary>
        /// <param name="filterArgument">Possible filter statement.</param>
        /// <param name="noBuild">if you run MSBuild or dotnet build first, set to false.</param>
        /// <param name="verbose">Set NUnit verbosity to 5, enables seing more info from the run in StdOut.</param>
        /// <returns>VSTestResults.</returns>
        public VSTestResult DotNetTest(string filterArgument = "", bool noBuild = false, bool verbose = false, Action<string> log = null)
        {
            using var tempTrxFile = new TempFile();

            var dotnettest = ConfigureRun("dotnet")
                .Add("test")
                .AddIf(noBuild, "--no-build")
                .Add("-v:n")
                .Add("--logger").Add("trx;LogFileName=" + tempTrxFile);

            bool hasNUnitWhere = filterArgument.StartsWith("NUnit.Where");

            if (filterArgument.Length > 0 && !hasNUnitWhere)
            {
                dotnettest.Add("--filter").Add($"{filterArgument}");
            }
            else if (hasNUnitWhere)
            {
                dotnettest.Add("--").Add(filterArgument);
            }
            if (verbose)
            {
                if (!hasNUnitWhere)
                    dotnettest.Add("--");
                dotnettest.Add("NUnit.Verbosity=5");
            }
            log?.Invoke($"\n{dotnettest.ArgumentsAsEscapedString}");
            var result = dotnettest.Run(throwOnError: false);

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

        public void MsBuild(string target = null, bool restore = false)
        {
            ConfigureRun(toolResolver.MSBuild)
                .AddIf(target != null, "/t:" + target)
                .AddIf(restore, "/restore")
                .Run();
        }

        public VSTestResult VSTest(string testAssemblyPath, IFilterArgument filter)
        {
            using var tempTrxFile = new TempFile();

            var vstest = ConfigureRun(toolResolver.VSTest)
                .Add(testAssemblyPath)
                .Add("/logger:trx;LogFileName=" + tempTrxFile);

            if (filter.HasArguments)
            {
                vstest.Add(filter.CompletedArgument());
            }

            if (DumpTestExecution)
                vstest.Add("--").Add("NUnit.DumpXmlTestResults=true");

            var result = vstest.Run(throwOnError: false);

            if (new FileInfo(tempTrxFile).Length == 0)
            {
                result.ThrowIfError();
                return new VSTestResult(result);
            }

            return VSTestResult.Load(result, tempTrxFile);
        }

        private RunSettings ConfigureRun(string filename) => new(Directory, filename);
    }
}
