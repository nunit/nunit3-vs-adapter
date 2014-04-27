// ****************************************************************
// Copyright (c) 2011-2013 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;
using NUnit.VisualStudio.TestAdapter.Internal;
using NUnitTestResult = NUnit.Core.TestResult;
using VSTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter
{
    public class TestConverter : IDisposable
    {
        private readonly TestLogger logger;
        private readonly Dictionary<string, TestCase> vsTestCaseMap;
        private readonly string sourceAssembly;
        private AppDomain asyncMethodHelperDomain;

        #region Constructors

        public TestConverter(TestLogger logger, string sourceAssembly)
        {
            this.logger = logger;
            this.sourceAssembly = sourceAssembly;
            this.vsTestCaseMap = new Dictionary<string, TestCase>();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts an NUnit test into a TestCase for Visual Studio,
        /// using the best method available according to the exact
        /// type passed and caching results for efficiency.
        /// </summary>
        public TestCase ConvertTestCase(ITest test)
        {
            if (test.IsSuite)
                throw new ArgumentException("The argument must be a test case", "test");

            // Return cached value if we have one
            if (vsTestCaseMap.ContainsKey(test.TestName.UniqueName))
                return vsTestCaseMap[test.TestName.UniqueName];
           
            // Convert to VS TestCase and cache the result
            var testCase = MakeTestCaseFromNUnitTest(test);
            vsTestCaseMap.Add(test.TestName.UniqueName, testCase);
            return testCase;             
        }

        public TestCase GetCachedTestCase(string key)
        {
            if (vsTestCaseMap.ContainsKey(key))
                return vsTestCaseMap[key];

            logger.SendErrorMessage("Test " + key + " not found in cache");
            return null;
        }

        public VSTestResult ConvertTestResult(NUnitTestResult result)
        {
            TestCase ourCase = GetCachedTestCase(result.Test.TestName.UniqueName);
            if (ourCase == null) return null;

            VSTestResult ourResult = new VSTestResult(ourCase)
                {
                    DisplayName = ourCase.DisplayName,
                    Outcome = ResultStateToTestOutcome(result.ResultState),
                    Duration = TimeSpan.FromSeconds(result.Time)
                };

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
            }

            return ourResult;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.diaSession != null) this.diaSession.Dispose();
                if (this.asyncMethodHelperDomain != null) AppDomain.Unload(this.asyncMethodHelperDomain);
            }
            diaSession = null;
            asyncMethodHelperDomain = null;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Makes a TestCase from an NUnit test, adding
        /// navigation data if it can be found.
        /// </summary>
        private TestCase MakeTestCaseFromNUnitTest(ITest nunitTest)
        {
            //var testCase = MakeTestCaseFromTestName(nunitTest.TestName);
            var testCase = new TestCase(
                                     nunitTest.TestName.FullName,
                                     new Uri(NUnitTestExecutor.ExecutorUri),
                                     this.sourceAssembly)
            {
                DisplayName = nunitTest.TestName.Name,
                CodeFilePath = null,
                LineNumber = 0
            };

            var navData = GetNavigationData(nunitTest.ClassName, nunitTest.MethodName);
            if (navData != null)
            {
                testCase.CodeFilePath = navData.FileName;
                testCase.LineNumber = navData.MinLineNumber;
            }

            testCase.AddTraitsFromNUnitTest(nunitTest);

            return testCase;
        }

        // public for testing
        public DiaNavigationData GetNavigationData(string className, string methodName)
        {
            if (this.DiaSession == null) return null;

            var navData = DiaSession.GetNavigationData(className, methodName);

            if (navData != null && navData.FileName != null) return navData;

            // DiaSession returned null, see if it's an async method. 
            if (AsyncMethodHelper != null)
            {
                string stateMachineClassName = AsyncMethodHelper.GetClassNameForAsyncMethod(className, methodName);
                if (stateMachineClassName != null)
                    navData = diaSession.GetNavigationData(stateMachineClassName, "MoveNext");
            }

            if (navData == null || navData.FileName == null)
                logger.SendWarningMessage(string.Format("No source data found for {0}.{1}", className, methodName));

            return navData;
        }

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

        private string GetErrorMessage(NUnitTestResult result)
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

        private AsyncMethodHelper TryCreateHelper(string sourceAssembly)
        {
            var setup = new AppDomainSetup();
            
            var thisAssembly = Assembly.GetExecutingAssembly();
            setup.ApplicationBase = AssemblyHelper.GetDirectoryName(thisAssembly);

            var evidence = AppDomain.CurrentDomain.Evidence;
            this.asyncMethodHelperDomain = AppDomain.CreateDomain("AsyncMethodHelper", evidence, setup);

            try
            {
                var helper = this.asyncMethodHelperDomain.CreateInstanceAndUnwrap(
                    thisAssembly.FullName,
                    "NUnit.VisualStudio.TestAdapter.Internal.AsyncMethodHelper") as AsyncMethodHelper;
                helper.LoadAssembly(sourceAssembly);
                return helper as AsyncMethodHelper;
            }
            catch(Exception ex)
            {
                // If we can't load it for some reason, we issue a warning
                // and won't try to do it again for the assembly.
                logger.SendWarningMessage("Unable to reflect on " + sourceAssembly + "\r\nSource data will not be available for some of the tests",ex);
                return null;
            }
        }


        #endregion

        #region Private Properties

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

        // NOTE: There is some sort of timing issue involved
        // in creating the DiaSession. When it is created
        // in the constructor, an exception is thrown on the
        // call to GetNavigationData. We don't understand
        // this, we're just dealing with it.
        private DiaSession diaSession;
        private bool tryToCreateDiaSession = true;
        private DiaSession DiaSession
        {
            get
            {
                if (tryToCreateDiaSession)
                {
                    try
                    {
                        diaSession = new DiaSession(sourceAssembly);
                    }
                    catch (Exception)
                    {
                        // If this isn't a project type supporting DiaSession,
                        // we just issue a warning. We won't try this again.
                        logger.SendWarningMessage("Unable to create DiaSession for " + sourceAssembly + "\r\nNo source location data will be available for this assembly.");
                    }

                    tryToCreateDiaSession = false;
                }

                return diaSession;
            }
        }

        private AsyncMethodHelper asyncMethodHelper;
        bool tryToCreateHelper = true;
        private AsyncMethodHelper AsyncMethodHelper
        {
            get
            {
                if (tryToCreateHelper)
                {
                    tryToCreateHelper = false;
                    asyncMethodHelper = TryCreateHelper(sourceAssembly);
                }

                return asyncMethodHelper;
            }
        }

        #endregion
    }
}