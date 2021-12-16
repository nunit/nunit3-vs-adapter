// ***********************************************************************
// Copyright (c) 2014 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.IO;

using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NUnit.Tests
{
    namespace Assemblies
    {
        /// <summary>
        /// Constant definitions for the mock-assembly dll.
        /// </summary>
        public class MockAssembly
        {
            public const int Classes = 11;
            public const int NamespaceSuites = 6; // assembly, NUnit, Tests, Assemblies, Singletons, TestAssembly

            // While const values are copied to other projects at compile time,
            // readonly are taken from assembly loaded at runtime
            // We can check the difference to see if value has changed since compilation
            public static readonly int TestsAtRuntime = Tests;
            public const int Tests = MockTestFixture.Tests
                        + Singletons.OneTestCase.Tests
                        + TestAssembly.MockTestFixture.Tests
                        + IgnoredFixture.Tests
                        + ExplicitFixture.Tests
                        + BadFixture.Tests
                        + FixtureWithTestCases.Tests
                        + ParameterizedFixture.Tests
                        + GenericFixtureConstants.Tests
                        + ParentClass.Tests
                        + FixtureWithAttachment.Tests
                        + OneOfEach.Tests;

            public const int Suites = MockTestFixture.Suites
                        + Singletons.OneTestCase.Suites
                        + TestAssembly.MockTestFixture.Suites
                        + IgnoredFixture.Suites
                        + ExplicitFixture.Suites
                        + BadFixture.Suites
                        + FixtureWithTestCases.Suites
                        + ParameterizedFixture.Suites
                        + GenericFixtureConstants.Suites
                        + NamespaceSuites;

            public const int Nodes = Tests + Suites;

            public const int ExplicitFixtures = 1;
            public const int SuitesRun = Suites - ExplicitFixtures;

            public const int Ignored = MockTestFixture.Ignored + IgnoredFixture.Tests + OneOfEach.IgnoredTests;
            public const int Explicit = MockTestFixture.Explicit + ExplicitFixture.Tests + OneOfEach.ExplicitTests;
            public const int NotRun = Ignored + Explicit + NotRunnable;
            public const int TestsRun = Tests - NotRun;
            public const int ResultCount = Tests;

            public const int Errors = MockTestFixture.Errors;
            public const int Failures = MockTestFixture.Failures;
            public const int NotRunnable = MockTestFixture.NotRunnable + BadFixture.Tests;
            public const int ErrorsAndFailures = Errors + Failures + NotRunnable;
            public const int Inconclusive = MockTestFixture.Inconclusive;
            public const int Success = TestsRun - Errors - Failures - Inconclusive;

            public const int Categories = MockTestFixture.Categories;
        }

        [TestFixture(Description = "Fake Test Fixture")]
        [Category("FixtureCategory")]
        public class MockTestFixture
        {
            public const int Tests = 11;
            public const int Suites = 1;

            public const int Ignored = 1;
            public const int Explicit = 1;

            public const int NotRun = Ignored + Explicit;
            public const int TestsRun = Tests - NotRun;
            public const int ResultCount = Tests - Explicit;

            public const int Failures = 1;
            public const int Errors = 1;
            public const int NotRunnable = 2;
            public const int ErrorsAndFailures = Errors + Failures + NotRunnable;

            public const int Inconclusive = 1;

            public const int Categories = 5;
            public const int MockCategoryTests = 2;

            [Test(Description = "Mock Test #1")]
            public void MockTest1()
            { }

            [Test]
            [Category("MockCategory")]
            [Property("Severity", "Critical")]
            [Description("This is a really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really, really long description")]
            public void MockTest2()
            { }

            [Test]
            [Category("MockCategory")]
            [Category("AnotherCategory")]
            public void MockTest3()
            { Assert.Pass("Succeeded!"); }

            [Test]
            protected static void MockTest5()
            { }

            [Test]
            public void FailingTest()
            {
                Assert.Fail("Intentional failure");
            }

            [Test, Property("TargetMethod", "SomeClassName"), Property("Size", 5), /*Property("TargetType", typeof( System.Threading.Thread ))*/]
            public void TestWithManyProperties()
            { }

            [Test]
            [Ignore("ignoring this test method for now")]
            [Category("Foo")]
            public void MockTest4()
            { }

            [Test, Explicit]
            [Category("Special")]
            public void ExplicitlyRunTest()
            { }

            [Test]
            public void NotRunnableTest(int a, int b)
            {
            }

            [Test]
            public void InconclusiveTest()
            {
                Assert.Inconclusive("No valid data");
            }

            [Test]
            public void TestWithException()
            {
                MethodThrowsException();
            }

            private void MethodThrowsException()
            {
                throw new Exception("Intentional Exception");
            }
        }
    }

    namespace Singletons
    {
        [TestFixture]
        public class OneTestCase
        {
            public const int Tests = 1;
            public const int Suites = 1;

            [Test]
            public virtual void TestCase()
            { }
        }
    }

    namespace TestAssembly
    {
        [TestFixture]
        public class MockTestFixture
        {
            public const int Tests = 1;
            public const int Suites = 1;

            [Test]
            public void MyTest()
            {
            }
        }
    }

    [TestFixture, Ignore("BECAUSE")]
    public class IgnoredFixture
    {
        public const int Tests = 3;
        public const int Suites = 1;

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }

        [Test]
        public void Test3() { }
    }

    [TestFixture, Explicit]
    public class ExplicitFixture
    {
        public const int Tests = 2;
        public const int Suites = 1;
        public const int Nodes = Tests + Suites;

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }
    }

    [TestFixture]
    public class BadFixture
    {
        public const int Tests = 1;
        public const int Suites = 1;

        public BadFixture(int val) { }

        [Test]
        public void SomeTest() { }
    }

    [TestFixture]
    public class FixtureWithTestCases
    {
        public const int Tests = 4;
        public const int Suites = 3;

        [TestCase(2, 2, ExpectedResult = 4)]
        [TestCase(9, 11, ExpectedResult = 20)]
        public int MethodWithParameters(int x, int y)
        {
            return x + y;
        }

        [TestCase(2, 4)]
        [TestCase(9.2, 11.7)]
        public void GenericMethod<T>(T x, T y)
        {
        }
    }

    [TestFixture(5)]
    [TestFixture(42)]
    public class ParameterizedFixture
    {
        public const int Tests = 4;
        public const int Suites = 3;

        public ParameterizedFixture(int num) { }

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }
    }

    public class GenericFixtureConstants
    {
        public const int Tests = 4;
        public const int Suites = 3;
    }

    [TestFixture(5)]
    [TestFixture(11.5)]
    public class GenericFixture<T>
    {
        public GenericFixture(T num) { }

        [Test]
        public void Test1() { }

        [Test]
        public void Test2() { }
    }

    public class ParentClass
    {
        public const int Tests = 3;

        [Test]
        public void NestedClassTest1() { }

        public class ChildClass
        {
            [Test]
            public void NestedClassTest2() { }

            public class GrandChildClass
            {
                [Test]
                public void NestedClassTest3() { }
            }
        }
    }

    [TestFixture]
    public class FixtureWithAttachment
    {
        public const int Tests = 1;

        public static readonly string Attachment1Name = "mock-assembly.dll";
        public static readonly string Attachment1Description = "A description with some <values> including & special characters";

        public static readonly string Attachment2Name = "empty-assembly.dll";
        public static readonly string Attachment2Description = null;

        [Test]
        public void AttachmentTest()
        {
            var filepath1 = Path.Combine(TestContext.CurrentContext.WorkDirectory, Attachment1Name);
            var filepath2 = Path.Combine(TestContext.CurrentContext.WorkDirectory, Attachment2Name);
            Assert.That(File.Exists(filepath1), $"Could not find {filepath1}");
            TestContext.AddTestAttachment(filepath1, Attachment1Description);
            Assert.That(File.Exists(filepath2), $"Could not find {filepath2}");
            TestContext.AddTestAttachment(filepath2, Attachment2Description);
        }
    }

    [Category("OneOfEachCat")]
    [TestFixture]
    public class OneOfEach
    {
        public const int Tests = 3;
        public const int ExplicitTests = 1;
        public const int IgnoredTests = 1;

        [Test]
        public void NormalTest() { }

        [Explicit]
        [Test]
        public void ExplicitTest()
        {
            Environment.Exit(42);
        }

        [Ignore("")]
        [Test]
        public void IgnoredTest() { }
    }
}
