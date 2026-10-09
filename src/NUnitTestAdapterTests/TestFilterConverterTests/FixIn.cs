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
/// Categories and ignore reasons for the filter-parsing tests. See
/// <c>docs/TestFilterParsing-6.x.md</c> and <c>docs/TestFilterParsing-v7.md</c>.
///
/// A test marked <see cref="SixX"/> is expected to be red until its 6.x item is implemented and
/// green afterwards. A test marked <see cref="V7"/> is ignored, because it cannot pass before the
/// grammar change and the build must not be red while 6.x work is merged.
///
/// The split is measured, not assumed: each 6.x fix was prototyped against this suite and the
/// tests that turned green were recorded.
/// </summary>
public static class FixIn
{
    /// <summary>Fixed by one of the 6.x items.</summary>
    public const string SixX = "Fix6x";

    /// <summary>Needs the v7 grammar change.</summary>
    public const string V7 = "FixV7";

    /// <summary>
    /// Reason for ignoring a test that requires the v7 grammar change.
    /// </summary>
    public const string V7Reason =
        "Requires the escape-aware grammar in docs/TestFilterParsing-v7.md, which is a breaking " +
        "change scheduled for v7. Remove this Ignore as part of that change.";

    /// <summary>
    /// Reason for ignoring the unbalanced-parenthesis tests, 6.x item 1 / issue 1501.
    ///
    /// These are 6.x tests, not v7 ones, and they are ignored only because the defect they cover
    /// is destructive rather than merely failing: the lexer never stops consuming input, so it
    /// allocates until StringBuilder reaches its 2 GB cap and throws OutOfMemoryException. That
    /// is roughly twelve seconds and two gigabytes per test, which must not run on a build agent.
    /// </summary>
    public const string Item1Reason =
        "Ignored only because the defect is destructive before it is fixed: the lexer allocates " +
        "2 GB and throws OutOfMemoryException rather than failing. Bound " +
        "CollectBalancedParentheticalExpression at end of input (6.x item 1, issue 1501) and " +
        "remove this Ignore in the same change.";
}
