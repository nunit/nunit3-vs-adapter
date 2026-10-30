#nullable enable

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;
using NUnit.VisualStudio.TestAdapter.Tests.Filtering;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

[Category("FilterParsing")]
public class TestFilterParserConformanceTests
{
    private const string Source = "parser-conformance.dll";
    private static readonly Uri ExecutorUri = new("executor://nunit3testexecutor/");

    private static readonly (string Filter, string Xml)[] SharedCases =
    [
        ("Method", "<filter><test re='1'>Method</test></filter>"),
        (" FullyQualifiedName = Ns.Fixture.Method ", "<filter><test>Ns.Fixture.Method</test></filter>"),
        ("Name=Blah \\(\"b\"\\)", "<filter><name>Blah (&quot;b&quot;)</name></filter>"),
        ("Name=Blah\\(\"a\"", "<filter><name>Blah(&quot;a&quot;</name></filter>"),
        ("Priority=High", "<filter><prop name='Priority'>High</prop></filter>"),
        ("TestCategory=Urgent", "<filter><cat>Urgent</cat></filter>"),
        ("Property=Value", "<filter><prop name='Property'>Value</prop></filter>"),
        ("CustomProperty=Value", "<filter><prop name='CustomProperty'>Value</prop></filter>"),
        ("Name=First|Name=Second&Priority=High",
            "<filter><or><name>First</name><and><name>Second</name><prop name='Priority'>High</prop></and></or></filter>"),
        ("(Name=First|Name=Second)&Priority=High",
            "<filter><and><or><name>First</name><name>Second</name></or><prop name='Priority'>High</prop></and></filter>"),
        (@"Name=Value\&More", "<filter><name>Value&amp;More</name></filter>"),
        (@"Name=Value\|More", "<filter><name>Value|More</name></filter>"),
        (@"Name=Value\=More", "<filter><name>Value=More</name></filter>"),
        (@"Name=Value\\More", "<filter><name>Value\\More</name></filter>"),
        ("Name=Value with spaces", "<filter><name>Value with spaces</name></filter>"),
        (@"Priority~a.b\(c\)", "<filter><prop name='Priority' re='1'>a\\.b\\(c\\)</prop></filter>"),
        (@"Priority!~a.b\(c\)", "<filter><not><prop name='Priority' re='1'>a\\.b\\(c\\)</prop></not></filter>")
    ];

