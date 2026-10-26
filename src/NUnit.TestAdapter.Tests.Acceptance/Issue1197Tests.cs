using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Issue #1197: an explicit fixture and an ignored explicit fixture share a category.
/// Filtering on the category must run the explicit fixture; only the ignored one is skipped.
/// </summary>
public sealed class Issue1197Tests : CsProjAcceptanceTests
{
    protected override void AddTestsCs(IsolatedWorkspace workspace)
    {
        workspace.AddFile("Tests.cs", @"
                using NUnit.Framework;

                namespace Issue1197
                {
                    [TestFixture, Category(""MyCustomCategory"")]
                    [Explicit]
                    [Ignore(""Ignoring"")]
                    public class MyTestClass
                    {
                        [Test]
                        public void MyTest() => Assert.Pass();
                    }

                    [TestFixture, Category(""MyCustomCategory"")]
                    [Explicit]
                    public class AnotherTestClass
                    {
                        [Test]
                        public void AnotherTest() => Assert.Pass();
                    }
                }");
    }

    protected override string Framework => Frameworks.Net80;

    [Test, Platform("Win")]
    public void CategoryFilter_DotNetTest()
    {
        var workspace = Build();
        var results = workspace.DotNetTest("TestCategory=MyCustomCategory", true, true, TestContext.WriteLine);
        Verify(1, 2, results);
    }

    [Test, Platform("Win")]
    public void CategoryFilter_VSTest()
    {
        var workspace = Build();
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter("TestCategory=MyCustomCategory"));
        Verify(1, 2, results);
    }
}
