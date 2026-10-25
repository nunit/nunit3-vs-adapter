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

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

/// <summary>
/// Test names from the Issue1502 and Issue1349 repros in nunit3-vs-adapter.issues, as NUnit
/// renders them. Each filter is built with the VSTest escaping, the same as Test Explorer and
/// dotnet test use, and must select exactly that name in both filter parsers.
/// </summary>
public class ReproTestNameFilterTests
{
    private static readonly string[] Names =
    [
        """Issue1502.FilterEscapingTests.StringArgument("a & b")""",
        """Issue1502.FilterEscapingTests.StringArgument("a &amp; b")""",
        """Issue1502.FilterEscapingTests.StringArgument("say \"hi\"")""",
        """Issue1502.FilterEscapingTests.StringArgument("C:\\Temp")""",
        """Issue1502.FilterEscapingTests.StringArgument("a\nb")""",
        """Issue1502.FilterEscapingTests.StringArgument("x(y")""",
        """Issue1502.FilterEscapingTests.StringArgument("x)y")""",
        """Issue1502.FilterEscapingTests.StringArgument("a | b")""",
        """Issue1502.FilterEscapingTests.StringArgument("a = b")""",
        """Issue1502.FilterEscapingTests.StringArgument("<tag>")""",
        """UnitTest1.Test("\"C:\\Path\\File.txt\"")""",
        """UnitTest1.Test("C:\\Path\\File.txt\"")""",
        """ValuesTests.Test1("|")""",
        """ValuesTests.Test2("\\")""",
    ];

    // "Name with & and &amp;" from Issue1502 is only in the fast path test: TestFilterParser
    // does not support spaces outside an argument list (issue 876).
    private static readonly string[] FastPathOnlyNames =
    [
        "Issue1502.FilterEscapingTests.Name with & and &amp;",
    ];

    private static string[] AllNames => [.. Names, .. FastPathOnlyNames];

    [TestCaseSource(nameof(Names))]
    public void TestFilterParserSelectsTheName(string fullName)
    {
        string filter = "FullyQualifiedName=" + FilterHelper.Escape(fullName);

        Assert.That(new TestFilterParser().Parse(filter), Is.EqualTo($"<filter><test>{XmlEscape(fullName)}</test></filter>"));
    }

    [TestCaseSource(nameof(AllNames))]
    public void FastPathSelectsTheName(string fullName)
    {
        string filter = $"(FullyQualifiedName={FilterHelper.Escape(fullName)})";

        Assert.That(FullyQualifiedNameFilterParser.GetFullyQualifiedNames(filter), Is.EqualTo(new[] { fullName }));
    }

    private static string XmlEscape(string text) => text
        .Replace("&", "&amp;")
        .Replace("\"", "&quot;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;")
        .Replace("'", "&apos;");
}
