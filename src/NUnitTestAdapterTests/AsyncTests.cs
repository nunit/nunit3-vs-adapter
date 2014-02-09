using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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

        [TestCase(ExpectedResult = 1 )]
        public async Task<int> TaskTTestCaseWithResultCheckSuccess()
        {
            return await ReturnOne();
        }

        [TestCase(ExpectedResult=TestOutcome.Failed), ExpectedException(typeof(AssertionException))]
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
