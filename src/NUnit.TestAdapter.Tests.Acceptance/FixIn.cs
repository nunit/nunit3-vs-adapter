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
}
