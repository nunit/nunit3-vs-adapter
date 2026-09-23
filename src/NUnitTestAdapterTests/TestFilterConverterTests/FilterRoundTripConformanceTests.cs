// ***********************************************************************
// Copyright (c) 2026 Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Linq;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;
using NUnit.VisualStudio.TestAdapter.Tests.Filtering;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

/// <summary>
/// Conformance tests for the invariant stated in <c>docs/TestFilterParsing-v7.md</c>:
///
///     a filter that the test platform accepts at discovery must select the same test
///     when the adapter converts it to an NUnit filter at execution.
///
/// Rather than hand-writing the expected XML, every case here derives it from the test
/// platform itself. For a given NUnit full name:
///
///   1. the filter is built with <c>FilterHelper.Escape</c>, which is the documented way
///      to escape a value (see the character-escaping rules for selective unit tests);
///   2. <see cref="VStestAgreesTheFilterSelectsTheTest"/> asserts the platform's own filter
///      implementation selects the test, which proves the filter is well formed and that
///      the expectation below is not simply wrong;
///   3. <see cref="AdapterProducesTheSameSelection"/> asserts the adapter's parser emits a
///      <c>&lt;test&gt;</c> element holding exactly that full name.
///
/// Step 2 is expected to pass for every case. Step 3 is the specification: the cases it
/// fails on are the defects, and they are the ones to fix.
///
/// The unbalanced-parenthesis case is deliberately absent, because it does not fail here —
/// it exhausts memory and takes the test host with it. See
/// <see cref="FilterParsingDefectTests"/> for the bounded version.
/// </summary>
[Category("FilterParsing")]
public class FilterRoundTripConformanceTests
{
    private const string Source = "dummy.dll";
    private static readonly Uri ExecutorUri = new("executor://nunit3testexecutor/");

