using System;
using System.Diagnostics;
using System.IO;
using IO = System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    [DebuggerDisplay("{ToString(),nq}")]
    internal sealed class TempFile : IDisposable
    {
        public string Path { get; }

        public TempFile()
        {
            Path = IO.Path.GetTempFileName();
        }

        public void Dispose()
        {
            File.Delete(Path);
        }

        public override string ToString() => Path;

        public static implicit operator string(TempFile tempDirectory) => tempDirectory.Path;
    }
}
