// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Metadata;
using System;
using System.IO;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public static class NavigationDataProviderTests
    {
#if NET46
        [Test(Description =
            "This simulates what happens when the adapter is deployed: " +
            "nunit.framework.dll is no longer beside the adapter assembly " +
            "and we need to make sure the reflection AppDomain wouldn’t fail " +
            "to load it.")]
        public static void AsyncMethodWithAttributeDefinedOutsideAdapterDirectory()
        {
            using (var dir = new TempDirectory())
            {
                // The tests must run in the same AppDomain as VSTest for DiaSession to work,
                // but that VSTest has already loaded an old version of S.C.Immutable in this AppDomain.
                // To avoid MissingMethodException, it’s necessary to only deal with Roslyn in a separate AppDomain.
                using (var compileInvoker = new AppDomainInvoker())
                {
                    compileInvoker.Invoke(outputDir =>
                    {
                        var baseCompilation = CSharpCompilation.Create(null)
                            .AddReferences(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
                            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                        var dependencyAssemblyPath = Path.Combine(outputDir, "AssemblyOutsideAdapterDir.dll");
                        var dependencyAssembly = baseCompilation
                            .WithAssemblyName("AssemblyOutsideAdapterDir")
                            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public sealed class AttributeDefinedOutsideAdapterDir : System.Attribute { }"))
                            .Emit(Path.Combine(outputDir, "AssemblyOutsideAdapterDir.dll"));
                        if (!dependencyAssembly.Success) Assert.Fail("Broken test");

                        using (var outputDll = File.Create(Path.Combine(outputDir, "DependentAssembly.dll")))
                        using (var outputPdb = File.Create(Path.Combine(outputDir, "DependentAssembly.pdb")))
                        {
                            var dependentAssembly = baseCompilation
                                .WithAssemblyName("DependentAssembly")
                                .AddReferences(MetadataReference.CreateFromFile(dependencyAssemblyPath))
                                .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class TestClass { [AttributeDefinedOutsideAdapterDir] public async System.Threading.Tasks.Task AsyncMethod() { } }"))
                                .Emit(outputDll, outputPdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                            if (!dependentAssembly.Success) Assert.Fail("Broken test");
                        }
                    }, dir.Path);
                }

                var assemblyPath = Path.Combine(dir, "DependentAssembly.dll");
                using (var metadataProvider = new ReflectionAppDomainMetadataProvider(applicationBase: Path.GetDirectoryName(assemblyPath)))
                {
                    var result = metadataProvider.GetStateMachineType(assemblyPath, "TestClass", "AsyncMethod");
                    Assert.That(result, Is.Not.Null);
                }
            }
        }
#endif

        [Test]
        public static void ExceptionShouldDisableGetStateMachineTypeAndLogErrorForAssembly()
        {
            var fixture = new Fixture();
            fixture.MetadataProvider.GetStateMachineType(null, null, null).ReturnsForAnyArgs(_ => throw new Exception());

            fixture.CauseLookupFailure();

            fixture.MetadataProvider.ReceivedWithAnyArgs(requiredNumberOfCalls: 1).GetStateMachineType(null, null, null);
            fixture.AssertLoggerReceivedErrorForAssemblyPath();
        }

        [Test]
        public static void ExceptionShouldDisableGetDeclaringTypeAndLogErrorForAssembly()
        {
            var fixture = new Fixture();
            fixture.MetadataProvider.GetDeclaringType(null, null, null).ReturnsForAnyArgs(_ => throw new Exception());

            fixture.CauseLookupFailure();

            fixture.MetadataProvider.ReceivedWithAnyArgs(requiredNumberOfCalls: 1).GetDeclaringType(null, null, null);
            fixture.AssertLoggerReceivedErrorForAssemblyPath();
        }

        private sealed class Fixture
        {
            public ITestLogger Logger { get; } = Substitute.For<ITestLogger>();
            public IMetadataProvider MetadataProvider { get; } = Substitute.For<IMetadataProvider>();
            private readonly string _existingAssemblyPath = typeof(NavigationDataProviderTests).GetTypeInfo().Assembly.Location;

            public void CauseLookupFailure()
            {
                using (var navigationProvider = new NavigationDataProvider(_existingAssemblyPath, Logger, MetadataProvider))
                {
                    navigationProvider.GetNavigationData("NonExistentFixture", "SomeTest");
                    navigationProvider.GetNavigationData("NonExistentFixture", "SomeTest");
                }
            }

            public void AssertLoggerReceivedErrorForAssemblyPath()
            {
                Logger.Received().Warning(Arg.Is<string>(message => message.Contains(_existingAssemblyPath)), Arg.Any<Exception>());
            }
        }
    }
}
