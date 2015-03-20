using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class AsyncTests
    {
        [Test]
        public async Task TaskTestSuccess()
        {
            var result = await ReturnOne();

            Assert.AreEqual(1, result);
        }

        [TestCase(ExpectedResult = 1 )]
        public async Task<int> TaskTTestCaseWithResultCheckSuccess()
        {
            return await ReturnOne();
        }

        private static Task<int> ReturnOne()
        {
            return Task.Run(() => 1);
        }

        private static Task ThrowException()
        {
            return Task.Run(() =>
            {
                throw new InvalidOperationException();
            });
        }
    }
}
