using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    
    [TestFixture]
    public class TestFilterTests
    {
        [Test]
        public void PropertyProvider()
        {
            var testfilter = new TFSTestFilter(null);
            var prop = TFSTestFilter.PropertyProvider("Priority");
            Assert.NotNull(prop);
            prop = TFSTestFilter.PropertyProvider("TestCategory");
            Assert.NotNull(prop);
        }
        [Test]
        public void TraitProvider()
        {
            var testFilter = new TFSTestFilter(null);
            var trait = TFSTestFilter.TraitProvider("TestCategory");
            Assert.NotNull(trait);
        }

        [Test]
        public void TraitProviderWithNoCategory()
        {
            var testFilter = new TFSTestFilter(null);
            var trait = TFSTestFilter.TraitProvider("JustKidding");
            Assert.Null(trait);
        }

        [Test]
        public void PropertyValueProviderFqn()
        {
            var tc = new TestCase("Test1", new Uri("executor://xunit.codeplex.com/VsTestRunner"), "xUnit.VSIX");
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "FullyQualifiedName");
            Assert.AreSame("Test1", obj);
        }

        [Test]
        public void PropertyValueProviderCategory()
        {
            var tc = new TestCase("Test1", new Uri("executor://xunit.codeplex.com/VsTestRunner"), "xUnit.VSIX");
            tc.Traits.Add(new Trait("Category", "CI"));
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.AreSame("CI", obj);
        }

        [Test]
        public void PropertyValueProviderCategoryFail()
        {
            var tc = new TestCase("Test1", new Uri("executor://xunit.codeplex.com/VsTestRunner"), "xUnit.VSIX");
            tc.Traits.Add(new Trait("Category", "CI"));
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "Garbage");
            Assert.Null(obj);
        }


    }
}
