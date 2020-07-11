using System.Threading;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [Explicit]
    [TestFixture]
    [Category("LongRunning")]
    class IssueNo24Tests
    {
        [Explicit]
        [Test]
        public void Quick()
        {
            Thread.Sleep(1);
        }
        [Explicit]
        [Test]
        public void Slow()
        {
            Thread.Sleep(150000);
        }
        [Explicit]
        [Test]
        public void Slower()
        {
            Thread.Sleep(250000);
        }
        [Explicit]
        [Test]
        public void TooLateButFast()
        {
            Thread.Sleep(1);
        }
    }
}
