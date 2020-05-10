using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class TryParseTests
    {
        public enum Whatever
        {
            Something,
            Nothing
        }

        [TestCase("1", true, Whatever.Nothing)]
        [TestCase("Nothing", true, Whatever.Nothing)]
        [TestCase("something", true, Whatever.Something)]
        [TestCase("0", true, Whatever.Something)]
        public void EnumTryParseTestOk(string input, bool expected, Whatever expectedResult)
        {
            var ok = TryParse.EnumTryParse(input, out Whatever whatever);
            Assert.That(ok, Is.EqualTo(expected));
            Assert.That(whatever, Is.EqualTo(expectedResult));
        }

        [TestCase("10")]
        [TestCase("svada")]
        [TestCase("")]
        [TestCase(null)]
        public void EnumTryParseTestNotOk(string input)
        {
            var ok = TryParse.EnumTryParse(input, out Whatever whatever);
            Assert.That(ok, Is.EqualTo(false));
        }
    }
}