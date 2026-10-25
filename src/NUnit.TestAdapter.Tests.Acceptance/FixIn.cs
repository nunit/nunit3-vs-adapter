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
}
