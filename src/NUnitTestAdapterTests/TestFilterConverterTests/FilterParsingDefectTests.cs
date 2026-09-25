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
/// Tests that need the v7 grammar are ignored with <c>FixIn.V7Reason</c>, so an ordinary run is
/// red on the 6.x work only.
/// </summary>
[Category("FilterParsing")]
public class FilterParsingDefectTests
{
    /// <summary>
    /// The lexer splits on the operator characters without consulting a preceding backslash,
    /// so an escaped operator ends a token instead of being part of it. The value should stay
    /// in one token, still escaped — unescaping is the parser's job and happens later.
    ///
    /// Root cause: <c>Tokenizer.WORD_BREAK_CHARS</c> and <c>Tokenizer.IsWordChar</c>.
    /// Issues 1488 and 1405; the grammar change is a v7 item.
    ///
    /// The escaped <em>open</em> parenthesis belongs in this list too. It is in
    /// <see cref="UnbalancedParenthesisDefectTests.EscapedOpenParenStaysInsideOneToken"/> instead,
    /// because before 6.x item 1 the lexer treats that '(' as a group opener, finds no ')', and
    /// allocates until it runs out of memory. Move it back here once the loop is bounded.
    /// </summary>
    [Property("Issue", "1488")]
    [Property("DocItem", "v7 grammar")]
    [Category(FixIn.V7)]
    [Ignore(FixIn.V7Reason)]
    [TestCase(@"a\|b", TestName = "{m}_EscapedPipe")]
    [TestCase(@"a\&b", TestName = "{m}_EscapedAmpersand")]
    [TestCase(@"a\=b", TestName = "{m}_EscapedEquals")]
    [TestCase(@"a\!b", TestName = "{m}_EscapedBang")]
    [TestCase(@"a\~b", TestName = "{m}_EscapedTilde")]
    [TestCase(@"a\)b", TestName = "{m}_EscapedCloseParen")]
    public void EscapedOperatorStaysInsideOneToken(string escapedValue)
    {
        var tokenizer = new Tokenizer(escapedValue);
        var token = tokenizer.NextToken();

        Assert.Multiple(() =>
        {
            Assert.That(token.Text, Is.EqualTo(escapedValue),
                "An escaped operator must not break the value into several tokens.");
            Assert.That(tokenizer.NextToken().Kind, Is.EqualTo(TokenKind.Eof),
                "The whole value should have been consumed by the first token.");
        });
    }

