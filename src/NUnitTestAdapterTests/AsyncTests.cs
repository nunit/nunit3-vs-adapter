using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class AsyncTests
    {
        [Test]
        public async void VoidTestSuccess()
        {
            var result = await ReturnOne();

            Assert.AreEqual(1, result);
        }

        [Test, ExpectedException(typeof(AssertionException))]
        public async void VoidTestFailure()
        {
            var result = await ReturnOne();

            Assert.AreEqual(2, result);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async void VoidTestExpectedException()
        {
            await ThrowException();
        }

        [Test]
        public async Task TaskTestSuccess()
        {
            var result = await ReturnOne();

            Assert.AreEqual(1, result);
        }

        [Test, ExpectedException(typeof(AssertionException))]
        public async Task TaskTestFailure()
        {
            var result = await ReturnOne();

            Assert.AreEqual(2, result);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task TaskTestExpectedException()
        {
            await ThrowException();
        }

        [TestCase(Result = 1)]
        public async Task<int> TaskTTestCaseWithResultCheckSuccess()
        {
            return await ReturnOne();
        }

        [TestCase(Result = 2), ExpectedException(typeof(AssertionException))]
        public async Task<int> TaskTTestCaseWithResultCheckFailure()
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
