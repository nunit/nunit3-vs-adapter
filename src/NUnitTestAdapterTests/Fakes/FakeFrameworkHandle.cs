using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    /// <summary>
    /// FakeFrameworkHandle is used in all tests that require an
    /// IFrameworkHandle, ITestExecutionRecorder or IMessageLogger.
    /// </summary>
    class FakeFrameworkHandle : IFrameworkHandle
    {
        #region Constructor

        public FakeFrameworkHandle()
        {
            Events = new List<Event>();
        }

        #endregion

        #region Nested Types

        public enum EventType
        {
            RecordStart,
            RecordEnd,
            RecordResult,
            SendMessage
        }

        public struct TextMessage
        {
            public TestMessageLevel Level { get; set; }
            public string Text { get; set; }
        }

        public struct Event
        {
            public EventType EventType { get; set; }
            public TestCase TestCase { get; set; }
            public TestResult TestResult { get; set; }
            public TestOutcome TestOutcome { get; set; }
            public TextMessage Message { get; set; }
        }

        #endregion

        #region Properties

        public List<Event> Events { get; private set; }

        #endregion

        #region IFrameworkHandle Members

        bool IFrameworkHandle.EnableShutdownAfterTestRun
        {
            get; set;
        }

        int IFrameworkHandle.LaunchProcessWithDebuggerAttached(string filePath, string workingDirectory, string arguments, IDictionary<string, string> environmentVariables)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ITestExecutionRecorder Members

        void ITestExecutionRecorder.RecordStart(TestCase testCase)
        {
            Events.Add(new Event
            {
                EventType = EventType.RecordStart,
                TestCase = testCase
            });
        }

        void ITestExecutionRecorder.RecordResult(TestResult testResult)
        {
            Events.Add(new Event
            {
                EventType = EventType.RecordResult,
                TestResult = testResult
            });
        }

        void ITestExecutionRecorder.RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            Events.Add(new Event
            {
                EventType = EventType.RecordEnd,
                TestCase = testCase,
                TestOutcome = outcome
            });
        }

        void ITestExecutionRecorder.RecordAttachments(IList<AttachmentSet> attachmentSets)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMessageLogger

        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            var textMessage = new TextMessage
            {
                Level = testMessageLevel,
                Text = message
            };

            Events.Add(new Event
            {
                EventType = EventType.SendMessage,
                Message = textMessage
            });
        }

        #endregion
    }
}
