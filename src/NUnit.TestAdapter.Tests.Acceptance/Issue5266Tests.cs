using System.IO;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Acceptance tests for Issue 5266: IncludeStackTraceForSuites setting should control
/// stack traces for test cases that fail due to parent SetUpFixture/TestFixture OneTimeSetUp failures.
/// </summary>
public class Issue5266Tests : AcceptanceTests
{
    private const string SetUpFixtureTestCode = @"
using System;
using NUnit.Framework;

namespace Issue5266.SetUpFixtureScenario
{
    [SetUpFixture]
    public class FailingSetupFixture
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            throw new InvalidOperationException(
                ""Database connection failed: Unable to connect to server."");
        }
    }

    public class TestClass1
    {
        [Test]
        public void Test1() => Assert.Pass();

        [Test]
        public void Test2() => Assert.Pass();
    }
}
";

    private const string TestFixtureTestCode = @"
using System;
using NUnit.Framework;

namespace Issue5266.TestFixtureScenario
{
    [TestFixture]
    public class FixtureWithFailingOneTimeSetUp
    {
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            throw new ArgumentException(
                ""Invalid configuration: The ConnectionString setting is missing."");
        }

        [Test]
        public void Test1() => Assert.Pass();

        [Test]
        public void Test2() => Assert.Pass();
    }

    [TestFixture]
    public class WorkingFixture
    {
        [Test]
        public void PassingTest() => Assert.Pass();
    }
}
";

    [TestCaseSource(typeof(SingleFrameworkSource), nameof(SingleFrameworkSource.AllFrameworks))]
    public void SetUpFixture_SuiteStackTraceFalse(SingleFrameworkSource source)
    {
        // Arrange
        var workspace = CreateWorkspace()
            .AddProject("Test.csproj", $@"
                <Project Sdk='Microsoft.NET.Sdk'>
                  <PropertyGroup>
                    <TargetFramework>{source.Framework}</TargetFramework>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include='Microsoft.NET.Test.Sdk' Version='{MicrosoftTestSdkVersion}' />
                    <PackageReference Include='NUnit' Version='{source.NUnitVersion}' />
                    <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                  </ItemGroup>
                </Project>")
            .AddFile("SetUpFixtureTests.cs", SetUpFixtureTestCode);

        workspace.MsBuild(restore: true);

        // Act - Run with IncludeStackTraceForSuites=false
        var result = RunDotNetTestWithSetting(workspace.Directory, "NUnit.IncludeStackTraceForSuites=false");

        // Assert
        TestContext.Out.WriteLine("=== Test Output ===");
        TestContext.Out.WriteLine(result.StdOut);

        // The SetUpFixture failure message should appear (once, at suite level)
        Assert.That(result.StdOut, Does.Contain("Database connection failed"),
            "The SetUpFixture error message should be present");

        // But the stack trace should NOT appear multiple times for each child test
        // Count occurrences of the stack trace marker
        int stackTraceCount = CountOccurrences(result.StdOut, "FailingSetupFixture.OneTimeSetUp()");

        // With IncludeStackTraceForSuites=false, we should see the stack trace only once (at suite level)
        // not repeated for each child test
        Assert.That(stackTraceCount, Is.LessThanOrEqualTo(1),
            $"Stack trace should appear at most once, but appeared {stackTraceCount} times. " +
            "The IncludeStackTraceForSuites=false setting should prevent stack traces from being repeated for each child test.");
    }

    [TestCaseSource(typeof(SingleFrameworkSource), nameof(SingleFrameworkSource.AllFrameworks))]
    public void TestFixture_SuiteStackTraceFalse(SingleFrameworkSource source)
    {
        // Arrange
        var workspace = CreateWorkspace()
            .AddProject("Test.csproj", $@"
                <Project Sdk='Microsoft.NET.Sdk'>
                  <PropertyGroup>
                    <TargetFramework>{source.Framework}</TargetFramework>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include='Microsoft.NET.Test.Sdk' Version='{MicrosoftTestSdkVersion}' />
                    <PackageReference Include='NUnit' Version='{source.NUnitVersion}' />
                    <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                  </ItemGroup>
                </Project>")
            .AddFile("TestFixtureTests.cs", TestFixtureTestCode);

        workspace.MsBuild(restore: true);

        // Act - Run with IncludeStackTraceForSuites=false
        var result = RunDotNetTestWithSetting(workspace.Directory, "NUnit.IncludeStackTraceForSuites=false");

        // Assert
        TestContext.Out.WriteLine("=== Test Output ===");
        TestContext.Out.WriteLine(result.StdOut);

        // The TestFixture failure message should appear (once, at fixture level)
        Assert.That(result.StdOut, Does.Contain("ConnectionString setting is missing"),
            "The TestFixture error message should be present");

        // But the stack trace should NOT appear multiple times for each child test
        int stackTraceCount = CountOccurrences(result.StdOut, "FixtureWithFailingOneTimeSetUp.OneTimeSetUp()");

        Assert.That(stackTraceCount, Is.LessThanOrEqualTo(1),
            $"Stack trace should appear at most once, but appeared {stackTraceCount} times. " +
            "The IncludeStackTraceForSuites=false setting should prevent stack traces from being repeated for each child test.");
    }

    [TestCaseSource(typeof(SingleFrameworkSource), nameof(SingleFrameworkSource.AllFrameworks))]
    public void SetUpFixture_SuiteStackTraceTrue(SingleFrameworkSource source)
    {
        // Arrange - Default behavior (IncludeStackTraceForSuites=true)
        var workspace = CreateWorkspace()
            .AddProject("Test.csproj", $@"
                <Project Sdk='Microsoft.NET.Sdk'>
                  <PropertyGroup>
                    <TargetFramework>{source.Framework}</TargetFramework>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include='Microsoft.NET.Test.Sdk' Version='{MicrosoftTestSdkVersion}' />
                    <PackageReference Include='NUnit' Version='{source.NUnitVersion}' />
                    <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                  </ItemGroup>
                </Project>")
            .AddFile("SetUpFixtureTests.cs", SetUpFixtureTestCode);

        workspace.MsBuild(restore: true);

        // Act - Run with default settings (IncludeStackTraceForSuites=true)
        var result = RunDotNetTestWithSetting(workspace.Directory, "NUnit.IncludeStackTraceForSuites=true");

        // Assert
        TestContext.Out.WriteLine("=== Test Output ===");
        TestContext.Out.WriteLine(result.StdOut);

        // With IncludeStackTraceForSuites=true (default), stack traces should appear for child tests
        int stackTraceCount = CountOccurrences(result.StdOut, "FailingSetupFixture.OneTimeSetUp()");

        // We expect the stack trace to appear multiple times (once per child test + once for suite)
        Assert.That(stackTraceCount, Is.GreaterThan(1),
            $"With IncludeStackTraceForSuites=true, stack trace should appear multiple times, but appeared {stackTraceCount} times.");
    }

    private static ProcessRunResult RunDotNetTestWithSetting(string workingDirectory, string nunitSetting)
    {
        var arguments = new[]
        {
            "test",
            "--no-build",
            "-v:n",
            "--",
            nunitSetting
        };

        return ProcessUtils.Run(workingDirectory, "dotnet", arguments);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(pattern))
            return 0;

        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
