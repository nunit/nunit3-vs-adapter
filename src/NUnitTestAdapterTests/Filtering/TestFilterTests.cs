// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests.Filtering
{
    [TestFixture, Category("TFS")]
    public class TestFilterTests
    {
        private IAdapterSettings settings;

        [OneTimeSetUp]
        public void Setup()
        {
            settings = Substitute.For<IAdapterSettings>();
            settings.DiscoveryMethod.Returns(DiscoveryMethod.Legacy);
        }


        [Test]
        public void PropertyProvider()
        {
            var testfilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var prop = VsTestFilter.PropertyProvider("Priority");
            Assert.That(prop, Is.Not.Null);
            prop = VsTestFilter.PropertyProvider("TestCategory");
            Assert.That(prop, Is.Not.Null);
            prop = VsTestFilter.PropertyProvider("Category");
            Assert.That(prop, Is.Not.Null);
        }

        [Test]
        public void TraitProvider()
        {
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var trait = VsTestFilter.TraitProvider("TestCategory");
            Assert.That(trait, Is.Not.Null);
            trait = VsTestFilter.TraitProvider("Category");
            Assert.That(trait, Is.Not.Null);
        }

        [Test]
        public void TraitProviderWithNoCategory()
        {
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var trait = VsTestFilter.TraitProvider("JustKidding");
            Assert.That(trait, Is.Null);
        }

        [Test]
        public void PropertyValueProviderFqn()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "FullyQualifiedName");
            Assert.That(obj, Is.SameAs("Test1"));
        }

        [Test]
        public void PropertyValueProviderWithOneCategoryAndTestCategoryAsFilter()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.That(obj, Is.SameAs("CI"));
        }

        [Test]
        public void PropertyValueProviderWithOneCategoryAndCategoryAsFilter()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "Category");
            Assert.That(obj, Is.SameAs("CI"));
        }

        [Test]
        public void PropertyValueProviderWithNoTraitsAndTestCategoryAsFilter()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.IsNull(obj);
        }

        [Test]
        public void PropertyValueProviderWithNoTraitsAndCategoryAsFilter()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "Category");
            Assert.That(obj, Is.Null);
        }

        [Test]
        public void PropertyValueProviderWithMultipleCategoriesAndTestCategoryAsFilter()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            tc.AddTrait("Category", "MyOwn");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "TestCategory") as string[];
            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Length, Is.EqualTo(2));
            Assert.That(obj[0], Is.SameAs("CI"));
            Assert.That(obj[1], Is.SameAs("MyOwn"));
        }

        [Test]
        public void PropertyValueProviderWithMultipleCategoriesAndCategoryAsFilter()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            tc.AddTrait("Category", "MyOwn");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "Category") as string[];
            Assert.That(obj, Is.Not.Null);
            Assert.That(obj.Length, Is.EqualTo(2));
            Assert.That(obj[0], Is.SameAs("CI"));
            Assert.That(obj[1], Is.SameAs("MyOwn"));
        }

        [Test]
        public void PropertyValueProviderCategoryFail()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            var testFilter = VsTestFilterFactory.CreateVsTestFilter(settings, null);
            var obj = VsTestFilter.PropertyValueProvider(tc, "Garbage");
            Assert.That(obj, Is.Null);
        }


        [TestCase("CategoryThatMatchesNothing", new string[0])]
        [TestCase("AsmCat", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.dNUnitTest", "nUnitClassLibrary.ClassD.nUnitTest", "nUnitClassLibrary.NestedClasses.NC11", "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        [TestCase("BaseClass", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.dNUnitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("Base", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("DerivedClass", new[] { "nUnitClassLibrary.ClassD.dNUnitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("Derived", new[] { "nUnitClassLibrary.ClassD.dNUnitTest" })]
        [TestCase("NS1", new[] { "nUnitClassLibrary.NestedClasses.NC11" })]
        [TestCase("NS11", new[] { "nUnitClassLibrary.NestedClasses.NC11" })]
        [TestCase("NS2", new[] { "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        [TestCase("NS21", new[] { "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        public void CanFilterConvertedTestsByCategoryWithTestCategoryAsFilter(string category, IReadOnlyCollection<string> expectedMatchingTestNames)
        {
            FilteringTestUtils.AssertExpectedResult(
                TestDoubleFilterExpression.AnyIsEqualTo("TestCategory", category),
                TestCaseUtils.ConvertTestCases(FakeTestData.HierarchyTestXml),
                expectedMatchingTestNames);
        }

        [TestCase("CategoryThatMatchesNothing", new string[0])]
        [TestCase("AsmCat", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.dNUnitTest", "nUnitClassLibrary.ClassD.nUnitTest", "nUnitClassLibrary.NestedClasses.NC11", "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        [TestCase("BaseClass", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.dNUnitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("Base", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("DerivedClass", new[] { "nUnitClassLibrary.ClassD.dNUnitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("Derived", new[] { "nUnitClassLibrary.ClassD.dNUnitTest" })]
        [TestCase("NS1", new[] { "nUnitClassLibrary.NestedClasses.NC11" })]
        [TestCase("NS11", new[] { "nUnitClassLibrary.NestedClasses.NC11" })]
        [TestCase("NS2", new[] { "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        [TestCase("NS21", new[] { "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        public void CanFilterConvertedTestsByCategoryWithCategoryAsFilter(string category, IReadOnlyCollection<string> expectedMatchingTestNames)
        {
            FilteringTestUtils.AssertExpectedResult(
                TestDoubleFilterExpression.AnyIsEqualTo("Category", category),
                TestCaseUtils.ConvertTestCases(FakeTestData.HierarchyTestXml),
                expectedMatchingTestNames);
        }
    }
}
