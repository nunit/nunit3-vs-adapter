#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Provides predefined test filter operations for equality and pattern-based matching.
/// </summary>
/// <remarks>
/// Exposes reusable operation instances that represent equal, not equal, contains, and not contains
/// semantics, including whether an operation is negated and whether it uses regular-expression matching.
/// </remarks>
public static class TestFilterOperations
{
    /// <summary>
    /// Represents the equality filter operation.
    /// </summary>
    public static ITestFilterOperation Equal { get; } = new FilterOperation("=", isNegated: false, isRegex: false);

    /// <summary>
    /// Represents the inequality filter operation.
    /// </summary>
    public static ITestFilterOperation NotEqual { get; } = new FilterOperation("!=", isNegated: true, isRegex: false);

    /// <summary>
    /// Represents the contains filter operation.
    /// </summary>
    public static ITestFilterOperation Contains { get; } = new FilterOperation("~", isNegated: false, isRegex: true);

    /// <summary>
    /// Represents the not contains filter operation.
    /// </summary>
    public static ITestFilterOperation NotContains { get; } = new FilterOperation("!~", isNegated: true, isRegex: true);

    private sealed class FilterOperation : ITestFilterOperation
    {
        public FilterOperation(string symbol, bool isNegated, bool isRegex)
        {
            Symbol = symbol;
            IsNegated = isNegated;
            IsRegex = isRegex;
        }

        public string Symbol { get; }

        public bool IsNegated { get; }

        public bool IsRegex { get; }
    }
}
