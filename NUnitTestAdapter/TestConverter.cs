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
        private Dictionary<string, DiaSession> diaMap = new Dictionary<string, DiaSession>();
        private Dictionary<string, TestCase> testCaseMap;

        public TestConverter()
        {
        }

        public TestConverter(Dictionary<string, TestCase> testCaseMap)
        {
            this.testCaseMap = testCaseMap;
        }

        public TestCase ConvertTestCase(ITest test)
        {
            return ConvertTestCase(test, null);
        }

        public TestCase ConvertTestCase(ITest test, string source)
        {
            if (test.IsSuite)
                throw new ArgumentException("The argument must be a test case", "test");

            if (source == null)
                source = test.GetSourceAssembly();

            if (testCaseMap != null && testCaseMap.ContainsKey(test.TestName.FullName))
                return testCaseMap[test.TestName.FullName];

            return MakeTestCase(source, test.TestName);
        }

        public TestCase ConvertTestName(TestName testName, string source)
        {
            if (testCaseMap != null && testCaseMap.ContainsKey(testName.FullName))
                return testCaseMap[testName.FullName];

            return MakeTestCase(source, testName);
        }

        private TestCase MakeTestCase(string source, TestName testName)
        {
            TestCase testCase = new TestCase(testName.FullName, new Uri(NUnitTestExecutor.ExecutorUri));
            testCase.DisplayName = testName.Name;
            testCase.Source = source;
            
            string filePath = null;
            int lineNumber = 0;
            int columnNumber = 0;

            if (testCase.Source != null)
            {
                var diaSession = GetDiaSession(testCase.Source);

                if (diaSession != null)
                {
                    DiaNavigationData navigationData = diaSession.GetNavigationData(testName.GetClassName(), testName.GetMethodName());

                    if (navigationData != null)
                    {
                        filePath = navigationData.FileName;
                        lineNumber = navigationData.MinLineNumber;
                        columnNumber = navigationData.MinColumnNumber;
                    }
                }
            }

            testCase.CodeFilePath = filePath;
            testCase.LineNumber = lineNumber;
            testCase.ColumnNumber = columnNumber;

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
                var stackFrame = new Internal.Stacktrace(stackTrace).GetTopStackFrame();
                if (stackFrame != null)
                {
                    ourResult.ErrorFilePath = stackFrame.FileName;

                    ourResult.SetPropertyValue(TestResultProperties.ErrorLineNumber, stackFrame.LineNumber);
                }
            }

            return ourResult;
        }

        private DiaSession GetDiaSession(string source)
        {
            DiaSession diaSession = null;

            if (!diaMap.TryGetValue(source, out diaSession))
            {
                try
                {
                    diaSession = new DiaSession(source);
                    diaMap.Add(source, diaSession);
                }
                catch (COMException)
                {
                    diaMap.Add(source, diaSession);
                }
            }

            return diaSession;
        }

        public void Dispose()
        {
            foreach (DiaSession diaSession in diaMap.Values)
            {
                if (diaSession != null)
                    diaSession.Dispose();
            }
        }
    }
}
