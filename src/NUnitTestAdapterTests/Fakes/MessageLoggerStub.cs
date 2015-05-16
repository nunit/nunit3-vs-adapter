// ****************************************************************
// Copyright (c) 2015 NUnit Software. All rights reserved.
// ****************************************************************

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    public class MessageLoggerStub : IMessageLogger
    {
        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            // Do nothing
        }
    }
}
