#if NET35
using System;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
    public static class TypeExtensions
    {
        public static Type GetTypeInfo(this Type type) => type;
    }
}
#endif
