// ***********************************************************************
// Copyright (c) 2020 Charlie Poole, Terje Sandstrom
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NSubstitute;
using NUnit.Engine;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Filtering;

public class NUnitTestFilterBuilderTests
{
    [Test]
    public void ThatConvertMsFilterToNUnitFilterHandlesNoTests()
    {
            var filterService = Substitute.For<ITestFilterService>();
            var settings = Substitute.For<IAdapterSettings>();
            settings.AssemblySelectLimit.Returns(2000);
            var sut = new NUnitTestFilterBuilder(filterService, settings, new LegacyTestFilterParser());
            var loadedTestCases = new List<TestCase>();
            var msFilter = Substitute.For<IVsTestFilter>();
            var results = sut.ConvertMsFilterToNUnitFilter(msFilter, loadedTestCases);
            Assert.That(results, Is.EqualTo(NUnitTestFilterBuilder.NoTestsFound));
        }

    [Test]
    public void ThatWhereFilterIsAdded()
    {
            var filterService = Substitute.For<ITestFilterService>();
            var settings = Substitute.For<IAdapterSettings>();
            settings.AssemblySelectLimit.Returns(2000);
            var sut = new NUnitTestFilterBuilder(filterService, settings, new LegacyTestFilterParser());
            string where = "name='abc'";
            var testFilterBuilder = Substitute.For<ITestFilterBuilder>();
            filterService.GetTestFilterBuilder().Returns(testFilterBuilder);
            sut.FilterByWhere(where);
            testFilterBuilder.Received().SelectWhere(Arg.Is<string>(x => x == where));
        }

    [Test]
    public void ThatTheGivenParserReadsTheFilter()
    {
        var settings = Substitute.For<IAdapterSettings>();
        settings.ExplicitMode.Returns(ExplicitModeEnum.Strict);
        var parser = Substitute.For<ITestFilterParser>();
        parser.Parse("Custom").Returns(NUnitTestFilterBuilder.NameFilter(["A.B"]));
        var vsFilter = Substitute.For<IVsTestFilter>();
        vsFilter.MsTestCaseFilterExpression.TestCaseFilterValue.Returns("Custom");
        var sut = new NUnitTestFilterBuilder(Substitute.For<ITestFilterService>(), settings, parser);

        var result = sut.ConvertVsTestFilterToNUnitFilter(vsFilter);

        Assert.That(result.Text, Is.EqualTo("<filter><test>A.B</test></filter>"));
    }
}
