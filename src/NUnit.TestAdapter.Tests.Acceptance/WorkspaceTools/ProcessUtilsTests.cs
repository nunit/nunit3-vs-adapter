using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance.WorkspaceTools
{
    public static class ProcessUtilsTests
    {
        [Test]
        public static void EscapeProcessArguments_null()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new string[] { null }), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArguments_null_alwaysQuote()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new string[] { null }, true), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArguments_empty()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { string.Empty }), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArguments_empty_alwaysQuote()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { string.Empty }, true), Is.EqualTo("\"\""));
        }

        [Test]
        public static void EscapeProcessArguments_simple()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "123" }), Is.EqualTo("123"));
        }

        [Test]
        public static void EscapeProcessArguments_simple_alwaysQuote()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "123" }, true), Is.EqualTo("\"123\""));
        }

        [Test]
        public static void EscapeProcessArguments_with_ending_backslash()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "123\\" }), Is.EqualTo("123\\"));
        }

        [Test]
        public static void EscapeProcessArguments_with_ending_backslash_alwaysQuote()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "123\\" }, true), Is.EqualTo("\"123\\\\\""));
        }

        [Test]
        public static void EscapeProcessArguments_with_spaces_and_ending_backslash()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { " 1 2 3 \\" }), Is.EqualTo("\" 1 2 3 \\\\\""));
        }

        [Test]
        public static void EscapeProcessArguments_with_spaces()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { " 1 2 3 " }), Is.EqualTo("\" 1 2 3 \""));
        }

        [Test]
        public static void EscapeProcessArguments_with_quotes()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "\"1\"2\"3\"" }), Is.EqualTo("\"\\\"1\\\"2\\\"3\\\"\""));
        }

        [Test]
        public static void EscapeProcessArguments_with_slashes()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "1\\2\\\\3\\\\\\" }), Is.EqualTo("1\\2\\\\3\\\\\\"));
        }

        [Test]
        public static void EscapeProcessArguments_with_slashes_alwaysQuote()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "1\\2\\\\3\\\\\\" }, true), Is.EqualTo("\"1\\2\\\\3\\\\\\\\\\\\\""));
        }

        [Test]
        public static void EscapeProcessArguments_slashes_followed_by_quotes()
        {
            Assert.That(ProcessUtils.EscapeProcessArguments(new[] { "\\\\\"" }), Is.EqualTo("\"\\\\\\\\\\\"\""));
        }
    }
}
