using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

#if NETCOREAPP1_1
namespace NUnitCoreTestDemo
#else
namespace NUnitTestDemo
#endif
{
    [TestFixture(typeof(int))]
    public class GenericTests<T>
    {
        [Test, ExpectPass]
        public void TestIt() { }
    }

    [ExpectPass]
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
