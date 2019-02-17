using System;
using System.IO;

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
            var vsInstallation = ProcessUtils.Run(
                Environment.CurrentDirectory,
                VSWhere,
                new[] { "-latest", "-products", "*", "-requires", "Microsoft.Component.MSBuild", "-property", "installationPath" });

            if (string.IsNullOrWhiteSpace(vsInstallation))
                throw new InvalidOperationException("MSBuild is not installed with Visual Studio on this machine.");

            return Path.Combine(vsInstallation, @"MSBuild\15.0\Bin\MSBuild.exe");
        });

        public static string MSBuild => msBuild.Value;

        private static readonly Lazy<string> vsTest = new Lazy<string>(() =>
        {
            var vsInstallation = ProcessUtils.Run(
                Environment.CurrentDirectory,
                VSWhere,
                // https://github.com/Microsoft/vswhere/issues/126#issuecomment-360542783
                new[] { "-latest", "-products", "*", "-requires", "Microsoft.VisualStudio.TestTools.TestPlatform.V1.CLI", "-property", "installationPath" });

            if (string.IsNullOrWhiteSpace(vsInstallation))
                throw new InvalidOperationException("VSTest is not installed with Visual Studio on this machine.");

            return Path.Combine(vsInstallation, @"Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe");
        });

        public static string VSTest => vsTest.Value;
    }
}
