using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// The Microsoft Testing Platform counterpart of <see cref="FilterKnownDefectsTests"/>.
///
/// A plain command line run takes the command line filter path (<c>TestFilterParser</c>).
/// <see cref="IdeModeSelectsTheTest"/> sets <c>DesignMode</c>, which is what Visual Studio Test
/// Explorer does, and so reaches
/// <c>NUnitTestFilterBuilder.ConvertVsTestFilterToNUnitFilterForMTP</c> and its fast path, item 4
/// of <c>docs/TestFilterParsing-6.x.md</c>.
///
/// The platform exposes <c>--list-tests</c> and <c>--filter</c> on the test application itself,
/// which lets these tests assert the invariant from the v7 document in its original form rather
/// than a proxy for it: give discovery and execution the same filter string, and require that
/// they agree.
///
/// The cases first marked as needing a breaking v7 grammar pass with the non-breaking 6.4
/// tokenizer, which reads a value up to the next unescaped operator.
/// </summary>
[Category("FilterParsing")]
public sealed class MtpFilterKnownDefectsTests : MtpCsProjAcceptanceTests
{
    protected override string Framework => Frameworks.Net80;

    private const int TotalTests = 8;

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

                    // Item 4: an HTML entity that is part of the name.
                    [TestCase(1, TestName = @"HtmlEntity(a &amp; b)")]
                    public void HtmlEntityCase(int a) => Assert.Pass();

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
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", "SpaceBefore (Case 1)", TestName = "{m}_SpaceBeforeArguments_Issue1490", Category = FixIn.SixX)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedBackslash\(""C:\\Temp""\)", @"QuotedBackslash(""C:\Temp"")", TestName = "{m}_QuotedBackslash_Issue1489")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedPipe\(""This \| That""\)", @"QuotedPipe(""This | That"")", TestName = "{m}_QuotedPipe_Issue1405")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.PipeOutside_A\|B", "PipeOutside_A|B", TestName = "{m}_PipeOutsideArguments_Issue1488", Category = FixIn.SixX)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.AmpersandOutside_A\&B", "AmpersandOutside_A&B", TestName = "{m}_AmpersandOutsideArguments_Issue1488", Category = FixIn.SixX)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.HtmlEntity\(a \&amp; b\)", "HtmlEntity(a &amp; b)", TestName = "{m}_HtmlEntity")]
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
    /// The same selection with <c>DesignMode</c> set, as in Visual Studio Test Explorer, so the
    /// adapter takes the FullyQualifiedName fast path. The dump shows the path: only the fast
    /// path writes <c>ParsedTestCases</c> (<c>MsFilter</c> is written before the choice).
    /// </summary>
    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=KnownDefects.Foo.Sanity", "Sanity", TestName = "{m}_Sanity")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", "SpaceBefore (Case 1)", TestName = "{m}_SpaceBeforeArguments_Issue1490")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedBackslash\(""C:\\Temp""\)", @"QuotedBackslash(""C:\Temp"")", TestName = "{m}_QuotedBackslash_Issue1489")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedPipe\(""This \| That""\)", @"QuotedPipe(""This | That"")", TestName = "{m}_QuotedPipe_Issue1405")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.HtmlEntity\(a \&amp; b\)", "HtmlEntity(a &amp; b)", TestName = "{m}_HtmlEntity_Item4")]
    public void IdeModeSelectsTheTest(string filter, string expectedTestName)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;
        workspace.DesignMode = true;

        var results = workspace.MtpTest(TestApplicationPath, filter, TestContext.WriteLine);

        VerifySelected(expectedTestName, results);
        var dump = System.IO.File.ReadAllText(System.IO.Path.Combine(workspace.Directory, $@"bin\Debug\{Framework}\Dump\E_Test.dll.dump"));
        Assert.That(dump, Does.Contain("<ParsedTestCases>"), "The run did not take the Test Explorer (fast path) filter conversion.");
    }

    /// <summary>
    /// Issue 1501 under the platform: the tokenizer used to not terminate, so the test
    /// application allocated until it was killed. The platform wraps the filter in parentheses,
    /// (FullyQualifiedName=KnownDefects.Foo.NoClose\(); the escaped '(' is part of the name, so
    /// the outer ')' closes the group.
    /// </summary>
    [Test, Platform("Win")]
    [Property("Issue", "1501")]
    [Category(FixIn.SixX)]
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
