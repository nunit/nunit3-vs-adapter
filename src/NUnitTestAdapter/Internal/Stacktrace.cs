//------------------------------------------------------------------------------
// <copyright file='Stacktrace.cs' company='Microsoft Corporation'>
//     Copyright (c) Microsoft Corporation. All Rights Reserved.
//     Information Contained Herein is Proprietary and Confidential.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Linq;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
    internal class Stacktrace
    {
        private readonly List<StackFrame> stackFrames;

        public Stacktrace(string stackTrace)
        {
            ValidateArg.NotNullOrEmpty(stackTrace, "stackTrace");

            var stackFrameParser = StackFrameParser.CreateStackFrameParser();

            var lines = stackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            stackFrames = lines.Select(stackFrameParser.GetStackFrame).Where(stackFrame => stackFrame != null).ToList();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ReadOnlyCollection<StackFrame> StackFrames
        {
            get { return stackFrames.AsReadOnly(); }
        }

        public StackFrame GetTopStackFrame()
        {
            return stackFrames.FirstOrDefault(stackframe => !string.IsNullOrEmpty(stackframe.FileName));
        }
    }
}
