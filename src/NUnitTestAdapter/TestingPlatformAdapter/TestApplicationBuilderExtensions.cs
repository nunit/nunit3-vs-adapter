using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Testing.Extensions.VSTestBridge.Capabilities;
using Microsoft.Testing.Extensions.VSTestBridge.Helpers;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    public static class TestApplicationBuilderExtensions
    {
        public static void AddNUnit(this ITestApplicationBuilder testApplicationBuilder, Func<IEnumerable<Assembly>> getTestAssemblies)
        {
            NUnitExtension extension = new();

            testApplicationBuilder.AddRunSettingsService(extension);
            testApplicationBuilder.AddTestCaseFilterService(extension);
            testApplicationBuilder.AddTestRunParametersService(extension);
            testApplicationBuilder.AddRunSettingsEnvironmentVariableProvider(extension);

            testApplicationBuilder.RegisterTestFramework(
                _ => new TestFrameworkCapabilities(new VSTestBridgeExtensionBaseCapabilities()),
                (capabilities, serviceProvider) => new NUnitBridgedTestFramework(extension, getTestAssemblies, serviceProvider, capabilities));
        }
    }
}
