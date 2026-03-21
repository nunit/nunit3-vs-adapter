// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, Terje Sandstrom
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

namespace CakeScripts.Tests;

public class VersionParsersTests
{
    [TestCase("1.0.0", ExpectedResult = "1.0.0.0")]
    [TestCase("2.3.4", ExpectedResult = "2.3.4.0")]
    [TestCase("1.0.0-alpha", ExpectedResult = "1.0.0.0")]
    [TestCase("2.3.4-beta.1", ExpectedResult = "2.3.4.0")]
    [TestCase("6.2.0-alpha.0.17", ExpectedResult = "6.2.0.0")]
    [TestCase("1.2.3-rc.1.5", ExpectedResult = "1.2.3.0")]
    public string ParseAssemblyVersion_ReturnsCorrectVersion(string version)
    {
        return VersionParsers.ParseAssemblyVersion(version);
    }

    [Test]
    public void ParseAssemblyVersion_WithoutPreRelease_AppendsDotZero()
    {
        var result = VersionParsers.ParseAssemblyVersion("1.2.3");
        Assert.That(result, Is.EqualTo("1.2.3.0"));
    }

    [Test]
    public void ParseAssemblyVersion_WithPreRelease_StripsPreReleaseAndAppendsDotZero()
    {
        var result = VersionParsers.ParseAssemblyVersion("1.2.3-preview.5");
        Assert.That(result, Is.EqualTo("1.2.3.0"));
    }
}
