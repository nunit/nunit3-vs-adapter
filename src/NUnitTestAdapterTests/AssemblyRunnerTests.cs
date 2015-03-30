﻿// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture]
    public class AssemblyRunnerTests
    {
        [SetUp]
        public void SetUp()
        {
            //MethodInfo fakeTestMethod1 = GetType()
            //    .GetMethod("FakeTestMethod1", BindingFlags.Instance | BindingFlags.NonPublic);
            //fakeTest1 = new NUnitTestMethod(fakeTestMethod1);
            //MethodInfo fakeTestMethod2 = GetType()
            //    .GetMethod("FakeTestMethod2", BindingFlags.Instance | BindingFlags.NonPublic);
            //fakeTest2 = new NUnitTestMethod(fakeTestMethod2);
        }

        private static readonly Uri ExecutorUri = new Uri(NUnit3TestExecutor.ExecutorUri);

// ReSharper disable once UnusedMember.Local
        private void FakeTestMethod1()
        {
        }

// ReSharper disable once UnusedMember.Local
        private void FakeTestMethod2()
        {
        }

        //[Test]
        //public void AddsFilteredCorrectly()
        //{
        //    var t1 = new TestCase(fakeTest1.TestName.FullName, ExecutorUri, "test");
        //    var t2 = new TestCase(fakeTest2.TestName.FullName, ExecutorUri, "test");
        //    var list = new List<TestCase> {t1, t2};
        //    var runner = new AssemblyRunner(new TestLogger(), "test", list);
        //    runner.AddTestCases(fakeTest1);
        //    runner.AddTestCases(fakeTest2);
        //    Assert.That(runner.NUnitFilter.IsEmpty, Is.False, "NUnitfilter should not be empty, we have added testcases");
        //    Assert.That(runner.LoadedTestCases.Count, Is.EqualTo(2), "We should have had 2 converted MS test cases here");
        //}

        //[Test]
        //public void AddsNonFilteredCorrectly()
        //{
        //    var runner = new AssemblyRunner(new TestLogger(), "test");
        //    runner.AddTestCases(fakeTest1);
        //    runner.AddTestCases(fakeTest2);
        //    Assert.That(runner.NUnitFilter.IsEmpty, Is.True, "NUnitfilter has been touched");
        //    Assert.That(runner.LoadedTestCases.Count, Is.EqualTo(2), "We should have had 2 test cases here");
        //}


        [Test]
        public void VerifyConstruction1()
        {
            var runner = new AssemblyRunner(new TestLogger(), "test");
            Assert.That(runner.NUnitFilter.Xml.ChildNodes.Count, Is.EqualTo(0));
        }

        [Test]
        public void VerifyConstruction2()
        {
            var t1 = new TestCase("Test1", ExecutorUri, "test");
            var t2 = new TestCase("Test2", ExecutorUri, "test");
            var cases = new List<TestCase> { t1, t2 };
            var runner = new AssemblyRunner(new TestLogger(), "test", cases);
            var tests = runner.NUnitFilter.Xml.SelectNodes("tests/test");

            var names = new List<string>();
            foreach (XmlNode testNode in tests)
                names.Add(testNode.InnerText);

            Assert.That(names, Is.EquivalentTo(new string[] { "Test1", "Test2" }));
        }

        // TODO: Instead of using AddTestCases, we should be loading an actual assembly

        //[Test]
        //public void HandleTfsFilterCorrectlyWhenFilterIsEmpty()
        //{
        //    var tfsfilter = new Mock<ITfsTestFilter>();
        //    tfsfilter.Setup(f => f.HasTfsFilterValue).Returns(false);
        //    var runner = new AssemblyRunner(new TestLogger(), "test", tfsfilter.Object);
        //    runner.AddTestCases(fakeTest1);
        //    runner.AddTestCases(fakeTest2);

        //    Assert.That(runner.NUnitFilter.IsEmpty, Is.False, "NUnitfilter should not be empty, we have added testcases");
        //    Assert.That(runner.LoadedTestCases.Count, Is.EqualTo(2), "We should have had 2 converted MS test cases here");
        //}
        //[Test]
        //public void HandleTfsFilterCorrectlyWhenNoFilter()
        //{
        //    var runner = new AssemblyRunner(new TestLogger(), "test", (TFSTestFilter)null);
        //    runner.AddTestCases(fakeTest1);
        //    runner.AddTestCases(fakeTest2);

        //    Assert.That(runner.NUnitFilter.IsEmpty, Is.False, "NUnitfilter should not be empty, we have added testcases");
        //    Assert.That(runner.LoadedTestCases.Count, Is.EqualTo(2), "We should have had 2 converted MS test cases here");
        //}
    }
}