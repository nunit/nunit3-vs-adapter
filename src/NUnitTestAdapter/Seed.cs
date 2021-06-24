using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public class Seed
    {
        internal static readonly TestProperty NUnitSeedProperty = TestProperty.Register(
            "NUnit.Seed",
            "Seed", typeof(string), TestPropertyAttributes.None, typeof(TestCase));
    }
}
