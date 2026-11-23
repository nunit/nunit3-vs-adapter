#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// Writes a test filter as NUnit filter XML.
/// </summary>
public sealed class NUnitFilterXmlWriter : ITestFilterVisitor<string>
{
    private const string RegexChars = @".[]{}()*+?|^$\";

    /// <summary>
    /// Gets a shared instance. The writer holds no state.
    /// </summary>
    public static NUnitFilterXmlWriter Instance { get; } = new();

    /// <inheritdoc/>
    public string VisitFilter(ITestFilter filter)
    {
        return $"<filter>{filter.Expression.Accept(this)}</filter>";
    }

    /// <inheritdoc/>
    public string VisitExpression(ITestFilterExpression expression)
    {
        return Join("or", expression.Terms);
    }

    /// <inheritdoc/>
    public string VisitTerm(ITestFilterTerm term)
    {
        return Join("and", term.Factors);
    }

    /// <inheritdoc/>
    public string VisitGroup(ITestFilterGroup group)
    {
        return group.Expression.Accept(this);
    }

    /// <inheritdoc/>
    public string VisitNegation(ITestFilterNegation negation)
    {
        return $"<not>{negation.Operand.Accept(this)}</not>";
    }

    /// <inheritdoc/>
    public string VisitCondition(ITestFilterCondition condition)
    {
        string property = condition.Property;
        string value = condition.Value;
        string elementName = property switch
        {
            "FullyQualifiedName" => "test",
            "Name" => "name",
            "Category" or "TestCategory" => "cat",
            _ => "prop"
        };

        // NUnit XML has no contains element, so a contains comparison is written as an escaped regex.
        bool isContains = condition.Comparison switch
        {
            TestFilterComparison.Exact => false,
            TestFilterComparison.Contains => true,
            _ => throw new InvalidOperationException($"Unknown comparison '{condition.Comparison}'."),
        };

        if ((elementName is "test" or "name") && TestNameFilter.NeedsRegex(value))
        {
            string regex = TestNameFilter.ToRegex(value);
            if (!isContains)
            {
                regex = TestNameFilter.Exact(regex);
            }

            return $"<{elementName} re='1'>{EscapeXml(regex)}</{elementName}>";
        }

        string escapedValue = isContains
            ? EscapeRegexChars(value)
            : value;
        escapedValue = EscapeXml(escapedValue);

        string regexAttribute = isContains ? " re='1'" : string.Empty;
        return elementName == "prop"
            ? $"<prop name='{EscapeXml(property)}'{regexAttribute}>{escapedValue}</prop>"
            : $"<{elementName}{regexAttribute}>{escapedValue}</{elementName}>";
    }

    private string Join(string elementName, IReadOnlyList<ITestFilterNode> nodes)
    {
        if (nodes.Count is 1)
        {
            return nodes[0].Accept(this);
        }

        var xml = new StringBuilder("<").Append(elementName).Append('>');
        foreach (var node in nodes)
        {
            xml.Append(node.Accept(this));
        }

        return xml.Append("</").Append(elementName).Append('>').ToString();
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;");
    }

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
