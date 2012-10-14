using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    [TestFixture(typeof(int))]
    public class GenericTests<T>
    {
        [Test]
        public void TestIt() { }
    }

    [TestFixture(typeof(ArrayList))]
    [TestFixture(typeof(List<int>))]
    public class GenericTests_IList<TList> where TList : IList, new()
    {
        private IList list;

        [SetUp]
        public void CreateList()
        {
            this.list = new TList();
        }

        [Test]
        public void CanAddToList()
        {
            list.Add(1); list.Add(2); list.Add(3);
            Assert.AreEqual(3, list.Count);
        }
    }
}
