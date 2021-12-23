using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class ConsoleOutTests : CsProjAcceptanceTests
    {
        protected override void AddTestsCs(IsolatedWorkspace workspace)
        {
            workspace.AddFile("Issue774.cs", @"
                using System;
                using NUnit.Framework;

                namespace Issue774
                {
                    public class ConsoleOutTest
                    {
                        [Test]
                        public void Test1()
                        {
                            Console.WriteLine(); // Did not work pre-Issue774 fix
                            Assert.Pass();
                        }

                        [Test]
                        public void Test2()
                        {
                            Console.WriteLine(""Does work"");
                            Assert.Pass();
                        }
                    }
                }");
        }

        protected override string Framework => Frameworks.NetCoreApp31;

        [Test, Platform("Win")]
        public void DotNetTest()
        {
            var workspace = Build();
            var results = workspace.DotNetTest("", true, true, TestContext.WriteLine);
            Verify(2, 2, results);
        }

        [Test, Platform("Win")]
        public void VsTest()
        {
            var workspace = Build();
            var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", VsTestFilter.NoFilter);
            Verify(2, 2, results);
        }
    }
}