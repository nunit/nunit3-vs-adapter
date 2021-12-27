// ***********************************************************************
// Copyright (c) 2020-2021 Charlie Poole, Terje Sandstrom
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
using System.Linq;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    public class NUnitTestEventsTests
    {
        private const string StartSuite = @"<start-suite id = '0-1073' parentId='0-1141' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' type='TestFixture' />";
        private readonly string testSuite = @"<test-suite type='TestFixture' id='0-1073' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' testcasecount='20' result='Failed' site='Child' start-time='2020-01-24 13:02:55Z' end-time='2020-01-24 13:02:55Z' duration='0.032827' total='15' passed='6' failed='8' warnings='0' inconclusive='1' skipped='0' asserts='11' parentId='0-1141'>
   <failure>
      <message><![CDATA[One or more child tests had errors]]></message>
   </failure>
</test-suite>";

        [Test]
        public void ThatTestEventIsParsedForTestSuite()
        {
            var sut = new NUnitTestEventSuiteFinished(testSuite);
            Assert.Multiple(() =>
            {
                Assert.That(sut.FullName, Is.EqualTo("NUnitTestDemo.SimpleTests"));
                Assert.That(sut.TestType(), Is.EqualTo(NUnitTestEvent.TestTypes.TestFixture));
                Assert.That(sut.Name, Is.EqualTo("SimpleTests"));
                Assert.That(sut.TestType, Is.EqualTo(NUnitTestEvent.TestTypes.TestFixture));
                Assert.That(sut.Id, Is.EqualTo("0-1073"));
            });
            Assert.That(sut.HasFailure);
            Assert.That(sut.FailureMessage, Is.EqualTo("One or more child tests had errors"));
        }

        private readonly string startTest = @"<start-test id='0-1139' parentId='0-1138' name='Test2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2.Test2' type='TestMethod' />";

        [Test]
        public void ThatTestEventIsParsedForStartTest()
        {
            var sut = new NUnitTestEventStartTest(startTest);
            Assert.Multiple(() =>
            {
                Assert.That(sut.FullName, Is.EqualTo("NUnitTestDemo.SetUpFixture.TestFixture2.Test2"));
                Assert.That(sut.TestType(), Is.EqualTo(NUnitTestEvent.TestTypes.TestMethod));
                Assert.That(sut.Name, Is.EqualTo("Test2"));
                Assert.That(sut.TestType, Is.EqualTo(NUnitTestEvent.TestTypes.TestMethod));
                Assert.That(sut.Id, Is.EqualTo("0-1139"));
            });
        }

        private readonly string testCaseFailing =
            @"<test-case id='0-1076' name='TestFails' fullname='NUnitTestDemo.SimpleTests.TestFails' methodname='TestFails' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='325575216' result='Failed' start-time='2020-01-23 18:07:42Z' end-time='2020-01-23 18:07:42Z' duration='0.001060' asserts='1' parentId='0-1073'>
   <properties>
      <property name='Expect' value='Failure' />
   </properties>
   <failure>
      <message><![CDATA[  Expected: 5
  But was:  4
]]></message>
      <stack-trace><![CDATA[   at NUnitTestDemo.SimpleTests.TestFails() in D:\repos\NUnit\nunit3-vs-adapter-demo\src\csharp\SimpleTests.cs:line 29
]]></stack-trace>
   </failure>
   <assertions>
      <assertion result='Failed'>
         <message><![CDATA[  Expected: 5
  But was:  4
]]></message>
         <stack-trace><![CDATA[   at NUnitTestDemo.SimpleTests.TestFails() in D:\repos\NUnit\nunit3-vs-adapter-demo\src\csharp\SimpleTests.cs:line 29
]]></stack-trace>
      </assertion>
   </assertions>
</test-case>";
        [Test]
        public void ThatTestEventIsParsedForFailingTestCase()
        {
            var sut = new NUnitTestEventTestCase(testCaseFailing);
            Assert.Multiple(() =>
            {
                Assert.That(sut.FullName, Is.EqualTo("NUnitTestDemo.SimpleTests.TestFails"));
                Assert.That(sut.TestType(), Is.EqualTo(NUnitTestEvent.TestTypes.NoIdea));
                Assert.That(sut.Name, Is.EqualTo("TestFails"));
                Assert.That(sut.Id, Is.EqualTo("0-1076"));
                Assert.That(sut.Result, Is.EqualTo(NUnitTestEvent.ResultType.Failed));
                Assert.That(sut.Duration.TotalMilliseconds, Is.GreaterThanOrEqualTo(1.0));
                Assert.That(sut.StartTime().Ok);
                Assert.That(sut.EndTime().Ok);
            });
        }

        [Test]
        public void ThatTestCasePropertiesAreParsedWhenFailing()
        {
            var sut = new NUnitTestEventTestCase(testCaseFailing);
            Assert.That(sut.Properties, Is.Not.Null);
            Assert.That(sut.Properties.Count, Is.EqualTo(1));
            var property = sut.Properties.FirstOrDefault();
            Assert.That(property, Is.Not.Null);
            Assert.That(property.Name, Is.EqualTo("Expect"));
            Assert.That(property.Value, Is.EqualTo("Failure"));
        }

        [Test]
        public void ThatTestCaseFailureIsParsedWhenFailing()
        {
            var sut = new NUnitTestEventTestCase(testCaseFailing);
            var failure = sut.Failure;
            Assert.That(failure, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(failure.Message.Trim(), Does.StartWith("Expected: 5"), $"Failure.Message is: {failure.Message}");
                Assert.That(failure.Stacktrace, Does.StartWith("   at NUnitTestDemo.SimpleTests.TestFails()"), $"Stacktrace:{failure.Stacktrace}");
            });
        }


        private readonly string testCaseSucceeds = @"<test-case id='0-1006' name='AsyncTaskTestSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestSucceeds' methodname='AsyncTaskTestSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1350317088' result='Passed' start-time='2020-01-23 18:07:42Z' end-time='2020-01-23 18:07:42Z' duration='0.001131' asserts='1' parentId='0-1004'>
   <properties>
      <property name='Expect' value='Pass' />
   </properties>
