using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// The repro test names (see <see cref="ReproTestNames"/>) selected one by one with
/// <c>vstest /TestCaseFilter:</c>. <c>dotnet test --filter</c> is not used: it loses the
/// quoting of a filter with a double quote (MSB4177) before the adapter sees it.
/// </summary>
[Category("FilterParsing")]
public sealed class ReproTestNameFilterTests : CsProjAcceptanceTests
{
    protected override string Framework => Frameworks.Net80;

    protected override void AddTestsCs(IsolatedWorkspace workspace) => workspace.AddFile("ReproTests.cs", ReproTestNames.TestsCs);

    [Test, Platform("Win")]
    public void NoFilterRunsEverything()
    {
        var workspace = Build();
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", VsTestFilter.NoFilter);
        Verify(ReproTestNames.TotalTests, ReproTestNames.TotalTests, results);
    }

    [Test, Platform("Win")]
    [TestCaseSource(typeof(ReproTestNames), nameof(ReproTestNames.Cases))]
    public void VsTestSelectsTheTest(string filter, string expectedTestName)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));

        Verify(1, 1, results);
        Assert.That(results.ExecutedTestNames, Is.EqualTo([expectedTestName]), $"Filter: {filter}");
    }
}
