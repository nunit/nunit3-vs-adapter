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
		private readonly string fileName;
		private readonly int lineNumber;
		private readonly string methodName;

		public StackFrame(string methodName)
			: this(methodName, null, 0)
		{
		}

		public StackFrame(string methodName, string fileName, int lineNumber)
		{
			this.methodName = methodName;
			this.fileName = fileName;
			this.lineNumber = lineNumber;
		}

		public string FileName { get { return fileName; } }

		public int LineNumber { get { return lineNumber; } }

		public string MethodName { get { return methodName; } }
	}
}
