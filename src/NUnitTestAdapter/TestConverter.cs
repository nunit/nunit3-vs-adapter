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

        public TestConverter(string sourceAssembly)
        {
            this.sourceAssembly = sourceAssembly;
        }

        public TestConverter(string sourceAssembly, Dictionary<string, TestCase> testCaseMap)
            : this(sourceAssembly)
        {
            this.testCaseMap = testCaseMap;
        }

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
            TestCase testCase = new TestCase(testName.FullName, new Uri(NUnitTestExecutor.ExecutorUri));
            testCase.DisplayName = testName.Name;
            testCase.Source = this.sourceAssembly;
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
                DiaNavigationData navigationData = diaSession.GetNavigationData(testName.GetClassName(), testName.GetMethodName());

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
            ourResult.Outcome = result.ResultState.ToTestOutcome();
            ourResult.Duration = TimeSpan.FromSeconds(result.Time);
            ourResult.ComputerName = Environment.MachineName;
            if (result.Message != null)
                ourResult.ErrorMessage = result.Message;

            if (!string.IsNullOrEmpty(result.StackTrace))
            {
                string stackTrace = StackTraceFilter.Filter(result.StackTrace);
                ourResult.ErrorStackTrace = stackTrace;
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    var stackFrame = new Internal.Stacktrace(stackTrace).GetTopStackFrame();
                    if (stackFrame != null)
                    {
                        ourResult.ErrorFilePath = stackFrame.FileName;
                        ourResult.SetPropertyValue(TestResultProperties.ErrorLineNumber, stackFrame.LineNumber);
                    }
                }
            }

            return ourResult;
        }

        public void Dispose()
        {
            if (this.diaSession != null)
                this.diaSession.Dispose();
        }
    }
}
