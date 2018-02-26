using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Metadata;
using System;
using System.Reflection;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public static class NavigationDataProviderTests
    {
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
