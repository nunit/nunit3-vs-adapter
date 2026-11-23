#nullable enable

using System;

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
    public T Accept<T>(ITestFilterVisitor<T> visitor)
    {
        return visitor.VisitCondition(this);
    }
}
