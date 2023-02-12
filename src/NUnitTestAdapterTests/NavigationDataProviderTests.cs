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

using System;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Metadata;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public static class NavigationDataProviderTests
    {
#if NET462
        [Ignore("Joseph: This doesn't really work!")]
        [Description(
            "This simulates what happens when the adapter is deployed: " +
            "nunit.framework.dll is no longer beside the adapter assembly " +
            "and we need to make sure the reflection AppDomain wouldn’t fail " +
            "to load it.")]
        [TestCase(false, TestName = "AsyncMethodWithAttributeDefinedOutsideAdapterDirectory()")]
        [TestCase(true, TestName = "AsyncMethodWithAttributeDefinedOutsideAdapterDirectory(with binding redirect)")]
        public static void AsyncMethodWithAttributeDefinedOutsideAdapterDirectory(bool withBindingRedirect)
        {
            using (var dir = new TempDirectory())
            {
                // The tests must run in the same AppDomain as VSTest for DiaSession to work,
                // but that VSTest has already loaded an old version of S.C.Immutable in this AppDomain.
                // To avoid MissingMethodException, it’s necessary to only deal with Roslyn in a separate AppDomain.
                using (var compileInvoker = new AppDomainInvoker())
                {
                    compileInvoker.Invoke(
                        marshalled =>
                    {
                        var baseCompilation = CSharpCompilation.Create(null)
                            .AddReferences(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
                            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                        var dependencyAssemblyPath = Path.Combine(marshalled.outputDir, "AssemblyOutsideAdapterDir.dll");
                        var dependencyBaseCompilation = baseCompilation
                            .WithAssemblyName("AssemblyOutsideAdapterDir")
                            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public sealed class AttributeDefinedOutsideAdapterDir : System.Attribute { }"));

                        if (marshalled.withBindingRedirect)
                        {
                            dependencyBaseCompilation = dependencyBaseCompilation
                                .WithOptions(baseCompilation.Options
                                    .WithPublicSign(true)
                                    .WithCryptoKeyFile(Path.Combine(Path.GetDirectoryName(typeof(NavigationDataProviderTests).Assembly.Location), "temp.snk")));
                        }

                        var dependencyAssembly = dependencyBaseCompilation
                            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("[assembly: System.Reflection.AssemblyVersion(\"1.0.1.0\")]"))
                            .Emit(Path.Combine(marshalled.outputDir, "AssemblyOutsideAdapterDir.dll"));
                        if (!dependencyAssembly.Success) Assert.Fail("Broken test");

                        MetadataReference reference;
                        if (marshalled.withBindingRedirect)
                        {
                            reference = dependencyBaseCompilation
                                .AddSyntaxTrees(CSharpSyntaxTree.ParseText("[assembly: System.Reflection.AssemblyVersion(\"1.0.0.0\")]"))
                                .ToMetadataReference();
                        }
                        else
                        {
                            reference = MetadataReference.CreateFromFile(dependencyAssemblyPath);
                        }

                        using (var outputDll = File.Create(Path.Combine(marshalled.outputDir, "DependentAssembly.dll")))
                        using (var outputPdb = File.Create(Path.Combine(marshalled.outputDir, "DependentAssembly.pdb")))
                        {
                            var dependentAssembly = baseCompilation
                                .WithAssemblyName("DependentAssembly")
                                .AddReferences(reference)
                                .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class TestClass { [AttributeDefinedOutsideAdapterDir] public async System.Threading.Tasks.Task AsyncMethod() { } }"))
                                .Emit(outputDll, outputPdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                            if (!dependentAssembly.Success) Assert.Fail("Broken test");
                        }
                    }, (outputDir: dir.Path, withBindingRedirect));
                }

                var assemblyPath = Path.Combine(dir, "DependentAssembly.dll");
                if (withBindingRedirect)
                {
                    File.WriteAllText(
                        assemblyPath + ".config",
                        @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <runtime>
    <assemblyBinding xmlns=""urn:schemas-microsoft-com:asm.v1"">
      <dependentAssembly>
        <assemblyIdentity name=""AssemblyOutsideAdapterDir"" publicKeyToken=""0bf10b92861e5519"" culture=""neutral"" />
        <bindingRedirect oldVersion=""0.0.0.0-1.0.1.0"" newVersion=""1.0.1.0"" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>");
                }

                using (var metadataProvider = NavigationDataProvider.CreateMetadataProvider(assemblyPath))
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
            private ITestLogger Logger { get; } = Substitute.For<ITestLogger>();
            public IMetadataProvider MetadataProvider { get; } = Substitute.For<IMetadataProvider>();
            private readonly string existingAssemblyPath = typeof(NavigationDataProviderTests).GetTypeInfo().Assembly.Location;

            public void CauseLookupFailure()
            {
                using var navigationProvider = new NavigationDataProvider(existingAssemblyPath, Logger, MetadataProvider);
                navigationProvider.GetNavigationData("NonExistentFixture", "SomeTest");
                navigationProvider.GetNavigationData("NonExistentFixture", "SomeTest");
            }

            public void AssertLoggerReceivedErrorForAssemblyPath()
            {
                Logger.Received().Warning(Arg.Is<string>(message => message.Contains(existingAssemblyPath)), Arg.Any<Exception>());
            }
        }
    }
}
