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

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitTestEventTestOutput
    {
        NUnitTestEventTestOutput.Streams Stream { get; }
        string TestId { get; }
        string TestName { get; }

        /// <summary>
        /// Returns the output information.
        /// </summary>
        string Content { get; }

        bool IsProgressStream { get; }
        bool IsErrorStream { get; }
        bool IsNullOrEmptyStream { get; }
    }

    /// <summary>
    /// Handles the 'test-output' event.
    /// </summary>
    public class NUnitTestEventTestOutput : NUnitTestEvent, INUnitTestEventTestOutput
    {
        public enum Streams
        {
            NoIdea,
            Error,
            Progress
        }

        public Streams Stream { get; }
        public string TestId => Node.GetAttribute("testid");

        public string TestName => Node.GetAttribute("testname");


        public NUnitTestEventTestOutput(INUnitTestEventForXml theEvent) : this(theEvent.Node)
        {
            if (theEvent.Node.Name != "test-output")
                throw new NUnitEventWrongTypeException($"Expected 'test-output', got {theEvent.Node.Name}");
        }
        public NUnitTestEventTestOutput(XmlNode node) : base(node)
        {
            Stream = node.GetAttribute("stream") switch
            {
                "Error" => Streams.Error,
                "Progress" => Streams.Progress,
                _ => Streams.NoIdea
            };
        }

        public bool IsProgressStream => Stream == Streams.Progress;
        public bool IsErrorStream => Stream == Streams.Error;

        public bool IsNullOrEmptyStream => Stream == Streams.NoIdea;

        /// <summary>
        /// Returns the output information.
        /// </summary>
        public string Content => Node.InnerText;

        // Notes:
        // The input doesnt have any id, but used testid instead.
        // Properties FullName and Name is not in use
    }
}