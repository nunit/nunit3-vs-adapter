using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;
// ReSharper disable StringLiteralTypo

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
        /// [Category("BaseClass")]
        /// public class Class1
        /// {
        ///    [Category("Base")]
        ///    [Test]
        ///    public void nUnitTest()
        ///    {
        ///
        ///    }
        /// }
        ///
        /// [Category("DerivedClass")]
        /// public class ClassD : Class1
        /// {
        ///    [Category("Derived")]
        ///    [Test]
        ///    public void dNUnitTest()
        ///    { }
        /// }.
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
		</test-suite>
	</test-suite>
</test-run>";

        /// <summary>
        /// [Category("NS1")]
        /// public class NestedClasses
        /// {
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
        /// }.
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
        /// public class ManyTests
        /// {
        ///    [TestCase(1), Category("TestCase level")]
        ///   [TestCase(2)]
        ///    [Category("MethodLevel")]
        ///    public void ThatWeExist(int n)
        ///    {
        ///        Assert.IsTrue(true);
        ///    }
        /// }.
        /// </summary>
        const string TestXmlParameterizedData =
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
        /// public class StandardClass
        /// {
        ///   [Category("MethodLevel")]
        ///   [Test]
        ///   public void ThatWeExist()
        ///   {
        ///       Assert.IsTrue(true);
        ///   }
        /// }.
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
        /// }.
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

        // [Category("BaseClass")]
        // public class TestBase
        // {
        // [Category("BaseMethod")]
        // [Test]
        // public void TestItBase()
        // {
        // Assert.That(true);
        // }
        // }
        // [Category("DerivedClass")]
        // public class Derived : TestBase
        // {
        // [Category("DerivedMethod")]
        // [Test]
        // public void TestItDerived()
        // {
        // Assert.That(true);
        // }
        // }
        private const string TestCaseWithInheritedTestsInSameAssembly =
            @"<test-suite type='Assembly' id='0-1005' name='ClassLibrary11.dll' fullname='C:\Users\Terje\documents\visual studio 2017\Projects\ClassLibrary11\ClassLibrary11\bin\Debug\ClassLibrary11.dll' runstate='Runnable' testcasecount='3'>
	  <properties>
		 <property name='_PID' value='27456' />
		 <property name='_APPDOMAIN' value='domain-aa3de7f5-ClassLibrary11.dll' />
	  </properties>
	  <test-suite type='TestSuite' id='0-1006' name='ClassLibrary11' fullname='ClassLibrary11' runstate='Runnable' testcasecount='3'>
		 <test-suite type='TestFixture' id='0-1002' name='Derived' fullname='ClassLibrary11.Derived' classname='ClassLibrary11.Derived' runstate='Runnable' testcasecount='2'>
			<properties>
			   <property name='Category' value='DerivedClass' />
			   <property name='Category' value='BaseClass' />
			</properties>
			<test-case id='0-1004' name='TestItBase' fullname='ClassLibrary11.Derived.TestItBase' methodname='TestItBase' classname='ClassLibrary11.TestBase' runstate='Runnable' seed='1107082401'>
			   <properties>
				  <property name='Category' value='BaseMethod' />
			   </properties>
			</test-case>
			<test-case id='0-1003' name='TestItDerived' fullname='ClassLibrary11.Derived.TestItDerived' methodname='TestItDerived' classname='ClassLibrary11.Derived' runstate='Runnable' seed='1484432600'>
			   <properties>
				  <property name='Category' value='DerivedMethod' />
			   </properties>
			</test-case>
		 </test-suite>
		 <test-suite type='TestFixture' id='0-1000' name='TestBase' fullname='ClassLibrary11.TestBase' classname='ClassLibrary11.TestBase' runstate='Runnable' testcasecount='1'>
			<properties>
			   <property name='Category' value='BaseClass' />
			</properties>
			<test-case id='0-1001' name='TestItBase' fullname='ClassLibrary11.TestBase.TestItBase' methodname='TestItBase' classname='ClassLibrary11.TestBase' runstate='Runnable' seed='144634857'>
			   <properties>
				  <property name='Category' value='BaseMethod' />
			   </properties>
			</test-case>
		 </test-suite>
	  </test-suite>
   </test-suite>";

        // [Category("BaseClass")]
        // public abstract class TestBase
        // {
        // [Category("BaseMethod")]
        // [Test]
        // public void TestItBase()
        // {
        // Assert.That(true);
        // }
        // }
        // [Category("DerivedClass")]
        // public class Derived : TestBase
        // {
        // [Category("DerivedMethod")]
        // [Test]
        // public void TestItDerived()
        // {
        // Assert.That(true);
        // }
        // }
        private const string TestCaseWithAbstractInheritedTestsInSameAssembly =
            @"<test-suite type='Assembly' id='0-1003' name='ClassLibrary11.dll' fullname='C:\Users\Terje\documents\visual studio 2017\Projects\ClassLibrary11\ClassLibrary11\bin\Debug\ClassLibrary11.dll' runstate='Runnable' testcasecount='2'>
	  <properties>
		 <property name='_PID' value='47684' />
		 <property name='_APPDOMAIN' value='domain-aa3de7f5-ClassLibrary11.dll' />
	  </properties>
	  <test-suite type='TestSuite' id='0-1004' name='ClassLibrary11' fullname='ClassLibrary11' runstate='Runnable' testcasecount='2'>
		 <test-suite type='TestFixture' id='0-1000' name='Derived' fullname='ClassLibrary11.Derived' classname='ClassLibrary11.Derived' runstate='Runnable' testcasecount='2'>
			<properties>
			   <property name='Category' value='DerivedClass' />
			   <property name='Category' value='BaseClass' />
			</properties>
			<test-case id='0-1002' name='TestItBase' fullname='ClassLibrary11.Derived.TestItBase' methodname='TestItBase' classname='ClassLibrary11.TestBase' runstate='Runnable' seed='1628925226'>
			   <properties>
				  <property name='Category' value='BaseMethod' />
			   </properties>
			</test-case>
			<test-case id='0-1001' name='TestItDerived' fullname='ClassLibrary11.Derived.TestItDerived' methodname='TestItDerived' classname='ClassLibrary11.Derived' runstate='Runnable' seed='1596181992'>
			   <properties>
				  <property name='Category' value='DerivedMethod' />
			   </properties>
			</test-case>
		 </test-suite>
	  </test-suite>
