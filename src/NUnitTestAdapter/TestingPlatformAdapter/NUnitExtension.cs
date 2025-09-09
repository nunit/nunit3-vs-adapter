using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Testing.Platform.Extensions;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter
{
    internal sealed class NUnitExtension : IExtension
    {
        public string Uid => nameof(NUnitExtension);

        public string DisplayName => "NUnit";

        public string Version { get; } = GetAssemblyVersion();

        public string Description => "NUnit adapter for Microsoft Testing Platform";

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        private static string GetAssemblyVersion()
        {
            var assembly = typeof(NUnitExtension).Assembly;
            var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString();
            return version;
        }
    }
}
