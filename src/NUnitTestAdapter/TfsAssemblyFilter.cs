namespace NUnit.VisualStudio.TestAdapter
{
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    using NUnit.Core.Filters;

    public class TfsAssemblyFilter : AssemblyFilter
    {
        private readonly TFSTestFilter tfsfilter;
        public TfsAssemblyFilter(string assemblyName, IRunContext runContext)
            : base(assemblyName)
        {
            this.tfsfilter = new TFSTestFilter(runContext);
        }

        internal override void ProcessTfsFilter()
        {
            if ( this.tfsfilter.HasTfsFilterValue)
            {
                var filteredTestCases = this.tfsfilter.CheckFilter(this.VsTestCases);
                var filter = new SimpleNameFilter();
                foreach (TestCase testCase in filteredTestCases)
                {
                    filter.Add(testCase.FullyQualifiedName);
                }
                this.NUnitFilter = filter;
            }
        }

    }
}