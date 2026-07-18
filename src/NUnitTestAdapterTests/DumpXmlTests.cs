// ***********************************************************************
// Copyright (c) 2017 Charlie Poole, Terje Sandstrom
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
using System.Text.RegularExpressions;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter;
using NUnit.VisualStudio.TestAdapter.Dump;

namespace NUnit.VisualStudio.TestAdapter.Tests;

[Category(nameof(DumpXml))]
public class DumpXmlTests
{
    [Test]
    public void ThatWritingClearsBuffer()
    {
            string res = "";
            var file = Substitute.For<IFile>();
            file.WriteAllText(Arg.Any<string>(), Arg.Do<string>(o => res = o));
            var sut = new DumpXml("whatever", file);
            var string1 = "something";
            sut.AddString(string1);
            sut.DumpForDiscovery();
            Assert.That(res, Does.Contain(string1));
            var string2 = "new string";
            sut.AddString(string2);
            sut.DumpForDiscovery();
            Assert.That(res, Does.Contain(string2));
            Assert.That(res, Does.Not.Contain(string1));
    }

    [Test]
    public void ThatRandomNameContainsValidCharacters()
    {
            var sut = new DumpXml("whatever");
            var res = sut.RandomName();
            Assert.That(res, Does.EndWith(".dump"));
            var parts = res.Split('.');
            Assert.That(parts.Length, Is.EqualTo(2), $"Too many dots in {res}");
            var part1 = parts[0];
            var rg = new Regex(@"^[a-zA-Z0-9\s]*$");
            Assert.That(rg.IsMatch(part1));
        }

