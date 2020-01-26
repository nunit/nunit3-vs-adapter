using System;
using System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class DirectoryMutex : IDisposable
    {
        private readonly FileStream mutexFile;

        public static DirectoryMutex TryAcquire(string directoryPath)
        {
            var mutexFilePath = Path.Combine(directoryPath, ".mutex");

            FileStream stream;
            try
            {
                stream = new FileStream(mutexFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.DeleteOnClose);
            }
            catch (IOException) // On Windows, (ushort)ex.HResult will be ERROR_SHARING_VIOLATION
            {
                return null;
            }

            return new DirectoryMutex(stream, directoryPath);
        }

        private DirectoryMutex(FileStream mutexFile, string directoryPath)
        {
            this.mutexFile = mutexFile ?? throw new ArgumentNullException(nameof(mutexFile));
            DirectoryPath = directoryPath ?? throw new ArgumentNullException(nameof(directoryPath));
        }

        public string DirectoryPath { get; }

        public void Dispose() => mutexFile.Dispose();
    }
}
