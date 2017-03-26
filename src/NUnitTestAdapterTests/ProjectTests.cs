using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [TestFixture]
    public class ProjectTests
    {
#if !NETCOREAPP1_0
        [Test]
        public void ThatTheTestAdapterUsesFrameWork35()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            var assembly = Assembly.LoadFrom(dir+"/NUnit3.TestAdapter.dll");
            var version = assembly.ImageRuntimeVersion;
            Assert.That(version,Is.EqualTo("v2.0.50727"),"The NUnitTestAdapter project must be set to target .net framework 3.5");
        }

        [Test]
        public void ThatTheReferenceToMicrosoftTestObjectModelPointsToVS2012Version()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            var assembly = Assembly.LoadFrom(dir + "/NUnit3.TestAdapter.dll");
            var refNames = assembly.GetReferencedAssemblies().Where(ass => ass.Name == "Microsoft.VisualStudio.TestPlatform.ObjectModel").ToList();
            Assert.IsTrue(refNames != null && refNames.Count() == 1, "No reference to Microsoft.VisualStudio.TestPlatform.ObjectModel found");
            Assert.IsTrue(refNames[0].Version.Major == 11, "Microsoft.VisualStudio.TestPlatform.ObjectModel must point to the 2012 version (11)");
        }
#endif

        [Test]
        public void ThatTheTestAdapterEndsWithTestAdapterDll()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            bool found = File.Exists(dir + "/NUnit3.TestAdapter.dll");
            Assert.That(found,Is.True,string.Format(@"Did not find 'NUnit3.TestAdapter.dll' in {0}. Ensure the Testadapter ends with '.TestAdapter.dll'",dir));
        }

        [Test]
        public void ThatNoMSTestDLLIsCopiedToOutput()
        {
            var dir = TestContext.CurrentContext.TestDirectory;
            var filesNotToExist = Directory.EnumerateFiles(dir, "Microsoft", SearchOption.TopDirectoryOnly);
            Assert.IsTrue(!filesNotToExist.Any(),"The reference of NUnitTestAdapter - Microsoft.VisualStudio.TestPlatform.ObjectModel must be set Copy Local to false");
        }
    }
}
