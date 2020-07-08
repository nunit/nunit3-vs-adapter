// ***********************************************************************
// Copyright (c) 2011-2020 Charlie Poole, Terje Sandstrom
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
#if NET35
using System.Runtime.Remoting;
#endif
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.Internal;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitEventListener implements the EventListener interface and
    /// translates each event into a message for the VS test platform.
    /// </summary>
    public class NUnitEventListener :
#if NET35
        MarshalByRefObject,
#endif
        ITestEventListener, IDisposable // Public for testing
    {
        private static readonly ICollection<INUnitTestEventTestOutput> EmptyNodes = new List<INUnitTestEventTestOutput>();
        private readonly ITestExecutionRecorder _recorder;
        private readonly ITestConverterCommon _testConverter;
        private readonly IAdapterSettings _settings;
        private readonly Dictionary<string, ICollection<INUnitTestEventTestOutput>> _outputNodes = new Dictionary<string, ICollection<INUnitTestEventTestOutput>>();

#if NET35
        public override object InitializeLifetimeService()
        {
            // Give the listener an infinite lease lifetime by returning null
            // http://msdn.microsoft.com/en-us/magazine/cc300474.aspx#edupdate
            // This also means RemotingServices.Disconnect() must be called to prevent memory leaks
            // http://nbevans.wordpress.com/2011/04/17/memory-leaks-with-an-infinite-lifetime-instance-of-marshalbyrefobject/
            return null;
        }
#endif

        private readonly INUnit3TestExecutor executor;

        public NUnitEventListener(ITestConverterCommon testConverter, INUnit3TestExecutor executor)
        {
            this.executor = executor;
            dumpXml = executor.Dump;
            _settings = executor.Settings;
            _recorder = executor.FrameworkHandle;
            _testConverter = testConverter;
        }

        #region ITestEventListener

        public void OnTestEvent(string report)
        {
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
                _recorder.SendMessage(TestMessageLevel.Warning, $"Error processing {node.Name} event for {node.FullName}");
                _recorder.SendMessage(TestMessageLevel.Warning, ex.ToString());
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
#if NET35
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
            var ourCase = _testConverter.GetCachedTestCase(testNode.Id);

            // Simply ignore any TestCase not found in the cache
            if (ourCase != null)
                _recorder.RecordStart(ourCase);
        }

        public void TestFinished(INUnitTestEventTestCase resultNode)
        {
            var testId = resultNode.Id;
            if (_outputNodes.TryGetValue(testId, out var outputNodes))
            {
                _outputNodes.Remove(testId);
            }

            var result = _testConverter.GetVsTestResults(resultNode, outputNodes ?? EmptyNodes);
            if (_settings.ConsoleOut == 1 && !string.IsNullOrEmpty(result.ConsoleOutput))
            {
                _recorder.SendMessage(TestMessageLevel.Informational, result.ConsoleOutput);
            }
            if (_settings.ConsoleOut == 1 && !string.IsNullOrEmpty(resultNode.ReasonMessage))
            {
                _recorder.SendMessage(TestMessageLevel.Informational, $"{resultNode.Name}: {resultNode.ReasonMessage}");
            }

            _recorder.RecordEnd(result.TestCaseResult.TestCase, result.TestCaseResult.Outcome);
            foreach (var vsResult in result.TestResults)
            {
                _recorder.RecordResult(vsResult);
            }

            if (result.TestCaseResult.Outcome == TestOutcome.Failed && _settings.StopOnError)
            {
                executor.StopRun();
            }
        }

        public void SuiteFinished(INUnitTestEventSuiteFinished resultNode)
        {
            if (!resultNode.IsFailed)
                return;
            var site = resultNode.Site();
            if (site != NUnitTestEvent.SiteType.Setup && site != NUnitTestEvent.SiteType.TearDown)
                return;
            _recorder.SendMessage(TestMessageLevel.Warning, $"{site} failed for test fixture {resultNode.FullName}");

            if (resultNode.HasFailure)
                _recorder.SendMessage(TestMessageLevel.Warning, resultNode.FailureMessage);

            // Should not be any stacktrace on Suite-finished
            // var stackNode = resultNode.Failure.StackTrace;
            // if (!string.IsNullOrEmpty(stackNode))
            //    _recorder.SendMessage(TestMessageLevel.Warning, stackNode);
        }

        private static readonly string NL = Environment.NewLine;
        private static readonly int NL_LENGTH = NL.Length;
        private readonly IDumpXml dumpXml;

        public void TestOutput(INUnitTestEventTestOutput outputNodeEvent)
        {
            string text = outputNodeEvent.Content;

            // Remove final newline since logger will add one
            if (text.EndsWith(NL))
                text = text.Substring(0, text.Length - NL_LENGTH);

            if (text.IsNullOrWhiteSpace())
            {
                return;
            }

            string testId = outputNodeEvent.TestId;
            if (!string.IsNullOrEmpty(testId))
            {
                if (!_outputNodes.TryGetValue(testId, out var outputNodes))
                {
                    outputNodes = new List<INUnitTestEventTestOutput>();
                    _outputNodes.Add(testId, outputNodes);
                }

                outputNodes.Add(outputNodeEvent);
            }

            _recorder.SendMessage(TestMessageLevel.Warning, text);
        }
    }
}
