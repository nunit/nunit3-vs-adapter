using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace NUnit.VisualStudio.TestAdapter
{
    public class TestConverter : IDisposable
    {
        private Dictionary<string, TestCase> vsTestCaseMap;
        private string sourceAssembly;
        private Dictionary<string, NUnit.Core.TestNode> nunitTestCases;

        #region Constructors

        public TestConverter(string sourceAssembly, Dictionary<string, NUnit.Core.TestNode> nunitTestCases)
        {
            this.sourceAssembly = sourceAssembly;
            this.vsTestCaseMap = new Dictionary<string, TestCase>();
            this.nunitTestCases = nunitTestCases;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts an NUnit test into a TestCase for Visual Studio,
        /// using the best method available according to the exact
        /// type passed and caching results for efficiency.
        /// </summary>
        public TestCase ConvertTestCase(NUnit.Core.ITest test)
        {
            if (test.IsSuite)
                throw new ArgumentException("The argument must be a test case", "test");

            // Return cached value if we have one
            if (vsTestCaseMap.ContainsKey(test.TestName.UniqueName))
                return vsTestCaseMap[test.TestName.UniqueName];

            // See if this is a TestNode - if not, try to
            // find one in our cache of NUnit TestNodes
            var testNode = test as TestNode;
            if (testNode == null && nunitTestCases.ContainsKey(test.TestName.UniqueName))
                testNode = nunitTestCases[test.TestName.UniqueName];

            // No test node: just build a TestCase without any
            // navigation data using the TestName
            if (testNode == null)
                return MakeTestCaseFromTestName(test.TestName);
            
            // Use the TestNode and cache the result
            var testCase = MakeTestCaseFromNUnitTest(testNode);
            vsTestCaseMap.Add(test.TestName.UniqueName, testCase);
            return testCase;             
        }

        /// <summary>
        /// Makes a TestCase from a TestNode, adding
        /// navigation data if it can be found.
        /// </summary>
        public TestCase MakeTestCaseFromNUnitTest(NUnit.Core.ITest nunitTest)
        {
            var testCase = MakeTestCaseFromTestName(nunitTest.TestName);

            var navigationData = GetNavigationData(nunitTest.ClassName, nunitTest.MethodName);
            if (navigationData != null)
            {
                testCase.CodeFilePath = navigationData.FileName;
                testCase.LineNumber = navigationData.MinLineNumber;
            }

            testCase.AddTraitsFromNUnitTest(nunitTest);

            return testCase;
        }

        /// <summary>
        /// Makes a TestCase without source info from TestName.
        /// </summary>
        public TestCase MakeTestCaseFromTestName(TestName testName)
        {
            TestCase testCase = new TestCase(testName.UniqueName, new Uri(NUnitTestExecutor.ExecutorUri), this.sourceAssembly);
            testCase.DisplayName = testName.Name;
            testCase.CodeFilePath = null;
            testCase.LineNumber = 0;

            return testCase;
        }

        public TestResult ConvertTestResult(NUnit.Core.TestResult result)
        {
            TestCase ourCase = ConvertTestCase(result.Test);

            TestResult ourResult = new TestResult(ourCase);
            ourResult.DisplayName = ourCase.DisplayName;
            ourResult.Outcome = ResultStateToTestOutcome(result.ResultState);
            ourResult.Duration = TimeSpan.FromSeconds(result.Time);
            // TODO: Remove this when NUnit provides a better duration
            if (ourResult.Duration == TimeSpan.Zero && (ourResult.Outcome == TestOutcome.Passed || ourResult.Outcome == TestOutcome.Failed))
                ourResult.Duration = TimeSpan.FromTicks(1);
            ourResult.ComputerName = Environment.MachineName;

            // TODO: Stuff we don't yet set
            //   StartTime   - not in NUnit result
            //   EndTime     - not in NUnit result
            //   Messages    - could we add messages other than the error message? Where would they appear?
            //   Attachments - don't exist in NUnit

            if (result.Message != null)
                ourResult.ErrorMessage = GetErrorMessage(result);

            if (!string.IsNullOrEmpty(result.StackTrace))
            {
                string stackTrace = StackTraceFilter.Filter(result.StackTrace);
                ourResult.ErrorStackTrace = stackTrace;
                //if (!string.IsNullOrEmpty(stackTrace))
                //{
                //    var stackFrame = new Internal.Stacktrace(stackTrace).GetTopStackFrame();
                //    if (stackFrame != null)
                //    {
                //       /ourResult.ErrorFilePath = stackFrame.FileName;
                //        ourResult.SetPropertyValue(TestResultProperties.ErrorLineNumber, stackFrame.LineNumber);
                //    }
                //}
            }

            return ourResult;
        }

        public void Dispose()
        {
            if (this._diaSession != null)
                this._diaSession.Dispose();
        }

        #endregion

        #region Helper Methods

        // Public for testing
        public static TestOutcome ResultStateToTestOutcome(ResultState resultState)
        {
            switch (resultState)
            {
                case ResultState.Cancelled:
                    return TestOutcome.None;
                case ResultState.Error:
                    return TestOutcome.Failed;
                case ResultState.Failure:
                    return TestOutcome.Failed;
                case ResultState.Ignored:
                    return TestOutcome.Skipped;
                case ResultState.Inconclusive:
                    return TestOutcome.None;
                case ResultState.NotRunnable:
                    return TestOutcome.Failed;
                case ResultState.Skipped:
                    return TestOutcome.Skipped;
                case ResultState.Success:
                    return TestOutcome.Passed;
            }

            return TestOutcome.None;
        }

        private string GetErrorMessage(NUnit.Core.TestResult result)
        {
            string message = result.Message;
            string NL = Environment.NewLine;

            // If we're running in the IDE, remove any caret line from the message
            // since it will be displayed using a variable font and won't make sense.
            if (message != null && RunningUnderIDE && (result.ResultState == ResultState.Failure || result.ResultState == ResultState.Inconclusive))
            {
                string pattern = NL + "  -*\\^" + NL;
                message = Regex.Replace(message, pattern, NL, RegexOptions.Multiline);
            }

            return message;
        }

        // Public for test purposes
        public DiaNavigationData GetNavigationData(string className, string methodName)
        {
            if (this.DiaSession == null) return null;

            var navData = DiaSession.GetNavigationData(className, methodName);

            if (navData != null && navData.FileName != null) return navData;

            // DiaSession returned null. The rest of this code checks to see 
            // if this test is an async method, which needs special handling.

            var definingType = Type.GetType(className + "," + Path.GetFileNameWithoutExtension(sourceAssembly));
            if (definingType == null) return null;

            var method = definingType.GetMethod(methodName);
            if (method == null) return null;
            
            var asyncAttribute = Reflect.GetAttribute(method, "System.Runtime.CompilerServices.AsyncStateMachineAttribute", false);
            if (asyncAttribute == null) return null;

            PropertyInfo stateMachineTypeProperty = asyncAttribute.GetType().GetProperty("StateMachineType");
            if (stateMachineTypeProperty == null) return null;

            Type stateMachineType = stateMachineTypeProperty.GetValue(asyncAttribute, new object[0]) as Type;
            if (stateMachineType == null) return null;

            navData = DiaSession.GetNavigationData(stateMachineType.FullName, "MoveNext");

            return navData;
        }

        #endregion

        #region Private Properties

        // NOTE: There is some sort of timing issue involved
        // in creating the DiaSession. When it is created
        // in the constructor, an exception is thrown on the
        // call to GetNavigationData. We don't understand
        // this, we're just dealing with it.
        private DiaSession _diaSession;
        private bool _tryToCreateDiaSession = true;
        private DiaSession DiaSession
        {
            get
            {
                if (_tryToCreateDiaSession)
                {
                    try
                    {
                        _diaSession = new DiaSession(sourceAssembly);
                    }
                    catch (Exception)
                    {
                        // If this isn't a project type supporting DiaSession,
                        // we just ignore the error. We won't try this again 
                        // for the project.
                    }

                    _tryToCreateDiaSession = false;
                }

                return _diaSession;
            }
        }

        private string exeName;
        private bool RunningUnderIDE
        {
            get
            {
                if (exeName == null)
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly != null)
                        exeName = Path.GetFileName(AssemblyHelper.GetAssemblyPath(entryAssembly));
                }
                
                return exeName == "vstest.executionengine.exe";
            }
        }

        #endregion
    }
}