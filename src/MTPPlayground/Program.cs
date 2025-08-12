using System.Reflection;

using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

ITestApplicationBuilder testApplicationBuilder = await TestApplication.CreateBuilderAsync(args);

testApplicationBuilder.AddNUnit(() => [Assembly.GetEntryAssembly()!]);
testApplicationBuilder.AddTrxReportProvider();
testApplicationBuilder.AddAppInsightsTelemetryProvider();
using ITestApplication testApplication = await testApplicationBuilder.BuildAsync();
return await testApplication.RunAsync();

[TestFixture]
public class TestClass
{
    [Test]
    public void TestMethod()
    {
        Assert.Fail("Failing");
    }
}
