#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Represents a negated filter factor.
/// </summary>
public interface ITestFilterNegation : ITestFilterFactor
{
    /// <summary>
    /// Gets the factor being negated.
    /// </summary>
    ITestFilterFactor Operand { get; }
}
