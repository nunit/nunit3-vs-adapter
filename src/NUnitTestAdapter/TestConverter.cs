// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.VisualStudio.TestAdapter.Internal;
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
        public TestCase ConvertTestCase(XmlNode testNode)
        {
            if (testNode == null || testNode.Name != "test-case")
                throw new ArgumentException("The argument must be a test case", "test");

            // Return cached value if we have one
            string id = testNode.GetAttribute("id");
            if (vsTestCaseMap.ContainsKey(id))
                return vsTestCaseMap[id];
           
            // Convert to VS TestCase and cache the result
            var testCase = MakeTestCaseFromXmlNode(testNode);
            vsTestCaseMap.Add(id, testCase);
            return testCase;             
        }

        public TestCase GetCachedTestCase(string id)
        {
            if (vsTestCaseMap.ContainsKey(id))
                return vsTestCaseMap[id];

            logger.SendErrorMessage("Test " + id + " not found in cache");
            return null;
        }

        public VSTestResult ConvertTestResult(XmlNode resultNode)
        {
            TestCase ourCase = GetCachedTestCase(resultNode.GetAttribute("id"));
            if (ourCase == null) return null;

            VSTestResult ourResult = new VSTestResult(ourCase)
            {
                DisplayName = ourCase.DisplayName,
                Outcome = GetTestOutcome(resultNode),
                Duration = TimeSpan.FromSeconds(resultNode.GetAttribute("duration", 0.0))
            };

            var startTime = resultNode.GetAttribute("start-time");
            if (startTime != null)
                ourResult.StartTime = DateTimeOffset.Parse(startTime);

            var endTime = resultNode.GetAttribute("end-time");
            if (endTime != null)
                ourResult.EndTime = DateTimeOffset.Parse(endTime);

            // TODO: Remove this when NUnit provides a better duration
            if (ourResult.Duration == TimeSpan.Zero && (ourResult.Outcome == TestOutcome.Passed || ourResult.Outcome == TestOutcome.Failed))
                ourResult.Duration = TimeSpan.FromTicks(1);

            ourResult.ComputerName = Environment.MachineName;

            ourResult.ErrorMessage = GetErrorMessage(resultNode);

            XmlNode stackTraceNode = resultNode.SelectSingleNode("failure/stack-trace");
            if (stackTraceNode != null)
                ourResult.ErrorStackTrace = stackTraceNode.InnerText;

            XmlNode outputNode = resultNode.SelectSingleNode("output");
            if (outputNode != null)
                ourResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, outputNode.InnerText));

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
        private TestCase MakeTestCaseFromXmlNode(XmlNode testNode)
        {
            var testCase = new TestCase(
                                     testNode.GetAttribute("fullname"),
                                     new Uri(NUnit3TestExecutor.ExecutorUri),
                                     this.sourceAssembly)
            {
                DisplayName = testNode.GetAttribute("name"),
                CodeFilePath = null,
                LineNumber = 0
            };

            var className = testNode.GetAttribute("classname");
            var methodName = testNode.GetAttribute("methodname");
            var navData = GetNavigationData(className, methodName);
            if (navData != null)
            {
                testCase.CodeFilePath = navData.FileName;
                testCase.LineNumber = navData.MinLineNumber;
            }

            testCase.AddTraitsFromTestNode(testNode);

            return testCase;
        }

        // public for testing
        public DiaNavigationData GetNavigationData(string className, string methodName)
        {
            if (this.DiaSession == null) return null;

            var navData = DiaSession.GetNavigationData(className, methodName);

            if (navData != null && navData.FileName != null) return navData;

            // DiaSession.GetNavigationData returned null, see if it's an async method. 
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
        public static TestOutcome GetTestOutcome(XmlNode resultNode)
        {
            switch (resultNode.GetAttribute("result"))
            {
                case "Passed":
                    return TestOutcome.Passed;
                case "Failed":
                    return TestOutcome.Failed;
                case "Skipped":
                    return TestOutcome.Skipped;
                default:
                    return TestOutcome.None;
            }
        }

        private static readonly string NL = Environment.NewLine;

        private string GetErrorMessage(XmlNode resultNode)
        {
            XmlNode messageNode = resultNode.SelectSingleNode("failure/message");
            if (messageNode != null)
            {
                string message = messageNode.InnerText;

                // If we're running in the IDE, remove any caret line from the message
                // since it will be displayed using a variable font and won't make sense.
                if (!string.IsNullOrEmpty(message) && NUnitTestAdapter.IsRunningUnderIDE)
                {
                    string pattern = NL + "  -*\\^" + NL;
                    message = Regex.Replace(message, pattern, NL, RegexOptions.Multiline);
                }

                return message;
            }
            else
            {
                XmlNode reasonNode = resultNode.SelectSingleNode("reason/message");
                if (reasonNode != null)
                    return reasonNode.InnerText;
            }

            return null;
        }

        private AsyncMethodHelper TryCreateHelper(string sourceAssembly)
        {
            var setup = new AppDomainSetup();

            var thisAssembly = Assembly.GetExecutingAssembly();
            setup.ApplicationBase = Path.GetDirectoryName(thisAssembly.ManifestModule.FullyQualifiedName);

            this.asyncMethodHelperDomain = AppDomain.CreateDomain("AsyncMethodHelper", null, setup);

            try
            {
                var helper = this.asyncMethodHelperDomain.CreateInstanceAndUnwrap(
                    thisAssembly.FullName,
                    typeof(AsyncMethodHelper).FullName) as AsyncMethodHelper;
                helper.LoadAssembly(sourceAssembly);
                return helper as AsyncMethodHelper;
            }
            catch (Exception ex)
            {
                // If we can't load it for some reason, we issue a warning
                // and won't try to do it again for the assembly.
                logger.SendWarningMessage("Unable to create AsyncMethodHelper\r\nSource data will not be available for some of the tests", ex);
                return null;
            }
        }


        #endregion

        #region Private Properties

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
                    catch (Exception ex)
                    {
                        // If this isn't a project type supporting DiaSession,
                        // we just issue a warning. We won't try this again.
                        logger.SendWarningMessage("Unable to create DiaSession for " + sourceAssembly + "\r\nNo source location data will be available for this assembly.");
                        logger.SendDebugMessage(ex.Message);
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