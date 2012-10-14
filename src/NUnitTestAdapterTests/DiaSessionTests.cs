using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    // These are not tests of the nunit test adapter,
    // but exploratory tests intended to verify
    // the proper call sequences to DiaSession in
    // order to get navigation data. In case of 
    // a new release of Visual Studio, it's possible
    // that some of these cases will fail, leading
    // to modifying our use of DiaSession.
    public class DiaSessionTests
    {
        DiaSession dia;
        const string prefix = "NUnit.VisualStudio.TestAdapter.Tests.DiaSessionTestData";

        [TestFixtureSetUp]
        public void InitializeDiaSession()
        {
            dia = new DiaSession(NUnit.Core.AssemblyHelper.GetAssemblyPath(Assembly.GetExecutingAssembly()));
        }

        [TestFixtureTearDown]
        public void DisposeDiaSession()
        {
            dia.Dispose();
        }

        [TestCase("", "EmptyMethod_OneLine", 8, 8)]
        [TestCase("", "EmptyMethod_TwoLines", 11, 12)]
        [TestCase("", "EmptyMethod_ThreeLines", 15, 16)]
        [TestCase("", "EmptyMethod_LotsOfLines", 19, 22)]
        [TestCase("", "SimpleMethod_Void_NoArgs", 25, 28)]
        [TestCase("", "SimpleMethod_Void_OneArg", 31, 34)]
        [TestCase("", "SimpleMethod_Void_TwoArgs", 37, 40)]
        [TestCase("", "SimpleMethod_ReturnsInt_NoArgs", 43, 46)]
        [TestCase("", "SimpleMethod_ReturnsString_OneArg", 49, 51)]
        // Generic method uses simple name
        [TestCase("", "GenericMethod_ReturnsString_OneArg", 54, 56)]
        [TestCase("+NestedClass", "SimpleMethod_Void_NoArgs", 61, 64)]
        [TestCase("+ParameterizedFixture", "SimpleMethod_ReturnsString_OneArg", 79, 81)]
        // Generic Fixture requires ` plus type arg count
        [TestCase("+GenericFixture`2", "Matches", 94, 96)]
        [TestCase("+GenericFixture`2+DoublyNested", "WriteBoth", 110, 112)]
        [TestCase("+GenericFixture`2+DoublyNested`1", "WriteAllThree", 129, 131)]
        public void VerifyNavigationData(string suffix, string methodName, int minLine, int maxLine)
        {
            // We put this here because the Test Window will not
            // display a failure occuring in the fixture setup method
            Assert.NotNull(dia, "DiaSession object could not be created");

            // Get the navigation data - ensure names are spelled correctly!
            var className = prefix + suffix;
            var data = dia.GetNavigationData(className, methodName);
            Assert.NotNull(data, "Unable to retrieve navigation data");

            // Verify the navigation data
            Assert.That(data.FileName, Is.StringEnding("DiaSessionTestData.cs"));
            Assert.That(data.MinLineNumber, Is.EqualTo(minLine));
            Assert.That(data.MaxLineNumber, Is.EqualTo(maxLine));
        }
    }
}
