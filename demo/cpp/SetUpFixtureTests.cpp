#include "ExpectedOutcomeAttributes.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
	namespace SetUpFixture
	{
		[SetUpFixture]
		public ref class SetUpFixture
		{
		public:
			static int SetUpCount;
			static int TearDownCount;

			[OneTimeSetUp]
			void BeforeTests()
			{
				Assert::That(SetUpCount, Is::EqualTo(0));
				SetUpCount++;
			}

			[OneTimeTearDown]
			void AfterTests()
			{
				Assert::That(TearDownCount, Is::EqualTo(0));
				TearDownCount++;
			}
		};

		[TestFixture, ExpectPass]
		public ref class TestFixture1
		{
		public:
			[Test]
			void Test1()
			{
				Assert::That(SetUpFixture::SetUpCount, Is::EqualTo(1));
				Assert::That(SetUpFixture::TearDownCount, Is::EqualTo(0));
			}
		};

		[TestFixture, ExpectPass]
		public ref class TestFixture2
		{
			[Test]
			void Test2()
			{
				Assert::That(SetUpFixture::SetUpCount, Is::EqualTo(1));
				Assert::That(SetUpFixture::TearDownCount, Is::EqualTo(0));
			}
		};
	}
}