    /// <summary>
    /// Full names as NUnit renders them into <c>TestCase.FullyQualifiedName</c>. Each is a
    /// shape that is either already reported as broken or is a near neighbour of one.
    /// </summary>
    public static readonly TestCaseData[] FullNames =
    [
        // Baselines. These already work and must keep working.
        new TestCaseData("My.Test.Fixture.Method").SetName("{m}_Plain"),
        new TestCaseData("My.Test.Fixture.Method(42)").SetName("{m}_SimpleArgument"),
        new TestCaseData("My.Test.Fixture+Nested.Method(1,2,3)").SetName("{m}_NestedFixture"),
        new TestCaseData("My.Test.Fixture(99).Method(42)").SetName("{m}_FixtureAndMethodArguments"),

        // Filter operators appearing literally in the name, inside an argument list. These
        // were fixed by PR 1489 and are here as a regression guard.
        new TestCaseData("My.Test.Fixture.Method(Case 1: X = Y)").SetName("{m}_Equals").SetProperty("Issue", "1488"),
        new TestCaseData("My.Test.Fixture.Method(a & b)").SetName("{m}_Ampersand").SetProperty("Issue", "1488"),
        new TestCaseData("My.Test.Fixture.Method(a | b)").SetName("{m}_Pipe").SetProperty("Issue", "1405"),
        new TestCaseData("My.Test.Fixture.Method(a ! b)").SetName("{m}_Bang").SetProperty("Issue", "1488"),
        new TestCaseData("My.Test.Fixture.Method(a ~ b)").SetName("{m}_Tilde").SetProperty("Issue", "1488"),
        new TestCaseData("My.Test.Fixture.Method(a != b)").SetName("{m}_NotEquals").SetProperty("Issue", "1488"),

        // The same operators with no argument list to re-glue the token. The
        // parenthesis-balancing heuristic cannot help here, so these still fail.
        new TestCaseData("My.Test.Fixture.MethodWithEquals_X=Y").SetName("{m}_EqualsOutsideArguments").SetProperty("Issue", "1488"),
        new TestCaseData("My.Test.Fixture.MethodWithPipe_A|B").SetName("{m}_PipeOutsideArguments").SetProperty("Issue", "1488"),
        new TestCaseData("My.Test.Fixture.MethodWithAmpersand_A&B").SetName("{m}_AmpersandOutsideArguments").SetProperty("Issue", "1488"),

        // Whitespace between the name and the argument list.
        new TestCaseData("My.Test.Fixture.Method (Case 1)").SetName("{m}_SpaceBeforeArguments").SetProperty("Issue", "1490"),

        // String arguments, which NUnit renders in quotes. The tokenizer has a second,
        // independent unescaping pass for these, so escaping is applied twice.
        new TestCaseData("My.Test.Fixture.Method(\"plain\")").SetName("{m}_QuotedArgument"),
        new TestCaseData("My.Test.Fixture.Method(\"This | That\",False)").SetName("{m}_QuotedPipe").SetProperty("Issue", "1405"),
        new TestCaseData("My.Test.Fixture.Method(\"C:\\\\Temp\")").SetName("{m}_QuotedBackslash").SetProperty("Issue", "1489"),
        new TestCaseData("My.Test.Fixture.Method(\"a\\\"b\")").SetName("{m}_QuotedEscapedQuote").SetProperty("Issue", "1489"),
        new TestCaseData("My.Test.Fixture.Method(\"a(b\")").SetName("{m}_QuotedOpenParen"),
        new TestCaseData("My.Test.Fixture.Method(\"a)b\")").SetName("{m}_QuotedCloseParen"),

        // A backslash outside a quoted argument.
        new TestCaseData("My.Test.Fixture.Method(C:\\Temp)").SetName("{m}_UnquotedBackslash"),

        // Spaces in the name itself, with no argument list at all. The whitespace defect is
        // not limited to the gap before '('.
        new TestCaseData("My.Test.Fixture.Computing work in progress.No Workpackages exist")
            .SetName("{m}_SpacesInName").SetProperty("Issue", "876"),
        new TestCaseData("My.Test.Fixture.AddTwoNumbers(\"Spa ces\",null)")
            .SetName("{m}_SpaceInQuotedArgument").SetProperty("Issue", "807"),

        // A close parenthesis followed by a period inside a string argument, which looks to
        // the lexer like the end of an argument list followed by more name.
        new TestCaseData("My.Test.Fixture.Method(\"I am a good test case (the best, even).\")")
            .SetName("{m}_CloseParenDotInsideArgument").SetProperty("Issue", "1097"),
        new TestCaseData("My.Test.Fixture.NUnitTestTwo(\"TestAttribute).\")")
            .SetName("{m}_CloseParenDotAtEndOfArgument").SetProperty("Issue", "654"),

        // A string argument that itself ends with an escaped quote, the shape the MTP bridge
        // reports as "includes unrecognized escape sequence".
        new TestCaseData("My.Test.Fixture.Test(\"\\\"C:\\\\Path\\\\File.txt\\\"\")")
            .SetName("{m}_QuotedPathWithTrailingQuote").SetProperty("Issue", "1349"),

        // Collection arguments rendered with brackets.
        new TestCaseData("My.Test.Fixture.Slice_IsCorrect([0,1,2,3],3,3,[3])")
            .SetName("{m}_BracketArguments").SetProperty("Issue", "1437"),

        // A non-character code point in an argument. This one is not about tokenizing at all:
        // U+FFFF is not a legal XML character, and the NUnit filter is an XML document.
        new TestCaseData("My.Test.Fixture.Test_01(\"\uffff\")")
            .SetName("{m}_NonCharacterArgument").SetProperty("Issue", "761"),
    ];

    /// <summary>
    /// Sanity check. Every filter in the corpus must be one the test platform itself accepts
    /// and matches, otherwise the expectation in the next test would be meaningless.
    /// This test is expected to pass for every case.
    /// </summary>
    [TestCaseSource(nameof(FullNames))]
    public void VStestAgreesTheFilterSelectsTheTest(string fullName)
    {
        var filter = BuildFilter(fullName);
        var testCase = new TestCase(fullName, ExecutorUri, Source);

        var expression = FilteringTestUtils.CreateVSTestFilterExpression(filter);
        var selected = FilteringTestUtils.CreateTestFilter(expression).CheckFilter([testCase]);

        Assert.That(selected.Select(t => t.FullyQualifiedName), Is.EqualTo(new[] { fullName }),
            $"The test platform did not select the test for filter '{filter}'. " +
            "Fix the corpus entry, not the adapter.");
    }

