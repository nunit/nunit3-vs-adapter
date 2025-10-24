using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.ExecutionProcesses;

public class VsTestExecution(IExecutionContext ctx) : Execution(ctx)
{
    public override bool Run(TestFilter filter, DiscoveryConverter discovery, NUnit3TestExecutor nUnit3TestExecutor)
    {
        filter = CheckVsTestFilter(filter, discovery, VsTestFilter);

        if (filter == NUnitTestFilterBuilder.NoTestsFound)
        {
            TestLog.Info("   Skipping assembly - no matching test cases found");
            return false;
        }
        return base.Run(filter, discovery, nUnit3TestExecutor);
    }

    public TestFilter CheckVsTestFilter(TestFilter filter, IDiscoveryConverter discovery, IVsTestFilter vsTestFilter)
    {
        // If we have a VSTest TestFilter, convert it to a NUnit filter
        if (vsTestFilter == null || vsTestFilter.IsEmpty)
            return filter;
        TestLog.Debug($"Microsoft Test Filter used, length: {vsTestFilter.MsTestCaseFilterExpression?.TestCaseFilterValue.Length}");
        // NOTE This overwrites filter used in call
        var filterBuilder = CreateTestFilterBuilder();
        filter = Settings.DiscoveryMethod == DiscoveryMethod.Current
            ? Settings.UseNUnitFilter
                ? filterBuilder.ConvertVsTestFilterToNUnitFilter(vsTestFilter)
                : filterBuilder.ConvertMsFilterToNUnitFilter(vsTestFilter, discovery)
            : filterBuilder.ConvertMsFilterToNUnitFilter(vsTestFilter, discovery.LoadedTestCases);

        Dump?.AddString($"\n\nMicrosoft Test Filter: {vsTestFilter.MsTestCaseFilterExpression?.TestCaseFilterValue}\n");
        Dump?.DumpVSInputFilter(filter, "(At Execution (Microsoft Test Filter)");

        return filter;
    }
    public override TestFilter CheckFilterInCurrentMode(TestFilter filter, IDiscoveryConverter discovery)
    {
        if (!discovery.IsDiscoveryMethodCurrent)
            return filter;
        if (filter != TestFilter.Empty)
        {
            filter = CheckFilter(filter, discovery);
        }
        else if (VsTestFilter is { IsEmpty: false } && !Settings.UseNUnitFilter)
        {
            var s = VsTestFilter.MsTestCaseFilterExpression.TestCaseFilterValue;
            var scount = s.Split('|', '&').Length;
            filter = CheckAssemblySelectLimit(filter, scount);
        }

        return filter;
    }

    private TestFilter CheckAssemblySelectLimit(TestFilter filter, int scount)
    {
        if (scount <= Settings.AssemblySelectLimit)
        {
            return filter;
        }
        TestLog.Debug("Setting filter to empty due to Test Filter size which is exceeding AssemblySelectLimit");
        return TestFilter.Empty;
    }
}