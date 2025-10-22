// ***********************************************************************
// Copyright (c) 2024 Charlie Poole, Terje Sandstrom
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

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

public class FullyQualifiedNameFilterParserTests
{
    [Test]
    public void Should_extract_fully_qualified_names_from_testhost_filter()
    {
        const string filter = """(FullyQualifiedName=Issue1332.MyTest.TestMethod\(" \"Code block\", @ \"Test\"    app",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("'Code block' @ \"Test\" \" app",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"   \"",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"\"",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"Code block\" @ \"Test\" \" app",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"Code block\" \"This is a test\" ",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"Code\" \" block\" ",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"Code\" \"block\"",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("\"Code\"\"block\"",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("Code @ Test",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("Code @",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("Code",System.Collections.Generic.List`1[System.String]\)|FullyQualifiedName=Issue1332.MyTest.TestMethod\("null",System.Collections.Generic.List`1[System.String]\))""";

        var expected = new[]
        {
            """Issue1332.MyTest.TestMethod\(" \"Code block\", @ \"Test\"    app",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("'Code block' @ \"Test\" \" app",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"   \"",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"\"",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"Code block\" @ \"Test\" \" app",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"Code block\" \"This is a test\" ",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"Code\" \" block\" ",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"Code\" \"block\"",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("\"Code\"\"block\"",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("Code @ Test",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("Code @",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("Code",System.Collections.Generic.List`1[System.String]\)""",
            """Issue1332.MyTest.TestMethod\("null",System.Collections.Generic.List`1[System.String]\)"""
        };

        var result = FullyQualifiedNameFilterParser.GetFullyQualifiedNames(filter);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Should_return_empty_collection_when_filter_is_missing(string? filter)
    {
        var result = FullyQualifiedNameFilterParser.GetFullyQualifiedNames(filter);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Should_skip_segments_that_do_not_start_with_prefix()
    {
        const string filter = "(Name=Something|FullyQualifiedName=Issue1332.MyTest.TestMethod)";

        var result = FullyQualifiedNameFilterParser.GetFullyQualifiedNames(filter);

        Assert.That(result, Is.EqualTo(new[] { "Issue1332.MyTest.TestMethod" }));
    }
}
