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
    [Category("Acceptance")]
    public abstract class AcceptanceTests
    {
        public static string NuGetPackageId { get; } = "NUnit3TestAdapter";

        public static string NuGetPackageVersion => Initialization.Value.nupkgVersion;

        public static string LowestNetfxTarget = "net35";
        public static string LegacyProjectTargetFrameworkVersion = "v3.5";

        public static IEnumerable<string> TargetFrameworks => new[]
        {
            LowestNetfxTarget,
            "netcoreapp1.0"
        };

        private readonly static Lazy<(IsolatedWorkspaceManager manager, string nupkgVersion)> Initialization = new Lazy<(IsolatedWorkspaceManager, string)>(() =>
        {
            var directory = TestContext.Parameters["ProjectWorkspaceDirectory"]
                ?? TryAutoDetectProjectWorkspaceDirectory()
                ?? throw new InvalidOperationException("The test parameter ProjectWorkspaceDirectory must be set in order to run this test.");

            var nupkgDirectory = TestContext.Parameters["TestNupkgDirectory"]
                ?? TryAutoDetectTestNupkgDirectory(NuGetPackageId)
                ?? throw new InvalidOperationException("The test parameter TestNupkgDirectory must be set in order to run this test.");

            var nupkgVersion = TryGetTestNupkgVersion(nupkgDirectory, packageId: NuGetPackageId)
                ?? throw new InvalidOperationException($"No NuGet package with the ID {NuGetPackageId} was found in {nupkgDirectory}.");

            return (
                new IsolatedWorkspaceManager(
                    reason: string.Join(Environment.NewLine,
                        "Test assembly: " + typeof(AcceptanceTests).Assembly.Location,
                        "Runner process: " + Process.GetCurrentProcess().MainModule.FileName),
                    directory,
                    nupkgDirectory,
                    Path.Combine(directory, ".isolatednugetcache")),
                nupkgVersion);
        });

        private readonly static Dictionary<string, List<IsolatedWorkspace>> WorkspacesByTestId = new Dictionary<string, List<IsolatedWorkspace>>();

        protected static IsolatedWorkspace CreateWorkspace()
        {
            var test = TestContext.CurrentContext?.Test ?? throw new InvalidOperationException("There is no current test.");

            var workspace = Initialization.Value.manager.CreateWorkspace(test.Name);

            if (!WorkspacesByTestId.TryGetValue(test.ID, out var workspaces))
                WorkspacesByTestId.Add(test.ID, workspaces = new List<IsolatedWorkspace>());
            workspaces.Add(workspace);

            return workspace;
        }

        [TearDown]
        public static void TearDown()
        {
            var test = TestContext.CurrentContext?.Test ?? throw new InvalidOperationException("There is no current test.");

            if (WorkspacesByTestId.TryGetValue(test.ID, out var workspaces))
            {
                WorkspacesByTestId.Remove(test.ID);

                if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
                {
                    Initialization.Value.manager.PreserveDirectory(
                        test.FullName + " failed:" + Environment.NewLine
                        + TestContext.CurrentContext.Result.Message.TrimEnd() + Environment.NewLine);
                }
                else
                {
                    foreach (var workspace in workspaces)
                        Directory.Delete(workspace.Directory, recursive: true);
                }
            }
        }

        internal static void OnGlobalTeardown()
        {
            if (!Initialization.IsValueCreated) return;

            Initialization.Value.manager.Dispose();
        }

        private static string TryAutoDetectProjectWorkspaceDirectory()
        {
            for (var directory = TestContext.CurrentContext.TestDirectory; directory != null; directory = Path.GetDirectoryName(directory))
            {
                if (File.Exists(Path.Combine(directory, "build.cake")))
                {
                    return Path.Combine(directory, ".acceptanceworkspace");
                }
            }

            return null;
        }

        private static string TryAutoDetectTestNupkgDirectory(string packageId)
        {
            // Keep in sync with build.cake.

            for (var directory = TestContext.CurrentContext.TestDirectory; directory != null; directory = Path.GetDirectoryName(directory))
            {
                var packagePath = Path.Combine(directory, "package");

                try
                {
                    if (Directory.EnumerateFiles(Path.Combine(directory, "package"), packageId + ".*.nupkg").Any())
                        return packagePath;
                }
                catch (DirectoryNotFoundException)
                {
                }
            }

            return null;
        }

        private static string TryGetTestNupkgVersion(string directory, string packageId)
        {
            var path = Directory.GetFileSystemEntries(directory, packageId + ".*.nupkg").SingleOrDefault();

            return path is null ? null :
                Path.GetFileNameWithoutExtension(path).Substring(packageId.Length + 1);
        }
    }
}
