// ***********************************************************************
// Copyright (c) 2019-2021 Charlie Poole, Terje Sandstrom
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

using System.Linq;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class CurrentDirectoryTests
    {
        [TestCase(@"C:\Windows")]
        [TestCase(@"C:\Windows\Whatever")]
        [TestCase(@"C:\Program Files\Something")]
        [TestCase(@"C:\Program Files (x86)\Something")]
        [Platform("Win")]
        public void ThatWeFindForbiddenFolders(string folder)
        {
            var sut = new NUnit3TestExecutor();
            sut.InitializeForbiddenFolders();
            Assert.That(sut.CheckDirectory(folder));
        }

        [TestCase(@"C:\Whatever")]
        [TestCase(@"C:\WindowsWhatever")]
        [TestCase(@"C:\Program Files Whatever\Something")]
        public void ThatWeAcceptNonForbiddenFolders(string folder)
        {
            var sut = new NUnit3TestExecutor();
            sut.InitializeForbiddenFolders();
            Assert.That(sut.CheckDirectory(folder), Is.False);
        }

        [Test]
        public void ThatForbiddenFoldersAreUnique()
        {
            var sut = new NUnit3TestExecutor();
            sut.InitializeForbiddenFolders();
            var sutunique = sut.ForbiddenFolders.Distinct();
            Assert.That(sutunique.Count(), Is.EqualTo(sut.ForbiddenFolders.Count), "There are duplicate entries in ForbiddenFolders");
        }
    }
}
