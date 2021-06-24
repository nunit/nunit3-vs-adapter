using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NUnit.Engine;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class ExecutionTests
    {
        IExecutionContext ctx;
        TestFilter filter;
        IDiscoveryConverter discovery;

        [SetUp]
        public void Setup()
        {
            ctx = Substitute.For<IExecutionContext>();
            var settings = Substitute.For<IAdapterSettings>();
            settings.AssemblySelectLimit.Returns(10);
            ctx.Settings.Returns(settings);
            var engineAdapter = new NUnitEngineAdapter();
            engineAdapter.Initialize();
            ctx.EngineAdapter.Returns(engineAdapter);
            settings.DiscoveryMethod.Returns(DiscoveryMethod.Current);
            discovery = Substitute.For<IDiscoveryConverter>();
            discovery.NoOfLoadedTestCases.Returns(1);
            discovery.IsDiscoveryMethodCurrent.Returns(true);

            discovery.LoadedTestCases.Returns(new List<TestCase>
            {
                new ("A", new Uri(NUnitTestAdapter.ExecutorUri), "line 23")
            });
            filter = new TestFilter("<filter><or>A<or>B</or></or></filter>");
        }

        [Explicit("Need to mock out the engine, it crashes on [command line] build due to multiple instances that can't be handled")]
        [Test]
        public void ThatCheckFilterInCurrentModeWorks()
        {
            var sut = new IdeExecution(ctx);
            var result = sut.CheckFilterInCurrentMode(filter, discovery);
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Text, Is.Not.EqualTo(filter.Text));
            Assert.That(result.Text, Is.EqualTo("<filter><test>A</test></filter>"));
        }

        [TearDown]
        public void TearDown()
        {
            ctx.EngineAdapter.CloseRunner();
        }
    }
}
