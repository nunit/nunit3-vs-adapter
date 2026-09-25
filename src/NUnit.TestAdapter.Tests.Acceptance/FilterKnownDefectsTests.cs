using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Acceptance coverage for the filter-parsing defects described in
/// <c>docs/TestFilterParsing-6.x.md</c> and <c>docs/TestFilterParsing-v7.md</c>.
///
/// Every filter below is escaped the way the character-escaping rules for selective unit
/// tests prescribe, and the test platform discovers each of these tests with the same
/// filter string. The invariant being asserted is the one from the v7 document: a test that
/// <c>--list-tests</c> shows with a given filter must run with that same filter.
///
/// These are expected to fail until the parser is fixed. They complement the unit-level
/// tests in <c>FilterRoundTripConformanceTests</c> by proving the failure end to end,
/// through a real <c>vstest</c> or <c>dotnet test</c> invocation, rather than only against
/// the parser in isolation.
///
/// Display names are set with <c>TestName</c> rather than generated from arguments, so the
/// fully qualified name each filter has to match is exact and does not depend on how NUnit
/// happens to render a particular argument value.
///
/// Each case asserts which test ran, not just how many. Every generated test body is
/// <c>Assert.Pass()</c>, so a filter that selected the wrong single test would satisfy a
/// count-only assertion.
///
/// Tests that need the v7 grammar are ignored with <c>FixIn.V7Reason</c>, so an ordinary run is
/// red on the 6.x work only.
/// </summary>
[Category("FilterParsing")]
public sealed class FilterKnownDefectsTests : CsProjAcceptanceTests
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

                    // A backslash inside a quoted string argument, which the lexer unescapes
                    // once on its own and the parser then unescapes again.
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
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", VsTestFilter.NoFilter);
        Verify(TotalTests, TotalTests, results);
    }

    /// <summary>
    /// The <c>vstest</c> entry point, which takes the filter as a single
    /// <c>/TestCaseFilter:</c> argument and so can carry any escaped value.
    /// </summary>
    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=KnownDefects.Foo.Sanity", "Sanity", TestName = "{m}_Sanity")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", "SpaceBefore (Case 1)", TestName = "{m}_SpaceBeforeArguments_Issue1490", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedBackslash\(""C:\\Temp""\)", @"QuotedBackslash(""C:\Temp"")", TestName = "{m}_QuotedBackslash_Issue1489")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedPipe\(""This \| That""\)", @"QuotedPipe(""This | That"")", TestName = "{m}_QuotedPipe_Issue1405")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.PipeOutside_A\|B", "PipeOutside_A|B", TestName = "{m}_PipeOutsideArguments_Issue1488", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.AmpersandOutside_A\&B", "AmpersandOutside_A&B", TestName = "{m}_AmpersandOutsideArguments_Issue1488", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    public void VsTestSelectsTheTest(string filter, string expectedTestName)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));

        VerifySelected(expectedTestName, results);
    }

    /// <summary>
    /// The <c>dotnet test --filter</c> entry point.
    ///
    /// The cases here are a subset of the <c>vstest</c> ones: a filter value containing a
    /// double quote cannot be carried through this CLI at all — <c>dotnet test</c> forwards
    /// it to MSBuild as a property and the quoting is lost, so the run fails with
    /// <c>MSB4177: Invalid property</c> before the adapter is ever reached. That is a
    /// limitation of the command line rather than a defect in the adapter, so asserting on
    /// it here would prove nothing. The quoted-argument shapes are covered by the
    /// <c>vstest</c> cases above and by the unit-level conformance tests.
    /// </summary>
    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=KnownDefects.Foo.Sanity", "Sanity", TestName = "{m}_Sanity")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", "SpaceBefore (Case 1)", TestName = "{m}_SpaceBeforeArguments_Issue1490", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.PipeOutside_A\|B", "PipeOutside_A|B", TestName = "{m}_PipeOutsideArguments_Issue1488", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.AmpersandOutside_A\&B", "AmpersandOutside_A&B", TestName = "{m}_AmpersandOutsideArguments_Issue1488", Category = FixIn.V7, Ignore = FixIn.V7Reason)]
    public void DotNetTestSelectsTheTest(string filter, string expectedTestName)
    {
        var workspace = Build();
        var results = workspace.DotNetTest(filter, true, true, TestContext.WriteLine);

        VerifySelected(expectedTestName, results);
    }

    /// <summary>
    /// Issue 1501. The adapter's tokenizer never terminates on an argument list with no
    /// closing parenthesis, so the test host allocates until it runs out of memory.
    ///
    /// A 6.x test, ignored only because the failure mode is memory exhaustion rather than a
    /// normal assertion failure. Bound the loop at end of input and remove the Ignore in the
    /// same change.
    /// </summary>
    [Test, Platform("Win")]
    [Property("Issue", "1501")]
    [Category(FixIn.SixX)]
    [Ignore(FixIn.Item1Reason)]
    public void VsTestSelectsTheTestWithUnbalancedParenthesis()
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;
        var results = workspace.VSTest(
            $@"bin\Debug\{Framework}\Test.dll",
            new VsTestTestCaseFilter(@"FullyQualifiedName=KnownDefects.Foo.NoClose\("));

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
