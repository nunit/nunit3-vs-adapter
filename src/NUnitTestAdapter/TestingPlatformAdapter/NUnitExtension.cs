using System.Threading.Tasks;

using Microsoft.Testing.Platform.Extensions;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    internal sealed class NUnitExtension : IExtension
    {
        public string Uid => nameof(NUnitExtension);

        public string DisplayName => "NUnit";

        // TODO: Decide whether to read from assembly or use hardcoded string.
        public string Version => "4.5.0";

        public string Description => "NUnit adapter for Microsoft Testing Platform";

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }
}
