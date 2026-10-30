#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents a grouped expression.
/// </summary>
public interface ITestFilterGroup : ITestFilterFactor
{
    /// <summary>
    /// Gets the test filter expression.
    /// </summary>
    ITestFilterExpression Expression { get; }
}
