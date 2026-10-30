#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents a complete test filter.
/// </summary>
public interface ITestFilter
{
    /// <summary>
    /// Gets the filter expression contained by this filter.
    /// </summary>
    ITestFilterExpression Expression { get; }

    /// <summary>
    /// Returns this filter as NUnit filter XML.
    /// </summary>
    string ToXml();
}
