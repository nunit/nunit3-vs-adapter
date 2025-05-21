using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

public sealed class TestSourceWithCustomNames : AcceptanceTests
{
    private static void AddTestsCs(IsolatedWorkspace workspace)
    {
        workspace.AddFile("Tests.cs", @"
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using NUnit.Framework;

                namespace Test
                {
                    public class Tests
                    {
                        [Test]
                        [TestCaseSource(nameof(TestCaseSourceMethod))]
                        public void PassingTestStr(object arg)
                        {
                            Assert.Pass();
                        }


                        private static IEnumerable<TestCaseData> TestCaseSourceMethod()
                        {
                            yield return new TestCaseData(""Name with mismatched parenthesis 'pp:-) :-)'"").SetName(""Name with mismatched parenthesis 'pp:-) :-)'"");
                            yield return new TestCaseData(""Name with mismatched quote '\""c'"").SetName(""Name with mismatched quote '\""c'"");
                            
                            // Adding a parenthesis to the end of this test name will stop the exception from throwing (e.g. $""TestName(...)"")
                            yield return new TestCaseData(1).SetName($""TestName(..."");

                            // Cannot be duplicated without a second test included that ends with a ']'
                            yield return new TestCaseData(2).SetName($""TestName(...)]"");
                        }

                        [Test]
                        [TestCase(typeof(IEnumerable<(string oneValue, int twoValue)>))]
                        public void UnitTest_TestCaseWithTuple_TestIsNotExecuted(Type targetType)
                        {
                            Assert.That(targetType, Is.EqualTo(typeof(IEnumerable<(string oneValue, int twoValue)>)));
                        }

                        [Test, TestCaseSource(nameof(SourceA))]
                        public void TestA((int a, int b) x, int y) { }
                        public static IEnumerable SourceA => new[] {new TestCaseData((a: 1, b: 2), 5)};

                        [Test, TestCaseSource(nameof(SourceB))]
                        public void TestB(int y, (int a, int b) x) { }
                        public static IEnumerable SourceB => new[] {new TestCaseData(5, (a: 1, b: 2))};

                        [Test, TestCaseSource(nameof(SourceC))]
                        public void TestC((int a, int b) x, int y) { }
                        public static IEnumerable SourceC => new[] {new TestCaseData((a: 1, b: 2), 5).SetArgDisplayNames(""a+b"", ""y"")};

                        [Test(), TestCaseSource(typeof(CaseTestData), nameof(CaseTestData.EqualsData))]
                        public void EqualsTest(Case case1, Case case2)
                        {
                            Assert.That(case1, Is.EqualTo(case2));
                        }

                        public class CaseTestData
                        {
                            public static IEnumerable EqualsData()
                            {
                                yield return new object[] { new Case { Name = ""case1"" }, new Case { Name = ""case1"" } };
                            }
                        }

                        public class Case
                        {
                            public string Name;
                            public override string ToString() => Name;
                            public override bool Equals(object obj) => obj is Case other && other.Name == this.Name;
                        }

                    }
                }");
    }

    [Test, Platform("Win")]
    [TestCaseSource(typeof(SingleFrameworkSource), nameof(SingleFrameworkSource.AllFrameworks))]
    public static void Single_target_csproj(SingleFrameworkSource source)
    {
        var valuetupleItemgroup = source.Framework is "netcoreapp3.1" or "net5.0" ? @"
                      <ItemGroup>
                        <Reference Include=""System.ValueTuple"" />
                      </ItemGroup>" : "";
        TestContext.Out.WriteLine($"Testing {source.Framework}");
        var workspace = CreateWorkspace()
            .AddProject("Test.csproj", $@"
                    <Project Sdk='Microsoft.NET.Sdk'>

                      <PropertyGroup>
                        <TargetFramework>{source.Framework}</TargetFramework>
                        <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally><Deterministic>false</Deterministic>
                      </PropertyGroup>

                      <ItemGroup>
                        <PackageReference Include='Microsoft.NET.Test.Sdk' Version='17.13.0' />
                        <PackageReference Include='NUnit' Version='{source.NUnitVersion}' />
                        <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                        <PackageReference Include='System.ValueTuple' Version='4.5.0' />
                      </ItemGroup>

                    

                    </Project>");

        AddTestsCs(workspace);

        workspace.MsBuild(restore: true);

        var results = workspace.VSTest($@"bin\Debug\{source.Framework}\Test.dll", VsTestFilter.NoFilter);

        // Total Tests =
        //              3 from PassingTestStr/TestCaseSourceMethod
        //              1 from UnitTest_TestCaseWithTuple_TestIsNotExecuted
        //              1 from TestA/SourceA
        //              1 from TestB/SourceB
        //              1 from TestC/SourceC
        //              2 from EqualsTest/EqualsData
        //-------------------
        //              9 Total Tests



        Assert.That(results.Counters.Total, Is.EqualTo(9), "Total tests counter did not match expectation");
        Assert.That(results.Counters.Executed, Is.EqualTo(9), "Executed tests counter did not match expectation");
        Assert.That(results.Counters.Passed, Is.EqualTo(9), "Passed tests counter did not match expectation");
    }
}