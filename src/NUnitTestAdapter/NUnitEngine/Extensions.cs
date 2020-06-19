using System;
using System.Collections.Generic;
using System.Linq;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public static class Extensions
    {
        /// <summary>
        /// All will return true if seq is empty.  This returns false if sequence is empty.
        /// </summary>
        public static bool AllWithEmptyFalse<T>(this IEnumerable<T> list, Func<T, bool> pred) =>
            list.All(pred) && list.Any();
    }
}
