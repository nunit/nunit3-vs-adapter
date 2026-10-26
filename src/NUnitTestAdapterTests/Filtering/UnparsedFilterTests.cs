// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, Terje Sandstrom
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
using Microsoft.VisualStudio.TestPlatform.Common.Filtering;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.Filtering;

/// <summary>
/// VSTest rejects values with unescaped parentheses. The adapter keeps the filter string and parses it itself.
/// </summary>
public class UnparsedFilterTests
{
    // Issue 623, an Azure DevOps rerun, and issue 677, dotnet test --filter.
    [TestCase(
        "FullyQualifiedName=Company.Tests.MarketplaceTesting(\"Chrome\").SubmitItemforSaleWithEmptyValues",
        "<filter><test>Company.Tests.MarketplaceTesting(&quot;Chrome&quot;).SubmitItemforSaleWithEmptyValues</test></filter>",
        TestName = "Issue623")]
    [TestCase(
        "FullyQualifiedName=TestFixtureSourceError.TestsBroken(Windows Edgium).Test1",
        "<filter><test>TestFixtureSourceError.TestsBroken(Windows Edgium).Test1</test></filter>",
        TestName = "Issue677")]
    [TestCase(
        """FullyQualifiedName=TestNunit.StupidTest.Check("v1"\,True\,"C2")""",
        "<filter><test>TestNunit.StupidTest.Check(&quot;v1&quot;,True,&quot;C2&quot;)</test></filter>",
        TestName = "Issue817_AzureDevOpsRerunEscapesCommas")]
    [TestCase(
        "FullyQualifiedName=Ns.Fixture.Method(1)|FullyQualifiedName=Ns.Fixture.Method(2)",
        "<filter><or><test>Ns.Fixture.Method(1)</test><test>Ns.Fixture.Method(2)</test></or></filter>",
        TestName = "TwoTestCases")]
    public void FilterRejectedByVSTestIsParsedByTheAdapter(string filter, string expected)
    {
        var vsFilter = CreateFilterRejectedByVSTest(filter);

        Assert.That(vsFilter.IsEmpty, Is.False);
        Assert.That(new TestFilterParser().Parse(vsFilter.MsTestCaseFilterExpression.TestCaseFilterValue), Is.EqualTo(expected));
    }

    [Test]
    public void MatchingATestCaseThrowsTheVSTestParseError()
    {
        var vsFilter = CreateFilterRejectedByVSTest("FullyQualifiedName=Ns.Fixture.Method(1)");

        var testCase = new TestCase("Ns.Fixture.Method(1)", new Uri("executor://test"), "test.dll");

        Assert.That(() => vsFilter.CheckFilter([testCase]), Throws.TypeOf<TestPlatformFormatException>());
    }

    [Test]
    public void FilterWithoutTextIsNotKept()
    {
        var context = Substitute.For<IRunContext>();
        context.GetTestCaseFilter(null, null).ReturnsForAnyArgs(_ => throw new TestPlatformFormatException("error"));
        var vsFilter = VsTestFilterFactory.CreateVsTestFilter(Substitute.For<IAdapterSettings>(), context);

        Assert.That(() => vsFilter.IsEmpty, Throws.TypeOf<TestPlatformFormatException>());
    }

    [Test]
    public void DiscoveryKeepsTheFilterRejectedByVSTest()
    {
        const string filter = "FullyQualifiedName=TestFixtureSourceError.TestsBroken(Windows Edgium).Test1";

        var vsFilter = new VsTestFilterForDiscovery(new DiscoveryContextRejectingTheFilter(filter), null);

        Assert.That(vsFilter.MsTestCaseFilterExpression, Is.TypeOf<UnparsedFilterExpression>());
        Assert.That(vsFilter.MsTestCaseFilterExpression.TestCaseFilterValue, Is.EqualTo(filter));
    }

    /// <summary>
    /// A run context that throws as VSTest and the MTP bridge do when their parser rejects the filter.
    /// </summary>
    private static VsTestFilter CreateFilterRejectedByVSTest(string filter)
    {
        var wrapper = new FilterExpressionWrapper(filter);
        Assert.That(wrapper.ParseError, Is.Not.Empty, "VSTest should reject this filter");
        var context = Substitute.For<IRunContext>();
        context.GetTestCaseFilter(null, null).ReturnsForAnyArgs(_ => throw new TestPlatformFormatException(wrapper.ParseError, wrapper.FilterString));
        return VsTestFilterFactory.CreateVsTestFilter(Substitute.For<IAdapterSettings>(), context);
    }

    /// <summary>
    /// A discovery context like VSTest's: GetTestCaseFilter is found by reflection, and throws for this filter.
    /// </summary>
    private sealed class DiscoveryContextRejectingTheFilter(string filter) : IDiscoveryContext
    {
        public IRunSettings RunSettings => null;

        public ITestCaseFilterExpression GetTestCaseFilter(IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider)
        {
            var wrapper = new FilterExpressionWrapper(filter);
            throw new TestPlatformFormatException(wrapper.ParseError, wrapper.FilterString);
        }
    }
}
