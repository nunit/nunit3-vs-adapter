using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.ExecutionProcesses;

public interface IExecutionContext
{
    ITestLogger Log { get; }
    INUnitEngineAdapter EngineAdapter { get; }
    string TestOutputXmlFolder { get; }
    IAdapterSettings Settings { get; }
    IDumpXml Dump { get; }

    IVsTestFilter VsTestFilter { get; }
}