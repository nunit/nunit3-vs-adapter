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
    public class NUnitEventListener : MarshalByRefObject, EventListener, IDisposable // Public for testing
    {
        private ITestExecutionRecorder testLog;
        private Dictionary<string, TestCase> map;
        private string assemblyName;
        private TestConverter testConverter;

        public NUnitEventListener(ITestExecutionRecorder testLog, Dictionary<string, TestCase> map, string assemblyName)
        {
            this.testLog = testLog;
            this.map = map;
            this.assemblyName = assemblyName;
            this.testConverter = new TestConverter(assemblyName, map);
        }

        public void RunStarted(string name, int testCount)
        {
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
        }

        public void TestStarted(TestName testName)
        {
            TestCase ourCase = testConverter.ConvertTestName(testName);
            this.testLog.RecordStart(ourCase);
        }

        public void TestFinished(NUnit.Core.TestResult result)
        {
            TestResult ourResult = testConverter.ConvertTestResult(result);
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
