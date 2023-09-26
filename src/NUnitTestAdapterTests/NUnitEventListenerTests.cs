// ***********************************************************************
// Copyright (c) 2012-2021 Charlie Poole, Terje Sandstrom
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
#if NET462
using System.Runtime.Remoting;
#endif

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using NSubstitute;

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

using VSTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter.Tests;

public class NUnitEventListenerTests
{
    private NUnitEventListener listener;
    private FakeFrameworkHandle testLog;
    private NUnitEventTestCase fakeTestNode;
    private INUnit3TestExecutor executor;
    private IAdapterSettings settings;

    [SetUp]
    public void SetUp()
    {
        testLog = new FakeFrameworkHandle();
        settings = Substitute.For<IAdapterSettings>();
        executor = Substitute.For<INUnit3TestExecutor>();
        executor.Settings.Returns(settings);
        executor.FrameworkHandle.Returns(testLog);
        settings.CollectSourceInformation.Returns(true);
        using var testConverter = new TestConverterForXml(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath, settings);
        fakeTestNode = new NUnitEventTestCase(FakeTestData.GetTestNode());

        // Ensure that the converted testcase is cached
        testConverter.ConvertTestCase(fakeTestNode);
        Assert.That(testConverter.GetCachedTestCase("123"), Is.Not.Null);

        listener = new NUnitEventListener(testConverter, executor);
    }

    #region TestStarted Tests

    [Test]
    public void TestStarted_CallsRecordStartCorrectly()
    {
        listener.OnTestEvent("<start-test id='123' name='FakeTestMethod'/>");
        Assert.That(testLog.Events, Has.Count.EqualTo(1));
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
        listener.TestFinished(new NUnitTestEventTestCase(FakeTestData.GetResultNode().AsString()));
        Assert.That(testLog.Events, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(testLog.Events[0].EventType, Is.EqualTo(FakeFrameworkHandle.EventType.RecordEnd));
            Assert.That(
                testLog.Events[1].EventType, Is.EqualTo(FakeFrameworkHandle.EventType.RecordResult));
        });
    }

    [Test]
    public void TestFinished_CallsRecordEndCorrectly()
    {
        listener.TestFinished(new NUnitTestEventTestCase(FakeTestData.GetResultNode().AsString()));
        Assume.That(testLog.Events.Count, Is.EqualTo(2));
        Assume.That(testLog.Events[0].EventType, Is.EqualTo(FakeFrameworkHandle.EventType.RecordEnd));

        VerifyTestCase(testLog.Events[0].TestCase);
        Assert.That(testLog.Events[0].TestOutcome, Is.EqualTo(TestOutcome.Passed));
    }

    /// <summary>
    /// Issue516.
    /// </summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    [TestCase("\r")]
    [TestCase("\n")]
    [TestCase("\r\n")]
    public void TestFinished_DoNotSendWhiteSpaceToMessages(string data)
    {
        var testcase = Substitute.For<INUnitTestEventTestCase>();
        testcase.Name.Returns($"Test1({data})");
        testcase.FullName.Returns($"Issue516.Tests.Test1({data})");
        testcase.Output.Returns($"{data}");
        settings.ConsoleOut.Returns(1);
        listener.TestFinished(testcase);
        Assert.That(testLog.Events, Is.Empty);
    }



    [Test]
    public void TestFinished_CallsRecordResultCorrectly()
    {
        listener.TestFinished(new NUnitTestEventTestCase(FakeTestData.GetResultNode().AsString()));
        Assume.That(testLog.Events.Count, Is.EqualTo(2));
        Assume.That(testLog.Events[1].EventType, Is.EqualTo(FakeFrameworkHandle.EventType.RecordResult));

        VerifyTestResult(testLog.Events[1].TestResult);
    }


    // [TestCase(ResultState.Success, TestOutcome.Passed, null)]
    // [TestCase(ResultState.Failure, TestOutcome.Failed, "My failure message")]
    // [TestCase(ResultState.Error, TestOutcome.Failed, "Error!")]
    // [TestCase(ResultState.Cancelled, TestOutcome.None, null)]
    // [TestCase(ResultState.Inconclusive, TestOutcome.None, null)]
    // [TestCase(ResultState.NotRunnable, TestOutcome.Failed, "No constructor")]
    // [TestCase(ResultState.Skipped, TestOutcome.Skipped, null)]
    // [TestCase(ResultState.Ignored, TestOutcome.Skipped, "my reason")]
    // public void TestFinished_OutcomesAreCorrectlyTranslated(ResultState resultState, TestOutcome outcome, string message)
    // {
    //    fakeNUnitResult.SetResult(resultState, message, null);
    //    listener.TestFinished(fakeNUnitResult);
    //    Assume.That(testLog.Events.Count, Is.EqualTo(2));
    //    Assume.That(
    //        testLog.Events[0].EventType,
    //        Is.EqualTo(FakeFrameworkHandle.EventType.RecordEnd));
    //    Assume.That(
    //        testLog.Events[1].EventType,
    //        Is.EqualTo(FakeFrameworkHandle.EventType.RecordResult));

    // Assert.AreEqual(outcome, testLog.Events[0].TestOutcome);
    //    Assert.AreEqual(outcome, testLog.Events[1].TestResult.Outcome);
    //    Assert.AreEqual(message, testLog.Events[1].TestResult.ErrorMessage);
    // }

    #endregion

    #region TestOutput Tests

    [TestCase(NUnitTestEventTestOutput.Streams.Error, TestMessageLevel.Warning, TestName = "TestOutput with 'Error' stream is send as message level Warning")]
    [TestCase(NUnitTestEventTestOutput.Streams.Progress, TestMessageLevel.Informational, TestName = "TestOutput with 'Progress' stream is send as message level Informational")]
    [TestCase(NUnitTestEventTestOutput.Streams.NoIdea, TestMessageLevel.Informational, TestName = "TestOutput with 'NoIdea' stream is send as message level Informational")]
    public void TestOutput_SendsMessageWithCorrectMessageLevel(NUnitTestEventTestOutput.Streams streamType, TestMessageLevel expectedTestMessageLevel)
    {
        var outPutXml = @$"<test-output stream='{streamType}' testid='0-1001' testname='Something.TestClass.SomeTest'><![CDATA[SomeData]]></test-output>";
        var testOutput = new NUnitTestEventTestOutput(XmlHelper.CreateXmlNode(outPutXml));

        listener.TestOutput(testOutput);
        Assume.That(testLog.Events.Count, Is.EqualTo(1));
        Assume.That(testLog.Events[0].EventType, Is.EqualTo(FakeFrameworkHandle.EventType.SendMessage));

        Assert.That(testLog.Events[0].Message.Level, Is.EqualTo(expectedTestMessageLevel));
    }

    #endregion

    #region Listener Lifetime Tests
