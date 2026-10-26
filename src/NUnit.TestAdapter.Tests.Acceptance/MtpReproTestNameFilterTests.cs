using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// The repro test names (see <see cref="ReproTestNames"/>) selected one by one with the MTP
/// test application's <c>--filter</c>, for both discovery and execution.
/// </summary>
[Category("FilterParsing")]
public sealed class MtpReproTestNameFilterTests : MtpCsProjAcceptanceTests
{
    protected override string Framework => Frameworks.Net80;

    protected override void AddTestsCs(IsolatedWorkspace workspace) => workspace.AddFile("ReproTests.cs", ReproTestNames.TestsCs);

    [Test, Platform("Win")]
    public void NoFilterRunsEverything()
    {
        var workspace = Build();
        var results = workspace.MtpTest(TestApplicationPath, log: TestContext.WriteLine);
        Verify(ReproTestNames.TotalTests, ReproTestNames.TotalTests, results);
    }

    [Test, Platform("Win")]
    [TestCaseSource(typeof(ReproTestNames), nameof(ReproTestNames.Cases))]
    public void DiscoveryAndExecutionSelectTheTest(string filter, string expectedTestName, string listedTestName)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;

        var listed = workspace.MtpListTests(TestApplicationPath, filter, TestContext.WriteLine);
        Assert.That(listed, Is.EqualTo(new[] { listedTestName }), $"Discovery, filter: {filter}");

        var results = workspace.MtpTest(TestApplicationPath, filter, TestContext.WriteLine);
        Verify(1, 1, results);
        Assert.That(results.ExecutedTestNames, Is.EqualTo([expectedTestName]), $"Execution, filter: {filter}");
    }
}
