#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Describes a filter comparison operator.
/// </summary>
public interface ITestFilterOperation
{
    /// <summary>
    /// Gets the operator symbol.
    /// </summary>
    string Symbol { get; }

    /// <summary>
    /// Gets whether the operation negates the comparison.
    /// </summary>
    bool IsNegated { get; }

    /// <summary>
    /// Gets whether the operation performs a regex match.
    /// </summary>
    bool IsRegex { get; }
}
