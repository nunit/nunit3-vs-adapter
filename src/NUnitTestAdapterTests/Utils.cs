using System.IO;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    internal static class Utils
    {
        public static void DeleteDirectoryRobust(string directory)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    Directory.Delete(directory, recursive: true);
                    break;
                }
                catch (DirectoryNotFoundException)
                {
                    break;
                }
                catch (IOException ex) when (attempt < 3 && (WinErrorCode)ex.HResult == WinErrorCode.DirNotEmpty)
                {
                    TestContext.WriteLine("Another process added files to the directory while its contents were being deleted. Retrying...");
                }
            }
        }

        private enum WinErrorCode : ushort
        {
            DirNotEmpty = 145
        }
    }
}
