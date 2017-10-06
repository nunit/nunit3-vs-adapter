// ***********************************************************************
// Copyright (c) 2011-2015 Charlie Poole, Terje Sandstrom
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
using System.IO;
using System.Linq;
using System.Reflection;
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
            testConverter = new TestConverter(new TestLogger(new MessageLoggerStub(), 0), FakeTestData.AssemblyPath, collectSourceInformation: true);
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

            Assert.That(testConverter.AttributesCache.Keys.Count, Is.EqualTo(1));
            Assert.That(testConverter.AttributesCache["121"].Count, Is.EqualTo(1));
            var parentTrait = testConverter.AttributesCache["121"];
            Assert.That(parentTrait[0].Name, Is.EqualTo("Category"));
            Assert.That(parentTrait[0].Value, Is.EqualTo("super"));
        }

        [Test]
        public void CanMakeTestCaseShouldBuildTraitsCache()
        {
            var xmlNodeList = FakeTestData.GetTestNodes();

            foreach(XmlNode node in xmlNodeList)
            {
                var testCase = testConverter.ConvertTestCase(node);
            }

            var attributesCache = testConverter.AttributesCache;

            // There are 12 ids in the TestXml2, but will be storing only ancestor properties.
            // Not the leaf node test-case properties.
            Assert.That(attributesCache.Keys.Count, Is.EqualTo(7));

            // Even though ancestor doesn't have any properties. Will be storing their ids.
            // So that we will not make call SelectNodes call again.
            CheckNodesWithNoProperties(attributesCache);

            // Will not be storing leaf nodes test-case nodes in the cache.
            CheckNoTestCaseNodesExist(attributesCache);

            // Checking assembly level attribute.
            CheckNodeProperties(attributesCache, "0-1009", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Category", "AsmCat") });

            // Checking Class level attributes base class & dervied class
            CheckNodeProperties(attributesCache, "0-1000", new KeyValuePair<string,string>[] { new KeyValuePair<string, string>("Category", "BaseClass") });
            CheckNodeProperties(attributesCache, "0-1002", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Category", "DerivedClass"), new KeyValuePair<string, string>("Category", "BaseClass") });

            // Checking Nested class attributes.
            CheckNodeProperties(attributesCache, "0-1005", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Category", "NS1") });
            CheckNodeProperties(attributesCache, "0-1007", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("Category", "NS2") });

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
            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            // This should put the TestCase in the cache
            var cachedTestCase = testConverter.ConvertTestCase(fakeTestNode);
            var fakeResultNode = FakeTestData.GetResultNode();

            var testResult = testConverter.GetVSTestResults(fakeResultNode)[0];
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

                var traitList = testCase.GetTraits().Select(trait => trait.Name + ":" + trait.Value).ToList();
                Assert.That(traitList, Is.EquivalentTo(new[] { "Category:super", "Category:cat1", "Priority:medium" }));
        }

        private void CheckNodesWithNoProperties(IDictionary<string, List<Trait>> attributesCache)
        {
            Assert.That(attributesCache["2"].Count, Is.EqualTo(0));
            Assert.That(attributesCache["0-1010"].Count, Is.EqualTo(0));
        }

        private void CheckNoTestCaseNodesExist(IDictionary<string, List<Trait>> attributesCache)
        {
            Assert.That(!attributesCache.ContainsKey("0-1008"));
            Assert.That(!attributesCache.ContainsKey("0-1006"));
            Assert.That(!attributesCache.ContainsKey("0-1004"));
            Assert.That(!attributesCache.ContainsKey("0-1003"));
            Assert.That(!attributesCache.ContainsKey("0-1001"));
        }

        private void CheckNodeProperties(IDictionary<string, List<Trait>> attributesCache, string id, KeyValuePair<string,string>[] kps)
        {
            Assert.That(attributesCache.ContainsKey(id));
            Assert.That(attributesCache[id].Count, Is.EqualTo(kps.Count()));
            var traits = attributesCache[id];

            foreach(var kp in kps)
            {
                Assert.That(traits.Any(t => t.Name == kp.Key && t.Value == kp.Value));
            }
        }
    }
}
