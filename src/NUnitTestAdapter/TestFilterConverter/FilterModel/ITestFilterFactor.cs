#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents a grouped expression, a negation, or a filter condition.
/// </summary>
public interface ITestFilterFactor
{
    /// <summary>
    /// Returns this factor as NUnit filter XML.
    /// </summary>
    string ToXml();
}
