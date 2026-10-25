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
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

/// <summary>
/// Narrow demonstrations of the individual defects behind the conformance failures in
/// <see cref="FilterRoundTripConformanceTests"/>, each pinned to the method that causes it.
/// These are the tests to work against when fixing one item at a time; see
/// <c>docs/TestFilterParsing-6.x.md</c> and <c>docs/TestFilterParsing-v7.md</c>.
///
/// Run the whole filter-parsing set with <c>--filter "Category=FilterParsing"</c>, one issue
/// at a time with <c>--filter "Issue=1490"</c>, or one plan item with
/// <c>--filter "DocItem=6.x item 2"</c>.
///
/// The tests first marked as needing a breaking v7 grammar pass with the non-breaking 6.4
/// tokenizer, which reads a value up to the next unescaped operator. Each test gives a filter
/// string and asserts the NUnit filter it becomes.
/// </summary>
[Category("FilterParsing")]
public class FilterParsingDefectTests
{
    /// <summary>
    /// An escaped operator is part of the value, wherever it appears. Issues 1488 and 1405.
    /// </summary>
    [Property("Issue", "1488")]
    [Property("DocItem", "6.x item 6")]
    [Category(FixIn.SixX)]
    [TestCase(@"a\\b", @"a\b", TestName = "{m}_EscapedBackslash")]
    [TestCase(@"a\(b", "a(b", TestName = "{m}_EscapedOpenParen")]
    [TestCase(@"a\)b", "a)b", TestName = "{m}_EscapedCloseParen")]
    [TestCase(@"a\&b", "a&amp;b", TestName = "{m}_EscapedAmpersand")]
    [TestCase(@"a\|b", "a|b", TestName = "{m}_EscapedPipe")]
    [TestCase(@"a\=b", "a=b", TestName = "{m}_EscapedEquals")]
    [TestCase(@"a\!b", "a!b", TestName = "{m}_EscapedBang")]
    [TestCase(@"a\~b", "a~b", TestName = "{m}_EscapedTilde")]
    public void EscapedOperatorIsPartOfTheName(string escapedValue, string xmlName)
    {
        Assert.That(Parse("FullyQualifiedName=" + escapedValue), Is.EqualTo($"<filter><test>{xmlName}</test></filter>"));
    }

    /// <summary>
    /// An unescaped operator is a real operator, wherever it appears. This is the other half
    /// of the rule above — it is here so a fix cannot regress it.
    /// </summary>
    [Property("DocItem", "6.x item 6")]
    [Test]
    public void UnescapedOperatorSeparatesConditions()
    {
        Assert.That(
            Parse("FullyQualifiedName=a|FullyQualifiedName=b"),
            Is.EqualTo("<filter><or><test>a</test><test>b</test></or></filter>"));
    }

    /// <summary>
    /// Whitespace inside a value is not significant to the test platform, which only trims at
    /// token boundaries. The adapter used to end a value at the first space, so a name with a
    /// space before its argument list could not be selected. Issue 1490.
    /// </summary>
    [Property("Issue", "1490")]
    [Property("DocItem", "6.x item 6")]
    [Category(FixIn.SixX)]
    [TestCase(@"My.Test.Fixture.Method \(Case 1\)", TestName = "{m}_Escaped")]
    [TestCase("My.Test.Fixture.Method (Case 1)", TestName = "{m}_Unescaped")]
    public void WhitespaceBeforeArgumentListIsPartOfTheName(string value)
    {
        Assert.That(Parse("FullyQualifiedName=" + value), Is.EqualTo("<filter><test>My.Test.Fixture.Method (Case 1)</test></filter>"));
    }

