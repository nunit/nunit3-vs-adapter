using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter;

public interface IAdapterSettings
{
    int MaxCpuCount { get; }
    string ResultsDirectory { get; }
    string TargetPlatform { get; }
    string TargetFrameworkVersion { get; }
    string TestAdapterPaths { get; }
    bool CollectSourceInformation { get; }
    IDictionary<string, string> TestProperties { get; }
    InternalTraceLevel InternalTraceLevelEnum { get; }
    string WorkDirectory { get; }
    string Where { get; }
    int DefaultTimeout { get; }
    int NumberOfTestWorkers { get; }
    bool ShadowCopyFiles { get; }
    int Verbosity { get; }
    bool UseVsKeepEngineRunning { get; }
    string BasePath { get; }
    string PrivateBinPath { get; }
    int? RandomSeed { get; }
    bool RandomSeedSpecified { get; }
    bool InProcDataCollectorsAvailable { get; }
    // ReSharper disable once UnusedMemberInSuper.Global
    bool CollectDataForEachTestSeparately { get; }  // Used implicitly by MS
    bool SynchronousEvents { get; }
    string DomainUsage { get; }
    bool DumpXmlTestDiscovery { get; }
    bool DumpXmlTestResults { get; }

    bool DumpVsInput { get; }

    bool PreFilter { get; }

    /// <summary>
    ///  Syntax documentation <see cref="https://github.com/nunit/docs/wiki/Template-Based-Test-Naming"/>.
    /// </summary>
    string DefaultTestNamePattern { get; }

    VsTestCategoryType VsTestCategoryType { get; }
    string TestOutputXml { get; }
    string TestOutputXmlFileName { get; }
    bool UseTestOutputXml { get; }
    OutputXmlFolderMode OutputXmlFolderMode { get; }

    /// <summary>
    /// For retry runs create a new file for each run.
    /// </summary>
    bool NewOutputXmlFileForEachRun { get; }

    /// <summary>
    /// True if test run is triggered in an IDE/Editor context.
    /// </summary>
    bool DesignMode { get; }

    /// <summary>
    /// If true, an adapter shouldn't create appdomains to run tests.
    /// </summary>
    bool DisableAppDomain { get; }

    /// <summary>
    /// If true, an adapter should disable any test case parallelization.
    /// </summary>
    bool DisableParallelization { get; }

    /// <summary>
    /// Default is that when the adapter notice it is running with a debugger attached it will disable parallelization.
    /// By changing this setting to `true` the adapter will allow parallelization even if a debugger is attached.
    /// </summary>
    bool AllowParallelWithDebugger { get; }

    bool ShowInternalProperties { get; }

    bool UseParentFQNForParametrizedTests { get; }

    bool UseNUnitIdforTestCaseId { get; }

    int ConsoleOut { get; }
    bool StopOnError { get; }
    TestOutcome MapWarningTo { get; }
    bool UseTestNameInConsoleOutput { get; }
    DisplayNameOptions DisplayName { get; }
    char FullnameSeparator { get; }
    DiscoveryMethod DiscoveryMethod { get; }
    bool SkipNonTestAssemblies { get; }

    int AssemblySelectLimit { get; }

    bool UseNUnitFilter { get; }
    bool IncludeStackTraceForSuites { get; }


    void Load(IDiscoveryContext context, TestLogger testLogger = null);
    void Load(string settingsXml);
    void SaveRandomSeed(string dirname);
    void RestoreRandomSeed(string dirname);

    bool EnsureAttachmentFileScheme { get; }

    // For Internal Development use
    bool FreakMode { get; }  // displays metadata instead of real data in Test Explorer
    bool Debug { get; }
    bool DebugExecution { get; }
    bool DebugDiscovery { get; }

    // Filter control
    ExplicitModeEnum ExplicitMode { get; }
    bool SkipExecutionWhenNoTests { get; }
    string TestOutputFolder { get; }
    string SetTestOutputFolder(string workDirectory);
}