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
        filter = CheckFilterInCurrentMode(filter, discovery);
        nUnit3TestExecutor.Dump?.StartExecution(filter, "(At Execution)");
        var converter = CreateConverter(discovery);
        using var listener = new NUnitEventListener(converter, nUnit3TestExecutor);
        try
        {
            var results = NUnitEngineAdapter.Run(listener, filter);
            NUnitEngineAdapter.GenerateTestOutput(results, discovery.AssemblyPath, TestOutputXmlFolder);
        }
        catch (NullReferenceException)
        {
            // this happens during the run when CancelRun is called.
            TestLog.Debug("   Null ref caught");
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