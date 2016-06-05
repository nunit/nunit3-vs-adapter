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
using VSTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter
{
    public class TestConverter : IDisposable
    {
        private readonly TestLogger _logger;
        private readonly Dictionary<string, TestCase> _vsTestCaseMap;
        private readonly string _sourceAssembly;
        private NavigationDataProvider _navigationDataProvider;

        #region Constructor

        public TestConverter(TestLogger logger, string sourceAssembly)
        {
            _logger = logger;
            _sourceAssembly = sourceAssembly;
            _vsTestCaseMap = new Dictionary<string, TestCase>();
            _navigationDataProvider = new NavigationDataProvider(sourceAssembly);
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
            if (_vsTestCaseMap.ContainsKey(id))
                return _vsTestCaseMap[id];
           
            // Convert to VS TestCase and cache the result
            var testCase = MakeTestCaseFromXmlNode(testNode);
            _vsTestCaseMap.Add(id, testCase);
            return testCase;             
        }

        public TestCase GetCachedTestCase(string id)
        {
            if (_vsTestCaseMap.ContainsKey(id))
                return _vsTestCaseMap[id];

            _logger.Error("Test " + id + " not found in cache");
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                //?
            }
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
                                     _sourceAssembly)
            {
                DisplayName = testNode.GetAttribute("name"),
                CodeFilePath = null,
                LineNumber = 0
            };

            var className = testNode.GetAttribute("classname");
            var methodName = testNode.GetAttribute("methodname");
            var navData = _navigationDataProvider.GetNavigationData(className, methodName);
            if (navData.IsValid)
            {
                testCase.CodeFilePath = navData.FilePath;
                testCase.LineNumber = navData.LineNumber;
            }

            testCase.AddTraitsFromTestNode(testNode);

            return testCase;
        }

        // public for testing
        //public NavigationData GetNavigationData(string className, string methodName)
        //{
            //    if (this.DiaSession == null) return null;

            //    // First try using the class and method names provided directly
            //    var navData = DiaSession.GetNavigationData(className, methodName);

            //    if (NavigationDataIsValid(navData)) return navData;

            //    // We only use NavigationDataHelper if the normal call to DiaSession fails
            //    // because it causes creation of a separate AppDomain for reflection.
            //    if (NavigationDataHelper != null)
            //    {
            //        string definingClassName = NavigationDataHelper.GetDefiningClassName(className, methodName);
            //        if (definingClassName != className)
            //        {
            //            navData = DiaSession.GetNavigationData(definingClassName, methodName);
            //            if (NavigationDataIsValid(navData))
            //                return navData;
            //        }

            //        string stateMachineClassName = NavigationDataHelper.GetClassNameForAsyncMethod(className, methodName);
            //        if (stateMachineClassName != null)
            //            navData = diaSession.GetNavigationData(stateMachineClassName, "MoveNext");
            //    }

            //    if (!NavigationDataIsValid(navData))
            //        logger.Warning(string.Format("No source data found for {0}.{1}", className, methodName));

            //    return navData;
        //}

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
                    return resultNode.GetAttribute("label")=="Ignored"
                        ? TestOutcome.Skipped
                        : TestOutcome.None;
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

        #endregion
    }
}