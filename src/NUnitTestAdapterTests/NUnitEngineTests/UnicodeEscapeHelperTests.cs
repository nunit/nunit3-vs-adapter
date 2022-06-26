using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    public class UnicodeEscapeHelperTests
    {
        [TestCase("\\u001b", "\u001b")]
        [TestCase("\\u001", "\\u001")]
        [TestCase("\\u01", "\\u01")]
        [TestCase("\\u1", "\\u1")]
        [TestCase("\\u001b6", "\u001b6")]
        [TestCase("some-text", "some-text")]
        public void UnEscapeUnicodeCharacters_ShouldReplaceBackslashU(string value, string expected)
        {
            Assert.That(value.UnEscapeUnicodeCharacters(), Is.EqualTo(expected));
        }
    }
}