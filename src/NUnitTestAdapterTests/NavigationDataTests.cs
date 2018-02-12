using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    [Category("Navigation")]
    public class NavigationDataTests
    {
        NavigationDataProvider _provider;

        const string Prefix = "NUnit.VisualStudio.TestAdapter.Tests.NavigationTestData";

        [SetUp]
        public void SetUp()
        {
            _provider = new NavigationDataProvider(
                typeof(NavigationDataTests).GetTypeInfo().Assembly.Location);
        }

        [TearDown]
        public void TearDown()
        {
            _provider.Dispose();
        }

        [TestCase("", "EmptyMethod_OneLine", 11, 11)]
        [TestCase("", "EmptyMethod_TwoLines", 14, 15)]
        [TestCase("", "EmptyMethod_ThreeLines", 18, 19)]
        [TestCase("", "EmptyMethod_LotsOfLines", 22, 25)]
        [TestCase("", "SimpleMethod_Void_NoArgs", 28, 30)]
        [TestCase("", "SimpleMethod_Void_OneArg", 34, 35)]
        [TestCase("", "SimpleMethod_Void_TwoArgs", 40, 41)]
        [TestCase("", "SimpleMethod_ReturnsInt_NoArgs", 46, 48)]
        [TestCase("", "SimpleMethod_ReturnsString_OneArg", 52, 53)]
        // Generic method uses simple name
        [TestCase("", "GenericMethod_ReturnsString_OneArg", 57, 58)]
        [TestCase("", "AsyncMethod_Void", 62, 64)]
        [TestCase("", "AsyncMethod_Task", 69, 71)]
        [TestCase("", "AsyncMethod_ReturnsInt", 76, 78)]
        [TestCase("", "IteratorMethod_ReturnsEnumerable", 83, 85)]
        [TestCase("", "IteratorMethod_ReturnsEnumerator", 89, 91)]
        // Nested classes use Type.FullName format
        [TestCase("+NestedClass", "SimpleMethod_Void_NoArgs", 97, 99)]
        [TestCase("+ParameterizedFixture", "SimpleMethod_ReturnsString_OneArg", 115, 116)]
        // Generic Fixture requires ` plus type arg count
        [TestCase("+GenericFixture`2", "Matches", 130, 131)]
        [TestCase("+GenericFixture`2+DoublyNested", "WriteBoth", 146, 147)]
        [TestCase("+GenericFixture`2+DoublyNested`1", "WriteAllThree", 165, 166)]
        [TestCase("+DerivedClass", "EmptyMethod_ThreeLines", 174, 175)]
        public void VerifyNavigationData_WithinAssembly(string suffix, string methodName, int expectedLineDebug, int expectedLineRelease)
        {
            VerifyNavigationData(suffix, methodName, "NUnitTestAdapterTests", expectedLineDebug, expectedLineRelease);
        }
      
#if !NETCOREAPP1_0
        // .NET Standard does not have the assembly resolvers, so the fixes for this do not work
        [TestCase("+DerivedFromExternalAbstractClass", "EmptyMethod_ThreeLines", 6, 7)]
        [TestCase("+DerivedFromExternalConcreteClass", "EmptyMethod_ThreeLines", 13, 14)]
        public void VerifyNavigationData_WithExternalAssembly(string suffix, string methodName, int expectedLineDebug, int expectedLineRelease)
        {
            VerifyNavigationData(suffix, methodName, "NUnit3AdapterExternalTests", expectedLineDebug, expectedLineRelease);
        }
#endif

        private void VerifyNavigationData(string suffix, string methodName, string expectedDirectory, int expectedLineDebug, int expectedLineRelease)
        {
            // Get the navigation data - ensure names are spelled correctly!
            var className = Prefix + suffix;
            var data = _provider.GetNavigationData(className, methodName);
            Assert.That(data.IsValid, "Unable to retrieve navigation data");

            // Verify the navigation data
            // Note that different line numbers are returned for our
            // debug and release builds, as follows:
            //
            // DEBUG
            //   Opening curly brace.
            //
            // RELEASE
            //   First non-comment code line or closing
            //   curly brace if the method is empty.

            Assert.That(data.FilePath, Does.EndWith("NavigationTestData.cs"));
            Assert.That(Path.GetDirectoryName(data.FilePath), Does.EndWith(expectedDirectory));
#if DEBUG
            Assert.That(data.LineNumber, Is.EqualTo(expectedLineDebug));
#else
            Assert.That(data.LineNumber, Is.EqualTo(expectedLineRelease));
#endif
        }
    }
}