    /// <summary>
    /// Escape sequences inside a quoted argument are unescaped once. Since PR 1489 the lexer
    /// and the parser both unescaped them, so one level of escaping was lost. Item 2 in the 6.x
    /// document.
    /// </summary>
    [Property("Issue", "1489")]
    [Property("DocItem", "6.x item 2")]
    [Category(FixIn.SixX)]
    [TestCase("""("C:\\Temp")""", @"(&quot;C:\Temp&quot;)", TestName = "{m}_EscapedBackslash")]
    [TestCase("""("a\|b")""", "(&quot;a|b&quot;)", TestName = "{m}_EscapedPipe")]
    [TestCase("""("a\=b")""", "(&quot;a=b&quot;)", TestName = "{m}_EscapedEquals")]
    public void QuotedArgumentIsUnescapedOnce(string escapedArguments, string xmlArguments)
    {
        Assert.That(
            Parse("FullyQualifiedName=My.Test.Fixture.Method" + escapedArguments),
            Is.EqualTo($"<filter><test>My.Test.Fixture.Method{xmlArguments}</test></filter>"));
    }

    /// <summary>
    /// An escaped operator outside an argument list used to leave a dangling backslash, which
    /// raised an <see cref="ArgumentException"/> (6.x item 3). It is part of the value now, so
    /// these filters select the test they name.
    /// </summary>
    [Property("Issue", "1489")]
    [Property("DocItem", "6.x item 3")]
    [Category(FixIn.SixX)]
    [TestCase(@"FullyQualifiedName=A.B\|C", "A.B|C")]
    [TestCase(@"FullyQualifiedName=A.B\&C", "A.B&amp;C")]
    [TestCase(@"FullyQualifiedName=A.B.C(1)\)tail", "A.B.C(1))tail")]
    public void EscapedOperatorOutsideArgumentsSelectsTheTest(string filter, string xmlName)
    {
        Assert.That(Parse(filter), Is.EqualTo($"<filter><test>{xmlName}</test></filter>"));
    }