</test-case>";
        [Test]
        public void ThatTestEventIsParsedForSuccessTestCase()
        {
            var sut = new NUnitTestEventTestCase(testCaseSucceeds);
            Assert.Multiple(() =>
            {
                Assert.That(sut.FullName, Is.EqualTo("NUnitTestDemo.AsyncTests.AsyncTaskTestSucceeds"));
                Assert.That(sut.TestType(), Is.EqualTo(NUnitTestEvent.TestTypes.NoIdea));
                Assert.That(sut.Name, Is.EqualTo("AsyncTaskTestSucceeds"));
                Assert.That(sut.Id, Is.EqualTo("0-1006"));
                Assert.That(sut.Result, Is.EqualTo(NUnitTestEvent.ResultType.Success));
            });
        }

        private readonly string testSuiteFinished = @"<test-suite type='TestFixture' id='0-1094' name='TextOutputTests' fullname='NUnitTestDemo.TextOutputTests' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' testcasecount='9' result='Passed' start-time='2020-01-23 18:07:42Z' end-time='2020-01-23 18:07:42Z' duration='0.018222' total='9' passed='9' failed='0' warnings='0' inconclusive='0' skipped='0' asserts='0' parentId='0-1141'>
   <properties>
      <property name='Expect' value='Pass' />
   </properties>
</test-suite>";
        [Test]
        public void ThatTestEventIsParsedForFinishSuite()
        {
            var sut = new NUnitTestEventSuiteFinished(testSuiteFinished);
            Assert.Multiple(() =>
            {
                Assert.That(sut.FullName, Is.EqualTo("NUnitTestDemo.TextOutputTests"));
                Assert.That(sut.TestType(), Is.EqualTo(NUnitTestEvent.TestTypes.TestFixture));
                Assert.That(sut.Name, Is.EqualTo("TextOutputTests"));
                Assert.That(sut.Id, Is.EqualTo("0-1094"));
                Assert.That(sut.Result, Is.EqualTo(NUnitTestEvent.ResultType.Success));
            });
        }

        private readonly string testSuiteFinishedWithReason =
            @"<test-suite type='ParameterizedMethod' id='0-1043' name='TestCaseWarns' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarns' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1' result='Warning' site='Child' start-time='2020-01-26 12:45:23Z' end-time='2020-01-26 12:45:23Z' duration='0.001552' total='0' passed='0' failed='0' warnings='1' inconclusive='0' skipped='0' asserts='1' parentId='0-1031'>
   <properties>
      <property name='Expect' value='Warning' />
   </properties>
   <reason>
      <message><![CDATA[One or more child tests had warnings]]></message>
   </reason>
