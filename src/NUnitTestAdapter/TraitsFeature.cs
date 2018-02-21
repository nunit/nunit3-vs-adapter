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

using System.Collections.Generic;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    public static class TraitsFeature
    {
        public static void AddTraitsFromTestNode(this TestCase testCase, XmlNode testNode, IDictionary<string, TestTraitInfo> traitsCache)
        {
            var combinedTraitInfo = BuildTestTraitInfo(traitsCache, testNode);

            combinedTraitInfo.ApplyTo(testCase);
        }

        private static TestTraitInfo BuildTestTraitInfo(IDictionary<string, TestTraitInfo> traitsCache, XmlNode testNode)
        {
            var currentNodeInfo = ParseNodeTraits(testNode);

            var parentId = testNode.ParentNode?.Attributes?["id"]?.Value;
            if (parentId == null) return currentNodeInfo;

            if (!traitsCache.TryGetValue(parentId, out var combinedAncestorInfo))
                traitsCache.Add(parentId, combinedAncestorInfo = BuildTestTraitInfo(traitsCache, testNode.ParentNode));

            return TestTraitInfo.Combine(combinedAncestorInfo, currentNodeInfo);
        }

        private static TestTraitInfo ParseNodeTraits(XmlNode testNode)
        {
            var nodelist = testNode.SelectNodes("properties/property");

            var traits = new List<Trait>();
            var categories = new List<string>();

            foreach (XmlNode propertyNode in nodelist)
            {
                string propertyName = propertyNode.GetAttribute("name");
                string propertyValue = propertyNode.GetAttribute("value");

                if (IsInternalProperty(propertyName, propertyValue)) continue;

                if (propertyName == "Category")
                {
                    if (!categories.Contains(propertyValue))
                        categories.Add(propertyValue);
                }
                else
                {
                    traits.Add(new Trait(propertyName, propertyValue));
                }
            }

            return new TestTraitInfo(traits.ToArray(), categories.ToArray());
        }

        private static bool IsInternalProperty(string propertyName, string propertyValue)
        {
            // Property names starting with '_' are for internal use only
            return string.IsNullOrEmpty(propertyName) || propertyName[0] == '_' || string.IsNullOrEmpty(propertyValue);
        }
    }
}
