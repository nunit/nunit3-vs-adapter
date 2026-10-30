#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilterExpression"/>
public sealed class TestFilterExpression : ITestFilterExpression
{
    public TestFilterExpression(IEnumerable<ITestFilterTerm> terms)
    {
        Terms = CopyNonEmpty(terms, nameof(terms));
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITestFilterTerm> Terms { get; }

    /// <inheritdoc/>
    public string ToXml()
    {
        return Join("or", Terms);
    }

    private static IReadOnlyList<T> CopyNonEmpty<T>(IEnumerable<T> source, string parameterName)
    {
        if (source is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var items = new List<T>();
        foreach (var item in source)
        {
            if (item is null)
            {
                throw new ArgumentException("Filter parts cannot contain null.", parameterName);
            }

            items.Add(item);
        }

        if (items.Count is 0)
        {
            throw new ArgumentException("A filter expression must contain at least one term.", parameterName);
        }

        return [.. items];
    }

    private static string Join<T>(string elementName, IReadOnlyList<T> items)
        where T : class
    {
        if (items.Count is 1)
        {
            return ((dynamic)items[0]).ToXml();
        }

        var xml = new StringBuilder("<").Append(elementName).Append('>');
        foreach (var item in items)
        {
            xml.Append(((dynamic)item).ToXml());
        }

        return xml.Append("</").Append(elementName).Append('>').ToString();
    }
}
