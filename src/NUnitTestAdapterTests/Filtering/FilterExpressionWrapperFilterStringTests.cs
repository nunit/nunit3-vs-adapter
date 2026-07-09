// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, Terje Sandstrom
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

using System.Collections.Generic;

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Filtering;

/// <summary>
/// Regression tests for the fix in
/// <see cref="FullyQualifiedNameFilterParser.SplitOnFullyQualifiedName"/>.
///
/// The parser splits a filter into individual FullyQualifiedName clauses on the
/// '|' (OR) separator. The fix changed the value-capturing group from
/// <c>[^|]*</c> to <c>(?:\\.|[^|])*</c> so an *escaped* '|' (i.e. <c>\|</c>) that
/// appears inside a test-argument value is consumed as a literal and is no longer
/// mistaken for a clause separator. Only an unescaped '|' ends a value.
///
/// These tests exercise that method (and its <c>GetFullyQualifiedNames</c> caller)
/// directly, with no VSTest/engine/mocking scaffolding, so a failure points at the
/// split logic itself.
/// </summary>
public class FilterExpressionWrapperFilterStringTests
{
    // Two FullyQualifiedName clauses. The first value contains an escaped '\|'
    // inside its argument, which must be preserved rather than split on.
    private const string TwoClausesWithEscapedPipe =
        """FullyQualifiedName=NUnitPlayground.TestClass.PrintArg\("as\|"\)|FullyQualifiedName=NUnitPlayground.TestClass.Other""";

    [Test]
    public void SplitOnFullyQualifiedName_DoesNotSplitOnEscapedPipe()
    {
        var result = FullyQualifiedNameFilterParser.SplitOnFullyQualifiedName(TwoClausesWithEscapedPipe);

        // Only the single unescaped '|' between the two clauses is a separator.
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void SplitOnFullyQualifiedName_KeepsEscapedPipeAsLiteralInValue()
    {
        var result = FullyQualifiedNameFilterParser.SplitOnFullyQualifiedName(TwoClausesWithEscapedPipe);

        // After unescaping, the escaped '\|' becomes a literal '|' that stays
        // inside the first fully qualified name instead of truncating it.
        var expectedFirst = FullyQualifiedNameFilterParser.Unescape(
            """NUnitPlayground.TestClass.PrintArg\("as\|"\)""");

        Assert.That(result[0], Is.EqualTo(expectedFirst));
        Assert.That(result[0], Does.Contain("|"));
        Assert.That(result[1], Is.EqualTo("NUnitPlayground.TestClass.Other"));
    }

    [Test]
    public void GetFullyQualifiedNames_ExtractsBothNames_WhenValueContainsEscapedPipe()
    {
        // Same payload wrapped in the outer parentheses the test platform adds.
        var filter = $"({TwoClausesWithEscapedPipe})";

        var result = FullyQualifiedNameFilterParser.GetFullyQualifiedNames(filter);

        var expected = new List<string>
        {
            FullyQualifiedNameFilterParser.Unescape("""NUnitPlayground.TestClass.PrintArg\("as\|"\)"""),
            "NUnitPlayground.TestClass.Other"
        };

        Assert.That(result, Is.EqualTo(expected));
    }
}
