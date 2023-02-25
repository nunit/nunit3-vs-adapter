// ***********************************************************************
// Copyright (c) 2011-2017 Charlie Poole, 2014-2021 Terje Sandstrom
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
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture]
    public class ProjectTests
    {
#if NET462
        [Test]
        public void ThatTheReferenceToMicrosoftTestObjectModelPointsToVS2012Version()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            var assembly = Assembly.LoadFrom(dir + "/NUnit3.TestAdapter.dll");
            var refNames = assembly.GetReferencedAssemblies().Where(ass => ass.Name == "Microsoft.VisualStudio.TestPlatform.ObjectModel").ToList();
            Assert.IsTrue(refNames != null && refNames.Count == 1, "No reference to Microsoft.VisualStudio.TestPlatform.ObjectModel found");
            Assert.IsTrue(refNames[0].Version.Major == 11, "Microsoft.VisualStudio.TestPlatform.ObjectModel must point to the 2012 version (11)");
        }
#endif

        [Test]
        public void ThatTheTestAdapterEndsWithTestAdapterDll()
        {
            var adapter = typeof(NUnitTestAdapter).GetTypeInfo().Assembly.Location;
            Assert.That(adapter, Does.EndWith(".TestAdapter.dll"), $"Ensure the Testadapter {Path.GetFileName(adapter)} ends with '.TestAdapter.dll'");
        }

        [Test]
        public void ThatNoMSTestDLLIsCopiedToOutput()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            var filesNotToExist = Directory.EnumerateFiles(dir, "Microsoft", SearchOption.TopDirectoryOnly);
            Assert.That(!filesNotToExist.Any(), Is.True, "The reference of NUnitTestAdapter - Microsoft.VisualStudio.TestPlatform.ObjectModel must be set Copy Local to false");
        }
    }

    public static class DirectoryInfoExtensions
    {
        public static DirectoryInfo MoveUp(this DirectoryInfo di, int noOfLevels)
        {
            var grandParent = di;
            for (int i = 0; i < noOfLevels; i++)
                grandParent = grandParent?.Parent;
            return grandParent;
        }
    }
}