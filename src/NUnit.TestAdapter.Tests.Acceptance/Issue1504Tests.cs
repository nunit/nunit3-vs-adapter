using System.IO;
using System.Linq;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Issue #1504: with MTP, VS real-time discovery shows an extra "not run" node for tests with
/// nullable or constructed generic parameters. The TestMethodIdentifierProperty published by the
/// adapter must use managed-name encoding for parameter types (angle-bracket generic arguments),
/// otherwise VS cannot match it against the RTD entry.
/// A data consumer registered in the test project dumps the property for every test node.
/// </summary>
public class Issue1504Tests : AcceptanceTests
{
    private const string TestCode = @"
using System.Collections.Generic;
using NUnit.Framework;

namespace Issue1504
{
    public class Tests
    {
        [TestCase(1.5)]
        public void PlainDecimal(decimal value) => Assert.Pass();

        [TestCase(null)]
        [TestCase(1.5)]
        public void NullableDecimal(decimal? value) => Assert.Pass();

        [TestCase(null)]
        [TestCase(true)]
        public void NullableBool(bool? value) => Assert.Pass();

        [TestCaseSource(nameof(Lists))]
        public void GenericList(List<int> value) => Assert.Pass();

        public static IEnumerable<List<int>> Lists() { yield return new List<int> { 1 }; }
    }
}
";

    private const string DumpExtensionCode = @"
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;

namespace Issue1504
{
    public static class IdentifierDumpHook
    {
        public static void AddExtensions(ITestApplicationBuilder builder, string[] args)
            => builder.TestHost.AddDataConsumer(_ => new IdentifierDumpConsumer());
    }

    public sealed class IdentifierDumpConsumer : IDataConsumer
    {
        private static readonly object Lock = new object();
        public Type[] DataTypesConsumed => new[] { typeof(TestNodeUpdateMessage) };
        public string Uid => nameof(IdentifierDumpConsumer);
        public string Version => ""1.0.0"";
        public string DisplayName => nameof(IdentifierDumpConsumer);
        public string Description => nameof(IdentifierDumpConsumer);
        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Task ConsumeAsync(IDataProducer dataProducer, IData value, CancellationToken cancellationToken)
        {
            var id = ((TestNodeUpdateMessage)value).TestNode.Properties.SingleOrDefault<TestMethodIdentifierProperty>();
            if (id != null)
            {
                lock (Lock)
                    File.AppendAllText(
                        Path.Combine(AppContext.BaseDirectory, ""identifiers.txt""),
                        id.MethodName + ""|"" + string.Join("";"", id.ParameterTypeFullNames) + Environment.NewLine);
            }
            return Task.CompletedTask;
        }
    }
}
";

    [TestCaseSource(typeof(SingleFrameworkSource), nameof(SingleFrameworkSource.AllFrameworks))]
    public void ManagedNames(SingleFrameworkSource source)
    {
        var workspace = CreateWorkspace()
            .AddProject("Test.csproj", $@"
                <Project Sdk='Microsoft.NET.Sdk'>
                  <PropertyGroup>
                    <TargetFramework>{source.Framework}</TargetFramework>
                    <OutputType>Exe</OutputType>
                    <EnableNUnitRunner>true</EnableNUnitRunner>
                    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include='NUnit' Version='{source.NUnitVersion}' />
                    <PackageReference Include='NUnit3TestAdapter' Version='{NuGetPackageVersion}' />
                  </ItemGroup>
                  <ItemGroup>
                    <TestingPlatformBuilderHook Include='1504D0D0-0000-4000-8000-000000001504'>
                      <DisplayName>IdentifierDump</DisplayName>
                      <TypeFullName>Issue1504.IdentifierDumpHook</TypeFullName>
                    </TestingPlatformBuilderHook>
                  </ItemGroup>
                </Project>")
            .AddFile("Tests.cs", TestCode)
            .AddFile("IdentifierDump.cs", DumpExtensionCode);

        workspace.MsBuild(restore: true);

        var result = ProcessUtils.Run(workspace.Directory, "dotnet", ["run", "--no-build"]);
        TestContext.Out.WriteLine(result.StdOut);

        var dumpFile = Path.Combine(workspace.Directory, "bin", "Debug", source.Framework, "identifiers.txt");
        Assert.That(dumpFile, Does.Exist, "Data consumer saw no TestMethodIdentifierProperty");
        var identifiers = File.ReadAllLines(dumpFile).Distinct().ToArray();

        Assert.That(identifiers, Is.EquivalentTo([
            "PlainDecimal|System.Decimal",
            "NullableDecimal|System.Nullable`1<System.Decimal>",
            "NullableBool|System.Nullable`1<System.Boolean>",
            "GenericList|System.Collections.Generic.List`1<System.Int32>"
        ]));
    }
}
