using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class Class1
    {
        [Datapoints]
        int[] data = new int[] { 0, 1, 42 };

        [Test]
        public void TwoPlusTwo()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
            //Assert.Inconclusive("Testing");
        }

        [TestCase(2, 2, Result=4)]
        [TestCase(0, 5, Result=5)]
        [TestCase(31, 11, Result=42)]
        public int TestAddition(int a, int b)
        {
            return a + b;
        }

        [Theory]
        public void AdditionIsCommutative(int a, int b)
        {
            Assert.That(a + b, Is.EqualTo(b + a));
        }
    }
}
