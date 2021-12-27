namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public interface IFilterArgument
    {
        string CompletedArgument();
        bool HasArguments { get; }
    }

    public abstract class VsTestFilter : IFilterArgument
    {
        protected string Arguments { get; }
        public bool HasArguments => Arguments.Length > 0;
        protected VsTestFilter(string arguments)
        {
            Arguments = arguments;
        }

        public abstract string CompletedArgument();

        public static IFilterArgument NoFilter => new VsTestTestCaseFilter("");
    }



    public class VsTestTestCaseFilter : VsTestFilter
    {
        public VsTestTestCaseFilter(string arguments) : base(arguments)
        {
        }

        public override string CompletedArgument()
        {
            var completeFilterStatement = $"/TestCaseFilter:{Arguments}";
            return completeFilterStatement;
        }
    }

    public class VsTestTestsFilter : VsTestFilter
    {
        public VsTestTestsFilter(string arguments) : base(arguments)
        {
        }
        public override string CompletedArgument()
        {
            var completeFilterStatement = $"/Tests:{Arguments}";
            return completeFilterStatement;
        }
    }
}
