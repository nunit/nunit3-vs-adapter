﻿using System;
using System.IO;
using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public sealed class IsolatedWorkspaceManager : IDisposable
    {
        private readonly string workspaceDirectory;
        private readonly ToolResolver toolResolver;
        private readonly StreamWriter reasonFile;
        private bool keep;

        public IsolatedWorkspaceManager(string reason, string baseDirectory, string testNupkgDirectory, string packageCachePath, string downloadCachePath)
        {
            baseDirectory = Path.GetFullPath(baseDirectory);
            testNupkgDirectory = Path.GetFullPath(testNupkgDirectory);
            packageCachePath = Path.GetFullPath(packageCachePath);
            downloadCachePath = Path.GetFullPath(downloadCachePath);

            Directory.CreateDirectory(baseDirectory);

            workspaceDirectory = Utils.CreateUniqueDirectory(baseDirectory);

            toolResolver = new ToolResolver(downloadCachePath);

            reasonFile = File.CreateText(Path.Combine(workspaceDirectory, "Reason.txt"));
            reasonFile.WriteLine(reason);
            reasonFile.WriteLine();
            reasonFile.Flush();

            WriteNuGetConfig(baseDirectory, testNupkgDirectory, packageCachePath);

            File.WriteAllText(Path.Combine(baseDirectory, "Directory.Build.props"), "<Project />");
            File.WriteAllText(Path.Combine(baseDirectory, "Directory.Build.targets"), "<Project />");
        }

        public void Dispose()
        {
            reasonFile.Dispose();
            if (!keep) Utils.DeleteDirectoryRobust(workspaceDirectory);
        }

        public IsolatedWorkspace CreateWorkspace(string name)
        {
            return new IsolatedWorkspace(
                Utils.CreateUniqueDirectory(workspaceDirectory, name),
                toolResolver);
        }

        /// <summary>
        /// Prevents the workspace directory from being deleted when <see cref="Dispose"/> is called.
        /// </summary>
        public void PreserveDirectory(string reason)
        {
            if (!keep)
            {
                keep = true;
                reasonFile.WriteLine("Preserving workspace after cleanup, due to:");
                reasonFile.WriteLine();
            }

            reasonFile.WriteLine(reason);
            reasonFile.Flush();
        }

        private static void WriteNuGetConfig(string directory, string testNupkgDirectory, string packageCachePath)
        {
            const string fallbackFolder = @"C:\Program Files\dotnet\sdk\NuGetFallbackFolder";

            using (var file = File.CreateText(Path.Combine(directory, "nuget.config")))
            using (var writer = XmlWriter.Create(file, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteComment(string.Join(Environment.NewLine,
                    "",
                    "This file exists so that if any of the projects under this folder are opened by an IDE or restored from the CLI by acceptance tests or by hand,",
                    " 1. the .nupkg that is being tested can be referenced by these projects, and",
                    " 2. the .nupkg that is tested does not pollute the global cache in %userprofile%\\.nuget.",
                    ""));

                writer.WriteStartElement("configuration");
                writer.WriteStartElement("config");

                writer.WriteComment(" Implements the second point ");
                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", "globalPackagesFolder");
                writer.WriteAttributeString("value", packageCachePath);
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteStartElement("packageSources");

                writer.WriteComment(" Implements the first point ");
                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", "Build script package output");
                writer.WriteAttributeString("value", testNupkgDirectory);
                writer.WriteEndElement();

                writer.WriteComment($" Speeds up first-time restore by populating {GetLeafDirectoryName(packageCachePath)} from {GetLeafDirectoryName(fallbackFolder)} rather than nuget.org. ");
                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", "Pre-downloaded packages");
                writer.WriteAttributeString("value", fallbackFolder);
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }

        private static string GetLeafDirectoryName(string directoryPath)
        {
            return Path.GetFileName(directoryPath)
                ?? Path.GetFileName(Path.GetDirectoryName(directoryPath));
        }
    }
}
