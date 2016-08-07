using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NUnit.Framework.Interfaces;

namespace NUnitTestDemo
{
    public class ParameterizedTests
    {
        [ExpectPass]
        [TestCase(2, 2, 4)]
        [TestCase(0, 5, 5)]
        [TestCase(31, 11, 42)]
        public void TestCaseSucceeds(int a, int b, int sum)
        {
            Assert.That(a + b, Is.EqualTo(sum));
        }

        [ExpectPass]
        [TestCase(2, 2, ExpectedResult = 4)]
        [TestCase(0, 5, ExpectedResult = 5)]
        [TestCase(31, 11, ExpectedResult = 42)]
        public int TestCaseSucceeds_Result(int a, int b)
        {
            return a + b;
        }

        [ExpectFailure]
        [TestCase(31, 11, 99)]
        public void TestCaseFails(int a, int b, int sum)
        {
            Assert.That(a + b, Is.EqualTo(sum));
        }

        [TestCase(31, 11, ExpectedResult = 99), ExpectFailure]
        public int TestCaseFails_Result(int a, int b)
        {
            return a + b;
        }

        [TestCase(31, 11), ExpectInconclusive]
        public void TestCaseIsInconclusive(int a, int b)
        {
            Assert.Inconclusive("Inconclusive test case");
        }

        [Ignore("Ignored test"), ExpectIgnore]
        [TestCase(31, 11)]
        public void TestCaseIsIgnored_Attribute(int a, int b)
        {
        }

        [TestCase(31, 11, Ignore = "Ignoring this"), ExpectIgnore]
        public void TestCaseIsIgnored_Property(int a, int b)
        {
        }

        [TestCase(31, 11), ExpectIgnore]
        public void TestCaseIsIgnored_Assert(int a, int b)
        {
            Assert.Ignore("Ignoring this test case");
        }

        [TestCase(31, 11, ExcludePlatform="NET"), ExpectSkip]
        public void TestCaseIsSkipped_Property(int a, int b)
        {
        }

        [Platform(Exclude = "NET"), ExpectSkip]
        [TestCase(31, 11)]
        public void TestCaseIsSkipped_Attribute(int a, int b)
        {
        }

        [Explicit, ExpectSkip]
        [TestCase(31, 11)]
        public void TestCaseIsExplicit(int a, int b)
        {
        }

        [TestCase(31, 11), ExpectError]
        public void TestCaseThrowsException(int a, int b)
        {
            throw new Exception("Exception from test case");
        }

        [TestCase(42, TestName="AlternateTestName"), ExpectPass]
        public void TestCaseWithAlternateName(int x)
        {
        }

        [TestCase(42, TestName="NameWithSpecialChar->Here")]
        public void TestCaseWithSpecialCharInName(int x)
        {
        }

        [Test]
        public void TestCaseWithRandomParameter([Random(1)] int x)
        {
        }

#if false // Test for issue #144
        [MyTestCase]
        public void TestCaseWithBadTestBuilder(string baz)
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
    }
}
