using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnitTestDemo
{
	public class AsyncTests
	{
		[Test, Should("Pass")]
		public async void AsyncVoidTestSucceeds()
		{
			var result = await ReturnOne();

			Assert.AreEqual(1, result);
		}

		[Test, Should("Fail")]
		public async void AsyncVoidTestFails()
		{
			var result = await ReturnOne();

			Assert.AreEqual(2, result);
		}

		[Test, Should("Error")]
		public async void AsyncVoidTestThrowsException()
		{
			await ThrowException();

			Assert.Fail("Should never get here");
		}

		[Test, Should("Pass")]
		public async Task AsyncTaskTestSucceeds()
		{
			var result = await ReturnOne();

			Assert.AreEqual(1, result);
		}

		[Test, Should("Fail")]
		public async Task AsyncTaskTestFails()
		{
			var result = await ReturnOne();

			Assert.AreEqual(2, result);
		}

		[Test, Should("Error")]
		public async Task AsyncTaskTestThrowsException()
		{
			await ThrowException();

			Assert.Fail("Should never get here");
		}

		[TestCase(ExpectedResult = 1), Should("Pass")]
		public async Task<int> AsyncTaskWithResultSucceeds()
		{
			return await ReturnOne();
		}

		[TestCase(ExpectedResult = 2), Should("Fail")]
		public async Task<int> AsyncTaskWithResultFails()
		{
			return await ReturnOne();
		}

        [TestCase(ExpectedResult = 0), Should("Error")]
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