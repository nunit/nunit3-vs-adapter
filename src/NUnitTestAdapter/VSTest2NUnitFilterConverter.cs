namespace NUnit.VisualStudio.TestAdapter
{
    public class VsTest2NUnitFilterConverter
    {
        private string NUnitFilter { get; }
        public VsTest2NUnitFilterConverter(string vstestfilter)
        {
            NUnitFilter = vstestfilter.Replace("TestCategory", "Category");
        }

        public override string ToString()
        {
            return NUnitFilter;
        }
    }
}