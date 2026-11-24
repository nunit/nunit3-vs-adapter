// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, Terje Sandstrom
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

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter;

/// <summary>
/// The regular expression for a test name that NUnit cannot match by its exact text.
///
/// NUnit writes a character that it does not allow in its XML (control characters, U+FFFF and
/// others) as the text \uXXXX, so that is the name the adapter and the test platform see. The
/// framework's own FullName keeps the character, so an exact match on the reported name selects
/// nothing (nunit/nunit#4826). Such a name is selected with a regular expression that accepts
/// both the character and the text.
/// </summary>
internal static class TestNameRegex
{
    private static readonly Regex EscapedCharacter = new(@"\\u([0-9a-fA-F]{4})", RegexOptions.Compiled);

    /// <summary>
    /// Anchors a regular expression to the whole name. It uses \z, not $, because $ also matches
    /// before a final newline.
    /// </summary>
    public static string Exact(string regex) => @"\A" + regex + @"\z";

    /// <summary>
    /// True when the name holds a character NUnit does not allow in its XML, raw or as \uXXXX text.
    /// </summary>
    public static bool NeedsRegex(string name)
    {
        for (int i = 0; i < name.Length; i++)
        {
            if (IsReplacedByNUnit(name, i))
                return true;
        }

        foreach (Match m in EscapedCharacter.Matches(name))
        {
            if (IsReplacedByNUnit(ToChar(m)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The name as an unanchored regular expression, accepting either form of each such character.
    /// </summary>
    public static string ToRegex(string name)
    {
        var regex = new StringBuilder();
        int start = 0;
        for (int i = 0; i < name.Length; i++)
        {
            Match m = EscapedCharacter.Match(name, i);
            bool isEscaped = m.Success && m.Index == i && IsReplacedByNUnit(ToChar(m));
            if (!isEscaped && !IsReplacedByNUnit(name, i))
                continue;

            regex.Append(Regex.Escape(name.Substring(start, i - start)));
            char c = isEscaped ? ToChar(m) : name[i];

            // Reported text is kept as written, so a literal escape with uppercase hex in a custom
            // name still matches itself. NUnit itself writes lowercase hex.
            string text = isEscaped ? Regex.Escape(m.Value) : $@"\\u{(int)c:x4}";
            regex.Append($@"(?:\u{(int)c:x4}|{text})");
            i += isEscaped ? m.Length - 1 : 0;
            start = i + 1;
        }

        return regex.Append(Regex.Escape(name.Substring(start))).ToString();
    }

    private static char ToChar(Match m) => (char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    // The characters NUnit's XmlExtensions.EscapeInvalidXmlCharacters writes as \uXXXX.
    // In a raw name a surrogate counts only when it is not part of a pair.
    private static bool IsReplacedByNUnit(string name, int i)
    {
        char c = name[i];
        if (char.IsHighSurrogate(c))
            return i + 1 >= name.Length || !char.IsLowSurrogate(name[i + 1]);
        if (char.IsLowSurrogate(c))
            return i == 0 || !char.IsHighSurrogate(name[i - 1]);
        return IsReplacedByNUnit(c);
    }

    // As \uXXXX text, any surrogate is one NUnit replaced, because NUnit keeps a valid pair raw.
    private static bool IsReplacedByNUnit(char c) =>
        c <= 0x8 || c == 0xB || c == 0xC || (c >= 0xE && c <= 0x1F)
        || (c >= 0x7F && c <= 0x84) || (c >= 0x86 && c <= 0x9F)
        || char.IsSurrogate(c) || c == 0xFFFE || c == 0xFFFF;
}
