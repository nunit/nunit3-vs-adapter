using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public abstract class CsProjAcceptanceTests : AcceptanceTests
    {
        protected abstract void AddTestsCs(IsolatedWorkspace workspace);

        protected abstract string Framework { get; }
        protected const string NoFilter = "";
        protected IsolatedWorkspace CreateTestWorkspace(string framework)
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

        protected IsolatedWorkspace Build()
        {
            var workspace = CreateTestWorkspace(Framework);
            AddTestsCs(workspace);
            workspace.MsBuild(restore: true);
            return workspace;
        }

        protected void Verify(int executed, int total, VSTestResult results)
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
