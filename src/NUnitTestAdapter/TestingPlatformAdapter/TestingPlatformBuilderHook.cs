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
#pragma warning disable SA1010 // Opening square brackets should be spaced correctly - FP from StyleCop
            testApplicationBuilder.AddNUnit(() => [Assembly.GetEntryAssembly()!]);
#pragma warning restore SA1010 // Opening square brackets should be spaced correctly
        }
    }
}