    /// <summary>
    /// An unescaped operator is a real operator, wherever it appears. This is the other half
    /// of the rule above and already holds today — it is here so a fix cannot regress it.
    /// </summary>
    [Property("DocItem", "v7 grammar")]
    [Test]
    public void UnescapedOperatorStillBreaksTheToken()
    {
        var tokenizer = new Tokenizer("a|b");

        Assert.Multiple(() =>
        {
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "a")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Symbol, "|")));
            Assert.That(tokenizer.NextToken(), Is.EqualTo(new Token(TokenKind.Word, "b")));
        });
    }

    /// <summary>
    /// Whitespace inside a value is not significant to the test platform, which only trims at
    /// token boundaries. The adapter ends a word at the first space, and then only treats a
    /// following '(' as an argument list when it is immediately adjacent, so a name with a
    /// space before its argument list tokenizes as two tokens and the parse fails.
    ///
    /// Root cause: <c>Tokenizer.IsWordChar</c> rejecting whitespace, and the adjacency test in
    /// <c>Tokenizer.GetWordOrFqn</c>. Issue 1490, deferred to v7 rather than patched.
    /// </summary>
    [Property("Issue", "1490")]
    [Property("DocItem", "v7 grammar")]
    [Category(FixIn.V7)]
    [Ignore(FixIn.V7Reason)]
    [Test]
    public void WhitespaceBeforeArgumentListDoesNotEndTheValue()
    {
        const string value = @"My.Test.Fixture.Method \(Case 1\)";

        var tokenizer = new Tokenizer(value);
        var token = tokenizer.NextToken();

        Assert.Multiple(() =>
        {
            Assert.That(token.Text, Is.EqualTo(value));
            Assert.That(tokenizer.NextToken().Kind, Is.EqualTo(TokenKind.Eof));
        });
    }

    /// <summary>
    /// <c>Tokenizer.CollectQuotedString</c> consumes a backslash at lex time, and since the
    /// change in PR 1489 <c>TestFilterParser.UnEscape</c> unescapes the finished token again.
    /// Escape sequences inside a quoted argument are therefore processed twice, so one level
    /// of escaping is lost.
    ///
    /// The token here should still carry the escaping exactly as it arrived.
    /// Item 2 in the 6.x document.
    /// </summary>
    [Property("Issue", "1489")]
    [Property("DocItem", "6.x item 2")]
    [Category(FixIn.SixX)]
    [TestCase(@"(""C:\\Temp"")", TestName = "{m}_EscapedBackslash")]
    [TestCase(@"(""a\|b"")", TestName = "{m}_EscapedPipe")]
    [TestCase(@"(""a\=b"")", TestName = "{m}_EscapedEquals")]
    public void QuotedArgumentIsNotUnescapedByTheLexer(string escapedArguments)
    {
        const string name = "My.Test.Fixture.Method";

        var token = new Tokenizer(name + escapedArguments).NextToken();

        Assert.That(token.Text, Is.EqualTo(name + escapedArguments),
            "The lexer must not interpret escape sequences; that is the parser's job.");
    }

    /// <summary>
    /// A backslash that survives to <c>FilterHelper.Unescape</c> in a position the platform
    /// considers invalid raises <see cref="ArgumentException"/>, which currently travels all
    /// the way out of the adapter and is reported as an executor crash rather than as a
    /// problem with the filter.
    ///
    /// Whatever the eventual parse, a malformed filter should surface as
    /// <see cref="TestFilterParserException"/>. Item 3 in the 6.x document.
    /// </summary>
    [Property("Issue", "1489")]
    [Property("DocItem", "6.x item 3")]
    [Category(FixIn.SixX)]
    [TestCase(@"FullyQualifiedName=A.B\|C")]
    [TestCase(@"FullyQualifiedName=A.B\&C")]
    [TestCase(@"FullyQualifiedName=A.B.C(1)\)tail")]
    public void MalformedFilterIsReportedAsAFilterError(string filter)
    {
        // Written out rather than expressed with Throws.Nothing.Or.TypeOf<T>(): after .Or the
        // constraint applies to the delegate's return value, not to the exception, so that form
        // fails even when the right exception is thrown.
        //
        // "Null or TestFilterParserException" is deliberately the contract, and it is the one
        // assertion that holds across both releases. Today these filters raise a raw
        // ArgumentException and this fails. After 6.x item 3 wraps it, they raise
        // TestFilterParserException and this passes. After the v7 grammar lands they parse
        // cleanly and nothing is thrown, which is the desired end state, not a regression.
        //
        // It does not verify that the resulting filter is *correct* when nothing is thrown —
        // that is asserted by FilterRoundTripConformanceTests.AdapterProducesTheSameSelection
        // for the same inputs, which is where a wrong-but-silent parse is caught.
        Exception caught = null;

        try
        {
            new TestFilterParser().Parse(filter);
        }
        catch (Exception e)
        {
            caught = e;
        }

        Assert.That(caught, Is.Null.Or.TypeOf<TestFilterParserException>(),
            "A filter problem must not escape the adapter as a raw ArgumentException.");
    }

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
    [TestCase("My.Test.Fixture.Method", TestName = "{m}_Plain")]
    [TestCase("My.Test.Fixture.Method(42)", TestName = "{m}_SimpleArgument")]
    [TestCase(@"My.Test.Fixture.Method(""a\nb"")", TestName = "{m}_LiteralBackslashN")]
    [TestCase(@"My.Test.Fixture.Method(""a\tb"")", TestName = "{m}_LiteralBackslashT")]
    [TestCase(@"My.Test.Fixture.Method(""C:\Temp"")", TestName = "{m}_LiteralBackslash")]
    [TestCase(@"My.Test.Fixture.Method(\d)", TestName = "{m}_LiteralRegexEscape")]
    [TestCase("My.Test.Fixture.Method(a &amp; b)", TestName = "{m}_LiteralHtmlEntity")]
    [TestCase(@"My.Test.Fixture.Method(a | b)", TestName = "{m}_Pipe")]
    public void MtpFastPathUnescapeRoundTrips(string fullName)
    {
        var escaped = Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.FilterHelper.Escape(fullName);

        Assert.That(() => FullyQualifiedNameFilterParser.Unescape(escaped), Is.EqualTo(fullName),
            $"Escaping and unescaping '{fullName}' must be lossless.");
    }

    /// <summary>
    /// End to end through the MTP fast path, which is the entry point the IDE uses under the
    /// Microsoft Testing Platform. Item 4 in the 6.x document.
    /// </summary>
    [Property("DocItem", "6.x item 4")]
    [Category(FixIn.SixX)]
    [TestCase("My.Test.Fixture.Method(42)", TestName = "{m}_SimpleArgument")]
    [TestCase("My.Test.Fixture.Method(a | b)", TestName = "{m}_Pipe")]
    [TestCase(@"My.Test.Fixture.Method(""C:\Temp"")", TestName = "{m}_LiteralBackslash")]
    [TestCase("My.Test.Fixture.Method (Case 1)", TestName = "{m}_SpaceBeforeArguments")]
    public void MtpFastPathRecoversTheFullName(string fullName)
    {
        var escaped = Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.FilterHelper.Escape(fullName);
        var filter = "FullyQualifiedName=" + escaped;

        Assert.That(
            () => FullyQualifiedNameFilterParser.GetFullyQualifiedNames(filter),
            Is.EqualTo(new List<string> { fullName }));
    }
}

/// <summary>
/// The unbalanced-parenthesis defect, issue 1501, item 1 in the 6.x document.
///
/// <c>Tokenizer.CollectBalancedParentheticalExpression</c> loops <c>while (depth &gt; 0)</c>
/// and never tests for end of input, so <c>GetChar</c> returns the EOF sentinel forever while
/// the <see cref="System.Text.StringBuilder"/> keeps growing until the process runs out of
/// memory.
///
/// These are 6.x tests, not v7 ones. They are ignored only because the defect is destructive
/// rather than merely failing: measured at roughly twelve seconds and two gigabytes per test,
/// ending in <see cref="System.OutOfMemoryException"/>. The runaway loop cannot be cancelled from
/// outside, so there is no way to run them safely on a build agent beforehand.
///
/// Bound the loop at end of input and remove the <see cref="IgnoreAttribute">Ignore</see> in the
/// same change, so that item 1 has a test that goes from red to green with its fix.
/// </summary>
[Category("FilterParsing")]
[Property("Issue", "1501")]
[Property("DocItem", "6.x item 1")]
[Category(FixIn.SixX)]
[Ignore(FixIn.Item1Reason)]
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

        Assert.Multiple(() =>
        {
            Assert.That(token.Text, Is.EqualTo(escapedValue),
                "An escaped operator must not break the value into several tokens.");
            Assert.That(tokenizer.NextToken().Kind, Is.EqualTo(TokenKind.Eof),
                "The whole value should have been consumed by the first token.");
        });
    }
}
