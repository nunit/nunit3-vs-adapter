﻿using System.Linq;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class BundledDependencyTests : AcceptanceTests
    {
        [Test]
        public static void User_tests_get_the_version_of_Mono_Cecil_referenced_from_the_test_project()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFrameworks>{string.Join(";", TargetFrameworks)}</TargetFrameworks>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='Mono.Cecil' Version='0.10.0-beta5' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                      <ItemGroup Condition=""'$(TargetFrameworkIdentifier)' == '.NETCoreApp'"">
                        <PackageReference Include='System.Diagnostics.FileVersionInfo' Version='*' />
                      </ItemGroup>

                    </Project>")
                .AddFile("BundledDependencyTests.cs", @"
                    using System.Diagnostics;
                    using System.Reflection;
                    using NUnit.Framework;

                    public static class BundledDependencyTests
                    {
                        [Test]
                        public static void User_tests_get_the_version_of_Mono_Cecil_referenced_from_the_test_project()
                        {
                            var assembly = typeof(Mono.Cecil.ReaderParameters)
                    #if NETCOREAPP1_0
                                .GetTypeInfo()
                    #endif
                                .Assembly;

                            var versionBlock = FileVersionInfo.GetVersionInfo(assembly.Location);

                            Assert.That(versionBlock.ProductVersion, Is.EqualTo(""0.10.0.0-beta5""));
                        }
                    }");

            workspace.MSBuild(restore: true);

            workspace.VSTest(
                from targetFramework in TargetFrameworks
                select $@"bin\Debug\{targetFramework}\Test.dll");
        }

        [Test]
        public static void Engine_uses_its_bundled_version_of_Mono_Cecil_instead_of_the_version_referenced_by_the_test_project()
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFrameworks>{string.Join(";", TargetFrameworks)}</TargetFrameworks>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='Mono.Cecil' Version='0.10.0' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>
                      <ItemGroup Condition=""'$(TargetFrameworkIdentifier)' == '.NETFramework'"">
                        <PackageReference Include='nunit.engine.api' Version='3.9.0' />
                      </ItemGroup>

                      <ItemGroup Condition=""'$(TargetFrameworkIdentifier)' == '.NETCoreApp'"">
                        <PackageReference Include='System.Diagnostics.FileVersionInfo' Version='*' />
                        <PackageReference Include='nunit.engine.netstandard' Version='3.8.0' />
                      </ItemGroup>

                      <ItemGroup>
                        <None Update='test.addins' CopyToOutputDirectory='Always' />
                      </ItemGroup>

                    </Project>")
                .AddFile("BundledDependencyTests.cs", @"
                    using System.Diagnostics;
                    using System.Reflection;
                    using NUnit.Framework;

                    public class ReferencingMonoCecilTests
                    {
                        [Test]
                        public void Engine_uses_its_bundled_version_of_Mono_Cecil_instead_of_the_version_referenced_by_the_test_project()
                        {
                            var assembly = typeof(Mono.Cecil.ReaderParameters)
                    #if NETCOREAPP1_0
                                .GetTypeInfo()
                    #endif
                                .Assembly;

                            var versionBlock = FileVersionInfo.GetVersionInfo(assembly.Location);

                            Assert.That(versionBlock.ProductVersion, Is.EqualTo(""0.10.0.0""));
                        }
                    }")
                .AddFile("TestNUnitEngineExtension.cs", @"
                    using NUnit.Engine;
                    using NUnit.Engine.Extensibility;

                    // Trigger Mono.Cecil binary break between older versions and 0.10.0
                    // (test.addins points the engine to search all classes in this file and should result
                    // in a runtime failure to cast 'Mono.Cecil.InterfaceImplementation' to 'Mono.Cecil.TypeReference'
                    // if the engine is using the newer version of Mono.Cecil)
                    [Extension]
                    public sealed class TestNUnitEngineExtension : ITestEventListener
                    {
                        public void OnTestEvent(string report)
                        {
                        }
                    }")
                .AddFile("test.addins", @"
                    ﻿Test.dll");

            workspace.MSBuild(restore: true);

            workspace.VSTest(
                from targetFramework in TargetFrameworks
                select $@"bin\Debug\{targetFramework}\Test.dll");
        }
    }
}
