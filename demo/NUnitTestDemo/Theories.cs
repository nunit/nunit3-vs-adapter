using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class Theories
    {
        [Datapoints]
        int[] data = new int[] { 0, 1, 42 };

        [Theory, ExpectPass]
        public void Theory_AllCasesSucceed(int a, int b)
        {
            Assert.That(a + b, Is.EqualTo(b + a));
        }

        [Theory, ExpectMixed]
        public void Theory_SomeCasesAreInconclusive(int a, int b)
        {
            Assume.That(b != 0);
        }

        [Theory, ExpectMixed]
        public void Theory_SomeCasesFail(int a, int b)
        {
            Assert.That(b != 0);
        }
    }
}
