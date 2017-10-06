using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using NUnit.Framework;


namespace NUnit.VisualStudio.TestAdapter.Tests
{
   
    public class TestDataForTraits
    {
        #region TestXml Data
        const string TestXml =
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

        /// <summary>
        ///[Category("BaseClass")]
        ///public class Class1
        ///{
        ///    [Category("Base")]
        ///    [Test]
        ///    public void nUnitTest()
        ///    {
        ///
        ///    }
        ///}
        ///
        ///[Category("DerivedClass")]
        ///public class ClassD : Class1
        ///{
        ///    [Category("Derived")]
        ///    [Test]
        ///    public void dNunitTest()
        ///    { }
        ///}
        /// </summary>
        const string XmlHierarchyOfClasses = @"<test-run id='2' name='nUnitClassLibrary.dll' fullname='C:\Users\navb\source\repos\nUnitClassLibrary\nUnitClassLibrary\bin\Debug\nUnitClassLibrary.dll' testcasecount='5'>
	<test-suite type='Assembly' id='0-1009' name='nUnitClassLibrary.dll' fullname='C:\Users\navb\source\repos\nUnitClassLibrary\nUnitClassLibrary\bin\Debug\nUnitClassLibrary.dll' runstate='Runnable' testcasecount='5'>
		<properties>
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
				<test-case id='0-1003' name='dNunitTest' fullname='nUnitClassLibrary.ClassD.dNunitTest' methodname='dNunitTest' classname='nUnitClassLibrary.ClassD' runstate='Runnable' seed='405714082'>
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
		</test-suite>
	</test-suite>
</test-run>";

        /// <summary>
        ///[Category("NS1")]
        ///public class NestedClasses
        ///{
        ///    [Category("NS11")]
        ///    [Test]
        ///    public void NC11()
        ///    {
        ///    }
        ///
        ///    [Category("NS2")]
        ///    public class NestedClass2
        ///    {
        ///        [Category("NS21")]
        ///        [Test]
        ///        public void NC21()
        ///        {
        ///        }
        ///    }
        ///}
        /// </summary>
        const string XmlNestedClasses = @"<test-run id='2' name='nUnitClassLibrary.dll' fullname='C:\Users\navb\source\repos\nUnitClassLibrary\nUnitClassLibrary\bin\Debug\nUnitClassLibrary.dll' testcasecount='5'>
	<test-suite type='Assembly' id='0-1009' name='nUnitClassLibrary.dll' fullname='C:\Users\navb\source\repos\nUnitClassLibrary\nUnitClassLibrary\bin\Debug\nUnitClassLibrary.dll' runstate='Runnable' testcasecount='5'>
		<properties>
			<property name='_PID' value='6164' />
			<property name='_APPDOMAIN' value='domain-71b2ab93-nUnitClassLibrary.dll' />
		</properties>
		<test-suite type='TestSuite' id='0-1010' name='nUnitClassLibrary' fullname='nUnitClassLibrary' runstate='Runnable' testcasecount='5'>
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

        /// <summary>
        /// [Category("ClassLevel")]
        ///public class ManyTests
        ///{
        ///    [TestCase(1), Category("TestCase level")]
        ///   [TestCase(2)]
        ///    [Category("MethodLevel")]
        ///    public void ThatWeExist(int n)
        ///    {
        ///        Assert.IsTrue(true);
        ///    }
        ///}
        /// </summary>
        const string TestXmlParametrizedData =
            @"<test-suite type='Assembly' id='4-1004' name='ClassLibrary11.dll' fullname='C:\Users\Terje\documents\visual studio 2017\Projects\ClassLibrary11\ClassLibrary11\bin\Debug\ClassLibrary11.dll' runstate='Runnable' testcasecount='2'>
    <properties>
        <property name='_PID' value='10904' />
        <property name='_APPDOMAIN' value='domain-aa3de7f5-ClassLibrary11.dll' />
    </properties>
    <test-suite type='TestSuite' id='4-1005' name='ClassLibrary11' fullname='ClassLibrary11' runstate='Runnable' testcasecount='2'>
        <test-suite type='TestFixture' id='4-1000' name='ManyTests' fullname='ClassLibrary11.ManyTests' classname='ClassLibrary11.ManyTests' runstate='Runnable' testcasecount='2'>
            <properties>
                <property name='Category' value='ClassLevel' />
            </properties>
            <test-suite type='ParameterizedMethod' id='4-1003' name='ThatWeExist' fullname='ClassLibrary11.ManyTests.ThatWeExist' classname='ClassLibrary11.ManyTests' runstate='Runnable' testcasecount='2'>
                <properties>
                    <property name='Category' value='TestCase level' />
                    <property name='Category' value='MethodLevel' />
                </properties>
                <test-case id='4-1001' name='ThatWeExist(1)' fullname='ClassLibrary11.ManyTests.ThatWeExist(1)' methodname='ThatWeExist' classname='ClassLibrary11.ManyTests' runstate='Runnable' seed='1087191830' />
                <test-case id='4-1002' name='ThatWeExist(2)' fullname='ClassLibrary11.ManyTests.ThatWeExist(2)' methodname='ThatWeExist' classname='ClassLibrary11.ManyTests' runstate='Runnable' seed='1855643337' />
            </test-suite>
        </test-suite>
    </test-suite>
</test-suite>";

