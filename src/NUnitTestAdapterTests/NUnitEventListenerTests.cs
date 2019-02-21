﻿// ***********************************************************************
// Copyright (c) 2012-2017 Charlie Poole, Terje Sandstrom
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
#if !NETCOREAPP1_0
using System.Runtime.Remoting;
#endif
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

using VSTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NUnitEventListenerTests
    {
        private NUnitEventListener listener;
        private FakeFrameworkHandle testLog;
        private XmlNode fakeTestNode;

        [SetUp]
        public void SetUp()
        {
            testLog = new FakeFrameworkHandle();
            var settings = Substitute.For<IAdapterSettings>();
            settings.CollectSourceInformation.Returns(true);
            using (var testConverter = new TestConverter(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath,settings))
            {
                fakeTestNode = FakeTestData.GetTestNode();

                // Ensure that the converted testcase is cached
                testConverter.ConvertTestCase(fakeTestNode);
                Assert.NotNull(testConverter.GetCachedTestCase("123"));

                listener = new NUnitEventListener(testLog, testConverter, null);
            }
        }

        #region TestStarted Tests

        [Test]
        public void TestStarted_CallsRecordStartCorrectly()
        {
            listener.OnTestEvent("<start-test id='123' name='FakeTestMethod'/>");
            Assert.That(testLog.Events.Count, Is.EqualTo(1));
            Assert.That(
                testLog.Events[0].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordStart));

            VerifyTestCase(testLog.Events[0].TestCase);
        }

        #endregion

        #region TestFinished Tests

        [Test]
        public void TestFinished_CallsRecordEnd_Then_RecordResult()
        {
            listener.TestFinished(FakeTestData.GetResultNode());
            Assert.AreEqual(2, testLog.Events.Count);
            Assert.AreEqual(
                FakeFrameworkHandle.EventType.RecordEnd,
                testLog.Events[0].EventType);
            Assert.AreEqual(
                FakeFrameworkHandle.EventType.RecordResult,
                testLog.Events[1].EventType);
        }

        [Test]
        public void TestFinished_CallsRecordEndCorrectly()
        {
            listener.TestFinished(FakeTestData.GetResultNode());
            Assume.That(testLog.Events.Count, Is.EqualTo(2));
            Assume.That(
                testLog.Events[0].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordEnd));

            VerifyTestCase(testLog.Events[0].TestCase);
            Assert.AreEqual(TestOutcome.Passed, testLog.Events[0].TestOutcome);
        }

        [Test]
        public void TestFinished_CallsRecordResultCorrectly()
        {
            listener.TestFinished(FakeTestData.GetResultNode());
            Assume.That(testLog.Events.Count, Is.EqualTo(2));
            Assume.That(
                testLog.Events[1].EventType,
                Is.EqualTo(FakeFrameworkHandle.EventType.RecordResult));

            VerifyTestResult(testLog.Events[1].TestResult);
        }

        //[TestCase(ResultState.Success, TestOutcome.Passed, null)]
        //[TestCase(ResultState.Failure, TestOutcome.Failed, "My failure message")]
        //[TestCase(ResultState.Error, TestOutcome.Failed, "Error!")]
        //[TestCase(ResultState.Cancelled, TestOutcome.None, null)]
        //[TestCase(ResultState.Inconclusive, TestOutcome.None, null)]
        //[TestCase(ResultState.NotRunnable, TestOutcome.Failed, "No constructor")]
        //[TestCase(ResultState.Skipped, TestOutcome.Skipped, null)]
        //[TestCase(ResultState.Ignored, TestOutcome.Skipped, "my reason")]
        //public void TestFinished_OutcomesAreCorrectlyTranslated(ResultState resultState, TestOutcome outcome, string message)
        //{
        //    fakeNUnitResult.SetResult(resultState, message, null);
        //    listener.TestFinished(fakeNUnitResult);
        //    Assume.That(testLog.Events.Count, Is.EqualTo(2));
        //    Assume.That(
        //        testLog.Events[0].EventType,
        //        Is.EqualTo(FakeFrameworkHandle.EventType.RecordEnd));
        //    Assume.That(
        //        testLog.Events[1].EventType,
        //        Is.EqualTo(FakeFrameworkHandle.EventType.RecordResult));

        //    Assert.AreEqual(outcome, testLog.Events[0].TestOutcome);
        //    Assert.AreEqual(outcome, testLog.Events[1].TestResult.Outcome);
        //    Assert.AreEqual(message, testLog.Events[1].TestResult.ErrorMessage);
        //}

        #endregion

        #region Listener Lifetime Tests
