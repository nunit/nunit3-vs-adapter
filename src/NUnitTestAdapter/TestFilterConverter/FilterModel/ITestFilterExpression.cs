#nullable enable

using System.Collections.Generic;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents one or more terms joined by OR.
/// </summary>
public interface ITestFilterExpression
{
    /// <summary>
    /// Gets the terms in this expression.
    /// </summary>
    IReadOnlyList<ITestFilterTerm> Terms { get; }

    /// <summary>
    /// Returns this expression as NUnit filter XML.
    /// </summary>
    string ToXml();
}
