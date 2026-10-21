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

    /// <summary>
    /// Full name of the fixture instance that holds the test, e.g. Ns.Fixture("A") for a parameterized fixture.
    /// </summary>
    internal static readonly TestProperty NUnitFixtureName = TestProperty.Register(
        "NUnit.FixtureName",
        "NUnit Fixture Name", typeof(string), TestPropertyAttributes.None, typeof(TestCase));
}