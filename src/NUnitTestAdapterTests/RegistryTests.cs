
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture]
    public class RegistryTests
    {
        [Test]
        public void RegistryTestDoesExist()
        {
            var reg = new RegistryCurrentUser("UseDuringTest");

            Assert.That(reg, Is.Not.Null);

        }

        // [TestCase(true,true)] doesnt do booleans
        [TestCase("yep", "yep")]
        [TestCase(42, 42)]
        // [TestCase('c','c')] doesnt do chars
        [Test]
        public void RegistryTestWriteReadSimpleTypes<T>(T data, T expected)
        {
            var reg = new RegistryCurrentUser("UseDuringTest");

            reg.Write("SomeData", data);

            var wr = reg.Read<T>("SomeData");
            Assert.That(wr, Is.EqualTo(expected));

        }


        [Test]
        public void ReadRegistryWithNoData()
        {
            var reg = new RegistryCurrentUser("UseDuringTest");
            var wr = reg.Read<int>("SomeThingNotExisting");
            Assert.That(wr,Is.EqualTo(default(int)));
        }

        [TestCase("UseDuringTest", true)]
        [TestCase("ShouldNotBeThere", false)]
        [Test]
        public void RegistryTestExists(string key, bool expected)
        {
            var reg = new RegistryCurrentUser(key);
            var res = reg.Exist;
            Assert.That(res, Is.EqualTo(expected));
        }
    }
}
