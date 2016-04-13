// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using Fakes;

    [Category("TestDiscovery")]
    public class TestDiscoveryTests : ITestCaseDiscoverySink
    {
        static readonly string MockAssemblyPath = 
            Path.Combine(TestContext.CurrentContext.TestDirectory, "mock-assembly.dll");

        static readonly List<TestCase> TestCases = new List<TestCase>();

        private static ITestDiscoverer nunittestDiscoverer;

        [OneTimeSetUp]
        public void LoadMockassembly()
        {
            // Sanity check to be sure we have the correct version of mock-assembly.dll
            Assert.That(NUnit.Tests.Assemblies.MockAssembly.Tests, Is.EqualTo(31),
                "The reference to mock-assembly.dll appears to be the wrong version");

            // Load the NUnit mock-assembly.dll once for this test, saving
            // the list of test cases sent to the discovery sink
            nunittestDiscoverer = ((ITestDiscoverer)new NUnit3TestDiscoverer());
            nunittestDiscoverer.DiscoverTests(
                new[] { MockAssemblyPath}, 
                new FakeDiscoveryContext(), 
                new MessageLoggerStub(), 
                this);
        }

        [Test]
        public void VerifyTestCaseCount()
        {
            Assert.That(TestCases.Count, Is.EqualTo(NUnit.Tests.Assemblies.MockAssembly.Tests));
        }

        [TestCase("MockTest3", "NUnit.Tests.Assemblies.MockTestFixture.MockTest3")]
        [TestCase("MockTest4", "NUnit.Tests.Assemblies.MockTestFixture.MockTest4")]
        [TestCase("ExplicitlyRunTest", "NUnit.Tests.Assemblies.MockTestFixture.ExplicitlyRunTest")]
        [TestCase("MethodWithParameters(9,11)", "NUnit.Tests.FixtureWithTestCases.MethodWithParameters(9,11)")]
        public void VerifyTestCaseIsFound(string name, string fullName)
        {
            var testCase = TestCases.Find(tc => tc.DisplayName == name);
            Assert.That(testCase.FullyQualifiedName, Is.EqualTo(fullName));
        }

        #region ITestCaseDiscoverySink Methods

        void ITestCaseDiscoverySink.SendTestCase(TestCase discoveredTest)
        {
            TestCases.Add(discoveredTest);
        }

        #endregion
    }

    [Category("TestDiscovery")]
    public class EmptyAssemblyDiscoveryTests : ITestCaseDiscoverySink
    {
        static readonly string EmptyAssemblyPath = 
            Path.Combine(TestContext.CurrentContext.TestDirectory, "empty-assembly.dll");

        private static ITestDiscoverer nunittestDiscoverer;

        [Test]
        public void VerifyLoading()
        {
            // Load the NUnit empty-assembly.dll once for this test
            nunittestDiscoverer = ((ITestDiscoverer)new NUnit3TestDiscoverer());
            nunittestDiscoverer.DiscoverTests(
                new[] { EmptyAssemblyPath}, 
                new FakeDiscoveryContext(), 
                new MessageLoggerStub(), 
                this);
        }

         #region ITestCaseDiscoverySink Methods

        void ITestCaseDiscoverySink.SendTestCase(TestCase discoveredTest)
        {
        }

        #endregion
    }
}
