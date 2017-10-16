// ***********************************************************************
// Copyright (c) 2011-2017 Charlie Poole, Terje Sandstrom
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
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            tc.AddTrait("Category", "CI");
            var testFilter = new TfsTestFilter(null);
            var obj = TfsTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.AreSame("CI", obj);
        }

        [Test]
        public void PropertyValueProviderCategoryWithNoTraits()
        {
            var tc = new TestCase("Test1", new Uri("executor://NUnitTestExecutor"), "NUnit.VSIX");
            var testFilter = new TfsTestFilter(null);
            var obj = TfsTestFilter.PropertyValueProvider(tc, "TestCategory");
            Assert.IsNull(obj);
        }

        [Test]
        public void PropertyValueProviderCategoryWithMultipleCategories()
        {
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
    }
}
