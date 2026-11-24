#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents a complete test filter.
/// </summary>
public interface ITestFilter : ITestFilterNode
{
    /// <summary>
    /// Gets the filter expression contained by this filter.
    /// </summary>
    ITestFilterExpression Expression { get; }
}
