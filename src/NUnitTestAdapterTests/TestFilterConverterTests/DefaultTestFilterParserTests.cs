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
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests;

public class DefaultTestFilterParserTests
{
    private const string Filter = "Name=A";

    [Test]
    public void FirstParserThatAcceptsTheFilterIsUsed()
    {
        var expected = NUnitTestFilterBuilder.NameFilter(["A"]);
        var first = Substitute.For<ITestFilterParser>();
        first.Parse(Filter).Returns(expected);
        var second = Substitute.For<ITestFilterParser>();

        var result = new DefaultTestFilterParser([first, second]).Parse(Filter);

        Assert.That(result, Is.SameAs(expected));
        second.DidNotReceiveWithAnyArgs().Parse(default);
    }

    [Test]
    public void RejectedFilterGoesToTheNextParserAndIsLogged()
    {
        var expected = NUnitTestFilterBuilder.NameFilter(["A"]);
        var first = Substitute.For<ITestFilterParser>();
        first.Parse(Filter).Returns(_ => throw new TestFilterParserException("first error"));
        var second = Substitute.For<ITestFilterParser>();
        second.Parse(Filter).Returns(expected);
        var log = Substitute.For<ITestLogger>();

        var result = new DefaultTestFilterParser([first, second], log).Parse(Filter);

        Assert.That(result, Is.SameAs(expected));
        log.Received().Info(Arg.Is<string>(message => message.Contains("first error") && message.Contains(Filter)));
    }

    [Test]
    public void WhenEveryParserRejectsTheFilterTheLastErrorIsThrown()
    {
        var first = Substitute.For<ITestFilterParser>();
        first.Parse(Filter).Returns(_ => throw new TestFilterParserException("first error"));
        var second = Substitute.For<ITestFilterParser>();
        second.Parse(Filter).Returns(_ => throw new TestFilterParserException("second error"));

        Assert.That(
            () => new DefaultTestFilterParser([first, second]).Parse(Filter),
            Throws.TypeOf<TestFilterParserException>().With.Message.EqualTo("second error"));
    }

    [Test]
    public void ParserErrorsOtherThanRejectionAreNotCaught()
    {
        var first = Substitute.For<ITestFilterParser>();
        first.Parse(Filter).Returns(_ => throw new InvalidOperationException("bug"));
        var second = Substitute.For<ITestFilterParser>();

        Assert.That(() => new DefaultTestFilterParser([first, second]).Parse(Filter), Throws.InvalidOperationException);
    }

    [Test]
    public void NeedsAtLeastOneParser()
    {
        Assert.That(() => new DefaultTestFilterParser([]), Throws.ArgumentException);
    }

    [TestCase("FullyQualifiedName=A.B(1,2)", "<filter><test>A.B(1,2)</test></filter>")]
    [TestCase("!(Name=A)", "<filter><not><name>A</name></not></filter>")]
    public void FilterTheVSTestParserRejectsGetsTheLegacyFilter(string filter, string expected)
    {
        Assert.That(new DefaultTestFilterParser().Parse(filter).ToNUnitXml(), Is.EqualTo(expected));
    }

    [Test]
    public void FilterNeitherParserAcceptsGivesTheLegacyError()
    {
        Assert.That(
            () => new DefaultTestFilterParser().Parse(@"Name=a\b"),
            Throws.TypeOf<TestFilterParserException>().With.Message.Contains("does not escape"));
    }

    [TestCase(FilterParserType.Default, typeof(DefaultTestFilterParser))]
    [TestCase(FilterParserType.VSTest, typeof(VSTestFilterParser))]
    [TestCase(FilterParserType.Legacy, typeof(LegacyTestFilterParser))]
    public void SettingSelectsTheParser(FilterParserType setting, Type expected)
    {
        Assert.That(NUnitTestAdapter.CreateFilterParser(setting, null), Is.TypeOf(expected));
    }
}
