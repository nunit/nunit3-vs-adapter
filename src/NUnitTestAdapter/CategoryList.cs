// ***********************************************************************
// Copyright (c) 2014-2021 Charlie Poole, Terje Sandstrom
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter
{
    public class CategoryList
    {
        private const string NUnitTestCategoryLabel = "Category";

        internal static readonly TestProperty NUnitTestCategoryProperty = TestProperty.Register(
            id: "NUnit.TestCategory",
            // This label is what causes VSTest to include the values in the Test Categories column and show the
            // grouping as `X` rather than `Category [X]`. (https://github.com/nunit/nunit3-vs-adapter/issues/310)
            label: "TestCategory",
            valueType: typeof(string[]),
            TestPropertyAttributes.Hidden
#pragma warning disable CS0618 // This is the only way to fix https://github.com/nunit/nunit3-vs-adapter/issues/310, and MSTest also depends on this.
                | TestPropertyAttributes.Trait,
#pragma warning restore CS0618
            owner: typeof(TestCase));

        internal static readonly TestProperty NUnitExplicitProperty = TestProperty.Register(
            "NUnit.Explicit",
            "Explicit", typeof(bool), TestPropertyAttributes.Hidden, typeof(TestCase));

        private const string ExplicitTraitName = "Explicit";

        // The empty string causes the UI we want.
        // If it's null, the explicit trait doesn't show up in Test Explorer.
        // If it's not empty, it shows up as “Explicit [value]” in Test Explorer.
        private const string ExplicitTraitValue = "";

        private readonly NUnitProperty explicitTrait = new (ExplicitTraitName, ExplicitTraitValue);

        private readonly List<string> categorylist = new ();
        private readonly TestCase testCase;
        private readonly IAdapterSettings settings;
        private readonly bool showInternalProperties;

        public CategoryList(TestCase testCase, IAdapterSettings adapterSettings)
        {
            settings = adapterSettings;
            showInternalProperties = settings.ShowInternalProperties;
            this.testCase = testCase;
            // MsTestCategoryProperty = TestProperty.Register(MSTestCategoryName, VsTestCategoryLabel, typeof(string[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait, typeof(TestCase));
        }

        public void AddRange(IEnumerable<string> categories)
        {
            categorylist.AddRange(categories);
        }

        /// <summary>
        /// Unsure about purpose of this, see https://github.com/nunit/nunit3-vs-adapter/pull/763#discussion_r446668680.
        /// </summary>
        public int LastNodeListCount { get; private set; }

        public IEnumerable<string> ProcessTestCaseProperties(INUnitTestCasePropertyInfo testNode, bool addToCache, string key = null,
            IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache = null)
        {
            LastNodeListCount = testNode.Properties.Count();
            foreach (var propertyNode in testNode.Properties)
            {
                if (addToCache)
                    AddTraitsToCache(traitsCache, key, propertyNode);
                if (IsInternalProperty(propertyNode))
                    continue;
                if (propertyNode.Name != NUnitTestCategoryLabel)
                {
                    testCase.Traits.Add(new Trait(propertyNode.Name, propertyNode.Value));
                }
                else
                {
                    categorylist.Add(propertyNode.Value);
                }
            }

            if (testNode.RunState != RunStateEnum.Explicit) // Attributes?["runstate"]?.Value != "Explicit")
                return categorylist;
            // Add UI grouping “Explicit”
            if (testCase.Traits.All(trait => trait.Name != ExplicitTraitName))
                testCase.Traits.Add(new Trait(ExplicitTraitName, ExplicitTraitValue));

            // Track whether the test is actually explicit since multiple things result in the same UI grouping
            testCase.SetPropertyValue(NUnitExplicitProperty, true);

            if (addToCache)
            {
                // Add UI grouping “Explicit”
                AddTraitsToCache(traitsCache, key, explicitTrait);

                // Track whether the test is actually explicit since multiple things result in the same UI grouping
                GetCachedInfo(traitsCache, key).Explicit = true;
            }

            return categorylist;
        }

        /// <summary>
        /// See https://github.com/nunit/nunit/blob/master/src/NUnitFramework/framework/Internal/PropertyNames.cs.
        /// </summary>
        private readonly HashSet<string> _internalProperties = new (StringComparer.OrdinalIgnoreCase)
        { "Author", "ApartmentState", "Description", "IgnoreUntilDate", "LevelOfParallelism", "MaxTime", "Order", "ParallelScope", "Repeat", "RequiresThread", "SetCulture", "SetUICulture", "TestOf", "Timeout" };


        private bool IsInternalProperty(NUnitProperty property)
        {
            if (property.Name == ExplicitTraitName)
            {
                // Otherwise the IsNullOrEmpty check does the wrong thing,
                // but I'm not sure of the consequences of allowing all empty strings.
                return false;
            }

            // Property names starting with '_' are for internal use only, but over time this has changed, so we now use a list
            return (!showInternalProperties &&
                _internalProperties.Contains(property.Name)) || (string.IsNullOrEmpty(property.Name) ||
                                                                property.Name[0] == '_' ||
                                                                string.IsNullOrEmpty(property.Value));
        }

        private void AddTraitsToCache(IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache, string key, NUnitProperty property)
        {
            if (IsInternalProperty(property)) return;

            var info = GetCachedInfo(traitsCache, key);
            info.Traits.Add(new Trait(property.Name, property.Value));
        }

        private static TraitsFeature.CachedTestCaseInfo GetCachedInfo(IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache, string key)
        {
            if (!traitsCache.TryGetValue(key, out var info))
                traitsCache.Add(key, info = new TraitsFeature.CachedTestCaseInfo());
            return info;
        }

        public void UpdateCategoriesToVs()
        {
            if (categorylist.Any())
            {
                testCase.SetPropertyValue(
                    settings.VsTestCategoryType == VsTestCategoryType.NUnit
                        ? NUnitTestCategoryProperty
                        : null, categorylist.Distinct().ToArray());
            }
        }
    }
}