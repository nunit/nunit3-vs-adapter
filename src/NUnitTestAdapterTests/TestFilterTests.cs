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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

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


        [TestCase("CategoryThatMatchesNothing", new string[0])]
        [TestCase("AsmCat", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.dNunitTest", "nUnitClassLibrary.ClassD.nUnitTest", "nUnitClassLibrary.NestedClasses.NC11", "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        [TestCase("BaseClass", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.dNunitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("Base", new[] { "nUnitClassLibrary.Class1.nUnitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("DerivedClass", new[] { "nUnitClassLibrary.ClassD.dNunitTest", "nUnitClassLibrary.ClassD.nUnitTest" })]
        [TestCase("Derived", new[] { "nUnitClassLibrary.ClassD.dNunitTest" })]
        [TestCase("NS1", new[] { "nUnitClassLibrary.NestedClasses.NC11" })]
        [TestCase("NS11", new[] { "nUnitClassLibrary.NestedClasses.NC11" })]
        [TestCase("NS2", new[] { "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        [TestCase("NS21", new[] { "nUnitClassLibrary.NestedClasses+NestedClass2.NC21" })]
        public static void CanFilterConvertedTestsByCategory(string category, IReadOnlyCollection<string> expectedMatchingTestNames)
        {
            var filter = CreateFilter(TestDoubleFilterExpression.AnyIsEqualTo("TestCategory", category));

            var matchingTestCases = filter.CheckFilter(GetConvertedTestCases());

            Assert.That(matchingTestCases.Select(t => t.FullyQualifiedName), Is.EquivalentTo(expectedMatchingTestNames));
        }

        private static IReadOnlyCollection<TestCase> GetConvertedTestCases()
        {
            var testConverter = new TestConverter(
                new TestLogger(new MessageLoggerStub()),
                FakeTestData.AssemblyPath,
                collectSourceInformation: true);

            return FakeTestData.GetTestNodes()
                .Cast<XmlNode>()
                .Select(testConverter.ConvertTestCase)
                .ToList();
        }

        private static TfsTestFilter CreateFilter(ITestCaseFilterExpression filterExpression)
        {
            var context = Substitute.For<IRunContext>();
            context.GetTestCaseFilter(null, null).ReturnsForAnyArgs(filterExpression);
            return new TfsTestFilter(context);
        }

        private sealed class TestDoubleFilterExpression : ITestCaseFilterExpression
        {
            private readonly Func<Func<string, object>, bool> predicate;

            public TestDoubleFilterExpression(string testCaseFilterValue, Func<Func<string, object>, bool> predicate)
            {
                TestCaseFilterValue = testCaseFilterValue;
                this.predicate = predicate;
            }

            public string TestCaseFilterValue { get; }

            public bool MatchTestCase(TestCase testCase, Func<string, object> propertyValueProvider)
            {
                return predicate.Invoke(propertyValueProvider);
            }

            public static TestDoubleFilterExpression AnyIsEqualTo(string propertyName, object value)
            {
                return new TestDoubleFilterExpression($"{propertyName}={value}", propertyValueProvider =>
                {
                    var list = propertyValueProvider.Invoke(propertyName) as IEnumerable;
                    return list == null ? false : list.Cast<object>().Contains(value);
                });
            }
        }
    }
}
