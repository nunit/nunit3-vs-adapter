using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public sealed class FixtureTests : CsProjAcceptanceTests
    {
        protected override void AddTestsCs(IsolatedWorkspace workspace)
        {
            workspace.AddFile("Issue918.cs", @"
                using System;
                using NUnit.Framework;

                namespace Issue918
                {
                    [Category(""918"")]
                    [TestFixtureSource(typeof(FixtureSources), nameof(FixtureSources.Types))]
                    public class SomeTest<T>
                    {
                        [Test]
                        public void Foo()
                        {
                            Assert.Pass();
                        }
                    }

                    public static class FixtureSources
                    {
                        public static Type[] Types =
                        {
                            typeof(object)
                        };
                    }
                }");

            workspace.AddFile("Issue869.cs", @"
                using NUnit.Framework;
                using System;
                using System.Collections.Generic;

                namespace Issue869
                {
                    public static class Sources
                    {
                        public static IEnumerable<Type> GetTypes() => new List<Type>
                        {
                            typeof(string),
                            typeof(bool)
                        };
                    }
                    [Category(""869"")]
                    [TestFixtureSource(typeof(Sources), nameof(Sources.GetTypes))]
                    public class Tests<T>
                    {
                        [Test]
                        public void SomeRandomTest()
                        {
                        }
                    }
                }");

            workspace.AddFile("Issue884.SetupFixture.cs", @"
                using NUnit.Framework;

                namespace NUnitTestAdapterIssueRepro
                {
                    [SetUpFixture]
                    public class SetupFixture
                    {
                        [OneTimeSetUp]
                        public void OneTimeSetup()
                        {
                        }

                        [OneTimeTearDown]
                        public void OneTimeTeardown()
                        {
                        }
                    }
                }");
            workspace.AddFile("Issue884.Tests.cs", @"
                using NUnit.Framework;

                namespace NUnitTestAdapterIssueRepro
                {
                    [Category(""884"")]
                    [TestFixture(1)]
                    [TestFixture(2)]
                    [TestFixture(3)]
                    public class Fixture
                    {
                        public Fixture(int n)
                        {
                        }
                        
                        [SetUp]
                        public void Setup()
                        {
                        }

                        [Test]
                        public void Test()
                        {
                            Assert.Pass();
                        }
                    }
                }");
        }

        protected override string Framework => Frameworks.NetCoreApp31;

        [Test, Platform("Win")]
        [TestCase("TestCategory=869", 2, 2)]
        [TestCase("TestCategory=884", 3, 3)]
        [TestCase("TestCategory=918", 1, 1)]
        public void DotNetTest(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.DotNetTest(filter, true, true, TestContext.WriteLine);
            Verify(executed, total, results);
        }

        [Test, Platform("Win")]
        [TestCase("TestCategory=869", 2, 2)]
        [TestCase("TestCategory=884", 3, 3)]
        [TestCase("TestCategory=918", 1, 1)]
        public void VsTest(string filter, int executed, int total)
        {
            var workspace = Build();
            var results = workspace.VSTest($@"bin\Debug\{Framework}\Test.dll", new VsTestTestCaseFilter(filter));
            Verify(executed, total, results);
        }
    }
}
