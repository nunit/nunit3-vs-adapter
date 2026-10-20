using System;
using System.Collections.Generic;
using System.Reflection;

using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Extensions.VSTestBridge.Helpers;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    public static class TestApplicationBuilderExtensions
    {
        // We intentionally do NOT use VSTestBridgeExtensionBaseCapabilities because that type also implements
        // INamedFeatureCapability and advertises the "vstestProvider" capability. When a server (this adapter)
        // advertises vstestProvider, Visual Studio Test Explorer consumes the legacy "vstest.TestCase.*"
        // properties (serialized through Microsoft.Testing.Platform's internal SerializableKeyValuePairStringProperty
        // key/value bag) instead of the public, structured properties.
        //
        // NUnitBridgedTestFramework.AddAdditionalProperties already emits the public TestMethodIdentifierProperty
        // (serialized as location.type / location.method / location.method-arity), and location.file /
        // location.line-start are emitted by the bridge from the TestCase. By not advertising vstestProvider we let
        // the IDE consume those public properties and we stop depending on the internal key/value-pair shape.
        //
        // This mirrors what MSTest does with its own MSTestCapabilities type:
        // https://github.com/microsoft/testfx/blob/main/src/Adapter/MSTest.TestAdapter/TestingPlatformAdapter/TestApplicationBuilderExtensions.cs
        // MSTest can implement the internal IInternalVSTestBridgeTrxReportCapability (it is on the bridge's
        // InternalsVisibleTo list); external adapters cannot, so we implement the public ITrxReportCapability here.
        internal sealed class NUnitTestFrameworkCapabilities : ITrxReportCapability
        {
            /// <summary>
            /// True when a trx report was requested (e.g. --report-trx). The bridge always adds the trx properties,
            /// so this only tells NUnitBridgedTestFramework which class name to put in TestMethodIdentifierProperty.
            /// </summary>
            public bool IsTrxReportEnabled { get; private set; }

            // Returning true advertises that NUnit can enrich nodes with the trx-report properties; the platform's
            // TrxDataConsumer checks IsSupported and then calls Enable() only when a trx report was requested.
            bool ITrxReportCapability.IsSupported => true;

            void ITrxReportCapability.Enable() => IsTrxReportEnabled = true;
        }

        public static void AddNUnit(this ITestApplicationBuilder testApplicationBuilder, Func<IEnumerable<Assembly>> getTestAssemblies)
        {
            NUnitExtension extension = new();

            testApplicationBuilder.AddRunSettingsService(extension);
            testApplicationBuilder.AddTestCaseFilterService(extension);
            testApplicationBuilder.AddTestRunParametersService(extension);
            testApplicationBuilder.AddRunSettingsEnvironmentVariableProvider(extension);

            NUnitTestFrameworkCapabilities nunitCapabilities = new();

            testApplicationBuilder.RegisterTestFramework(
                _ => new TestFrameworkCapabilities(nunitCapabilities),
                (capabilities, serviceProvider) => new NUnitBridgedTestFramework(extension, getTestAssemblies, serviceProvider, capabilities, nunitCapabilities));
        }
    }
}
