#include "ExpectedOutcomeAttributes.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace NUnit::Framework;
using namespace NUnit::Framework::Internal;
using namespace NUnit::Framework::Interfaces;

namespace NUnitTestDemo
{
    public class ParameterizedTests
    {
	public:
        [ExpectPass]
        [TestCase(2, 2, 4)]
        [TestCase(0, 5, 5)]
        [TestCase(31, 11, 42)]
        void TestCaseSucceeds(int a, int b, int sum)
        {
            Assert::That(a + b, Is::EqualTo(sum));
        }

        [ExpectPass]
        [TestCase(2, 2, ExpectedResult = 4)]
        [TestCase(0, 5, ExpectedResult = 5)]
        [TestCase(31, 11, ExpectedResult = 42)]
        int TestCaseSucceeds_Result(int a, int b)
        {
            return a + b;
        }

        [ExpectFailure]
        [TestCase(31, 11, 99)]
        void TestCaseFails(int a, int b, int sum)
        {
            Assert::That(a + b, Is::EqualTo(sum));
        }

        [ExpectWarning]
        [TestCase(31, 11, 99)]
        void TestCaseWarns(int a, int b, int sum)
        {
            Warn::Unless(a + b, Is::EqualTo(sum));
        }

        [ExpectWarning]
        [TestCase(31, 11, 99)]
        void TestCaseWarnsThreeTimes(int a, int b, int answer)
        {
            Warn::Unless(a + b, Is::EqualTo(answer), "Bad sum");
            Warn::Unless(a - b, Is::EqualTo(answer), "Bad difference");
            Warn::Unless(a * b, Is::EqualTo(answer), "Bad product");
        }

        [TestCase(31, 11, ExpectedResult = 99), ExpectFailure]
        int TestCaseFails_Result(int a, int b)
        {
            return a + b;
        }

        [TestCase(31, 11), ExpectInconclusive]
        void TestCaseIsInconclusive(int a, int b)
        {
            Assert::Inconclusive("Inconclusive test case");
        }

        [Ignore("Ignored test"), ExpectIgnore]
        [TestCase(31, 11)]
        void TestCaseIsIgnored_Attribute(int a, int b)
        {
        }

        [TestCase(31, 11, Ignore = "Ignoring this"), ExpectIgnore]
        void TestCaseIsIgnored_Property(int a, int b)
        {
        }

        [TestCase(31, 11), ExpectIgnore]
        void TestCaseIsIgnored_Assert(int a, int b)
        {
            Assert::Ignore("Ignoring this test case");
        }

#if !NETCOREAPP1_1
        [TestCase(31, 11, ExcludePlatform="NET"), ExpectSkip]
        void TestCaseIsSkipped_Property(int a, int b)
        {
        }

        [Platform(Exclude = "NET"), ExpectSkip]
        [TestCase(31, 11)]
        void TestCaseIsSkipped_Attribute(int a, int b)
        {
        }
#endif

        [Explicit, ExpectSkip]
        [TestCase(31, 11)]
        void TestCaseIsExplicit(int a, int b)
        {
        }

        [TestCase(31, 11), ExpectError]
        void TestCaseThrowsException(int a, int b)
        {
            throw gcnew Exception("Exception from test case");
        }

        [TestCase(42, TestName="AlternateTestName"), ExpectPass]
        void TestCaseWithAlternateName(int x)
        {
        }

        [TestCase(42, TestName="NameWithSpecialChar->Here")]
        void TestCaseWithSpecialCharInName(int x)
        {
        }

        [Test]
        void TestCaseWithRandomParameter([Random(1)] int x)
        {
        }

#if false // Test for issue #144
        [MyTestCase]
        void TestCaseWithBadTestBuilder(string baz)
        {
            Assert.That(baz, Is.EqualTo("baz"));
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=false)]
        class MyTestCaseAttribute : Attribute, ITestBuilder
        {
            public IEnumerable<TestMethod> BuildFrom(IMethodInfo method, Test suite)
            {
                throw new InvalidOperationException("This is intentionally broken");
            }
        }
#endif
	};
}
