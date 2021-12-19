using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class FilterTests : AcceptanceTests
    {
        private static void AddTestsCs(IsolatedWorkspace workspace)
        {
            workspace.AddFile("Tests.cs", @"
                using NUnit.Framework;

                namespace Filter
                {
                    public class Tests
                    {
        
                        [Test,Category(""FooGroup"")]
                        public void Foo()
                        {
                            Assert.Pass();
                        }

                        [Test,Explicit,Category(""IsExplicit""),Category(""FooGroup"")]
                        public void FooExplicit()
                        {
                            Assert.Pass();
                        }

                        [Test, Category(""BarGroup"")]
                        public void Bar()
                        {
                            Assert.Pass();
                        }
                    }
                }");
        }

        private static IsolatedWorkspace CreateTestWorkspace(string framework)
        {
            var workspace = CreateWorkspace()
                .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{framework}</TargetFramework>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='*' />
                        <PackageReference Include='NUnit' Version='*' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                      </ItemGroup>

                    </Project>");
            return workspace;
        }
        private const string Framework = "netcoreapp3.1";
        private static IsolatedWorkspace Build()
        {
            var workspace = CreateTestWorkspace(Framework);
            AddTestsCs(workspace);
            workspace.MsBuild(restore: true);
            return workspace;
        }

        private const string NoFilter = "";

        [Test, Platform("Win")]
        [TestCase(NoFilter, 2, 3)]
        [TestCase(@"TestCategory=FooGroup", 1, 2)]
        [TestCase(@"TestCategory!=BarGroup", 1, 2)]
        [TestCase(@"TestCategory=IsExplicit", 1, 1)]
        [TestCase(@"FullyQualifiedName=Filter.Tests.Foo", 1, 1)]
        [TestCase(@"FullyQualifiedName!=Filter.Tests.Foo", 1, 1)]
        // [TestCase(@"FullyQualifiedName~Filter.Tests.Foo", 1, 1)]
        // [TestCase(@"FullyQualifiedName~Foo", 1, 1)]
        public static void Filter_DotNetTest(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.DotNetTest(filter, true, true);
            Verify(executed, total, results);
        }

        [Test, Platform("Win")]
        [TestCase(NoFilter, 2, 3)]
        [TestCase(@"TestCategory=FooGroup", 1, 2)]
        [TestCase(@"TestCategory!=BarGroup", 1, 2)]
        [TestCase(@"TestCategory=IsExplicit", 1, 1)]
        [TestCase(@"FullyQualifiedName=Filter.Tests.Foo", 1, 1)]
        public static void Filter_VSTest(string filter, int executed, int total)
        {
            var workspace = Build();
            var completeFilterStatement = filter.Length > 0
                ? $"/TestCaseFilter:{filter}"
                : "";
            var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", completeFilterStatement);
            Verify(executed, total, results);
        }

        [Test, Platform("Win")]
        [TestCase(NoFilter, 2, 3)]
        [TestCase("Category=FooGroup", 1, 1)]
        [TestCase("cat==FooGroup", 1, 2)]
        [TestCase("Category!=BarGroup", 1, 1)]
        [TestCase("Category=IsExplicit", 1, 1)]
        [TestCase("test==Filter.Tests.Foo", 1, 1)]
        [TestCase("name==Foo", 1, 1)]
        [TestCase("name!=Bar", 1, 1)]
        //  [TestCase("test=~Foo", 1, 1)]
        public static void Filter_DotNetTest_NUnitWhere(string filter, int executed, int total)
        {
            var workspace = Build();
            var nunitWhere = $@"NUnit.Where={filter}";
            var results = workspace.DotNetTest(nunitWhere, true, true,TestContext.WriteLine);
            Verify(executed, total, results);
        }

        private static void Verify(int executed, int total, VSTestResult results)
        {
            TestContext.WriteLine(" ");
            foreach (var error in results.RunErrors)
                TestContext.WriteLine(error);
            Assert.Multiple(() =>
            {
                Assert.That(results.Counters.Total, Is.EqualTo(total),
                    $"Total tests counter did not match expectation\n{results.ProcessRunResult.StdOut}");
                Assert.That(results.Counters.Executed, Is.EqualTo(executed),
                    "Executed tests counter did not match expectation");
                Assert.That(results.Counters.Passed, Is.EqualTo(executed), "Passed tests counter did not match expectation");
            });
        }
    }
}
