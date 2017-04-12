using System.Diagnostics;
using System.IO;
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
                Path.Combine(TestContext.CurrentContext.TestDirectory, "NUnit.VisualStudio.TestAdapter.Tests.dll"));
        }

        [TestCase("", "EmptyMethod_OneLine", 9, 9)]
        [TestCase("", "EmptyMethod_TwoLines", 12, 13)]
        [TestCase("", "EmptyMethod_ThreeLines", 16, 17)]
        [TestCase("", "EmptyMethod_LotsOfLines", 20, 23)]
        [TestCase("", "SimpleMethod_Void_NoArgs", 26, 28)]
        [TestCase("", "SimpleMethod_Void_OneArg", 32, 33)]
        [TestCase("", "SimpleMethod_Void_TwoArgs", 38, 39)]
        [TestCase("", "SimpleMethod_ReturnsInt_NoArgs", 44, 46)]
        [TestCase("", "SimpleMethod_ReturnsString_OneArg", 50, 51)]
        // Generic method uses simple name
        [TestCase("", "GenericMethod_ReturnsString_OneArg", 55, 56)]
        [TestCase("", "AsyncMethod_Void", 60, 62)]
        [TestCase("", "AsyncMethod_Task", 67, 69)]
        [TestCase("", "AsyncMethod_ReturnsInt", 74, 76)]
        // Nested classes use ECMA-335 format
        [TestCase("/NestedClass", "SimpleMethod_Void_NoArgs", 83, 85)]
        [TestCase("/ParameterizedFixture", "SimpleMethod_ReturnsString_OneArg", 101, 102)]
        // Generic Fixture requires ` plus type arg count
        [TestCase("/GenericFixture`2", "Matches", 116, 117)]
        [TestCase("/GenericFixture`2/DoublyNested", "WriteBoth", 132, 133)]
        [TestCase("/GenericFixture`2/DoublyNested`1", "WriteAllThree", 151, 152)]
        [TestCase("/DerivedClass", "EmptyMethod_ThreeLines", 160, 161)]
        // Handles sub class format from Type.FullName
        [TestCase("+NestedClass", "SimpleMethod_Void_NoArgs", 83, 85)]
        [TestCase("+GenericFixture`2+DoublyNested", "WriteBoth", 132, 133)]
        [TestCase("+GenericFixture`2+DoublyNested`1", "WriteAllThree", 151, 152)]
        public void VerifyNavigationData_WithinAssembly(string suffix, string methodName, int expectedLineDebug, int expectedLineRelease)
        {
            VerifyNavigationData(suffix, methodName, "NUnitTestAdapterTests", expectedLineDebug, expectedLineRelease);
        }

        [TestCase("/DerivedFromExternalAbstractClass", "EmptyMethod_ThreeLines", 6, 7)]
        [TestCase("/DerivedFromExternalConcreteClass", "EmptyMethod_ThreeLines", 13, 14)]
        public void VerifyNavigationData_WithExternalAssembly(string suffix, string methodName, int expectedLineDebug, int expectedLineRelease)
        {
#if LAUNCH_DEBUGGER
            if (!Debugger.IsAttached) { Debugger.Launch(); }
#endif
            VerifyNavigationData(suffix, methodName, "NUnit3AdapterExternalTests", expectedLineDebug, expectedLineRelease);
        }

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
