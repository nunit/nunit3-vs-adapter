// ****************************************************************
// Copyright (c) 2012-2015 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

using VSTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NUnitEventListenerTests
    {
        private NUnitEventListener listener;
        private FakeFrameworkHandle testLog;
        private XmlNode fakeTestNode;
        private TestConverter testConverter;

        [SetUp]
        public void SetUp()
        {
            testLog = new FakeFrameworkHandle();
            testConverter = new TestConverter(new TestLogger(), FakeTestData.AssemblyPath);
            fakeTestNode = FakeTestData.GetTestNode();

            // Ensure that the converted testcase is cached
            testConverter.ConvertTestCase(fakeTestNode);
            Assert.NotNull(testConverter.GetCachedTestCase("123"));
            
            listener = new NUnitEventListener(testLog, testConverter);
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
        [Test]
        public void Listener_LeaseLifetimeWillNotExpire()
        {
            testLog = new FakeFrameworkHandle();
            testConverter = new TestConverter(new TestLogger(), FakeTestData.AssemblyPath);
            MarshalByRefObject localInstance = (MarshalByRefObject)Activator.CreateInstance(typeof(NUnitEventListener), testLog, testConverter);

            RemotingServices.Marshal(localInstance);

            var lifetime = ((MarshalByRefObject)localInstance).GetLifetimeService();
            
            // A null lifetime (as opposed to an ILease) means the object has an infinite lifetime
            Assert.IsNull(lifetime);
        }
        #endregion

        #region Helper Methods

        private void VerifyTestCase(TestCase ourCase)
        {
            Assert.NotNull(ourCase, "TestCase not set");
            Assert.That(ourCase.DisplayName, Is.EqualTo(FakeTestData.DisplayName));
            Assert.That(ourCase.FullyQualifiedName, Is.EqualTo(FakeTestData.FullyQualifiedName));
            Assert.That(ourCase.Source, Is.EqualTo(FakeTestData.AssemblyPath));
            Assert.That(ourCase.CodeFilePath, Is.SamePath(FakeTestData.CodeFile));
            Assert.That(ourCase.LineNumber, Is.EqualTo(FakeTestData.LineNumber));
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
}
