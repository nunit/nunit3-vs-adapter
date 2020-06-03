//------------------------------------------------------------------------------
// <copyright file='StackFrame.cs' company='Microsoft Corporation'>
//     Copyright (c) Microsoft Corporation. All Rights Reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//------------------------------------------------------------------------------

namespace NUnit.VisualStudio.TestAdapter.Internal
{
    internal class StackFrame
    {
        public StackFrame(string methodName)
            : this(methodName, null, 0)
        {
        }

        public StackFrame(string methodName, string fileName, int lineNumber)
        {
            MethodName = methodName;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        public string FileName { get; }

        public int LineNumber { get; }

        public string MethodName { get; }
    }
}
