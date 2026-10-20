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
    [TestCase("Ns.Fixture(\"A\").NoParameter", "Ns.Fixture", "NoParameter", ExpectedResult = "Fixture(\"A\").NoParameter")]
    [TestCase("Ns.Fixture(\"A\").WithParameter(1)", "Ns.Fixture", "WithParameter(1)", ExpectedResult = "Fixture(\"A\").WithParameter(1)")]
    [TestCase("Ns.Outer+Inner(2).Test", "Ns.Outer+Inner", "Test", ExpectedResult = "Outer+Inner(2).Test")]
    [TestCase("Ns.Plain.Test", "Ns.Plain", "Test", ExpectedResult = "Test")]
    [TestCase("Ns.Plain.Test(1)", "Ns.Plain", "Test(1)", ExpectedResult = "Test(1)")]
    [TestCase("Ns.Fixture(\"A\").NoParameter", "Ns.Fixture", "Fixture(\"A\").NoParameter", ExpectedResult = "Fixture(\"A\").NoParameter")]
    [TestCase("Ns.Fixture(\"A\").NoParameter", "Ns.Fixture", "Ns.Fixture(\"A\").NoParameter", ExpectedResult = "Ns.Fixture(\"A\").NoParameter")]
    public string AddsFixtureArgumentsWithoutTrx(string fullyQualifiedName, string className, string displayName)
    {
        var testCase = CreateTestCase(fullyQualifiedName, className, displayName);

        new FixtureDisplayName(new TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities()).Apply(testCase);

        return testCase.DisplayName;
    }

    [Test]
    public void KeepsDisplayNameWithTrx()
    {
        var capabilities = new TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities();
        ((ITrxReportCapability)capabilities).Enable();
        var testCase = CreateTestCase("Ns.Fixture(\"A\").NoParameter", "Ns.Fixture", "NoParameter");

        new FixtureDisplayName(capabilities).Apply(testCase);

        Assert.That(testCase.DisplayName, Is.EqualTo("NoParameter"));
    }

    [Test]
    public void AppliesToResultAndItsTestCase()
    {
        var testCase = CreateTestCase("Ns.Fixture(\"A\").NoParameter", "Ns.Fixture", "NoParameter");
        var testResult = new TestResult(testCase) { DisplayName = "NoParameter" };

        new FixtureDisplayName(new TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities()).Apply(testResult);

        Assert.Multiple(() =>
        {
            Assert.That(testResult.DisplayName, Is.EqualTo("Fixture(\"A\").NoParameter"));
            Assert.That(testCase.DisplayName, Is.EqualTo("Fixture(\"A\").NoParameter"));
        });
    }

    private static TestCase CreateTestCase(string fullyQualifiedName, string className, string displayName)
    {
        var testCase = new TestCase(fullyQualifiedName, new Uri(NUnitTestAdapter.ExecutorUri), "test.dll")
        {
            DisplayName = displayName
        };
        testCase.SetPropertyValue(Seed.NUnitClassName, className);
        return testCase;
    }
}
