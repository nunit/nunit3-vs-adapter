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

        [TestCase("", "EmptyMethod_OneLine", 9, 9)]
        [TestCase("", "EmptyMethod_TwoLines", 12, 13)]
        [TestCase("", "EmptyMethod_ThreeLines", 16, 17)]
        [TestCase("", "EmptyMethod_LotsOfLines", 20, 23)]
        [TestCase("", "SimpleMethod_Void_NoArgs", 26, 29)]
        [TestCase("", "SimpleMethod_Void_OneArg", 32, 35)]
        [TestCase("", "SimpleMethod_Void_TwoArgs", 38, 41)]
        [TestCase("", "SimpleMethod_ReturnsInt_NoArgs", 44, 47)]
        [TestCase("", "SimpleMethod_ReturnsString_OneArg", 50, 52)]
        // Generic method uses simple name
        [TestCase("", "GenericMethod_ReturnsString_OneArg", 55, 57)]
        // TODO: Async requires NUnit change to export MethodInfo
        [TestCase("", "AsyncMethod_Void", 60, 64, Explicit = true, Reason = "NYI")]
        [TestCase("", "AsyncMethod_Task", 67, 71, Explicit = true, Reason = "NYI")]
        [TestCase("", "AsyncMethod_ReturnsInt", 74, 78, Explicit = true, Reason = "NYI")]
        [TestCase("+NestedClass", "SimpleMethod_Void_NoArgs", 83, 86)]
        [TestCase("+ParameterizedFixture", "SimpleMethod_ReturnsString_OneArg", 101, 103)]
        // Generic Fixture requires ` plus type arg count
        [TestCase("+GenericFixture`2", "Matches", 116, 118)]
        [TestCase("+GenericFixture`2+DoublyNested", "WriteBoth", 132, 134)]
        [TestCase("+GenericFixture`2+DoublyNested`1", "WriteAllThree", 151, 153)]
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
