using System;
using System.IO;
using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public sealed class IsolatedWorkspaceManager : IDisposable
    {
        private readonly string workspaceDirectory;
        private readonly StreamWriter reasonFile;
        private bool keep;

        public IsolatedWorkspaceManager(string reason, string baseDirectory, string testNupkgDirectory, string packageCachePath)
        {
            baseDirectory = Path.GetFullPath(baseDirectory);
            testNupkgDirectory = Path.GetFullPath(testNupkgDirectory);
            packageCachePath = Path.GetFullPath(packageCachePath);

            Directory.CreateDirectory(baseDirectory);

            workspaceDirectory = Utils.CreateUniqueDirectory(baseDirectory);

            reasonFile = File.CreateText(Path.Combine(workspaceDirectory, "Reason.txt"));
            reasonFile.WriteLine(reason);
            reasonFile.WriteLine();
            reasonFile.Flush();

            WriteNuGetConfig(baseDirectory, testNupkgDirectory, packageCachePath);
        }

        public void Dispose()
        {
            reasonFile.Dispose();
            if (!keep) Utils.DeleteDirectoryRobust(workspaceDirectory);
        }

        public IsolatedWorkspace CreateWorkspace(string name)
        {
            return new IsolatedWorkspace(Utils.CreateUniqueDirectory(workspaceDirectory, name));
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
            using (var file = File.CreateText(Path.Combine(directory, "nuget.config")))
            using (var writer = XmlWriter.Create(file, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartElement("configuration");
                writer.WriteStartElement("config");

                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", "globalPackagesFolder");
                writer.WriteAttributeString("value", packageCachePath);
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteStartElement("packageSources");

                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", "Build script package output");
                writer.WriteAttributeString("value", testNupkgDirectory);
                writer.WriteEndElement();

                writer.WriteStartElement("add");
                writer.WriteAttributeString("key", "Pre-downloaded packages");
                writer.WriteAttributeString("value", @"C:\Program Files\dotnet\sdk\NuGetFallbackFolder");
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndElement();
            }
        }
    }
}
