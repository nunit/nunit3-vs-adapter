// ***********************************************************************
// Copyright (c) 2012 Charlie Poole, Terje Sandstrom
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    class FakeRunContext : FakeDiscoveryContext, IRunContext
    {
        public FakeRunContext() : base(new FakeRunSettings())
        {
        }


        public FakeRunContext(FakeRunSettings fakeRunSettings) : base(fakeRunSettings)
        {
        }

        #region IRunContext Members

        bool IRunContext.InIsolation => throw new NotImplementedException();

        bool IRunContext.IsBeingDebugged => throw new NotImplementedException();

        bool IRunContext.IsDataCollectionEnabled => throw new NotImplementedException();

        bool IRunContext.KeepAlive => true;

        string IRunContext.TestRunDirectory => throw new NotImplementedException();

        ITestCaseFilterExpression IRunContext.GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
        {
            return null;  // as if we don't have a TFS Build, equal to testing from VS
        }

        #endregion

        public string SolutionDirectory => throw new NotImplementedException();
    }
}
