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

        public static void AddTrait(this TestCase testCase, string name, string value)
        {
            if(testCase != null)
            {
                testCase.Traits.Add(new Trait(name, value));
            }
        }

        public static void AddTraitsFromTestNode(this TestCase testCase, XmlNode test)
        {
            if (test.ParentNode != null)
                AddTraitsFromTestNode(testCase, test.ParentNode);

            foreach (XmlNode propertyNode in test.SelectNodes("properties/property"))
            {
                string propertyName = propertyNode.GetAttribute("name");
                string propertyValue = propertyNode.GetAttribute("value");

                // Property names starting with '_' are for internal use only
                if (!string.IsNullOrEmpty(propertyName) && propertyName[0] != '_' && !string.IsNullOrEmpty(propertyValue))
                {
                    AddTrait(testCase, propertyName, propertyValue);
                }
            }
        }

        public static IEnumerable<NTrait> GetTraits(this TestCase testCase)
        {
            var traits = new List<NTrait>();

            if (testCase != null && testCase.Traits != null)
            {
                traits.AddRange(from trait in testCase.Traits let name = trait.Name let value = trait.Value select new NTrait(name, value));
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
