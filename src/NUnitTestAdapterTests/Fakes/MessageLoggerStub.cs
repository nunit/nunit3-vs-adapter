// ***********************************************************************
// Copyright (c) 2015-2017 Charlie Poole, Terje Sandstrom
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
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    public class MessageLoggerStub : IMessageLogger
    {
        private readonly List<Tuple<TestMessageLevel, string>> messages = new ();
        public void SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            messages.Add(new Tuple<TestMessageLevel, string>(testMessageLevel, message));
        }

        public TestMessageLevel LatestTestMessageLevel => messages.Last().Item1;
        public string LatestMessage => messages.Last().Item2;

        public int Count => messages.Count;

        public IEnumerable<Tuple<TestMessageLevel, string>> Messages => messages;
        public IEnumerable<Tuple<TestMessageLevel, string>> WarningMessages => messages.Where(o => o.Item1 == TestMessageLevel.Warning);
        public IEnumerable<Tuple<TestMessageLevel, string>> ErrorMessages => messages.Where(o => o.Item1 == TestMessageLevel.Error);
    }
}
