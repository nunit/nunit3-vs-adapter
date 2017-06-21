// ***********************************************************************
// Copyright (c) 2013-2015 Charlie Poole, Terje Sandstrom
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
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public static class TraitsFeature
    {
        private static readonly PropertyInfo TraitsProperty;
        private static readonly MethodInfo TraitsCollectionAdd;
        private static readonly PropertyInfo NameProperty;
        private static readonly PropertyInfo ValueProperty;

        static TraitsFeature()
        {
            TraitsProperty = typeof(TestCase).GetProperty("Traits");
            if (TraitsProperty != null)
            {

                var traitsCollectionType = TraitsProperty.PropertyType;
                TraitsCollectionAdd = traitsCollectionType.GetMethod("Add", new[] { typeof(string), typeof(string) });

                var traitType = Type.GetType("Microsoft.VisualStudio.TestPlatform.ObjectModel.Trait,Microsoft.VisualStudio.TestPlatform.ObjectModel");
                if (traitType != null)
                {
                    NameProperty = traitType.GetProperty("Name");
                    ValueProperty = traitType.GetProperty("Value");
                }
            }

            IsSupported = TraitsProperty != null && NameProperty != null && ValueProperty != null;
        }

        public static bool IsSupported { get; private set; }

        public static void AddTrait(this TestCase testCase, string name, string value)
        {
            if (TraitsCollectionAdd != null)
            {
                object traitsCollection = TraitsProperty.GetValue(testCase, new object[0]);
                TraitsCollectionAdd.Invoke(traitsCollection, new object[] { name, value });
            }
        }

        public static void AddTraitsFromTestNode(this TestCase testCase, XmlNode testNode)
        {
            if (IsSupported)
                AddTraitsFromTestNode(testNode, TraitsProperty.GetValue(testCase, new object[0]));
        }

        private static void AddTraitsFromTestNode(XmlNode test, object traitsCollection)
        {
            if (test.ParentNode != null)
                AddTraitsFromTestNode(test.ParentNode, traitsCollection);

            foreach (XmlNode propertyNode in test.SelectNodes("properties/property"))
            {
                string propertyName = propertyNode.GetAttribute("name");
                string propertyValue = propertyNode.GetAttribute("value");

                // Property names starting with '_' are for internal use only
                if (!string.IsNullOrEmpty(propertyName) && propertyName[0] != '_' && !string.IsNullOrEmpty(propertyValue))
                {
                    TraitsCollectionAdd.Invoke(traitsCollection, new object[] { propertyName, propertyValue });
                }
            }
        }

        public static IEnumerable<NTrait> GetTraits(this TestCase testCase)
        {
            var traits = new List<NTrait>();

            if (IsSupported)
            {
                var traitsCollection = TraitsProperty.GetValue(testCase, new object[0]) as IEnumerable<object>;

                if (traitsCollection != null)
                    traits.AddRange(from traitObject in traitsCollection let name = NameProperty.GetValue(traitObject, new object[0]) as string let value = ValueProperty.GetValue(traitObject, new object[0]) as string select new NTrait(name, value));
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
            Name = name;
            Value = value;
        }
    }
}
