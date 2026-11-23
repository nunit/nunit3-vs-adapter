#nullable enable

using System;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Provides predefined test filter operations for equality and pattern-based matching.
/// </summary>
/// <remarks>
/// Exposes reusable operation instances for the equal and contains comparisons. The negated
/// operators <c>!=</c> and <c>!~</c> are represented by wrapping a condition in a
/// <see cref="TestFilterNegation"/>; use <see cref="CreateCondition"/> to build either form from an operator symbol.
/// </remarks>
public static class TestFilterOperations
{
    /// <summary>
    /// Represents the equality filter operation.
    /// </summary>
    public static ITestFilterOperation Equal { get; } = new FilterOperation("=", isRegex: false);

    /// <summary>
    /// Represents the contains filter operation.
    /// </summary>
    public static ITestFilterOperation Contains { get; } = new FilterOperation("~", isRegex: true);

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
            "=" or "!=" => new TestFilterCondition(property, Equal, value),
            "~" or "!~" => new TestFilterCondition(property, Contains, value),
            _ => throw new ArgumentException($"Invalid operator '{symbol}'.", nameof(symbol)),
        };

        return symbol[0] == '!' ? new TestFilterNegation(condition) : condition;
    }

    private sealed class FilterOperation : ITestFilterOperation
    {
        public FilterOperation(string symbol, bool isRegex)
        {
            Symbol = symbol;
            IsRegex = isRegex;
        }

        public string Symbol { get; }

        public bool IsRegex { get; }
    }
}
