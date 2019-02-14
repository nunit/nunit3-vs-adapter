using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;

namespace Referencing_Mono_Cecil
{
    public class ReferencingMonoCecilTests
    {
        [Test]
        public void UsesCorrectVersionOfMonoCecil()
        {
            const string versionNotUsedByNUnit = "0.10.0.0-beta5";

            var assembly = typeof(Mono.Cecil.ReaderParameters)
#if NETCOREAPP1_0
                .GetTypeInfo()
#endif
                .Assembly;

            var versionBlock = FileVersionInfo.GetVersionInfo(assembly.Location);

            Assert.That(versionBlock.ProductVersion, Is.EqualTo(versionNotUsedByNUnit));
        }
    }
}
