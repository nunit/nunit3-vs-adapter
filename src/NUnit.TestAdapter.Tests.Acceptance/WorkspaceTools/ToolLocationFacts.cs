using System;
using System.IO;
using System.Linq;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public static class ToolLocationFacts
    {
        private static readonly Lazy<string> vsWhere = new Lazy<string>(() =>
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                @"Microsoft Visual Studio\Installer\vswhere.exe");
        });

        public static string VSWhere => vsWhere.Value;

        private static readonly Lazy<string> msBuild = new Lazy<string>(() =>
        {
            var vsInstallation =
                FindVisualStudio(requiredComponent: "Microsoft.Component.MSBuild")
                ?? throw new InvalidOperationException("MSBuild is not installed with Visual Studio on this machine.");

            return Path.Combine(vsInstallation, @"MSBuild\15.0\Bin\MSBuild.exe");
        });

        public static string MSBuild => msBuild.Value;

        private static readonly Lazy<string> vsTest = new Lazy<string>(() =>
        {
            var vsInstallation =
                FindVisualStudio(requiredComponent: "Microsoft.VisualStudio.TestTools.TestPlatform.V1.CLI") // https://github.com/Microsoft/vswhere/issues/126#issuecomment-360542783
                ?? throw new InvalidOperationException("VSTest is not installed with Visual Studio on this machine.");

            return Path.Combine(vsInstallation, @"Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe");
        });

        public static string VSTest => vsTest.Value;

        private static string FindVisualStudio(string requiredComponent)
        {
            var arguments = new[] { "-latest", "-products", "*", "-requires", requiredComponent, "-property", "installationPath" };

            var releaseInstallationPath = ProcessUtils.Run(
                Environment.CurrentDirectory,
                VSWhere,
                arguments);

            if (!string.IsNullOrEmpty(releaseInstallationPath))
                return releaseInstallationPath;

            var prereleaseInstallationPath = ProcessUtils.Run(
                Environment.CurrentDirectory,
                VSWhere,
                arguments.Concat(new[] { "-prerelease" }));

            if (!string.IsNullOrEmpty(prereleaseInstallationPath))
                return prereleaseInstallationPath;

            return null;
        }
    }
}
