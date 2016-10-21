// ****************************************************************
// Copyright (c) 2013 NUnit Software. All rights reserved.
// ****************************************************************

using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// TestLogger wraps an IMessageLogger and adds various
    /// utility methods for sending messages. Since the
    /// IMessageLogger is only provided when the discovery
    /// and execution objects are called, we use two-phase
    /// construction. Until Initialize is called, the logger
    /// simply swallows all messages without sending them
    /// anywhere.
    /// </summary>
    public class TestLogger : IMessageLogger
    {
        private const string EXCEPTION_FORMAT = "Exception {0}, {1}";

        private IMessageLogger MessageLogger { get; set; }

        public int Verbosity { get; set; }

        public TestLogger(IMessageLogger messageLogger) : this(messageLogger, 0) { }

        public TestLogger(IMessageLogger messageLogger, int verbosity)
        {
            MessageLogger = messageLogger;
            Verbosity = verbosity;
        }

        #region Error Messages

        public void Error(string message)
        {
            SendMessage(TestMessageLevel.Error, message);
        }

        public void Error(string message, Exception ex)
        {
            SendMessage(TestMessageLevel.Error, message, ex);
        }

        #endregion

        #region Warning Messages

        public void Warning(string message)
        {
            SendMessage(TestMessageLevel.Warning, message);
        }

        public void Warning(string message,Exception ex)
        {
            SendMessage(TestMessageLevel.Warning, message, ex);
        }

        #endregion

        #region Information Messages

        public void Info(string message)
        {
            SendMessage(TestMessageLevel.Informational, message);
        }

        #endregion

        #region Debug Messages

        public void Debug(string message)
        {
#if DEBUG
            SendMessage(TestMessageLevel.Informational, message);
#endif
        }

        #endregion

        #region SendMessage

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            if (MessageLogger != null)
                MessageLogger.SendMessage(testMessageLevel, message);
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message, Exception ex)
        {
            switch (Verbosity)
            {
                case 0:
                    var type = ex.GetType();
                    SendMessage(testMessageLevel, string.Format(EXCEPTION_FORMAT, type, message));
                    SendMessage(testMessageLevel, ex.Message);
                    break;

                default:
                    SendMessage(testMessageLevel, message);
                    SendMessage(testMessageLevel, ex.ToString());
                    break;
            }
        }
        #endregion
    }
}
