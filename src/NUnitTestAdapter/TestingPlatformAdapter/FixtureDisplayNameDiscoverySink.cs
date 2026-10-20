using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

/// <summary>
/// Applies <see cref="FixtureDisplayName"/> to every discovered test case before the bridge sees it.
/// </summary>
internal sealed class FixtureDisplayNameDiscoverySink(ITestCaseDiscoverySink inner, FixtureDisplayName displayName) : ITestCaseDiscoverySink
{
    public void SendTestCase(TestCase discoveredTest)
    {
        displayName.Apply(discoveredTest);
        inner.SendTestCase(discoveredTest);
    }
}
