using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class CurrentDirectoryTests
    {
        [TestCase(@"C:\Windows")]
        [TestCase(@"C:\Program Files\IIS")]
        [TestCase(@"C:\Program Files (x86)\IIS")]
        public void ThatWeFindForbiddenFolders(string folder)
        {
            Assert.That(TestAdapter.NUnitTestAdapter.CheckDirectory(folder), Is.False);
        }

        [TestCase(@"C:\WindowsWhatever")]
        [TestCase(@"C:\Program Files Yeah\IIS")]
        public void ThatWeAcceptNonForbiddenFolders(string folder)
        {
            Assert.That(TestAdapter.NUnitTestAdapter.CheckDirectory(folder));
        }
    }
}
