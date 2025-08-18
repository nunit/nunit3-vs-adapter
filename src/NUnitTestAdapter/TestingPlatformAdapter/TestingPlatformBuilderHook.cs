using System.Reflection;

using Microsoft.Testing.Platform.Builder;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    public static class TestingPlatformBuilderHook
    {
#pragma warning disable IDE0060 // Remove unused parameter - Method signature is expected by Microsoft.Testing.Platform.MSBuild
        public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] arguments)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            testApplicationBuilder.AddNUnit(() => [Assembly.GetEntryAssembly()!]);
        }
    }
}
