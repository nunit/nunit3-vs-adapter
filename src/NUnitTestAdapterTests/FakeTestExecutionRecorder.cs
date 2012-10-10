using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System;
using System.Collections.Generic;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    class FakeTestExecutionRecorder : ITestExecutionRecorder
    {
        #region Constructor

        public FakeTestExecutionRecorder()
        {
            RecordResultCalls = 0;
            SendMessageCalls = 0;
            LastMessageLevel = (LastMessageLevel) - 1;
            LastMessage = null;
            LastResult = null;
        }

        #endregion

        #region Properties

        public int RecordResultCalls { get; private set; }
        public int SendMessageCalls { get; private set; }

        public TestResult LastResult { get; private set; }

        public TestMessageLevel LastMessageLevel { get; private set; }
        public string LastMessage { get; private set; }

        #endregion

        #region ITestExecutionRecorder

        public void RecordStart(TestCase testCase)
        {
            throw new NotImplementedException();
        }

        public void RecordResult(TestResult testResult)
        {
            RecordResultCalls++;
            LastResult = testResult;
        }

        public void RecordEnd(TestCase testCase, TestOutcome outcome)
        {
            throw new NotImplementedException();
        }

        public void RecordAttachments(IList<AttachmentSet> attachmentSets)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMessageLogger

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            SendMessageCalls++;
            LastMessageLevel = testMessageLevel;
            LastMessage = message;
        }

        #endregion
    }
}
