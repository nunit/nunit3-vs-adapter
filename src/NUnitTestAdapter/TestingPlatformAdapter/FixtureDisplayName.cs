using System;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

/// <summary>
/// Puts the fixture arguments in front of the display name, e.g. Fixture("A").NoParameter (#1513).
/// Without trx, TestMethodIdentifierProperty.TypeName is the plain CLR type, so VS Test Explorer
/// shows all instances of a parameterized fixture under one class node. The display name is then
/// the only place that tells them apart. With trx, TypeName keeps the fixture arguments (#1351),
/// so the display name is left as it is.
/// </summary>
internal sealed class FixtureDisplayName(TestApplicationBuilderExtensions.NUnitTestFrameworkCapabilities capabilities)
{
    public void Apply(TestCase testCase)
    {
        testCase.DisplayName = Create(testCase.DisplayName, testCase);
    }

    public void Apply(TestResult testResult)
    {
        Apply(testResult.TestCase);
        testResult.DisplayName = Create(testResult.DisplayName, testResult.TestCase);
    }

    private string Create(string displayName, TestCase testCase)
    {
        if (capabilities.IsTrxReportEnabled || displayName is null)
            return displayName;

        var className = testCase.GetPropertyValue<string>(Seed.NUnitClassName, null);
        var fixture = testCase.GetPropertyValue<string>(Seed.NUnitFixtureName, null);
        if (className is null || fixture is null || fixture == className)
            return displayName;

        // Apply runs more than once on the same test case, so skip names that already have the prefix.
        var prefix = TestMethodIdentifierBuilder.FixtureTypeName(className, fixture) + ".";
        return displayName.StartsWith(prefix, StringComparison.Ordinal) || displayName.StartsWith(fixture + ".", StringComparison.Ordinal)
            ? displayName
            : prefix + displayName;
    }
}
