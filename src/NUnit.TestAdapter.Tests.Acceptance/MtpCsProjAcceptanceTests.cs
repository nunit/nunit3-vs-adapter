using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Base class for acceptance tests that run the adapter under the Microsoft Testing Platform
/// rather than under VSTest.
///
/// The project it generates sets <c>EnableNUnitRunner</c>, which is what switches the adapter's
/// build assets into MTP mode: the targets then set <c>IsTestingPlatformApplication</c>, suppress
/// the generated entry point and reference the adapter as a library so that
/// <c>TestingPlatformBuilderHook</c> is registered. The project therefore has to be an
/// executable, and it needs the TRX report extension, because the platform does not ship one by
/// default and the harness reads results from a TRX file.
///
/// Tests built on this base exercise
/// <c>NUnitTestFilterBuilder.ConvertVsTestFilterToNUnitFilterForMTP</c> and the bridge's filter
/// handling, neither of which the VSTest-based acceptance tests reach.
/// </summary>
public abstract class MtpCsProjAcceptanceTests : CsProjAcceptanceTests
{
    /// <summary>
    /// Version of the Microsoft Testing Platform TRX report extension to reference. Keep this in
    /// step with the Microsoft.Testing.* versions in <c>Directory.Packages.props</c> and the
    /// dependency versions in <c>nuget/NUnit3TestAdapter.nuspec</c>.
    /// </summary>
    protected const string TestingPlatformTrxReportVersion = "2.4.1";

    /// <summary>
    /// Path of the built test application, relative to the workspace.
    /// </summary>
    protected string TestApplicationPath => $@"bin\Debug\{Framework}\Test.exe";

    protected override IsolatedWorkspace CreateTestWorkspace(string framework)
    {
        return CreateWorkspace()
            .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{framework}</TargetFramework>
                        <OutputType>Exe</OutputType>
                        <EnableNUnitRunner>true</EnableNUnitRunner>
                        <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
                        <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally><Deterministic>false</Deterministic>
                        <!-- The isolated package cache is not always fully populated with
                             satellite assemblies, and copying them adds nothing here. -->
                        <SatelliteResourceLanguages>en</SatelliteResourceLanguages>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='NUnit' Version='{NUnitVersion(framework)}' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                        <PackageReference Include='Microsoft.Testing.Extensions.TrxReport' Version='{TestingPlatformTrxReportVersion}' />
                      </ItemGroup>

                    </Project>");
    }
}
