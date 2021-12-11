// ***********************************************************************
// Copyright (c) 2015 Charlie Poole, Terje Sandstrom
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

        #region TestXmls
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

        public const string HierarchyTestXml = @"<test-run id='2' name='nUnitClassLibrary.dll' fullname='C:\Users\navb\source\repos\nUnitClassLibrary\nUnitClassLibrary\bin\Debug\nUnitClassLibrary.dll' testcasecount='5'>
    <test-suite type='Assembly' id='0-1009' name='nUnitClassLibrary.dll' fullname='C:\Users\navb\source\repos\nUnitClassLibrary\nUnitClassLibrary\bin\Debug\nUnitClassLibrary.dll' runstate='Runnable' testcasecount='5'>
        <properties>
            <property name='Category' value='AsmCat' />
            <property name='_PID' value='6164' />
            <property name='_APPDOMAIN' value='domain-71b2ab93-nUnitClassLibrary.dll' />
        </properties>
        <test-suite type='TestSuite' id='0-1010' name='nUnitClassLibrary' fullname='nUnitClassLibrary' runstate='Runnable' testcasecount='5'>
            <test-suite type='TestFixture' id='0-1000' name='Class1' fullname='nUnitClassLibrary.Class1' classname='nUnitClassLibrary.Class1' runstate='Runnable' testcasecount='1'>
                <properties>
                    <property name='Category' value='BaseClass' />
                </properties>
                <test-case id='0-1001' name='nUnitTest' fullname='nUnitClassLibrary.Class1.nUnitTest' methodname='nUnitTest' classname='nUnitClassLibrary.Class1' runstate='Runnable' seed='113395783'>
                    <properties>
                        <property name='Category' value='Base' />
                    </properties>
                </test-case>
            </test-suite>
            <test-suite type='TestFixture' id='0-1002' name='ClassD' fullname='nUnitClassLibrary.ClassD' classname='nUnitClassLibrary.ClassD' runstate='Runnable' testcasecount='2'>
                <properties>
                    <property name='Category' value='DerivedClass' />
                    <property name='Category' value='BaseClass' />
                </properties>
                <test-case id='0-1003' name='dNUnitTest' fullname='nUnitClassLibrary.ClassD.dNUnitTest' methodname='dNUnitTest' classname='nUnitClassLibrary.ClassD' runstate='Runnable' seed='405714082'>
                    <properties>
                        <property name='Category' value='Derived' />
                    </properties>
                </test-case>
                <test-case id='0-1004' name='nUnitTest' fullname='nUnitClassLibrary.ClassD.nUnitTest' methodname='nUnitTest' classname='nUnitClassLibrary.Class1' runstate='Runnable' seed='1553985978'>
                    <properties>
                        <property name='Category' value='Base' />
                    </properties>
                </test-case>
            </test-suite>
            <test-suite type='TestFixture' id='0-1005' name='NestedClasses' fullname='nUnitClassLibrary.NestedClasses' classname='nUnitClassLibrary.NestedClasses' runstate='Runnable' testcasecount='1'>
                <properties>
                    <property name='Category' value='NS1' />
                </properties>
                <test-case id='0-1006' name='NC11' fullname='nUnitClassLibrary.NestedClasses.NC11' methodname='NC11' classname='nUnitClassLibrary.NestedClasses' runstate='Runnable' seed='1107340752'>
                    <properties>
                        <property name='Category' value='NS11' />
                    </properties>
                </test-case>
            </test-suite>
            <test-suite type='TestFixture' id='0-1007' name='NestedClasses+NestedClass2' fullname='nUnitClassLibrary.NestedClasses+NestedClass2' classname='nUnitClassLibrary.NestedClasses+NestedClass2' runstate='Runnable' testcasecount='1'>
                <properties>
                    <property name='Category' value='NS2' />
                </properties>
                <test-case id='0-1008' name='NC21' fullname='nUnitClassLibrary.NestedClasses+NestedClass2.NC21' methodname='NC21' classname='nUnitClassLibrary.NestedClasses+NestedClass2' runstate='Runnable' seed='1823789309'>
                    <properties>
                        <property name='Category' value='NS21' />
                    </properties>
                </test-case>
            </test-suite>
        </test-suite>
    </test-suite>
</test-run>";
        #endregion

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
                    <attachments>
                        <attachment>
                            <filePath>c:\results\att.log</filePath>
                            <description>win, no scheme</description>
                        </attachment>
                        <attachment>
                            <filePath>file://c:\results\att.log</filePath>
                            <description>win, with scheme</description>
                        </attachment>
                        <attachment>
                            <filePath>/home/results/att.log</filePath>
                            <description>lin, no scheme</description>
                        </attachment>
                        <attachment>
                            <filePath>file:///home/results/att.log</filePath>
                            <description>lin, with scheme</description>
                        </attachment>
                        <attachment>
                            <filePath>C:\Windows\WindowsUpdate.log</filePath>
                            <description>win, Issue914</description>
                        </attachment>
                        <attachment>
                            <filePath></filePath>
                            <description>empty path</description>
                        </attachment>
                    </attachments>
                </test-case>
            </test-suite>";

        public const string DisplayName = "FakeTestCase";

        public const string FullyQualifiedName = "NUnit.VisualStudio.TestAdapter.Tests.Fakes.FakeTestData.FakeTestCase";

        public static readonly string AssemblyPath =
            typeof(FakeTestData).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName;

        public static readonly string CodeFile = Path.Combine(Path.GetDirectoryName(AssemblyPath), @"..\..\..\Fakes\FakeTestData.cs");

        // NOTE: If the location of the FakeTestCase method defined
        // above changes, update the value of LineNumber.
        public const int LineNumber = 36;

        public static XmlNode GetTestNode()
        {
            return XmlHelper.CreateXmlNode(TestXml).SelectSingleNode("test-case");
        }

        public static XmlNodeList GetTestNodes()
        {
            return XmlHelper.CreateXmlNode(HierarchyTestXml).SelectNodes("//test-case");
        }

        public static XmlNode GetResultNode()
        {
            return XmlHelper.CreateXmlNode(ResultXml).SelectSingleNode("test-case");
        }
    }
}
