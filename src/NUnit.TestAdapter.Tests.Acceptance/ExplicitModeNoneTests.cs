using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

public sealed class ExplicitNoneTests : CsProjAcceptanceTests
{
    protected override void AddTestsCs(IsolatedWorkspace workspace)
    {
        workspace.AddFile("Tests.cs", @"
                using NUnit.Framework;

                namespace Filter
                {
                    public class Tests
                    {
        
                        [Test,Explicit,Category(""FooGroup""),Category(""AllGroup"")]
                        public void Foo()
                        {
                            Assert.Pass();
                        }

                        [Test,Explicit,Category(""IsExplicit""),Category(""FooGroup""),Category(""AllGroup"")]
                        public void FooExplicit()
                        {
                            Assert.Pass();
                        }

                        [Test,Explicit, Category(""BarGroup""),Category(""AllGroup"")]
                        public void Bar()
                        {
                            Assert.Pass();
                        }
                    }
                }");
    }

    protected override string Framework => Frameworks.NetCoreApp31;

    [Test, Platform("Win")]
    [TestCase(NoFilter, 0, 3)]
    [TestCase("TestCategory=FooGroup", 0, 2)]
    [TestCase("TestCategory!=BarGroup", 0, 2)]
    [TestCase("TestCategory=IsExplicit", 0, 1)]
    [TestCase("FullyQualifiedName=Filter.Tests.Foo", 0, 1)]
    [TestCase("FullyQualifiedName!=Filter.Tests.Foo", 0, 2)]
    [TestCase("TestCategory!=AllGroup", 0, 0)]
    public void Filter_DotNetTest(string filter, int executed, int total)
    {
        var workspace = Build();
        workspace.ExplicitMode = "None";
        var results = workspace.DotNetTest(filter, false, true, TestContext.WriteLine);
        Verify(executed, total, results);
    }

    [Test, Platform("Win")]
    [TestCase(NoFilter, 0, 3)]
    [TestCase("TestCategory=FooGroup", 0, 2)]
    [TestCase("TestCategory!=BarGroup", 0, 2)]
    [TestCase("TestCategory=IsExplicit", 0, 1)]
    [TestCase("FullyQualifiedName=Filter.Tests.Foo", 0, 1)]
    [TestCase("TestCategory=XXXX", 0, 0)]
    public void Filter_VSTest(string filter, int executed, int total)
    {
        var workspace = Build();
        workspace.ExplicitMode = "None";
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));
        Verify(executed, total, results);
    }

    [Test, Platform("Win")]
    [TestCase(NoFilter, 0, 3)]
    [TestCase("Category=FooGroup", 0, 2)]
    [TestCase("cat==FooGroup", 0, 2)]
    [TestCase("cat!=FooGroup", 0, 1)]
    [TestCase("Category!=BarGroup", 0, 2)]
    [TestCase("Category=IsExplicit", 0, 1)]
    [TestCase("test==Filter.Tests.Foo", 0, 1)]
    [TestCase("name==Foo", 0, 1)]
    [TestCase("name!=Bar", 0, 2)]
    public void Filter_DotNetTest_NUnitWhere(string filter, int executed, int total)
    {
        var workspace = Build();
        workspace.ExplicitMode = "None";
        var nunitWhere = $"NUnit.Where={filter}";
        var results = workspace.DotNetTest(nunitWhere, false, true, TestContext.WriteLine);
        Verify(executed, total, results);
    }

    [TestCase("", 0, 3)]
    [TestCase("TestCategory=IsExplicit", 0, 1)]
    public void DotNetTest_ExplicitModeIsNone(string filter, int executed, int total)
    {
        var workspace = Build();
        workspace.ExplicitMode = "None";
        var results = workspace.DotNetTest(filter);
        Verify(executed, total, results);
    }
}