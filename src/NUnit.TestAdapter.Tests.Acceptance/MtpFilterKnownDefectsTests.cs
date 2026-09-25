using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// The Microsoft Testing Platform counterpart of <see cref="FilterKnownDefectsTests"/>.
///
/// This is the only acceptance coverage that reaches
/// <c>NUnitTestFilterBuilder.ConvertVsTestFilterToNUnitFilterForMTP</c> and the bridge's own
/// filter handling — item 4 of <c>docs/TestFilterParsing-6.x.md</c> and the escapability section
/// of <c>docs/TestFilterParsing-v7.md</c> are both about code that only runs here.
///
/// The platform exposes <c>--list-tests</c> and <c>--filter</c> on the test application itself,
/// which lets these tests assert the invariant from the v7 document in its original form rather
/// than a proxy for it: give discovery and execution the same filter string, and require that
/// they agree.
///
/// Cases needing the v7 grammar are ignored with <c>FixIn.V7Reason</c>; the rest are expected to
/// fail until their 6.x item is implemented.
/// </summary>
[Category("FilterParsing")]
public sealed class MtpFilterKnownDefectsTests : MtpCsProjAcceptanceTests
{
    protected override string Framework => Frameworks.Net80;

    private const int TotalTests = 7;

    protected override void AddTestsCs(IsolatedWorkspace workspace)
    {
        workspace.AddFile("KnownDefects.cs", """
            using System;
            using NUnit.Framework;

            namespace KnownDefects
            {
                public class Foo
                {
                    // Issue 1490: a space between the name and the argument list.
                    [TestCase(1, TestName = @"SpaceBefore (Case 1)")]
                    public void SpaceBeforeCase(int a) => Assert.Pass();

                    // A backslash inside a quoted string argument.
                    [TestCase(1, TestName = @"QuotedBackslash(""C:\Temp"")")]
                    public void QuotedBackslashCase(int a) => Assert.Pass();

                    // Issue 1405: a pipe inside a quoted string argument.
                    [TestCase(1, TestName = @"QuotedPipe(""This | That"")")]
                    public void QuotedPipeCase(int a) => Assert.Pass();

                    // An escaped operator with no argument list to re-glue the token.
                    [TestCase(1, TestName = @"PipeOutside_A|B")]
                    public void PipeOutsideCase(int a) => Assert.Pass();

                    [TestCase(1, TestName = @"AmpersandOutside_A&B")]
                    public void AmpersandOutsideCase(int a) => Assert.Pass();

                    // Issue 1501: an argument list with no closing parenthesis.
                    [TestCase(1, TestName = @"NoClose(")]
                    public void NoCloseCase(int a) => Assert.Pass();

                    [Test]
                    public void Sanity() => Assert.Pass();
                }
            }
            """);
    }

    [Test, Platform("Win")]
    public void NoFilterRunsEverything()
    {
        var workspace = Build();
        var results = workspace.MtpTest(TestApplicationPath, log: TestContext.WriteLine);
        Verify(TotalTests, TotalTests, results);
    }

    [Test, Platform("Win")]
    public void NoFilterListsEverything()
    {
        var workspace = Build();
        var listed = workspace.MtpListTests(TestApplicationPath, log: TestContext.WriteLine);
        Assert.That(listed, Has.Count.EqualTo(TotalTests));
    }

    /// <summary>
    /// Each filter selects exactly one test, and the platform's own discovery agrees that it
    /// does. The assertion on <c>--list-tests</c> comes first so that a failure distinguishes
    /// "the filter never matched anything" from "discovery matched but execution disagreed",
    /// which are different defects.
    /// </summary>
    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=KnownDefects.Foo.Sanity", "Sanity", TestName = "{m}_Sanity")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", "SpaceBefore (Case 1)", TestName = "{m}_SpaceBeforeArguments_Issue1490", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedBackslash\(""C:\\Temp""\)", @"QuotedBackslash(""C:\Temp"")", TestName = "{m}_QuotedBackslash_Issue1489")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedPipe\(""This \| That""\)", @"QuotedPipe(""This | That"")", TestName = "{m}_QuotedPipe_Issue1405")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.PipeOutside_A\|B", "PipeOutside_A|B", TestName = "{m}_PipeOutsideArguments_Issue1488", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.AmpersandOutside_A\&B", "AmpersandOutside_A&B", TestName = "{m}_AmpersandOutsideArguments_Issue1488", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    public void DiscoveryAndExecutionAgree(string filter, string expectedTestName)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;

        var listed = workspace.MtpListTests(TestApplicationPath, filter, TestContext.WriteLine);

        Assert.That(listed, Is.EqualTo(new[] { expectedTestName }),
            $"Discovery should list '{expectedTestName}' and nothing else for filter '{filter}'.");

        var results = workspace.MtpTest(TestApplicationPath, filter, TestContext.WriteLine);

        VerifySelected(expectedTestName, results);
    }

    /// <summary>
    /// Issue 1501 under the platform. A 6.x test, ignored for the same reason as the VSTest
    /// version: the tokenizer does not terminate, so the test application allocates until it is
    /// killed. Bound the loop and remove the Ignore in the same change.
    /// </summary>
    [Test, Platform("Win")]
    [Property("Issue", "1501")]
    [Category(FixIn.SixX)]
    [Ignore(FixIn.Item1Reason)]
    public void DiscoveryAndExecutionAgreeWithUnbalancedParenthesis()
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;

        const string filter = @"FullyQualifiedName=KnownDefects.Foo.NoClose\(";

        var listed = workspace.MtpListTests(TestApplicationPath, filter, TestContext.WriteLine);
        Assert.That(listed, Is.EqualTo(new[] { "NoClose(" }));

        var results = workspace.MtpTest(TestApplicationPath, filter, TestContext.WriteLine);
        VerifySelected("NoClose(", results);
    }

    /// <summary>
    /// Asserts that exactly the requested test ran, and passed. Counters alone would accept a
    /// filter that selected a different single test, since every generated test body is
    /// <c>Assert.Pass()</c>.
    /// </summary>
    private void VerifySelected(string expectedTestName, VSTestResult results)
    {
        Verify(1, 1, results);

        Assert.That(results.ExecutedTestNames, Is.EqualTo(new[] { expectedTestName }),
            $"The filter should have selected '{expectedTestName}' and nothing else.");
    }
}
