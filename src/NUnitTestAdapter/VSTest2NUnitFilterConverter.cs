namespace NUnit.VisualStudio.TestAdapter
{
    public class VSTest2NUnitFilterConverter
    {
        public bool Error { get; private set; }
        private string NUnitFilter { get; set; }
        public VSTest2NUnitFilterConverter(string vstestfilter)
        {
            
        }

        public override string ToString()
        {
            return NUnitFilter;
        }
    }
}