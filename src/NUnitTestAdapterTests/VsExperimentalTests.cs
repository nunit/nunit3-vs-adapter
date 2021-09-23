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
// ***********************************************************************using System;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    /// <summary>
    /// Experimental tests used to deduce functionality of VSTest.
    /// </summary>
    public class VsExperimentalTests
    {
        [Test]
        public void ThatCategoriesAreDistinct()
        {
            var testCase = new TestCase(
                "whatever",
                new Uri(NUnitTestAdapter.ExecutorUri),
                "someassemblyname")
            {
                DisplayName = nameof(ThatCategoriesAreDistinct),
                CodeFilePath = null,
                LineNumber = 0
            };
            var settings = Substitute.For<IAdapterSettings>();
            settings.VsTestCategoryType.Returns(VsTestCategoryType.NUnit);
            var cl = new CategoryList(testCase, settings);
            cl.AddRange(new List<string> { "one", "one", "two", "two" });
            cl.UpdateCategoriesToVs();

            var returnedCategoryList = testCase.GetCategories();
            Assert.That(returnedCategoryList.Count(), Is.EqualTo(2), $"Found {testCase.GetCategories().Count()} category entries");
        }
    }
}
