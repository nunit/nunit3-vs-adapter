#nullable enable

using System;

using NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// Maps the comparison operators of the VSTest and legacy filter syntaxes to the filter model.
/// </summary>
internal static class VSTestFilterOperators
{
    /// <summary>
    /// Creates the filter factor for a condition written with one of the operators <c>=</c>, <c>!=</c>, <c>~</c> or <c>!~</c>.
    /// </summary>
    /// <returns>
    /// A <see cref="TestFilterCondition"/>, wrapped in a <see cref="TestFilterNegation"/> for <c>!=</c> and <c>!~</c>.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="symbol"/> is not a supported operator.</exception>
    public static ITestFilterFactor CreateCondition(string property, string symbol, string value)
    {
        ITestFilterFactor condition = symbol switch
        {
            "=" or "!=" => new TestFilterCondition(property, TestFilterComparison.Exact, value),
            "~" or "!~" => new TestFilterCondition(property, TestFilterComparison.Contains, value),
            _ => throw new ArgumentException($"Invalid operator '{symbol}'.", nameof(symbol)),
        };

        return symbol[0] == '!' ? new TestFilterNegation(condition) : condition;
    }
}
