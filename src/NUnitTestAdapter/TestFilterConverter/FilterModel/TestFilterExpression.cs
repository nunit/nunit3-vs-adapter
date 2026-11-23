#nullable enable

using System;
using System.Collections.Generic;

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
    public T Accept<T>(ITestFilterVisitor<T> visitor)
    {
        return visitor.VisitExpression(this);
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
}
