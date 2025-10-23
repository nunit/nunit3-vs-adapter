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
    /// Returns true when it can be handled exclusively by
    /// fully qualified name parsing; otherwise returns false.
    /// </summary>
    /// <param name="filterString">The raw filter string provided by the test platform.</param>
    /// <returns>false when unsupported.</returns>
    public static bool CheckFullyQualifiedNameFilter(string filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return false;

        var span = TrimWhitespace(filterString.AsSpan());

        if (span.Length == 0)
            return false;

        while (TryStripOuterParentheses(span, out var inner))
        {
            span = TrimWhitespace(inner);

            if (span.Length == 0)
                return false;
        }

        var candidateString = span.ToString();
        var candidate = candidateString.AsSpan();

        if (OtherPropertyPattern.IsMatch(candidateString))
            return false;

        var index = 0;
        var parsedClause = false;
        var endedWithOperator = false;

        while (index < candidate.Length)
        {
            SkipWhitespace(candidate, ref index);

            if (index >= candidate.Length)
                break;

            if (!TryConsumeFullyQualifiedName(candidate, ref index))
                return false;

            SkipWhitespace(candidate, ref index);

            if (index >= candidate.Length || candidate[index] != '=')
                return false;

            index++;

            var valueStart = index;
            var hasValue = false;

            while (index < candidate.Length)
            {
                var ch = candidate[index];

                if (ch == OrOperator && !IsEscaped(candidate, index))
                    break;

                if (ch == AndOperator && !IsEscaped(candidate, index))
                    return false;

                if (!char.IsWhiteSpace(ch))
                    hasValue = true;

                index++;
            }

            if (!hasValue)
                return false;

            var trailing = index - 1;

            while (trailing >= valueStart && char.IsWhiteSpace(candidate[trailing]))
                trailing--;

            if (trailing < valueStart)
                return false;

            SkipWhitespace(candidate, ref index);

            if (index < candidate.Length)
            {
                if (candidate[index] != OrOperator)
                    return false;

                index++;
                endedWithOperator = true;
            }
            else
            {
                endedWithOperator = false;
            }

            parsedClause = true;
        }

        return parsedClause && !endedWithOperator;
    }

    /// <summary>
    /// Retrieves all fully qualified test names from <paramref name="filterString"/>.
    /// </summary>
    /// <param name="filterString">The raw filter string provided by the test platform.</param>
    /// <returns>A read-only list of the fully qualified names contained in the filter.</returns>
    public static IReadOnlyList<string> GetFullyQualifiedNames(string filterString)
    {
        if (string.IsNullOrWhiteSpace(filterString))
            return [];

        var trimmed = filterString.Trim();

        if (trimmed.Length == 0)
            return [];

        bool done = false;
        while (!done)
        {
            if (trimmed[0] == '(' && trimmed[trimmed.Length - 1] == ')' && trimmed.Length > 1)
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            else
                done = true;
        }

        if (trimmed.Length == 0)
            return [];

        var result = SplitOnFullyQualifiedName(trimmed);

        return result;
    }

    /// <summary>
    /// Splits on start or '|' only when followed by "FullyQualifiedName" and '='
    /// </summary>
    /// <returns>Returns the values after '=' up to the next '|' (or end), trimmed.</returns>
    public static List<string> SplitOnFullyQualifiedName(string input)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(input)) return result;

        var pattern = @"(?:^|\|)\s*FullyQualifiedName\s*=\s*([^|]*)";

        foreach (Match m in Regex.Matches(input, pattern))
        {
            var value = m.Groups[1].Value.Trim();
            if (value.Length > 0) result.Add(value);
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

        if (span.Length < 2 || span[0] != '(' || span[span.Length - 1] != ')')
            return false;

        var depth = 0;

        for (var index = 0; index < span.Length; index++)
        {
            var ch = span[index];

            switch (ch)
            {
                case '(' when !IsEscaped(span, index):
                    depth++;
                    break;
                case ')' when !IsEscaped(span, index):
                    depth--;

                    switch (depth)
                    {
                        case < 0:
                        case 0 when index != span.Length - 1:
                            return false;
                    }

                    break;
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

        // Fix for CS0176: Use static string.Equals instead of instance Equals
        if (!string.Equals(property.ToString(), FullyQualifiedNameProperty, StringComparison.Ordinal))
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
