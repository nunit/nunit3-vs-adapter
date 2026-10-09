using System;
using System.Diagnostics;
using System.IO;
using IO = System.IO;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

[DebuggerDisplay("{ToString(),nq}")]
internal sealed class TempFile : IDisposable
{
    private readonly string reservedPath;

    public string Path { get; }

    /// <param name="extension">
    /// Optional extension, without a leading dot, for tools that insist on one. The Microsoft
    /// Testing Platform rejects <c>--report-trx-filename</c> unless it ends with <c>.trx</c>.
    /// The returned path does not exist yet, which such tools also tend to require.
    /// </param>
    public TempFile(string extension = null)
    {
        reservedPath = IO.Path.GetTempFileName();
        Path = extension is null ? reservedPath : IO.Path.ChangeExtension(reservedPath, extension);
    }

    public void Dispose()
    {
        File.Delete(reservedPath);

        if (Path != reservedPath)
            File.Delete(Path);
    }

    public override string ToString() => Path;

    public static implicit operator string(TempFile tempDirectory) => tempDirectory.Path;
}