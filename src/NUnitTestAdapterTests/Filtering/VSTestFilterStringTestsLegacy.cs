// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Filtering
{
    public static class VSTestFilterStringTestsLegacy
    {
        [TestCase(null, new[] { "NonExplicitParent.NonExplicitTest" })]
        [TestCase("", new[] { "NonExplicitParent.NonExplicitTest" })]
        [TestCase("TestCategory = CategoryThatMatchesNothing", new string[0])]
        [TestCase("MeaninglessName", new string[0])]
        [TestCase("TestCategory != CategoryThatMatchesNothing", new[] { "NonExplicitParent.NonExplicitTest" })]
        [TestCase("TestCategory = SomeCat", new[] { "NonExplicitParent.NonExplicitTest" })]
        [TestCase("TestCategory != SomeCat", new string[0])]
        public static void NoFiltersIncludeExplicitTests(string vsTestFilterString, IReadOnlyCollection<string> nonExplicitTestIds)
        {
            var result = ApplyFilter(vsTestFilterString, @"
                <test-suite id='1' name='NonExplicitParent' fullname='NonExplicitParent'>
                    <test-case id='2' name='NonExplicitTest' fullname='NonExplicitParent.NonExplicitTest'>
                        <properties>
                            <property name='Category' value='SomeCat' />
                        </properties>
                    </test-case>
                    <test-case id='3' name='ExplicitTest' fullname='NonExplicitParent.ExplicitTest' runstate='Explicit'>
                        <properties>
                            <property name='Category' value='SomeCat' />
                        </properties>
                    </test-case>
                </test-suite>
                <test-suite id='4' name='ExplicitParent' fullname='ExplicitParent' runstate='Explicit'>
                    <test-case id='5' name='NonExplicitTest' fullname='ExplicitParent.NonExplicitTest'>
                        <properties>
                            <property name='Category' value='SomeCat' />
                        </properties>
                    </test-case>
                </test-suite>");

            Assert.That(from test in result select test.FullyQualifiedName, Is.EquivalentTo(nonExplicitTestIds));
        }

        private static IEnumerable<TestCase> ApplyFilter(string vsTestFilterString, string testCasesXml)
        {
            var filter = FilteringTestUtils.CreateTestFilter(string.IsNullOrEmpty(vsTestFilterString) ? null :
                FilteringTestUtils.CreateVSTestFilterExpression(vsTestFilterString));

            return filter.CheckFilter(TestCaseUtils.ConvertTestCases(testCasesXml));
        }
    }
}
