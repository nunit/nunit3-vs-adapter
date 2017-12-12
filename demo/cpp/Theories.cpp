#include "ExpectedOutcomeAttributes.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
    public class Theories
    {
	private:
        [Datapoints]
        int data[3] = { 0, 1, 42 };

	public:
        [Theory, ExpectPass]
        void Theory_AllCasesSucceed(int a, int b)
        {
            Assert::That(a + b, Is::EqualTo(b + a));
        }

        [Theory, ExpectMixed]
        void Theory_SomeCasesAreInconclusive(int a, int b)
        {
            Assume::That(b != 0);
        }

        [Theory, ExpectMixed]
        void Theory_SomeCasesFail(int a, int b)
        {
            Assert::That(b != 0);
        }
	};
}
