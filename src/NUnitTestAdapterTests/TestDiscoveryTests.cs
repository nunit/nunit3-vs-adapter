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

using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using NSubstitute;

using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using Fakes;

    internal static class TestDiscoveryDataProvider
    {
        public static IEnumerable<IDiscoveryContext> TestDiscoveryData()
        {
            // yield return new FakeDiscoveryContext(null);
            yield return new FakeDiscoveryContext(new FakeRunSettings());
        }
    }

    [Ignore("These tests needs to rewritten as isolated tests")]
    [Category("TestDiscovery")]
    public class TestDiscoveryTests : ITestCaseDiscoverySink
    {
        static readonly string MockAssemblyPath =
            Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");

        private readonly List<TestCase> testCases = new();

        private readonly IDiscoveryContext _context;

        public TestDiscoveryTests()
        {
            _context = Substitute.For<IDiscoveryContext>();
        }

        [OneTimeSetUp]
        public void LoadMockassembly()
        {
            // Sanity check to be sure we have the correct version of mock-assembly.dll.
            Assert.That(NUnit.Tests.Assemblies.MockAssembly.TestsAtRuntime, Is.EqualTo(NUnit.Tests.Assemblies.MockAssembly.Tests),
                "The reference to mock-assembly.dll appears to be the wrong version");
            Assert.That(File.Exists(MockAssemblyPath), $"Can't locate mock-assembly.dll at {MockAssemblyPath}");
            var runsettings = "<RunSettings><NUnit><UseParentFQNForParametrizedTests>True</UseParentFQNForParametrizedTests><SkipNonTestAssemblies>false</SkipNonTestAssemblies><DiscoveryMethod>Legacy</DiscoveryMethod></NUnit></RunSettings>";
            var rs = Substitute.For<IRunSettings>();
            rs.SettingsXml.Returns(runsettings);
            _context.RunSettings.Returns(rs);
            // Load the NUnit mock-assembly.dll once for this test, saving
            // the list of test cases sent to the discovery sink.
            var discoverer = TestAdapterUtils.CreateDiscoverer();
            discoverer.DiscoverTests(
                new[] { MockAssemblyPath },
                _context,
                new MessageLoggerStub(),
                this);
        }

        [Test]
        public void VerifyTestCaseCount()
        {
            Assert.That(testCases.Count, Is.EqualTo(NUnit.Tests.Assemblies.MockAssembly.Tests));
        }

        [TestCase("MockTest3", "NUnit.Tests.Assemblies.MockTestFixture.MockTest3")]
        [TestCase("MockTest4", "NUnit.Tests.Assemblies.MockTestFixture.MockTest4")]
        [TestCase("ExplicitlyRunTest", "NUnit.Tests.Assemblies.MockTestFixture.ExplicitlyRunTest")]
        [TestCase("MethodWithParameters(9,11)", "NUnit.Tests.FixtureWithTestCases.MethodWithParameters", true)]
        public void VerifyTestCaseIsFound(string name, string fullName, bool parentFQN = false)
        {
            var testCase = testCases.Find(tc => tc.DisplayName == name);
            Assert.That(testCase.FullyQualifiedName, Is.EqualTo(fullName));
        }

        [Category("Navigation")]
        [TestCase("NestedClassTest1")] // parent
        [TestCase("NestedClassTest2")] // child
        [TestCase("NestedClassTest3")] // grandchild
        public void VerifyNestedTestCaseSourceIsAvailable(string name)
        {
            var testCase = testCases.Find(tc => tc.DisplayName == name);

            Assert.That(!string.IsNullOrEmpty(testCase.Source));
            Assert.That(testCase.LineNumber, Is.GreaterThan(0));
        }

        #region ITestCaseDiscoverySink Methods

        void ITestCaseDiscoverySink.SendTestCase(TestCase discoveredTest)
        {
            testCases.Add(discoveredTest);
        }

        #endregion
    }

    [Category("TestDiscovery")]
    public class EmptyAssemblyDiscoveryTests : ITestCaseDiscoverySink
    {
        static readonly string EmptyAssemblyPath =
            Path.Combine(TestContext.CurrentContext.TestDirectory, "empty-assembly.dll");

        [TestCaseSource(typeof(TestDiscoveryDataProvider), nameof(TestDiscoveryDataProvider.TestDiscoveryData))]
        public void VerifyLoading(IDiscoveryContext context)
        {
            // Load the NUnit empty-assembly.dll once for this test
            TestAdapterUtils.CreateDiscoverer().DiscoverTests(
                new[] { EmptyAssemblyPath },
                context,
                new MessageLoggerStub(),
                this);
        }

        #region ITestCaseDiscoverySink Methods

        void ITestCaseDiscoverySink.SendTestCase(TestCase discoveredTest)
        {
        }

        #endregion
    }

    [Category("TestDiscovery")]
    public class FailuresInDiscovery : ITestCaseDiscoverySink
    {
        bool testcaseWasSent;


        [SetUp]
        public void Setup()
        {
            testcaseWasSent = false;
        }

        [Test]
        public void WhenAssemblyDontExist()
        {
            int noOfMessagesFound = 3; // Start + end, + info
            var context = new FakeDiscoveryContext(null);
            var messageLoggerStub = new MessageLoggerStub();
            TestAdapterUtils.CreateDiscoverer().DiscoverTests(
                    new[] { "FileThatDoesntExist.dll" },
                    context,
                    messageLoggerStub,
                    this);
            Assert.Multiple(() =>
            {
                Assert.That(messageLoggerStub.Count, Is.EqualTo(noOfMessagesFound));
                Assert.That(messageLoggerStub.LatestTestMessageLevel, Is.EqualTo(TestMessageLevel.Informational));
                Assert.That(testcaseWasSent, Is.False);
                Assert.That(!messageLoggerStub.ErrorMessages.Any());
                Assert.That(!messageLoggerStub.WarningMessages.Any());
            });
        }

#if NET462
        [Test]
        public void WhenAssemblyIsNative()
        {
            var context = new FakeDiscoveryContext(null);
            var messageLoggerStub = new MessageLoggerStub();
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "NativeTests.dll");
            Assert.That(File.Exists(path));
            TestAdapterUtils.CreateDiscoverer().DiscoverTests(
                new[] { path },
                context,
                messageLoggerStub,
                this);
            Assert.Multiple(() =>
            {
                Assert.That(testcaseWasSent, Is.False);
                Assert.That(messageLoggerStub.WarningMessages.Count(), Is.EqualTo(1));
                Assert.That(!messageLoggerStub.ErrorMessages.Any());
                string warningmsg = messageLoggerStub.WarningMessages.Select(o => o.Item2).FirstOrDefault();
                Assert.That(warningmsg, Is.Not.Null);
                if (!string.IsNullOrEmpty(warningmsg))
                    Assert.That(warningmsg, Does.Contain("Assembly not supported"));
            });
        }
#endif

        public void SendTestCase(TestCase discoveredTest)
        {
            testcaseWasSent = true;
        }
    }
}
