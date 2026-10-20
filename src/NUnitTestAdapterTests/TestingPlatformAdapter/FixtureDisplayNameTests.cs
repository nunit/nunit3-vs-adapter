using System;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestingPlatformAdapter;

/// <summary>
/// Issue #1513: without trx, all instances of a parameterized fixture share one class node in
/// VS Test Explorer, so the display name must show the fixture arguments.
/// </summary>
public class FixtureDisplayNameTests
{
    [TestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "NoParameter", ExpectedResult = "Fixture(\"A\").NoParameter")]
    [TestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "WithParameter(1)", ExpectedResult = "Fixture(\"A\").WithParameter(1)")]
    [TestCase("Ns.Outer+Inner(2)", "Ns.Outer+Inner", "Test", ExpectedResult = "Outer+Inner(2).Test")]
    [TestCase("Ns.Plain", "Ns.Plain", "Test", ExpectedResult = "Test")]
    [TestCase("Ns.Plain", "Ns.Plain", "Test(1)", ExpectedResult = "Test(1)")]
    [TestCase(null, "Ns.Plain", "Test", ExpectedResult = "Test")]
    [TestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "Fixture(\"A\").NoParameter", ExpectedResult = "Fixture(\"A\").NoParameter")]
    [TestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "Ns.Fixture(\"A\").NoParameter", ExpectedResult = "Ns.Fixture(\"A\").NoParameter")]
    [TestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "scenario.step", ExpectedResult = "Fixture(\"A\").scenario.step")]
    [TestCase("Ns.Fixture(\")\")", "Ns.Fixture", "Test", ExpectedResult = "Fixture(\")\").Test")]
    [TestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "WithParameter(\")\")", ExpectedResult = "Fixture(\"A\").WithParameter(\")\")")]
    [TestCase("Ns.Plain", "Ns.Plain", "case.version.1", ExpectedResult = "case.version.1")]
    public string AddsFixtureArgumentsWithoutTrx(string fixtureFullName, string className, string displayName)
    {
        var testCase = CreateTestCase(fixtureFullName, className, displayName);

        new FixtureDisplayName(new TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities()).Apply(testCase);

        return testCase.DisplayName;
    }

    [Test]
    public void IsIdempotent()
    {
        var testCase = CreateTestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "NoParameter");
        var displayName = new FixtureDisplayName(new TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities());

        displayName.Apply(testCase);
        displayName.Apply(testCase);

        Assert.That(testCase.DisplayName, Is.EqualTo("Fixture(\"A\").NoParameter"));
    }

    [Test]
    public void KeepsDisplayNameWithTrx()
    {
        var capabilities = new TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities();
        ((ITrxReportCapability)capabilities).Enable();
        var testCase = CreateTestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "NoParameter");

        new FixtureDisplayName(capabilities).Apply(testCase);

        Assert.That(testCase.DisplayName, Is.EqualTo("NoParameter"));
    }

    [Test]
    public void AppliesToResultAndItsTestCase()
    {
        var testCase = CreateTestCase("Ns.Fixture(\"A\")", "Ns.Fixture", "NoParameter");
        var testResult = new TestResult(testCase) { DisplayName = "NoParameter" };

        new FixtureDisplayName(new TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities()).Apply(testResult);

        Assert.Multiple(() =>
        {
            Assert.That(testResult.DisplayName, Is.EqualTo("Fixture(\"A\").NoParameter"));
            Assert.That(testCase.DisplayName, Is.EqualTo("Fixture(\"A\").NoParameter"));
        });
    }

    private static TestCase CreateTestCase(string fixtureFullName, string className, string displayName)
    {
        var testCase = new TestCase($"{fixtureFullName ?? className}.{displayName}", new Uri(NUnitTestAdapter.ExecutorUri), "test.dll")
        {
            DisplayName = displayName
        };
        testCase.SetPropertyValue(Seed.NUnitClassName, className);
        if (fixtureFullName != null)
            testCase.SetPropertyValue(Seed.NUnitFixtureName, fixtureFullName);
        return testCase;
    }
}
