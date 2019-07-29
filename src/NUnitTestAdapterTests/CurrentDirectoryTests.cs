using System.Linq;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class CurrentDirectoryTests
    {
        [TestCase(@"C:\Windows")]
        [TestCase(@"C:\Windows\Whatever")]
        [TestCase(@"C:\Program Files\Something")]
        [TestCase(@"C:\Program Files (x86)\Something")]
        public void ThatWeFindForbiddenFolders(string folder)
        {
            var sut = new TestAdapter.NUnit3TestExecutor();
            sut.InitializeForbiddenFolders();
            Assert.That(sut.CheckDirectory(folder));
        }

        [TestCase(@"C:\Whatever")]
        [TestCase(@"C:\WindowsWhatever")]
        [TestCase(@"C:\Program Files Whatever\Something")]
        public void ThatWeAcceptNonForbiddenFolders(string folder)
        {
            var sut = new TestAdapter.NUnit3TestExecutor();
            sut.InitializeForbiddenFolders();
            Assert.That(sut.CheckDirectory(folder),Is.False);
        }

        [Test]
        public void ThatForbiddenFoldersAreUnique()
        {
            var sut = new TestAdapter.NUnit3TestExecutor();
            sut.InitializeForbiddenFolders();
            var sutunique = sut.ForbiddenFolders.Distinct();
            Assert.That(sutunique.Count(),Is.EqualTo(sut.ForbiddenFolders.Count),"There are duplicate entries in ForbiddenFolders");
        }
    }
}
