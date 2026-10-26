using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// The tests from the Issue1502 and Issue1349 repros in nunit3-vs-adapter.issues, used by
/// <see cref="ReproTestNameFilterTests"/> (VSTest) and <see cref="MtpReproTestNameFilterTests"/> (MTP).
/// Test Explorer selected each of them correctly with adapter 6.3.1. The filters here are built
/// with the same VSTest escaping, so a failure means the command line filter path has changed.
/// </summary>
internal static class ReproTestNames
{
    public const int TotalTests = 21;

    public const string TestsCs = """"
        using NUnit.Framework;

        namespace Issue1502
        {
            public class FilterEscapingTests
            {
                [TestCase("a & b")]
                [TestCase("a &amp; b")]
                [TestCase("say \"hi\"")]
                [TestCase(@"C:\Temp")]
                [TestCase("a\nb")]
                [TestCase("x(y")]
                [TestCase("x)y")]
                [TestCase("a | b")]
                [TestCase("a = b")]
                [TestCase("<tag>")]
                public void StringArgument(string s) => Assert.Pass();

                [TestCase(1, TestName = "Name with & and &amp;")]
                public void CustomName(int i) => Assert.Pass();
            }
        }

        public sealed class UnitTest1
        {
            [TestCase("\"C:\\Path\\File.txt\"")]
            [TestCase("C:\\Path\\File.txt\"")]
            public void Test(string input) => Assert.Pass();
        }

        public sealed class ValuesTests
        {
            [Test]
            public void Test1([Values("|")] string arg) => Assert.Pass();

            [Test]
            public void Test2([Values("\\")] string arg) => Assert.Pass();
        }

        // NUnit writes these characters as \uXXXX text in its XML (nunit/nunit#4826)
        public sealed class ControlCharacterTests
        {
            [TestCase("a\u001bb")]
            [TestCase("a\u0001b")]
            [TestCase("a\uffffb")]
            public void Arg(string s) => Assert.Pass();

            [TestCase(1, TestName = "Named\u001bEsc")]
            public void Named(int i) => Assert.Pass();
        }

        // Fixture arguments in the name: issue 623 (Azure DevOps rerun) and issue 677 (dotnet test --filter)
        [TestFixtureSource(nameof(Browsers))]
        public sealed class Marketplace(string browser)
        {
            public static readonly string[] Browsers = ["Chrome"];

            [Test]
            public void Submit() => Assert.That(browser, Is.Not.Empty);
        }

        public sealed class Definition
        {
            public override string ToString() => "Windows Edgium";
        }

        [TestFixtureSource(nameof(Definitions))]
        public sealed class Broken(Definition definition)
        {
            public static readonly object[] Definitions = [new Definition()];

            [Test]
            public void Test1() => Assert.That(definition, Is.Not.Null);
        }
        """";

    public static TestCaseData[] Cases =>
    [
        Case("Issue1502.FilterEscapingTests", """StringArgument("a & b")""", "Ampersand"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("a &amp; b")""", "HtmlEntity"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("say \"hi\"")""", "EscapedQuote"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("C:\\Temp")""", "Backslash"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("a\nb")""", "BackslashN"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("x(y")""", "OpenParen"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("x)y")""", "CloseParen"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("a | b")""", "Pipe"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("a = b")""", "Equals"),
        Case("Issue1502.FilterEscapingTests", """StringArgument("<tag>")""", "AngleBrackets"),
        Case("UnitTest1", """Test("\"C:\\Path\\File.txt\"")""", "QuotedPath_Issue1349"),
        Case("UnitTest1", """Test("C:\\Path\\File.txt\"")""", "PathWithTrailingQuote_Issue1349"),
        Case("ValuesTests", """Test1("|")""", "ValuesPipe_Issue1349"),
        Case("ValuesTests", """Test2("\\")""", "ValuesBackslash_Issue1349"),
        Case("Issue1502.FilterEscapingTests", "Name with & and &amp;", "CustomNameWithSpaces_Issue876"),
        Case("ControlCharacterTests", """Arg("a\u001bb")""", "EscapeCharacter_Issue761"),
        Case("ControlCharacterTests", """Arg("a\u0001b")""", "C0Control_Issue761"),
        Case("ControlCharacterTests", """Arg("a\uffffb")""", "NonCharacter_Issue761"),
        Case("ControlCharacterTests", """Named\u001bEsc""", "CustomNameWithEscapeCharacter_Issue761"),
        // MTP --list-tests shows a test in a parameterized fixture with the fixture name first
        Case("""Marketplace("Chrome")""", "Submit", "FixtureArgument_Issue623", """Marketplace("Chrome").Submit"""),
        Case("Broken(Windows Edgium)", "Test1", "FixtureArgumentWithSpace_Issue677", "Broken(Windows Edgium).Test1"),

        // Not escaped: VSTest rejects these, and the adapter reads them itself (issue 1527)
        UnescapedCase("""Marketplace("Chrome")""", "Submit", "UnescapedFixtureArgument_Issue623", """Marketplace("Chrome").Submit"""),
        UnescapedCase("Broken(Windows Edgium)", "Test1", "UnescapedFixtureArgumentWithSpace_Issue677", "Broken(Windows Edgium).Test1"),
    ];

    private static TestCaseData Case(string className, string testName, string label, string listedTestName = null) =>
        new TestCaseData("FullyQualifiedName=" + FilterHelper.Escape($"{className}.{testName}"), testName, listedTestName ?? testName)
            .SetName($"{{m}}_{label}");

    private static TestCaseData UnescapedCase(string className, string testName, string label, string listedTestName) =>
        new TestCaseData($"FullyQualifiedName={className}.{testName}", testName, listedTestName)
            .SetName($"{{m}}_{label}");
}
