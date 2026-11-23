#nullable enable

using System;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilterNegation"/>
public sealed class TestFilterNegation : ITestFilterNegation
{
    public TestFilterNegation(ITestFilterExpression expression)
    {
        Expression = expression ?? throw new ArgumentNullException(nameof(expression));
    }

    /// <inheritdoc/>
    public ITestFilterExpression Expression { get; }

    /// <inheritdoc/>
    public string ToXml()
    {
        return $"<not>{Expression.ToXml()}</not>";
    }
}
