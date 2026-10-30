#nullable enable

using System;

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

public static class XmlEscaperHelper
{
    public static string Escape(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("'", "&apos;");
    }

    public static string Unescape(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return value
            .Replace("&quot;", "\"")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Replace("&apos;", "'")
            .Replace("&amp;", "&");
    }
}
