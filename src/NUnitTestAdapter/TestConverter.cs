using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;
using System.Runtime.InteropServices;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter
{
    public class TestConverter : IDisposable
    {
        private Dictionary<string, TestCase> testCaseMap;
        private string sourceAssembly;
        private DiaSession diaSession;
        private bool tryToCreateDiaSession = true;

        #region Constructors

        public TestConverter(string sourceAssembly)
        {
            this.sourceAssembly = sourceAssembly;
        }

        public TestConverter(string sourceAssembly, Dictionary<string, TestCase> testCaseMap)
            : this(sourceAssembly)
        {
            this.testCaseMap = testCaseMap;
        }

        #endregion

        public TestCase ConvertTestCase(ITest test)
        {
            if (test.IsSuite)
                throw new ArgumentException("The argument must be a test case", "test");

            if (testCaseMap != null && testCaseMap.ContainsKey(test.TestName.FullName))
                return testCaseMap[test.TestName.FullName];

            return MakeTestCase(test.TestName);
        }

        public TestCase ConvertTestName(TestName testName)
        {
            if (testCaseMap != null && testCaseMap.ContainsKey(testName.FullName))
                return testCaseMap[testName.FullName];

            return MakeTestCase(testName);
        }

        private TestCase MakeTestCase(TestName testName)
        {
            TestCase testCase = new TestCase(testName.FullName, new Uri(NUnitTestExecutor.ExecutorUri), this.sourceAssembly);
            testCase.DisplayName = testName.Name;
            //testCase.Source = this.sourceAssembly;
            testCase.CodeFilePath = null;
            testCase.LineNumber = 0;

            // NOTE: There is some sort of timing issue involved
            // in creating the DiaSession. When it is created
            // in the constructor, an exception is thrown on the
            // call to GetNavigationData. We don't understand
            // this, we're just dealing with it.
            if (tryToCreateDiaSession)
            {
                try
                {
                    this.diaSession = new DiaSession(sourceAssembly);
                }
                catch (Exception)
                {
                    // If this isn't a project type supporting DiaSession,
                    // we just ignore the error. We won't try this again 
                    // for the project.
                }

                tryToCreateDiaSession = false;
            }

            if (this.diaSession != null)
            {
                DiaNavigationData navigationData = diaSession.GetNavigationData(GetClassName(testName), GetMethodName(testName));

                if (navigationData != null)
                {
                    testCase.CodeFilePath = navigationData.FileName;
                    testCase.LineNumber = navigationData.MinLineNumber;
                }
            }

            return testCase;
        }

        public TestResult ConvertTestResult(NUnit.Core.TestResult result)
        {
            TestCase ourCase = ConvertTestCase(result.Test);

            TestResult ourResult = new TestResult(ourCase);
            ourResult.Outcome = ResultStateToTestOutcome(result.ResultState);
            ourResult.Duration = TimeSpan.FromSeconds(result.Time);
            ourResult.ComputerName = Environment.MachineName;
            if (result.Message != null)
                ourResult.ErrorMessage = result.Message;

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
            if (this.diaSession != null)
                this.diaSession.Dispose();
        }

        #region Static Methods

        public static string GetClassName(TestName testName)
        {
            var className = testName.FullName;
            var name = testName.Name;

            if (className.Length > name.Length + 1)
                className = className.Substring(0, className.Length - name.Length - 1);

            return className;
        }

        public static string GetMethodName(TestName testName)
        {
            var methodName = testName.Name;

            if (methodName.EndsWith(")"))
            {
                var lpar = methodName.IndexOf('(');
                if (lpar > 0)
                    methodName = methodName.Substring(0, lpar);
            }

            return methodName;
        }

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

        #endregion
    }
}
