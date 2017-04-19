namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public abstract class AbstractBaseClass
    {
        public void EmptyMethod_ThreeLines()
        { // expectedLineDebug = 6
        } // expectedLineRelease = 7
    }

    public class ConcreteBaseClass
    {
        public void EmptyMethod_ThreeLines()
        { // expectedLineDebug = 13
        } // expectedLineRelease = 14
    }
}
