using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.ExecutionProcesses;

public class IdeExecution(IExecutionContext ctx) : Execution(ctx)
{
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