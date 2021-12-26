using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class ParanthesisTests : CsProjAcceptanceTests
    {
        protected override void AddTestsCs(IsolatedWorkspace workspace)
        {
            workspace.AddFile("Issue919.cs", @" 
                using System;
                using NUnit.Framework;

                namespace Issue919
                {
                    public class Foo
                    {
                       [TestCase(1)]
                       public void Baz(int a)
                       {
                           Assert.Pass();
                       }
       
                       [Test]
                       public void Bzzt()
                       {
                           Assert.Pass();
                       }
                    }
               }");
        }

        protected override string Framework => Frameworks.NetCoreApp31;

        [Test, Platform("Win")]
        [TestCase]
        public void VsTestNoFilter()
        {
            var workspace = Build();
            var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", VsTestFilter.NoFilter);
            Verify(2, 2, results);
        }

        [Test, Platform("Win")]
        [TestCase(@"FullyQualifiedName=Issue919.Foo.Bzzt", 1, 1)] // Sanity check
        [TestCase(@"FullyQualifiedName=Issue919.Foo.Bar\(1\)", 0, 0)]
        [TestCase(@"FullyQualifiedName=Issue919.Foo.Baz\(1\)", 1, 1)]
        [TestCase(@"Name=Bzzt", 1, 1)] // Sanity check
        [TestCase(@"Name=Bar\(1\)", 0, 0)]
        [TestCase(@"Name=Baz\(1\)", 1, 1)]
        [TestCase(@"", 2, 2)]
        public void VsTestTestCases(string filter, int executed, int total)
        {
            var workspace = Build();
            workspace.DumpTestExecution = true;
            var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));
            Verify(executed, total, results);
        }

        [Test, Platform("Win")]
        [TestCase(@"Bzzt", 1, 1)] // Sanity check
        [TestCase(@"Bar\(1\)", 0, 0)]
        [TestCase(@"Baz\(1\)", 1, 1)]
        public void VsTestTests(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestsFilter(filter));
            Verify(executed, total, results);
        }

        [Test, Platform("Win")]
        [TestCase(@"FullyQualifiedName=Issue919.Foo.Bzzt", 1, 1)] // Sanity check
        [TestCase(@"FullyQualifiedName=Issue919.Foo.Bar\(1\)", 0, 0)]
        [TestCase(@"FullyQualifiedName=Issue919.Foo.Baz\(1\)", 1, 1)]
        [TestCase(@"Name=Bzzt", 1, 1)] // Sanity check
        [TestCase(@"Name=Bar\(1\)", 0, 0)]
        [TestCase(@"Name=Baz\(1\)", 1, 1)]
        [TestCase(@"", 2, 2)]
        public void DotnetTestCases(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.DotNetTest(filter, true, true, TestContext.WriteLine);
            Verify(executed, total, results);
        }
    }
}