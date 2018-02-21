// ***********************************************************************
// Copyright (c) 2011-2018 Charlie Poole, Terje Sandstrom
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
using System.Linq;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using Fakes;

    [Category("TestConverter")]
    public class TestConverterTests
    {
        private XmlNode fakeTestNode;
        private TestConverter testConverter;

        [SetUp]
        public void SetUp()
        {
            fakeTestNode = FakeTestData.GetTestNode();
            testConverter = new TestConverter(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath, collectSourceInformation: true);
        }

        [TearDown]
        public void TearDown()
        {
            testConverter.Dispose();
        }

        [Test]
        public void CanMakeTestCaseFromTest()
        {
            var testCase = testConverter.ConvertTestCase(fakeTestNode);
            CheckTestCase(testCase);
        }

        [Test]
        public void CanMakeTestCaseFromTestWithCache()
        {

            var testCase = testConverter.ConvertTestCase(fakeTestNode);

            CheckTestCase(testCase);

            CheckNodeProperties(testConverter.TraitsCache, "121", categories: new[] { "super" });
        }

        [Test]
        public void CanMakeTestCaseShouldBuildTraitsCache()
        {
            var xmlNodeList = FakeTestData.GetTestNodes();

            foreach(XmlNode node in xmlNodeList)
            {
                var testCase = testConverter.ConvertTestCase(node);
            }

            var traitsCache = testConverter.TraitsCache;

            // There are 12 ids in the TestXml2, but will be storing only ancestor properties.
            // Not the leaf node test-case properties.
            Assert.That(traitsCache.Keys.Count, Is.EqualTo(7));

            // Even though ancestor doesn't have any properties. Will be storing their ids.
            // So that we will not make call SelectNodes call again.
            CheckNodeProperties(traitsCache, "2");

            // Will not be storing leaf nodes test-case nodes in the cache.
            CheckNoTestCaseNodesExist(traitsCache);

            // Checking assembly and namespace level attributes.
            CheckNodeProperties(traitsCache, "0-1009", categories: new[] { "AsmCat" });
            CheckNodeProperties(traitsCache, "0-1010", categories: new[] { "AsmCat" });

            // Checking Class level attributes base class & derived class
            CheckNodeProperties(traitsCache, "0-1000", categories: new[] { "AsmCat", "BaseClass" });
            CheckNodeProperties(traitsCache, "0-1002", categories: new[] { "AsmCat", "DerivedClass", "BaseClass" });

            // Checking Nested class attributes.
            CheckNodeProperties(traitsCache, "0-1005", categories: new[] { "AsmCat", "NS1" });
            CheckNodeProperties(traitsCache, "0-1007", categories: new[] { "AsmCat", "NS2" });
        }

        [Test]
        public void ConvertedTestCaseIsCached()
        {
            testConverter.ConvertTestCase(fakeTestNode);
            var testCase = testConverter.GetCachedTestCase("123");

            CheckTestCase(testCase);
        }

        [Test]
        public void CannotMakeTestResultWhenTestCaseIsNotInCache()
        {
            var fakeResultNode = FakeTestData.GetResultNode();
            var results = testConverter.GetVSTestResults(fakeResultNode);
            Assert.That(results.TestResults.Count, Is.EqualTo(0));
        }

        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            // This should put the TestCase in the cache
            var cachedTestCase = testConverter.ConvertTestCase(fakeTestNode);
            var fakeResultNode = FakeTestData.GetResultNode();

            var testResults = testConverter.GetVSTestResults(fakeResultNode);
            var testResult = testResults.TestResults[0];
            var testCase = testResult.TestCase;

            Assert.That(testCase, Is.SameAs(cachedTestCase));

            CheckTestCase(testCase);

            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Passed));
            Assert.That(testResult.ErrorMessage, Is.EqualTo("It passed!"));
            Assert.That(testResult.Duration, Is.EqualTo(TimeSpan.FromSeconds(1.234)));
        }

        private void CheckTestCase(TestCase testCase)
        {
            Assert.That(testCase.FullyQualifiedName, Is.EqualTo(FakeTestData.FullyQualifiedName));
            Assert.That(testCase.DisplayName, Is.EqualTo(FakeTestData.DisplayName));
            Assert.That(testCase.Source, Is.SamePath(FakeTestData.AssemblyPath));

            if (testCase.CodeFilePath != null) // Unavailable if not running under VS
            {
                Assert.That(testCase.CodeFilePath, Is.SamePath(FakeTestData.CodeFile));
                Assert.That(testCase.LineNumber, Is.EqualTo(FakeTestData.LineNumber));
            }

            CheckTraitInfo(testCase,
                traits: new[] { new Trait("Priority", "medium") },
                categories: new[] { "super", "cat1" });
        }

        private void CheckNoTestCaseNodesExist(IDictionary<string, TestTraitInfo> traits)
        {
            Assert.That(!traits.ContainsKey("0-1008"));
            Assert.That(!traits.ContainsKey("0-1006"));
            Assert.That(!traits.ContainsKey("0-1004"));
            Assert.That(!traits.ContainsKey("0-1003"));
            Assert.That(!traits.ContainsKey("0-1001"));
        }

        private void CheckNodeProperties(IDictionary<string, TestTraitInfo> traitsCache, string id, IEnumerable<Trait> traits = null, IEnumerable<string> categories = null)
        {
            Assert.That(traitsCache, Contains.Key(id));
            CheckTraitInfo(traitsCache[id], traits, categories);
        }

        private void CheckTraitInfo(TestCase testCase, IEnumerable<Trait> traits = null, IEnumerable<string> categories = null)
        {
            CheckTraitInfo(TestTraitInfo.FromTestCase(testCase), traits, categories);
        }

        private void CheckTraitInfo(TestTraitInfo traitInfo, IEnumerable<Trait> traits = null, IEnumerable<string> categories = null)
        {
            Assert.That(traitInfo, Has.Property("Traits")
                .EquivalentTo(traits ?? Enumerable.Empty<Trait>())
                .Using(TraitComparer.Instance));

            Assert.That(traitInfo, Has.Property("Categories")
                .EquivalentTo(categories ?? Enumerable.Empty<string>()));
        }
    }
}
