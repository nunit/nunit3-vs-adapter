#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Describes how a condition compares a property with its value.
/// </summary>
public enum TestFilterComparison
{
    /// <summary>
    /// The property equals the value. Written as <c>=</c> in a filter.
    /// </summary>
    Exact,

    /// <summary>
    /// The property contains the value. Written as <c>~</c> in a filter.
    /// </summary>
    Contains,
}
