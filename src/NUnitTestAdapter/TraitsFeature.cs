// ****************************************************************
// Copyright (c) 2013 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;

namespace NUnit.VisualStudio.TestAdapter
{
    public static class TraitsFeature
    {
        private static readonly PropertyInfo traitsProperty;
        private static readonly MethodInfo traitsCollectionAdd;
        private static readonly PropertyInfo nameProperty;
        private static readonly PropertyInfo valueProperty;

        static TraitsFeature()
        {
            traitsProperty = typeof(TestCase).GetProperty("Traits");
            if (traitsProperty != null)
            {

                var traitsCollectionType = traitsProperty.PropertyType;
                traitsCollectionAdd = traitsCollectionType.GetMethod("Add", new Type[] { typeof(string), typeof(string) });

                var traitType = Type.GetType("Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait,Microsoft.VisualStudio.TestPlatform.ObjectModel");
                if (traitType != null)
                {
                    nameProperty = traitType.GetProperty("Name");
                    valueProperty = traitType.GetProperty("Value");
                }
            }

            IsSupported = traitsProperty != null && nameProperty != null && valueProperty != null;
        }

        public static bool IsSupported { get; private set; }

        public static void AddTrait(this TestCase testCase, string name, string value)
        {
            if (traitsCollectionAdd != null)
            {
                object traitsCollection = traitsProperty.GetValue(testCase, new object[0]);
                traitsCollectionAdd.Invoke(traitsCollection, new object[] { name, value });
            }
        }

        public static void AddTraitsFromNUnitTest(this TestCase testCase, ITest nunitTest)
        {
            if (IsSupported)
                AddTraitsFromNUnitTest(nunitTest, traitsProperty.GetValue(testCase, new object[0]));
        }

        private static void AddTraitsFromNUnitTest(ITest test, object traitsCollection)
        {
            if (test.Parent != null)
                AddTraitsFromNUnitTest(test.Parent, traitsCollection);

            foreach (string propertyName in test.Properties.Keys)
            {
                object propertyValue = test.Properties[propertyName];

                if (propertyName == "_CATEGORIES")
                {
                    var categories = propertyValue as System.Collections.IEnumerable;
                    if (categories != null)
                        foreach (string category in categories)
                            traitsCollectionAdd.Invoke(traitsCollection, new object[] { "Category", category });
                }
                else if (propertyName[0] != '_') // internal use only
                    traitsCollectionAdd.Invoke(traitsCollection, new object[] { propertyName, propertyValue.ToString() });
            }
        }

        public static IEnumerable<NTrait> GetTraits(this TestCase testCase)
        {
            var traits = new List<NTrait>();

            if (IsSupported)
            {
                var traitsCollection = traitsProperty.GetValue(testCase, new object[0]) as IEnumerable<object>;

                foreach (object traitObject in traitsCollection)
                {
                    var name = nameProperty.GetValue(traitObject, new object[0]) as string;
                    var value = valueProperty.GetValue(traitObject, new object[0]) as string;
                    traits.Add(new NTrait(name, value));
                }
            }

            return traits;
        }
    }

    public class NTrait
    {
        public string Name { get; private set; }
        public string Value { get; private set; }

        public NTrait(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}
