using System;

using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;

using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.ExecutionProcesses;

public abstract class Execution(IExecutionContext ctx)
{
    private const string ExcludeExplicitTests = "<not><prop name='Explicit'>true</prop></not>";
    protected string TestOutputXmlFolder => ctx.TestOutputXmlFolder;
    protected ITestLogger TestLog => ctx.Log;
    protected IAdapterSettings Settings => ctx.Settings;

    protected IDumpXml Dump => ctx.Dump;
    protected IVsTestFilter VsTestFilter => ctx.VsTestFilter;

    protected INUnitEngineAdapter NUnitEngineAdapter => ctx.EngineAdapter;


    public virtual bool Run(TestFilter filter, DiscoveryConverter discovery, NUnit3TestExecutor nUnit3TestExecutor)
    {
        if (nUnit3TestExecutor.IsCancelled)
        {
            TestLog.Debug("Execution cancelled before engine run");
            return false;
        }

        filter = CheckFilterInCurrentMode(filter, discovery);
        nUnit3TestExecutor.Dump?.StartExecution(filter, "(At Execution)");

        if (nUnit3TestExecutor.IsCancelled)
        {
            TestLog.Debug("Execution cancelled after filter check");
            return false;
        }

        var converter = CreateConverter(discovery);
        using var listener = new NUnitEventListener(converter, nUnit3TestExecutor);

        if (nUnit3TestExecutor.IsCancelled)
        {
            TestLog.Debug("Execution cancelled before engine run start");
            return false;
        }

        // CRITICAL: Dump immediately before engine call to see if we get stuck here
        nUnit3TestExecutor.LogToDump("AboutToCallEngineRun", "Engine.Run() about to be called");
        TestLog.Debug("About to call engine Run - dump written");

        try
        {
            TestLog.Debug("About to call NUnitEngineAdapter.Run()");
            var results = NUnitEngineAdapter.Run(listener, filter);
            TestLog.Debug("NUnitEngineAdapter.Run() completed");

            // If we get here, the engine returned - dump this fact
            nUnit3TestExecutor.LogToDump("EngineRunCompleted", "Engine.Run() completed successfully");

            if (nUnit3TestExecutor.IsCancelled)
            {
                TestLog.Debug("Execution was cancelled, skipping test output generation");
                nUnit3TestExecutor.LogToDump("SkippedTestOutput", "Test output generation skipped due to cancellation");
            }
            else
            {
                TestLog.Debug("About to generate test output");
                nUnit3TestExecutor.LogToDump("AboutToGenerateTestOutput", "Starting test output generation");

                NUnitEngineAdapter.GenerateTestOutput(results, discovery.AssemblyPath, TestOutputXmlFolder);

                TestLog.Debug("Test output generation completed");
                nUnit3TestExecutor.LogToDump("TestOutputCompleted", "Test output generation completed successfully");
            }
        }
        catch (NullReferenceException)
        {
            // this happens during the run when CancelRun is called.
            TestLog.Debug("   Null ref caught - likely due to cancellation");
            nUnit3TestExecutor.LogToDump("NullRefException", "Null reference exception caught - likely due to cancellation");
        }

        return true;
    }

    public abstract TestFilter CheckFilterInCurrentMode(TestFilter filter, IDiscoveryConverter discovery);

    protected NUnitTestFilterBuilder CreateTestFilterBuilder()
        => new(NUnitEngineAdapter.GetService<ITestFilterService>(), Settings);
    protected ITestConverterCommon CreateConverter(DiscoveryConverter discovery) => Settings.DiscoveryMethod == DiscoveryMethod.Current ? discovery.TestConverter : discovery.TestConverterForXml;

    protected TestFilter CheckFilter(TestFilter testFilter, IDiscoveryConverter discovery)
    {
        if (discovery.NoOfLoadedTestCasesAboveLimit && !testFilter.IsCategoryFilter())
        {
            TestLog.Debug("Setting filter to empty due to number of testcases");
            var filter = TestFilter.Empty;
            return filter;
        }
        if (discovery.NoOfLoadedTestCases == 0)
            return testFilter;
        if (testFilter.IsCategoryFilter())
        {
            if (!discovery.IsExplicitRun && discovery.HasExplicitTests && Settings.ExplicitMode is ExplicitModeEnum.Strict)
            {
                var filterExt = new TestFilter(ExcludeExplicitTests);
                var combiner = new TestFilterCombiner(testFilter, filterExt);
                return combiner.GetFilter();
            }

            if (discovery.IsExplicitRun && Settings.ExplicitMode is ExplicitModeEnum.None)
            {
                var filterExt = new TestFilter(ExcludeExplicitTests);
                var combiner = new TestFilterCombiner(testFilter, filterExt);
                return combiner.GetFilter();
            }

            return testFilter;
        }

        if (testFilter.IsPartitionFilter())
        {
            return testFilter;
        }
        var filterBuilder = CreateTestFilterBuilder();
        if (discovery.HasExplicitTests && Settings.ExplicitMode == ExplicitModeEnum.None)
        {
            return filterBuilder.FilterByList(discovery.GetLoadedNonExplicitTestCases());
        }
        return filterBuilder.FilterByList(discovery.LoadedTestCases);
    }
}