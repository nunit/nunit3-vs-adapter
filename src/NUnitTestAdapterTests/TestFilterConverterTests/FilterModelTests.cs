#nullable enable

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

public class FilterModelTests
{
    [Test]
    public void FilterWritesRootElement()
    {
        var expression = new TestFilterExpression(
            [new TestFilterTerm([Condition("FullyQualifiedName", TestFilterComparison.Exact, "Ns.Fixture.Test")])]);

        Assert.That(
            new TestFilter(expression).ToNUnitXml(),
            Is.EqualTo("<filter><test>Ns.Fixture.Test</test></filter>"));
    }

    [Test]
    public void ExpressionWritesSingleTermWithoutOrWrapper()
    {
        var expression = new TestFilterExpression(
            [new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "Test")])]);

        Assert.That(expression.ToNUnitXml(), Is.EqualTo("<name>Test</name>"));
    }

    [Test]
    public void ExpressionWritesMultipleTermsWithOrWrapper()
    {
        var expression = new TestFilterExpression(
        [
            new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "First")]),
            new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "Second")]),
        ]);

        Assert.That(
            expression.ToNUnitXml(),
            Is.EqualTo("<or><name>First</name><name>Second</name></or>"));
    }

    [Test]
    public void TermWritesSingleFactorWithoutAndWrapper()
    {
        var term = new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "Test")]);

        Assert.That(term.ToNUnitXml(), Is.EqualTo("<name>Test</name>"));
    }

    [Test]
    public void TermWritesMultipleFactorsWithAndWrapper()
    {
        var term = new TestFilterTerm(
        [
            Condition("Name", TestFilterComparison.Exact, "Test"),
            Condition("Priority", TestFilterComparison.Exact, "High")
        ]);

        Assert.That(
            term.ToNUnitXml(),
            Is.EqualTo("<and><name>Test</name><prop name='Priority'>High</prop></and>"));
    }

    [Test]
    public void GroupWritesExpressionWithoutWrapper()
    {
        var expression = new TestFilterExpression(
        [
            new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "First")]),
            new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "Second")]),
        ]);

        Assert.That(
            new TestFilterGroup(expression).ToNUnitXml(),
            Is.EqualTo("<or><name>First</name><name>Second</name></or>"));
    }

    [Test]
    public void NegationWritesOperandInsideNotElement()
    {
        var expression = new TestFilterExpression(
        [
            new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "First")]),
            new TestFilterTerm([Condition("Name", TestFilterComparison.Exact, "Second")]),
        ]);

        Assert.That(
            new TestFilterNegation(new TestFilterGroup(expression)).ToNUnitXml(),
            Is.EqualTo("<not><or><name>First</name><name>Second</name></or></not>"));
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
            Condition(property, TestFilterComparison.Exact, "value").ToNUnitXml(),
            Is.EqualTo(expected));
    }

    [TestCase("=", "<prop name='Priority'>a.b(c)</prop>")]
    [TestCase("!=", "<not><prop name='Priority'>a.b(c)</prop></not>")]
    [TestCase("~", "<prop name='Priority' re='1'>a\\.b\\(c\\)</prop>")]
    [TestCase("!~", "<not><prop name='Priority' re='1'>a\\.b\\(c\\)</prop></not>")]
    public void CreateConditionWritesEachOperator(string symbol, string expected)
    {
        Assert.That(
            VSTestFilterOperators.CreateCondition("Priority", symbol, "a.b(c)").ToNUnitXml(),
            Is.EqualTo(expected));
    }

    [Test]
    public void CreateConditionRejectsUnknownOperator()
    {
        Assert.That(
            () => VSTestFilterOperators.CreateCondition("Priority", "!", "value"),
            Throws.ArgumentException);
    }

    [Test]
    public void ConditionRejectsUndefinedComparison()
    {
        Assert.That(
            () => Condition("Name", (TestFilterComparison)99, "value"),
            Throws.InstanceOf<System.ArgumentOutOfRangeException>());
    }

    [Test]
    public void XmlWriterHandlesEveryComparison([Values] TestFilterComparison comparison)
    {
        Assert.That(() => Condition("Priority", comparison, "value").ToNUnitXml(), Throws.Nothing);
    }

    [Test]
    public void ConditionEscapesPropertyNamesAndValuesForXml()
    {
        var condition = Condition("Custom&'Property", TestFilterComparison.Exact, "\"<value>&'");

        Assert.That(
            condition.ToNUnitXml(),
            Is.EqualTo("<prop name='Custom&amp;&apos;Property'>&quot;&lt;value&gt;&amp;&apos;</prop>"));
    }

    private static TestFilterCondition Condition(string property, TestFilterComparison comparison, string value)
        => new(property, comparison, value);
}
