// ***********************************************************************
// Copyright (c) 2020-2021 Charlie Poole, Terje Sandstrom
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

using System.Xml;
using NUnit.VisualStudio.TestAdapter.Dump;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public class NUnitTestEventHeader : INUnitTestEventForXml
    {
        public enum EventType
        {
            NoIdea,
            StartTest,  // Match: A test was started
            TestCase,   // Match: A test was finished
            TestSuite,  // Match: A suite was finished
            TestOutput, // Match: Test output, not part of test results, but should be added to it
            StartRun,  // Currently not used
            StartSuite // Currently not used
        }
        public XmlNode Node { get; }
        public string AsString() => Node.AsString();
        public string FullName => Node.GetAttribute("fullname");
        public string Name => Node.GetAttribute("name");
        public EventType Type { get; }
        public NUnitTestEventHeader(string sNode)
        {
            Node = XmlHelper.CreateXmlNode(sNode);
            Type = Node.Name switch
            {
                "start-test" => EventType.StartTest,
                "test-case" => EventType.TestCase,
                "test-suite" => EventType.TestSuite,
                "test-output" => EventType.TestOutput,
                "start-run" => EventType.StartRun,
                "start-suite" => EventType.StartSuite,
                _ => EventType.NoIdea
            };
        }
    }
}