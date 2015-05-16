using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo.SetUpFixture
{
    [SetUpFixture]
    public class SetUpFixture
    {
        public static int SetUpCount;
        public static int TearDownCount;

        [OneTimeSetUp]
        public void BeforeTests()
        {
            Assert.That(SetUpCount, Is.EqualTo(0));
            SetUpCount++;
        }

        [OneTimeTearDown]
        public void AfterTests()
        {
            Assert.That(TearDownCount, Is.EqualTo(0));
            TearDownCount++;
        }
    }

    [TestFixture, ExpectPass]
    public class TestFixture1
    {
        [Test]
        public void Test1()
        {
            Assert.That(SetUpFixture.SetUpCount, Is.EqualTo(1));
            Assert.That(SetUpFixture.TearDownCount, Is.EqualTo(0));
        }
    }

    [TestFixture, ExpectPass]
    public class TestFixture2
    {
        [Test]
        public void Test2()
        {
            Assert.That(SetUpFixture.SetUpCount, Is.EqualTo(1));
            Assert.That(SetUpFixture.TearDownCount, Is.EqualTo(0));
        }
    }
}
