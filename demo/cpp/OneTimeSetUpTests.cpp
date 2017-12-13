#include "ExpectedOutcomeAttributes.h"

using namespace System::Collections::Generic;
using namespace System::Text;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
    [TestFixture, ExpectPass]
    public ref class OneTimeSetUpTests
    {
	private:
        int SetUpCount;
        int TearDownCount;

	public:
        [OneTimeSetUp]
        void BeforeTests()
        {
            Assert::That(SetUpCount, Is::EqualTo(0));
            Assert::That(TearDownCount, Is::EqualTo(0));
            SetUpCount++;
		}

        [OneTimeTearDown]
        void AfterTests()
        {
            Assert::That(SetUpCount, Is::EqualTo(1), "Unexpected error");
            Assert::That(TearDownCount, Is::EqualTo(0));
            TearDownCount++;
        }

        [Test]
        void Test1()
        {
            Assert::That(SetUpCount, Is::EqualTo(1));
            Assert::That(TearDownCount, Is::EqualTo(0));
        }

        [Test]
        void Test2()
        {
            Assert::That(SetUpCount, Is::EqualTo(1));
            Assert::That(TearDownCount, Is::EqualTo(0));
        }
	};
}
