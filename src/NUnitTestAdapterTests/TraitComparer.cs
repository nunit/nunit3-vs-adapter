// ***********************************************************************
// Copyright (c) 2011-2018 Charlie Poole, Terje Sandstrom
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

using System.Collections.Generic;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public sealed class TraitComparer : IEqualityComparer<Trait>
    {
        public static TraitComparer Instance { get; } = new TraitComparer();
        public TraitComparer() { }

        public bool Equals(Trait x, Trait y)
        {
            if (x == y) return true;
            if (x == null | y == null) return false;
            return x.Name == y.Name && x.Value == y.Value;
        }

        public int GetHashCode(Trait obj)
        {
            if (obj == null) return 0;
            var hashCode = 1477024672;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(obj.Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(obj.Value);
            return hashCode;
        }
    }
}
