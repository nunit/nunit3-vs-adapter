
// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************
using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;


namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using System.Reflection;

    using NUnit.Core;
    using NUnit.Core.Filters;

    [TestFixture]
    public class AssemblyRunnerTests
    {
        private readonly static Uri EXECUTOR_URI = new Uri(NUnitTestExecutor.ExecutorUri);

        private void FakeTestMethod1()
        {
        }

        private void FakeTestMethod2()
        {
        }

        private ITest fakeTest1;

        private ITest fakeTest2;

        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod1 = this.GetType().GetMethod("FakeTestMethod1", BindingFlags.Instance | BindingFlags.NonPublic);
            this.fakeTest1 = new NUnitTestMethod(fakeTestMethod1);
            MethodInfo fakeTestMethod2 = this.GetType().GetMethod("FakeTestMethod2", BindingFlags.Instance | BindingFlags.NonPublic);
            this.fakeTest2 = new NUnitTestMethod(fakeTestMethod2);
       }



        [Test]
        public void VerifyConstruction1()
        {
            var runner = new AssemblyRunner(new TestLogger(), "test");
            Assert.That(runner.NUnitFilter.IsEmpty, Is.True);
        }

        [Test]
        public void VerifyConstruction2()
        {
            var t1 = new TestCase(fakeTest1.TestName.FullName, EXECUTOR_URI, "test");
            var t2 = new TestCase(fakeTest2.TestName.FullName, EXECUTOR_URI, "test");
            var list = new List<TestCase> { t1, t2 };
            var runner = new AssemblyRunner(new TestLogger(), "test", list);
            Assert.False(runner.NUnitFilter.IsEmpty);
            Assert.That(runner.NUnitFilter, Is.TypeOf<SimpleNameFilter>());
            Assert.True(runner.NUnitFilter.Match(fakeTest1));
            Assert.True(runner.NUnitFilter.Match(fakeTest2));
        }

        // TODO: Instead of using AddTestCases, we should be loading an actual assembly

        [Test]
        public void AddsNonFilteredCorrectly()
        {
            var runner = new AssemblyRunner(new TestLogger(), "test");
            runner.AddTestCases(fakeTest1);
            runner.AddTestCases(fakeTest2);
            Assert.That(runner.NUnitFilter.IsEmpty,Is.True,"NUnitfilter has been touched");
            Assert.That(runner.LoadedTestCases.Count,Is.EqualTo(2),"We should have had 2 test cases here");
        }

        [Test]
        public void AddsFilteredCorrectly()
        {
            var t1 = new TestCase(fakeTest1.TestName.FullName, EXECUTOR_URI, "test");
            var t2 = new TestCase(fakeTest2.TestName.FullName, EXECUTOR_URI, "test");
            var list = new List<TestCase> { t1, t2 };
            var runner = new AssemblyRunner(new TestLogger(), "test",list);
            runner.AddTestCases(fakeTest1);
            runner.AddTestCases(fakeTest2);
            Assert.That(runner.NUnitFilter.IsEmpty, Is.False, "NUnitfilter should not be empty, we have added testcases");
            Assert.That(runner.LoadedTestCases.Count, Is.EqualTo(2), "We should have had 2 converted MS test cases here");
        }
    }
}
