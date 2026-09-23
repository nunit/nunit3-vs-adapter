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

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

/// <summary>
/// Categories marking which release is expected to make a currently failing filter-parsing
/// test pass. See <c>docs/TestFilterParsing-6.x.md</c> and <c>docs/TestFilterParsing-v7.md</c>.
///
/// The split is not a guess. Each of the 6.x fixes was prototyped against this suite and the
/// tests that went green were recorded; <see cref="SixX"/> marks exactly those, and
/// <see cref="V7"/> marks the ones that stayed red because they need the grammar change.
///
/// While working on 6.x, run
/// <c>--filter "Category=FilterParsing &amp; Category!=FixV7"</c>
/// and drive it to green. A test carrying neither category passes today and is a regression
/// guard.
/// </summary>
public static class FixIn
{
    /// <summary>Fixed by one of the non-breaking 6.x items.</summary>
    public const string SixX = "Fix6x";

    /// <summary>Needs the v7 grammar change; expected to stay red through 6.x.</summary>
    public const string V7 = "FixV7";
}
