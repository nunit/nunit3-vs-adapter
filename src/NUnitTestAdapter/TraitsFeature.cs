﻿// ***********************************************************************
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

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public static class TraitsFeature
    {
        public static void AddTrait(this TestCase testCase, string name, string value)
        {
            testCase?.Traits.Add(new Trait(name, value));
        }
        private const string NunitTestCategoryLabel = "Category";


        /// <summary>
        /// Stores the information needed to initialize a <see cref="TestCase"/>
        /// which can be inherited from an ancestor node during the conversion of test cases.
        /// </summary>
        public sealed class CachedTestCaseInfo
        {
            /// <summary>
            /// Used to populate a test case’s <see cref="TestObject.Traits"/> collection.
            /// Currently, the only effect this has is to add a Test Explorer grouping header
            /// for each trait with the name “Name [Value]”, or for an empty value, “Name”.
            /// </summary>
            public List<Trait> Traits { get; } = new List<Trait>();

            /// <summary>
            /// Used by <see cref="TfsTestFilter"/>; does not affect the Test Explorer UI.
            /// </summary>
            public bool Explicit { get; set; }

            // Eventually, we might split out the Categories collection and make this
            // an immutable struct. (https://github.com/nunit/nunit3-vs-adapter/pull/457)
        }

        public static void AddTraitsFromTestNode(this TestCase testCase, XmlNode testNode,
            IDictionary<string, CachedTestCaseInfo> traitsCache, ITestLogger logger, IAdapterSettings adapterSettings)
        {
            var ancestor = testNode.ParentNode;
            var key = ancestor.Attributes?["id"]?.Value;
            var categorylist = new CategoryList(testCase,adapterSettings);
            // Reading ancestor properties of a test-case node. And adding to the cache.
            while (ancestor != null && key != null)
            {
                if (traitsCache.ContainsKey(key))
                {
                    categorylist.AddRange(traitsCache[key].Traits.Where(o => o.Name == NunitTestCategoryLabel).Select(prop => prop.Value).ToList());

                    if (traitsCache[key].Explicit)
                        testCase.SetPropertyValue(CategoryList.NUnitExplicitProperty, true);

                    var traitslist = traitsCache[key].Traits.Where(o => o.Name != NunitTestCategoryLabel).ToList();
                    if (traitslist.Count > 0)
                        testCase.Traits.AddRange(traitslist);
                }
                else
                {
                    categorylist.ProcessTestCaseProperties(ancestor,true,key,traitsCache);
                    // Adding entry to dictionary, so that we will not make SelectNodes call again.
                    if (categorylist.LastNodeListCount == 0 && !traitsCache.ContainsKey(key))
                    {
                        traitsCache[key] = new CachedTestCaseInfo();
                    }
                }
                ancestor = ancestor.ParentNode;
                key = ancestor?.Attributes?["id"]?.Value;
            }

            // No Need to store test-case properties in cache.
            categorylist.ProcessTestCaseProperties(testNode,false);
            categorylist.UpdateCategoriesToVs();
        }


        public static IEnumerable<NTrait> GetTraits(this TestCase testCase)
        {
            var traits = new List<NTrait>();

            if (testCase?.Traits != null)
            {
                traits.AddRange(from trait in testCase.Traits let name = trait.Name let value = trait.Value select new NTrait(name, value));
            }
            return traits;
        }

        public static IEnumerable<string> GetCategories(this TestCase testCase)
        {
            var categories = testCase.GetPropertyValue(CategoryList.NUnitTestCategoryProperty) as string[];
            return categories;
        }
    }

    public class NTrait
    {
        public string Name { get; }
        public string Value { get; }

        public NTrait(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
