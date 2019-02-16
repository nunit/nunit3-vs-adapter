using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class NuGetRestoreTests : AcceptanceTests
    {
        public static IEnumerable<string> TargetFrameworks => new[]
        {
            "net35",
            "netcoreapp1.0"
        };

        [TestCaseSource(nameof(TargetFrameworks))]
        public static void NuGet_package_can_be_restored_for_single_target_csproj(string targetFramework)
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{targetFramework}</TargetFramework>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>")
                .AddFile("SimpleTests.cs", @"
                    using NUnit.Framework;

                    namespace Simple
                    {
                        public class SimpleTests
                        {
                            [Test]
                            public void PassingTest()
                            {
                                Assert.Pass();
                            }
                        }
                    }");

            Assert.That(1, Is.EqualTo(2));
        }

        [TestCaseSource(nameof(TargetFrameworks))]
        public static void NuGet_package_can_be_restored_for_single_target_vbproj(string targetFramework)
        {
            throw new NotImplementedException();
        }
    }
}
