using System;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public abstract class InheritedTestBaseClass
    {
        [Test]
        public void TestInBaseClass()
        {
        }
    }

    public class InheritedTestDerivedClass : InheritedTestBaseClass
    {
    }
}
