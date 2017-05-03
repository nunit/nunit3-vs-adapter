using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
