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

using System;
using System.Xml;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestFilterConverter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestFilterConverterTests
{
    public class TestFilterParserTests
    {
        private TestFilterParser _parser;

        [SetUp]
        public void CreateParser()
        {
            _parser = new TestFilterParser();
        }

        // Default
        [TestCase(
            "Method",
            "<test re='1'>Method</test>")]

        // Test Category
        [TestCase("TestCategory=Urgent", "<cat>Urgent</cat>")]
        [TestCase("TestCategory!=Urgent", "<not><cat>Urgent</cat></not>")]
        [TestCase("TestCategory ~ Urgent", "<cat re='1'>Urgent</cat>")]
        [TestCase("TestCategory !~ Urgent", "<not><cat re='1'>Urgent</cat></not>")]

        // Priority
        [TestCase("Priority = High", "<prop name='Priority'>High</prop>")]
        [TestCase("Priority != Urgent", "<not><prop name='Priority'>Urgent</prop></not>")]
        [TestCase("Priority ~ Normal", "<prop name='Priority' re='1'>Normal</prop>")]
        [TestCase("Priority !~ Low", "<not><prop name='Priority' re='1'>Low</prop></not>")]

        // Name
        [TestCase("Name=SomeTest", "<name>SomeTest</name>")]
        [TestCase("Name!=SomeTest", "<not><name>SomeTest</name></not>")]
        [TestCase("Name~SomeTest", "<name re='1'>SomeTest</name>")]
        [TestCase("Name!~SomeTest", "<not><name re='1'>SomeTest</name></not>")]

        // FQN - No arguments
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method",
            "<test>My.Test.Fixture.Method</test>")]
        [TestCase(
            "FullyQualifiedName!=My.Test.Fixture.Method",
            "<not><test>My.Test.Fixture.Method</test></not>")]
        [TestCase(
            "FullyQualifiedName~My.Test.Fixture.Method",
            @"<test re='1'>My\.Test\.Fixture\.Method</test>")]
        [TestCase(
            "FullyQualifiedName!~My.Test.Fixture.Method",
            @"<not><test re='1'>My\.Test\.Fixture\.Method</test></not>")]

        // FQN - Method arguments
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method(42)",
            "<test>My.Test.Fixture.Method(42)</test>")]
        [TestCase(
            "FullyQualifiedName!=My.Test.Fixture.Method(42)",
            "<not><test>My.Test.Fixture.Method(42)</test></not>")]
        [TestCase(
            "FullyQualifiedName~My.Test.Fixture.Method(42)",
            @"<test re='1'>My\.Test\.Fixture\.Method\(42\)</test>")]
        [TestCase(
            "FullyQualifiedName!~My.Test.Fixture.Method(42)",
            @"<not><test re='1'>My\.Test\.Fixture\.Method\(42\)</test></not>")]

        // FQN - String argument escaping
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method(\"xyz\")",
            "<test>My.Test.Fixture.Method(&quot;xyz&quot;)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method(\"abc's\")",
            "<test>My.Test.Fixture.Method(&quot;abc&apos;s&quot;)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method(\"x&y&z\")",
            "<test>My.Test.Fixture.Method(&quot;x&amp;y&amp;z&quot;)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method(\"<xyz>\")",
            "<test>My.Test.Fixture.Method(&quot;&lt;xyz&gt;&quot;)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method(\"x(y(z\")",
            "<test>My.Test.Fixture.Method(&quot;x(y(z&quot;)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture.Method(\"x)y)z\")",
            "<test>My.Test.Fixture.Method(&quot;x)y)z&quot;)</test>")]
        [TestCase(
            "FullyQualifiedName~My.Test.Fixture.Method(\"xyz\")",
            @"<test re='1'>My\.Test\.Fixture\.Method\(&quot;xyz&quot;\)</test>")]
        [TestCase(
            "FullyQualifiedName~My.Test.Fixture.Method(\"abc's\")",
            @"<test re='1'>My\.Test\.Fixture\.Method\(&quot;abc&apos;s&quot;\)</test>")]
        [TestCase(
            "FullyQualifiedName~My.Test.Fixture.Method(\"x&y&z\")",
            @"<test re='1'>My\.Test\.Fixture\.Method\(&quot;x&amp;y&amp;z&quot;\)</test>")]
        [TestCase(
            "FullyQualifiedName~My.Test.Fixture.Method(\"<xyz>\")",
            @"<test re='1'>My\.Test\.Fixture\.Method\(&quot;&lt;xyz&gt;&quot;\)</test>")]

        // FQN - Fixture Arguments
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture(99).Method",
            "<test>My.Test.Fixture(99).Method</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture(99).Method(42)",
            "<test>My.Test.Fixture(99).Method(42)</test>")]

        // FQN - Nested Fixture
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture+NestedFixture.Method",
            "<test>My.Test.Fixture+NestedFixture.Method</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture+NestedFixture.Method(1,2,3)",
            "<test>My.Test.Fixture+NestedFixture.Method(1,2,3)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture(1,2,3)+NestedFixture.Method(\"fred\")",
            "<test>My.Test.Fixture(1,2,3)+NestedFixture.Method(&quot;fred&quot;)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture+NestedFixture(1,2,3).Method(4,5,6)",
            "<test>My.Test.Fixture+NestedFixture(1,2,3).Method(4,5,6)</test>")]
        [TestCase(
            "FullyQualifiedName=My.Test.Fixture(1,2,3)+NestedFixture(4,5,6).Method(7,8,9)",
            "<test>My.Test.Fixture(1,2,3)+NestedFixture(4,5,6).Method(7,8,9)</test>")]

        // Logical expressions
        [TestCase(
            "TestCategory = Urgent | TestCategory = High",
            "<or><cat>Urgent</cat><cat>High</cat></or>")]
        [TestCase(
            "TestCategory=Urgent & FullyQualifiedName=My.Tests",
            "<and><cat>Urgent</cat><test>My.Tests</test></and>")]
        [TestCase(
            "TestCategory=Urgent | FullyQualifiedName=My.Tests",
            "<or><cat>Urgent</cat><test>My.Tests</test></or>")]
        [TestCase(
            "TestCategory=Urgent | FullyQualifiedName=My.Tests & TestCategory = high",
            "<or><cat>Urgent</cat><and><test>My.Tests</test><cat>high</cat></and></or>")]
        [TestCase(
            "TestCategory=Urgent & FullyQualifiedName=My.Tests | TestCategory = high",
            "<or><and><cat>Urgent</cat><test>My.Tests</test></and><cat>high</cat></or>")]
        [TestCase(
            "TestCategory=Urgent & (FullyQualifiedName=My.Tests | TestCategory = high)",
            "<and><cat>Urgent</cat><or><test>My.Tests</test><cat>high</cat></or></and>")]
        [TestCase(
            "TestCategory=Urgent & !(FullyQualifiedName=My.Tests | TestCategory = high)",
            "<and><cat>Urgent</cat><not><or><test>My.Tests</test><cat>high</cat></or></not></and>")]
        [TestCase("Bug = 12345", "<prop name='Bug'>12345</prop>")]
        public void TestParser(string input, string output)
        {
            Assert.That(_parser.Parse(input), Is.EqualTo($"<filter>{output}</filter>"));

            XmlDocument doc = new ();
            Assert.DoesNotThrow(() => doc.LoadXml(output));
        }

        [TestCase(null, typeof(ArgumentNullException))]
        [TestCase("", typeof(TestFilterParserException))]
        [TestCase("   ", typeof(TestFilterParserException))]
        [TestCase("  \t\t ", typeof(TestFilterParserException))]
        public void TestParser_InvalidInput(string input, Type type)
        {
            Assert.That(() => _parser.Parse(input), Throws.TypeOf(type));
        }
    }
}
