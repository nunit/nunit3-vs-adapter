using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public class CategoryList
    {
        public const string NUnitCategoryName = "NUnit.TestCategory";
        private const string NunitTestCategoryLabel = "Category";
        private const string VsTestCategoryLabel = "TestCategory";
        internal static readonly TestProperty NUnitTestCategoryProperty = TestProperty.Register(NUnitCategoryName, VsTestCategoryLabel, typeof(string[]), TestPropertyAttributes.Hidden | TestPropertyAttributes.Trait, typeof(TestCase));

        internal static readonly TestProperty NUnitExplicitProperty = TestProperty.Register("NUnit.Explicit", "Explicit", typeof(bool), TestPropertyAttributes.Hidden, typeof(TestCase));

        private const string ExplicitTraitName = "Explicit";
        // The empty string causes the UI we want.
        // If it's null, the explicit trait doesn't show up in Test Explorer.
        // If it's not empty, it shows up as “Explicit [value]” in Test Explorer.
        private const string ExplicitTraitValue = "";

        private readonly List<string> categorylist = new List<string>();
        private readonly TestCase testCase;

        public CategoryList(TestCase testCase)
        {
            this.testCase = testCase;
        }

        public void AddRange(IEnumerable<string> categories)
        {
            categorylist.AddRange(categories);
        }

        public int LastNodeListCount { get; private set; }
        public IEnumerable<string> ProcessTestCaseProperties(XmlNode testNode, bool addToCache, string key = null, IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache = null)
        {
            var nodelist = testNode.SelectNodes("properties/property");
            LastNodeListCount = nodelist.Count;
            foreach (XmlNode propertyNode in nodelist)
            {
                string propertyName = propertyNode.GetAttribute("name");
                string propertyValue = propertyNode.GetAttribute("value");
                if (addToCache)
                    AddTraitsToCache(traitsCache, key, propertyName, propertyValue);
                if (!IsInternalProperty(propertyName, propertyValue))
                {
                    if (propertyName != NunitTestCategoryLabel)
                        testCase.Traits.Add(new Trait(propertyName, propertyValue));
                    else
                    {
                        categorylist.Add(propertyValue);
                    }
                }
            }

            if (testNode.Attributes?["runstate"]?.Value == "Explicit")
            {
                // Add UI grouping “Explicit”
                if (!testCase.Traits.Any(trait => trait.Name == ExplicitTraitName))
                    testCase.Traits.Add(new Trait(ExplicitTraitName, ExplicitTraitValue));

                // Track whether the test is actually explicit since multiple things result in the same UI grouping
                testCase.SetPropertyValue(NUnitExplicitProperty, true);

                if (addToCache)
                {
                    // Add UI grouping “Explicit”
                    AddTraitsToCache(traitsCache, key, ExplicitTraitName, ExplicitTraitValue);

                    // Track whether the test is actually explicit since multiple things result in the same UI grouping
                    GetCachedInfo(traitsCache, key).Explicit = true;
                }
            }

            return categorylist;
        }

        private readonly string[] internalProperties = { "description","timeout","repeat" };
        private bool IsInternalProperty(string propertyName, string propertyValue)
        {
            
            if (propertyName == ExplicitTraitName)
            {
                // Otherwise the IsNullOrEmpty check does the wrong thing,
                // but I'm not sure of the consequences of allowing all empty strings.
                return false;
            }
            if ( internalProperties.Contains(propertyName.ToLower()))
                return true;

            // Property names starting with '_' are for internal use only
            return string.IsNullOrEmpty(propertyName) || propertyName[0] == '_' || string.IsNullOrEmpty(propertyValue);
        }

        private void AddTraitsToCache(IDictionary<string, TraitsFeature.CachedTestCaseInfo> traitsCache, string key, string propertyName, string propertyValue)
        {
            if (IsInternalProperty(propertyName, propertyValue)) return;

            var info = GetCachedInfo(traitsCache, key);
            info.Traits.Add(new Trait(propertyName, propertyValue));
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
                testCase.SetPropertyValue(NUnitTestCategoryProperty, categorylist.Distinct().ToArray());
            }
        }



    }
}