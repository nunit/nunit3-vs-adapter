#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilterTerm"/>
public sealed class TestFilterTerm : ITestFilterTerm
{
    public TestFilterTerm(IEnumerable<ITestFilterFactor> factors)
    {
        Factors = CopyNonEmpty(factors, nameof(factors));
    }

    /// <inheritdoc/>
    public IReadOnlyList<ITestFilterFactor> Factors { get; }

    /// <inheritdoc/>
    public string ToXml()
    {
        if (Factors.Count is 1)
        {
            return Factors[0].ToXml();
        }

        var xml = new StringBuilder("<and>");
        foreach (var factor in Factors)
        {
            xml.Append(factor.ToXml());
        }

        return xml.Append("</and>").ToString();
    }

    private static IReadOnlyList<ITestFilterFactor> CopyNonEmpty(IEnumerable<ITestFilterFactor> source, string parameterName)
    {
        if (source is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var items = new List<ITestFilterFactor>();
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
            throw new ArgumentException("A filter term must contain at least one factor.", parameterName);
        }

        return [.. items];
    }
}
