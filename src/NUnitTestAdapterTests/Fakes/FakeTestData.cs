// ***********************************************************************
// Copyright (c) 2015 Charlie Poole
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

using System.IO;
using System.Reflection;
using System.Xml;

namespace NUnit.VisualStudio.TestAdapter.Tests.Fakes
{
    /// <summary>
    /// FakeTestData provides xml data representing a test.
    /// </summary>
    public static class FakeTestData
    {
        // ReSharper disable once UnusedMember.Local
        private static void FakeTestCase() { } // LineNumber should be this line

        public const string TestXml =
            @"<test-suite
                id='121'
                name='FakeTestData'
                fullname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData'
                classname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData'>
                <properties>
                    <property name='Category' value='super' />
                </properties>
                <test-case
                    id='123' 
                    name='FakeTestCase'
                    fullname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData.FakeTestCase'
                    methodname='FakeTestCase'
                    classname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData'>
                    <properties>
                        <property name='Category' value='cat1' />
                        <property name='Priority' value='medium' />
                    </properties>
                </test-case>
            </test-suite>";

        public const string ResultXml =
            @"<test-suite
                id='121'
                name='FakeTestData'
                fullname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData'
                classname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData'>
                <properties>
                    <property name='Category' value='super' />
                </properties>
                <test-case
                    id='123' 
                    name='FakeTestCase'
                    fullname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData.FakeTestCase'
                    methodname='FakeTestCase'
                    classname='NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData'
                    result='Passed'
                    start-time='2015-03-18 15:58:12Z'
                    end-time='2015-03-18 15:58:13Z'
                    duration='1.234'>
                    <properties>
                        <property name='Category' value='cat1' />
                        <property name='Priority' value='medium' />
                    </properties>
                    <reason>
                        <message>It passed!</message>
                    </reason>
                </test-case>
            </test-suite>";

        public const string DisplayName = "FakeTestCase";

        public const string FullyQualifiedName = "NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData.FakeTestCase";

        public static readonly string AssemblyPath =
            Assembly.GetExecutingAssembly().ManifestModule.FullyQualifiedName;

        public static readonly string CodeFile =
            Path.Combine(Path.GetDirectoryName(AssemblyPath), @"..\..\src\NUnitTestAdapterTests\Fakes\FakeTestData.cs");

        // NOTE: If the location of the FakeTestCase method defined 
        // above changes, update the value of LineNumber.
        public const int LineNumber = 36;

        public static XmlNode GetTestNode()
        {
            return XmlHelper.CreateXmlNode(TestXml).SelectSingleNode("test-case");
        }

        public static XmlNode GetResultNode()
        {
            return XmlHelper.CreateXmlNode(ResultXml).SelectSingleNode("test-case");
        }
    }
}
