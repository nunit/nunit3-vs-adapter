using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public static class Constants
    {
        private const string AdjustedFQNPropertyName = "AdjustedFQN";

        public static readonly TestProperty TestCaseAdjustedFQNProperty = TestProperty.Register(AdjustedFQNPropertyName, AdjustedFQNPropertyName, typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));        
    }
}
