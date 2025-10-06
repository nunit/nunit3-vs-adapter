namespace NUnit.VisualStudio.TestAdapter.ExecutionProcesses;

public static class ExecutionFactory
{
    public static Execution Create(IExecutionContext ctx) => ctx.Settings.DesignMode ? new IdeExecution(ctx) : new VsTestExecution(ctx);
}