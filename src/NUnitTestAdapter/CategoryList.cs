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
        public IEnumerable<string> ProcessTestCaseProperties(XmlNode testNode, bool addToCache, string key = null, IDictionary<string, List<Trait>> traitsCache = null)
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

            const string explicitTraitName = "Explicit";
            if (!categorylist.Contains(explicitTraitName) && testNode.Attributes?["runstate"]?.Value == "Explicit")
            {
                categorylist.Add(explicitTraitName);
            }

            return categorylist;
        }

        private static bool IsInternalProperty(string propertyName, string propertyValue)
        {
            // Property names starting with '_' are for internal use only
            return String.IsNullOrEmpty(propertyName) || propertyName[0] == '_' || String.IsNullOrEmpty(propertyValue);
        }

        private static void AddTraitsToCache(IDictionary<string, List<Trait>> traitsCache, string key, string propertyName, string propertyValue)
        {
            if (traitsCache.ContainsKey(key))
            {
                if (!IsInternalProperty(propertyName, propertyValue))
                    traitsCache[key].Add(new Trait(propertyName, propertyValue));
                return;
            }

            var traits = new List<Trait>();

            // Will add empty list of traits, if the property is internal type. So that we will not make SelectNodes call again.
            if (!IsInternalProperty(propertyName, propertyValue))
            {
                traits.Add(new Trait(propertyName, propertyValue));
            }
            traitsCache[key] = traits;
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