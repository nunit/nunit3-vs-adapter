using NUnit.Engine;
using NUnit.Engine.Extensibility;

namespace Referencing_Mono_Cecil_0_10_0
{
    // Trigger Mono.Cecil binary break between older versions and 0.10.0
    // (test.addins points the engine to search all classes in this file and should result
    // in a runtime failure to cast 'Mono.Cecil.InterfaceImplementation' to 'Mono.Cecil.TypeReference'
    // if the engine is using the newer version of Mono.Cecil)
    [Extension]
    public sealed class TestNUnitEngineExtension : ITestEventListener
    {
        public void OnTestEvent(string report)
        {
        }
    }
}