#if !NETCOREAPP1_0
        [Test]
        public void Listener_LeaseLifetimeWillNotExpire()
        {
            testLog = new FakeFrameworkHandle();
            var settings = Substitute.For<IAdapterSettings>();
            settings.CollectSourceInformation.Returns(true);
            using (var testConverter = new TestConverter(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath, settings))
            {
                var localInstance = (MarshalByRefObject)Activator.CreateInstance(typeof(NUnitEventListener), testLog, testConverter, null);

                RemotingServices.Marshal(localInstance);

                var lifetime = ((MarshalByRefObject)localInstance).GetLifetimeService();

                // A null lifetime (as opposed to an ILease) means the object has an infinite lifetime
                Assert.IsNull(lifetime);
            }
        }
#endif
        #endregion

        #region Helper Methods

        private void VerifyTestCase(TestCase ourCase)
        {
            Assert.NotNull(ourCase, "TestCase not set");
            Assert.That(ourCase.DisplayName, Is.EqualTo(FakeTestData.DisplayName));
            Assert.That(ourCase.FullyQualifiedName, Is.EqualTo(FakeTestData.FullyQualifiedName));
            Assert.That(ourCase.Source, Is.EqualTo(FakeTestData.AssemblyPath));
            if (ourCase.CodeFilePath != null) // Unavailable if not running under VS
            {
                Assert.That(ourCase.CodeFilePath, Is.SamePath(FakeTestData.CodeFile));
                Assert.That(ourCase.LineNumber, Is.EqualTo(FakeTestData.LineNumber));
            }
        }

        private void VerifyTestResult(VSTestResult ourResult)
        {
            Assert.NotNull(ourResult, "TestResult not set");
            VerifyTestCase(ourResult.TestCase);

            Assert.AreEqual(Environment.MachineName, ourResult.ComputerName);
            Assert.AreEqual(TestOutcome.Passed, ourResult.Outcome);
            Assert.AreEqual("It passed!", ourResult.ErrorMessage);
            Assert.AreEqual(TimeSpan.FromSeconds(1.234), ourResult.Duration);
        }

        #endregion
    }

    public class NUnitEventListenerOutputTests
    {
        private ITestExecutionRecorder recorder;
        private ITestConverter converter;
        private IDumpXml dumpxml;

        private const string Testoutput =
            @"<test-output stream='Progress' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever
]]></test-output>";

        private const string TestoutputError =
            @"<test-output stream='Error' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever
]]></test-output>";

        private const string BlankTestoutput =
            @"<test-output stream='Progress' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[   ]]></test-output>";

        private const string TestFinish = 
            @"<test-case id='0-1001' name='Test1' fullname='UnitTests.Test1' methodname='Test1' classname='UnitTests' runstate='Runnable' seed='108294034' result='Passed' start-time='2018-10-15 09:41:24Z' end-time='2018-10-15 09:41:24Z' duration='0.000203' asserts='0' parentId='0-1000' />";

        [SetUp]
        public void Setup()
        {
            recorder = Substitute.For<ITestExecutionRecorder>();
            converter = Substitute.For<ITestConverter>();
            dumpxml = Substitute.For<IDumpXml>();
        }

        [Test]
        public void ThatNormalTestOutputIsOutput()
        {
            var sut = new NUnitEventListener(recorder, converter, dumpxml);
            sut.OnTestEvent(Testoutput);
            sut.OnTestEvent(TestFinish);

            recorder.Received().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Is<string>(x=>x.StartsWith("Whatever")));
            converter.Received().GetVSTestResults(Arg.Any<XmlElement>(), Arg.Is<ICollection<XmlNode>>(x => x.Count == 1));
        }

        [Test]
        public void ThatNormalTestOutputIsError()
        {
            var sut = new NUnitEventListener(recorder, converter, dumpxml);
            sut.OnTestEvent(TestoutputError);
            sut.OnTestEvent(TestFinish);

            recorder.Received().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Is<string>(x=>x.StartsWith("Whatever")));
            converter.Received().GetVSTestResults(Arg.Any<XmlElement>(), Arg.Is<ICollection<XmlNode>>(x => x.Count == 1));
        }

        [Test]
        public void ThatTestOutputWithWhiteSpaceIsNotOutput()
        {
            var sut = new NUnitEventListener(recorder, converter, dumpxml);

            sut.OnTestEvent(BlankTestoutput);

            recorder.DidNotReceive().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Any<string>());
        }
    }
}
