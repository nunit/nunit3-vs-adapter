
// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using System.Collections;


namespace NUnit.VisualStudio.TestAdapter.Tests
{
    using System.Diagnostics;
    using System.Reflection;

    using NUnit.Core;
    using NUnit.Core.Filters;
    using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

    [TestFixture]
    public class AssemblyFilterTests
    {
        private readonly static Uri EXECUTOR_URI = new Uri(NUnitTestExecutor.ExecutorUri);

        private void FakeTestMethod1()
        {
        }

        private void FakeTestMethod2()
        {
        }

        private TestNode fakeTest1;

        private TestNode fakeTest2;
        [SetUp]
        public void SetUp()
        {
            MethodInfo fakeTestMethod1 = this.GetType().GetMethod("FakeTestMethod1", BindingFlags.Instance | BindingFlags.NonPublic);
            var fakeNUnitTest1 = new NUnitTestMethod(fakeTestMethod1);
            this.fakeTest1 = new TestNode(fakeNUnitTest1);
            MethodInfo fakeTestMethod2 = this.GetType().GetMethod("FakeTestMethod2", BindingFlags.Instance | BindingFlags.NonPublic);
            var fakeNUnitTest2 = new NUnitTestMethod(fakeTestMethod2);
            this.fakeTest2 = new TestNode(fakeNUnitTest2);
       }



        [Test]
        public void VerifyConstruction1()
        {
            var target = new AssemblyFilter("test");
            Assert.That(target.NUnitFilter.IsEmpty, Is.True);
        }

        [Test]
        public void VerifyConstruction2()
        {
            var sf = new SimpleNameFilter();
            var target2 = new AssemblyFilter("test", sf);
            Assert.That(target2.NUnitFilter.IsEmpty, Is.False);
        }

        [Test]
        public void IsTfsSet()
        {
            using (AssemblyFilter target = new TfsAssemblyFilter("test",null))
            {
                Assert.That(target.IsCalledFromTfs, Is.True);
            }
        }

        [Test]
        public void IsTfsNotSet()
        {
            var target = new AssemblyFilter("test");
            Assert.That(target.IsCalledFromTfs, Is.False);
        }

        [Test]
        public void AddsNonFilteredCorrectly()
        {
            var target = new AssemblyFilter("test");
            target.AddTestCases(fakeTest1);
            target.AddTestCases(fakeTest2);
            Assert.That(target.NUnitFilter.IsEmpty,Is.True,"NUnitfilter has been touched");
            Assert.That(target.VsTestCases.Count,Is.EqualTo(2),"We should have had 2 test cases here");
            Assert.That(target.NUnitTestCaseMap.ContainsKey(fakeTest1.TestName.UniqueName), Is.True, "FakeTestMethod1 is not in the map");
            Assert.That(target.NUnitTestCaseMap.ContainsKey(fakeTest2.TestName.UniqueName), Is.True, "FakeTestMethod2 is not in the map");
        }

        [Test]
        public void AddsFilteredCorrectly()
        {
            var t1 = new TestCase(fakeTest1.TestName.FullName, EXECUTOR_URI, "test");
            var t2 = new TestCase(fakeTest2.TestName.FullName, EXECUTOR_URI, "test");
            var list = new List<TestCase> { t1, t2 };
            var target = AssemblyFilter.Create("test",list);
            target.AddTestCases(fakeTest1);
            target.AddTestCases(fakeTest2);
            Assert.That(target.NUnitFilter.IsEmpty, Is.False, "NUnitfilter should not be empty, we have added testcases");
            Assert.That(target.VsTestCases.Count, Is.EqualTo(2), "We should have had 2 converted MS test cases here");
            Assert.That(target.NUnitTestCaseMap.ContainsKey(fakeTest1.TestName.UniqueName), Is.True, "FakeTestMethod1 is not in the map");
            Assert.That(target.NUnitTestCaseMap.ContainsKey(fakeTest2.TestName.UniqueName), Is.True, "FakeTestMethod2 is not in the map");
        }

        [Test]
        public void TfsAssemblyFilterIsCorrectlyConstructed()
        {
            var target = new TfsAssemblyFilter("test", new FakeRunContext());
            Assert.That(target.NUnitFilter.IsEmpty,Is.True,"NUnitFilter should be empty before processing");
            Assert.That(target.IsCalledFromTfs,Is.True,"IsCalledFromTfs should be true");


        }

    }
}
