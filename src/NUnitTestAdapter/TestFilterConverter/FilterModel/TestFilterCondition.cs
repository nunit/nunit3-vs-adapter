#nullable enable

using System;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilterCondition"/>
public sealed class TestFilterCondition : ITestFilterCondition
{
    public TestFilterCondition(string property, TestFilterComparison comparison, string value)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Comparison = Enum.IsDefined(typeof(TestFilterComparison), comparison)
            ? comparison
            : throw new ArgumentOutOfRangeException(nameof(comparison), comparison, "Unknown comparison.");
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <inheritdoc/>
    public string Property { get; }

    /// <inheritdoc/>
    public TestFilterComparison Comparison { get; }

    /// <inheritdoc/>
    public string Value { get; }

    /// <inheritdoc/>
    public string ToXml()
    {
        string elementName = Property switch
        {
            "FullyQualifiedName" => "test",
            "Name" => "name",
            "Category" or "TestCategory" => "cat",
            _ => "prop"
        };

        // NUnit XML has no contains element, so a contains comparison is written as an escaped regex.
        bool isContains = Comparison switch
        {
            TestFilterComparison.Exact => false,
            TestFilterComparison.Contains => true,
            _ => throw new InvalidOperationException($"Unknown comparison '{Comparison}'."),
        };

        if ((elementName is "test" or "name") && TestNameFilter.NeedsRegex(Value))
        {
            string regex = TestNameFilter.ToRegex(Value);
            if (!isContains)
            {
                regex = TestNameFilter.Exact(regex);
            }

            return $"<{elementName} re='1'>{XmlEscaperHelper.Escape(regex)}</{elementName}>";
        }

        string escapedValue = isContains
            ? EscapeRegexChars(Value)
            : Value;
        escapedValue = XmlEscaperHelper.Escape(escapedValue);

        string regexAttribute = isContains ? " re='1'" : string.Empty;
        return elementName == "prop"
            ? $"<prop name='{XmlEscaperHelper.Escape(Property)}'{regexAttribute}>{escapedValue}</prop>"
            : $"<{elementName}{regexAttribute}>{escapedValue}</{elementName}>";
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
