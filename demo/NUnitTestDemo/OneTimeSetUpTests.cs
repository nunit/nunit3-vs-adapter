using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    [TestFixture, ExpectPass]
    public class OneTimeSetUpTests
    {
        int SetUpCount;
        int TearDownCount;

        [OneTimeSetUp]
        public void BeforeTests()
        {
            Assert.That(SetUpCount, Is.EqualTo(0));
            Assert.That(TearDownCount, Is.EqualTo(0));
            SetUpCount++;
        }

        [OneTimeTearDown]
        public void AfterTests()
        {
            Assert.That(SetUpCount, Is.EqualTo(1), "Unexpected error");
            Assert.That(TearDownCount, Is.EqualTo(0));
            TearDownCount++;
        }

        [Test]
        public void Test1()
        {
            Assert.That(SetUpCount, Is.EqualTo(1));
            Assert.That(TearDownCount, Is.EqualTo(0));
        }

        [Test]
        public void Test2()
        {
            Assert.That(SetUpCount, Is.EqualTo(1));
            Assert.That(TearDownCount, Is.EqualTo(0));
        }
    }
}
