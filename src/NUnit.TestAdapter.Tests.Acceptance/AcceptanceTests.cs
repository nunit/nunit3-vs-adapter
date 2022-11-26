using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public class Frameworks
    {
        public const string NetCoreApp31 = "netcoreapp3.1";
        public const string Net50 = "net5.0";
        public const string Net60 = "net6.0";
        public const string Net70 = "net7.0";
    }

    [Category("Acceptance")]
    public abstract class AcceptanceTests
    {
        public static string NuGetPackageId => "NUnit3TestAdapter";

        public static string NuGetPackageVersion => Initialization.Value.NupkgVersion;

        public const string LowestNetfxTarget = "net462";
        public const string LegacyProjectTargetFrameworkVersion = "v4.6.2";

        public static IEnumerable<string> TargetFrameworks => new[]
        {
            LowestNetfxTarget,
            Frameworks.NetCoreApp31
        };

        public static IEnumerable<string> DotNetCliTargetFrameworks => new[]
        {
            Frameworks.NetCoreApp31,
            Frameworks.Net50,
            Frameworks.Net60,
            Frameworks.Net70
        };

        private static readonly Lazy<(IsolatedWorkspaceManager Manager, string NupkgVersion, bool KeepWorkspaces)> Initialization = new(() =>
       {
           var directory = TestContext.Parameters["ProjectWorkspaceDirectory"]
               ?? TryAutoDetectProjectWorkspaceDirectory()
               ?? throw new InvalidOperationException("The test parameter ProjectWorkspaceDirectory must be set in order to run this test.");

           var nupkgDirectory = TestContext.Parameters["TestNupkgDirectory"]
               ?? TryAutoDetectTestNupkgDirectory(NuGetPackageId)
               ?? throw new InvalidOperationException("The test parameter TestNupkgDirectory must be set in order to run this test.");

           var nupkgVersion = TryGetTestNupkgVersion(nupkgDirectory, packageId: NuGetPackageId)
               ?? throw new InvalidOperationException($"No NuGet package with the ID {NuGetPackageId} was found in {nupkgDirectory}.");

           var keepWorkspaces = TestContext.Parameters.Get("KeepWorkspaces", defaultValue: false);

           var packageCachePath = Path.Combine(directory, ".isolatednugetcache");
           ClearCachedTestNupkgs(packageCachePath);

           var manager = new IsolatedWorkspaceManager(
               reason: string.Join(
                   Environment.NewLine,
                   "Test assembly: " + typeof(AcceptanceTests).Assembly.Location,
                   "Runner process: " + Process.GetCurrentProcess().MainModule.FileName),
               directory,
               nupkgDirectory,
               packageCachePath,
               downloadCachePath: Path.Combine(directory, ".toolcache"));

           if (keepWorkspaces) manager.PreserveDirectory("The KeepWorkspaces test parameter was set to true.");
           TestContext.WriteLine($"Directory: {directory}, NugetPackageDirectory {nupkgDirectory},NugetPackageVersion: {nupkgVersion}");
           return (manager, nupkgVersion, keepWorkspaces);
       });

        private static void ClearCachedTestNupkgs(string packageCachePath)
        {
            Utils.DeleteDirectoryRobust(Path.Combine(packageCachePath, NuGetPackageId));
        }

        private static readonly Dictionary<string, List<IsolatedWorkspace>> WorkspacesByTestId = new();

        protected static IsolatedWorkspace CreateWorkspace()
        {
            var test = TestContext.CurrentContext?.Test ?? throw new InvalidOperationException("There is no current test.");
            const string chars = "=()!,~-";
            string name = chars.Aggregate(test.Name, (current, ch) => current.Replace(ch, '_'));
            var workspace = Initialization.Value.Manager.CreateWorkspace(name);

            lock (WorkspacesByTestId)
            {
                if (!WorkspacesByTestId.TryGetValue(test.ID, out var workspaces))
                    WorkspacesByTestId.Add(test.ID, workspaces = new List<IsolatedWorkspace>());
                workspaces.Add(workspace);
            }
            return workspace;
        }

        protected static void InconclusiveOnException(Action action)
        {
            Assume.That(action.Invoke, Throws.Nothing);
        }

        [TearDown]
        public static void TearDown()
        {
            var test = TestContext.CurrentContext?.Test ?? throw new InvalidOperationException("There is no current test.");

            List<IsolatedWorkspace> workspaces;
            lock (WorkspacesByTestId)
            {
                if (!WorkspacesByTestId.TryGetValue(test.ID, out workspaces))
                    return;

                WorkspacesByTestId.Remove(test.ID);
            }

            foreach (var workspace in workspaces)
                workspace.Dispose();

            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                Initialization.Value.Manager.PreserveDirectory(
                    test.FullName + " failed:" + Environment.NewLine
                    + TestContext.CurrentContext.Result.Message.TrimEnd() + Environment.NewLine);
            }
            else if (!Initialization.Value.KeepWorkspaces)
            {
                foreach (var workspace in workspaces)
                    Utils.DeleteDirectoryRobust(workspace.Directory);
            }
        }

        internal static void OnGlobalTeardown()
        {
            if (!Initialization.IsValueCreated) return;

            Initialization.Value.Manager.Dispose();
        }

        private static string TryAutoDetectProjectWorkspaceDirectory()
        {
            for (var directory = TestContext.CurrentContext.TestDirectory; directory != null; directory = Path.GetDirectoryName(directory))
            {
                if (File.Exists(Path.Combine(directory, "build.cake")))
                {
                    return Path.Combine(directory, ".acceptance");
                }
            }

            return null;
        }

        private static string TryAutoDetectTestNupkgDirectory(string packageId)
        {
            // Keep in sync with build.cake.

            // Search for it
            for (var directory = TestContext.CurrentContext.TestDirectory; directory != null; directory = Path.GetDirectoryName(directory))
            {
                var packagePath = Path.Combine(directory, "package");

                try
                {
                    if (Directory.EnumerateFiles(Path.Combine(directory, "package"), packageId + ".*.nupkg").Any())
                    {
                        return packagePath;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                }
            }

            return null;
        }

        private static string TryGetTestNupkgVersion(string directory, string packageId)
        {
            var dir = new DirectoryInfo(directory);
            var packages = dir.EnumerateFiles(packageId + ".*.nupkg").ToList();
            var selected = packages.Count > 1 ? packages.OrderByDescending(f => f.LastWriteTime).First() : packages.SingleOrDefault();

            var path = selected?.FullName;

            return path is null ? null :
                Path.GetFileNameWithoutExtension(path).Substring(packageId.Length + 1);
        }
    }
}
