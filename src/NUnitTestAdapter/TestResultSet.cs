using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter;

public struct TestResultSet
{
    public IList<TestResult> TestResults { get; set; }
    public TestResult TestCaseResult { get; set; }

    public string ConsoleOutput { get; set; }
}