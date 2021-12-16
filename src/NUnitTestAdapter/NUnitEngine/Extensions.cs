// ***********************************************************************
// Copyright (c) 2020-2021 Charlie Poole, Terje Sandstrom
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
using System.Linq;
using NUnit.Engine;
#pragma warning disable SA1618 // Generic type parameters should be documented

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public static class Extensions
    {
        /// <summary>
        /// All will return true if seq is empty.  This returns false if sequence is empty.
        /// </summary>
        public static bool AllWithEmptyFalse<T>(this IEnumerable<T> list, Func<T, bool> pred) =>
            list.All(pred) && list.Any();

        public static bool IsEmpty(this TestFilter filter) => filter == TestFilter.Empty;


        public static bool IsCategoryFilter(this TestFilter filter) =>
            filter != TestFilter.Empty && filter.Text.Contains("<cat>");

        public static bool IsNegativeCategoryFilter(this TestFilter filter) =>
            filter.IsCategoryFilter() && filter.Text.Contains("<not><cat>");
    }
}
