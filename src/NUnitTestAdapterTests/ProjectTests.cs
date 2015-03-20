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
        [Test]
        public void ThatTheTestAdapterUsesFrameWork35()
        {
            var dir = Directory.GetCurrentDirectory();
            var assembly = Assembly.LoadFrom(dir+"/NUnit.VisualStudio.TestAdapter.dll");
            var version = assembly.ImageRuntimeVersion;
            Assert.That(version,Is.EqualTo("v2.0.50727"),"The NUnitTestAdapter project must be set to target .net framework 3.5");
        }

        [Test]
        public void ThatNoMSTestDLLIsCopiedToOutput()
        {
            var dir = Directory.GetCurrentDirectory();
            var filesNotToExist = Directory.EnumerateFiles(dir, "Microsoft", SearchOption.TopDirectoryOnly);
            Assert.IsTrue(!filesNotToExist.Any(),"The reference of NUnitTestAdapter - Microsoft.VisualStudio.TestPlatform.ObjectModel must be set Copy Local to false");
        }

        [Test]
        public void ThatTheReferenceToMicrosoftTestObjectModelPointsToVS2012Version()
        {
            var dir = Directory.GetCurrentDirectory();
            var assembly = Assembly.LoadFrom(dir + "/NUnit.VisualStudio.TestAdapter.dll");
            var refNames = assembly.GetReferencedAssemblies().Where(ass=>ass.Name=="Microsoft.VisualStudio.TestPlatform.ObjectModel").ToList();
            Assert.IsTrue(refNames != null && refNames.Count() == 1, "No reference to Microsoft.VisualStudio.TestPlatform.ObjectModel found");
            Assert.IsTrue(refNames[0].Version.Major == 11, "Microsoft.VisualStudio.TestPlatform.ObjectModel must point to the 2012 version (11)");

        }
    }
}