</test-suite>";

        [Test]
        public void ThatTestEventIsParsedForFinishSuiteWithReason()
        {
            var sut = new NUnitTestEventSuiteFinished(testSuiteFinishedWithReason);
            Assert.That(sut.HasReason);
            Assert.That(sut.ReasonMessage, Is.EqualTo("One or more child tests had warnings"));
            Assert.That(sut.HasFailure, Is.False);
        }
        private readonly string testCaseSucceedsWithOutputAndReason = @"<test-case id='0-1074' name='TestSucceeds' fullname='NUnitTestDemo.SimpleTests.TestSucceeds' methodname='TestSucceeds' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1232497275' result='Passed' start-time='2020-01-24 11:18:32Z' end-time='2020-01-24 11:18:32Z' duration='0.016868' asserts='1' parentId='0-1073'>
   <properties>
      <property name='Expect' value='Pass' />
   </properties>
    <reason>
      <message><![CDATA[One or more child tests had warnings]]></message>
   </reason>
   <output><![CDATA[Simple test running
]]></output>
</test-case>";

        [Test]
        public void ThatTestEventIsParsedForTestCaseWithReason()
        {
            var sut = new NUnitTestEventTestCase(testCaseSucceedsWithOutputAndReason);
            Assert.That(sut.HasReason);
            Assert.That(sut.ReasonMessage, Is.EqualTo("One or more child tests had warnings"));
        }

        private readonly string testSuiteFinishedWithFailure = @"<test-suite type='ParameterizedMethod' id='0-1072' name='TestCaseWithRandomParameterWithFixedNaming' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameterWithFixedNaming' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='2' result='Failed' site='Child' start-time='2020-01-26 12:45:23Z' end-time='2020-01-26 12:45:23Z' duration='0.000101' total='2' passed='1' failed='1' warnings='0' inconclusive='0' skipped='0' asserts='0' parentId='0-1031'>
   <failure>
      <message><![CDATA[One or more child tests had errors]]></message>
   </failure>
</test-suite>";

        [Test]
        public void ThatTestEventIsParsedForFinishSuiteWithFailure()
        {
            var sut = new NUnitTestEventSuiteFinished(testSuiteFinishedWithFailure);
            Assert.That(sut.HasFailure);
            Assert.That(sut.FailureMessage, Is.EqualTo("One or more child tests had errors"));
        }

        /// <summary>
        /// Exception in OneTimeSetUp
        /// </summary>
        private readonly string testSuiteFinishedWithFailureAndStackTrace = @"<test-suite type='TestFixture' id='0-1000' name='Tests' fullname='Issue671.Tests' classname='Issue671.Tests' runstate='Runnable' testcasecount='2' result='Failed' label='Error' site='SetUp' start-time='2021-05-29T09:34:10.2386869Z' end-time='2021-05-29T09:34:10.2532435Z' duration='0.014557' total='2' passed='0' failed='2' warnings='0' inconclusive='0' skipped='0' asserts='0'>
            <failure>
               <message><![CDATA[System.Exception : oops Deep Down]]></message>
               <stack-trace><![CDATA[   at Issue671.SomeWhereDeepDown.WhatDoWeDoHere() in d:\repos\NUnit\nunit3-vs-adapter.issues\Issue671\UnitTest1.cs:line 44
   at Issue671.Tests.OneTimeSetup() in d:\repos\NUnit\nunit3-vs-adapter.issues\Issue671\UnitTest1.cs:line 12]]></stack-trace>
            </failure>
            <test-case id='0-1001' name='Test1' fullname='Issue671.Tests.Test1' methodname='Test1' classname='Issue671.Tests' runstate='Runnable' seed='1958545482' result='Failed' label='Error' site='Parent' start-time='2021-05-29T09:34:10.2386869Z' end-time='2021-05-29T09:34:10.2510075Z' duration='0.012408' asserts='0'>
               <failure>
                  <message><![CDATA[OneTimeSetUp: System.Exception : oops Deep Down]]></message>
               </failure>
            </test-case>
            <test-case id='0-1002' name='Test2' fullname='Issue671.Tests.Test2' methodname='Test2' classname='Issue671.Tests' runstate='Runnable' seed='711019934' result='Failed' label='Error' site='Parent' start-time='2021-05-29T09:34:10.2386869Z' end-time='2021-05-29T09:34:10.2523634Z' duration='0.013677' asserts='0'>
               <failure>
                  <message><![CDATA[OneTimeSetUp: System.Exception : oops Deep Down]]></message>
               </failure>
            </test-case>
         </test-suite>";

        [Test]
        public void ThatTestEventIsParsedForFinishSuiteWithExceptionInOneTimeSetUp()
        {
            var sut = new NUnitTestEventSuiteFinished(testSuiteFinishedWithFailureAndStackTrace);
            Assert.That(sut.HasFailure);
            Assert.That(sut.FailureMessage.Length, Is.GreaterThan(0));
            Assert.That(sut.StackTrace.Length, Is.GreaterThan(0));
        }



        private readonly string testCaseSucceedsWithOutput = @"<test-case id='0-1074' name='TestSucceeds' fullname='NUnitTestDemo.SimpleTests.TestSucceeds' methodname='TestSucceeds' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1232497275' result='Passed' start-time='2020-01-24 11:18:32Z' end-time='2020-01-24 11:18:32Z' duration='0.016868' asserts='1' parentId='0-1073'>
   <properties>
      <property name='Expect' value='Pass' />
   </properties>
   <output><![CDATA[Simple test running
]]></output>
</test-case>";
        [Test]
        public void ThatTestEventIsParsedForSuccessTestCaseWithOutput()
        {
            var sut = new NUnitTestEventTestCase(testCaseSucceedsWithOutput);
            Assert.Multiple(() =>
            {
                Assert.That(sut.FullName, Is.EqualTo("NUnitTestDemo.SimpleTests.TestSucceeds"));
                Assert.That(sut.TestType(), Is.EqualTo(NUnitTestEvent.TestTypes.NoIdea));
                Assert.That(sut.Name, Is.EqualTo("TestSucceeds"));
                Assert.That(sut.Id, Is.EqualTo("0-1074"));
                Assert.That(sut.Result, Is.EqualTo(NUnitTestEvent.ResultType.Success));
                Assert.That(sut.MethodName, Is.EqualTo("TestSucceeds"));
                Assert.That(sut.ClassName, Is.EqualTo("NUnitTestDemo.SimpleTests"));
            });
        }

        private readonly string testCaseFails = @"<test-case id='0-1076' name='TestFails' fullname='NUnitTestDemo.SimpleTests.TestFails' methodname='TestFails' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='299199212' result='Failed' start-time='2020-01-26 12:45:23Z' end-time='2020-01-26 12:45:23Z' duration='0.000959' asserts='1' parentId='0-1073'>
   <properties>
      <property name='Expect' value='Failure' />
   </properties>
   <failure>
      <message><![CDATA[  Expected: 5
  But was:  4
]]></message>
      <stack-trace><![CDATA[   at NUnitTestDemo.SimpleTests.TestFails() in D:\repos\NUnit\nunit3-vs-adapter-demo\src\csharp\SimpleTests.cs:line 29
]]></stack-trace>
   </failure>
   <assertions>
      <assertion result='Failed'>
         <message><![CDATA[  Expected: 5
  But was:  4
]]></message>
         <stack-trace><![CDATA[   at NUnitTestDemo.SimpleTests.TestFails() in D:\repos\NUnit\nunit3-vs-adapter-demo\src\csharp\SimpleTests.cs:line 29
]]></stack-trace>
      </assertion>
   </assertions>
