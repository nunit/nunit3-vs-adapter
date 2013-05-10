// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;
using TestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// NUnitEventListener implements the EventListener interface and
    /// translates each event into a message for the VS test platform.
    /// </summary>
    public class NUnitEventListener : MarshalByRefObject, EventListener, IDisposable // Public for testing
    {
        private ITestExecutionRecorder testLog;
        private string assemblyName;
        private Dictionary<string, NUnit.Core.TestNode> nunitTestCases;
        private TestConverter testConverter;

        public NUnitEventListener(ITestExecutionRecorder testLog, Dictionary<string, NUnit.Core.TestNode> nunitTestCases, string assemblyName, bool isBuildFromTfs)
        {
            this.testLog = testLog;
            this.assemblyName = assemblyName;
            this.nunitTestCases = nunitTestCases;
            this.testConverter = new TestConverter(assemblyName, nunitTestCases,isBuildFromTfs);
        }

        public void RunStarted(string name, int testCount)
        {
            testLog.SendMessage(TestMessageLevel.Informational, "Run started: "+name);
        }

        public void RunFinished(Exception exception)
        {
        }

        public void RunFinished(NUnit.Core.TestResult result)
        {
        }

        public void SuiteStarted(TestName testName)
        {
        }

        public void SuiteFinished(NUnit.Core.TestResult result)
        {
            if ((result.IsError || result.IsFailure) &&
                (result.FailureSite == FailureSite.SetUp || result.FailureSite == FailureSite.TearDown))
            {
                testLog.SendMessage(
                    TestMessageLevel.Error, 
                    string.Format("{0} failed for test fixture {1}", result.FailureSite, result.FullName));
                if (result.Message != null)
                    testLog.SendMessage(TestMessageLevel.Error, result.Message);
                if (result.StackTrace != null)
                    testLog.SendMessage(TestMessageLevel.Error, result.StackTrace);
            }
        }

        public void TestStarted(TestName testName)
        {
            string key = testName.UniqueName;

            // Simply ignore any TestName not found
            if (nunitTestCases.ContainsKey(key))
            {
                var nunitTest = nunitTestCases[key];
                var ourCase = testConverter.ConvertTestCase(nunitTest);
                this.testLog.RecordStart(ourCase);
            }
        }

        public void TestFinished(NUnit.Core.TestResult result)
        {
            TestResult ourResult = testConverter.ConvertTestResult(result);
            this.testLog.RecordEnd(ourResult.TestCase, ourResult.Outcome);
            this.testLog.RecordResult(ourResult);
        }

        public void TestOutput(TestOutput testOutput)
        {
            string message = testOutput.Text;
            int length = message.Length;
            int drop = message.EndsWith(Environment.NewLine)
                ? Environment.NewLine.Length
                : message[length - 1] == '\n' || message[length - 1] == '\r'
                    ? 1
                    : 0;
            if (drop > 0)
                message = message.Substring(0, length - drop);
            this.testLog.SendMessage(TestMessageLevel.Informational, message);
        }

        public void UnhandledException(Exception exception)
        {
        }

        public void Dispose()
        {
            if (this.testConverter != null)
                this.testConverter.Dispose();
        }
    }
}
