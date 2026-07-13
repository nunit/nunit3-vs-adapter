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

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NSubstitute;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests.Filtering;

public static class FilteringTestUtils
{
    public static ITestCaseFilterExpression CreateVSTestFilterExpression(string filter)
    {
        // FilterExpressionWrapper and TestCaseFilterExpression come from the source-only
        // Microsoft.TestPlatform.Filter.Source package (compiled into this test assembly). This is the
        // same package Microsoft.Testing.Extensions.VSTestBridge uses to parse filters, so the tests build
        // the filter expression exactly as the product does.
        //
        // Outside the vstest repo that package compiles TestCaseFilterExpression as an internal type whose
        // MatchTestCase takes only the property value provider and which does NOT implement the public
        // ITestCaseFilterExpression. So we wrap it here, mirroring the adapter the bridge itself uses
        // (VSTestBridge's BridgeFilterExpression / MSTest's MSTestFilterExpression).
        var testCaseFilterExpression = new TestCaseFilterExpression(new FilterExpressionWrapper(filter));
        return new BridgeFilterExpression(testCaseFilterExpression);
    }

    private sealed class BridgeFilterExpression : ITestCaseFilterExpression
    {
        private readonly TestCaseFilterExpression _testCaseFilterExpression;

        public BridgeFilterExpression(TestCaseFilterExpression testCaseFilterExpression)
            => _testCaseFilterExpression = testCaseFilterExpression;

        public string TestCaseFilterValue => _testCaseFilterExpression.TestCaseFilterValue;

        public bool MatchTestCase(TestCase testCase, Func<string, object> propertyValueProvider)
            => _testCaseFilterExpression.MatchTestCase(propertyValueProvider);
    }

    public static VsTestFilter CreateTestFilter(ITestCaseFilterExpression filterExpression)
    {
        var context = Substitute.For<IRunContext>();
        context.GetTestCaseFilter(null, null).ReturnsForAnyArgs(filterExpression);
        var settings = Substitute.For<IAdapterSettings>();
        settings.DiscoveryMethod.Returns(DiscoveryMethod.Legacy);
        return VsTestFilterFactory.CreateVsTestFilter(settings, context);
    }

    public static void AssertExpectedResult(ITestCaseFilterExpression filterExpression, IReadOnlyCollection<TestCase> testCases, IReadOnlyCollection<string> expectedMatchingTestNames)
    {
        var matchingTestCases = CreateTestFilter(filterExpression).CheckFilter(testCases);

        Assert.That(matchingTestCases.Select(t => t.FullyQualifiedName), Is.EquivalentTo(expectedMatchingTestNames));
    }
}