        /// <summary>
        /// [Category("ClassLevel")]
        ///public class StandardClass
        ///{
        ///   [Category("MethodLevel")]
        ///   [Test]
        ///   public void ThatWeExist()
        ///   {
        ///       Assert.IsTrue(true);
        ///   }
        ///}
        /// </summary>
        private const string TestXmlStandardClass =
            @"<test-suite type='Assembly' id='5-1002' name='ClassLibrary11.dll' fullname='C:\Users\Terje\documents\visual studio 2017\Projects\ClassLibrary11\ClassLibrary11\bin\Debug\ClassLibrary11.dll' runstate='Runnable' testcasecount='1'>
    <properties>
        <property name='_PID' value='10904' />
        <property name='_APPDOMAIN' value='domain-aa3de7f5-ClassLibrary11.dll' />
    </properties>
    <test-suite type='TestSuite' id='5-1003' name='ClassLibrary11' fullname='ClassLibrary11' runstate='Runnable' testcasecount='1'>
        <test-suite type='TestFixture' id='5-1000' name='StandardClass' fullname='ClassLibrary11.StandardClass' classname='ClassLibrary11.StandardClass' runstate='Runnable' testcasecount='1'>
            <properties>
                <property name='Category' value='ClassLevel' />
            </properties>
            <test-case id='5-1001' name='ThatWeExist' fullname='ClassLibrary11.StandardClass.ThatWeExist' methodname='ThatWeExist' classname='ClassLibrary11.StandardClass' runstate='Runnable' seed='1462235782'>
                <properties>
                    <property name='Category' value='MethodLevel' />
                </properties>
            </test-case>
        </test-suite>
    </test-suite>
</test-suite>";





        /// <summary>
        ///  [TestCase(1, 2, ExpectedResult = 3, Category="Single")]
        ///  [TestCase(4, 5, ExpectedResult = 9)]
        ///  [TestCase(27, 30, ExpectedResult = 57)]
        ///  public int SumTests(int a, int b)
        ///  {
        ///    var sut = new Calculator();
        ///
        ///    return sut.Sum(a, b);
        ///}
        /// </summary>
        private const string TestCaseWithCategory =
            @"<test-suite type='Assembly' id='3-1005' name='ClassLibrary11.dll' fullname='C:\Users\Terje\documents\visual studio 2017\Projects\ClassLibrary11\ClassLibrary11\bin\Debug\ClassLibrary11.dll' runstate='Runnable' testcasecount='3'>
   <properties>
      <property name='_PID' value='23304' />
      <property name='_APPDOMAIN' value='domain-aa3de7f5-ClassLibrary11.dll' />
   </properties>
   <test-suite type='TestSuite' id='3-1006' name='ClassLibrary11' fullname='ClassLibrary11' runstate='Runnable' testcasecount='3'>
      <test-suite type='TestFixture' id='3-1000' name='Class1' fullname='ClassLibrary11.Class1' classname='ClassLibrary11.Class1' runstate='Runnable' testcasecount='3'>
         <test-suite type='ParameterizedMethod' id='3-1004' name='SumTests' fullname='ClassLibrary11.Class1.SumTests' classname='ClassLibrary11.Class1' runstate='Runnable' testcasecount='3'>
            <test-case id='3-1001' name='SumTests(1,2)' fullname='ClassLibrary11.Class1.SumTests(1,2)' methodname='SumTests' classname='ClassLibrary11.Class1' runstate='Runnable' seed='433189107'>
               <properties>
                  <property name='Category' value='Single' />
               </properties>
            </test-case>
            <test-case id='3-1002' name='SumTests(4,5)' fullname='ClassLibrary11.Class1.SumTests(4,5)' methodname='SumTests' classname='ClassLibrary11.Class1' runstate='Runnable' seed='1896735603' />
            <test-case id='3-1003' name='SumTests(27,30)' fullname='ClassLibrary11.Class1.SumTests(27,30)' methodname='SumTests' classname='ClassLibrary11.Class1' runstate='Runnable' seed='225813975' />
         </test-suite>
      </test-suite>
   </test-suite>
</test-suite>";



