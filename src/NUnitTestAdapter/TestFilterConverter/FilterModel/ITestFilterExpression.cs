#nullable enable

using System.Collections.Generic;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents one or more terms joined by OR.
/// </summary>
public interface ITestFilterExpression : ITestFilterNode
{
    /// <summary>
    /// Gets the terms in this expression.
    /// </summary>
    IReadOnlyList<ITestFilterTerm> Terms { get; }
}
