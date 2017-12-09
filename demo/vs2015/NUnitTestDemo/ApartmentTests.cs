using System;
using System.Threading;
using NUnit.Framework;

namespace NUnitTestDemo
{
    [Apartment(ApartmentState.STA)]
    public class FixtureWithApartmentAttributeOnClass
    {
        [Test]
        public void TestMethodInSTAFixture()
        {
            Assert.That(Thread.CurrentThread.GetApartmentState(), Is.EqualTo(ApartmentState.STA));
        }
    }

    public class FixtureWithApartmentAttributeOnMethod
    {
        [Test, Apartment(ApartmentState.STA)]
        public void TestMethodInSTA()
        {
            Assert.That(Thread.CurrentThread.GetApartmentState(), Is.EqualTo(ApartmentState.STA));
        }
    }
}