    [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder\Dump\D_Whatever.dll.dump")]
    [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder\Dump")]
    [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder")]
    [Platform("Win")]
    public void ThatPathIsCorrectlyParsedInDiscoveryPhase(string path, string expected)
    {
            var file = Substitute.For<IFile>();
            var sut = new DumpXml(path, file);
            sut.AddString("whatever");
            sut.DumpForDiscovery();
            file.Received().WriteAllText(Arg.Is<string>(o => o.StartsWith(expected, StringComparison.OrdinalIgnoreCase)), Arg.Any<string>());
        }

    [TestCase("/some/Folder/Whatever.dll", "/some/Folder/Dump/D_Whatever.dll.dump")]
    [TestCase("/some/Folder/Whatever.dll", "/some/Folder/Dump")]
    [TestCase("/some/Folder/Whatever.dll", "/some/Folder")]
    [Platform("Unix")]
    public void ThatPathIsCorrectlyParsedInDiscoveryPhaseOnUnix(string path, string expected)
    {
            var file = Substitute.For<IFile>();
            var sut = new DumpXml(path, file);
            sut.AddString("whatever");
            sut.DumpForDiscovery();
            file.Received().WriteAllText(Arg.Is<string>(o => o.StartsWith(expected, StringComparison.OrdinalIgnoreCase)), Arg.Any<string>());
        }


    [Test]
    public void ThatEmptyContainsHeaders()
    {
            string res = "";
            var file = Substitute.For<IFile>();
            file.WriteAllText(Arg.Any<string>(), Arg.Do<string>(o => res = o));
            var sut = new DumpXml("Whatever", file);
            sut.DumpForDiscovery();
            Assert.That(res, Does.Contain("NUnitXml"));
            var sarray = res.Split('\n');
            Assert.That(sarray.Length, Is.GreaterThanOrEqualTo(3));
        }

    [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder\Dump\E_Whatever.dll.dump")]
    [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder\Dump")]
    [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder")]
    [Platform("Win")]
    public void ThatPathIsCorrectlyParsedInExecutionPhase(string path, string expected)
    {
        var file = Substitute.For<IFile>();
        var sut = new DumpXml(path, file);
        sut.AddString("whatever");
        sut.DumpForExecution();
        file.Received().WriteAllText(Arg.Is<string>(o => o.StartsWith(expected, StringComparison.OrdinalIgnoreCase)), Arg.Any<string>());
    }

    [TestCase("/some/Folder/Whatever.dll", "/some/Folder/Dump/E_Whatever.dll.dump")]
    [TestCase("/some/Folder/Whatever.dll", "/some/Folder/Dump")]
    [TestCase("/some/Folder/Whatever.dll", "/some/Folder")]
    [Platform("Unix")]
    public void ThatPathIsCorrectlyParsedInExecutionPhaseOnUnix(string path, string expected)
    {
        var file = Substitute.For<IFile>();
        var sut = new DumpXml(path, file);
        sut.AddString("whatever");
        sut.DumpForExecution();
        file.Received().WriteAllText(Arg.Is<string>(o => o.StartsWith(expected, StringComparison.OrdinalIgnoreCase)), Arg.Any<string>());
    }

    [Test]
    public void ThatAddTestEventWrapsContentInNUnitTestEventElement()
    {
        string res = "";
        var file = Substitute.For<IFile>();
        file.WriteAllText(Arg.Any<string>(), Arg.Do<string>(o => res = o));
        var sut = new DumpXml("whatever", file);
        const string eventContent = "SomeTestCaseName";
        sut.AddTestEvent(eventContent);
        sut.DumpForDiscovery();
        Assert.That(res, Does.Contain("<NUnitTestEvent>"));
        Assert.That(res, Does.Contain("</NUnitTestEvent>"));
        Assert.That(res, Does.Contain(eventContent));
    }

    [TestCase("a&b", "&amp;")]
    [TestCase("a<b", "&lt;")]
    [TestCase("a>b", "&gt;")]
    [TestCase("a\"b", "&quot;")]
    [TestCase("a'b", "&apos;")]
    public void ThatAddXmlElementEscapesSpecialCharacters(string content, string expectedEscape)
    {
        string res = "";
        var file = Substitute.For<IFile>();
        file.WriteAllText(Arg.Any<string>(), Arg.Do<string>(o => res = o));
        var sut = new DumpXml("whatever", file);
        sut.AddXmlElement("El", content);
        sut.DumpForDiscovery();
        Assert.That(res, Does.Contain(expectedEscape));
    }

    [Test]
    public void ThatAddCancellationMessageIncludesExpectedElements()
    {
        string res = "";
        var file = Substitute.For<IFile>();
        file.WriteAllText(Arg.Any<string>(), Arg.Do<string>(o => res = o));
        var sut = new DumpXml("whatever", file);
        sut.AddCancellationMessage();
        sut.DumpForDiscovery();
        Assert.That(res, Does.Contain("<CancellationTime>"));
        Assert.That(res, Does.Contain("<Status>Execution was cancelled</Status>"));
    }

    [Test]
    public void ThatDumpVSInputFilterAddsTestFilterElement()
    {
        string res = "";
        var file = Substitute.For<IFile>();
        file.WriteAllText(Arg.Any<string>(), Arg.Do<string>(o => res = o));
        var sut = new DumpXml("whatever", file);
        sut.DumpVSInputFilter(new NUnit.Engine.TestFilter("<filter/>"), "SomeInfo");
        sut.DumpForDiscovery();
        Assert.That(res, Does.Contain("<TestFilter>"));
        Assert.That(res, Does.Contain("SomeInfo"));
    }

    [Test]
    public void ThatMissingDirectoryIsCreatedOnDump()
    {
        var file = Substitute.For<IFile>();
        file.DirectoryExist(Arg.Any<string>()).Returns(false);
        var sut = new DumpXml(@"C:\Some\Path\test.dll", file);
        sut.DumpForDiscovery();
        file.Received().CreateDirectory(Arg.Any<string>());
    }

    [Test]
    public void ThatExistingDirectoryIsNotRecreatedOnDump()
    {
        var file = Substitute.For<IFile>();
        file.DirectoryExist(Arg.Any<string>()).Returns(true);
        var sut = new DumpXml(@"C:\Some\Path\test.dll", file);
        sut.DumpForDiscovery();
        file.DidNotReceive().CreateDirectory(Arg.Any<string>());
    }

    [Test]
    public void ThatCreateDumpReturnsNullWhenDisabled()
    {
        var settings = Substitute.For<IAdapterSettings>();
        settings.DumpXmlTestResults.Returns(false);
        var result = DumpXml.CreateDump("whatever.dll", null, settings);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ThatCreateDumpReturnsInstanceWhenEnabled()
    {
        using var tempDir = new TempDirectory();
        var assemblyPath = System.IO.Path.Combine(tempDir.Path, "test.dll");
        var settings = Substitute.For<IAdapterSettings>();
        settings.DumpXmlTestResults.Returns(true);
        var result = DumpXml.CreateDump(assemblyPath, null, settings);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void ThatCreateDumpWritesInitialExecutionFileWithRunningByElement()
    {
        using var tempDir = new TempDirectory();
        var assemblyPath = System.IO.Path.Combine(tempDir.Path, "test.dll");
        var settings = Substitute.For<IAdapterSettings>();
        settings.DumpXmlTestResults.Returns(true);
        DumpXml.CreateDump(assemblyPath, null, settings);
        var dumpPath = System.IO.Path.Combine(tempDir.Path, "Dump", "E_test.dll.dump");
        Assert.That(System.IO.File.Exists(dumpPath), Is.True);
        var content = System.IO.File.ReadAllText(dumpPath);
        Assert.That(content, Does.Contain("RunningBy"));
    }

    [Test]
    public void ThatAppendToExistingDumpCreatesFileWhenNonePresent()
    {
        using var tempDir = new TempDirectory();
        var assemblyPath = System.IO.Path.Combine(tempDir.Path, "test.dll");
        var sut = new DumpXml(assemblyPath);
        sut.AddString("<data/>");
        sut.AppendToExistingDump();
        var dumpPath = System.IO.Path.Combine(tempDir.Path, "Dump", "E_test.dll.dump");
        Assert.That(System.IO.File.Exists(dumpPath), Is.True);
    }

    [Test]
    public void ThatAppendToExistingDumpAddsContentToExistingFile()
    {
        using var tempDir = new TempDirectory();
        var assemblyPath = System.IO.Path.Combine(tempDir.Path, "test.dll");

        var sut = new DumpXml(assemblyPath);
        sut.AddString("<first/>");
        sut.DumpForExecution();

        var sut2 = new DumpXml(assemblyPath);
        sut2.AddString("<second/>");
        sut2.AppendToExistingDump();

        var dumpPath = System.IO.Path.Combine(tempDir.Path, "Dump", "E_test.dll.dump");
        var content = System.IO.File.ReadAllText(dumpPath);
        Assert.That(content, Does.Contain("<first/>"));
        Assert.That(content, Does.Contain("<second/>"));
    }
}