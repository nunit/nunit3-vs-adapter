using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class PropertyTests : CsProjAcceptanceTests
    {
        protected override void AddTestsCs(IsolatedWorkspace workspace)
        {
            workspace.AddFile("Issue779.cs", @"
                using System;
                using NUnit.Framework;

                namespace Issue779
                {
                    public class PropertyTest
                    {
                        [Property(""Bug"", ""12345"")]
                        [Test]
                        public void Test1()
                        {
                            Assert.Pass();
                        }

                        [Test]
                        public void Test2()
                        {
                            Assert.Pass();
                        }
                    }
                }");
        }

        protected override string Framework => Frameworks.NetCoreApp31;

        [Test, Platform("Win")]
        [TestCase("Bug=99999", 0, 0)]
        [TestCase("Bug=12345", 1, 1)]
        [TestCase("Bug!=12345", 1, 1)]
        public void DotNetTest(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.DotNetTest(filter, true, true, TestContext.WriteLine);
            Verify(executed, total, results);
        }

        [Test, Platform("Win")]
        [TestCase("Bug=99999", 0, 0)]
        [TestCase("Bug=12345", 1, 1)]
        [TestCase("Bug!=12345", 1, 1)]
        public void VsTest(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));
            Verify(executed, total, results);
        }
    }
}