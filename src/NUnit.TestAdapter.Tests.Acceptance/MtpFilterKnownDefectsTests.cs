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
/// Expected to fail until the parser is fixed.
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
    [TestCase("FullyQualifiedName=KnownDefects.Foo.Sanity", TestName = "{m}_Sanity")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", TestName = "{m}_SpaceBeforeArguments_Issue1490", Category = FixIn.V7)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedBackslash\(""C:\\Temp""\)", TestName = "{m}_QuotedBackslash_Issue1489")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedPipe\(""This \| That""\)", TestName = "{m}_QuotedPipe_Issue1405")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.PipeOutside_A\|B", TestName = "{m}_PipeOutsideArguments_Issue1488", Category = FixIn.V7)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.AmpersandOutside_A\&B", TestName = "{m}_AmpersandOutsideArguments_Issue1488", Category = FixIn.V7)]
    public void DiscoveryAndExecutionAgree(string filter)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;

        var listed = workspace.MtpListTests(TestApplicationPath, filter, TestContext.WriteLine);

        Assert.That(listed, Has.Count.EqualTo(1),
            $"Discovery should list exactly one test for filter '{filter}'.");

        var results = workspace.MtpTest(TestApplicationPath, filter, TestContext.WriteLine);

        Verify(1, 1, results);
    }

    /// <summary>
    /// Issue 1501 under the platform. Explicit for the same reason as the VSTest version: the
    /// tokenizer does not terminate, so the test application allocates until it is killed.
    /// </summary>
    [Test, Platform("Win")]
    [Property("Issue", "1501")]
    [Category(FixIn.SixX)]
    [Explicit("Exhausts memory in the child test application until the unbalanced-parenthesis loop is bounded.")]
    public void DiscoveryAndExecutionAgreeWithUnbalancedParenthesis()
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;

        const string filter = @"FullyQualifiedName=KnownDefects.Foo.NoClose\(";

        var listed = workspace.MtpListTests(TestApplicationPath, filter, TestContext.WriteLine);
        Assert.That(listed, Has.Count.EqualTo(1));

        var results = workspace.MtpTest(TestApplicationPath, filter, TestContext.WriteLine);
        Verify(1, 1, results);
    }
}