</test-suite>";


        #endregion

        public XmlNode XmlForNestedClasses => XmlHelper.CreateXmlNode(XmlNestedClasses);
        public XmlNode XmlForHierarchyOfClasses => XmlHelper.CreateXmlNode(XmlHierarchyOfClasses);
        public XmlNode XmlForParameterizedTests => XmlHelper.CreateXmlNode(TestXmlParameterizedData);
        public XmlNode XmlForStandardTest => XmlHelper.CreateXmlNode(TestXmlStandardClass);

        public XmlNode XmlForTestCaseWithCategory => XmlHelper.CreateXmlNode(TestCaseWithCategory);

        public XmlNode XmlForTestCaseWithInheritedTestsInSameAssembly => XmlHelper.CreateXmlNode(TestCaseWithInheritedTestsInSameAssembly);
        public XmlNode XmlForTestCaseWithAbstractInheritedTestsInSameAssembly => XmlHelper.CreateXmlNode(TestCaseWithAbstractInheritedTestsInSameAssembly);
    }

    [Category(nameof(TestTraits))]
    public class TestTraits
    {
        private TestConverterForXml testconverter;
        private List<TestCase> testcaselist;
        private TestDataForTraits testDataForTraits;


        [SetUp]
        public void SetUp()
        {
            testDataForTraits = new TestDataForTraits();
            var messagelogger = Substitute.For<IMessageLogger>();
            var adaptersettings = Substitute.For<IAdapterSettings>();
            adaptersettings.Verbosity.Returns(5);
            var testlogger = new TestLogger(messagelogger);
            testlogger.InitSettings(adaptersettings);
            var settings = Substitute.For<IAdapterSettings>();
            settings.CollectSourceInformation.Returns(false);
            testconverter = new TestConverterForXml(testlogger, "whatever", settings);
            testcaselist = new List<TestCase>();
        }

        [TearDown]
        public void TearDown()
        {
            testconverter.Dispose();
        }

        [Test]
        public void ThatParameterizedTestsHaveTraits()
        {
            var xml = testDataForTraits.XmlForParameterizedTests;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(2), "Wrong number of testcases found");
            var testcase1 = testcaselist.FirstOrDefault(o => o.DisplayName == "ThatWeExist(1)");
            Assert.That(testcase1, Is.Not.Null, "Didn't find the first testcase");
            Assert.That(testcase1.GetCategories().Count(), Is.EqualTo(3), "Wrong number of categories for first test case");

            var testcase2 = testcaselist.FirstOrDefault(o => o.DisplayName == "ThatWeExist(2)");
            Assert.That(testcase2, Is.Not.Null, "Didn't find the second testcase");
            Assert.That(testcase2.GetCategories().Count(), Is.EqualTo(3), "Wrong number of categories for second test case");
        }

        [Test]
        public void ThatDerivedClassesHaveTraits()
        {
            var xml = testDataForTraits.XmlForHierarchyOfClasses;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(3), "Wrong number of testcases found");
            var testcase1 = testcaselist.FirstOrDefault(o => o.DisplayName == "dNUnitTest");
            Assert.That(testcase1, Is.Not.Null, "Didn't find the  testcase");
            VerifyCategoriesOnly(testcase1, 3, "derived");
       }

        [Test]
        public void ThatNestedClassesHaveTraits()
        {
            var xml = testDataForTraits.XmlForNestedClasses;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(2), "Wrong number of testcases found");
            var testcase1 = testcaselist.FirstOrDefault(o => o.DisplayName == "NC21");
            Assert.That(testcase1, Is.Not.Null, "Didn't find the  testcase");
            VerifyCategoriesOnly(testcase1, 2, "nested");
        }


        [Test]
        public void ThatInheritedConcreteClassesHaveTraits()
        {
            var xml = testDataForTraits.XmlForTestCaseWithInheritedTestsInSameAssembly;
            ProcessXml2TestCase(xml);
            Assert.That(testcaselist.Count, Is.EqualTo(3), "Wrong number of testcases found");
            var uniqueTraits = UniqueCategories();
            Assert.That(uniqueTraits.Count(), Is.EqualTo(4), "Wrong number of traits");
            string searchTrait = "BaseClass";
            var tcWithTrait = TcWithTrait(searchTrait);
            Assert.That(tcWithTrait.Count(), Is.EqualTo(3), $"Wrong number of testcases found for trait={searchTrait}");
        }

        private IEnumerable<TestCase> TcWithTrait(string searchTrait)
        {
            return testcaselist.Where(o => o.GetCategories().Contains(searchTrait));
        }

        private IEnumerable<string> UniqueCategories()
        {
            var traits = new List<string>();
            foreach (var tc in testcaselist)
            {
                traits.AddRange(tc.GetCategories());
            }
            var uniqueTraits = traits.Distinct();
            return uniqueTraits;
        }


        [Test]
        public void ThatInheritedAbstractClassesHaveTraits()
        {
            var xml = testDataForTraits.XmlForTestCaseWithAbstractInheritedTestsInSameAssembly;
            ProcessXml2TestCase(xml);
            Assert.That(testcaselist.Count, Is.EqualTo(2), "Wrong number of testcases found");
            var uniqueTraits = UniqueCategories();
            Assert.That(uniqueTraits.Count(), Is.EqualTo(4), "Wrong number of traits");
            string searchTrait = "BaseClass";
            var tcWithTrait = TcWithTrait(searchTrait);
            Assert.That(tcWithTrait.Count(), Is.EqualTo(2), $"Wrong number of testcases found for trait={searchTrait}");
        }


        private void ProcessXml2TestCase(XmlNode xml)
        {
            foreach (XmlNode node in xml.SelectNodes("//test-case"))
            {
                var testcase = testconverter.ConvertTestCase(new NUnitEventTestCase(node));
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

            VerifyCategoriesOnly(testcase1, 2, "first");
        }

        [Test]
        public void ThatTestCaseHasTraits()
        {
            var xml = testDataForTraits.XmlForTestCaseWithCategory;

            ProcessXml2TestCase(xml);

            Assert.That(testcaselist.Count, Is.EqualTo(3), "Wrong number of testcases found");
            var testcasesWithCategories = testcaselist.Where(o => o.GetCategories()?.FirstOrDefault() != null).ToList();
            Assert.That(testcasesWithCategories, Is.Not.Null, "Didn't find the  testcases");
            Assert.That(testcasesWithCategories.Count, Is.EqualTo(1), "Wrong number of testcases with categories, should be only 1");
            var tc = testcasesWithCategories.FirstOrDefault();
            VerifyCategoriesOnly(tc, 1, "simple");
            Assert.That(tc.GetCategories().First(), Is.EqualTo("Single"));
        }


        private void VerifyCategoriesOnly(TestCase testcase, int expectedCategories, string forTest)
        {
            var categories = testcase.GetCategories();
            Assert.Multiple(() =>
            {
                Assert.That(categories.Count(), Is.EqualTo(expectedCategories), $"Wrong number of categories for {forTest} testcase");
                Assert.That(testcase.Traits.Any(), Is.False, "There should be no traits");
            });
        }

        private static IReadOnlyList<TestCase> GetTestCases(string xml)
        {
            var settings = Substitute.For<IAdapterSettings>();
            settings.CollectSourceInformation.Returns(false);
            using var converter = new TestConverterForXml(
                new TestLogger(new MessageLoggerStub()),
                "unused",
                settings);
            return converter.ConvertTestCases(xml);
        }

        [Test]
        public static void ThatExplicitTestCaseHasExplicitTrait()
        {
            var testCase = GetTestCases(
                @"<test-suite id='1' name='Fixture' fullname='Fixture' classname='Fixture'>
					<test-case id='2' name='Test' fullname='Fixture.Test' methodname='Test' classname='Fixture' runstate='Explicit' />
				</test-suite>").Single();

            Assert.That(testCase.Traits, Has.One.With.Property("Name").EqualTo("Explicit"));
        }

        [Test]
        public static void ThatTestCaseWithExplicitParentHasExplicitTrait()
        {
            var testCase = GetTestCases(
                @"<test-suite id='1' name='Fixture' fullname='Fixture' classname='Fixture' runstate='Explicit'>
					<test-case id='2' name='Test' fullname='Fixture.Test' methodname='Test' classname='Fixture'/>
				</test-suite>").Single();

            Assert.That(testCase.Traits, Has.One.With.Property("Name").EqualTo("Explicit"));
        }

        [Test]
        public static void ThatMultipleChildTestCasesWithExplicitParentHaveExplicitTraits()
        {
            var testCases = GetTestCases(
                @"<test-suite id='1' name='Fixture' fullname='Fixture' classname='Fixture' runstate='Explicit'>
					<test-case id='2' name='Test' fullname='Fixture.Test' methodname='Test' classname='Fixture'/>
					<test-case id='3' name='Test2' fullname='Fixture.Test2' methodname='Test2' classname='Fixture'/>
				</test-suite>");

            foreach (var testCase in testCases)
                Assert.That(testCase.Traits, Has.One.With.Property("Name").EqualTo("Explicit"));
        }

        [Test]
        public static void ThatExplicitTraitValueIsEmptyString()
        {
            var testCase = GetTestCases(
                @"<test-suite id='1' name='Fixture' fullname='Fixture' classname='Fixture'>
					<test-case id='2' name='Test' fullname='Fixture.Test' methodname='Test' classname='Fixture' runstate='Explicit' />
				</test-suite>").Single();

            Assert.That(testCase.Traits, Has.One.With.Property("Name").EqualTo("Explicit").And.Property("Value").SameAs(string.Empty));
        }
    }
}