    [TestCaseSource(nameof(SharedCases))]
    public void SharedCasesProduceSameExpectedXml((string Filter, string Xml) testCase)
    {
        string oldXml = new TestFilterParser().Parse(testCase.Filter);
        string newXml = new VSTestFilterParser().Parse(testCase.Filter).ToXml();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(oldXml, Is.EqualTo(testCase.Xml), "Legacy parser output");
            Assert.That(newXml, Is.EqualTo(testCase.Xml), "VSTest-conformant parser output");
        }
    }

    [TestCase(
        "Category=Urgent",
        "<filter><prop name='Category'>Urgent</prop></filter>",
        "<filter><cat>Urgent</cat></filter>")]
    public void CategoryMappingIsAnExplicitParserDifference(string filter, string oldXml, string newXml)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new TestFilterParser().Parse(filter), Is.EqualTo(oldXml));
            Assert.That(new VSTestFilterParser().Parse(filter).ToXml(), Is.EqualTo(newXml));
        }
    }

    [TestCase("")]
    [TestCase("(")]
    [TestCase("()")]
    [TestCase("(Name=Test")]
    [TestCase("Name=Test)")]
    [TestCase("Name=First&")]
    [TestCase("|Name=First")]
    [TestCase("Name=|Name=First")]
    [TestCase("=Value")]
    [TestCase("Name=")]
    [TestCase("Name=First (1)")]
    [TestCase(@"Name=Value\q")]
    [TestCase("Name=Value\\")]
    [TestCase("!(Name=Test)")]
    public void NewParserRejectsMalformedOrUnsupportedSyntax(string filter)
    {
        var exception = Assert.Throws<TestFilterParserException>(() => new VSTestFilterParser().Parse(filter));

        Assert.That(exception.Message, Does.Contain("position"));
    }

    [Test]
    public void NewParserRejectsNullInputWithPosition()
    {
        var exception = Assert.Throws<TestFilterParserException>(() => new VSTestFilterParser().Parse(null!));

        Assert.That(exception.Message, Does.Contain("position 0"));
    }

    [TestCase("Name=First&")]
    [TestCase("(Name=First")]
    [TestCase("Name=Value\\q")]
    [TestCase("Name=First (1)")]
    public void VSTestRejectedFiltersAreAlsoRejectedByNewParser(string filter)
    {
        Assert.That(
            new FilterExpressionWrapper(filter).ParseError,
            Is.Not.Empty,
            "The reference VSTest parser should reject this case.");
        Assert.That(
            () => new VSTestFilterParser().Parse(filter),
            Throws.TypeOf<TestFilterParserException>());
    }

    [TestCase("Name=First", "First", true)]
    [TestCase("Name=First", "Second", false)]
    [TestCase("FullyQualifiedName=Ns.Fixture.Method", "Ns.Fixture.Method", true)]
    [TestCase(@"FullyQualifiedName=Ns.Fixture.Method\(1\)", "Ns.Fixture.Method(1)", true)]
    [TestCase(@"Name=Value\&More", "Value&More", true)]
    public void EscapedFiltersMatchTheSameVSTestCase(string filter, string name, bool expectedMatch)
    {
        var testCase = new TestCase(name, ExecutorUri, Source);
        var vstestFilter = FilteringTestUtils.CreateVSTestFilterExpression(filter);

        Assert.That(
            vstestFilter.MatchTestCase(testCase, property => GetProperty(testCase, property)),
            Is.EqualTo(expectedMatch));
    }

    [TestCaseSource(typeof(FilterRoundTripConformanceTests), nameof(FilterRoundTripConformanceTests.FullNames))]
    public void NewParserXmlSelectsExactVSTestFullName(string fullName)
    {
        string filter = "FullyQualifiedName="
            + Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities.FilterHelper.Escape(fullName);
        var testCase = new TestCase(fullName, ExecutorUri, Source);
        var vstestExpression = FilteringTestUtils.CreateVSTestFilterExpression(filter);
        var vstestSelected = FilteringTestUtils.CreateTestFilter(vstestExpression).CheckFilter([testCase]);
        string xml = new VSTestFilterParser().Parse(filter).ToXml();

        Assert.That(
            vstestSelected.Select(test => test.FullyQualifiedName),
            Is.EqualTo([fullName]),
            "VSTest must accept and select the case used as the reference.");

        var testElement = XElement.Parse(xml).Element("test");
        Assert.That(testElement, Is.Not.Null, xml);
        bool selected = testElement!.Attribute("re")?.Value == "1"
            ? Regex.IsMatch(fullName, testElement.Value)
            : testElement.Value == fullName;

        Assert.That(selected, Is.True, $"Filter XML '{xml}' should select '{fullName}'.");
    }

    [TestCase("Name=First", "First", true)]
    [TestCase("Name=First", "Second", false)]
    [TestCase("FullyQualifiedName=Ns.Fixture.Method", "Ns.Fixture.Method", true)]
    public void LegacyParserXmlSelectsSimpleNames(string filter, string name, bool expectedSelected)
    {
        string xml = new TestFilterParser().Parse(filter);
        var element = XElement.Parse(xml).Element("name") ?? XElement.Parse(xml).Element("test");
        Assert.That(element, Is.Not.Null, xml);
        bool selected = element!.Attribute("re")?.Value == "1"
            ? Regex.IsMatch(name, element.Value)
            : element.Value == name;

        Assert.That(selected, Is.EqualTo(expectedSelected), xml);
    }

    private static object GetProperty(TestCase testCase, string propertyName) => propertyName switch
    {
        "FullyQualifiedName" => testCase.FullyQualifiedName,
        "Name" => testCase.DisplayName,
        _ => testCase.GetPropertyValue(TestProperty.Find(propertyName)!) ?? string.Empty
    };
}
