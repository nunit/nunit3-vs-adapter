using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

#if NETCOREAPP1_1
namespace NUnitCoreTestDemo
#else
namespace NUnitTestDemo
#endif
{
	public class AsyncTests
	{
		[Test, ExpectError]
		public async void AsyncVoidTestIsInvalid()
		{
            var result = await ReturnOne();

            Assert.AreEqual(1, result);
		}

		[Test, ExpectPass]
		public async Task AsyncTaskTestSucceeds()
		{
			var result = await ReturnOne();

			Assert.AreEqual(1, result);
		}

		[Test, ExpectFailure]
		public async Task AsyncTaskTestFails()
		{
			var result = await ReturnOne();

			Assert.AreEqual(2, result);
		}

		[Test, ExpectError]
		public async Task AsyncTaskTestThrowsException()
		{
			await ThrowException();

			Assert.Fail("Should never get here");
		}

		[TestCase(ExpectedResult = 1), ExpectPass]
		public async Task<int> AsyncTaskWithResultSucceeds()
		{
			return await ReturnOne();
		}

		[TestCase(ExpectedResult = 2), ExpectFailure]
		public async Task<int> AsyncTaskWithResultFails()
		{
			return await ReturnOne();
		}

        [TestCase(ExpectedResult = 0), ExpectError]
		public async Task<int> AsyncTaskWithResultThrowsException()
		{
			return await ThrowException();
		}

		private static Task<int> ReturnOne()
		{
			return Task.Run(() => 1);
		}

		private static Task<int> ThrowException()
		{
			return Task.Run(() =>
			{
				throw new InvalidOperationException();
				return 1;
			});
		}
	}
}