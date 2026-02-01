// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, 2014-2026 Terje Sandstrom
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
#if NET462
using System.Runtime.Remoting;
#endif
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.Internal;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter;

/// <summary>
/// NUnitEventListener implements the EventListener interface and
/// translates each event into a message for the VS test platform.
/// </summary>
public class NUnitEventListener(ITestConverterCommon testConverter, INUnit3TestExecutor executor)
    :
#if NET462
        MarshalByRefObject,
#endif
        ITestEventListener, IDisposable // Public for testing
{
    private static readonly ICollection<INUnitTestEventTestOutput> EmptyNodes = [];
    private ITestExecutionRecorder Recorder { get; } = executor.FrameworkHandle;
    private ITestConverterCommon TestConverter { get; } = testConverter;
    private IAdapterSettings Settings { get; } = executor.Settings;
    private Dictionary<string, ICollection<INUnitTestEventTestOutput>> OutputNodes { get; } = [];

#if NET462
    public override object InitializeLifetimeService()
    {
        // Give the listener an infinite lease lifetime by returning null
        // https://msdn.microsoft.com/en-us/magazine/cc300474.aspx#edupdate
        // This also means RemotingServices.Disconnect() must be called to prevent memory leaks
        // https://nbevans.wordpress.com/2011/04/17/memory-leaks-with-an-infinite-lifetime-instance-of-marshalbyrefobject/
        return null;
    }
#endif

    private INUnit3TestExecutor Executor { get; } = executor;

    #region ITestEventListener

    public void OnTestEvent(string report)
    {
        if (Executor.IsCancelled)
        {
            // Stop processing events when cancelled
            return;
        }

        var node = new NUnitTestEventHeader(report);
        dumpXml?.AddTestEvent(node.AsString());
        try
        {
            switch (node.Type)
            {
                case NUnitTestEventHeader.EventType.StartTest:
                    TestStarted(new NUnitTestEventStartTest(node));
                    break;

                case NUnitTestEventHeader.EventType.TestCase:
                    TestFinished(new NUnitTestEventTestCase(node));
                    break;

                case NUnitTestEventHeader.EventType.TestSuite:
                    SuiteFinished(new NUnitTestEventSuiteFinished(node));
                    break;

                case NUnitTestEventHeader.EventType.TestOutput:
                    TestOutput(new NUnitTestEventTestOutput(node));
                    break;
            }
        }
        catch (Exception ex)
        {
            Recorder.SendMessage(TestMessageLevel.Warning, $"Error processing {node.Name} event for {node.FullName}");
            Recorder.SendMessage(TestMessageLevel.Warning, ex.ToString());
        }
    }

    #endregion

    #region IDisposable
    private bool disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
#if NET462
                RemotingServices.Disconnect(this);
#endif
            }
        }
        disposed = true;
    }

    ~NUnitEventListener()
    {
        Dispose(false);
    }
    #endregion

    public void TestStarted(INUnitTestEventStartTest testNode)
    {
        if (Executor.IsCancelled)
        {
            return;
        }

        var ourCase = TestConverter.GetCachedTestCase(testNode.Id);

        // Simply ignore any TestCase not found in the cache
        if (ourCase != null)
            Recorder.RecordStart(ourCase);
    }

    /// <summary>
    /// Collects up all text output messages in the current test, and outputs them here.
    /// Note:  Error and Progress are handled in TestOutput.
    /// </summary>
    /// <param name="resultNode">resultNode.</param>
    public void TestFinished(INUnitTestEventTestCase resultNode)
    {
        if (Executor.IsCancelled)
        {
            return;
        }

        var testId = resultNode.Id;
        if (OutputNodes.TryGetValue(testId, out var outputNodes))
        {
            OutputNodes.Remove(testId);
        }

        var result = TestConverter.GetVsTestResults(resultNode, outputNodes ?? EmptyNodes);
        if (Settings.ConsoleOut >= 1)
        {
            if (!result.ConsoleOutput.IsNullOrWhiteSpace() && result.ConsoleOutput != Nl)
            {
                string msg = result.ConsoleOutput;
                if (Settings.UseTestNameInConsoleOutput)
                    msg = $"{resultNode.Name}: {msg}";
                var messageLevel = Settings.ConsoleOut == 1
                    ? TestMessageLevel.Informational
                    : TestMessageLevel.Warning;
                Recorder.SendMessage(messageLevel, msg);
            }
            if (!resultNode.ReasonMessage.IsNullOrWhiteSpace())
            {
                Recorder.SendMessage(TestMessageLevel.Informational, $"{resultNode.Name}: {resultNode.ReasonMessage}");
            }
        }

        if (result.TestCaseResult != null)
        {
            Recorder.RecordEnd(result.TestCaseResult.TestCase, result.TestCaseResult.Outcome);
            foreach (var vsResult in result.TestResults)
            {
                Recorder.RecordResult(vsResult);
            }

            if (result.TestCaseResult.Outcome == TestOutcome.Failed && Settings.StopOnError)
            {
                Executor.StopRun();
            }
        }
    }

    public void SuiteFinished(INUnitTestEventSuiteFinished resultNode)
    {
        if (Executor.IsCancelled)
        {
            return;
        }

        if (!resultNode.IsFailed)
            return;
        var site = resultNode.Site();
        if (site != NUnitTestEvent.SiteType.Setup && site != NUnitTestEvent.SiteType.TearDown)
            return;
        Recorder.SendMessage(TestMessageLevel.Error, $"{site} failed for test fixture {resultNode.FullName}");

        if (resultNode.HasFailure)
        {
            string msg = resultNode.FailureMessage;
            var stackNode = resultNode.StackTrace;
            if (!string.IsNullOrEmpty(stackNode) && Settings.IncludeStackTraceForSuites)
                msg += $"\nStackTrace: {stackNode}";
            Recorder.SendMessage(TestMessageLevel.Error, msg);
        }
    }

    private static readonly string Nl = Environment.NewLine;
    private static readonly int NlLength = Nl.Length;
    private readonly IDumpXml dumpXml = executor.Dump;

    /// <summary>
    /// Error stream and Progress stream are both sent here.
    /// </summary>
    /// <param name="outputNodeEvent">outputNodeEvent.</param>
    public void TestOutput(INUnitTestEventTestOutput outputNodeEvent)
    {
        if (Executor.IsCancelled)
        {
            return;
        }

        if (Settings.ConsoleOut == 0)
            return;
        string text = outputNodeEvent.Content;

        // Remove final newline since logger will add one
        if (text.EndsWith(Nl))
            text = text.Substring(0, text.Length - NlLength);

        if (text.IsNullOrWhiteSpace())
        {
            return;
        }

        string testId = outputNodeEvent.TestId;
        if (!string.IsNullOrEmpty(testId))
        {
            if (!OutputNodes.TryGetValue(testId, out var outputNodes))
            {
                outputNodes = [];
                OutputNodes.Add(testId, outputNodes);
            }

            outputNodes.Add(outputNodeEvent);
        }

        var testMessageLevel = outputNodeEvent.IsErrorStream
            ? TestMessageLevel.Warning
            : TestMessageLevel.Informational;

        Recorder.SendMessage(testMessageLevel, text);
    }
}