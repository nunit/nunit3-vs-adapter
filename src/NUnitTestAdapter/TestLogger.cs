// ***********************************************************************
// Copyright (c) 2013-2021 Charlie Poole, Terje Sandstrom
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
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter
{
    public interface ITestLogger
    {
        void Error(string message);
        void Error(string message, Exception ex);
        void Warning(string message);
        void Warning(string message, Exception ex);
        void Info(string message);
        int Verbosity { get; set; }
        void Debug(string message);
    }

    /// <summary>
    /// TestLogger wraps an IMessageLogger and adds various
    /// utility methods for sending messages. Since the
    /// IMessageLogger is only provided when the discovery
    /// and execution objects are called, we use two-phase
    /// construction. Until Initialize is called, the logger
    /// simply swallows all messages without sending them
    /// anywhere.
    /// </summary>
    public class TestLogger : IMessageLogger, ITestLogger
    {
        private IAdapterSettings adapterSettings;
        private const string EXCEPTION_FORMAT = "Exception {0}, {1}";

        private IMessageLogger MessageLogger { get; }

        public int Verbosity { get; set; }

        public TestLogger(IMessageLogger messageLogger)
        {
            MessageLogger = messageLogger;
        }

        public TestLogger InitSettings(IAdapterSettings settings)
        {
            adapterSettings = settings;
            Verbosity = adapterSettings.Verbosity;
            return this;
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

        public void Warning(string message, Exception ex)
        {
            SendMessage(TestMessageLevel.Warning, message, ex);
        }

        #endregion

        #region Information Messages

        public void Info(string message)
        {
            if (adapterSettings?.Verbosity >= 0)
                SendMessage(TestMessageLevel.Informational, message);
        }

        #endregion

        #region Debug Messages

        public void Debug(string message)
        {
            if (adapterSettings?.Verbosity >= 5)
                SendMessage(TestMessageLevel.Informational, message);
        }

        #endregion

        #region SendMessage

        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            MessageLogger?.SendMessage(testMessageLevel, message);
        }

        public void SendMessage(TestMessageLevel testMessageLevel, string message, Exception ex)
        {
            switch (Verbosity)
            {
                case 0:
                    var type = ex.GetType();
                    SendMessage(testMessageLevel, string.Format(EXCEPTION_FORMAT, type, message));
                    SendMessage(testMessageLevel, ex.Message);
                    SendMessage(testMessageLevel, ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        SendMessage(testMessageLevel, $"InnerException: {ex.InnerException}");
                    }
                    break;

                default:
                    SendMessage(testMessageLevel, message);
                    SendMessage(testMessageLevel, ex.ToString());
                    SendMessage(testMessageLevel, ex.StackTrace);
                    break;
            }
        }
        #endregion

        #region SpecializedMessages
        public void DebugRunfrom()
        {
#if NET462
            string fw = ".Net Framework";
#else
            string fw = ".Net/ .Net Core";
#endif
            var assLoc = Assembly.GetExecutingAssembly().Location;
            Debug($"{fw} adapter running from {assLoc}");
            Debug($"Current directory: {Environment.CurrentDirectory}");
        }

        public void InfoNoTests(bool discoveryResultsHasNoNUnitTests, string assemblyPath)
        {
            Info(discoveryResultsHasNoNUnitTests
                ? "   NUnit couldn't find any tests in " + assemblyPath
                : "   NUnit failed to load " + assemblyPath);
        }

        public void InfoNoTests(string assemblyPath)
        {
            Info($"   NUnit couldn't find any tests in {assemblyPath}");
        }
        #endregion
    }
}
