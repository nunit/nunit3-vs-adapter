namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Categories marking which release is expected to make a currently failing filter-parsing
/// test pass. See <c>docs/TestFilterParsing-6.x.md</c> and <c>docs/TestFilterParsing-v7.md</c>.
///
/// Mirrors the type of the same name in the unit test project; the two assemblies are
/// separate, so the constants are declared in each.
///
/// While working on 6.x, run
/// <c>--filter "Category=FilterParsing &amp; Category!=FixV7"</c>
/// and drive it to green. A test carrying neither category passes today and is a regression
/// guard.
/// </summary>
public static class FixIn
{
    /// <summary>Fixed by one of the non-breaking 6.x items.</summary>
    public const string SixX = "Fix6x";

    /// <summary>Needs the v7 grammar change; expected to stay red through 6.x.</summary>
    public const string V7 = "FixV7";
}
