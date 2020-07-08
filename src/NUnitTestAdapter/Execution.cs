using System;
using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter
{
    public interface IExecutionContext
    {
        ITestLogger Log { get; }
        INUnitEngineAdapter EngineAdapter { get; }
        string TestOutputXmlFolder { get; }
        IAdapterSettings Settings { get; }
        IDumpXml Dump { get; }

        IVsTestFilter VsTestFilter { get; }
    }

    public static class ExecutionFactory
    {
        public static Execution Create(IExecutionContext ctx)
        {
            if (ctx.Settings.DesignMode) // We come from IDE
                return new IdeExecution(ctx);
            return new VsTestExecution(ctx);
        }
    }

    public abstract class Execution
    {
        protected string TestOutputXmlFolder => ctx.TestOutputXmlFolder;
        private readonly IExecutionContext ctx;
        protected ITestLogger TestLog => ctx.Log;
        protected IAdapterSettings Settings => ctx.Settings;

        protected IDumpXml Dump => ctx.Dump;
        protected IVsTestFilter VsTestFilter => ctx.VsTestFilter;

        protected INUnitEngineAdapter NUnitEngineAdapter => ctx.EngineAdapter;
        protected Execution(IExecutionContext ctx)
        {
            this.ctx = ctx;
        }



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
            => new NUnitTestFilterBuilder(NUnitEngineAdapter.GetService<ITestFilterService>(), Settings);
        protected ITestConverterCommon CreateConverter(DiscoveryConverter discovery) => Settings.DiscoveryMethod == DiscoveryMethod.Modern ? discovery.TestConverter : discovery.TestConverterForXml;
    }

    public class IdeExecution : Execution
    {
        public IdeExecution(IExecutionContext ctx) : base(ctx)
        {
        }
        public override bool Run(TestFilter filter, DiscoveryConverter discovery, NUnit3TestExecutor nUnit3TestExecutor)
        {
            return base.Run(filter, discovery, nUnit3TestExecutor);
        }

        public override TestFilter CheckFilterInCurrentMode(TestFilter filter, IDiscoveryConverter discovery)
        {
            if (!discovery.IsDiscoveryMethodCurrent)
                return filter;
            if (filter.IsEmpty())
                return filter;
            if (discovery.NoOfLoadedTestCasesAboveLimit)
            {
                TestLog.Debug("Setting filter to empty due to number of testcases");
                filter = TestFilter.Empty;
            }
            else
            {
                var filterBuilder = CreateTestFilterBuilder();
                filter = filterBuilder.FilterByList(discovery.LoadedTestCases);
            }
            return filter;
        }
    }

    public class VsTestExecution : Execution
    {
        public VsTestExecution(IExecutionContext ctx) : base(ctx)
        {
        }

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
            // If we have a VSTest TestFilter, convert it to an nunit filter
            if (vsTestFilter == null || vsTestFilter.IsEmpty)
                return filter;
            TestLog.Debug(
                $"TfsFilter used, length: {vsTestFilter.TfsTestCaseFilterExpression?.TestCaseFilterValue.Length}");
            // NOTE This overwrites filter used in call
            var filterBuilder = CreateTestFilterBuilder();
            if (Settings.DiscoveryMethod == DiscoveryMethod.Modern)
            {
                filter = filterBuilder.ConvertTfsFilterToNUnitFilter(vsTestFilter, discovery.LoadedTestCases);
            }
            else
            {
                filter = filterBuilder.ConvertTfsFilterToNUnitFilter(vsTestFilter, discovery.LoadedTestCases);
            }

            Dump?.AddString($"\n\nTFSFilter: {vsTestFilter.TfsTestCaseFilterExpression.TestCaseFilterValue}\n");
            Dump?.DumpVSInputFilter(filter, "(At Execution (TfsFilter)");

            return filter;
        }
        public override TestFilter CheckFilterInCurrentMode(TestFilter filter, IDiscoveryConverter discovery)
        {
            if (!discovery.IsDiscoveryMethodCurrent)
                return filter;
            if ((VsTestFilter == null || VsTestFilter.IsEmpty) && filter != TestFilter.Empty)
            {
                if (discovery.NoOfLoadedTestCasesAboveLimit)
                {
                    TestLog.Debug("Setting filter to empty due to number of testcases");
                    filter = TestFilter.Empty;
                }
                else
                {
                    var filterBuilder = CreateTestFilterBuilder();
                    filter = filterBuilder.FilterByList(discovery.LoadedTestCases);
                }
            }
            else if (VsTestFilter != null && !VsTestFilter.IsEmpty)
            {
                var s = VsTestFilter.TfsTestCaseFilterExpression.TestCaseFilterValue;
                var scount = s.Split('|', '&').Length;
                if (scount > Settings.AssemblySelectLimit)
                {
                    TestLog.Debug("Setting filter to empty due to TfsFilter size");
                    filter = TestFilter.Empty;
                }
            }

            return filter;
        }
    }
}
