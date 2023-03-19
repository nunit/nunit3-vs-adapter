using System.Linq;
using System.Xml;

using NSubstitute;

using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;
// ReSharper disable StringLiteralTypo

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    public class NUnitDiscoveryTests
    {
        private ITestLogger logger;
        private IAdapterSettings settings;

        private const string FullDiscoveryXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='108'>
   <test-suite type='Assembly' id='0-1157' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='108'>
      <properties>
         <property name='_PID' value='9856' />
         <property name='_APPDOMAIN' value='domain-807ad471-CSharpTestDemo.dll' />
      </properties>
         <test-suite type='TestSuite' id='0-1158' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='108'>
         <test-suite type='TestFixture' id='0-1004' name='AsyncTests' fullname='NUnitTestDemo.AsyncTests' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='7'>
            <test-case id='0-1007' name='AsyncTaskTestFails' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestFails' methodname='AsyncTaskTestFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='771768390'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1006' name='AsyncTaskTestSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestSucceeds' methodname='AsyncTaskTestSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1918603611'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
            </test-case>
            <test-case id='0-1008' name='AsyncTaskTestThrowsException' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestThrowsException' methodname='AsyncTaskTestThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1307982736'>
               <properties>
                  <property name='Expect' value='Error' />
               </properties>
            </test-case>
            <test-suite type='ParameterizedMethod' id='0-1012' name='AsyncTaskWithResultFails' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
               <test-case id='0-1011' name='AsyncTaskWithResultFails()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultFails()' methodname='AsyncTaskWithResultFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1830045852' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1010' name='AsyncTaskWithResultSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1009' name='AsyncTaskWithResultSucceeds()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultSucceeds()' methodname='AsyncTaskWithResultSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='175092178' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1014' name='AsyncTaskWithResultThrowsException' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Error' />
               </properties>
               <test-case id='0-1013' name='AsyncTaskWithResultThrowsException()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultThrowsException()' methodname='AsyncTaskWithResultThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='778775186' />
            </test-suite>
            <test-case id='0-1005' name='AsyncVoidTestIsInvalid' fullname='NUnitTestDemo.AsyncTests.AsyncVoidTestIsInvalid' methodname='AsyncVoidTestIsInvalid' classname='NUnitTestDemo.AsyncTests' runstate='NotRunnable' seed='1368779348'>
               <properties>
                  <property name='_SKIPREASON' value='Async test method must have non-void return type' />
                  <property name='Expect' value='Error' />
               </properties>
            </test-case>
         </test-suite>
         <test-suite type='TestFixture' id='0-1015' name='ConfigFileTests' fullname='NUnitTestDemo.ConfigFileTests' classname='NUnitTestDemo.ConfigFileTests' runstate='Runnable' testcasecount='2'>
            <properties>
               <property name='Expect' value='Pass' />
            </properties>
            <test-case id='0-1017' name='CanReadConfigFile' fullname='NUnitTestDemo.ConfigFileTests.CanReadConfigFile' methodname='CanReadConfigFile' classname='NUnitTestDemo.ConfigFileTests' runstate='Runnable' seed='676901490' />
            <test-case id='0-1016' name='ProperConfigFileIsUsed' fullname='NUnitTestDemo.ConfigFileTests.ProperConfigFileIsUsed' methodname='ProperConfigFileIsUsed' classname='NUnitTestDemo.ConfigFileTests' runstate='Runnable' seed='1953588481' />
         </test-suite>
         <test-suite type='TestFixture' id='0-1135' name='ExplicitClass' fullname='NUnitTestDemo.ExplicitClass' classname='NUnitTestDemo.ExplicitClass' runstate='Explicit' testcasecount='1'>
            <test-case id='0-1136' name='ThisIsIndirectlyExplicit' fullname='NUnitTestDemo.ExplicitClass.ThisIsIndirectlyExplicit' methodname='ThisIsIndirectlyExplicit' classname='NUnitTestDemo.ExplicitClass' runstate='Runnable' seed='1259099403' />
         </test-suite>
         <test-suite type='TestFixture' id='0-1000' name='FixtureWithApartmentAttributeOnClass' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass' runstate='Runnable' testcasecount='1'>
            <properties>
               <property name='ApartmentState' value='STA' />
            </properties>
            <test-case id='0-1001' name='TestMethodInSTAFixture' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass.TestMethodInSTAFixture' methodname='TestMethodInSTAFixture' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnClass' runstate='Runnable' seed='1032816623' />
         </test-suite>
         <test-suite type='TestFixture' id='0-1002' name='FixtureWithApartmentAttributeOnMethod' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod' runstate='Runnable' testcasecount='1'>
            <test-case id='0-1003' name='TestMethodInSTA' fullname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod.TestMethodInSTA' methodname='TestMethodInSTA' classname='NUnitTestDemo.FixtureWithApartmentAttributeOnMethod' runstate='Runnable' seed='1439800533'>
               <properties>
                  <property name='ApartmentState' value='STA' />
               </properties>
            </test-case>
         </test-suite>
         <test-suite type='GenericFixture' id='0-1025' name='GenericTests_IList&lt;TList&gt;' fullname='NUnitTestDemo.GenericTests_IList&lt;TList&gt;' runstate='Runnable' testcasecount='2'>
            <test-suite type='TestFixture' id='0-1021' name='GenericTests_IList&lt;ArrayList&gt;' fullname='NUnitTestDemo.GenericTests_IList&lt;ArrayList&gt;' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1022' name='CanAddToList' fullname='NUnitTestDemo.GenericTests_IList&lt;ArrayList&gt;.CanAddToList' methodname='CanAddToList' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' seed='879941949' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1023' name='GenericTests_IList&lt;List&lt;Int32&gt;&gt;' fullname='NUnitTestDemo.GenericTests_IList&lt;List&lt;Int32&gt;&gt;' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1024' name='CanAddToList' fullname='NUnitTestDemo.GenericTests_IList&lt;List&lt;Int32&gt;&gt;.CanAddToList' methodname='CanAddToList' classname='NUnitTestDemo.GenericTests_IList`1' runstate='Runnable' seed='2082560719' />
            </test-suite>
         </test-suite>
         <test-suite type='GenericFixture' id='0-1020' name='GenericTests&lt;T&gt;' fullname='NUnitTestDemo.GenericTests&lt;T&gt;' runstate='Runnable' testcasecount='1'>
            <test-suite type='TestFixture' id='0-1018' name='GenericTests&lt;Int32&gt;' fullname='NUnitTestDemo.GenericTests&lt;Int32&gt;' classname='NUnitTestDemo.GenericTests`1' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1019' name='TestIt' fullname='NUnitTestDemo.GenericTests&lt;Int32&gt;.TestIt' methodname='TestIt' classname='NUnitTestDemo.GenericTests`1' runstate='Runnable' seed='130064420'>
                  <properties>
                     <property name='Expect' value='Pass' />
                  </properties>
               </test-case>
            </test-suite>
         </test-suite>
         <test-suite type='TestFixture' id='0-1026' name='InheritedTestDerivedClass' fullname='NUnitTestDemo.InheritedTestDerivedClass' classname='NUnitTestDemo.InheritedTestDerivedClass' runstate='Runnable' testcasecount='1'>
            <test-case id='0-1027' name='TestInBaseClass' fullname='NUnitTestDemo.InheritedTestDerivedClass.TestInBaseClass' methodname='TestInBaseClass' classname='NUnitTestDemo.InheritedTestBaseClass' runstate='Runnable' seed='1519764308' />
         </test-suite>
         <test-suite type='TestFixture' id='0-1028' name='OneTimeSetUpTests' fullname='NUnitTestDemo.OneTimeSetUpTests' classname='NUnitTestDemo.OneTimeSetUpTests' runstate='Runnable' testcasecount='2'>
            <properties>
               <property name='Expect' value='Pass' />
            </properties>
            <test-case id='0-1029' name='Test1' fullname='NUnitTestDemo.OneTimeSetUpTests.Test1' methodname='Test1' classname='NUnitTestDemo.OneTimeSetUpTests' runstate='Runnable' seed='1237452323' />
            <test-case id='0-1030' name='Test2' fullname='NUnitTestDemo.OneTimeSetUpTests.Test2' methodname='Test2' classname='NUnitTestDemo.OneTimeSetUpTests' runstate='Runnable' seed='204346535' />
         </test-suite>
         <test-suite type='ParameterizedFixture' id='0-1151' name='ParameterizedTestFixture' fullname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' testcasecount='6'>
            <test-suite type='TestFixture' id='0-1142' name='ParameterizedTestFixture(""hello"",""hello"",""goodbye"")' fullname='NUnitTestDemo.ParameterizedTestFixture(""hello"",""hello"",""goodbye"")' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1143' name='TestEquality' fullname='NUnitTestDemo.ParameterizedTestFixture(""hello"",""hello"",""goodbye"").TestEquality' methodname='TestEquality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='1603810031' />
               <test-case id='0-1144' name='TestInequality' fullname='NUnitTestDemo.ParameterizedTestFixture(""hello"",""hello"",""goodbye"").TestInequality' methodname='TestInequality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='272235298' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1145' name='ParameterizedTestFixture(""zip"",""zip"")' fullname='NUnitTestDemo.ParameterizedTestFixture(""zip"",""zip"")' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1146' name='TestEquality' fullname='NUnitTestDemo.ParameterizedTestFixture(""zip"",""zip"").TestEquality' methodname='TestEquality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='297749680' />
               <test-case id='0-1147' name='TestInequality' fullname='NUnitTestDemo.ParameterizedTestFixture(""zip"",""zip"").TestInequality' methodname='TestInequality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='270828005' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1148' name='ParameterizedTestFixture(42,42,99)' fullname='NUnitTestDemo.ParameterizedTestFixture(42,42,99)' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1149' name='TestEquality' fullname='NUnitTestDemo.ParameterizedTestFixture(42,42,99).TestEquality' methodname='TestEquality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='1055390819' />
               <test-case id='0-1150' name='TestInequality' fullname='NUnitTestDemo.ParameterizedTestFixture(42,42,99).TestInequality' methodname='TestInequality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='889637628' />
            </test-suite>
         </test-suite>
         <test-suite type='TestFixture' id='0-1031' name='ParameterizedTests' fullname='NUnitTestDemo.ParameterizedTests' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='23'>
            <test-suite type='ParameterizedMethod' id='0-1041' name='TestCaseFails' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
               <test-case id='0-1040' name='TestCaseFails(31,11,99)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails(31,11,99)' methodname='TestCaseFails' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='834237553' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1047' name='TestCaseFails_Result' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
               <test-case id='0-1046' name='TestCaseFails_Result(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseFails_Result(31,11)' methodname='TestCaseFails_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1915510679' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1061' name='TestCaseIsExplicit' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Explicit' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Skipped' />
               </properties>
               <test-case id='0-1060' name='TestCaseIsExplicit(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit(31,11)' methodname='TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='2121477676' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1055' name='TestCaseIsIgnored_Assert' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Assert' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Ignore' />
               </properties>
               <test-case id='0-1054' name='TestCaseIsIgnored_Assert(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Assert(31,11)' methodname='TestCaseIsIgnored_Assert' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1371891043' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1051' name='TestCaseIsIgnored_Attribute' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Ignored' testcasecount='1'>
               <properties>
                  <property name='_SKIPREASON' value='Ignored test' />
                  <property name='Expect' value='Ignore' />
               </properties>
               <test-case id='0-1050' name='TestCaseIsIgnored_Attribute(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Attribute(31,11)' methodname='TestCaseIsIgnored_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='2018055565' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1053' name='TestCaseIsIgnored_Property' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Ignore' />
               </properties>
               <test-case id='0-1052' name='TestCaseIsIgnored_Property(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsIgnored_Property(31,11)' methodname='TestCaseIsIgnored_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Ignored' seed='458259159'>
                  <properties>
                     <property name='_SKIPREASON' value='Ignoring this' />
                  </properties>
               </test-case>
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1049' name='TestCaseIsInconclusive' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsInconclusive' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Inconclusive' />
               </properties>
               <test-case id='0-1048' name='TestCaseIsInconclusive(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsInconclusive(31,11)' methodname='TestCaseIsInconclusive' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1073275212' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1059' name='TestCaseIsSkipped_Attribute' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Skipped' testcasecount='1'>
               <properties>
                  <property name='_SKIPREASON' value='Not supported on NET' />
                  <property name='Expect' value='Skipped' />
               </properties>
               <test-case id='0-1058' name='TestCaseIsSkipped_Attribute(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Attribute(31,11)' methodname='TestCaseIsSkipped_Attribute' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='712512917' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1057' name='TestCaseIsSkipped_Property' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Skipped' />
               </properties>
               <test-case id='0-1056' name='TestCaseIsSkipped_Property(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsSkipped_Property(31,11)' methodname='TestCaseIsSkipped_Property' classname='NUnitTestDemo.ParameterizedTests' runstate='Skipped' seed='750348381'>
                  <properties>
                     <property name='_SKIPREASON' value='Not supported on NET' />
                  </properties>
               </test-case>
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1035' name='TestCaseSucceeds' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='3'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1032' name='TestCaseSucceeds(2,2,4)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds(2,2,4)' methodname='TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='218080466' />
               <test-case id='0-1033' name='TestCaseSucceeds(0,5,5)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds(0,5,5)' methodname='TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='169775980' />
               <test-case id='0-1034' name='TestCaseSucceeds(31,11,42)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds(31,11,42)' methodname='TestCaseSucceeds' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1283490061' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1039' name='TestCaseSucceeds_Result' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='3'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1036' name='TestCaseSucceeds_Result(2,2)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result(2,2)' methodname='TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1501681017' />
               <test-case id='0-1037' name='TestCaseSucceeds_Result(0,5)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result(0,5)' methodname='TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1559240459' />
               <test-case id='0-1038' name='TestCaseSucceeds_Result(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseSucceeds_Result(31,11)' methodname='TestCaseSucceeds_Result' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1262629200' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1063' name='TestCaseThrowsException' fullname='NUnitTestDemo.ParameterizedTests.TestCaseThrowsException' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Error' />
               </properties>
               <test-case id='0-1062' name='TestCaseThrowsException(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseThrowsException(31,11)' methodname='TestCaseThrowsException' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1261348723' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1043' name='TestCaseWarns' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarns' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Warning' />
               </properties>
               <test-case id='0-1042' name='TestCaseWarns(31,11,99)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarns(31,11,99)' methodname='TestCaseWarns' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1417543801' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1045' name='TestCaseWarnsThreeTimes' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarnsThreeTimes' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Warning' />
               </properties>
               <test-case id='0-1044' name='TestCaseWarnsThreeTimes(31,11,99)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWarnsThreeTimes(31,11,99)' methodname='TestCaseWarnsThreeTimes' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='271344885' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1065' name='TestCaseWithAlternateName' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithAlternateName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1064' name='AlternateTestName' fullname='NUnitTestDemo.ParameterizedTests.AlternateTestName' methodname='TestCaseWithAlternateName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='450773561' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1069' name='TestCaseWithRandomParameter' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameter' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1068' name='TestCaseWithRandomParameter(1787281192)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameter(1787281192)' methodname='TestCaseWithRandomParameter' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1248901625' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1072' name='TestCaseWithRandomParameterWithFixedNaming' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameterWithFixedNaming' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1070' name='TestCaseWithRandomParameterWithFixedNaming' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameterWithFixedNaming' methodname='TestCaseWithRandomParameterWithFixedNaming' classname='NUnitTestDemo.ParameterizedTests' runstate='NotRunnable' seed='216518253'>
                  <properties>
                     <property name='_SKIPREASON' value='System.Reflection.TargetParameterCountException : Method requires 1 arguments but TestCaseAttribute only supplied 0' />
                     <property name='_PROVIDERSTACKTRACE' value='   at NUnit.Framework.TestCaseAttribute.GetParametersForTestCase(IMethodInfo method) in D:\a\1\s\src\NUnitFramework\framework\Attributes\TestCaseAttribute.cs:line 329' />
                  </properties>
               </test-case>
               <test-case id='0-1071' name='TestCaseWithRandomParameterWithFixedNaming(1325111790)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithRandomParameterWithFixedNaming(1325111790)' methodname='TestCaseWithRandomParameterWithFixedNaming' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='214923416' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1067' name='TestCaseWithSpecialCharInName' fullname='NUnitTestDemo.ParameterizedTests.TestCaseWithSpecialCharInName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1066' name='NameWithSpecialChar-&gt;Here' fullname='NUnitTestDemo.ParameterizedTests.NameWithSpecialChar-&gt;Here' methodname='TestCaseWithSpecialCharInName' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='337857533' />
            </test-suite>
         </test-suite>
         <test-suite type='SetUpFixture' id='0-1152' name='SetUpFixture' fullname='NUnitTestDemo.SetUpFixture.SetUpFixture' classname='NUnitTestDemo.SetUpFixture.SetUpFixture' runstate='Runnable' testcasecount='2'>
            <test-suite type='TestFixture' id='0-1153' name='TestFixture1' fullname='NUnitTestDemo.SetUpFixture.TestFixture1' classname='NUnitTestDemo.SetUpFixture.TestFixture1' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1154' name='Test1' fullname='NUnitTestDemo.SetUpFixture.TestFixture1.Test1' methodname='Test1' classname='NUnitTestDemo.SetUpFixture.TestFixture1' runstate='Runnable' seed='56266244' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1155' name='TestFixture2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2' classname='NUnitTestDemo.SetUpFixture.TestFixture2' runstate='Runnable' testcasecount='1'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1156' name='Test2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2.Test2' methodname='Test2' classname='NUnitTestDemo.SetUpFixture.TestFixture2' runstate='Runnable' seed='1924396815' />
            </test-suite>
         </test-suite>
         <test-suite type='TestFixture' id='0-1073' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' testcasecount='20'>
            <test-case id='0-1076' name='TestFails' fullname='NUnitTestDemo.SimpleTests.TestFails' methodname='TestFails' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='664323801'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1083' name='TestFails_StringEquality' fullname='NUnitTestDemo.SimpleTests.TestFails_StringEquality' methodname='TestFails_StringEquality' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1599618939'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1088' name='TestIsExplicit' fullname='NUnitTestDemo.SimpleTests.TestIsExplicit' methodname='TestIsExplicit' classname='NUnitTestDemo.SimpleTests' runstate='Explicit' seed='1554997188'>
               <properties>
                  <property name='Expect' value='Skipped' />
               </properties>
            </test-case>
            <test-case id='0-1086' name='TestIsIgnored_Assert' fullname='NUnitTestDemo.SimpleTests.TestIsIgnored_Assert' methodname='TestIsIgnored_Assert' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1962471277'>
               <properties>
                  <property name='Expect' value='Ignore' />
               </properties>
            </test-case>
            <test-case id='0-1085' name='TestIsIgnored_Attribute' fullname='NUnitTestDemo.SimpleTests.TestIsIgnored_Attribute' methodname='TestIsIgnored_Attribute' classname='NUnitTestDemo.SimpleTests' runstate='Ignored' seed='1497112956'>
               <properties>
                  <property name='_SKIPREASON' value='Ignoring this test deliberately' />
                  <property name='Expect' value='Ignore' />
               </properties>
            </test-case>
            <test-case id='0-1084' name='TestIsInconclusive' fullname='NUnitTestDemo.SimpleTests.TestIsInconclusive' methodname='TestIsInconclusive' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1420725265'>
               <properties>
                  <property name='Expect' value='Inconclusive' />
               </properties>
            </test-case>
            <test-case id='0-1087' name='TestIsSkipped_Platform' fullname='NUnitTestDemo.SimpleTests.TestIsSkipped_Platform' methodname='TestIsSkipped_Platform' classname='NUnitTestDemo.SimpleTests' runstate='NotRunnable' seed='250061069'>
               <properties>
                  <property name='Expect' value='Skipped' />
                  <property name='_SKIPREASON' value='Invalid platform name: Exclude=""NET""' />
               </properties>
            </test-case>
            <test-case id='0-1074' name='TestSucceeds' fullname='NUnitTestDemo.SimpleTests.TestSucceeds' methodname='TestSucceeds' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1794834702'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
            </test-case>
            <test-case id='0-1075' name='TestSucceeds_Message' fullname='NUnitTestDemo.SimpleTests.TestSucceeds_Message' methodname='TestSucceeds_Message' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1551974055'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
            </test-case>
            <test-case id='0-1089' name='TestThrowsException' fullname='NUnitTestDemo.SimpleTests.TestThrowsException' methodname='TestThrowsException' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='894649592'>
               <properties>
                  <property name='Expect' value='Error' />
               </properties>
            </test-case>
            <test-case id='0-1077' name='TestWarns' fullname='NUnitTestDemo.SimpleTests.TestWarns' methodname='TestWarns' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='344483815'>
               <properties>
                  <property name='Expect' value='Warning' />
               </properties>
            </test-case>
            <test-case id='0-1078' name='TestWarnsThreeTimes' fullname='NUnitTestDemo.SimpleTests.TestWarnsThreeTimes' methodname='TestWarnsThreeTimes' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1176908295'>
               <properties>
                  <property name='Expect' value='Warning' />
               </properties>
            </test-case>
            <test-case id='0-1092' name='TestWithCategory' fullname='NUnitTestDemo.SimpleTests.TestWithCategory' methodname='TestWithCategory' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1683743891'>
               <properties>
                  <property name='Expect' value='Pass' />
                  <property name='Category' value='Slow' />
               </properties>
            </test-case>
            <test-case id='0-1081' name='TestWithFailureAndWarning' fullname='NUnitTestDemo.SimpleTests.TestWithFailureAndWarning' methodname='TestWithFailureAndWarning' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='380112843'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1090' name='TestWithProperty' fullname='NUnitTestDemo.SimpleTests.TestWithProperty' methodname='TestWithProperty' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1583983115'>
               <properties>
                  <property name='Expect' value='Pass' />
                  <property name='Priority' value='High' />
               </properties>
            </test-case>
            <test-case id='0-1079' name='TestWithThreeFailures' fullname='NUnitTestDemo.SimpleTests.TestWithThreeFailures' methodname='TestWithThreeFailures' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1223327733'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1093' name='TestWithTwoCategories' fullname='NUnitTestDemo.SimpleTests.TestWithTwoCategories' methodname='TestWithTwoCategories' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='705800364'>
               <properties>
                  <property name='Expect' value='Pass' />
                  <property name='Category' value='Slow' />
                  <property name='Category' value='Data' />
               </properties>
            </test-case>
            <test-case id='0-1080' name='TestWithTwoFailuresAndAnError' fullname='NUnitTestDemo.SimpleTests.TestWithTwoFailuresAndAnError' methodname='TestWithTwoFailuresAndAnError' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='1061304289'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1082' name='TestWithTwoFailuresAndAWarning' fullname='NUnitTestDemo.SimpleTests.TestWithTwoFailuresAndAWarning' methodname='TestWithTwoFailuresAndAWarning' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='982769804'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1091' name='TestWithTwoProperties' fullname='NUnitTestDemo.SimpleTests.TestWithTwoProperties' methodname='TestWithTwoProperties' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='702450911'>
               <properties>
                  <property name='Expect' value='Pass' />
                  <property name='Priority' value='Low' />
                  <property name='Action' value='Ignore' />
               </properties>
            </test-case>
         </test-suite>
         <test-suite type='TestFixture' id='0-1137' name='TestCaseSourceTests' fullname='NUnitTestDemo.TestCaseSourceTests' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' testcasecount='3'>
            <test-suite type='ParameterizedMethod' id='0-1141' name='DivideTest' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' testcasecount='3'>
               <test-case id='0-1138' name='DivideTest(12,3)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,3)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='411915691' />
               <test-case id='0-1139' name='DivideTest(12,2)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,2)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='303613405' />
               <test-case id='0-1140' name='DivideTest(12,4)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,4)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='1073425063' />
            </test-suite>
         </test-suite>
         <test-suite type='TestFixture' id='0-1094' name='TextOutputTests' fullname='NUnitTestDemo.TextOutputTests' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' testcasecount='9'>
            <properties>
               <property name='Expect' value='Pass' />
            </properties>
            <test-case id='0-1103' name='DisplayTestParameters' fullname='NUnitTestDemo.TextOutputTests.DisplayTestParameters' methodname='DisplayTestParameters' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1255004500' />
            <test-case id='0-1102' name='DisplayTestSettings' fullname='NUnitTestDemo.TextOutputTests.DisplayTestSettings' methodname='DisplayTestSettings' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='2022379752'>
               <properties>
                  <property name='Description' value='Displays various settings for verification' />
               </properties>
            </test-case>
            <test-case id='0-1095' name='WriteToConsole' fullname='NUnitTestDemo.TextOutputTests.WriteToConsole' methodname='WriteToConsole' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='499152285' />
            <test-case id='0-1096' name='WriteToError' fullname='NUnitTestDemo.TextOutputTests.WriteToError' methodname='WriteToError' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='148858386' />
            <test-case id='0-1097' name='WriteToTestContext' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContext' methodname='WriteToTestContext' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='583572471' />
            <test-case id='0-1099' name='WriteToTestContextError' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContextError' methodname='WriteToTestContextError' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1668790421' />
            <test-case id='0-1098' name='WriteToTestContextOut' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContextOut' methodname='WriteToTestContextOut' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1175978318' />
            <test-case id='0-1100' name='WriteToTestContextProgress' fullname='NUnitTestDemo.TextOutputTests.WriteToTestContextProgress' methodname='WriteToTestContextProgress' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1300651902' />
            <test-case id='0-1101' name='WriteToTrace' fullname='NUnitTestDemo.TextOutputTests.WriteToTrace' methodname='WriteToTrace' classname='NUnitTestDemo.TextOutputTests' runstate='Runnable' seed='1132298491' />
         </test-suite>
         <test-suite type='TestFixture' id='0-1104' name='Theories' fullname='NUnitTestDemo.Theories' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='27'>
            <test-suite type='Theory' id='0-1114' name='Theory_AllCasesSucceed' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='9'>
               <properties>
                  <property name='_JOINTYPE' value='Combinatorial' />
                  <property name='Expect' value='Pass' />
               </properties>
               <test-case id='0-1105' name='Theory_AllCasesSucceed(0,0)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(0,0)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='517808090' />
               <test-case id='0-1106' name='Theory_AllCasesSucceed(0,1)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(0,1)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1765293654' />
               <test-case id='0-1107' name='Theory_AllCasesSucceed(0,42)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(0,42)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1811935833' />
               <test-case id='0-1108' name='Theory_AllCasesSucceed(1,0)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(1,0)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1966725421' />
               <test-case id='0-1109' name='Theory_AllCasesSucceed(1,1)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(1,1)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='605556575' />
               <test-case id='0-1110' name='Theory_AllCasesSucceed(1,42)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(1,42)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1040383709' />
               <test-case id='0-1111' name='Theory_AllCasesSucceed(42,0)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(42,0)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='657673472' />
               <test-case id='0-1112' name='Theory_AllCasesSucceed(42,1)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(42,1)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1042500770' />
               <test-case id='0-1113' name='Theory_AllCasesSucceed(42,42)' fullname='NUnitTestDemo.Theories.Theory_AllCasesSucceed(42,42)' methodname='Theory_AllCasesSucceed' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='145107667' />
            </test-suite>
            <test-suite type='Theory' id='0-1124' name='Theory_SomeCasesAreInconclusive' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='9'>
               <properties>
                  <property name='_JOINTYPE' value='Combinatorial' />
                  <property name='Expect' value='Mixed' />
               </properties>
               <test-case id='0-1115' name='Theory_SomeCasesAreInconclusive(0,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(0,0)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='856382451' />
               <test-case id='0-1116' name='Theory_SomeCasesAreInconclusive(0,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(0,1)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='779713124' />
               <test-case id='0-1117' name='Theory_SomeCasesAreInconclusive(0,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(0,42)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='397402022' />
               <test-case id='0-1118' name='Theory_SomeCasesAreInconclusive(1,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(1,0)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1716122258' />
               <test-case id='0-1119' name='Theory_SomeCasesAreInconclusive(1,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(1,1)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='277984056' />
               <test-case id='0-1120' name='Theory_SomeCasesAreInconclusive(1,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(1,42)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1042633645' />
               <test-case id='0-1121' name='Theory_SomeCasesAreInconclusive(42,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(42,0)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1484009345' />
               <test-case id='0-1122' name='Theory_SomeCasesAreInconclusive(42,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(42,1)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1436803723' />
               <test-case id='0-1123' name='Theory_SomeCasesAreInconclusive(42,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesAreInconclusive(42,42)' methodname='Theory_SomeCasesAreInconclusive' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='266670815' />
            </test-suite>
            <test-suite type='Theory' id='0-1134' name='Theory_SomeCasesFail' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' testcasecount='9'>
               <properties>
                  <property name='_JOINTYPE' value='Combinatorial' />
                  <property name='Expect' value='Mixed' />
               </properties>
               <test-case id='0-1125' name='Theory_SomeCasesFail(0,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(0,0)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1942687535' />
               <test-case id='0-1126' name='Theory_SomeCasesFail(0,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(0,1)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1651837205' />
               <test-case id='0-1127' name='Theory_SomeCasesFail(0,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(0,42)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='18903538' />
               <test-case id='0-1128' name='Theory_SomeCasesFail(1,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(1,0)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='65078373' />
               <test-case id='0-1129' name='Theory_SomeCasesFail(1,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(1,1)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='517378582' />
               <test-case id='0-1130' name='Theory_SomeCasesFail(1,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(1,42)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1234934953' />
               <test-case id='0-1131' name='Theory_SomeCasesFail(42,0)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(42,0)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='1167797098' />
               <test-case id='0-1132' name='Theory_SomeCasesFail(42,1)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(42,1)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='195996329' />
               <test-case id='0-1133' name='Theory_SomeCasesFail(42,42)' fullname='NUnitTestDemo.Theories.Theory_SomeCasesFail(42,42)' methodname='Theory_SomeCasesFail' classname='NUnitTestDemo.Theories' runstate='Runnable' seed='258037408' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        [SetUp]
        public void SetUp()
        {
            logger = Substitute.For<ITestLogger>();
            settings = Substitute.For<IAdapterSettings>();
            settings.DiscoveryMethod.Returns(DiscoveryMethod.Legacy);
        }


        [Test]
        public void ThatWeCanParseDiscoveryXml()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(FullDiscoveryXml)));

            Assert.That(ndr.Id, Is.EqualTo("2"));
            Assert.That(ndr.TestAssembly, Is.Not.Null, "Missing test assembly");
            Assert.That(ndr.TestAssembly.NUnitDiscoveryProperties.Properties.Count(), Is.EqualTo(2));
            Assert.That(ndr.TestAssembly.NUnitDiscoveryProperties.AllInternal);
            var suite = ndr.TestAssembly.TestSuites.SingleOrDefault();
            Assert.That(suite, Is.Not.Null, "No top level suite");
            var fixturesCount = suite.TestFixtures.Count();
            var genericFixturesCount = suite.GenericFixtures.Count();
            var parameterizedFicturesCount = suite.ParameterizedFixtures.Count();
            var setupFixturesCount = suite.SetUpFixtures.Count();
            Assert.Multiple(() =>
            {
                Assert.That(fixturesCount, Is.EqualTo(12), nameof(fixturesCount));
                Assert.That(genericFixturesCount, Is.EqualTo(2), nameof(genericFixturesCount));
                Assert.That(parameterizedFicturesCount, Is.EqualTo(1), nameof(parameterizedFicturesCount));
                Assert.That(setupFixturesCount, Is.EqualTo(1), nameof(setupFixturesCount));
            });
        }


        private const string SimpleTestXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='1'>
   <test-suite type='Assembly' id='0-1160' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='1'>
      <test-suite type='TestSuite' id='0-1161' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='1'>
         <test-suite type='TestFixture' id='0-1162' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' runstate='Runnable' testcasecount='1'>
            <test-case id='0-1074' name='TestSucceeds' fullname='NUnitTestDemo.SimpleTests.TestSucceeds' methodname='TestSucceeds' classname='NUnitTestDemo.SimpleTests' runstate='Runnable' seed='296066266'>
               <properties>
                  <property name='Category' value='Whatever' />
                  <property name='Expect' value='Pass' />
               </properties>
            </test-case>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        [Test]
        public void ThatTestCaseHasAllData()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(SimpleTestXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            var testCase = topLevelSuite.TestFixtures.First().TestCases.First();
            Assert.Multiple(() =>
            {
                Assert.That(testCase.Id, Is.EqualTo("0-1074"), "Id fails");
                Assert.That(testCase.Name, Is.EqualTo("TestSucceeds"), "Name fails");
                Assert.That(testCase.FullName, Is.EqualTo("NUnitTestDemo.SimpleTests.TestSucceeds"), "Fullname fails");
                Assert.That(testCase.MethodName, Is.EqualTo("TestSucceeds"), "Methodname fails");
                Assert.That(testCase.ClassName, Is.EqualTo("NUnitTestDemo.SimpleTests"), "Classname fails");
                Assert.That(testCase.RunState, Is.EqualTo(RunStateEnum.Runnable), "Runstate fails");
                Assert.That(testCase.Seed, Is.EqualTo(296066266), "Seed fails");
                Assert.That(
                    testCase.NUnitDiscoveryProperties.Properties.Single(o => o.Name == "Category").Value,
                    Is.EqualTo("Whatever"));
                Assert.That(testCase.Parent, Is.Not.Null, "Parent missing");
            });
        }




        [Test]
        public void ThatNumberOfTestCasesAreCorrect()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(FullDiscoveryXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            var count = topLevelSuite.TestCaseCount;
            Assert.That(count, Is.EqualTo(108));
            var actualCount = topLevelSuite.NoOfActualTestCases;

            Assert.Multiple(() =>
            {
                // Special count checks for some
                Assert.That(topLevelSuite.GenericFixtures.Sum(o => o.NoOfActualTestCases), Is.EqualTo(3),
                    "Generic fixtures counts fails in itself");

                // Test Case count checks
                Assert.That(actualCount, Is.EqualTo(count), "Actual count doesn't match given count");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "AsyncTests").Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(7), "Asynctests wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "ConfigFileTests").Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(2), "ConfigFileTests wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "ExplicitClass").Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(1), "ExplicitClass wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "FixtureWithApartmentAttributeOnClass")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(1), "FixtureWithApartmentAttributeOnClass wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "FixtureWithApartmentAttributeOnMethod")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(1), "FixtureWithApartmentAttributeOnMethod wrong");
                Assert.That(
                    topLevelSuite.GenericFixtures.Where(o => o.Name == "GenericTests_IList<TList>")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(2), "GenericTests_IList&lt;TList&gt; wrong");
                Assert.That(
                    topLevelSuite.GenericFixtures.Where(o => o.Name == "GenericTests<T>")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(1), "GenericTests&lt;T&gt; wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "InheritedTestDerivedClass")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(1), "InheritedTestDerivedClass wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "OneTimeSetUpTests")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(2), "OneTimeSetUpTests wrong");
                Assert.That(
                    topLevelSuite.ParameterizedFixtures.Where(o => o.Name == "ParameterizedTestFixture")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(6), "ParameterizedTestFixture wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "ParameterizedTests")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(23), "ParameterizedTests wrong");
                Assert.That(
                    topLevelSuite.SetUpFixtures.Where(o => o.Name == "SetUpFixture").Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(2), "SetUpFixture wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "SimpleTests").Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(20), "SimpleTests wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "TestCaseSourceTests")
                        .Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(3), "TestCaseSourceTests wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "TextOutputTests").Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(9), "TextOutputTests wrong");
                Assert.That(
                    topLevelSuite.TestFixtures.Where(o => o.Name == "Theories").Sum(o => o.NoOfActualTestCases),
                    Is.EqualTo(27), "Theories wrong");
            });
            Assert.That(ndr.TestAssembly.AllTestCases.Count, Is.EqualTo(108));
        }

        private const string SetupFixtureXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='2'>
   <test-suite type='Assembly' id='0-1160' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='2'>
      <test-suite type='TestSuite' id='0-1161' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='2'>
         <test-suite type='SetUpFixture' id='0-1162' name='SetUpFixture' fullname='NUnitTestDemo.SetUpFixture.SetUpFixture' runstate='Runnable' testcasecount='2'>
            <test-suite type='TestFixture' id='0-1163' name='TestFixture1' fullname='NUnitTestDemo.SetUpFixture.TestFixture1' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1154' name='Test1' fullname='NUnitTestDemo.SetUpFixture.TestFixture1.Test1' methodname='Test1' classname='NUnitTestDemo.SetUpFixture.TestFixture1' runstate='Runnable' seed='1119121101' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1164' name='TestFixture2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1156' name='Test2' fullname='NUnitTestDemo.SetUpFixture.TestFixture2.Test2' methodname='Test2' classname='NUnitTestDemo.SetUpFixture.TestFixture2' runstate='Runnable' seed='1598200053' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        [Test]
        public void ThatSetUpFixtureWorks()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(SetupFixtureXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();

            Assert.That(topLevelSuite.SetUpFixtures.Count, Is.EqualTo(1), "Setupfixture count");
            foreach (var setupFixture in topLevelSuite.SetUpFixtures)
            {
                Assert.That(setupFixture.TestFixtures.Count, Is.EqualTo(2), "Test fixtures count");
                Assert.That(setupFixture.RunState, Is.EqualTo(RunStateEnum.Runnable),
                    "Runstate fails for setupfixture");
                foreach (var testFixture in setupFixture.TestFixtures)
                {
                    Assert.That(testFixture.RunState, Is.EqualTo(RunStateEnum.Runnable),
                        "Runstate fails for testFixture");
                    Assert.That(testFixture.TestCases.Count, Is.EqualTo(1), "Testcase count per fixture");
                    foreach (var testCase in testFixture.TestCases)
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(testCase.Name, Does.StartWith("Test"), "Name is wrong");
                            Assert.That(testCase.FullName, Does.StartWith("NUnitTestDemo.SetUpFixture.TestFixture"));
                            Assert.That(testCase.MethodName, Does.StartWith("Test"), "MethodName is wrong");
                            Assert.That(testCase.ClassName, Does.StartWith("NUnitTestDemo.SetUpFixture.TestFixture"),
                                "Name is wrong");
                            Assert.That(testCase.RunState, Is.EqualTo(RunStateEnum.Runnable),
                                "Runstate fails for testCase");
                            Assert.That(testCase.Seed, Is.GreaterThan(0), "Seed missing");
                        });
                    }
                }
            }
        }

        private const string ParameterizedMethodXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='3'>
   <test-suite type='Assembly' id='0-1160' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='3'>
      <test-suite type='TestSuite' id='0-1161' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='3'>
         <test-suite type='TestFixture' id='0-1162' name='TestCaseSourceTests' fullname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' testcasecount='3'>
            <test-suite type='ParameterizedMethod' id='0-1163' name='DivideTest' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' testcasecount='3'>
               <test-case id='0-1138' name='DivideTest(12,3)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,3)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='1090869545' />
               <test-case id='0-1139' name='DivideTest(12,2)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,2)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='1213876025' />
               <test-case id='0-1140' name='DivideTest(12,4)' fullname='NUnitTestDemo.TestCaseSourceTests.DivideTest(12,4)' methodname='DivideTest' classname='NUnitTestDemo.TestCaseSourceTests' runstate='Runnable' seed='1587561378' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";


        [Test]
        public void ThatParameterizedMethodsWorks()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(ParameterizedMethodXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            Assert.That(topLevelSuite.TestCaseCount, Is.EqualTo(3), "Count number from NUnit is wrong");
            Assert.That(topLevelSuite.TestFixtures.Count, Is.EqualTo(1), "Missing text fixture");
            Assert.That(topLevelSuite.TestFixtures.Single().ParameterizedMethods.Count(), Is.EqualTo(1),
                "Missing parameterizedMethod");
            Assert.That(
                topLevelSuite.TestFixtures.Single().ParameterizedMethods.Single().TestCases.Count,
                Is.EqualTo(3));
        }

        [Test]
        public void ThatTheoryWorks()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(FullDiscoveryXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            var theoryFixture = topLevelSuite.TestFixtures.FirstOrDefault(o => o.Name == "Theories");
            Assert.That(theoryFixture, Is.Not.Null);
        }


        private const string ExplicitXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='3'>
   <test-suite type='Assembly' id='0-1160' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='3'>
      <test-suite type='TestSuite' id='0-1161' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='3'>
         <test-suite type='TestFixture' id='0-1162' name='ExplicitClass' fullname='NUnitTestDemo.ExplicitClass' runstate='Explicit' testcasecount='1'>
            <test-case id='0-1136' name='ThisIsIndirectlyExplicit' fullname='NUnitTestDemo.ExplicitClass.ThisIsIndirectlyExplicit' methodname='ThisIsIndirectlyExplicit' classname='NUnitTestDemo.ExplicitClass' runstate='Runnable' seed='289706323' />
         </test-suite>
         <test-suite type='TestFixture' id='0-1163' name='ParameterizedTests' fullname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
            <test-suite type='ParameterizedMethod' id='0-1164' name='TestCaseIsExplicit' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Explicit' testcasecount='1'>
               <test-case id='0-1060' name='TestCaseIsExplicit(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit(31,11)' methodname='TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1206901902' />
            </test-suite>
         </test-suite>
         <test-suite type='TestFixture' id='0-1165' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' runstate='Runnable' testcasecount='1'>
            <test-case id='0-1088' name='TestIsExplicit' fullname='NUnitTestDemo.SimpleTests.TestIsExplicit' methodname='TestIsExplicit' classname='NUnitTestDemo.SimpleTests' runstate='Explicit' seed='450521388'>
               <properties>
                  <property name='Expect' value='Skipped' />
               </properties>
            </test-case>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        private const string ExplicitQuickTestXml =
            @"<test-run id='0' name='NUnit.VisualStudio.TestAdapter.Tests.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter\src\NUnitTestAdapterTests\bin\Debug\netcoreapp3.1\NUnit.VisualStudio.TestAdapter.Tests.dll' runstate='Runnable' testcasecount='1'>
   <test-suite type='Assembly' id='0-1358' name='NUnit.VisualStudio.TestAdapter.Tests.dll' fullname='D:/repos/NUnit/nunit3-vs-adapter/src/NUnitTestAdapterTests/bin/Debug/netcoreapp3.1/NUnit.VisualStudio.TestAdapter.Tests.dll' runstate='Runnable' testcasecount='1'>
      <test-suite type='TestSuite' id='0-1359' name='NUnit' fullname='NUnit' runstate='Runnable' testcasecount='1'>
         <test-suite type='TestSuite' id='0-1360' name='VisualStudio' fullname='NUnit.VisualStudio' runstate='Runnable' testcasecount='1'>
            <test-suite type='TestSuite' id='0-1361' name='TestAdapter' fullname='NUnit.VisualStudio.TestAdapter' runstate='Runnable' testcasecount='1'>
               <test-suite type='TestSuite' id='0-1362' name='Tests' fullname='NUnit.VisualStudio.TestAdapter.Tests' runstate='Runnable' testcasecount='1'>
                  <test-suite type='TestFixture' id='0-1363' name='IssueNo24Tests' fullname='NUnit.VisualStudio.TestAdapter.Tests.IssueNo24Tests' runstate='Explicit' testcasecount='1'>
                     <test-case id='0-1100' name='Quick' fullname='NUnit.VisualStudio.TestAdapter.Tests.IssueNo24Tests.Quick' methodname='Quick' classname='NUnit.VisualStudio.TestAdapter.Tests.IssueNo24Tests' runstate='Explicit' seed='845750879' />
                  </test-suite>
               </test-suite>
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";


        [TestCase(ExplicitXml, 3, TestName = nameof(ThatExplicitWorks) + "." + nameof(ExplicitXml))]
        public void ThatExplicitWorks(string xml, int count)
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(xml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            Assert.Multiple(() =>
            {
                var first = topLevelSuite.TestFixtures.First();
                Assert.That(first.IsExplicit, $"First {first.Id} failed");
                var second = topLevelSuite.TestFixtures.Skip(1).First();
                Assert.That(second.IsExplicit, $"Second {first.Id} failed");
                var third = topLevelSuite.TestFixtures.Skip(2).First();
                Assert.That(third.IsExplicit, $"Third {first.Id} failed");
            });
            Assert.That(topLevelSuite.IsExplicit, "TopLevelsuite failed");
            Assert.That(ndr.TestAssembly.AllTestCases.Count(), Is.EqualTo(count), "Count failed");
            Assert.Multiple(() =>
            {
                foreach (var testCase in ndr.TestAssembly.AllTestCases)
                {
                    Assert.That(testCase.IsExplicitReverse, $"Failed for {testCase.Id}");
                }
            });
        }


        [TestCase(ExplicitQuickTestXml, 1, TestName = nameof(ThatExplicitWorks2) + "." + nameof(ExplicitQuickTestXml))]
        public void ThatExplicitWorks2(string xml, int count)
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(xml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            Assert.Multiple(() =>
            {
                Assert.That(topLevelSuite.IsExplicit, "TopLevelsuite failed");
                var first = topLevelSuite.TestSuites.First();
                Assert.That(first.IsExplicit, $"First {first.Id} failed");
            });

            Assert.That(ndr.TestAssembly.AllTestCases.Count(), Is.EqualTo(count), "Count failed");
            Assert.Multiple(() =>
            {
                foreach (var testCase in ndr.TestAssembly.AllTestCases)
                {
                    Assert.That(testCase.IsExplicitReverse, $"Failed for {testCase.Id}");
                }
            });
        }





        private const string NotExplicitXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='3'>
   <test-suite type='Assembly' id='0-1160' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='3'>
      <test-suite type='TestSuite' id='0-1161' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='3'>
         <test-suite type='TestFixture' id='0-1162' name='ExplicitClass' fullname='NUnitTestDemo.ExplicitClass' runstate='Explicit' testcasecount='1'>
            <test-case id='0-1136' name='ThisIsIndirectlyExplicit' fullname='NUnitTestDemo.ExplicitClass.ThisIsIndirectlyExplicit' methodname='ThisIsIndirectlyExplicit' classname='NUnitTestDemo.ExplicitClass' runstate='Runnable' seed='289706323' />
         </test-suite>
         <test-suite type='TestFixture' id='0-1163' name='ParameterizedTests' fullname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' testcasecount='1'>
            <test-suite type='ParameterizedMethod' id='0-1164' name='TestCaseIsExplicit' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Explicit' testcasecount='1'>
               <test-case id='0-1060' name='TestCaseIsExplicit(31,11)' fullname='NUnitTestDemo.ParameterizedTests.TestCaseIsExplicit(31,11)' methodname='TestCaseIsExplicit' classname='NUnitTestDemo.ParameterizedTests' runstate='Runnable' seed='1206901902' />
            </test-suite>
         </test-suite>
         <test-suite type='TestFixture' id='0-1165' name='SimpleTests' fullname='NUnitTestDemo.SimpleTests' runstate='Runnable' testcasecount='1'>
            <test-case id='0-1088' name='TestIsExplicit' fullname='NUnitTestDemo.SimpleTests.TestIsExplicit' methodname='TestIsExplicit' classname='NUnitTestDemo.SimpleTests' runstate='Explicit' seed='450521388'>
               <properties>
                  <property name='Expect' value='Skipped' />
               </properties>
            </test-case>
            <test-case id='0-1008' name='RunnableTest' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestThrowsException' methodname='AsyncTaskTestThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1342062567'>
               <properties>
                  <property name='Expect' value='Error' />
               </properties>
            </test-case>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";


        [Test]
        public void ThatExplicitWorksWhenOneTestIsNotExplicit()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(NotExplicitXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            Assert.That(topLevelSuite.IsExplicit, Is.False);
        }

        private const string AsyncTestsXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='7'>
   <test-suite type='Assembly' id='0-1160' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='7'>
      <test-suite type='TestSuite' id='0-1161' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='7'>
         <test-suite type='TestFixture' id='0-1162' name='AsyncTests' fullname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='7'>
            <test-case id='0-1007' name='AsyncTaskTestFails' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestFails' methodname='AsyncTaskTestFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='564983252'>
               <properties>
                  <property name='Expect' value='Failure' />
               </properties>
            </test-case>
            <test-case id='0-1006' name='AsyncTaskTestSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestSucceeds' methodname='AsyncTaskTestSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1418808408'>
               <properties>
                  <property name='Expect' value='Pass' />
               </properties>
            </test-case>
            <test-case id='0-1008' name='AsyncTaskTestThrowsException' fullname='NUnitTestDemo.AsyncTests.AsyncTaskTestThrowsException' methodname='AsyncTaskTestThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1342062567'>
               <properties>
                  <property name='Expect' value='Error' />
               </properties>
            </test-case>
            <test-suite type='ParameterizedMethod' id='0-1163' name='AsyncTaskWithResultFails' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1011' name='AsyncTaskWithResultFails()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultFails()' methodname='AsyncTaskWithResultFails' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='1018572466' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1164' name='AsyncTaskWithResultSucceeds' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1009' name='AsyncTaskWithResultSucceeds()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultSucceeds()' methodname='AsyncTaskWithResultSucceeds' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='823587191' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1165' name='AsyncTaskWithResultThrowsException' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1013' name='AsyncTaskWithResultThrowsException()' fullname='NUnitTestDemo.AsyncTests.AsyncTaskWithResultThrowsException()' methodname='AsyncTaskWithResultThrowsException' classname='NUnitTestDemo.AsyncTests' runstate='Runnable' seed='922873877' />
            </test-suite>
            <test-case id='0-1005' name='AsyncVoidTestIsInvalid' fullname='NUnitTestDemo.AsyncTests.AsyncVoidTestIsInvalid' methodname='AsyncVoidTestIsInvalid' classname='NUnitTestDemo.AsyncTests' runstate='NotRunnable' seed='691553472'>
               <properties>
                  <property name='_SKIPREASON' value='Async test method must have non-void return type' />
                  <property name='Expect' value='Error' />
               </properties>
            </test-case>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";



        [Test]
        public void ThatAsyncTestsHasSevenTests()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(AsyncTestsXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            Assert.That(topLevelSuite.NoOfActualTestCases, Is.EqualTo(7));
        }

        private const string ParameterizedTestFixtureXml =
            @"<test-run id='2' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' testcasecount='6'>
   <test-suite type='Assembly' id='0-1160' name='CSharpTestDemo.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter-demo\solutions\vs2017\CSharpTestDemo\bin\Debug\CSharpTestDemo.dll' runstate='Runnable' testcasecount='6'>
      <test-suite type='TestSuite' id='0-1161' name='NUnitTestDemo' fullname='NUnitTestDemo' runstate='Runnable' testcasecount='6'>
         <test-suite type='ParameterizedFixture' id='0-1162' name='ParameterizedTestFixture' fullname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' testcasecount='6'>
            <test-suite type='TestFixture' id='0-1163' name='ParameterizedTestFixture(""hello"",""hello"",""goodbye"")' fullname='NUnitTestDemo.ParameterizedTestFixture(""hello"",""hello"",""goodbye"")' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1143' name='TestEquality' fullname='NUnitTestDemo.ParameterizedTestFixture(""hello"",""hello"",""goodbye"").TestEquality' methodname='TestEquality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='1153785709' />
               <test-case id='0-1144' name='TestInequality' fullname='NUnitTestDemo.ParameterizedTestFixture(""hello"",""hello"",""goodbye"").TestInequality' methodname='TestInequality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='336594823' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1164' name='ParameterizedTestFixture(""zip"",""zip"")' fullname='NUnitTestDemo.ParameterizedTestFixture(""zip"",""zip"")' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1146' name='TestEquality' fullname='NUnitTestDemo.ParameterizedTestFixture(""zip"",""zip"").TestEquality' methodname='TestEquality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='176412041' />
               <test-case id='0-1147' name='TestInequality' fullname='NUnitTestDemo.ParameterizedTestFixture(""zip"",""zip"").TestInequality' methodname='TestInequality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='872346411' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1165' name='ParameterizedTestFixture(42,42,99)' fullname='NUnitTestDemo.ParameterizedTestFixture(42,42,99)' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1149' name='TestEquality' fullname='NUnitTestDemo.ParameterizedTestFixture(42,42,99).TestEquality' methodname='TestEquality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='1898578770' />
               <test-case id='0-1150' name='TestInequality' fullname='NUnitTestDemo.ParameterizedTestFixture(42,42,99).TestInequality' methodname='TestInequality' classname='NUnitTestDemo.ParameterizedTestFixture' runstate='Runnable' seed='590170168' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";


        [Test]
        public void ThatParameterizedTestFixtureHasSixTests()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(ParameterizedTestFixtureXml)));
            var topLevelSuite = ndr.TestAssembly.TestSuites.Single();
            Assert.That(topLevelSuite.NoOfActualTestCases, Is.EqualTo(6));
        }


        private const string DotnetXml =
            @"<test-run id='2' name='Filtering.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter.issues\Issue497\bin\Debug\net461\Filtering.dll' testcasecount='20000'>
   <test-suite type='Assembly' id='0-21501' name='Filtering.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter.issues\Issue497\bin\Debug\net461\Filtering.dll' runstate='Runnable' testcasecount='20000'>
            <properties>
               <property name='Something' value='Foo' />
            </properties>
        <test-suite type='TestSuite' id='0-21506' name='Filtering' fullname='Filtering' runstate='Runnable' testcasecount='2'>
         <test-suite type='TestFixture' id='0-21507' name='ANonGeneratedExplicitTest' fullname='Filtering.ANonGeneratedExplicitTest' runstate='Runnable' testcasecount='2'>
            <test-case id='0-21511' name='TestExplicitTest' fullname='Filtering.ANonGeneratedExplicitTest.TestExplicitTest' methodname='TestExplicitTest' classname='Filtering.ANonGeneratedExplicitTest' runstate='Explicit' seed='154804577' />
            <test-case id='0-21512' name='TestNotExplicitTest' fullname='Filtering.ANonGeneratedExplicitTest.TestNotExplicitTest' methodname='TestNotExplicitTest' classname='Filtering.ANonGeneratedExplicitTest' runstate='Runnable' seed='334435265' />
         </test-suite>
      </test-suite>
      <test-suite type='TestFixture' id='0-21502' name='GeneratedTest0' fullname='GeneratedTest0' runstate='Runnable' testcasecount='40'>
        <properties>
               <property name='SomethingElse' value='FooToo' />
          </properties>
         <test-case id='0-1001' name='Test1' fullname='GeneratedTest0.Test1' methodname='Test1' classname='GeneratedTest0' runstate='Runnable' seed='1044071786'>
            <properties>
               <property name='Category' value='Foo' />
            </properties>
         </test-case>
         <test-case id='0-1010' name='Test10' fullname='GeneratedTest0.Test10' methodname='Test10' classname='GeneratedTest0' runstate='Runnable' seed='117332475' />
         <test-case id='0-1011' name='Test11' fullname='GeneratedTest0.Test11' methodname='Test11' classname='GeneratedTest0' runstate='Runnable' seed='12294536' />
         
      </test-suite>
      <test-suite type='TestFixture' id='0-21503' name='GeneratedTest1' fullname='GeneratedTest1' runstate='Runnable' testcasecount='40'>
         <test-case id='0-1042' name='Test1' fullname='GeneratedTest1.Test1' methodname='Test1' classname='GeneratedTest1' runstate='Runnable' seed='907302604' />
         <test-case id='0-1051' name='Test10' fullname='GeneratedTest1.Test10' methodname='Test10' classname='GeneratedTest1' runstate='Runnable' seed='542403258' />
         <test-case id='0-1052' name='Test11' fullname='GeneratedTest1.Test11' methodname='Test11' classname='GeneratedTest1' runstate='Runnable' seed='2036961476' />
         
      </test-suite>
        <test-suite type='TestFixture' id='0-21504' name='GeneratedTest10' fullname='GeneratedTest10' runstate='Runnable' testcasecount='40'>
         <test-case id='0-1083' name='Test1' fullname='GeneratedTest10.Test1' methodname='Test1' classname='GeneratedTest10' runstate='Runnable' seed='857897643' />
         <test-case id='0-1092' name='Test10' fullname='GeneratedTest10.Test10' methodname='Test10' classname='GeneratedTest10' runstate='Runnable' seed='162525546' />
         <test-case id='0-1093' name='Test11' fullname='GeneratedTest10.Test11' methodname='Test11' classname='GeneratedTest10' runstate='Runnable' seed='48042500' />
        </test-suite>
    </test-suite>
</test-run>";

        /// <summary>
        /// The dotnetxml has no top level suite, but fixtures directly under assembly.
        /// </summary>
        [Test]
        public void ThatDotNetTestWorks()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(DotnetXml)));
            var fixtures = ndr.TestAssembly.TestFixtures;
            Assert.That(fixtures.Count(), Is.EqualTo(3), "Didnt find all fixtures");
            foreach (var fixture in fixtures)
            {
                Assert.That(fixture.TestCases.Count, Is.EqualTo(3),
                    "Didnt find all testcases for fixture");
            }

            Assert.That(ndr.TestAssembly.TestSuites.Count, Is.EqualTo(1));
            var suite = ndr.TestAssembly.TestSuites.Single();
            Assert.That(suite.TestFixtures.Count, Is.EqualTo(1));
            Assert.That(suite.TestCaseCount, Is.EqualTo(2));
        }

        private const string MixedExplicitTestSourceXmlForNUnit312 =
            @"<test-run id='0' name='Issue545.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter.issues\Issue545\Issue545\bin\Debug\Issue545.dll' runstate='Runnable' testcasecount='3'>
   <test-suite type='Assembly' id='0-1007' name='Issue545.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter.issues\Issue545\Issue545\bin\Debug\Issue545.dll' runstate='Runnable' testcasecount='3'>
      <test-suite type='TestSuite' id='0-1008' name='Issue545' fullname='Issue545' runstate='Runnable' testcasecount='3'>
         <test-suite type='TestFixture' id='0-1009' name='FooTests' fullname='Issue545.FooTests' runstate='Runnable' testcasecount='3'>
            <test-suite type='ParameterizedMethod' id='0-1010' name='Test1' fullname='Issue545.FooTests.Test1' classname='Issue545.FooTests' runstate='Explicit' testcasecount='2'>
               <test-case id='0-1001' name='Test1(1)' fullname='Issue545.FooTests.Test1(1)' methodname='Test1' classname='Issue545.FooTests' runstate='Runnable' seed='727400600' />
               <test-case id='0-1002' name='Test1(2)' fullname='Issue545.FooTests.Test1(2)' methodname='Test1' classname='Issue545.FooTests' runstate='Runnable' seed='6586680' />
            </test-suite>
            <test-case id='0-1004' name='Test2' fullname='Issue545.FooTests.Test2' methodname='Test2' classname='Issue545.FooTests' runstate='Runnable' seed='756826920' />
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        /// <summary>
        /// See issue 1041 at https://github.com/nunit/nunit3-vs-adapter/issues/1044
        /// </summary>
        private const string MixedExplicitTestSourceXmlForIssue1041 =
            @"<test-run id='0' name='Issue1044.dll' fullname='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue1044\bin\Debug\net7.0\Issue1044.dll' runstate='Runnable' testcasecount='4'>
   <test-suite type='Assembly' id='0-1009' name='Issue1044.dll' fullname='d:/repos/NUnit/nunit3-vs-adapter.issues/Issue1044/bin/Debug/net7.0/Issue1044.dll' runstate='Runnable' testcasecount='4'>
      <environment framework-version='3.13.3.0' clr-version='7.0.0' os-version='Microsoft Windows 10.0.19044' platform='Win32NT' cwd='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue1044\bin\Debug\net7.0' machine-name='DESKTOP-SIATMVB' user='TerjeSandstrom' user-domain='AzureAD' culture='en-US' uiculture='en-US' os-architecture='x64' />
      <settings>
         <setting name='NumberOfTestWorkers' value='0' />
         <setting name='SynchronousEvents' value='False' />
         <setting name='InternalTraceLevel' value='Off' />
         <setting name='RandomSeed' value='1197244076' />
         <setting name='ProcessModel' value='InProcess' />
         <setting name='DomainUsage' value='Single' />
         <setting name='DefaultTestNamePattern' value='{m}{a}' />
         <setting name='WorkDirectory' value='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue1044\bin\Debug\net7.0' />
      </settings>
      <properties>
         <property name='_PID' value='83292' />
         <property name='_APPDOMAIN' value='testhost' />
      </properties>
      <test-suite type='TestSuite' id='0-1010' name='Issue1041' fullname='Issue1041' runstate='Runnable' testcasecount='4'>
         <test-suite type='TestFixture' id='0-1011' name='Tests' fullname='Issue1041.Tests' runstate='Runnable' testcasecount='4'>
            <test-suite type='ParameterizedMethod' id='0-1012' name='ExplicitTest' fullname='Issue1041.Tests.ExplicitTest' classname='Issue1041.Tests' runstate='Explicit' testcasecount='2'>
               <test-case id='0-1004' name='The test number 1.' fullname='Issue1041.Tests.The test number 1.' methodname='ExplicitTest' classname='Issue1041.Tests' runstate='Explicit' seed='1882810222' />
               <test-case id='0-1005' name='The test number 2.' fullname='Issue1041.Tests.The test number 2.' methodname='ExplicitTest' classname='Issue1041.Tests' runstate='Explicit' seed='531831831' />
            </test-suite>
            <test-suite type='ParameterizedMethod' id='0-1013' name='RegularTest' fullname='Issue1041.Tests.RegularTest' classname='Issue1041.Tests' runstate='Runnable' testcasecount='2'>
               <test-case id='0-1001' name='The test number 1.' fullname='Issue1041.Tests.The test number 1.' methodname='RegularTest' classname='Issue1041.Tests' runstate='Runnable' seed='501371230' />
               <test-case id='0-1002' name='The test number 2.' fullname='Issue1041.Tests.The test number 2.' methodname='RegularTest' classname='Issue1041.Tests' runstate='Runnable' seed='1044448715' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        [TestCase(MixedExplicitTestSourceXmlForIssue1041, 2, 4)]
        [TestCase(MixedExplicitTestSourceXmlForNUnit312, 1, 3)]
        public void ThatMixedExplicitTestSourceWorks(string xml, int expectedRunnable, int expectedAll)
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(xml)));
            Assert.Multiple(() =>
            {
                Assert.That(ndr.IsExplicit, Is.False, "Explicit check fails");
                Assert.That(ndr.TestAssembly.RunnableTestCases.Count, Is.EqualTo(expectedRunnable), "Runnable number fails");
                Assert.That(ndr.TestAssembly.AllTestCases.Count, Is.EqualTo(expectedAll), "Can't find all testcases");
            });
        }

        private const string ExplicitRun =
            @"<test-run id='0' name='Issue545.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter.issues\Issue545\Issue545\bin\Debug\Issue545.dll' runstate='Runnable' testcasecount='2'>
   <test-suite type='Assembly' id='0-1007' name='Issue545.dll' fullname='D:\repos\NUnit\nunit3-vs-adapter.issues\Issue545\Issue545\bin\Debug\Issue545.dll' runstate='Runnable' testcasecount='2'>
      <test-suite type='TestSuite' id='0-1008' name='Issue545' fullname='Issue545' runstate='Runnable' testcasecount='2'>
         <test-suite type='TestFixture' id='0-1009' name='FooTests' fullname='Issue545.FooTests' runstate='Runnable' testcasecount='2'>
            <test-suite type='ParameterizedMethod' id='0-1010' name='Test1' fullname='Issue545.FooTests.Test1' classname='Issue545.FooTests' runstate='Explicit' testcasecount='2'>
               <test-case id='0-1001' name='Test1(1)' fullname='Issue545.FooTests.Test1(1)' methodname='Test1' classname='Issue545.FooTests' runstate='Runnable' seed='581862553' />
               <test-case id='0-1002' name='Test1(2)' fullname='Issue545.FooTests.Test1(2)' methodname='Test1' classname='Issue545.FooTests' runstate='Runnable' seed='243299682' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        [Test]
        public void ThatExplicitRunWorks()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(ExplicitRun)));
            Assert.Multiple(() =>
            {
                Assert.That(ndr.IsExplicit, "Explicit check fails");
                Assert.That(ndr.TestAssembly.AllTestCases.Count, Is.EqualTo(2), "All testcases number fails");
                Assert.That(ndr.TestAssembly.AllTestCases.Count, Is.EqualTo(2), "Can't find all testcases");
                Assert.That(ndr.TestAssembly.TestSuites.First().IsExplicit, "Test suite don't match explicit");
                Assert.That(
                    ndr.TestAssembly.TestSuites.First().TestFixtures.First().IsExplicit,
                    "Test fixture don't match explicit");
                Assert.That(
                    ndr.TestAssembly.TestSuites.First().TestFixtures.First().ParameterizedMethods.First().IsExplicit,
                    "Parameterized method don't match explicit");
                Assert.That(
                    ndr.TestAssembly.TestSuites.First().TestFixtures.First().ParameterizedMethods.First().RunState,
                    Is.EqualTo(RunStateEnum.Explicit), "Runstate fails for parameterizedfixture");
            });
        }


        private const string SetupFixtureIssue770 =
            @"<test-run id='0' name='TestLib.dll' fullname='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue770\TestLib\bin\Debug\netcoreapp3.1\TestLib.dll' runstate='Runnable' testcasecount='1'>
   <test-suite type='Assembly' id='0-1005' name='TestLib.dll' fullname='d:/repos/NUnit/nunit3-vs-adapter.issues/Issue770/TestLib/bin/Debug/netcoreapp3.1/TestLib.dll' runstate='Runnable' testcasecount='1'>
      <test-suite type='SetUpFixture' id='0-1006' name='[default namespace]' fullname='SetupF' runstate='Runnable' testcasecount='1'>
         <test-suite type='TestSuite' id='0-1007' name='L' fullname='L' runstate='Runnable' testcasecount='1'>
            <test-suite type='TestFixture' id='0-1008' name='Class1' fullname='L.Class1' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1002' name='Test' fullname='L.Class1.Test' methodname='Test' classname='L.Class1' runstate='Runnable' seed='1622556793' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";


        [Test]
        public void ThatSetUpFixtureWorksIssue770()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(SetupFixtureIssue770)));
            Assert.That(ndr, Is.Not.Null);
        }

        private const string SetupFixtureIssue824 =
            @"<test-run id='0' name='Issue824.dll' fullname='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue824\bin\Debug\net5.0\Issue824.dll' runstate='Runnable' testcasecount='2'>
   <test-suite type='Assembly' id='0-1012' name='Issue824.dll' fullname='d:/repos/NUnit/nunit3-vs-adapter.issues/Issue824/bin/Debug/net5.0/Issue824.dll' runstate='Runnable' testcasecount='2'>
      <environment framework-version='3.13.1.0' clr-version='5.0.4' os-version='Microsoft Windows 10.0.18363' platform='Win32NT' cwd='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue824\bin\Debug\net5.0' machine-name='DESKTOP-SIATMVB' user='TerjeSandstrom' user-domain='AzureAD' culture='en-US' uiculture='en-US' os-architecture='x64' />
            <test-suite type='SetUpFixture' id='0-1013' name='[default namespace]' fullname='GlobalSetUpFixture' runstate='Runnable' testcasecount='2'>
         <test-suite type='SetUpFixture' id='0-1014' name='Issue824' fullname='Issue824.NUnitTestsDemoSetUpFixture1' runstate='Runnable' testcasecount='2'>
            <test-suite type='SetUpFixture' id='0-1015' name='Issue824' fullname='Issue824.NUnitTestsDemoSetUpFixture2' runstate='Runnable' testcasecount='2'>
               <test-suite type='SetUpFixture' id='0-1016' name='Inner' fullname='Issue824.Inner.NUnitTestsDemoInnerSetUpFixture1' runstate='Runnable' testcasecount='1'>
                  <test-suite type='SetUpFixture' id='0-1017' name='Inner' fullname='Issue824.Inner.NUnitTestsDemoInnerSetUpFixture2' runstate='Runnable' testcasecount='1'>
                     <test-suite type='TestFixture' id='0-1018' name='InnerTests' fullname='Issue824.Inner.InnerTests' runstate='Runnable' testcasecount='1'>
                        <test-case id='0-1008' name='Test' fullname='Issue824.Inner.InnerTests.Test' methodname='Test' classname='Issue824.Inner.InnerTests' runstate='Runnable' seed='1110169426' />
                     </test-suite>
                  </test-suite>
               </test-suite>
               <test-suite type='TestFixture' id='0-1019' name='Tests' fullname='Issue824.Tests' runstate='Runnable' testcasecount='1'>
                  <test-case id='0-1004' name='Test' fullname='Issue824.Tests.Test' methodname='Test' classname='Issue824.Tests' runstate='Runnable' seed='558639032' />
               </test-suite>
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";


        [Test]
        public void ThatSetUpFixtureWorksIssue824()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(SetupFixtureIssue824)));
            Assert.That(ndr, Is.Not.Null);
        }

        private const string SetupFixtureIssue884 =
            @"<test-run id='0' name='NUnitTestAdapterIssueRepro.dll' fullname='d:\repos\NUnit\nunitissues_otherrepos\Issue884\NUnitTestAdapterIssueRepro\bin\Debug\netcoreapp3.1\NUnitTestAdapterIssueRepro.dll' runstate='Runnable' testcasecount='3'>
   <test-suite type='Assembly' id='0-1010' name='NUnitTestAdapterIssueRepro.dll' fullname='d:/repos/NUnit/nunitissues_otherrepos/Issue884/NUnitTestAdapterIssueRepro/bin/Debug/netcoreapp3.1/NUnitTestAdapterIssueRepro.dll' runstate='Runnable' testcasecount='3'>
      <environment framework-version='3.13.2.0' clr-version='3.1.19' os-version='Microsoft Windows 10.0.19042' platform='Win32NT' cwd='d:\repos\NUnit\nunitissues_otherrepos\Issue884\NUnitTestAdapterIssueRepro\bin\Debug\netcoreapp3.1' machine-name='DESKTOP-SIATMVB' user='TerjeSandstrom' user-domain='AzureAD' culture='en-US' uiculture='en-US' os-architecture='x64' />
      <properties>
         <property name='_PID' value='36768' />
         <property name='_APPDOMAIN' value='testhost' />
      </properties>
      <test-suite type='SetUpFixture' id='0-1011' name='NUnitTestAdapterIssueRepro' fullname='NUnitTestAdapterIssueRepro.SetupFixture' runstate='Runnable' testcasecount='3'>
         <test-suite type='ParameterizedFixture' id='0-1012' name='Tests' fullname='NUnitTestAdapterIssueRepro.Tests' runstate='Runnable' testcasecount='3'>
            <test-suite type='TestFixture' id='0-1013' name='Tests(1)' fullname='NUnitTestAdapterIssueRepro.Tests(1)' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1002' name='Test1' fullname='NUnitTestAdapterIssueRepro.Tests(1).Test1' methodname='Test1' classname='NUnitTestAdapterIssueRepro.Tests' runstate='Runnable' seed='1282313027' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1014' name='Tests(2)' fullname='NUnitTestAdapterIssueRepro.Tests(2)' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1004' name='Test1' fullname='NUnitTestAdapterIssueRepro.Tests(2).Test1' methodname='Test1' classname='NUnitTestAdapterIssueRepro.Tests' runstate='Runnable' seed='1183073751' />
            </test-suite>
            <test-suite type='TestFixture' id='0-1015' name='Tests(3)' fullname='NUnitTestAdapterIssueRepro.Tests(3)' runstate='Runnable' testcasecount='1'>
               <test-case id='0-1006' name='Test1' fullname='NUnitTestAdapterIssueRepro.Tests(3).Test1' methodname='Test1' classname='NUnitTestAdapterIssueRepro.Tests' runstate='Runnable' seed='1826793735' />
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        [Test]
        public void ThatSetUpFixtureWorksIssue884()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(SetupFixtureIssue884)));
            Assert.That(ndr, Is.Not.Null);
        }

        private const string GenericIssue918 = @"<test-run id='0' name='Issue918.dll' fullname='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue918\bin\Debug\net6.0\Issue918.dll' runstate='Runnable' testcasecount='1'>
   <test-suite type='Assembly' id='0-1004' name='Issue918.dll' fullname='d:/repos/NUnit/nunit3-vs-adapter.issues/Issue918/bin/Debug/net6.0/Issue918.dll' runstate='Runnable' testcasecount='1'>
      <environment framework-version='3.13.2.0' clr-version='6.0.0' os-version='Microsoft Windows 10.0.19042' platform='Win32NT' cwd='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue918\bin\Debug\net6.0' machine-name='DESKTOP-SIATMVB' user='TerjeSandstrom' user-domain='AzureAD' culture='en-US' uiculture='en-US' os-architecture='x64' />
      <properties>
         <property name='_PID' value='90212' />
         <property name='_APPDOMAIN' value='testhost' />
      </properties>
      <test-suite type='TestSuite' id='0-1005' name='Issue918' fullname='Issue918' runstate='Runnable' testcasecount='1'>
         <test-suite type='GenericFixture' id='0-1003' name='Tests+SomeTest&lt;T&gt;' fullname='Issue918.Tests+SomeTest&lt;T&gt;' runstate='Runnable' testcasecount='1'>
            <test-suite type='GenericFixture' id='0-1000' name='Tests+SomeTest&lt;T&gt;' fullname='Issue918.Tests+SomeTest&lt;T&gt;' runstate='Runnable' testcasecount='1'>
               <test-suite type='TestFixture' id='0-1001' name='Tests+SomeTest&lt;Object&gt;' fullname='Issue918.Tests+SomeTest&lt;Object&gt;' classname='Issue918.Tests+SomeTest`1' runstate='Runnable' testcasecount='1'>
                  <test-case id='0-1002' name='Foo' fullname='Issue918.Tests+SomeTest&lt;Object&gt;.Foo' methodname='Foo' classname='Issue918.Tests+SomeTest`1' runstate='Runnable' seed='506899496' />
               </test-suite>
            </test-suite>
         </test-suite>
      </test-suite>
   </test-suite>
</test-run>";

        [Test]
        public void ThatGenericFixturesWorksIssue918()
        {
            var sut = new DiscoveryConverter(logger, settings);
            var ndr = sut.ConvertXml(
                new NUnitResults(XmlHelper.CreateXmlNode(GenericIssue918)));
            Assert.That(ndr, Is.Not.Null);
        }


        private const string ExtractFixturesHandlesProperties =
            @"<test-run id='0' name='Issue824.dll' fullname='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue824\bin\Debug\net5.0\Issue824.dll' runstate='Runnable' testcasecount='2'>
   <test-suite type='Assembly' id='0-1012' name='Issue824.dll' fullname='d:/repos/NUnit/nunit3-vs-adapter.issues/Issue824/bin/Debug/net5.0/Issue824.dll' runstate='Runnable' testcasecount='2'>
      <environment framework-version='3.13.1.0' clr-version='5.0.4' os-version='Microsoft Windows 10.0.18363' platform='Win32NT' cwd='d:\repos\NUnit\nunit3-vs-adapter.issues\Issue824\bin\Debug\net5.0' machine-name='DESKTOP-SIATMVB' user='TerjeSandstrom' user-domain='AzureAD' culture='en-US' uiculture='en-US' os-architecture='x64' />
            <test-suite type='ParameterizedFixture' id='0-1253' name='Issue3848' fullname='nunit.v3.Issue3848' runstate='Runnable' testcasecount='4'>
  <properties>
    <property name='ParallelScope' value='All' />
  </properties>
  <test-suite type='TestFixture' id='0-1254' name='Issue3848(&quot;Chrome&quot;)' fullname='nunit.v3.Issue3848(&quot;Chrome&quot;)' runstate='Runnable' testcasecount='2'>
    <test-case id='0-1106' name='Test1' fullname='nunit.v3.Issue3848(&quot;Chrome&quot;).Test1' methodname='Test1' classname='nunit.v3.Issue3848' runstate='Runnable' seed='1759656977' />
    <test-case id='0-1107' name='Test2' fullname='nunit.v3.Issue3848(&quot;Chrome&quot;).Test2' methodname='Test2' classname='nunit.v3.Issue3848' runstate='Runnable' seed='637248127' />
  </test-suite>
  <test-suite type='TestFixture' id='0-1255' name='Issue3848(&quot;Edge&quot;)' fullname='nunit.v3.Issue3848(&quot;Edge&quot;)' runstate='Runnable' testcasecount='2'>
    <test-case id='0-1109' name='Test1' fullname='nunit.v3.Issue3848(&quot;Edge&quot;).Test1' methodname='Test1' classname='nunit.v3.Issue3848' runstate='Runnable' seed='273999456' />
    <test-case id='0-1110' name='Test2' fullname='nunit.v3.Issue3848(&quot;Edge&quot;).Test2' methodname='Test2' classname='nunit.v3.Issue3848' runstate='Runnable' seed='777271813' />
  </test-suite>
</test-suite> 
</test-suite>
</test-run>";

        [Test]
        public void ThatExtractFixturesHandlesProperties()
        {
            var sut = new DiscoveryConverter(logger, settings);
            XmlNode node = null;
            Assert.DoesNotThrow(() => node = XmlHelper.CreateXmlNode(ExtractFixturesHandlesProperties));
            var ndr = sut.ConvertXml(
                new NUnitResults(node));
            Assert.That(ndr, Is.Not.Null);
        }
    }
}
