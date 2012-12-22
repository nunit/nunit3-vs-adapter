using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class NavigationDataTests
    {
        private static readonly string THIS_ASSEMBLY_PATH =
            Path.GetFullPath("NUnit.VisualStudio.TestAdapter.Tests.dll");

        NavigationData navData;

        const string prefix = "NUnit.VisualStudio.TestAdapter.Tests.NavigationTestData";

        [TestFixtureSetUp]
        public void InitializeNavigationData()
        {
            navData = new NavigationData(THIS_ASSEMBLY_PATH);
        }

        [TestFixtureTearDown]
        public void DisposeNavigationData()
        {
            navData.Dispose();
        }

        [TestCase("", "EmptyMethod_OneLine", 9, 9, 9, 9)]
        [TestCase("", "EmptyMethod_TwoLines", 12, 13, 13, 13)]
        [TestCase("", "EmptyMethod_ThreeLines", 16, 17, 17, 17)]
        [TestCase("", "EmptyMethod_LotsOfLines", 20, 23, 23, 23)]
        [TestCase("", "SimpleMethod_Void_NoArgs", 26, 27, 29, 29)]
        [TestCase("", "SimpleMethod_Void_OneArg", 32, 33, 35, 35)]
        [TestCase("", "SimpleMethod_Void_TwoArgs", 38, 39, 41, 41)]
        [TestCase("", "SimpleMethod_ReturnsInt_NoArgs", 44, 45, 46, 47)]
        [TestCase("", "SimpleMethod_ReturnsString_OneArg", 50, 51, 51, 52)]
        // Generic method uses simple name
        [TestCase("", "GenericMethod_ReturnsString_OneArg", 55, 56, 56, 57)]
        [TestCase("", "AsyncMethod_Void", 60, 61, 64, 64)]
        [TestCase("", "AsyncMethod_Task", 67, 68, 71, 71)]
        [TestCase("", "AsyncMethod_ReturnsInt", 74, 75, 78, 78)]
        [TestCase("+NestedClass", "SimpleMethod_Void_NoArgs", 83, 84, 86, 86)]
        [TestCase("+ParameterizedFixture", "SimpleMethod_ReturnsString_OneArg", 101, 102, 102, 103)]
        // Generic Fixture requires ` plus type arg count
        [TestCase("+GenericFixture`2", "Matches", 116, 117, 117, 118)]
        [TestCase("+GenericFixture`2+DoublyNested", "WriteBoth", 132, 133, 134, 134)]
        [TestCase("+GenericFixture`2+DoublyNested`1", "WriteAllThree", 151, 152, 153, 153)]
        public void VerifyNavigationData(string suffix, string methodName, int minLineDebug, int minLineRelease, int maxLineRelease, int maxLineDebug)
        {
            // We put this here because the Test Window will not
            // display a failure occuring in the fixture setup method
            Assert.NotNull(navData, "NavigationData could not be created");

            // Get the navigation data - ensure names are spelled correctly!
            var className = prefix + suffix;
            var data = navData.For(className, methodName);
            Assert.NotNull(data, "Unable to retrieve navigation data");

            // Verify the navigation data
            // Note that different line numbers are returned for our
            // debug and release builds, as follows:
            //
            // DEBUG
            //   Min line: Opening curly brace.
            //   Max line: Closing curly brace.
            //
            // RELEASE
            //   Min line: First non-comment code line or closing
            //             curly brace if the method is empty.
            //   Max line: Last code line if that line is an 
            //             unconditional return statement, otherwise
            //             the closing curly brace.
            Assert.That(data.FileName, Is.StringEnding("NavigationTestData.cs"));
#if DEBUG
            Assert.That(data.MinLineNumber, Is.EqualTo(minLineDebug));
            Assert.That(data.MaxLineNumber, Is.EqualTo(maxLineDebug));
#else
            Assert.That(data.MaxLineNumber, Is.EqualTo(maxLineRelease));
            Assert.That(data.MinLineNumber, Is.EqualTo(minLineRelease));
#endif
        }
    }
}
