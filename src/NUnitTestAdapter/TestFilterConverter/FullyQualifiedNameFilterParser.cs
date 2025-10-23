// ***********************************************************************
// Copyright (c) 2024 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// Extracts fully qualified test names from the filter string that is passed
/// to the adapter by the test platform.
/// </summary>
public static class FullyQualifiedNameFilterParser
{
    private const string FullyQualifiedNameProperty = "FullyQualifiedName";
    private const string FullyQualifiedNamePrefix = FullyQualifiedNameProperty + "=";
    private const char OrOperator = '|';
    private const char AndOperator = '&';

    private static readonly Regex OtherPropertyPattern = new(
        "(^|[|&!(])\\s*(TestCategory|Priority|Name)\\s*[=!~]",
        RegexOptions.Compiled);

    /// <summary>
    /// Returns the original filter string when it can be handled exclusively by
    /// fully qualified name parsing; otherwise returns <see cref="string.Empty"/>.
    /// </summary>
    /// <param name="filterString">The raw filter string provided by the test platform.</param>
    /// <returns>The normalized filter string or <see cref="string.Empty"/> when unsupported.</returns>
    public static string GetFullyQualifiedNameFilterOrEmpty(string? filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return string.Empty;

        var span = TrimWhitespace(filterString.AsSpan());

        if (span.Length == 0)
            return string.Empty;

        while (TryStripOuterParentheses(span, out var inner))
        {
            span = TrimWhitespace(inner);

            if (span.Length == 0)
                return string.Empty;
        }

        var candidateString = span.ToString();
        var candidate = candidateString.AsSpan();

        if (!candidateString.Contains(FullyQualifiedNamePrefix, StringComparison.Ordinal))
            return string.Empty;

        if (OtherPropertyPattern.IsMatch(candidateString))
            return string.Empty;

        var index = 0;
        var parsedClause = false;
        var endedWithOperator = false;

        while (index < candidate.Length)
        {
            SkipWhitespace(candidate, ref index);

            if (index >= candidate.Length)
                break;

            if (!TryConsumeFullyQualifiedName(candidate, ref index))
                return string.Empty;

            SkipWhitespace(candidate, ref index);

            if (index >= candidate.Length || candidate[index] != '=')
                return string.Empty;

            index++;

            var valueStart = index;
            var hasValue = false;

            while (index < candidate.Length)
            {
                var ch = candidate[index];

                if (ch == OrOperator && !IsEscaped(candidate, index))
                    break;

                if (ch == AndOperator && !IsEscaped(candidate, index))
                    return string.Empty;

                if (!char.IsWhiteSpace(ch))
                    hasValue = true;

                index++;
            }

            if (!hasValue)
                return string.Empty;

            var trailing = index - 1;

            while (trailing >= valueStart && char.IsWhiteSpace(candidate[trailing]))
                trailing--;

            if (trailing < valueStart)
                return string.Empty;

            SkipWhitespace(candidate, ref index);

            if (index < candidate.Length)
            {
                if (candidate[index] != OrOperator)
                    return string.Empty;

                index++;
                endedWithOperator = true;
            }
            else
            {
                endedWithOperator = false;
            }

            parsedClause = true;
        }

        if (!parsedClause || endedWithOperator)
            return string.Empty;

        return filterString!;
    }

    /// <summary>
    /// Retrieves all fully qualified test names from <paramref name="filterString"/>.
    /// </summary>
    /// <param name="filterString">The raw filter string provided by the test platform.</param>
    /// <returns>A read-only list of the fully qualified names contained in the filter.</returns>
    public static IReadOnlyList<string> GetFullyQualifiedNames(string? filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return Array.Empty<string>();

        var trimmed = filterString.Trim();

        if (trimmed.Length == 0)
            return Array.Empty<string>();

        if (trimmed[0] == '(' && trimmed[^1] == ')' && trimmed.Length > 1)
            trimmed = trimmed.Substring(1, trimmed.Length - 2);

        if (trimmed.Length == 0)
            return Array.Empty<string>();

        var segments = trimmed.Split(new[] { "|" + FullyQualifiedNamePrefix }, StringSplitOptions.None);

        if (segments.Length == 0)
            return Array.Empty<string>();

        var result = new List<string>(segments.Length);

        for (var index = 0; index < segments.Length; index++)
        {
            var segment = segments[index];

            if (index == 0)
            {
                if (segment.StartsWith(FullyQualifiedNamePrefix, StringComparison.Ordinal))
                    segment = segment.Substring(FullyQualifiedNamePrefix.Length);
                else
                    continue;
            }

            if (segment.Length == 0)
                continue;

            result.Add(segment);
        }

        return result;
}

    private static ReadOnlySpan<char> TrimWhitespace(ReadOnlySpan<char> span)
    {
        var start = 0;
        var end = span.Length - 1;

        while (start <= end && char.IsWhiteSpace(span[start]))
            start++;

        while (end >= start && char.IsWhiteSpace(span[end]))
            end--;

        return start > end ? ReadOnlySpan<char>.Empty : span.Slice(start, end - start + 1);
    }

    private static bool TryStripOuterParentheses(ReadOnlySpan<char> span, out ReadOnlySpan<char> inner)
    {
        inner = span;

        if (span.Length < 2 || span[0] != '(' || span[^1] != ')')
            return false;

        var depth = 0;

        for (var index = 0; index < span.Length; index++)
        {
            var ch = span[index];

            if (ch == '(' && !IsEscaped(span, index))
            {
                depth++;
            }
            else if (ch == ')' && !IsEscaped(span, index))
            {
                depth--;

                if (depth < 0)
                    return false;

                if (depth == 0 && index != span.Length - 1)
                    return false;
            }
        }

        if (depth != 0)
            return false;

        inner = span.Slice(1, span.Length - 2);

        return true;
    }

    private static void SkipWhitespace(ReadOnlySpan<char> span, ref int index)
    {
        while (index < span.Length && char.IsWhiteSpace(span[index]))
            index++;
    }

    private static bool TryConsumeFullyQualifiedName(ReadOnlySpan<char> span, ref int index)
    {
        if (index + FullyQualifiedNameProperty.Length > span.Length)
            return false;

        var property = span.Slice(index, FullyQualifiedNameProperty.Length);

        if (!property.Equals(FullyQualifiedNameProperty, StringComparison.Ordinal))
            return false;

        index += FullyQualifiedNameProperty.Length;

        if (index < span.Length)
        {
            var next = span[index];

            if (!char.IsWhiteSpace(next) && next != '=')
                return false;
        }

        return true;
    }

    private static bool IsEscaped(ReadOnlySpan<char> span, int index)
    {
        var escapeCount = 0;

        for (var i = index - 1; i >= 0; i--)
        {
            if (span[i] != '\\')
                break;

            escapeCount++;
        }

        return (escapeCount & 1) == 1;
    }
}