        #endregion

        public XmlNode XmlForNestedClasses => XmlHelper.CreateXmlNode(XmlNestedClasses);
        public XmlNode XmlForHierarchyOfClasses => XmlHelper.CreateXmlNode(XmlHierarchyOfClasses);
        public XmlNode XmlForParametrizedTests => XmlHelper.CreateXmlNode(TestXmlParametrizedData);
        public XmlNode XmlForStandardTest => XmlHelper.CreateXmlNode(TestXmlStandardClass);

        public XmlNode XmlForTestCaseWithCategory => XmlHelper.CreateXmlNode(TestCaseWithCategory);
    }

#if !NETCOREAPP1_0

    [Category(nameof(TestTraits))]
    public class TestTraits
    {
        private TestConverter testconverter;
        private List<TestCase> testcaselist;
        private TestDataForTraits testDataForTraits;


        [SetUp]
        public void SetUp()
        {
            testDataForTraits = new TestDataForTraits();
            var messagelogger = Substitute.For<IMessageLogger>();
            var testlogger = new TestLogger(messagelogger, 5);

            testconverter = new TestConverter(testlogger, "whatever", false);
            testcaselist = new List<TestCase>();
        }

        [Test]
        public void ThatParametrizedTestsHaveTraits()
        {
            var xml = testDataForTraits.XmlForParametrizedTests;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(2), "Wrong number of testcases found");
            var testcase1 = testcaselist.FirstOrDefault(o => o.DisplayName == "ThatWeExist(1)");
            Assert.That(testcase1, Is.Not.Null, "Didn't find the first testcase");
            Assert.That(testcase1.Traits.Count(), Is.EqualTo(3), "Wrong number of categories for first test case");

            var testcase2 = testcaselist.FirstOrDefault(o => o.DisplayName == "ThatWeExist(2)");
            Assert.That(testcase2, Is.Not.Null, "Didn't find the second testcase");
            Assert.That(testcase2.Traits.Count(), Is.EqualTo(3), "Wrong number of categories for second test case");

        }

        [Test]
        public void ThatDerivedClassesHaveTraits()
        {
            var xml = testDataForTraits.XmlForHierarchyOfClasses;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(3), "Wrong number of testcases found");
            var testcase1 = testcaselist.FirstOrDefault(o => o.DisplayName == "dNunitTest");
            Assert.That(testcase1, Is.Not.Null, "Didn't find the  testcase");
            Assert.That(testcase1.Traits.Count(), Is.EqualTo(3), "Wrong number of categories for derived test case");
        }

        [Test]
        public void ThatNestedClassesHaveTraits()
        {
            var xml = testDataForTraits.XmlForNestedClasses;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(2), "Wrong number of testcases found");
            var testcase1 = testcaselist.FirstOrDefault(o => o.DisplayName == "NC21");
            Assert.That(testcase1, Is.Not.Null, "Didn't find the  testcase");
            Assert.That(testcase1.Traits.Count(), Is.EqualTo(2), "Wrong number of categories for derived test case");
        }

        private void ProcessXml2TestCase(XmlNode xml)
        {
            foreach (XmlNode node in xml.SelectNodes("//test-case"))
            {
                var testcase = testconverter.ConvertTestCase(node);
                testcaselist.Add(testcase);
            }
        }


        [Test]
        public void ThatStandardClassHasTraits()
        {
            var xml = testDataForTraits.XmlForStandardTest;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(1), "Wrong number of testcases found");
            var testcase1 = testcaselist.FirstOrDefault(o => o.DisplayName == "ThatWeExist");
            Assert.That(testcase1, Is.Not.Null, "Didn't find the  testcase");
            Assert.That(testcase1.Traits.Count(), Is.EqualTo(2), "Wrong number of categories for first test case");

        }

        [Test]
        public void ThatTestCaseHasTraits()
        {
            var xml = testDataForTraits.XmlForTestCaseWithCategory;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(3), "Wrong number of testcases found");
            var testcasesWithCategories = testcaselist.Where(o => o.Traits?.FirstOrDefault(p=>p.Name=="Category")!= null);
            Assert.That(testcasesWithCategories, Is.Not.Null, "Didn't find the  testcases");
            Assert.That(testcasesWithCategories.Count(),Is.EqualTo(1),"Wrong number of testcases with categories, should be only 1");
            var tc = testcasesWithCategories.FirstOrDefault();
            Assert.That(tc.Traits.Count(), Is.EqualTo(1), "Wrong number of categories for test case");
            Assert.That(tc.Traits.First().Value,Is.EqualTo("Single"));

        }

    }
#endif
}