</test-case>";

        [Test]
        public void ThatTestCaseFailsCanBeParsed()
        {
            var sut = new NUnitTestEventTestCase(testCaseFails);
            Assert.That(sut.Properties.Count, Is.EqualTo(1));
            Assert.That(sut.HasFailure);
            Assert.Multiple(() =>
            {
                Assert.That(sut.Failure.Message.Trim(), Does.StartWith("Expected: 5"), $"Failure.Message is: {sut.Failure.Message}");
                Assert.That(sut.Failure.Stacktrace, Does.StartWith("   at NUnitTestDemo.SimpleTests.TestFails()"));
            });
        }

        private readonly string testCaseFailsWithReason = @"<test-case id='0-1086' name='TestIsIgnored_Assert' fullname='NUnitTestDemo.SimpleTests.TestIsIgnored_Assert' methodname='TestIsIgnored_Assert' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='202557333' result='Skipped' label='Ignored' start-time='2020-01-26 12:45:23Z' end-time='2020-01-26 12:45:23Z' duration='0.000540' asserts='0' parentId='0-1073'>
   <properties>
      <property name='Expect' value='Ignore' />
   </properties>
   <reason>
      <message><![CDATA[Ignoring this test deliberately]]></message>
   </reason>
</test-case>";

        [Test]
        public void ThatTestCaseFailsCanBeParsedWithReason()
        {
            var sut = new NUnitTestEventTestCase(testCaseFailsWithReason);
            Assert.That(sut.ReasonMessage, Is.EqualTo("Ignoring this test deliberately"));
        }

        private readonly string testCaseExplicitFixtureTime =
            @"<test-case id='0-1001' name='ExplicitTest' fullname='NUnit3VSIssue811.Explicit.ExplicitTest' methodname='ExplicitTest' classname='NUnit3VSIssue811.Explicit' runstate='Runnable' seed='1980958818' result='Skipped' label='Explicit' site='Parent' start-time='0001-01-01T00:00:00.0000000' end-time='0001-01-01T00:00:00.0000000' duration='0.000000' asserts='0' parentId='0-1000'/>";

        /// <summary>
        /// Issue 811.
        /// </summary>
        [Test]
        public void ThatExplicitTestFixtureWorksWithZeroStartTime()
        {
            var sut = new NUnitTestEventTestCase(testEvent: testCaseExplicitFixtureTime);
            Assert.DoesNotThrow(code: () => sut.StartTime());
        }
    }
}