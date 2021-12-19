using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class FilterTests : CsProjAcceptanceTests
    {
        protected override void AddTestsCs(IsolatedWorkspace workspace)
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

        protected override string Framework => Frameworks.NetCoreApp31;

        [Test, Platform("Win")]
        [TestCase(NoFilter, 2, 3)]
        [TestCase(@"TestCategory=FooGroup", 1, 2)]
        [TestCase(@"TestCategory!=BarGroup", 1, 2)]
        [TestCase(@"TestCategory=IsExplicit", 1, 1)]
        [TestCase(@"FullyQualifiedName=Filter.Tests.Foo", 1, 1)]
        [TestCase(@"FullyQualifiedName!=Filter.Tests.Foo", 1, 1)]
        // [TestCase(@"FullyQualifiedName~Filter.Tests.Foo", 1, 1)]
        // [TestCase(@"FullyQualifiedName~Foo", 1, 1)]
        public void Filter_DotNetTest(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.DotNetTest(filter, true, true, TestContext.WriteLine);
            Verify(executed, total, results);
        }

        [Test, Platform("Win")]
        [TestCase(NoFilter, 2, 3)]
        [TestCase(@"TestCategory=FooGroup", 1, 2)]
        [TestCase(@"TestCategory!=BarGroup", 1, 2)]
        [TestCase(@"TestCategory=IsExplicit", 1, 1)]
        [TestCase(@"FullyQualifiedName=Filter.Tests.Foo", 1, 1)]
        public void Filter_VSTest(string filter, int executed, int total)
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
        [TestCase("cat!=FooGroup", 1, 1)]
        [TestCase("Category!=BarGroup", 1, 1)]
        [TestCase("Category=IsExplicit", 1, 1)]
        [TestCase("test==Filter.Tests.Foo", 1, 1)]
        [TestCase("name==Foo", 1, 1)]
        [TestCase("name!=Bar", 1, 1)]
        // [TestCase("test=~Foo", 1, 1)]
        public void Filter_DotNetTest_NUnitWhere(string filter, int executed, int total)
        {
            var workspace = Build();
            var nunitWhere = $@"NUnit.Where={filter}";
            var results = workspace.DotNetTest(nunitWhere, true, true, TestContext.WriteLine);
            Verify(executed, total, results);
        }
    }
}