    /// <summary>
    /// A backslash that escapes nothing is a filter error, reported as
    /// <see cref="TestFilterParserException"/>: not a raw exception, and not a wrong selection.
    /// </summary>
    [Property("DocItem", "6.x item 3")]
    [Category(FixIn.SixX)]
    [TestCase(@"FullyQualifiedName=A.B\")]
    [TestCase(@"FullyQualifiedName=A.B\xC")]
    public void BackslashThatEscapesNothingIsAFilterError(string filter)
    {
        Assert.That(() => Parse(filter), Throws.TypeOf<TestFilterParserException>());
    }

    private static string Parse(string filter) => new TestFilterParser().Parse(filter);

    /// <summary>
    /// The MTP fast path has its own unescaping, built from <c>WebUtility.HtmlDecode</c> and
    /// <c>Regex.Unescape</c>. Neither implements the platform's escaping scheme, so escaping a
    /// name and unescaping it again is not guaranteed to be lossless.
    ///
    /// Most shapes do survive, because <c>FilterHelper.Escape</c> doubles a backslash and
    /// <c>Regex.Unescape</c> then collapses the pair correctly. The HTML entity is the case
    /// that does not.
    ///
    /// Root cause: <c>FullyQualifiedNameFilterParser.Unescape</c>. Item 4 in the 6.x document.
    /// </summary>
    [Property("DocItem", "6.x item 4")]
    [Category(FixIn.SixX)]
    [TestCase("""My.Test.Fixture.Method""", TestName = "{m}_Plain")]
    [TestCase("""My.Test.Fixture.Method(42)""", TestName = "{m}_SimpleArgument")]
    [TestCase("""My.Test.Fixture.Method("a\nb")""", TestName = "{m}_LiteralBackslashN")]
    [TestCase("""My.Test.Fixture.Method("a\tb")""", TestName = "{m}_LiteralBackslashT")]
    [TestCase("""My.Test.Fixture.Method("C:\Temp")""", TestName = "{m}_LiteralBackslash")]
    [TestCase("""My.Test.Fixture.Method(\d)""", TestName = "{m}_LiteralRegexEscape")]
    [TestCase("""My.Test.Fixture.Method(a &amp; b)""", TestName = "{m}_LiteralHtmlEntity")]
    [TestCase("""My.Test.Fixture.Method(a | b)""", TestName = "{m}_Pipe")]
    public void MtpFastPathUnescapeRoundTrips(string fullName)
    {
        string escaped = Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.FilterHelper.Escape(fullName);

        Assert.That(FullyQualifiedNameFilterParser.Unescape(escaped), Is.EqualTo(fullName),
            $"Escaping and unescaping '{fullName}' must be lossless.");
    }

    /// <summary>
    /// End to end through the MTP fast path, which is the entry point the IDE uses under the
    /// Microsoft Testing Platform. Item 4 in the 6.x document.
    /// </summary>
    [Property("DocItem", "6.x item 4")]
    [Category(FixIn.SixX)]
    [TestCase("""My.Test.Fixture.Method(42)""", TestName = "{m}_SimpleArgument")]
    [TestCase("""My.Test.Fixture.Method(a | b)""", TestName = "{m}_Pipe")]
    [TestCase("""My.Test.Fixture.Method("C:\Temp")""", TestName = "{m}_LiteralBackslash")]
    [TestCase("""My.Test.Fixture.Method (Case 1)""", TestName = "{m}_SpaceBeforeArguments")]
    public void MtpFastPathRecoversTheFullName(string fullName)
    {
        string escaped = Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.FilterHelper.Escape(fullName);
        string filter = $"FullyQualifiedName={escaped}";

        Assert.That(FullyQualifiedNameFilterParser.GetFullyQualifiedNames(filter), Is.EqualTo(new List<string> { fullName }));
    }
}

/// <summary>
/// The unbalanced-parenthesis defect, issue 1501, item 1 in the 6.x document.
///
/// <c>Tokenizer.CollectBalancedParentheticalExpression</c> used to loop <c>while (depth &gt; 0)</c>
/// without testing for end of input, so <c>GetChar</c> returned the EOF sentinel forever while
/// the <see cref="System.Text.StringBuilder"/> kept growing until the process ran out of
/// memory. The loop now stops at end of input.
/// </summary>
[Category("FilterParsing")]
[Property("Issue", "1501")]
[Property("DocItem", "6.x item 1")]
[Category(FixIn.SixX)]
public class UnbalancedParenthesisDefectTests
{
    [TestCase(@"FullyQualifiedName=My.Test.Fixture.Method\(", TestName = "{m}_Filter")]
    [TestCase(@"My.Test.Fixture.Method\(", TestName = "{m}_Value")]
    public void UnbalancedOpenParenthesisTerminates(string input)
    {
        var completed = new ManualResetEventSlim(false);

        var thread = new Thread(() =>
        {
            try
            {
                var tokenizer = new Tokenizer(input);
                while (tokenizer.NextToken().Kind != TokenKind.Eof)
                {
                }
            }
            catch
            {
                // A thrown exception is still termination; the assertion below only cares
                // that the loop ended.
            }

            completed.Set();
        })
        {
            IsBackground = true
        };

        thread.Start();

        Assert.That(completed.Wait(TimeSpan.FromSeconds(5)), Is.True,
            "Tokenizing an unbalanced '(' must terminate at end of input.");
    }

    /// <summary>
    /// An escaped open parenthesis is a value character, not a group opener, so this belongs with
    /// the other escaped operators in <see cref="FilterParsingDefectTests"/>.
    ///
    /// It is here instead because the current lexer does treat it as a group opener, finds no
    /// closing parenthesis, and allocates until <see cref="System.Text.StringBuilder"/> reaches
    /// its 2 GB cap — about twelve seconds and two gigabytes per run. That is the same defect as
    /// issue 1501 reached from a different input, and it is destructive rather than merely
    /// failing, so it must not sit in the ordinary suite.
    ///
    /// Once item 1 bounds the loop, move this case back to
    /// <see cref="FilterParsingDefectTests.EscapedOperatorStaysInsideOneToken"/>.
    /// </summary>
    [Test]
    public void EscapedOpenParenStaysInsideOneToken()
    {
        const string escapedValue = @"a\(b";

        var tokenizer = new Tokenizer(escapedValue);
        var token = tokenizer.NextToken();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(token.Text, Is.EqualTo(escapedValue),
                "An escaped operator must not break the value into several tokens.");

            Assert.That(tokenizer.NextToken().Kind, Is.EqualTo(TokenKind.Eof),
                "The whole value should have been consumed by the first token.");
        }
    }
}
