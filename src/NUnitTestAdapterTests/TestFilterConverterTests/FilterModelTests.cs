#nullable enable

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

public class FilterModelTests
{
    [Test]
    public void FilterWritesRootElement()
    {
        var expression = new TestFilterExpression(
            [new TestFilterTerm([Condition("FullyQualifiedName", TestFilterOperations.Equal, "Ns.Fixture.Test")])]);

        Assert.That(
            new TestFilter(expression).ToXml(),
            Is.EqualTo("<filter><test>Ns.Fixture.Test</test></filter>"));
    }

    [Test]
    public void ExpressionWritesSingleTermWithoutOrWrapper()
    {
        var expression = new TestFilterExpression(
            [new TestFilterTerm([Condition("Name", TestFilterOperations.Equal, "Test")])]);

        Assert.That(expression.ToXml(), Is.EqualTo("<name>Test</name>"));
    }

    [Test]
    public void ExpressionWritesMultipleTermsWithOrWrapper()
    {
        var expression = new TestFilterExpression(
        [
            new TestFilterTerm([Condition("Name", TestFilterOperations.Equal, "First")]),
            new TestFilterTerm([Condition("Name", TestFilterOperations.Equal, "Second")]),
        ]);

        Assert.That(
            expression.ToXml(),
            Is.EqualTo("<or><name>First</name><name>Second</name></or>"));
    }

    [Test]
    public void TermWritesSingleFactorWithoutAndWrapper()
    {
        var term = new TestFilterTerm([Condition("Name", TestFilterOperations.Equal, "Test")]);

        Assert.That(term.ToXml(), Is.EqualTo("<name>Test</name>"));
    }

    [Test]
    public void TermWritesMultipleFactorsWithAndWrapper()
    {
        var term = new TestFilterTerm(
        [
            Condition("Name", TestFilterOperations.Equal, "Test"),
            Condition("Priority", TestFilterOperations.Equal, "High")
        ]);

        Assert.That(
            term.ToXml(),
            Is.EqualTo("<and><name>Test</name><prop name='Priority'>High</prop></and>"));
    }

    [Test]
    public void GroupWritesExpressionWithoutWrapper()
    {
        var expression = new TestFilterExpression(
        [
            new TestFilterTerm([Condition("Name", TestFilterOperations.Equal, "First")]),
            new TestFilterTerm([Condition("Name", TestFilterOperations.Equal, "Second")]),
        ]);

        Assert.That(
            new TestFilterGroup(expression).ToXml(),
            Is.EqualTo("<or><name>First</name><name>Second</name></or>"));
    }

    [TestCase("FullyQualifiedName", "<test>value</test>")]
    [TestCase("Name", "<name>value</name>")]
    [TestCase("TestCategory", "<cat>value</cat>")]
    [TestCase("Category", "<cat>value</cat>")]
    [TestCase("Priority", "<prop name='Priority'>value</prop>")]
    [TestCase("Property", "<prop name='Property'>value</prop>")]
    [TestCase("CustomProperty", "<prop name='CustomProperty'>value</prop>")]
    public void ConditionMapsPropertiesToNUnitElements(string property, string expected)
    {
        Assert.That(
            Condition(property, TestFilterOperations.Equal, "value").ToXml(),
            Is.EqualTo(expected));
    }

    [TestCase("=", false, false, "<prop name='Priority'>a.b(c)</prop>")]
    [TestCase("!=", true, false, "<not><prop name='Priority'>a.b(c)</prop></not>")]
    [TestCase("~", false, true, "<prop name='Priority' re='1'>a\\.b\\(c\\)</prop>")]
    [TestCase("!~", true, true, "<not><prop name='Priority' re='1'>a\\.b\\(c\\)</prop></not>")]
    public void ConditionWritesEachOperation(string symbol, bool negated, bool regex, string expected)
    {
        var operation = Operation(symbol, negated, regex);

        Assert.That(Condition("Priority", operation, "a.b(c)").ToXml(), Is.EqualTo(expected));
    }

    [Test]
    public void ConditionEscapesPropertyNamesAndValuesForXml()
    {
        var condition = Condition("Custom&'Property", TestFilterOperations.Equal, "\"<value>&'");

        Assert.That(
            condition.ToXml(),
            Is.EqualTo("<prop name='Custom&amp;&apos;Property'>&quot;&lt;value&gt;&amp;&apos;</prop>"));
    }

    [Test]
    public void XmlEscaperUnescapeReversesEscape()
    {
        const string value = "A & B < C > D \"quoted\" and 'apostrophe' &amp;";

        Assert.That(
            XmlEscaperHelper.Unescape(XmlEscaperHelper.Escape(value)),
            Is.EqualTo(value));
    }

    private static TestFilterCondition Condition(string property, ITestFilterOperation operation, string value)
        => new(property, operation, value);

    private static ITestFilterOperation Operation(string symbol, bool negated, bool regex)
        => new TestOperation(symbol, negated, regex);

    private sealed class TestOperation(string symbol, bool negated, bool regex) : ITestFilterOperation
    {
        public string Symbol { get; } = symbol;
        public bool IsNegated { get; } = negated;
        public bool IsRegex { get; } = regex;
    }
}
