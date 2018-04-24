using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [Explicit]
    [TestFixture]
    [Ignore("Temporary disabled...")]
    [Category("LongRunning")]
    class IssueNo24Tests
    {
        [Test]
        public void Quick()
        {
            Thread.Sleep(1);
        }

        [Test]
        public void Slow()
        {
            Thread.Sleep(150000);
        }

        [Test]
        public void Slower()
        {
            Thread.Sleep(250000);
        }

        [Test]
        public void TooLateButFast()
        {
            Thread.Sleep(1);
        }
    }
}
