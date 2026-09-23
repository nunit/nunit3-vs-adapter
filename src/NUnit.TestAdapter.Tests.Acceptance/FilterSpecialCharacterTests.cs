using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

public sealed class FilterSpecialCharacterTests : CsProjAcceptanceTests
{
    protected override void AddTestsCs(IsolatedWorkspace workspace)
    {
        workspace.AddFile("Issue919.cs", """
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
            }
            """);

        // Add test cases for the rest of the special characters that need to be escaped in the filter:
        // https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests?pivots=nunit#character-escaping
        workspace.AddFile("Issue1488.cs", """
            using System;
            using NUnit.Framework;

            namespace Issue1488
            {
                public class Foo
                {
                    [TestCase(1, TestName = @"Backslash(C:\Temp)")]
                    public void BackslashCase(int a)
                    {
                        Assert.Pass();
                    }

                    [TestCase(1, TestName = @"Ampersand(a & b)")]
                    public void AmpersandCase(int a)
                    {
                        Assert.Pass();
                    }

                    [TestCase(1, TestName = @"Pipe(a | b)")]
                    public void PipeCase(int a)
                    {
                        Assert.Pass();
                    }

                    [TestCase(1, TestName = @"Equal(a = b)")]
                    public void EqualCase(int a)
                    {
                        Assert.Pass();
                    }

                    [TestCase(1, TestName = @"Bang(a ! b)")]
                    public void BangCase(int a)
                    {
                        Assert.Pass();
                    }

                    [TestCase(1, TestName = @"Tilde(a ~ b)")]
                    public void TildeCase(int a)
                    {
                        Assert.Pass();
                    }
                }
            }
            """);
    }

    protected override string Framework => Frameworks.Net80;

    [Test, Platform("Win")]
    [TestCase]
    public void VsTestNoFilter()
    {
        var workspace = Build();
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", VsTestFilter.NoFilter);
        Verify(8, 8, results);
    }

    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=Issue919.Foo.Bzzt", 1, 1)] // Sanity check
    [TestCase(@"FullyQualifiedName=Issue919.Foo.Bar\(1\)", 0, 0)]
    [TestCase(@"FullyQualifiedName=Issue919.Foo.Baz\(1\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Backslash\(C:\\Temp\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Ampersand\(a \& b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Pipe\(a \| b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Equal\(a \= b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Bang\(a \! b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Tilde\(a \~ b\)", 1, 1)]
    [TestCase("Name=Bzzt", 1, 1)] // Sanity check
    [TestCase(@"Name=Bar\(1\)", 0, 0)]
    [TestCase(@"Name=Baz\(1\)", 1, 1)]
    [TestCase("", 8, 8)]
    public void VsTestTestCases(string filter, int executed, int total)
    {
        var workspace = Build();
        workspace.DumpTestExecution = true;
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));
        Verify(executed, total, results);
    }

    [Test, Platform("Win")]
    [TestCase("Bzzt", 1, 1)] // Sanity check
    [TestCase(@"Bar\(1\)", 0, 0)]
    [TestCase(@"Baz\(1\)", 1, 1)]
    public void VsTestTests(string filter, int executed, int total)
    {
        var workspace = Build();
        var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestsFilter(filter));
        Verify(executed, total, results);
    }

    [Test, Platform("Win")]
    [TestCase("FullyQualifiedName=Issue919.Foo.Bzzt", 1, 1)] // Sanity check
    [TestCase(@"FullyQualifiedName=Issue919.Foo.Bar\(1\)", 0, 0)]
    [TestCase(@"FullyQualifiedName=Issue919.Foo.Baz\(1\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Backslash\(C:\\Temp\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Ampersand\(a \& b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Pipe\(a \| b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Equal\(a \= b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Bang\(a \! b\)", 1, 1)]
    [TestCase(@"FullyQualifiedName=Issue1488.Foo.Tilde\(a \~ b\)", 1, 1)]
    [TestCase("Name=Bzzt", 1, 1)] // Sanity check
    [TestCase(@"Name=Bar\(1\)", 0, 0)]
    [TestCase(@"Name=Baz\(1\)", 1, 1)]
    [TestCase("", 8, 8)]
    public void DotnetTestCases(string filter, int executed, int total)
    {
        var workspace = Build();
        var results = workspace.DotNetTest(filter, true, true, TestContext.WriteLine);
        Verify(executed, total, results);
    }
}
