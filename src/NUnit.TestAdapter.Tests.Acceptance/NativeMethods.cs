using System;
using System.Runtime.InteropServices;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    internal static class NativeMethods
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createdirectoryw
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

        public enum WindowsErrorCode : ushort
        {
            AlreadyExists = 0xB7,
        }
    }
}
