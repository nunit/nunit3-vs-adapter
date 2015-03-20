// ****************************************************************
// Copyright (c) 2015 NUnit Software. All rights reserved.
// ****************************************************************

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
            Path.Combine(Path.GetDirectoryName(AssemblyPath), @"..\..\Fakes\FakeTestData.cs");

        // NOTE: If the location of the FakeTestCase method defined 
        // above changes, update the value of LineNumber.
        public const int LineNumber = 17;

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
