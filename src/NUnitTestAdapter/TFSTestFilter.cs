// ***********************************************************************
// Copyright (c) 2013 Charlie Poole
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter
{
    using System.Collections;

    public interface ITfsTestFilter
    {
        ITestCaseFilterExpression TfsTestCaseFilterExpression { get; }

        bool IsEmpty { get; }

        IEnumerable<TestCase> CheckFilter(IEnumerable<TestCase> tests);
    }

    public class TfsTestFilter : ITfsTestFilter
    {
        /// <summary>   
        /// Supported properties for filtering

        ///</summary>
        private static readonly Dictionary<string, TestProperty> SupportedPropertiesCache;
        private static readonly Dictionary<string, NTrait> SupportedTraitCache;
        private static readonly Dictionary<NTrait, TestProperty> TraitPropertyMap;
        private static readonly List<string> SupportedProperties;

        static TfsTestFilter()
        {
            // Initialize the property cache
            SupportedPropertiesCache = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase);
            SupportedPropertiesCache["FullyQualifiedName"] = TestCaseProperties.FullyQualifiedName;
            SupportedPropertiesCache["Name"] = TestCaseProperties.DisplayName;
            // Initialize the trait cache
            var priorityTrait = new NTrait("Priority", "");
            var categoryTrait = new NTrait("Category", "");
            SupportedTraitCache = new Dictionary<string, NTrait>(StringComparer.OrdinalIgnoreCase);
            SupportedTraitCache["Priority"] = priorityTrait;
            SupportedTraitCache["TestCategory"] = categoryTrait;
            // Initialize the trait property map, since TFS doesnt know about traits
            TraitPropertyMap = new Dictionary<NTrait, TestProperty>(new NTraitNameComparer());
            var priorityProperty = TestProperty.Find("Priority") ??
                      TestProperty.Register("Priority", "Priority", typeof(string), typeof(TestCase));
            TraitPropertyMap[priorityTrait] = priorityProperty;
            var categoryProperty = TestProperty.Find("TestCategory") ??
                        TestProperty.Register("TestCategory", "TestCategory", typeof(string), typeof(TestCase));
            TraitPropertyMap[categoryTrait] = categoryProperty;
            // Initialize a merged list of properties and traits to fool TFS Build to think traits is properties
            SupportedProperties = new List<string>();
            SupportedProperties.AddRange(SupportedPropertiesCache.Keys);
            SupportedProperties.AddRange(SupportedTraitCache.Keys);
        }

        private readonly IRunContext runContext;
        public TfsTestFilter(IRunContext runContext)
        {
            this.runContext = runContext;
        }


        private ITestCaseFilterExpression testCaseFilterExpression;
        public ITestCaseFilterExpression TfsTestCaseFilterExpression
        {
            get
            {
                return testCaseFilterExpression ??
                       (testCaseFilterExpression = runContext.GetTestCaseFilter(SupportedProperties, PropertyProvider));
            }
        }

        public bool IsEmpty
        {
            get { return TfsTestCaseFilterExpression == null || TfsTestCaseFilterExpression.TestCaseFilterValue == string.Empty; }
        }

        public IEnumerable<TestCase> CheckFilter(IEnumerable<TestCase> tests)
        {

            return TfsTestCaseFilterExpression == null ? tests : tests.Where(underTest => !TfsTestCaseFilterExpression.MatchTestCase(underTest, p => PropertyValueProvider(underTest, p)) == false).ToList();
        }

        /// <summary>    
        /// Provides value of TestProperty corresponding to property name 'propertyName' as used in filter.    
        /// /// Return value should be a string for single valued property or array of strings for multi valued property (e.g. TestCategory)   
        /// /// </summary>     
        public static object PropertyValueProvider(TestCase currentTest, string propertyName)
        {

            var testProperty = LocalPropertyProvider(propertyName);
            if (testProperty != null)
            {

                // Test case might not have defined this property. In that case GetPropertyValue()             
                // would return default value. For filtering, if property is not defined return null.           
                if (currentTest.Properties.Contains(testProperty))
                {
                    return currentTest.GetPropertyValue(testProperty);
                }
            }
            // Now it may be a trait, so we check the trait collection as well
            var testTrait = TraitProvider(propertyName);
            if (testTrait != null)
            {
                var val = traitContains(currentTest, testTrait.Name);
                if (val.Length == 0) return null;
                if (val.Length == 1) // Contains a single string
                    return val[0];  // return that string
                return val;  // otherwise return the whole array 
            }
            return null;
        }

        static readonly Func<TestCase, string, string[]> traitContains = TraitContains();

        /// <summary>
        /// TestCase:  To be checked
        /// traitName: Name of trait to be checked against
        /// </summary>
        /// <returns>Value of trait</returns>
        private static Func<TestCase, string, string[]> TraitContains()
        {

            return (testCase, traitName) =>
            {
                var testCaseType = typeof(TestCase);
                var property = testCaseType.GetProperty("Traits");
                if (property == null)
                    return null;
                var traits = property.GetValue(testCase, null) as IEnumerable;
                return (from object t in traits let name = t.GetType().GetProperty("Name").GetValue(t, null) as string where name == traitName select t.GetType().GetProperty("Value").GetValue(t, null) as string).ToArray();
            };
        }

        /// <summary>   
        /// Provides TestProperty for property name 'propertyName' as used in filter.    
        /// </summary>    
        public static TestProperty LocalPropertyProvider(string propertyName)
        {
            TestProperty testProperty;
            SupportedPropertiesCache.TryGetValue(propertyName, out testProperty);
            return testProperty;
        }

        public static TestProperty PropertyProvider(string propertyName)
        {
            var testProperty = LocalPropertyProvider(propertyName);
            if (testProperty != null)
            {
                return testProperty;
            }
            var testTrait = TraitProvider(propertyName);
            if (testTrait != null)
            {
                TestProperty tp;
                if (TraitPropertyMap.TryGetValue(testTrait, out tp))
                {
                    return tp;
                }
            }
            return null;
        }

        public static NTrait TraitProvider(string traitName)
        {
            NTrait testTrait;
            SupportedTraitCache.TryGetValue(traitName, out testTrait);
            return testTrait;
        }



    }

    public class NTraitNameComparer : IEqualityComparer<NTrait>
    {

        public bool Equals(NTrait n, NTrait y)
        {
            return n.Name == y.Name;
        }

        public int GetHashCode(NTrait obj)
        {
            return obj.Name.GetHashCode();
        }
    }
}
