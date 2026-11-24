#nullable enable

using System;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilter"/>
public sealed class TestFilter : ITestFilter
{
    public TestFilter(ITestFilterExpression expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <inheritdoc/>
    public ITestFilterExpression Expression { get; }

    /// <inheritdoc/>
    public T Accept<T>(ITestFilterVisitor<T> visitor)
    {
        return visitor.VisitFilter(this);
    }
}
