using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests;

public class UnicodeEscapeHelperTests
{
    [TestCase("\\u001b", "\\u001b")]
    [TestCase("\\u001", "\\u001")]
    [TestCase("\\u01", "\\u01")]
    [TestCase("\\u1", "\\u1")]
    [TestCase("\\u001b6", "\\u001b6")]
    [TestCase("\\u001b[0m", "\u001b[0m")]
    [TestCase("\\u001b[36m", "\u001b[36m")]
    [TestCase("\\u001b[48;5;122mTest", "\u001b[48;5;122mTest")]
    [TestCase("some-text", "some-text")]
    public void UnEscapeUnicodeColorCodesCharactersShouldReplaceBackslashU(string value, string expected)
    {
        Assert.That(value.UnEscapeUnicodeColorCodesCharacters(), Is.EqualTo(expected));
    }
}