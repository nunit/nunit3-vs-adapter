using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

[DebuggerDisplay("{Directory,nq}")]
public sealed partial class IsolatedWorkspace(DirectoryMutex directoryMutex, ToolResolver toolResolver)
    : IDisposable
{
    private readonly List<string> projectPaths = [];
    private readonly ToolResolver toolResolver = toolResolver ?? throw new ArgumentNullException(nameof(toolResolver));
    private readonly DirectoryMutex directoryMutex = directoryMutex ?? throw new ArgumentNullException(nameof(toolResolver));

    public bool DumpTestExecution { get; set; } = false;

    public bool DebugTestRun { get; set; } = false;

    public string ExplicitMode { get; set; } = string.Empty;  // Empty means we don't set it, but rely on default.

    public string Directory => directoryMutex.DirectoryPath;

    public void Dispose() => directoryMutex.Dispose();

    public IsolatedWorkspace AddProject(string path, string contents)
    {
        AddFile(path, contents);
        projectPaths.Add(path);
        return this;
    }

    public IsolatedWorkspace AddFile(string path, string contents)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("File path must be specified.", nameof(path));

        if (Path.IsPathRooted(path))
            throw new ArgumentException("File path must not be rooted.", nameof(path));

        File.WriteAllText(Path.Combine(Directory, path), Utils.RemoveIndent(contents));
        return this;
    }

    public void DotNetRestore()
    {
        ConfigureRun("dotnet")
            .Add("restore")
            .Run();
    }

    public void DotNetBuild(bool noRestore = false)
    {
        ConfigureRun("dotnet")
            .Add("build")
            .AddIf(noRestore, "--no-restore")
            .Run();
    }

    /// <summary>
    /// Runs dotnet test.
    /// </summary>
    /// <param name="filterArgument">Possible filter statement.</param>
    /// <param name="noBuild">if you run MSBuild or dotnet build first, set to false.</param>
    /// <param name="verbose">Set NUnit verbosity to 5, enables seing more info from the run in StdOut.</param>
    /// <returns>VSTestResults.</returns>
    public VSTestResult DotNetTest(string filterArgument = "", bool noBuild = false, bool verbose = false, Action<string> log = null)
    {
        using var tempTrxFile = new TempFile();

        var dotnettest = ConfigureRun("dotnet")
            .Add("test")
            .AddIf(noBuild, "--no-build")
            .Add("-v:n")
            .Add("--logger")
            .Add("trx;LogFileName=" + tempTrxFile);

        bool hasNUnitWhere = filterArgument.StartsWith("NUnit.Where");

        if (filterArgument.Length > 0 && !hasNUnitWhere)
        {
            dotnettest.Add("--filter").Add($"{filterArgument}");
        }
        else if (hasNUnitWhere)
        {
            dotnettest.Add("--").Add(filterArgument);
        }
        if (verbose)
        {
            if (!hasNUnitWhere)
                dotnettest.Add("--");
            dotnettest.Add("NUnit.Verbosity=5");
        }

        if (ExplicitMode != string.Empty)
        {
            bool hasPrefix = hasNUnitWhere || verbose;
            if (!hasPrefix)
                dotnettest.Add("--");
            dotnettest.Add($"NUnit.ExplicitMode={ExplicitMode}");
        }

        log?.Invoke($"\n{dotnettest.ArgumentsAsEscapedString}");
        var result = dotnettest.Run(throwOnError: false);

        if (new FileInfo(tempTrxFile).Length == 0)
            result.ThrowIfError();

        return VSTestResult.Load(result, tempTrxFile);
    }

    public void DotNetVSTest(IEnumerable<string> testAssemblyPaths)
    {
        ConfigureRun("dotnet")
            .Add("vstest")
            .AddRange(testAssemblyPaths)
            .Run();
    }

    public void NuGetRestore(string packagesDirectory = null)
    {
        ConfigureRun(toolResolver.NuGet)
            .Add("restore")
            .AddRangeIf(packagesDirectory != null, ["-PackagesDirectory", packagesDirectory])
            .Run();
    }

    public void MsBuild(string target = null, bool restore = false)
    {
        ConfigureRun(toolResolver.MSBuild)
            .AddIf(target != null, "/t:" + target)
            .AddIf(restore, "/restore")
            .Run();
    }

    public VSTestResult VSTest(string testAssemblyPath, IFilterArgument filter)
    {
        using var tempTrxFile = new TempFile();

        var vstest = ConfigureRun(toolResolver.VSTest)
            .Add(testAssemblyPath)
            .Add("/logger:trx;LogFileName=" + tempTrxFile);

        if (filter.HasArguments)
        {
            vstest.Add(filter.CompletedArgument());
        }

        if (DumpTestExecution)
            vstest.Add("--").Add("NUnit.DumpXmlTestResults=true");

        if (ExplicitMode != string.Empty)
        {
            bool hasPrefix = DumpTestExecution;
            if (!hasPrefix)
                vstest.Add("--");
            vstest.Add($"NUnit.ExplicitMode={ExplicitMode}");
        }

        var result = vstest.Run(throwOnError: false);

        if (new FileInfo(tempTrxFile).Length == 0)
        {
            result.ThrowIfError();
            return new VSTestResult(result);
        }

        return VSTestResult.Load(result, tempTrxFile);
    }

    /// <summary>
    /// Runs a Microsoft Testing Platform test application directly, rather than through
    /// <c>dotnet test</c>.
    ///
    /// Invoking the executable is deliberate. Under <c>dotnet test</c> the filter is forwarded
    /// to MSBuild as a property, so a filter value containing a space, a colon or a double
    /// quote is mangled before the adapter sees it and the run fails with <c>MSB4177</c>.
    /// The test application takes <c>--filter</c> as an ordinary argument, so any escaped
    /// value can be passed through intact — which is what these tests need to exercise.
    /// </summary>
    /// <param name="executablePath">Path to the built test application, relative to the workspace.</param>
    /// <param name="filterArgument">A VSTest filter expression, or empty for no filter.</param>
    /// <param name="log">Optional sink for the command line and output.</param>
    public VSTestResult MtpTest(string executablePath, string filterArgument = "", Action<string> log = null)
    {
        // The platform requires the report name to end with ".trx", and the file must not
        // already exist.
        using var tempTrxFile = new TempFile("trx");

        var run = ConfigureRun(Path.Combine(Directory, executablePath))
            .Add("--report-trx")
            .Add("--report-trx-filename")
            .Add(tempTrxFile)
            .AddRangeIf(filterArgument.Length > 0, ["--filter", filterArgument]);

        // The platform has no equivalent of VSTest's trailing "-- NUnit.Key=value" arguments;
        // adapter settings reach it through a .runsettings file instead.
        var settingsFile = WriteNUnitRunSettings();

        if (settingsFile != null)
            run.Add("--settings").Add(settingsFile);

        log?.Invoke($"\n{run.ArgumentsAsEscapedString}");
        var result = run.Run(throwOnError: false);
        log?.Invoke($"\n{result.StdOut}");

        if (!File.Exists(tempTrxFile) || new FileInfo(tempTrxFile).Length == 0)
        {
            result.ThrowIfError();
            return new VSTestResult(result);
        }

        return VSTestResult.Load(result, tempTrxFile);
    }

    /// <summary>
    /// Runs a Microsoft Testing Platform test application with <c>--list-tests</c> and returns
    /// the display names it reports.
    ///
    /// This is what makes the invariant in <c>docs/TestFilterParsing-v7.md</c> directly
    /// testable: discovery and execution are given the same filter string, and the counts have
    /// to agree.
    /// </summary>
    public IReadOnlyList<string> MtpListTests(string executablePath, string filterArgument = "", Action<string> log = null)
    {
        var run = ConfigureRun(Path.Combine(Directory, executablePath))
            .Add("--list-tests")
            .AddRangeIf(filterArgument.Length > 0, ["--filter", filterArgument]);

        log?.Invoke($"\n{run.ArgumentsAsEscapedString}");
        var result = run.Run(throwOnError: false).ThrowIfError();
        log?.Invoke($"\n{result.StdOut}");

        var names = new List<string>();

        foreach (var rawLine in result.StdOut.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');

            // Listed names are indented by exactly two spaces. The run summary uses the same
            // indent for its own detail lines, so stop once the summary begins.
            if (line.StartsWith("Test discovery summary:", StringComparison.Ordinal))
                break;

            if (line.StartsWith("  ", StringComparison.Ordinal) && line.Length > 2 && line[2] != ' ')
                names.Add(line.Substring(2));
        }

        return names;
    }

    /// <summary>
    /// Writes a .runsettings file carrying whichever adapter settings this workspace has been
    /// configured with, and returns its path, or null when there is nothing to configure.
    /// </summary>
    private string WriteNUnitRunSettings()
    {
        if (!DumpTestExecution && ExplicitMode == string.Empty)
            return null;

        var settings = new StringBuilder();
        settings.AppendLine("<RunSettings>");
        settings.AppendLine("  <NUnit>");

        if (DumpTestExecution)
            settings.AppendLine("    <DumpXmlTestResults>true</DumpXmlTestResults>");

        if (ExplicitMode != string.Empty)
            settings.AppendLine($"    <ExplicitMode>{ExplicitMode}</ExplicitMode>");

        settings.AppendLine("  </NUnit>");
        settings.AppendLine("</RunSettings>");

        const string fileName = "mtp.runsettings";
        File.WriteAllText(Path.Combine(Directory, fileName), settings.ToString());
        return fileName;
    }

    private RunSettings ConfigureRun(string filename) => new(Directory, filename);
}