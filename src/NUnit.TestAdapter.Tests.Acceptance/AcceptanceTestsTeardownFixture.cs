using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    // https://github.com/nunit/nunit/issues/3166
    [SetUpFixture]
    public sealed class AcceptanceTestsTeardownFixture
    {
        [OneTimeTearDown]
        public static void OneTimeTearDown()
        {
            AcceptanceTests.OnGlobalTeardown();
        }
    }
}
