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
/// through a real <c>dotnet test</c> or <c>vstest</c> invocation, rather than only against
/// the parser in isolation.
///
/// Display names are set with <c>TestName</c> rather than generated from arguments, so the
/// fully qualified name each filter has to match is exact and does not depend on how NUnit
/// happens to render a particular argument value.
/// </summary>
public sealed class FilterKnownDefectsTests : CsProjAcceptanceTests
{
    protected override string Framework => Frameworks.Net80;

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

    private const int TotalTests = 7;

    [Test, Platform("Win")]
    public void NoFilterRunsEverything()
    {
        var workspace = Build();
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", VsTestFilter.NoFilter);
        Verify(TotalTests, TotalTests, results);
    }

    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=KnownDefects.Foo.Sanity", TestName = "{m}_Sanity")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", TestName = "{m}_SpaceBeforeArguments_Issue1490")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedBackslash\(""C:\\Temp""\)", TestName = "{m}_QuotedBackslash")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedPipe\(""This \| That""\)", TestName = "{m}_QuotedPipe_Issue1405")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.PipeOutside_A\|B", TestName = "{m}_PipeOutsideArguments")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.AmpersandOutside_A\&B", TestName = "{m}_AmpersandOutsideArguments")]
    public void VsTestSelectsTheTest(string filter)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));
        Verify(1, 1, results);
    }

    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=KnownDefects.Foo.Sanity", TestName = "{m}_Sanity")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.SpaceBefore \(Case 1\)", TestName = "{m}_SpaceBeforeArguments_Issue1490")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedBackslash\(""C:\\Temp""\)", TestName = "{m}_QuotedBackslash")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.QuotedPipe\(""This \| That""\)", TestName = "{m}_QuotedPipe_Issue1405")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.PipeOutside_A\|B", TestName = "{m}_PipeOutsideArguments")]
    [TestCase(@"FullyQualifiedName=KnownDefects.Foo.AmpersandOutside_A\&B", TestName = "{m}_AmpersandOutsideArguments")]
    public void DotNetTestSelectsTheTest(string filter)
    {
        var workspace = Build();
        var results = workspace.DotNetTest(filter, true, true, TestContext.WriteLine);
        Verify(1, 1, results);
    }

    /// <summary>
    /// Issue 1501. The adapter's tokenizer never terminates on an argument list with no
    /// closing parenthesis, so the test host allocates until it runs out of memory.
    ///
    /// Explicit on purpose: the failure mode is memory exhaustion rather than a normal
    /// assertion failure, and although the damage is confined to the child process it is
    /// still not something to run unattended on a build agent. Run it deliberately while
    /// working on the fix, and remove the attribute once the loop terminates at end of input.
    /// </summary>
    [Test, Platform("Win")]
    [Explicit("Exhausts memory in the child test host until the unbalanced-parenthesis loop is bounded.")]
    public void VsTestSelectsTheTestWithUnbalancedParenthesis()
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;
        var results = workspace.VSTest(
            $@"bin\Debug\{Framework}\Test.dll",
            new VsTestTestCaseFilter(@"FullyQualifiedName=KnownDefects.Foo.NoClose\("));
        Verify(1, 1, results);
    }
}
