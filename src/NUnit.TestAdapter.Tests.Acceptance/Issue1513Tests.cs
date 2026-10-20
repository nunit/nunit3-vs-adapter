using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance;

/// <summary>
/// Issue #1513: with MTP, VS real-time discovery shows an extra "not run" node for every test in a
/// parameterized fixture. VS knows only the CLR type, so TestMethodIdentifierProperty.TypeName must not
/// contain the fixture arguments; they go in the display name instead. The bridge builds the trx className from the same TypeName, so when a
/// trx report is requested the fixture arguments are kept (#1351).
/// A data consumer registered in the test project dumps the property for every test node.
/// </summary>
public class Issue1513Tests : AcceptanceTests
{
    // Same MTP version as Microsoft.Testing.Extensions.VSTestBridge in Directory.Packages.props.
    private const string MicrosoftTestingPlatformVersion = "2.5.1";

    private static readonly string[] Fixtures = ["Fixture(\"A\")", "Fixture(\"B\")", "Fixture(\")\")"];

    private static readonly string[] PlainIdentifiers = ["Issue1513|Plain|Test|Test", "Issue1513|Plain|Versioned|case.version.1"];

    private const string TestCode = @"
using NUnit.Framework;

namespace Issue1513
{
    [TestFixture(""A"")]
    [TestFixture(""B"")]
    [TestFixture("")"")]
    public class Fixture
    {
        private readonly string _arg;
        public Fixture(string arg) => _arg = arg;

        [Test]
        public void NoParameter() => Assert.Pass();

        [TestCase(1)]
        public void WithParameter(int value) => Assert.Pass();

        [TestCase("")"")]
        public void WithParen(string value) => Assert.Pass();

        [TestCase(TestName = ""scenario.step"")]
        public void Dotted() => Assert.Pass();
    }

    public class Plain
    {
        [Test]
        public void Test() => Assert.Pass();

        [TestCase(TestName = ""case.version.1"")]
        public void Versioned() => Assert.Pass();
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

namespace Issue1513
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
            var node = ((TestNodeUpdateMessage)value).TestNode;
            var id = node.Properties.SingleOrDefault<TestMethodIdentifierProperty>();
            if (id != null)
            {
                lock (Lock)
                    File.AppendAllText(
                        Path.Combine(AppContext.BaseDirectory, ""identifiers.txt""),
                        id.Namespace + ""|"" + id.TypeName + ""|"" + id.MethodName + ""|"" + node.DisplayName + Environment.NewLine);
            }
            return Task.CompletedTask;
        }
    }
}
";

    [TestCaseSource(typeof(SingleFrameworkSource), nameof(SingleFrameworkSource.AllFrameworks))]
    public void WithoutTrx(SingleFrameworkSource source)
    {
        var workspace = CreateTestWorkspace(source);

        var identifiers = Run(workspace, source);

        var expected = Fixtures.SelectMany(fixture => new[]
        {
            $"Issue1513|Fixture|NoParameter|{fixture}.NoParameter",
            $"Issue1513|Fixture|WithParameter|{fixture}.WithParameter(1)",
            $"Issue1513|Fixture|WithParen|{fixture}.WithParen(\")\")",
            $"Issue1513|Fixture|Dotted|{fixture}.scenario.step"
        }).Concat(PlainIdentifiers);
        Assert.That(identifiers, Is.EquivalentTo(expected));
    }

    [TestCaseSource(typeof(SingleFrameworkSource), nameof(SingleFrameworkSource.AllFrameworks))]
    public void WithTrx(SingleFrameworkSource source)
    {
        var workspace = CreateTestWorkspace(source);
        var resultsDirectory = Path.Combine(workspace.Directory, "TestResults");

        var identifiers = Run(workspace, source, "--report-trx", "--results-directory", resultsDirectory);

        var expected = Fixtures.SelectMany(fixture => new[]
        {
            $"Issue1513|{fixture}|NoParameter|NoParameter",
            $"Issue1513|{fixture}|WithParameter|WithParameter(1)",
            $"Issue1513|{fixture}|WithParen|WithParen(\")\")",
            $"Issue1513|{fixture}|Dotted|scenario.step"
        }).Concat(PlainIdentifiers);
        Assert.That(identifiers, Is.EquivalentTo(expected));

        var trxFile = Directory.GetFiles(resultsDirectory, "*.trx").Single();
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var classNames = XDocument.Load(trxFile)
            .Descendants(ns + "TestMethod")
            .Select(m => (string)m.Attribute("className"))
            .Distinct()
            .ToArray();
        Assert.That(classNames, Is.EquivalentTo(Fixtures.Select(f => "Issue1513." + f).Concat(["Issue1513.Plain"])));
    }

    private static IsolatedWorkspace CreateTestWorkspace(SingleFrameworkSource source)
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
                    <PackageReference Include='Microsoft.Testing.Extensions.TrxReport' Version='{MicrosoftTestingPlatformVersion}' />
                  </ItemGroup>
                  <ItemGroup>
                    <TestingPlatformBuilderHook Include='1513D0D0-0000-4000-8000-000000001513'>
                      <DisplayName>IdentifierDump</DisplayName>
                      <TypeFullName>Issue1513.IdentifierDumpHook</TypeFullName>
                    </TestingPlatformBuilderHook>
                  </ItemGroup>
                </Project>")
            .AddFile("Tests.cs", TestCode)
            .AddFile("IdentifierDump.cs", DumpExtensionCode);

        workspace.MsBuild(restore: true);
        return workspace;
    }

    private static string[] Run(IsolatedWorkspace workspace, SingleFrameworkSource source, params string[] arguments)
    {
        var result = ProcessUtils.Run(workspace.Directory, "dotnet", ["run", "--no-build", "--", .. arguments]);
        TestContext.Out.WriteLine(result.StdOut);
        result.ThrowIfError();

        var dumpFile = Path.Combine(workspace.Directory, "bin", "Debug", source.Framework, "identifiers.txt");
        Assert.That(dumpFile, Does.Exist, "Data consumer saw no TestMethodIdentifierProperty");
        return File.ReadAllLines(dumpFile).Distinct().ToArray();
    }
}
