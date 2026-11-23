#nullable enable

using System;

using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// Writes test filter nodes as NUnit filter XML.
/// </summary>
public static class NUnitFilterXmlExtensions
{
    /// <summary>
    /// Returns <paramref name="node"/> as NUnit filter XML.
    /// </summary>
    public static string ToNUnitXml(this ITestFilterNode node)
    {
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        return node.Accept(NUnitFilterXmlWriter.Instance);
    }
}
