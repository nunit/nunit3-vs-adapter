// ***********************************************************************
// Copyright (c) 2020-2020 Charlie Poole, Terje Sandstrom
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

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    public class NUnitTestEventsTests
    {
        private string startSuite = @"<start-suite id = '0-1073' parentId='0-1141' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' type='TestFixture' />";
        private string testSuite = @"<test-suite type='TestFixture' id='0-1073' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' testcasecount='20' result='Failed' site='Child' start-time='2020-01-24 13:02:55Z' end-time='2020-01-24 13:02:55Z' duration='0.032827' total='15' passed='6' failed='8' warnings='0' inconclusive='1' skipped='0' asserts='11' parentId='0-1141'>
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
        }

        private string startTest = @"<start-test id='0-1139' parentId='0-1138' name='Test2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2.Test2' type='TestMethod' />";

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

        private string testCaseFailing =
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

        private string testCaseSucceeds = @"<test-case id='0-1006' name='AsyncTaskTestSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestSucceeds' methodname='AsyncTaskTestSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1350317088' result='Passed' start-time='2020-01-23 18:07:42Z' end-time='2020-01-23 18:07:42Z' duration='0.001131' asserts='1' parentId='0-1004'>
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

        private string testSuiteFinished = @"<test-suite type='TestFixture' id='0-1094' name='TextOutputTests' fullname='NUnitTestDemo.TextOutputTests' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' testcasecount='9' result='Passed' start-time='2020-01-23 18:07:42Z' end-time='2020-01-23 18:07:42Z' duration='0.018222' total='9' passed='9' failed='0' warnings='0' inconclusive='0' skipped='0' asserts='0' parentId='0-1141'>
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
    }
}
