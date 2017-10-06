using System.Text.RegularExpressions;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Dump;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [Category(nameof(DumpXml))]
    public class DumpXmlTests
    {
        [Test]
        public void ThatWritingClearsBuffer()
        {
            var file = Substitute.For<IFile>();
            var sut = new DumpXml("whatever",file);
            var string1 = "something";
            sut.AddString(string1);
            sut.Dump4Discovery();
            file.Received().WriteAllText(Arg.Any<string>(),Arg.Is<string>(o=>o==string1));
            var string2 = "new string";
            sut.AddString(string2);
            sut.Dump4Discovery();
            file.Received().WriteAllText(Arg.Any<string>(), Arg.Is<string>(o => o == string2));
        }

        [Test]
        public void ThatRandomNameContainsValidCharacters()
        {
            var sut = new DumpXml("whatever");
            var res = sut.RandomName();
            Assert.That(res.EndsWith(".dump"));
            var parts = res.Split('.');
            Assert.That(parts.Length,Is.EqualTo(2),$"Too many dots in {res}");
            var part1 = parts[0];
            var rg = new Regex(@"^[a-zA-Z0-9\s]*$");
            Assert.That(rg.IsMatch(part1));
        }

        [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder\Dump\D_Whatever.dll.dump")]
        [TestCase(@"C:\MyFolder\Whatever.dll", @"C:\MyFolder\Dump")]
        [TestCase(@"C:\MyFolder\Whatever.dll",@"C:\MyFolder")]
        public void ThatPathIsCorrectlyParsedInDiscoveryPhase(string path,string expected)
        {
            var file = Substitute.For<IFile>();
            var sut = new DumpXml(path,file);
            sut.AddString("whatever");
            sut.Dump4Discovery();
            file.Received().WriteAllText(Arg.Is<string>(o=>o.StartsWith(expected)),Arg.Any<string>());

        }
    }
}
