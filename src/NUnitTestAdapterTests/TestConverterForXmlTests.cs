// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, Terje Sandstrom
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
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using Fakes;

    [Category("TestConverter")]
    public class TestConverterForXmlTests
    {
        private NUnitEventTestCase fakeTestNode;
        private TestConverterForXml testConverterForXml;

        [SetUp]
        public void SetUp()
        {
            fakeTestNode = new NUnitEventTestCase(FakeTestData.GetTestNode());
            var settings = Substitute.For<IAdapterSettings>();
            settings.ConsoleOut.Returns(0);
            settings.UseTestNameInConsoleOutput.Returns(false);
            settings.CollectSourceInformation.Returns(true);
            testConverterForXml = new TestConverterForXml(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath, settings);
        }

        [TearDown]
        public void TearDown()
        {
            testConverterForXml.Dispose();
        }

        [Test]
        public void CanMakeTestCaseFromTest()
        {
            var testCase = testConverterForXml.ConvertTestCase(fakeTestNode);
            CheckTestCase(testCase);
        }

        [Test]
        public void CanMakeTestCaseFromTestWithCache()
        {
            var testCase = testConverterForXml.ConvertTestCase(fakeTestNode);

            CheckTestCase(testCase);

            Assert.That(testConverterForXml.TraitsCache.Keys.Count, Is.EqualTo(1));
            Assert.That(testConverterForXml.TraitsCache["121"].Traits.Count, Is.EqualTo(1));
            var parentTrait = testConverterForXml.TraitsCache["121"].Traits;
            Assert.That(parentTrait[0].Name, Is.EqualTo("Category"));
            Assert.That(parentTrait[0].Value, Is.EqualTo("super"));
        }

        [Test]
        public void CanMakeTestCaseShouldBuildTraitsCache()
        {
            var xmlNodeList = FakeTestData.GetTestNodes();

            foreach (XmlNode node in xmlNodeList)
            {
                testConverterForXml.ConvertTestCase(new NUnitEventTestCase(node));
            }

            var traitsCache = testConverterForXml.TraitsCache;

            // There are 12 ids in the TestXml2, but will be storing only ancestor properties.
            // Not the leaf node test-case properties.
            Assert.That(traitsCache.Keys.Count, Is.EqualTo(7));

            // Even though ancestor doesn't have any properties. Will be storing their ids.
            // So that we will not make call SelectNodes call again.
            CheckNodesWithNoProperties(traitsCache);

            // Will not be storing leaf nodes test-case nodes in the cache.
            CheckNoTestCaseNodesExist(traitsCache);

            // Checking assembly level attribute.
            CheckNodeProperties(traitsCache, "0-1009", new[] { new KeyValuePair<string, string>("Category", "AsmCat") });

            // Checking Class level attributes base class & dervied class
            CheckNodeProperties(traitsCache, "0-1000", new[] { new KeyValuePair<string, string>("Category", "BaseClass") });
            CheckNodeProperties(traitsCache, "0-1002", new[] { new KeyValuePair<string, string>("Category", "DerivedClass"), new KeyValuePair<string, string>("Category", "BaseClass") });

            // Checking Nested class attributes.
            CheckNodeProperties(traitsCache, "0-1005", new[] { new KeyValuePair<string, string>("Category", "NS1") });
            CheckNodeProperties(traitsCache, "0-1007", new[] { new KeyValuePair<string, string>("Category", "NS2") });
        }

        [Test]
        public void ConvertedTestCaseIsCached()
        {
            testConverterForXml.ConvertTestCase(fakeTestNode);
            var testCase = testConverterForXml.GetCachedTestCase("123");

            CheckTestCase(testCase);
        }

        [Test]
        public void CannotMakeTestResultWhenTestCaseIsNotInCache()
        {
            var fakeResultNode = new NUnitTestEventTestCase(FakeTestData.GetResultNode());
            var results = testConverterForXml.GetVsTestResults(fakeResultNode, Enumerable.Empty<INUnitTestEventTestOutput>().ToList());
            Assert.That(results.TestResults.Count, Is.EqualTo(0));
        }

        [Test]
        public void CanMakeTestResultFromNUnitTestResult()
        {
            // This should put the TestCase in the cache
            var cachedTestCase = testConverterForXml.ConvertTestCase(fakeTestNode);
            var fakeResultNode = new NUnitTestEventTestCase(FakeTestData.GetResultNode());

            var testResults = testConverterForXml.GetVsTestResults(fakeResultNode, Enumerable.Empty<INUnitTestEventTestOutput>().ToList());
            var testResult = testResults.TestResults[0];
            var testCase = testResult.TestCase;

            Assert.That(testCase, Is.SameAs(cachedTestCase));

            CheckTestCase(testCase);

            Assert.That(testResult.Outcome, Is.EqualTo(TestOutcome.Passed));
            Assert.That(testResult.ErrorMessage, Is.EqualTo(null));
            Assert.That(testResult.Duration, Is.EqualTo(TimeSpan.FromSeconds(1.234)));
        }

        [TestCase(
            "<test-output stream=\"Error\" testid=\"0-1001\" testname=\"UnitTests.Test1\"><![CDATA[some stdErr]]></test-output>",
            "StdErrMsgs:some stdErr")]

        [TestCase(
            "<test-output stream=\"Progress\" testid=\"0-1001\" testname=\"UnitTests.Test1\"><![CDATA[some text]]></test-output>",
            "")]

        [TestCase(
            "<test-output stream=\"Error\" testid=\"0-1001\" testname=\"UnitTests.Test1\"><![CDATA[some stdErr]]></test-output>"
            + ";<test-output stream=\"Progress\" testid=\"0-1001\" testname=\"UnitTests.Test1\"><![CDATA[some text]]></test-output>",
            "StdErrMsgs:some stdErr")]
        public void CanMakeTestResultFromNUnitTestResult2(string output, string expectedMessages)
        {
            var cachedTestCase = testConverterForXml.ConvertTestCase(fakeTestNode);
            var fakeResultNode = new NUnitTestEventTestCase(FakeTestData.GetResultNode());
            var outputNodes = output.Split(';').Select(i => new NUnitTestEventTestOutput(XmlHelper.CreateXmlNode(i.Trim()))).ToList();
            var outputNodesCollection = new List<INUnitTestEventTestOutput>(outputNodes);
            var testResults = testConverterForXml.GetVsTestResults(fakeResultNode, outputNodesCollection);
            var testResult = testResults.TestResults[0];
            var actualMessages = string.Join(";", testResult.Messages.Select(i => i.Category + ":" + i.Text));

            Assert.That(actualMessages, Is.EqualTo(expectedMessages));
        }

        #region Attachment tests

        [Test]
        public void Attachments_CorrectAmountOfConvertedAttachments()
        {
            var cachedTestCase = testConverterForXml.ConvertTestCase(fakeTestNode);
            var fakeResultNode = new NUnitTestEventTestCase(FakeTestData.GetResultNode());

            var testResults = testConverterForXml.GetVsTestResults(fakeResultNode, Enumerable.Empty<INUnitTestEventTestOutput>().ToList());

            var fakeAttachments = fakeResultNode.NUnitAttachments
                .Where(n => !string.IsNullOrEmpty(n.FilePath))
                .ToArray();

            var convertedAttachments = testResults.TestResults
                .SelectMany(tr => tr.Attachments.SelectMany(ats => ats.Attachments))
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(convertedAttachments.Length, Is.GreaterThan(0), "Some converted attachments were expected");
                Assert.That(convertedAttachments.Length, Is.EqualTo(fakeAttachments.Length), "Attachments are not converted");
            });
        }

        [Test]
        public void Attachments_AllFilePathesStartWithFileScheme()
        {
            const string fileUriScheme = "file://";
            const string errorMessage = "Path must start with file:// uri scheme";

            var cachedTestCase = testConverterForXml.ConvertTestCase(fakeTestNode);
            var fakeResultNode = new NUnitTestEventTestCase(FakeTestData.GetResultNode().AsString());

            var testResults = testConverterForXml.GetVsTestResults(fakeResultNode, Enumerable.Empty<INUnitTestEventTestOutput>().ToList());

            var convertedAttachments = testResults.TestResults
                .SelectMany(tr => tr.Attachments.SelectMany(ats => ats.Attachments))
                .ToArray();

            foreach (var attachment in convertedAttachments)
            {
                var originalPath = attachment.Uri.OriginalString;
                Assert.That(originalPath.LastIndexOf(fileUriScheme), Is.EqualTo(0), errorMessage);
            }
        }

        #endregion Attachment tests

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
            Assert.That(traitList, Is.EquivalentTo(new[] { "Priority:medium" }));
            Assert.That(testCase.GetCategories(), Is.EquivalentTo(new[] { "super", "cat1", }));
        }

        private void CheckNodesWithNoProperties(IDictionary<string, TraitsFeature.CachedTestCaseInfo> cache)
        {
            Assert.That(cache["2"].Traits.Count, Is.EqualTo(0));
            Assert.That(cache["0-1010"].Traits.Count, Is.EqualTo(0));
        }

        private void CheckNoTestCaseNodesExist(IDictionary<string, TraitsFeature.CachedTestCaseInfo> cache)
        {
            Assert.That(!cache.ContainsKey("0-1008"));
            Assert.That(!cache.ContainsKey("0-1006"));
            Assert.That(!cache.ContainsKey("0-1004"));
            Assert.That(!cache.ContainsKey("0-1003"));
            Assert.That(!cache.ContainsKey("0-1001"));
        }

        private void CheckNodeProperties(IDictionary<string, TraitsFeature.CachedTestCaseInfo> cache, string id, KeyValuePair<string, string>[] kps)
        {
            Assert.That(cache.ContainsKey(id));
            Assert.That(cache[id].Traits.Count, Is.EqualTo(kps.Count()));
            var info = cache[id];

            foreach (var kp in kps)
            {
                Assert.That(info.Traits.Any(t => t.Name == kp.Key && t.Value == kp.Value));
            }
        }

        [Description("Third-party runners may opt to depend on this. https://github.com/nunit/nunit3-vs-adapter/issues/487#issuecomment-389222879")]
        [TestCase("NonExplicitParent.ExplicitTest")]
        [TestCase("ExplicitParent.NonExplicitTest")]
        public static void NUnitExplicitBoolPropertyIsProvidedForThirdPartyRunnersInExplicitTestCases(string testName)
        {
            var testCase = GetSampleTestCase(testName);

            var property = testCase.Properties.Single(p =>
                p.Id == "NUnit.Explicit"
                && p.GetValueType() == typeof(bool));

            Assert.That(testCase.GetPropertyValue(property), Is.True);
        }

        [TestCase("NonExplicitParent.NonExplicitTestWithExplicitCategory")]
        public static void NUnitExplicitBoolPropertyIsNotProvidedForThirdPartyRunnersInNonExplicitTestCases(string testName)
        {
            var testCase = GetSampleTestCase(testName);

            Assert.That(testCase, Has.Property("Properties").With.None.With.Property("Id").EqualTo("NUnit.Explicit"));
        }

        private static TestCase GetSampleTestCase(string fullyQualifiedName)
        {
            return TestCaseUtils.ConvertTestCases(@"
                <test-suite id='1' name='NonExplicitParent' fullname='NonExplicitParent'>
                    <test-case id='2' name='NonExplicitTest' fullname='NonExplicitParent.NonExplicitTestWithExplicitCategory'>
                        <properties>
                            <property name='Category' value='Explicit' />
                        </properties>
                    </test-case>
                    <test-case id='3' name='ExplicitTest' fullname='NonExplicitParent.ExplicitTest' runstate='Explicit' />
                </test-suite>
                <test-suite id='4' name='ExplicitParent' fullname='ExplicitParent' runstate='Explicit'>
                    <test-case id='5' name='NonExplicitTest' fullname='ExplicitParent.NonExplicitTest' />
                </test-suite>").Single(t => t.FullyQualifiedName == fullyQualifiedName);
        }
    }
}
