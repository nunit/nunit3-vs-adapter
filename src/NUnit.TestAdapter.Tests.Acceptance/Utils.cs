using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    internal static class Utils
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars()
            .Concat(new[] { '"' }) // VSTest strips quotes from the test assembly path and fails to locate the .deps.json in the same directory
            .Distinct().ToArray();

        public static string GetSafeFilename(string arbitraryString, bool allowDirectorySeparators = false)
        {
            var replaceIndex = arbitraryString.IndexOfAny(InvalidFileNameChars, 0);
            if (replaceIndex == -1) return arbitraryString;

            var r = new StringBuilder();
            var i = 0;

            do
            {
                r.Append(arbitraryString, i, replaceIndex - i);

                switch (arbitraryString[replaceIndex])
                {
                    case '<':
                    case '>':
                    case '|':
                    case ':':
                    case '*':
                    case '\\':
                    case '/':
                        r.Append(allowDirectorySeparators ? Path.DirectorySeparatorChar : '-');
                        break;
                    case '\0':
                    case '\f':
                    case '?':
                    case '"':
                        break;
                    case '\t':
                    case '\n':
                    case '\r':
                    case '\v':
                        r.Append(' ');
                        break;
                    default:
                        r.Append('_');
                        break;
                }

                i = replaceIndex + 1;
                replaceIndex = arbitraryString.IndexOfAny(InvalidFileNameChars, i);
            }
            while (replaceIndex != -1);

            r.Append(arbitraryString, i, arbitraryString.Length - i);

            return r.ToString();
        }

        public static DirectoryMutex CreateMutexDirectory(string parentDirectory, string name = null)
        {
            parentDirectory = Path.GetFullPath(parentDirectory);

            var safeName = name is null ? null : GetSafeFilename(name);

            for (var id = 1; ; id++)
            {
                var path = Path.Combine(
                    parentDirectory,
                    safeName is null ? id.ToString() :
                    id == 1 ? safeName :
                    safeName + "_" + id);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    if (DirectoryMutex.TryAcquire(path) is { } mutex)
                    {
                        // Make sure that the directory is still empty (besides the mutex file) at this point so that a
                        // non-empty directory is not used.
                        if (Directory.GetFileSystemEntries(path).Length == 1)
                            return mutex;
                    }
                }
            }
        }

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

        /// <summary>
        /// Removes any indentation that is common to all lines. If the first line has no indentation,
        /// indentation common to all other lines is removed. If the first line is empty, it is removed.
        /// Empty lines are not considered when calculating indentation.
        /// </summary>
        public static string RemoveIndent(string indented)
        {
            if (indented is null) return null;

            var reader = new StringReader(indented);
            var firstLine = reader.ReadLine();

            var firstLineStartsWithoutIndent = firstLine.Length != 0 && firstLine[0] != ' ';

            var readerForCountingPass = new StringReader(indented);

            // Skip the first line when determining common indentation if it has no indentation
            if (firstLineStartsWithoutIndent)
                _ = readerForCountingPass.ReadLine();

            var indentationCharCount = CountCommonIndentationChars(readerForCountingPass);

            var builder = new StringBuilder();

            var previousLineHasEnded = true;

            if (firstLine.Length != 0)
            {
                if (firstLineStartsWithoutIndent)
                {
                    builder.Append(firstLine);
                    previousLineHasEnded = false;
                }
                else
                {
                    // Start at beginning
                    reader = new StringReader(indented);
                }
            }

            var remainingIndentationChars = indentationCharCount;

            while (reader.Read() is var next && next != -1)
            {
                if (next == ' ' && remainingIndentationChars > 0)
                {
                    remainingIndentationChars--;
                    continue;
                }

                if (!previousLineHasEnded) builder.AppendLine();

                if (next == '\r')
                {
                    next = reader.Read();
                    if (next != '\n') throw new NotImplementedException("Carriage return without line feed");
                }

                if (next != '\n')
                {
                    builder.Append((char)next).Append(reader.ReadLine());
                }

                previousLineHasEnded = false;
                remainingIndentationChars = indentationCharCount;
            }

            return builder.ToString();
        }

        private static int CountCommonIndentationChars(StringReader reader)
        {
            var maxCount = 0;
            var currentLineCount = 0;

            while (true)
            {
                switch (reader.Read())
                {
                    case -1:
                        return maxCount;

                    case ' ':
                        currentLineCount++;
                        break;

                    case '\t':
                        throw new NotImplementedException("Tabs");

                    case '\r':
                    case '\n':
                        currentLineCount = 0;
                        break;

                    default:
                        if (currentLineCount == 0) return 0;

                        if (maxCount == 0 || maxCount > currentLineCount)
                            maxCount = currentLineCount;

                        currentLineCount = 0;

                        _ = reader.ReadLine();
                        break;
                }
            }
        }
    }
}
