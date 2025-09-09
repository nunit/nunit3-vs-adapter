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

using System;
using System.Linq;
using System.Xml;
using NUnit.VisualStudio.TestAdapter.Dump;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine;

public class NUnitResults
{
    public XmlNode TopNode { get; }
    public bool IsRunnable { get; }

    public int TestCaseCount { get; }

    public string AsString() => FullTopNode.AsString();

    public XmlNode FullTopNode { get; }

    public NUnitResults(XmlNode results)
    {
        FullTopNode = results;
        // Currently, this will always be the case but it might change
        TopNode = results.Name == "test-run" ? results.FirstChild : results;

        int.TryParse(TopNode.GetAttribute("testcasecount"), out int testcasecount);
        TestCaseCount = testcasecount;

        // ReSharper disable once StringLiteralTypo
        IsRunnable = TopNode.GetAttribute("runstate") == "Runnable";
    }

    public string SkipReason()
    {
        var msgNode = TopNode.SelectSingleNode("properties/property[@name='_SKIPREASON']");
        return msgNode?.GetAttribute("value");
    }

    public XmlNodeList TestCases()
    {
        return TopNode.SelectNodes("//test-case");
    }
}