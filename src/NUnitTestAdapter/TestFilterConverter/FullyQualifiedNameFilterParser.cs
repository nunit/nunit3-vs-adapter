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

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// Extracts fully qualified test names from the filter string that is passed
/// to the adapter by the test platform.
/// </summary>
public static class FullyQualifiedNameFilterParser
{
    private const string FullyQualifiedNamePrefix = "FullyQualifiedName=";

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
}
