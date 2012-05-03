using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class SimpleTests
    {
        [Test]
        public void TestSucceeds()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
        }

        [Test, ExpectedException(typeof(ApplicationException))]
        public void TestSucceeds_ExpectedException()
        {
            throw new ApplicationException("Expected");
        }

        [Test]
        public void TestFails()
        {
            Assert.That(2 + 2, Is.EqualTo(5));
        }

        [Test]
        public void TestIsInconclusive()
        {
            Assert.Inconclusive("Testing");
        }

        [Test, Ignore("Ignoring this test deliberately")]
        public void TestIsIgnored_Attribute()
        {
        }

        [Test]
        public void TestIsIgnored_Assert()
        {
            Assert.Ignore("Ignoring this test deliberately");
        }

        [Test]
        public void TestThrowsException()
        {
            throw new Exception("Deliberate exception thrown");
        }
    }
}
