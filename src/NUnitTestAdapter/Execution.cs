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
        public static Execution Create(IExecutionContext ctx) => ctx.Settings.DesignMode ? new IdeExecution(ctx) : new VsTestExecution(ctx);
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
                if (!discovery.IsExplicitRun && discovery.HasExplicitTests && Settings.ExplicitMode == ExplicitModeEnum.Strict)
                {
                    var filterExt = new TestFilter($"<not><prop name='Explicit'>true</prop></not>");
                    var combiner = new TestFilterCombiner(testFilter, filterExt);
                    return combiner.GetFilter();
                }
                return testFilter;
            }
            var filterBuilder = CreateTestFilterBuilder();
            return filterBuilder.FilterByList(discovery.LoadedTestCases);
        }
    }

    public class TestFilterCombiner
    {
        private readonly TestFilter _a;
        private readonly TestFilter _b;

        public TestFilterCombiner(TestFilter a, TestFilter b)
        {
            _a = a;
            _b = b;
        }

        public TestFilter GetFilter()
        {
            var innerA = StripFilter(_a);
            var innerB = StripFilter(_b);
            var inner = $"<filter>{innerA}{innerB}</filter>";
            return new TestFilter(inner);
        }

        private string StripFilter(TestFilter x)
        {
            var s = x.Text.Replace("<filter>", "");
            var s2 = s.Replace("</filter>", "");
            return s2;
        }
    }



    public class IdeExecution : Execution
    {
        public IdeExecution(IExecutionContext ctx) : base(ctx)
        {
        }
        public override TestFilter CheckFilterInCurrentMode(TestFilter filter, IDiscoveryConverter discovery)
        {
            if (!discovery.IsDiscoveryMethodCurrent)
                return filter;
            if (filter.IsEmpty())
                return filter;
            filter = CheckFilter(filter, discovery);
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
            TestLog.Debug($"TfsFilter used, length: {vsTestFilter.TfsTestCaseFilterExpression?.TestCaseFilterValue.Length}");
            // NOTE This overwrites filter used in call
            var filterBuilder = CreateTestFilterBuilder();
            filter = Settings.DiscoveryMethod == DiscoveryMethod.Current
                ? Settings.UseNUnitFilter
                    ? filterBuilder.ConvertVsTestFilterToNUnitFilter(vsTestFilter)
                    : filterBuilder.ConvertTfsFilterToNUnitFilter(vsTestFilter, discovery)
                : filterBuilder.ConvertTfsFilterToNUnitFilter(vsTestFilter, discovery.LoadedTestCases);

            Dump?.AddString($"\n\nTFSFilter: {vsTestFilter.TfsTestCaseFilterExpression?.TestCaseFilterValue}\n");
            Dump?.DumpVSInputFilter(filter, "(At Execution (TfsFilter)");

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
                var s = VsTestFilter.TfsTestCaseFilterExpression.TestCaseFilterValue;
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
            TestLog.Debug("Setting filter to empty due to TfsFilter size");
            return TestFilter.Empty;
        }
    }
}
