using System;
using System.Globalization;
using System.Text;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    internal static class UnicodeEscapeHelper
    {
        public static string UnEscapeUnicodeCharacters(this string text)
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

            escapedChar = (char)escapeValue;

            return true;
        }
    }
}