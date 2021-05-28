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

using System.Collections.Generic;
using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitTestCase : INUnitTestNode
    {
        bool IsTestCase { get; }
        bool IsParameterizedMethod { get; }
        string Type { get; }
        string ClassName { get; }
        string MethodName { get; }
        RunStateEnum RunState { get; }
        NUnitEventTestCase Parent { get; }
    }

    public enum RunStateEnum
    {
        NA,
        Runnable,
        Explicit,
        NotRunnable
    }

    public interface INUnitTestCasePropertyInfo
    {
        RunStateEnum RunState { get; }
        IEnumerable<NUnitProperty> Properties { get; }
    }

    public class NUnitEventTestCase : NUnitTestNode, INUnitTestCase, INUnitTestCasePropertyInfo
    {
        public bool IsTestCase => !IsNull && Node.Name == "test-case";
        public bool IsParameterizedMethod => Type == "ParameterizedMethod";
        public string Type => Node.GetAttribute("type");
        public string ClassName => Node.GetAttribute("classname");
        public string MethodName => Node.GetAttribute("methodname");

        private RunStateEnum runState = RunStateEnum.NA;

        public RunStateEnum RunState
        {
            get
            {
                if (runState == RunStateEnum.NA)
                {
                    runState = Node.GetAttribute("runstate") switch
                    {
                        "Runnable" => RunStateEnum.Runnable,
                        "Explicit" => RunStateEnum.Explicit,
                        "NotRunnable" => RunStateEnum.NotRunnable,
                        _ => runState
                    };
                }
                return runState;
            }
        }

        public NUnitEventTestCase(XmlNode testCase) : base(testCase)
        {
            if (Node.ParentNode != null)
                Parent = new NUnitEventTestCase(Node.ParentNode);
        }

        public NUnitEventTestCase Parent { get; }
    }
}