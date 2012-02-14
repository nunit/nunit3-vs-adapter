// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;
using NUnit.Core.Builders;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NUnitTestHelperTests
    {
        private static readonly string mockAssemblyPath = Path.GetFullPath("mock-assembly.dll");

        private List<ITest> nunitTestCases;

        [TestFixtureSetUp]
        public void LoadMockAssembly()
        {
            TestAssemblyBuilder builder = new TestAssemblyBuilder();
            TestSuite suite = builder.Build(mockAssemblyPath, true);

            this.nunitTestCases = new List<ITest>();
            FindTestCases(suite);
        }

        private void FindTestCases(ITest test)
        {
            if (test.IsSuite)
                foreach (ITest child in test.Tests)
                    FindTestCases(child);
            else
                nunitTestCases.Add(test);
        }

        private ITest GetTest(string name)
        {
            return nunitTestCases.Find(tc => tc.TestName.Name == name);
        }

        [TestCase("MockTest3")]
        [TestCase("MethodWithParameters(9,11)")]
        public void CanMakeTestCaseFromNUnitTest(string testName)
        {
            var testConverter = new TestConverter(mockAssemblyPath);
            var test = GetTest(testName);
            var testCase = testConverter.ConvertTestCase(test);
            Assert.That(testCase.Name, Is.EqualTo(test.TestName.FullName));
            Assert.That(testCase.DisplayName, Is.EqualTo(test.TestName.Name));
            Assert.That(testCase.Source, Is.SamePath(mockAssemblyPath));
            Assert.That(testCase.ExecutorUri, Is.EqualTo(new Uri(NUnitTestExecutor.ExecutorUri)));
        }

        //[TestCase("MockTest3")]
        //[TestCase("MethodWithParameters(9,11)")]
        //public void CanMakeTestCaseFromNUnitTestSpecifyingAssembly(string testName)
        //{
        //    var test = GetTest(testName);
        //    var testCase = test.ToTestCase("some.assembly.dll");
        //    Assert.That(testCase.Name, Is.EqualTo(test.TestName.FullName));
        //    Assert.That(testCase.DisplayName, Is.EqualTo(test.TestName.Name));
        //    Assert.That(testCase.Source, Is.EqualTo("some.assembly.dll"));
        //    Assert.That(testCase.ExecutorUri, Is.EqualTo(new Uri(NUnitTestExecutor.ExecutorUri)));
        //}

        [TestCase("MockTest3", "NUnit.Tests.Assemblies.MockTestFixture")]
        [TestCase("MethodWithParameters(9,11)", "NUnit.Tests.FixtureWithTestCases")]
        public void CanGetClassNameForNUnitTest(string name, string className)
        {
            var test = GetTest(name);
            Assert.That(test.GetClassName(), Is.EqualTo(className));
        }

        [TestCase("MockTest3", "NUnit.Tests.Assemblies.MockTestFixture")]
        [TestCase("MethodWithParameters(9,11)", "NUnit.Tests.FixtureWithTestCases")]
        public void CanGetClassNameFromTestName(string name, string className)
        {
            var testName = GetTest(name).TestName;
            Assert.That(testName.GetClassName(), Is.EqualTo(className));
        }

        [TestCase("MockTest3", "MockTest3")]
        [TestCase("MethodWithParameters(9,11)", "MethodWithParameters")]
        public void CanGetMethodNameForNUnitTest(string name, string methodName)
        {
            var test = GetTest(name);
            Assert.That(test.GetMethodName(), Is.EqualTo(methodName));
        }

        [TestCase("MockTest3", "MockTest3")]
        [TestCase("MethodWithParameters(9,11)", "MethodWithParameters")]
        public void CanGetMethodNameFromTestName(string name, string methodName)
        {
            var testName = GetTest(name).TestName;
            Assert.That(testName.GetMethodName(), Is.EqualTo(methodName));
        }

        [TestCase("MockTest3")]
        [TestCase("MethodWithParameters(9,11)")]
        public void CanGetSourceAssembly(string testName)
        {
            var test = GetTest(testName);
            Assert.That(test.GetSourceAssembly(), Is.SamePath(mockAssemblyPath));
        }
       
        [TestCase(ResultState.Cancelled, Result=TestOutcome.None)]
        [TestCase(ResultState.Error, Result=TestOutcome.Failed)]
        [TestCase(ResultState.Failure, Result=TestOutcome.Failed)]
        [TestCase(ResultState.Ignored, Result=TestOutcome.Skipped)]
        [TestCase(ResultState.Inconclusive, Result=TestOutcome.None)]
        [TestCase(ResultState.NotRunnable, Result=TestOutcome.Failed)]
        [TestCase(ResultState.Skipped, Result=TestOutcome.Skipped)]
        [TestCase(ResultState.Success, Result=TestOutcome.Passed)]
        public TestOutcome CanConvertResultStateToTestOutcome(NUnit.Core.ResultState resultState)
        {
            return resultState.ToTestOutcome();
        }
    }
}
