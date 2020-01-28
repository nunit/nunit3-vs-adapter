using System;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Acceptance
{
    public static class UtilsTests
    {
        [Test]
        public static void RemoveIndent_removes_indentation_of_line_with_least_indentation_from_all_lines()
        {
            var result = Utils.RemoveIndent(string.Join(
                Environment.NewLine,
                "    First line",
                "  Second line",
                "    "));

            Assert.That(result, Is.EqualTo(string.Join(
                Environment.NewLine,
                "  First line",
                "Second line",
                "  ")));
        }

        [Test]
        public static void RemoveIndent_ignores_whitespace_lines()
        {
            var result = Utils.RemoveIndent(string.Join(
                Environment.NewLine,
                "    First line",
                " ",
                "    Second line"));

            Assert.That(result, Is.EqualTo(string.Join(
                Environment.NewLine,
                "First line",
                "",
                "Second line")));
        }

        [Test]
        public static void RemoveIndent_ignores_first_line_if_it_begins_without_indent()
        {
            var result = Utils.RemoveIndent(string.Join(
                Environment.NewLine,
                "First line",
                "    Second line"));

            Assert.That(result, Is.EqualTo(string.Join(
                Environment.NewLine,
                "First line",
                "Second line")));
        }

        [Test]
        public static void RemoveIndent_removes_first_line_if_is_empty()
        {
            var result = Utils.RemoveIndent(string.Join(
                Environment.NewLine,
                "",
                "    Second line"));

            Assert.That(result, Is.EqualTo(string.Join(
                Environment.NewLine,
                "Second line")));
        }
    }
}
