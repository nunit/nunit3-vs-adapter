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

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    public class NUnitTestCaseTests
    {
        private const string XmlRunnable = @"<test-case id='0-1007' name='Test2M' fullname='TestWarnings.Test4.Test2M' methodname='Test2M' classname='TestWarnings.Test4' runstate='Runnable' seed='882017471' />";

        private const string XmlExplicit = @"<test-case id='0-1007' name='Test2M' fullname='TestWarnings.Test4.Test2M' methodname='Test2M' classname='TestWarnings.Test4' runstate='Explicit' seed='882017471' />";

        private const string XmlNone = @"<test-case id='0-1007' name='Test2M' fullname='TestWarnings.Test4.Test2M' methodname='Test2M' classname='TestWarnings.Test4' seed='882017471' />";


        [Test]
        public void ThatRunStateIsHandledForRunnable()
        {
            var sut = new NUnitEventTestCase(XmlHelper.CreateXmlNode(XmlRunnable));
            Assert.That(sut.RunState, Is.EqualTo(RunStateEnum.Runnable));
        }
        [Test]
        public void ThatRunStateIsHandledForExplicit()
        {
            var sut = new NUnitEventTestCase(XmlHelper.CreateXmlNode(XmlExplicit));
            Assert.That(sut.RunState, Is.EqualTo(RunStateEnum.Explicit));
        }

        [Test]
        public void ThatRunStateIsHandledForNone()
        {
            var sut = new NUnitEventTestCase(XmlHelper.CreateXmlNode(XmlNone));
            Assert.That(sut.RunState, Is.EqualTo(RunStateEnum.NA));
        }
    }
}