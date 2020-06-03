using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NSubstitute;
using NUnit.Engine;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    public class NUnitDiscoveryTests
    {
        private const string DiscoveryXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='102'>
   <test-suite type='Assembly' id='0-1147' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='102'>
      <properties>
         <property name = '_PID' value='78348' />
         <property name = '_APPDOMAIN' value='domain-807ad471-CSharpTestDemo.dll' />
      </properties>
      <test-suite type = 'TestSuite' id='0-1148' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='102'>
         <test-suite type = 'TestFixture' id='0-1004' name='AsyncTests' fullname='NUnitTestDemo.AsyncTests' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='7'>
            <test-case id='0-1007' name='AsyncTaskTestFails' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestFails' methodname='AsyncTaskTestFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='749249702'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1006' name='AsyncTaskTestSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestSucceeds' methodname='AsyncTaskTestSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='816573480'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
            </test-case>
            <test-case id='0-1008' name='AsyncTaskTestThrowsException' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestThrowsException' methodname='AsyncTaskTestThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='295190867'>
               <properties>
                  <property name = 'Expect' value='Error' />
               </properties>
            </test-case>
            <test-suite type = 'ParameterizedMethod' id='0-1012' name='AsyncTaskWithResultFails' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
               <test-case id='0-1011' name='AsyncTaskWithResultFails()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultFails()' methodname='AsyncTaskWithResultFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='851942449' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1010' name='AsyncTaskWithResultSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1009' name='AsyncTaskWithResultSucceeds()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultSucceeds()' methodname='AsyncTaskWithResultSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='117836766' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1014' name='AsyncTaskWithResultThrowsException' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Error' />
               </properties>
               <test-case id='0-1013' name='AsyncTaskWithResultThrowsException()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultThrowsException()' methodname='AsyncTaskWithResultThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='844321872' />
            </test-suite>
            <test-case id='0-1005' name='AsyncVoidTestIsInvalid' fullname='NUnitTestDemo.AsyncTests.AsyncVoidTestIsInvalid' methodname='AsyncVoidTestIsInvalid' classname='NUnitTestDemo.AsyncTests' runstate='NotRunnable' seed='1580316419'>
               <properties>
                  <property name = '_SKIPREASON' value='Async test method must have non-void return type' />
                  <property name = 'Expect' value='Error' />
               </properties>
            </test-case>
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1015' name='ConfigFileTests' fullname='NUnitTestDemo.ConfigFileTests' classname='NUnitTestDemo.ConfigFileTests' runstate='Runnable' testcasecount='2'>
            <properties>
               <property name = 'Expect' value='Pass' />
            </properties>
            <test-case id='0-1017' name='CanReadConfigFile' fullname='NUnitTestDemo.ConfigFileTests.CanReadConfigFile' methodname='CanReadConfigFile' classname='NUnitTestDemo.ConfigFileTests' runstate='Runnable' seed='281804770' />
            <test-case id='0-1016' name='ProperConfigFileIsUsed' fullname='NUnitTestDemo.ConfigFileTests.ProperConfigFileIsUsed' methodname='ProperConfigFileIsUsed' classname='NUnitTestDemo.ConfigFileTests' runstate='Runnable' seed='117847879' />
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1135' name='ExplicitClass' fullname='NUnitTestDemo.ExplicitClass' classname='NUnitTestDemo.ExplicitClass' runstate='Explicit' testcasecount='1'>
            <test-case id='0-1136' name='ThisIsIndirectlyExplicit' fullname='NUnitTestDemo.ExplicitClass.ThisIsIndirectlyExplicit' methodname='ThisIsIndirectlyExplicit' classname='NUnitTestDemo.ExplicitClass' runstate='Runnable' seed='1614265293' />
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1000' name='FixtureWithApartmentAttributeOnClass' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass' runstate='Runnable' testcasecount='1'>
            <properties>
               <property name = 'ApartmentState' value='STA' />
            </properties>
            <test-case id='0-1001' name='TestMethodInSTAFixture' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass.TestMethodInSTAFixture' methodname='TestMethodInSTAFixture' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass' runstate='Runnable' seed='1143185688' />
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1002' name='FixtureWithApartmentAttributeOnMethod' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod' runstate='Runnable' testcasecount='1'>
            <test-case id='0-1003' name='TestMethodInSTA' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod.TestMethodInSTA' methodname='TestMethodInSTA' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod' runstate='Runnable' seed='2044143445'>
               <properties>
                  <property name = 'ApartmentState' value='STA' />
               </properties>
            </test-case>
         </test-suite>
         <test-suite type = 'GenericFixture' id='0-1025' name='GenericTests_IList&lt;TList&gt;' fullname='NUnitTestDemo.GenericTests_IList&lt;TList&gt;' runstate='Runnable' testcasecount='2'>
            <test-suite type = 'TestFixture' id='0-1021' name='GenericTests_IList&lt;ArrayList&gt;' fullname='NUnitTestDemo.GenericTests_IList&lt;ArrayList&gt;' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1022' name='CanAddToList' fullname='NUnitTestDemo.GenericTests_IList&lt;ArrayList&gt;.CanAddToList' methodname='CanAddToList' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' seed='1465829868' />
            </test-suite>
            <test-suite type = 'TestFixture' id='0-1023' name='GenericTests_IList&lt;List&lt;Int32&gt;&gt;' fullname='NUnitTestDemo.GenericTests_IList&lt;List&lt;Int32&gt;&gt;' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1024' name='CanAddToList' fullname='NUnitTestDemo.GenericTests_IList&lt;List&lt;Int32&gt;&gt;.CanAddToList' methodname='CanAddToList' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' seed='2032401887' />
            </test-suite>
         </test-suite>
         <test-suite type = 'GenericFixture' id='0-1020' name='GenericTests&lt;T&gt;' fullname='NUnitTestDemo.GenericTests&lt;T&gt;' runstate='Runnable' testcasecount='1'>
            <test-suite type = 'TestFixture' id='0-1018' name='GenericTests&lt;Int32&gt;' fullname='NUnitTestDemo.GenericTests&lt;Int32&gt;' classname='NUnitTestDemo.GenericTests`1' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1019' name='TestIt' fullname='NUnitTestDemo.GenericTests&lt;Int32&gt;.TestIt' methodname='TestIt' classname='NUnitTestDemo.GenericTests`1' runstate='Runnable' seed='160665061'>
                  <properties>
                     <property name = 'Expect' value='Pass' />
                  </properties>
               </test-case>
            </test-suite>
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1026' name='InheritedTestDerivedClass' fullname='NUnitTestDemo.InheritedTestDerivedClass' classname='NUnitTestDemo.InheritedTestDerivedClass' runstate='Runnable' testcasecount='1'>
            <test-case id='0-1027' name='TestInBaseClass' fullname='NUnitTestDemo.InheritedTestDerivedClass.TestInBaseClass' methodname='TestInBaseClass' classname='NUnitTestDemo.InheritedTestBaseClass' runstate='Runnable' seed='1740332273' />
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1028' name='OneTimeSetUpTests' fullname='NUnitTestDemo.OneTimeSetUpTests' classname='NUnitTestDemo.OneTimeSetUpTests' runstate='Runnable' testcasecount='2'>
            <properties>
               <property name = 'Expect' value='Pass' />
            </properties>
            <test-case id='0-1029' name='Test1' fullname='NUnitTestDemo.OneTimeSetUpTests.Test1' methodname='Test1' classname='NUnitTestDemo.OneTimeSetUpTests' runstate='Runnable' seed='685678284' />
            <test-case id='0-1030' name='Test2' fullname='NUnitTestDemo.OneTimeSetUpTests.Test2' methodname='Test2' classname='NUnitTestDemo.OneTimeSetUpTests' runstate='Runnable' seed='780194933' />
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1031' name='ParameterizedTests' fullname='NUnitTestDemo.ParameterizedTests' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='23'>
            <test-suite type = 'ParameterizedMethod' id='0-1041' name='TestCaseFails' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
               <test-case id='0-1040' name='TestCaseFails(31,11,99)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails(31,11,99)' methodname='TestCaseFails' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='27983561' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1047' name='TestCaseFails_Result' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
               <test-case id='0-1046' name='TestCaseFails_Result(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails_Result(31,11)' methodname='TestCaseFails_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='273492817' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1061' name='TestCaseIsExplicit' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Explicit' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Skipped' />
               </properties>
               <test-case id='0-1060' name='TestCaseIsExplicit(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit(31,11)' methodname='TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1294212716' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1055' name='TestCaseIsIgnored_Assert' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Assert' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Ignore' />
               </properties>
               <test-case id='0-1054' name='TestCaseIsIgnored_Assert(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Assert(31,11)' methodname='TestCaseIsIgnored_Assert' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='686381917' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1051' name='TestCaseIsIgnored_Attribute' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Ignored' testcasecount='1'>
               <properties>
                  <property name = '_SKIPREASON' value='Ignored test' />
                  <property name = 'Expect' value='Ignore' />
               </properties>
               <test-case id='0-1050' name='TestCaseIsIgnored_Attribute(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Attribute(31,11)' methodname='TestCaseIsIgnored_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1595827512' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1053' name='TestCaseIsIgnored_Property' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Ignore' />
               </properties>
               <test-case id='0-1052' name='TestCaseIsIgnored_Property(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Property(31,11)' methodname='TestCaseIsIgnored_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Ignored' seed='196688809'>
                  <properties>
                     <property name = '_SKIPREASON' value='Ignoring this' />
                  </properties>
               </test-case>
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1049' name='TestCaseIsInconclusive' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsInconclusive' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Inconclusive' />
               </properties>
               <test-case id='0-1048' name='TestCaseIsInconclusive(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsInconclusive(31,11)' methodname='TestCaseIsInconclusive' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='290615520' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1059' name='TestCaseIsSkipped_Attribute' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Skipped' testcasecount='1'>
               <properties>
                  <property name = '_SKIPREASON' value='Not supported on NET' />
                  <property name = 'Expect' value='Skipped' />
               </properties>
               <test-case id='0-1058' name='TestCaseIsSkipped_Attribute(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Attribute(31,11)' methodname='TestCaseIsSkipped_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1141864498' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1057' name='TestCaseIsSkipped_Property' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Skipped' />
               </properties>
               <test-case id='0-1056' name='TestCaseIsSkipped_Property(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Property(31,11)' methodname='TestCaseIsSkipped_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Skipped' seed='444590'>
                  <properties>
                     <property name = '_SKIPREASON' value='Not supported on NET' />
                  </properties>
               </test-case>
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1035' name='TestCaseSucceeds' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='3'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1032' name='TestCaseSucceeds(2,2,4)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds(2,2,4)' methodname='TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1433684738' />
               <test-case id='0-1033' name='TestCaseSucceeds(0,5,5)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds(0,5,5)' methodname='TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='373107807' />
               <test-case id='0-1034' name='TestCaseSucceeds(31,11,42)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds(31,11,42)' methodname='TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='179354264' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1039' name='TestCaseSucceeds_Result' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='3'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1036' name='TestCaseSucceeds_Result(2,2)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result(2,2)' methodname='TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='304550160' />
               <test-case id='0-1037' name='TestCaseSucceeds_Result(0,5)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result(0,5)' methodname='TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='555462209' />
               <test-case id='0-1038' name='TestCaseSucceeds_Result(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result(31,11)' methodname='TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='632312779' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1063' name='TestCaseThrowsException' fullname='NUnitTestDemo.ParameterizedTests.TestCaseThrowsException' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Error' />
               </properties>
               <test-case id='0-1062' name='TestCaseThrowsException(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseThrowsException(31,11)' methodname='TestCaseThrowsException' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1975282630' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1043' name='TestCaseWarns' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarns' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Warning' />
               </properties>
               <test-case id='0-1042' name='TestCaseWarns(31,11,99)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarns(31,11,99)' methodname='TestCaseWarns' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1115205847' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1045' name='TestCaseWarnsThreeTimes' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarnsThreeTimes' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Warning' />
               </properties>
               <test-case id='0-1044' name='TestCaseWarnsThreeTimes(31,11,99)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarnsThreeTimes(31,11,99)' methodname='TestCaseWarnsThreeTimes' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1560199924' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1065' name='TestCaseWithAlternateName' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithAlternateName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1064' name='AlternateTestName' fullname='NUnitTestDemo.ParameterizedTests.AlternateTestName' methodname='TestCaseWithAlternateName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='910339158' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1069' name='TestCaseWithRandomParameter' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameter' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1068' name='TestCaseWithRandomParameter(1397185164)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameter(1397185164)' methodname='TestCaseWithRandomParameter' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1749517851' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1072' name='TestCaseWithRandomParameterWithFixedNaming' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameterWithFixedNaming' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1070' name='TestCaseWithRandomParameterWithFixedNaming' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameterWithFixedNaming' methodname='TestCaseWithRandomParameterWithFixedNaming' classname='NUnitTestDemo.ParameterizedTests' runstate='NotRunnable' seed='646863619'>
                  <properties>
                     <property name = '_SKIPREASON' value='System.Reflection.TargetParameterCountException : Method requires 1 arguments but TestCaseAttribute only supplied 0' />
                     <property name = '_PROVIDERSTACKTRACE' value='   at NUnit.Framework.TestCaseAttribute.GetParametersForTestCase(IMethodInfo method) in D:\a\1\s\src\NUnitFramework\framework\Attributes\TestCaseAttribute.cs:line 329' />
                  </properties>
               </test-case>
               <test-case id='0-1071' name='TestCaseWithRandomParameterWithFixedNaming(1329261585)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameterWithFixedNaming(1329261585)' methodname='TestCaseWithRandomParameterWithFixedNaming' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='2084583686' />
            </test-suite>
            <test-suite type = 'ParameterizedMethod' id='0-1067' name='TestCaseWithSpecialCharInName' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithSpecialCharInName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1066' name='NameWithSpecialChar-&gt;Here' fullname='NUnitTestDemo.ParameterizedTests.NameWithSpecialChar-&gt;Here' methodname='TestCaseWithSpecialCharInName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1239650622' />
            </test-suite>
         </test-suite>
         <test-suite type = 'SetUpFixture' id='0-1142' name='SetUpFixture' fullname='NUnitTestDemo.SetUpFixture.SetUpFixture' classname='NUnitTestDemo.SetUpFixture.SetUpFixture' runstate='Runnable' testcasecount='2'>
            <test-suite type = 'TestFixture' id='0-1143' name='TestFixture1' fullname='NUnitTestDemo.SetUpFixture.TestFixture1' classname='NUnitTestDemo.SetUpFixture.TestFixture1' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1144' name='Test1' fullname='NUnitTestDemo.SetUpFixture.TestFixture1.Test1' methodname='Test1' classname='NUnitTestDemo.SetUpFixture.TestFixture1' runstate='Runnable' seed='1061190666' />
            </test-suite>
            <test-suite type = 'TestFixture' id='0-1145' name='TestFixture2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2' classname='NUnitTestDemo.SetUpFixture.TestFixture2' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1146' name='Test2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2.Test2' methodname='Test2' classname='NUnitTestDemo.SetUpFixture.TestFixture2' runstate='Runnable' seed='380453022' />
            </test-suite>
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1073' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' testcasecount='20'>
            <test-case id='0-1076' name='TestFails' fullname='NUnitTestDemo.SimpleTests.TestFails' methodname='TestFails' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1849669472'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1083' name='TestFails_StringEquality' fullname='NUnitTestDemo.SimpleTests.TestFails_StringEquality' methodname='TestFails_StringEquality' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='779570304'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1088' name='TestIsExplicit' fullname='NUnitTestDemo.SimpleTests.TestIsExplicit' methodname='TestIsExplicit' classname='NUnitTestDemo.SimpleTests' runstate='Explicit' seed='246701979'>
               <properties>
                  <property name = 'Expect' value='Skipped' />
               </properties>
            </test-case>
            <test-case id='0-1086' name='TestIsIgnored_Assert' fullname='NUnitTestDemo.SimpleTests.TestIsIgnored_Assert' methodname='TestIsIgnored_Assert' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='135801477'>
               <properties>
                  <property name = 'Expect' value='Ignore' />
               </properties>
            </test-case>
            <test-case id='0-1085' name='TestIsIgnored_Attribute' fullname='NUnitTestDemo.SimpleTests.TestIsIgnored_Attribute' methodname='TestIsIgnored_Attribute' classname='NUnitTestDemo.SimpleTests' runstate='Ignored' seed='862646740'>
               <properties>
                  <property name = '_SKIPREASON' value='Ignoring this test deliberately' />
                  <property name = 'Expect' value='Ignore' />
               </properties>
            </test-case>
            <test-case id='0-1084' name='TestIsInconclusive' fullname='NUnitTestDemo.SimpleTests.TestIsInconclusive' methodname='TestIsInconclusive' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='258284110'>
               <properties>
                  <property name = 'Expect' value='Inconclusive' />
               </properties>
            </test-case>
            <test-case id='0-1087' name='TestIsSkipped_Platform' fullname='NUnitTestDemo.SimpleTests.TestIsSkipped_Platform' methodname='TestIsSkipped_Platform' classname='NUnitTestDemo.SimpleTests' runstate='NotRunnable' seed='1190887260'>
               <properties>
                  <property name = 'Expect' value='Skipped' />
               <property name = '_SKIPREASON' value='Invalid platform name: Exclude=NET' /> 
               </properties>
            </test-case>
            <test-case id='0-1074' name='TestSucceeds' fullname='NUnitTestDemo.SimpleTests.TestSucceeds' methodname='TestSucceeds' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1648245250'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
            </test-case>
            <test-case id='0-1075' name='TestSucceeds_Message' fullname='NUnitTestDemo.SimpleTests.TestSucceeds_Message' methodname='TestSucceeds_Message' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1965948868'>
               <properties>
                  <property name = 'Expect' value='Pass' />
               </properties>
            </test-case>
            <test-case id='0-1089' name='TestThrowsException' fullname='NUnitTestDemo.SimpleTests.TestThrowsException' methodname='TestThrowsException' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='844161522'>
               <properties>
                  <property name = 'Expect' value='Error' />
               </properties>
            </test-case>
            <test-case id='0-1077' name='TestWarns' fullname='NUnitTestDemo.SimpleTests.TestWarns' methodname='TestWarns' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1230805102'>
               <properties>
                  <property name = 'Expect' value='Warning' />
               </properties>
            </test-case>
            <test-case id='0-1078' name='TestWarnsThreeTimes' fullname='NUnitTestDemo.SimpleTests.TestWarnsThreeTimes' methodname='TestWarnsThreeTimes' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='245628988'>
               <properties>
                  <property name = 'Expect' value='Warning' />
               </properties>
            </test-case>
            <test-case id='0-1092' name='TestWithCategory' fullname='NUnitTestDemo.SimpleTests.TestWithCategory' methodname='TestWithCategory' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='940987669'>
               <properties>
                  <property name = 'Expect' value='Pass' />
                  <property name = 'Category' value='Slow' />
               </properties>
            </test-case>
            <test-case id='0-1081' name='TestWithFailureAndWarning' fullname='NUnitTestDemo.SimpleTests.TestWithFailureAndWarning' methodname='TestWithFailureAndWarning' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1588349131'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1090' name='TestWithProperty' fullname='NUnitTestDemo.SimpleTests.TestWithProperty' methodname='TestWithProperty' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='157912654'>
               <properties>
                  <property name = 'Expect' value='Pass' />
                  <property name = 'Priority' value='High' />
               </properties>
            </test-case>
            <test-case id='0-1079' name='TestWithThreeFailures' fullname='NUnitTestDemo.SimpleTests.TestWithThreeFailures' methodname='TestWithThreeFailures' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='2031797509'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1093' name='TestWithTwoCategories' fullname='NUnitTestDemo.SimpleTests.TestWithTwoCategories' methodname='TestWithTwoCategories' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1258744592'>
               <properties>
                  <property name = 'Expect' value='Pass' />
                  <property name = 'Category' value='Slow' />
                  <property name = 'Category' value='Data' />
               </properties>
            </test-case>
            <test-case id='0-1080' name='TestWithTwoFailuresAndAnError' fullname='NUnitTestDemo.SimpleTests.TestWithTwoFailuresAndAnError' methodname='TestWithTwoFailuresAndAnError' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1704100307'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1082' name='TestWithTwoFailuresAndAWarning' fullname='NUnitTestDemo.SimpleTests.TestWithTwoFailuresAndAWarning' methodname='TestWithTwoFailuresAndAWarning' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1607248011'>
               <properties>
                  <property name = 'Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1091' name='TestWithTwoProperties' fullname='NUnitTestDemo.SimpleTests.TestWithTwoProperties' methodname='TestWithTwoProperties' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='451548879'>
               <properties>
                  <property name = 'Expect' value='Pass' />
                  <property name = 'Priority' value='Low' />
                  <property name = 'Action' value='Ignore' />
               </properties>
            </test-case>
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1137' name='TestCaseSourceTests' fullname='NUnitTestDemo.TestCaseSourceTests' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' testcasecount='3'>
            <test-suite type = 'ParameterizedMethod' id='0-1141' name='DivideTest' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' testcasecount='3'>
               <test-case id='0-1138' name='DivideTest(12,3)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,3)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='31883873' />
               <test-case id='0-1139' name='DivideTest(12,2)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,2)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='248147619' />
               <test-case id='0-1140' name='DivideTest(12,4)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,4)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='886237210' />
            </test-suite>
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1094' name='TextOutputTests' fullname='NUnitTestDemo.TextOutputTests' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' testcasecount='9'>
            <properties>
               <property name = 'Expect' value='Pass' />
            </properties>
            <test-case id='0-1103' name='DisplayTestParameters' fullname='NUnitTestDemo.TextOutputTests.DisplayTestParameters' methodname='DisplayTestParameters' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1760241230' />
            <test-case id='0-1102' name='DisplayTestSettings' fullname='NUnitTestDemo.TextOutputTests.DisplayTestSettings' methodname='DisplayTestSettings' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1707552967'>
               <properties>
                  <property name = 'Description' value='Displays various settings for verification' />
               </properties>
            </test-case>
            <test-case id='0-1095' name='WriteToConsole' fullname='NUnitTestDemo.TextOutputTests.WriteToConsole' methodname='WriteToConsole' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='408458095' />
            <test-case id='0-1096' name='WriteToError' fullname='NUnitTestDemo.TextOutputTests.WriteToError' methodname='WriteToError' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='613025007' />
            <test-case id='0-1097' name='WriteToTestContext' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContext' methodname='WriteToTestContext' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='634634417' />
            <test-case id='0-1099' name='WriteToTestContextError' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContextError' methodname='WriteToTestContextError' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='530580352' />
            <test-case id='0-1098' name='WriteToTestContextOut' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContextOut' methodname='WriteToTestContextOut' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1391668399' />
            <test-case id='0-1100' name='WriteToTestContextProgress' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContextProgress' methodname='WriteToTestContextProgress' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1548063414' />
            <test-case id='0-1101' name='WriteToTrace' fullname='NUnitTestDemo.TextOutputTests.WriteToTrace' methodname='WriteToTrace' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1811830740' />
         </test-suite>
         <test-suite type = 'TestFixture' id='0-1104' name='Theories' fullname='NUnitTestDemo.Theories' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='27'>
            <test-suite type = 'Theory' id='0-1114' name='Theory_AllCasesSucceed' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='9'>
               <properties>
                  <property name = '_JOINTYPE' value='Combinatorial' />
                  <property name = 'Expect' value='Pass' />
               </properties>
               <test-case id='0-1105' name='Theory_AllCasesSucceed(0,0)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(0,0)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1801427983' />
               <test-case id='0-1106' name='Theory_AllCasesSucceed(0,1)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(0,1)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1974701869' />
               <test-case id='0-1107' name='Theory_AllCasesSucceed(0,42)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(0,42)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1848026656' />
               <test-case id='0-1108' name='Theory_AllCasesSucceed(1,0)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(1,0)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1375050822' />
               <test-case id='0-1109' name='Theory_AllCasesSucceed(1,1)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(1,1)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='911946952' />
               <test-case id='0-1110' name='Theory_AllCasesSucceed(1,42)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(1,42)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='826668962' />
               <test-case id='0-1111' name='Theory_AllCasesSucceed(42,0)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(42,0)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='439716959' />
               <test-case id='0-1112' name='Theory_AllCasesSucceed(42,1)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(42,1)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1805291731' />
               <test-case id='0-1113' name='Theory_AllCasesSucceed(42,42)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(42,42)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1052223107' />
            </test-suite>
            <test-suite type = 'Theory' id='0-1124' name='Theory_SomeCasesAreInconclusive' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='9'>
               <properties>
                  <property name = '_JOINTYPE' value='Combinatorial' />
                  <property name = 'Expect' value='Mixed' />
               </properties>
               <test-case id='0-1115' name='Theory_SomeCasesAreInconclusive(0,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(0,0)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='498133950' />
               <test-case id='0-1116' name='Theory_SomeCasesAreInconclusive(0,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(0,1)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1050443426' />
               <test-case id='0-1117' name='Theory_SomeCasesAreInconclusive(0,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(0,42)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='363947577' />
               <test-case id='0-1118' name='Theory_SomeCasesAreInconclusive(1,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(1,0)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='2135465349' />
               <test-case id='0-1119' name='Theory_SomeCasesAreInconclusive(1,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(1,1)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='137768518' />
               <test-case id='0-1120' name='Theory_SomeCasesAreInconclusive(1,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(1,42)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1353278712' />
               <test-case id='0-1121' name='Theory_SomeCasesAreInconclusive(42,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(42,0)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1768896445' />
               <test-case id='0-1122' name='Theory_SomeCasesAreInconclusive(42,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(42,1)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='349919139' />
               <test-case id='0-1123' name='Theory_SomeCasesAreInconclusive(42,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(42,42)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='479917037' />
            </test-suite>
            <test-suite type = 'Theory' id='0-1134' name='Theory_SomeCasesFail' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='9'>
               <properties>
                  <property name = '_JOINTYPE' value='Combinatorial' />
                  <property name = 'Expect' value='Mixed' />
               </properties>
               <test-case id='0-1125' name='Theory_SomeCasesFail(0,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(0,0)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='952519801' />
               <test-case id='0-1126' name='Theory_SomeCasesFail(0,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(0,1)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1368102019' />
               <test-case id='0-1127' name='Theory_SomeCasesFail(0,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(0,42)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1741154704' />
               <test-case id='0-1128' name='Theory_SomeCasesFail(1,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(1,0)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='380699618' />
               <test-case id='0-1129' name='Theory_SomeCasesFail(1,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(1,1)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1878635502' />
               <test-case id='0-1130' name='Theory_SomeCasesFail(1,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(1,42)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1134588290' />
               <test-case id='0-1131' name='Theory_SomeCasesFail(42,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(42,0)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='240536290' />
               <test-case id='0-1132' name='Theory_SomeCasesFail(42,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(42,1)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='764028334' />
               <test-case id='0-1133' name='Theory_SomeCasesFail(42,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(42,42)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1214124262' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";



        [Test]
        public void ThatWeCanParseDiscoveryXml()
        {
            var logger = Substitute.For<ITestLogger>();
            var settings = Substitute.For<IAdapterSettings>();
            settings.DiscoveryMethod.Returns(DiscoveryMethod.Classic);
            var sut = new DiscoveryConverter();
            var ndr = sut.Convert(new NUnitResults(XmlHelper.CreateXmlNode(DiscoveryXml)), new TestConverter(logger, "whatever", settings));
            Assert.That(ndr.Id, Is.EqualTo("2"));
            Assert.That(ndr.TestAssembly, Is.Not.Null, "Missing test assembly");
            Assert.That(ndr.TestAssembly.NUnitTestDiscoveryProperties.Properties.Count(), Is.EqualTo(2));
            Assert.That(ndr.TestAssembly.NUnitTestDiscoveryProperties.AllInternal);


        }

    }
}
