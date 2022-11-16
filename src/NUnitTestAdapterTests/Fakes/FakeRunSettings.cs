// ***********************************************************************
// Copyright (c) 2012-2021 Charlie Poole, Terje Sandstrom
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    class FakeRunSettings : IRunSettings
    {
        ISettingsProvider IRunSettings.GetSettings(string settingsName)
        {
            throw new NotImplementedException();
        }

        public virtual string SettingsXml => "<RunSettings><NUnit><SkipNonTestAssemblies>false</SkipNonTestAssemblies><Verbosity>5</Verbosity></NUnit></RunSettings>";
    }

    class FakeRunSettingsForTestOutput : FakeRunSettings
    {
        public override string SettingsXml => "<RunSettings><NUnit><TestOutputXml>TestResults</TestOutputXml><SkipNonTestAssemblies>false</SkipNonTestAssemblies></NUnit></RunSettings>";
    }

    class FakeRunSettingsForTestOutputAndWorkDir : FakeRunSettings
    {
        private readonly string _testOutput;
        private readonly string _workDir;

        public FakeRunSettingsForTestOutputAndWorkDir(string testOutput, string workDir)
        {
            _workDir = workDir;
            _testOutput = testOutput;
        }
        public override string SettingsXml => $"<RunSettings><NUnit><WorkDirectory>{_workDir}</WorkDirectory><TestOutputXml>{_testOutput}</TestOutputXml><SkipNonTestAssemblies>false</SkipNonTestAssemblies></NUnit></RunSettings>";
    }

    class FakeRunSettingsForWhere : FakeRunSettings
    {
        private readonly string _where;

        public FakeRunSettingsForWhere(string where)
        {
            _where = where;
        }
        public override string SettingsXml => $"<RunSettings><NUnit><Where>{_where}</Where><SkipNonTestAssemblies>false</SkipNonTestAssemblies></NUnit></RunSettings>";
    }
}
