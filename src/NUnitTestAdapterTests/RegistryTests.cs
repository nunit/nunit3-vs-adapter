
using Microsoft.Win32;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{


    [SetUpFixture]
    public class Init
    {
        [SetUp]
        public void SetUp()
        {
            // Try to ensure that the test registry entry is present
            const string userRoot = "HKEY_CURRENT_USER";
            const string subkey = "UseDuringTest";
            const string keyName = userRoot + "\\" + subkey;
            Registry.SetValue(keyName, "SomeDataText", "Test", RegistryValueKind.String);

        }
    }


    [TestFixture]
    public class RegistryTests
    {


        [SetUp]
        public void Init()
        {
            
        }

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

        [TestCase("UseDuringTest", "SomeDataText",true)]
        [TestCase("ShouldNotBeThere", "SomeDataText",false)]
        [Test]
        public void RegistryTestExists(string key, string parameter, bool expected)
        {
            var reg = new RegistryCurrentUser(key);
            var res = reg.Exist(parameter);
            Assert.That(res, Is.EqualTo(expected));
        }
    }
}
