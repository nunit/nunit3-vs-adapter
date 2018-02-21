// ***********************************************************************
// Copyright (c) 2013-2018 Charlie Poole, Terje Sandstrom
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public struct TestTraitInfo
    {
        public Trait[] Traits { get; }
        public string[] Categories { get; }

        public TestTraitInfo(Trait[] traits, string[] categories)
        {
            Traits = traits;
            Categories = categories;
        }

        public void ApplyTo(TestCase testCase)
        {
            if (testCase == null) throw new ArgumentNullException(nameof(testCase));

            if (Traits != null) testCase.Traits.AddRange(Traits);
            if (Categories != null) testCase.SetPropertyValue(NUnitTestCaseProperties.TestCategory, Categories);
        }

        public static TestTraitInfo Combine(TestTraitInfo inherited, TestTraitInfo current)
        {
            return new TestTraitInfo(
                CombineTraits(inherited.Traits, current.Traits),
                CombineCategories(inherited.Categories, current.Categories));
        }

        private static Trait[] CombineTraits(Trait[] inherited, Trait[] current)
        {
            if (inherited == null) return current;
            if (current == null) return inherited;
            if (inherited.Length == 0) return current;
            if (current.Length == 0) return inherited;

            var combined = new Trait[inherited.Length + current.Length];
            inherited.CopyTo(combined, 0);
            current.CopyTo(combined, inherited.Length);

            return combined;
        }

        private static string[] CombineCategories(string[] inherited, string[] current)
        {
            if (inherited == null) return current;
            if (current == null) return inherited;
            if (inherited.Length == 0) return current;
            if (current.Length == 0) return inherited;

            var combined = new List<string>(inherited.Length + current.Length);
            combined.AddRange(inherited);

            foreach (var category in current)
                if (!combined.Contains(category))
                    combined.Add(category);

            return combined.ToArray();
        }
    }
}
