#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents any node in a test filter.
/// </summary>
public interface ITestFilterNode
{
    /// <summary>
    /// Calls the method of <paramref name="visitor"/> that handles this node type.
    /// </summary>
    T Accept<T>(ITestFilterVisitor<T> visitor);
}
