// ***********************************************************************
// Copyright (c) 2011-2017 Charlie Poole, Terje Sandstrom
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
using System.Xml.Linq;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture]
    public class ProjectTests
    {
#if NET35
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
        public void ThatAdapterInstallProjectHasVsixManifestWithUnitTestExtensionAsset()
        {
            var location = TestContext.CurrentContext.TestDirectory;
            var di = new DirectoryInfo(location).MoveUp(4);
            Assert.That(di,Is.Not.Null,"Invalid parent");
            var installDir = di.EnumerateDirectories("NUnit3TestAdapterInstall").SingleOrDefault();
            Assert.That(installDir,Is.Not.Null,$"Didn't find NUnit3TestAdapterInstall folder at {di.Name}");            var vsixManifestFile = installDir.EnumerateFiles("*.vsixmanifest").SingleOrDefault();
            Assert.That(vsixManifestFile,Is.Not.Null,$"Didn't find any vsixmanifestfile at folder {installDir.Name}");
            var vsixManifestTxt = File.ReadAllText(vsixManifestFile.FullName);
            Assert.That(vsixManifestTxt.Length,Is.GreaterThan(0),"No content in vsixmanifestfile");

            var vsixManifest = XDocument.Parse(vsixManifestTxt);
            var desc = vsixManifest.Descendants();
            var assets = desc.FirstOrDefault(o=>o.Name.LocalName=="Assets");
            Assert.That(assets,Is.Not.Null,"Missing Assets");
            var assetItems = vsixManifest.Descendants().Where(o => o.Name.LocalName == "Asset").ToList();
            Assert.That(assetItems.Count,Is.GreaterThanOrEqualTo(1),"Missing asset items");
            var unitTestAsset = assetItems.FirstOrDefault(o => o.Attribute("Type") != null && o.Attribute("Type").Value!=null &&
                                                      o.Attribute("Type").Value == "UnitTestExtension");
            Assert.That(unitTestAsset,Is.Not.Null, "No asset with type UnitTestExtension found");
            var path = unitTestAsset.Attribute("Path");
            Assert.That(path,Is.Not.Null,"UnitTestAsset must have path");
            Assert.That(path.Value.EndsWith("NUnit3.TestAdapter.dll"), "UnitTestAsset path must contain the NUNit3TestAdapter.dll");

        }


        [Test]
        public void ThatNoMSTestDLLIsCopiedToOutput()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            var filesNotToExist = Directory.EnumerateFiles(dir, "Microsoft", SearchOption.TopDirectoryOnly);
            Assert.IsTrue(!filesNotToExist.Any(),"The reference of NUnitTestAdapter - Microsoft.VisualStudio.TestPlatform.ObjectModel must be set Copy Local to false");
        }

       

    }

    public static class DirectoryInfoExtensions
    {
        public static DirectoryInfo MoveUp(this DirectoryInfo di, int noOfLevels)
        {
            var grandParent = di;
            for (int i = 0; i < noOfLevels; i++)
                grandParent= grandParent?.Parent;
            return grandParent;
        }
    }

    

}
