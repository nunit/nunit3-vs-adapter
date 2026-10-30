#nullable enable

using System;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilterGroup"/>
public sealed class TestFilterGroup : ITestFilterGroup
{
    public TestFilterGroup(ITestFilterExpression expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <inheritdoc/>
    public ITestFilterExpression Expression { get; }

    /// <inheritdoc/>
    public string ToXml()
    {
        return Expression.ToXml();
    }
}
