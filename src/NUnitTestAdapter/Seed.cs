using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter;

public class Seed
{
    internal static readonly TestProperty NUnitSeedProperty = TestProperty.Register(
        "NUnit.Seed",
        "Seed", typeof(string), TestPropertyAttributes.None, typeof(TestCase));

    internal static readonly TestProperty NUnitClassName = TestProperty.Register(
        "NUnit.ClassName",
        "NUnit Class Name", typeof(string), TestPropertyAttributes.None, typeof(TestCase));

    internal static readonly TestProperty NUnitMethodName = TestProperty.Register(
        "NUnit.MethodName",
        "NUnit Method Name", typeof(string), TestPropertyAttributes.None, typeof(TestCase));
}