namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Categories and ignore reasons for the filter-parsing tests. See
/// <c>docs/TestFilterParsing-6.x.md</c> and <c>docs/TestFilterParsing-v7.md</c>.
///
/// Mirrors the type of the same name in the unit test project; the two assemblies are separate,
/// so the constants are declared in each.
/// </summary>
public static class FixIn
{
    /// <summary>Fixed by one of the 6.x items.</summary>
    public const string SixX = "Fix6x";

    /// <summary>Needs the v7 grammar change.</summary>
    public const string V7 = "FixV7";

    /// <summary>
    /// Reason for ignoring a test that requires the v7 grammar change.
    /// </summary>
    public const string V7Reason =
        "Requires the escape-aware grammar in docs/TestFilterParsing-v7.md, which is a breaking " +
        "change scheduled for v7. Remove this Ignore as part of that change.";

    /// <summary>
    /// Reason for ignoring the unbalanced-parenthesis tests, 6.x item 1 / issue 1501.
    ///
    /// These are 6.x tests, not v7 ones, and they are ignored only because the defect they cover
    /// is destructive rather than merely failing.
    /// </summary>
    public const string Item1Reason =
        "Ignored only because the defect is destructive before it is fixed: the lexer allocates " +
        "until it runs out of memory rather than failing. Bound " +
        "CollectBalancedParentheticalExpression at end of input (6.x item 1, issue 1501) and " +
        "remove this Ignore in the same change.";
}
