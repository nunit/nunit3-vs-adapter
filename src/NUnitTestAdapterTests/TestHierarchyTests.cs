// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests;

/// <summary>
/// The Test Explorer tree comes from the TestCase.Hierarchy property when UseHierarchyProperty is on.
/// </summary>
public class TestHierarchyTests
{
    // The test from issue 1097: Test Explorer split this name at the dot inside the argument.
    private const string Issue1097Xml =
        @"<test-suite type='TestFixture' id='1' name='Tests' fullname='Issue1097.Tests' classname='Issue1097.Tests'>
            <test-case id='2' name='Test1097(&quot;I am a good test case (the best, even).&quot;)'
                fullname='Issue1097.Tests.Test1097(&quot;I am a good test case (the best, even).&quot;)'
                methodname='Test1097' classname='Issue1097.Tests' />
          </test-suite>";

    [Test]
    public void TreeFollowsNUnitAndNotTheDotsInTheName()
    {
        var testCase = Convert(Issue1097Xml, useHierarchy: true);

        Assert.That(
            testCase.GetPropertyValue(TestHierarchy.HierarchyProperty),
            Is.EqualTo(new[] { "NUnit.VisualStudio.TestAdapter.Tests", "Issue1097", "Tests", "Test1097" }));
    }

    // An inherited test: NUnit gives the test the base class name, in another namespace.
    private const string InheritedXml =
        @"<test-suite type='TestFixture' id='1' name='Derived' fullname='Ns.Derived' classname='Ns.Derived'>
            <test-case id='2' name='Inherited' fullname='Ns.Derived.Inherited' methodname='Inherited' classname='Other.Base' />
          </test-suite>";

    [Test]
    public void InheritedTestIsUnderItsFixture()
    {
        var testCase = Convert(InheritedXml, useHierarchy: true);

        Assert.That(
            testCase.GetPropertyValue(TestHierarchy.HierarchyProperty),
            Is.EqualTo(new[] { "NUnit.VisualStudio.TestAdapter.Tests", "Ns", "Derived", "Inherited" }));
    }

    [Test]
    public void InheritedTestIsUnderItsFixtureInLegacyDiscovery()
    {
        var doc = new XmlDocument();
        doc.LoadXml(InheritedXml);
        var settings = Substitute.For<IAdapterSettings>();
        settings.UseHierarchyProperty.Returns(true);
        using var converter = new TestConverterForXml(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath, settings);

        var testCase = converter.ConvertTestCase(new NUnitEventTestCase(doc.SelectSingleNode("//test-case")));

        Assert.That(
            testCase.GetPropertyValue(TestHierarchy.HierarchyProperty),
            Is.EqualTo(new[] { "NUnit.VisualStudio.TestAdapter.Tests", "Ns", "Derived", "Inherited" }));
    }

    // A real method in this assembly, so the managed names can be found by reflection.
    private const string ManagedXml =
        @"<test-suite type='TestFixture' id='1' name='TestHierarchyTests' fullname='NUnit.VisualStudio.TestAdapter.Tests.TestHierarchyTests' classname='NUnit.VisualStudio.TestAdapter.Tests.TestHierarchyTests'>
            <test-case id='2' name='Levels(&quot;a.b&quot;)' fullname='NUnit.VisualStudio.TestAdapter.Tests.TestHierarchyTests.Levels(&quot;a.b&quot;)'
                methodname='Levels' classname='NUnit.VisualStudio.TestAdapter.Tests.TestHierarchyTests' />
          </test-suite>";

    [Test]
    public void ManagedNamesDescribeTheMethodAsTheCompilerSeesIt()
    {
        var testCase = Convert(ManagedXml, useHierarchy: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(testCase.GetPropertyValue(ManagedNames.ManagedTypeProperty), Is.EqualTo("NUnit.VisualStudio.TestAdapter.Tests.TestHierarchyTests"));
            Assert.That(testCase.GetPropertyValue(ManagedNames.ManagedMethodProperty), Is.EqualTo("Levels(System.String,System.String,System.String,System.String)"));
        }
    }

    [Test]
    public void NoManagedNamesWhenTheSettingIsOff()
    {
        var testCase = Convert(ManagedXml, useHierarchy: false);

        Assert.That(testCase.GetPropertyValue(ManagedNames.ManagedMethodProperty), Is.Null);
    }

    [Test]
    public void NoHierarchyWhenTheSettingIsOff()
    {
        var testCase = Convert(Issue1097Xml, useHierarchy: false);

        Assert.That(testCase.GetPropertyValue(TestHierarchy.HierarchyProperty), Is.Null);
    }

    [TestCase("Ns.Fixture", "Fixture(1, \"a.b\")", "Ns", "Fixture(1, \"a.b\")", TestName = "ParameterizedFixtureKeepsItsArguments")]
    [TestCase("Ns.Sub.Outer+Inner", "Outer+Inner", "Ns.Sub", "Outer+Inner", TestName = "NestedClassIsOneLevel")]
    [TestCase("Ns.Generic`1[[System.Int32, System.Private.CoreLib]]", "Generic<Int32>", "Ns", "Generic<Int32>", TestName = "GenericArgumentsDoNotSplitTheNamespace")]
    [TestCase("NoNamespace", "NoNamespace", null, "NoNamespace", TestName = "ClassWithoutNamespace")]
    [TestCase("Ns.Fixture", null, "Ns", "Fixture", TestName = "ClassNameWhenNoFixtureIsFound")]
    public void Levels(string className, string fixtureName, string expectedNamespace, string expectedClass)
    {
        var hierarchy = TestHierarchy.Create(Path.Combine("bin", "My.Tests.dll"), className, fixtureName, "Method");

        Assert.That(hierarchy, Is.EqualTo(new[] { "My.Tests", expectedNamespace, expectedClass, "Method" }));
    }

    [Test]
    public void NoHierarchyWithoutAMethod()
    {
        Assert.That(TestHierarchy.Create(Path.Combine("bin", "My.Tests.dll"), "Ns.Fixture", "Fixture", null), Is.Null);
    }

    private static TestCase Convert(string xml, bool useHierarchy)
    {
        var root = XElement.Parse(xml);
        var parent = Substitute.For<INUnitDiscoveryCanHaveTestFixture>();
        parent.Parent.Returns(null as INUnitDiscoverySuiteBase);
        var fixture = DiscoveryConverter.ExtractTestFixture(parent, root, root.Attribute("classname").Value);
        var testNode = DiscoveryConverter.ExtractTestCases(fixture, root).Single();

        var settings = Substitute.For<IAdapterSettings>();
        settings.UseHierarchyProperty.Returns(useHierarchy);
        using var converter = new TestConverter(new TestLogger(new MessageLoggerStub()), FakeTestData.AssemblyPath, settings, Substitute.For<IDiscoveryConverter>());
        return converter.ConvertTestCase(testNode);
    }
}
