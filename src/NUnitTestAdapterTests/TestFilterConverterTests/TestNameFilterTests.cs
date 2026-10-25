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

using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

/// <summary>
/// The filter for a list of test names, as the Test Explorer paths build it. NUnit writes a
/// character it does not allow in its XML as the text \uXXXX, so that is the name the test
/// platform reports; the framework's FullName keeps the character (nunit/nunit#4826).
/// </summary>
public class TestNameFilterTests
{
    [Test]
    public void OrdinaryNamesGiveTheSameFilterAsTheEngine()
    {
        Assert.That(
            TestNameFilter.ForNames(["A.B(\"x&y\")", "A.C"]),
            Is.EqualTo("<filter><or><test>A.B(&quot;x&amp;y&quot;)</test><test>A.C</test></or></filter>"));
    }

    [TestCase('\u001b')]
    [TestCase('\u0001')]
    [TestCase('\uffff')]
    [TestCase('\u0080')]
    public void ReportedNameSelectsTheTest(char character)
    {
        string frameworkName = $"A.B(\"x{character}y\")";
        string reportedName = $"A.B(\"x\\u{(int)character:x4}y\")";

        var tests = XElement.Parse(TestNameFilter.ForNames([reportedName, "A.C"])).Descendants("test").ToList();
        var regex = new Regex(tests[0].Value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tests[0].Attribute("re")?.Value, Is.EqualTo("1"));
            Assert.That(regex.IsMatch(frameworkName), Is.True, "The filter must select the test.");
            Assert.That(regex.IsMatch(reportedName), Is.True, "The filter must also match the reported name.");
            Assert.That(regex.IsMatch("A.B(\"xy\")"), Is.False, "The filter must not select the name without the character.");
            Assert.That(tests[1].ToString(), Is.EqualTo("<test>A.C</test>"), "Other names stay exact matches.");
        }
    }

    [Test]
    public void EscapedBackslashBeforeUIsNotACharacter()
    {
        // NUnit writes a real backslash as \\, so A\\u0041 is a backslash followed by u0041.
        Assert.That(TestNameFilter.NeedsRegex(@"A.B(""\\u0041"")"), Is.False);
    }
}
