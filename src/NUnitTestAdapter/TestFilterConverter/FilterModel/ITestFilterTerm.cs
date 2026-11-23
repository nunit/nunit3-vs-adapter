#nullable enable

using System.Collections.Generic;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents one or more factors joined by AND.
/// </summary>
public interface ITestFilterTerm : ITestFilterNode
{
    /// <summary>
    /// Gets the factors in this term.
    /// </summary>
    IReadOnlyList<ITestFilterFactor> Factors { get; }
}
