using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class ParameterizedTests
    {
        [TestCase(2, 2, 4)]
        [TestCase(0, 5, 5)]
        [TestCase(31, 11, 42)]
        public void TestCaseSucceeds(int a, int b, int sum)
        {
            Assert.That(a + b, Is.EqualTo(sum));
        }

        [TestCase(2, 2, Result = 4)]
        [TestCase(0, 5, Result = 5)]
        [TestCase(31, 11, Result = 42)]
        public int TestCaseSucceeds_Result(int a, int b)
        {
            return a + b;
        }

        [TestCase(31, 11, ExpectedException=typeof(Exception))]
        public void TestCaseSucceeds_ExpectedException(int a, int b)
        {
            throw new Exception("Expected");
        }

        [TestCase(31, 11, 99)]
        public void TestCaseFails(int a, int b, int sum)
        {
            Assert.That(a + b, Is.EqualTo(sum));
        }

        [TestCase(31, 11, Result = 99)]
        public int TestCaseFails_Result(int a, int b)
        {
            return a + b;
        }

        [TestCase(31, 11)]
        public void TestCaseIsInconclusive(int a, int b)
        {
            Assert.Inconclusive("Inconclusive test case");
        }

        [Ignore("Ignored test")]
        [TestCase(31, 11)]
        public void TestCaseIsIgnored_Attribute(int a, int b)
        {
        }

        [TestCase(31, 11, Ignore = true, Reason = "Ignoring this")]
        public void TestCaseIsIgnored_Property(int a, int b)
        {
        }

        [TestCase(31, 11)]
        public void TestCaseIsIgnored_Assert(int a, int b)
        {
            Assert.Ignore("Ignoring this test case");
        }

        [TestCase(31, 11)]
        public void TestCaseThrowsException(int a, int b)
        {
            throw new Exception("Exception from test case");
        }
    }
}