#if NET462
    [Test]
    public void Listener_LeaseLifetimeWillNotExpire()
    {
        testLog = new FakeFrameworkHandle();
        var adapterSettings = Substitute.For<IAdapterSettings>();
        adapterSettings.CollectSourceInformation.Returns(true);
        var discoveryConverter = Substitute.For<IDiscoveryConverter>();
        using var testConverter = new TestConverter(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath, adapterSettings, discoveryConverter);
        var executor = Substitute.For<INUnit3TestExecutor>();
        var localInstance = (MarshalByRefObject)Activator.CreateInstance(typeof(NUnitEventListener), testConverter, executor);

        RemotingServices.Marshal(localInstance);

        var lifetime = ((MarshalByRefObject)localInstance).GetLifetimeService();

        // A null lifetime (as opposed to an ILease) means the object has an infinite lifetime
        Assert.That(lifetime, Is.Null);
    }
#endif
    #endregion

    #region Helper Methods

    private void VerifyTestCase(TestCase ourCase)
    {
        Assert.That(ourCase, Is.Not.Null, "TestCase not set");
        Assert.Multiple(() =>
        {
            Assert.That(ourCase.DisplayName, Is.EqualTo(FakeTestData.DisplayName));
            Assert.That(ourCase.FullyQualifiedName, Is.EqualTo(FakeTestData.FullyQualifiedName));
            Assert.That(ourCase.Source, Is.EqualTo(FakeTestData.AssemblyPath));
        });
        if (ourCase.CodeFilePath != null) // Unavailable if not running under VS
        {
            Assert.Multiple(() =>
            {
                Assert.That(ourCase.CodeFilePath, Is.SamePath(FakeTestData.CodeFile));
                Assert.That(ourCase.LineNumber, Is.EqualTo(FakeTestData.LineNumber));
            });
        }
    }

    private void VerifyTestResult(VSTestResult ourResult)
    {
        Assert.That(ourResult, Is.Not.Null, "TestResult not set");
        VerifyTestCase(ourResult.TestCase);
        Assert.Multiple(() =>
        {
            Assert.That(ourResult.ComputerName, Is.EqualTo(Environment.MachineName));
            Assert.That(ourResult.Outcome, Is.EqualTo(TestOutcome.Passed));
            Assert.That(ourResult.ErrorMessage, Is.EqualTo(null));
            Assert.That(ourResult.Duration, Is.EqualTo(TimeSpan.FromSeconds(1.234)));
        });
    }

    #endregion

    [TearDown]
    public void TearDown()
    {
        listener.Dispose();
    }
}