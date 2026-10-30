#nullable enable

using System;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilterCondition"/>
public sealed class TestFilterCondition : ITestFilterCondition
{
    public TestFilterCondition(string property, ITestFilterOperation operation, string value)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Operation = operation ?? throw new ArgumentNullException(nameof(operation));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public string Property { get; }

    /// <inheritdoc/>
    public ITestFilterOperation Operation { get; }

    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public string ToXml()
    {
        string elementName = Property switch
        {
            "FullyQualifiedName" => "test",
            "TestCategory" => "cat",
            "Name" => "name",
            _ => "prop"
        };
        string escapedValue = Operation.IsRegex
            ? EscapeRegexChars(Value)
            : Value;
        escapedValue = XmlEscaperHelper.Escape(escapedValue);

        string regexAttribute = Operation.IsRegex ? " re='1'" : string.Empty;
        string element = elementName == "prop"
            ? $"<prop name='{XmlEscaperHelper.Escape(Property)}'{regexAttribute}>{escapedValue}</prop>"
            : $"<{elementName}{regexAttribute}>{escapedValue}</{elementName}>";

        return Operation.IsNegated ? $"<not>{element}</not>" : element;
    }

    private const string RegexChars = @".[]{}()*+?|^$\";

    private static string EscapeRegexChars(string value)
    {
        var escaped = new StringBuilder();
        foreach (char character in value)
        {
            if (RegexChars.IndexOf(character) >= 0)
            {
                escaped.Append('\\');
            }

            escaped.Append(character);
        }
        return escaped.ToString();
    }
}
