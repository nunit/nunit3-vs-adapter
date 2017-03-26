using System;

namespace NUnit.VisualStudio.TestAdapter.Internal
{
    /// <summary>
    /// Extensions to make types behave the same in .NET Core
    /// and in .NET 3.5
    /// </summary>
    public static class TypeExtensions
    {
#if !NETCOREAPP1_0
        /// <summary>
        /// Allows .NET 3.5 to use the GetTypeInfo call in the same
        /// way as .NET Core
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type GetTypeInfo(this Type type) => type;
#endif
    }
}
