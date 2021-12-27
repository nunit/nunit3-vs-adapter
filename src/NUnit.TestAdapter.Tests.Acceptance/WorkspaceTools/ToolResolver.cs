using System;
using System.IO;
using System.Linq;
using System.Net;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public sealed class ToolResolver
    {
        private readonly string downloadCacheDirectory;

        private readonly Lazy<string> nuGet;
        public string NuGet => nuGet.Value;

        private readonly Lazy<string> msBuild;
        public string MSBuild => msBuild.Value;

        private readonly Lazy<string> vsTest;
        public string VSTest => vsTest.Value;

        private readonly Lazy<string> vsWhere;
        public string VSWhere => vsWhere.Value;

        public ToolResolver(string downloadCacheDirectory)
        {
            if (!Path.IsPathRooted(downloadCacheDirectory))
                throw new ArgumentException(nameof(downloadCacheDirectory), "Download cache directory path must be rooted.");

            this.downloadCacheDirectory = downloadCacheDirectory;

            nuGet = new Lazy<string>(() => FindDownloadedTool("NuGet", "nuget.exe", "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"));

            msBuild = new Lazy<string>(() =>
            {
                var vsInstallation =
                    FindVisualStudio(requiredComponent: "Microsoft.Component.MSBuild")
                    ?? throw new InvalidOperationException("MSBuild is not installed with Visual Studio on this machine.");

                var path = Path.Combine(vsInstallation, @"MSBuild\Current\Bin\MSBuild.exe");
                if (File.Exists(path)) return path;

                var oldPath = Path.Combine(vsInstallation, @"MSBuild\15.0\Bin\MSBuild.exe");
                if (File.Exists(oldPath)) return oldPath;

                throw new FileNotFoundException("Cannot locate MSBuild.exe.");
            });

            vsTest = new Lazy<string>(() =>
            {
                var vsInstallation =
                    FindVisualStudio(requiredComponent: "Microsoft.VisualStudio.TestTools.TestPlatform.V1.CLI") // https://github.com/Microsoft/vswhere/issues/126#issuecomment-360542783
                    ?? throw new InvalidOperationException("VSTest is not installed with Visual Studio on this machine.");

                return Path.Combine(vsInstallation, @"Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe");
            });

            vsWhere = new Lazy<string>(() => Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"Microsoft Visual Studio\Installer\vswhere.exe"));
        }

        private string FindDownloadedTool(string id, string fileName, string downloadUrl)
        {
            var directory = Path.Combine(downloadCacheDirectory, Utils.GetSafeFilename(id));
            var toolPath = Path.Combine(directory, fileName);

            if (!File.Exists(toolPath))
            {
                Directory.CreateDirectory(directory);

                using var client = new WebClient();
                client.DownloadFile(downloadUrl, toolPath);
            }

            return toolPath;
        }

        private string FindVisualStudio(string requiredComponent)
        {
            var arguments = new[] { "-latest", "-products", "*", "-requires", requiredComponent, "-property", "installationPath" };

            var releaseInstallationPath = ProcessUtils.Run(Environment.CurrentDirectory, VSWhere, arguments)
                .ThrowIfError()
                .StdOut;

            if (!string.IsNullOrEmpty(releaseInstallationPath))
                return releaseInstallationPath;

            var prereleaseInstallationPath =
                ProcessUtils.Run(
                    Environment.CurrentDirectory,
                    VSWhere,
                    arguments.Concat(new[] { "-prerelease" }))
                .ThrowIfError()
                .StdOut;

            if (!string.IsNullOrEmpty(prereleaseInstallationPath))
                return prereleaseInstallationPath;

            return null;
        }
    }
}
