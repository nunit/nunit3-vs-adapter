using System;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture, Category("TFS")]
    public class TestFilterTests
    {
        [Test]
        public void PropertyProvider()
        {
            var testfilter = new TfsTestFilter(null);
            var prop = TfsTestFilter.PropertyProvider("Priority");
            Assert.NotNull(prop);
            prop = TfsTestFilter.PropertyProvider("TestCategory");
            Assert.NotNull(prop);
        }

        [Test]
        public void TraitProvider()
        {
            var testFilter = new TfsTestFilter(null);
            var trait = TfsTestFilter.TraitProvider("TestCategory");
            Assert.NotNull(trait);
        }

        [Test]
        public void TraitProviderWithNoCategory()
        {
            var testFilter = new TfsTestFilter(null);
            var trait = TfsTestFilter.TraitProvider("JustKidding");
            Assert.Null(trait);
        }

        [Test]
        public void PropertyValueProviderFqn()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = new TfsTestFilter(null);
            var obj = TfsTestFilter.PropertyValueProvider(tc, "FullyQualifiedName");
            Assert.AreSame("Test1", obj);
        }

        [Test]
        public void PropertyValueProviderCategoryWithOneCategory()
        {
            CheckTraitsSupported();

            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            var testFilter = new TfsTestFilter(null);
            var obj = TfsTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.AreSame("CI", obj);
        }

        [Test]
        public void PropertyValueProviderCategoryWithNoTraits()
        {
            CheckTraitsSupported();

            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = new TfsTestFilter(null);
            var obj = TfsTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.IsNull(obj);
        }

        [Test]
        public void PropertyValueProviderCategoryWithMultipleCategories()
        {
            CheckTraitsSupported();

            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            tc.AddTrait("Category", "MyOwn");
            var testFilter = new TfsTestFilter(null);
            var obj = TfsTestFilter.PropertyValueProvider(tc, "TestCategory") as string[];
            Assert.IsNotNull(obj);
            Assert.AreEqual(obj.Length,2);
            Assert.AreSame("CI", obj[0]);
            Assert.AreSame("MyOwn",obj[1]);
        }

        [Test]
        public void PropertyValueProviderCategoryFail()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            var testFilter = new TfsTestFilter(null);
            var obj = TfsTestFilter.PropertyValueProvider(tc, "Garbage");
            Assert.Null(obj);
        }

        private static void CheckTraitsSupported()
        {
            if (!TraitsFeature.IsSupported)
                Assert.Inconclusive("This version of Visual Studio does not support Traits");
        }
    }
}
