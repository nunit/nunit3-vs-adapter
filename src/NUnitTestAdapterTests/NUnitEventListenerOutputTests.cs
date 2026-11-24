using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests;

/// <summary>
/// These tests ensure correct console output, which is what we send to the "recorder".
/// </summary>
public class NUnitEventListenerOutputTests
{
    private ITestExecutionRecorder recorder;
    private ITestConverterCommon converter;
    private IAdapterSettings settings;
    private INUnit3TestExecutor executor;


    private const string TestOutputProgress =
        @"<test-output stream='Progress' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever
]]></test-output>";

    private const string TestOutputOut =
        @"<test-output stream='Out' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever
]]></test-output>";

    private const string TestOutputError =
        @"<test-output stream='Error' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever
]]></test-output>";

    private const string BlankTestOutput =
        "<test-output stream='Progress' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[   ]]></test-output>";

    private const string TestFinish =
        "<test-case id='0-1001' name='Test1' fullname='UnitTests.Test1' methodname='Test1' classname='UnitTests' runstate='Runnable' seed='108294034' result='Passed' start-time='2018-10-15 09:41:24Z' end-time='2018-10-15 09:41:24Z' duration='0.000203' asserts='0' parentId='0-1000' />";

    /// <summary>
    /// For Issue 811.
    /// </summary>
    private const string TestFinishWithExplicitFixture =
        "<test-case id='0-1001' name='ExplicitTest' fullname='NUnit3VSIssue811.Explicit.ExplicitTest' methodname='ExplicitTest' classname='NUnit3VSIssue811.Explicit' runstate='Runnable' seed='1980958818' result='Skipped' label='Explicit' site='Parent' start-time='0001-01-01T00:00:00.0000000' end-time='0001-01-01T00:00:00.0000000' duration='0.000000' asserts='0' parentId='0-1000'/>";

    [SetUp]
    public void Setup()
    {
        recorder = Substitute.For<IFrameworkHandle>();
        converter = Substitute.For<ITestConverterCommon>();
        settings = Substitute.For<IAdapterSettings>();
        settings.ConsoleOut.Returns(1);
        executor = Substitute.For<INUnit3TestExecutor>();
        executor.Settings.Returns(settings);
        executor.FrameworkHandle.Returns(recorder);
    }

    private void UseRealSettings(string nunitSettings)
    {
        var realSettings = new AdapterSettings(new TestLogger(new MessageLoggerStub()));
        realSettings.Load($"<RunSettings><NUnit>{nunitSettings}</NUnit></RunSettings>");
        executor.Settings.Returns(realSettings);
    }

    [Test]
    public void ThatNormalTestOutputIsOutput()
    {
        var sut = new NUnitEventListener(converter, executor);
        sut.OnTestEvent(TestOutputOut);
        sut.OnTestEvent(TestFinish);

        recorder.Received().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Is<string>(x => x.StartsWith("Whatever")));
        converter.Received().GetVsTestResults(Arg.Any<NUnitTestEventTestCase>(), Arg.Is<ICollection<INUnitTestEventTestOutput>>(x => x.Count == 1));
    }

    [Test]
    public void ThatProgressTestOutputIsOutput()
    {
        var sut = new NUnitEventListener(converter, executor);
        sut.OnTestEvent(TestOutputProgress);
        sut.OnTestEvent(TestFinish);

        recorder.Received().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Is<string>(x => x.StartsWith("Whatever")));
        converter.Received().GetVsTestResults(Arg.Any<NUnitTestEventTestCase>(), Arg.Is<ICollection<INUnitTestEventTestOutput>>(x => x.Count == 1));
    }

    [Test]
    public void ThatNormalTestOutputIsError()
    {
        var sut = new NUnitEventListener(converter, executor);
        sut.OnTestEvent(TestOutputError);
        sut.OnTestEvent(TestFinish);

        recorder.Received().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Is<string>(x => x.StartsWith("Whatever")));
        converter.Received().GetVsTestResults(Arg.Any<NUnitTestEventTestCase>(), Arg.Is<ICollection<INUnitTestEventTestOutput>>(x => x.Count == 1));
    }

    [Test]
    public void ThatConsoleOutCanStopAllTestOutput()
    {
        UseRealSettings("<AgentMode>Off</AgentMode><ConsoleOut>0</ConsoleOut>");
        var sut = new NUnitEventListener(converter, executor);
        sut.OnTestEvent(TestOutputOut);
        sut.OnTestEvent(TestOutputProgress);
        sut.OnTestEvent(TestOutputError);
        sut.OnTestEvent(TestFinish);

        recorder.DidNotReceive().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Any<string>());
    }

    [Test]
    public void ThatTestOutputWithOnlyWhiteSpaceIsNotOutput()
    {
        var sut = new NUnitEventListener(converter, executor);

        sut.OnTestEvent(BlankTestOutput);

        recorder.DidNotReceive().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Any<string>());
    }

    /// <summary>
    /// Issue 1515: in agent mode the output is not sent to the console, but it is attached to the test result.
    /// The VSTest console logger prints it from there for failed tests.
    /// </summary>
    [TestCase(TestOutcome.Failed)]
    [TestCase(TestOutcome.Passed)]
    public void ThatAgentModeLeavesOutputToTheTestResult(TestOutcome outcome)
    {
        UseRealSettings("<AgentMode>On</AgentMode>");
        ConverterReturns(outcome, consoleOutput: "Whatever");
        var sut = new NUnitEventListener(converter, executor);

        sut.OnTestEvent(TestOutputOut);
        sut.OnTestEvent(TestOutputError);
        sut.OnTestEvent(TestFinish);

        recorder.DidNotReceive().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Any<string>());
        converter.Received().GetVsTestResults(Arg.Any<NUnitTestEventTestCase>(), Arg.Is<ICollection<INUnitTestEventTestOutput>>(x => x.Count == 2));
    }

    /// <summary>
    /// Output from a failing OneTimeSetUp is buffered under the fixture id, and has no test result to go to.
    /// </summary>
    [Test]
    public void ThatAgentModeSendsBufferedOutputOfFailedSuite()
    {
        UseRealSettings("<AgentMode>On</AgentMode>");
        var sut = new NUnitEventListener(converter, executor);

        sut.OnTestEvent("<test-output stream='Error' testid='0-1000' testname='UnitTests'><![CDATA[Setup diagnostics\n]]></test-output>");
        recorder.DidNotReceive().SendMessage(Arg.Any<TestMessageLevel>(), Arg.Any<string>());

        sut.OnTestEvent("<test-suite type='TestFixture' id='0-1000' name='UnitTests' fullname='UnitTests' result='Failed' site='SetUp'><failure><message><![CDATA[Boom]]></message></failure></test-suite>");
        recorder.Received().SendMessage(TestMessageLevel.Warning, "Setup diagnostics");
    }

    private void ConverterReturns(TestOutcome outcome, string consoleOutput)
    {
        var testCase = new TestCase("UnitTests.Test1", new Uri(NUnit3TestExecutor.ExecutorUri), "UnitTests.dll");
        converter.GetVsTestResults(Arg.Any<INUnitTestEventTestCase>(), Arg.Any<ICollection<INUnitTestEventTestOutput>>())
            .Returns(new TestResultSet
            {
                TestCaseResult = new TestResult(testCase) { Outcome = outcome },
                TestResults = [],
                ConsoleOutput = consoleOutput
            });
    }

    /// <summary>
    /// Issue 811  System.FormatException: The UTC representation of the date falls outside the year range 1-9999" from skipped test in Eastern European time zone.
    /// </summary>
    [Test]
    public void ThatExplicitTestFixtureWorksWithZeroStartTime()
    {
        var sut = new NUnitEventListener(converter, executor);
        Assert.DoesNotThrow(() => sut.OnTestEvent(TestFinishWithExplicitFixture));
    }
}