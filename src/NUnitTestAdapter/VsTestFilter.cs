// ***********************************************************************
// Copyright (c) 2013-2021 Charlie Poole, Terje Sandstrom
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
    // ReSharper disable once RedundantUsingDirective
    using System.Reflection;  // Needed for .net core 2.1
    using Internal;  // Needed for reflection
    using TestFilterConverter;

    public interface IVsTestFilter
    {
        ITestCaseFilterExpression TfsTestCaseFilterExpression { get; }

        bool IsEmpty { get; }

        IEnumerable<TestCase> CheckFilter(IEnumerable<TestCase> tests);
    }

    public abstract class VsTestFilter : IVsTestFilter
    {
        /// <summary>
        /// Supported properties for filtering.
        /// </summary>
        private static readonly Dictionary<string, TestProperty> SupportedPropertiesCache;
        private static readonly Dictionary<string, NTrait> SupportedTraitCache;
        private static readonly Dictionary<NTrait, TestProperty> TraitPropertyMap;
        private static readonly List<string> SupportedProperties;

        static VsTestFilter()
        {
            // Initialize the property cache
            SupportedPropertiesCache = new Dictionary<string, TestProperty>(StringComparer.OrdinalIgnoreCase)
            {
                ["FullyQualifiedName"] = TestCaseProperties.FullyQualifiedName,
                ["Name"] = TestCaseProperties.DisplayName,
                ["TestCategory"] = CategoryList.NUnitTestCategoryProperty,
                ["Category"] = CategoryList.NUnitTestCategoryProperty,
            };
            // Initialize the trait cache
            var priorityTrait = new NTrait("Priority", "");
            var categoryTrait = new NTrait("Category", "");
            SupportedTraitCache = new Dictionary<string, NTrait>(StringComparer.OrdinalIgnoreCase)
            {
                ["Priority"] = priorityTrait,
                ["TestCategory"] = categoryTrait,
                ["Category"] = categoryTrait
            };
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

        protected VsTestFilter(IRunContext runContext)
        {
            this.runContext = runContext;
        }


        private ITestCaseFilterExpression testCaseFilterExpression;
        public ITestCaseFilterExpression TfsTestCaseFilterExpression =>
            testCaseFilterExpression ??= runContext.GetTestCaseFilter(SupportedProperties, PropertyProvider);

        public bool IsEmpty => TfsTestCaseFilterExpression == null || TfsTestCaseFilterExpression.TestCaseFilterValue == string.Empty;

        public IEnumerable<TestCase> CheckFilter(IEnumerable<TestCase> tests)
        {
            return tests.Where(CheckFilter).ToList();
        }

        protected abstract bool CheckFilter(TestCase testCase);

        /// <summary>
        /// Provides value of TestProperty corresponding to property name 'propertyName' as used in filter.
        /// Return value should be a string for single valued property or array of strings for multi valued property (e.g. TestCategory).
        /// </summary>
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
                var val = CachedTraitContainsDelegate(currentTest, testTrait.Name);
                if (val.Length == 0) return null;
                if (val.Length == 1) // Contains a single string
                    return val[0];  // return that string
                return val;  // otherwise return the whole array
            }
            return null;
        }

        static readonly Func<TestCase, string, string[]> CachedTraitContainsDelegate = TraitContains();

        /// <summary>
        /// TestCase:  To be checked
        /// traitName: Name of trait to be checked against.
        /// </summary>
        /// <returns>Value of trait.</returns>
        private static Func<TestCase, string, string[]> TraitContains()
        {
            return (testCase, traitName) =>
            {
                var testCaseType = typeof(TestCase);
                var property = testCaseType.GetTypeInfo().GetProperty("Traits");
                if (property == null)
                    return null;
                var traits = property.GetValue(testCase, null) as IEnumerable;
                return (from object t in traits let name = t.GetType().GetTypeInfo().GetProperty("Name").GetValue(t, null) as string where name == traitName select t.GetType().GetProperty("Value").GetValue(t, null) as string).ToArray();
            };
        }

        /// <summary>
        /// Provides TestProperty for property name 'propertyName' as used in filter.
        /// </summary>
        public static TestProperty LocalPropertyProvider(string propertyName)
        {
            SupportedPropertiesCache.TryGetValue(propertyName, out var testProperty);
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
            return testTrait == null ? null : TraitPropertyMap.TryGetValue(testTrait, out var tp) ? tp : null;
        }

        public static NTrait TraitProvider(string traitName)
        {
            SupportedTraitCache.TryGetValue(traitName, out var testTrait);
            return testTrait;
        }
    }

    public static class VsTestFilterFactory
    {
        public static VsTestFilter CreateVsTestFilter(IAdapterSettings settings, IRunContext context) =>
            settings.DiscoveryMethod == DiscoveryMethod.Legacy
                ? new VsTestFilterLegacy(context) :
                settings.DesignMode
                    ? new VsTestFilterIde(context)
                    : new VsTestFilterNonIde(context);
    }

    public class VsTestFilterLegacy : VsTestFilter
    {
        public VsTestFilterLegacy(IRunContext runContext) : base(runContext)
        {
        }

        protected override bool CheckFilter(TestCase testCase)
        {
            var isExplicit = testCase.GetPropertyValue(CategoryList.NUnitExplicitProperty, false);

            return !isExplicit && TfsTestCaseFilterExpression?.MatchTestCase(testCase, p => PropertyValueProvider(testCase, p)) != false;
        }
    }

    public class VsTestFilterIde : VsTestFilter
    {
        public VsTestFilterIde(IRunContext runContext) : base(runContext)
        {
        }

        protected override bool CheckFilter(TestCase testCase)
        {
            var isExplicit = testCase.GetPropertyValue(CategoryList.NUnitExplicitProperty, false);

            return !isExplicit && TfsTestCaseFilterExpression?.MatchTestCase(testCase, p => PropertyValueProvider(testCase, p)) != false;
        }
    }

    public class VsTestFilterNonIde : VsTestFilter
    {
        public VsTestFilterNonIde(IRunContext runContext) : base(runContext)
        {
        }

        protected override bool CheckFilter(TestCase testCase)
        {
            return TfsTestCaseFilterExpression?.MatchTestCase(testCase, p => PropertyValueProvider(testCase, p)) != false;
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
