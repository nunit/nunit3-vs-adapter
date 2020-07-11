// ***********************************************************************
// Copyright (c) 2020 Charlie Poole, Terje Sandstrom
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

#if !NETSTANDARD1_6
using System.Runtime.Serialization;
#endif

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter
{
    /// <summary>
    /// TestSelectionParserException is thrown when an error
    /// is found while parsing the selection expression.
    /// </summary>
#if !NETSTANDARD1_6
    [Serializable]
#endif
    public class TestFilterParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestFilterParserException"/> class.
        /// Construct with a message.
        /// </summary>
        public TestFilterParserException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestFilterParserException"/> class.
        /// Construct with a message and inner exception.
        /// </summary>
        public TestFilterParserException(string message, Exception innerException) : base(message, innerException) { }

#if !NETSTANDARD1_6
        /// <summary>
        /// Initializes a new instance of the <see cref="TestFilterParserException"/> class.
        /// Serialization constructor.
        /// </summary>
        public TestFilterParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif
    }
}
