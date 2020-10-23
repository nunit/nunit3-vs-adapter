using System.Reflection;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture]
    public class ExperimentalTests
    {
        [Test]
        public void LocationTest()
        {
            var location = typeof(ExperimentalTests).GetTypeInfo().Assembly.Location;
        }
    }
}
