#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents a negated filter expression.
/// </summary>
public interface ITestFilterNegation : ITestFilterFactor
{
    /// <summary>
    /// Gets the expression being negated.
    /// </summary>
    ITestFilterExpression Expression { get; }
}
