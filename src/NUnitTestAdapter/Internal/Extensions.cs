using System;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
#if NET35
    public static class TypeExtensions
    {
        public static Type GetTypeInfo(this Type type) => type;
    }
#endif
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return value == null || value.Trim().Length == 0;
        }
    }
}