    /// <summary>
    /// The specification. The adapter must turn the same filter into an NUnit filter that
    /// names exactly the same test.
    /// </summary>
    [TestCaseSource(nameof(FullNames))]
    public void AdapterProducesTheSameSelection(string fullName)
    {
        var filter = BuildFilter(fullName);
        var expected = $"<filter><test>{XmlEscape(fullName)}</test></filter>";

        Assert.That(() => new TestFilterParser().Parse(filter), Is.EqualTo(expected),
            $"Filter '{filter}' should select '{fullName}'.");
    }

    /// <summary>
    /// The NUnit filter is an XML document, so whatever the parser emits has to be loadable
    /// as one. Escaping the five XML metacharacters is not sufficient on its own: a name can
    /// contain a code point that is not a legal XML character at all, and no amount of entity
    /// escaping makes such a document parse.
    /// </summary>
    [TestCaseSource(nameof(FullNames))]
    public void AdapterProducesLoadableXml(string fullName)
    {
        var filter = BuildFilter(fullName);

        string produced = null;
        Assert.That(() => produced = new TestFilterParser().Parse(filter), Throws.Nothing,
            $"Filter '{filter}' should parse.");

        Assert.That(() => new XmlDocument().LoadXml(produced), Throws.Nothing,
            $"The filter emitted for '{fullName}' is not well-formed XML: {produced}");
    }

    /// <summary>
    /// The same invariant for the <c>Name</c> property, which takes the other code path
    /// through <c>EmitNameFilter</c>.
    /// </summary>
    [TestCase("Method")]
    [TestCase("Method(42)")]
    [TestCase("Method(a | b)")]
    [TestCase("Method (Case 1)")]   // Issue 1490
    [TestCase("Method_A|B")]
    public void NameFilterProducesTheSameSelection(string name)
    {
        var filter = "Name=" + Escape(name);
        var expected = $"<filter><name>{XmlEscape(name)}</name></filter>";

        Assert.That(() => new TestFilterParser().Parse(filter), Is.EqualTo(expected),
            $"Filter '{filter}' should select the test named '{name}'.");
    }

    /// <summary>
    /// Category values are never unescaped at all — <c>ParseFilterCondition</c> takes the
    /// raw <c>Word</c> token for <c>TestCategory</c> and passes it straight to
    /// <c>EmitCategoryFilter</c> — so a category containing a filter operator cannot be
    /// selected even though the platform escapes and matches it happily.
    /// </summary>
    [TestCase("Urgent")]
    [TestCase("Group(1)")]
    [TestCase("A|B")]
    [TestCase("A&B")]
    [TestCase("Needs=Network")]
    public void CategoryFilterProducesTheSameSelection(string category)
    {
        var filter = "TestCategory=" + Escape(category);
        var expected = $"<filter><cat>{XmlEscape(category)}</cat></filter>";

        Assert.That(() => new TestFilterParser().Parse(filter), Is.EqualTo(expected),
            $"Filter '{filter}' should select category '{category}'.");
    }

    /// <summary>
    /// The same for an arbitrary property name, which lands in the <c>default</c> branch of
    /// <c>ParseFilterCondition</c> and is likewise never unescaped.
    /// </summary>
    [TestCase("Bug", "12345")]
    [TestCase("Bug", "JIRA-1|JIRA-2")]
    [TestCase("Owner", "a&b")]
    public void PropertyFilterProducesTheSameSelection(string name, string value)
    {
        var filter = $"{name}=" + Escape(value);
        var expected = $"<filter><prop name='{name}'>{XmlEscape(value)}</prop></filter>";

        Assert.That(() => new TestFilterParser().Parse(filter), Is.EqualTo(expected),
            $"Filter '{filter}' should select property {name}='{value}'.");
    }

    private static string BuildFilter(string fullName) => "FullyQualifiedName=" + Escape(fullName);

    private static string Escape(string value)
        => Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.FilterHelper.Escape(value);

    /// <summary>
    /// Mirrors the private <c>TestFilterParser.XmlEscape</c>, since the NUnit filter is XML.
    /// </summary>
    private static string XmlEscape(string text)
        => text
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;");
}
