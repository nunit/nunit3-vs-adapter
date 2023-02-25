using System;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
#if NET462DISABLED
    public static class TypeExtensions
    {
        public static Type GetTypeInfo(this Type type) => type;
    }
#endif
    public static class StringExtensions
    {
        public static bool IsNullOrWhiteSpace(this string value) => string.IsNullOrEmpty(value) || value.Trim().Length == 0;
    }
}
