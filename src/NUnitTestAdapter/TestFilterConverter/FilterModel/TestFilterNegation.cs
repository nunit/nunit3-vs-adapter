#nullable enable

using System;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <inheritdoc cref="ITestFilterNegation"/>
public sealed class TestFilterNegation : ITestFilterNegation
{
    public TestFilterNegation(ITestFilterFactor operand)
    {
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
    }

    /// <inheritdoc/>
    public ITestFilterFactor Operand { get; }

    /// <inheritdoc/>
    public T Accept<T>(ITestFilterVisitor<T> visitor)
    {
        return visitor.VisitNegation(this);
    }
}
