using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using System.Reflection;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    
    [TestFixture]
    public class TestFilterTests
    {
        private MethodInfo traitsCollectionAdd;
        private readonly PropertyInfo traitsProperty;

        public TestFilterTests()
        {
            traitsProperty = typeof(TestCase).GetProperty("Traits");
            if (traitsProperty != null)
            {
                var traitCollectionType = traitsProperty.PropertyType;
                if (traitCollectionType != null)
                    traitsCollectionAdd = traitCollectionType.GetMethod("Add", new Type[] {typeof(string), typeof(string)});
            }
        }

        private void AddTrait(TestCase testCase, string traitname, string traitvalue)
        {
            object traitsCollection = traitsProperty.GetValue(testCase, new object[0]);
            if (traitsCollection == null) return;
            traitsCollectionAdd.Invoke(traitsCollection, new object[] { traitname, traitvalue });
        }


        [Test]
        [Category("TFS")]
        public void PropertyProvider()
        {
            var testfilter = new TFSTestFilter(null);
            var prop = TFSTestFilter.PropertyProvider("Priority");
            Assert.NotNull(prop);
            prop = TFSTestFilter.PropertyProvider("TestCategory");
            Assert.NotNull(prop);
        }
        [Test]
        [Category("TFS")]
        public void TraitProvider()
        {
            var testFilter = new TFSTestFilter(null);
            var trait = TFSTestFilter.TraitProvider("TestCategory");
            Assert.NotNull(trait);
        }

        [Test]
        [Category("TFS")]
        public void TraitProviderWithNoCategory()
        {
            var testFilter = new TFSTestFilter(null);
            var trait = TFSTestFilter.TraitProvider("JustKidding");
            Assert.Null(trait);
        }

        [Test]
        [Category("TFS")]
        public void PropertyValueProviderFqn()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "FullyQualifiedName");
            Assert.AreSame("Test1", obj);
        }

        [Test]
        [Category("TFS")]
        public void PropertyValueProviderCategoryWithOneCategory()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            this.AddTrait(tc, "Category", "CI");
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.AreSame("CI", obj);
        }

        [Test]
        [Category("TFS")]
        public void PropertyValueProviderCategoryWithNoTraits()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.IsNull(obj);
        }

        [Test]
        [Category("TFS")]
        public void PropertyValueProviderCategoryWithMultipleCategories()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            this.AddTrait(tc, "Category", "CI");
            this.AddTrait(tc, "Category", "MyOwn");
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "TestCategory") as string[];
            Assert.IsNotNull(obj);
            Assert.AreEqual(obj.Length,2);
            Assert.AreSame("CI", obj[0]);
            Assert.AreSame("MyOwn",obj[1]);
        }

        [Test]
        [Category("TFS")]
        public void PropertyValueProviderCategoryFail()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            this.AddTrait(tc, "Category", "CI");
            var testFilter = new TFSTestFilter(null);
            var obj = TFSTestFilter.PropertyValueProvider(tc, "Garbage");
            Assert.Null(obj);
        }


    }
}
