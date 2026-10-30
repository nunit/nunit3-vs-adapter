#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents a property comparison in a filter.
/// </summary>
public interface ITestFilterCondition : ITestFilterFactor
{
    /// <summary>
    /// Gets the name of the property being compared.
    /// </summary>
    string Property { get; }

    /// <summary>
    /// Gets the comparison operation.
    /// </summary>
    ITestFilterOperation Operation { get; }

    /// <summary>
    /// Gets the comparison value.
    /// </summary>
    string Value { get; }
}
