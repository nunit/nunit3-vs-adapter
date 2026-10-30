using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// Defines a parser that converts test filter expression strings into <see cref="ITestFilter"/> instances.
/// </summary>
public interface ITestFilterParser
{
    /// <summary>
    /// Parses a filter expression string into an <see cref="ITestFilter"/> instance.
    /// </summary>
    /// <param name="filter">The filter expression to parse.</param>
    /// <returns>An <see cref="ITestFilter"/> representing the parsed filter expression.</returns>
    ITestFilter Parse(string filter);
}