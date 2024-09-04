using System;
using System.Globalization;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine;

internal static class UnicodeEscapeHelper
{
    private const int EscapeAsciiValue = 0x1B;

    public static string UnEscapeUnicodeColorCodesCharacters(this string text)
    {
        if (text == null)
            return null;

        // Small optimization, if there are no "\u", then there is no need to rewrite the string
        var firstEscapeIndex = text.IndexOf("\\u", StringComparison.Ordinal);
        if (firstEscapeIndex == -1)
            return text;

        var stringBuilder = new StringBuilder(text.Substring(0, firstEscapeIndex));
        for (var position = firstEscapeIndex; position < text.Length; position++)
        {
            char c = text[position];
            if (c == '\\' && TryUnEscapeOneCharacter(text, position, out var escapedChar, out var extraCharacterRead))
            {
                stringBuilder.Append(escapedChar);
                position += extraCharacterRead;
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString();
    }

    private static bool TryUnEscapeOneCharacter(string text, int position, out char escapedChar, out int extraCharacterRead)
    {
        const string unicodeEscapeSample = "u0000";

        extraCharacterRead = 0;
        escapedChar = '\0';
        if (position + unicodeEscapeSample.Length >= text.Length)
            return false;

        extraCharacterRead = unicodeEscapeSample.Length;
        if (!int.TryParse(text.Substring(position + 2, unicodeEscapeSample.Length - 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var escapeValue))
            return false;

        // Here we only want to escape color escape character when used in a context of a ANSI color code
        // See https://github.com/nunit/nunit3-vs-adapter/issues/1124 for more information.
        if (escapeValue != EscapeAsciiValue)
            return false;

        if (!IsAnsiColorCodeSequence(text, position + extraCharacterRead + 1))
            return false;

        escapedChar = (char)escapeValue;

        return true;
    }

    private static bool IsAnsiColorCodeSequence(string text, int position)
    {
        var start = false;
        while (position < text.Length)
        {
            var c = text[position++];
            // Look for the begining [
            if (c == '[' && !start)
            {
                start = true;
                continue;
            }

            // Found the 'm' at the end
            if (c == 'm' && start)
                return true;

            // [ was not found
            if (!start)
                return false;

            // Ignore all number and ;
            var isDigit = c is >= '0' and <= '9';
            if (!isDigit && c != ';')
                return false;
        }

        // At the end without the ending 'm'
        return false;
    }
}