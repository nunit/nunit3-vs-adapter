// ***********************************************************************
// Copyright (c) 2011-2018 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using VSTestResult = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestResult;

namespace NUnit.VisualStudio.TestAdapter
{
    public class TestConverter
    {
        private readonly ITestLogger _logger;
        private readonly Dictionary<string, TestCase> _vsTestCaseMap;
        private readonly string _sourceAssembly;
        private readonly NavigationDataProvider _navigationDataProvider;
        private readonly bool _collectSourceInformation;

        #region Constructor

        public TestConverter(ITestLogger logger, string sourceAssembly, bool collectSourceInformation)
        {
            _logger = logger;
            _sourceAssembly = sourceAssembly;
            _vsTestCaseMap = new Dictionary<string, TestCase>();
            _collectSourceInformation = collectSourceInformation;
            TraitsCache = new Dictionary<string, List<Trait>>();

            if (_collectSourceInformation)
            {
                _navigationDataProvider = new NavigationDataProvider(sourceAssembly);
            }
        }

        #endregion

        public IDictionary<string, List<Trait>> TraitsCache { get; }

        #region Public Methods
        /// <summary>
        /// Converts an NUnit test into a TestCase for Visual Studio,
        /// using the best method available according to the exact
        /// type passed and caching results for efficiency.
        /// </summary>
        public TestCase ConvertTestCase(XmlNode testNode)
        {
            if (testNode == null || testNode.Name != "test-case")
                throw new ArgumentException("The argument must be a test case", nameof(testNode));

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

            _logger.Warning("Test " + id + " not found in cache");
            return null;
        }

        private static readonly string NL = Environment.NewLine;

        public TestResultSet GetVSTestResults(XmlNode resultNode)
        {
            var results = new List<VSTestResult>();
            XmlNodeList assertions = resultNode.SelectNodes("assertions/assertion");
            var testcaseResult = GetBasicResult(resultNode);
            foreach (XmlNode assertion in assertions)
            {
                var oneResult = GetBasicResult(resultNode); // we need a new copy, this is currently the simplest way
                if (oneResult != null)
                {
                    oneResult.Outcome = GetAssertionOutcome(assertion);
                    oneResult.ErrorMessage = assertion.SelectSingleNode("message")?.InnerText;
                    oneResult.ErrorStackTrace = assertion.SelectSingleNode("stack-trace")?.InnerText;
                    results.Add(oneResult);
                }
            }

            if (results.Count == 0)
            {
                var result = MakeTestResultFromLegacyXmlNode(resultNode);
                if (result != null)
                    results.Add(result);
            }

            return new TestResultSet {TestCaseResult = testcaseResult, TestResults = results};
        }

        public struct TestResultSet
        {
            public IList<VSTestResult> TestResults { get; set; }
            public TestResult TestCaseResult { get; set; }
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
                                     new Uri(NUnitTestAdapter.ExecutorUri),
                                     _sourceAssembly)
            {
                DisplayName = testNode.GetAttribute("name"),
                CodeFilePath = null,
                LineNumber = 0
            };

            if (_collectSourceInformation && _navigationDataProvider != null)
            {
                var className = testNode.GetAttribute("classname");
                var methodName = testNode.GetAttribute("methodname");
                var navData = _navigationDataProvider.GetNavigationData(className, methodName);
                if (navData.IsValid)
                {
                    testCase.CodeFilePath = navData.FilePath;
                    testCase.LineNumber = navData.LineNumber;
                }
            }

            testCase.AddTraitsFromTestNode(testNode, TraitsCache);

            return testCase;
        }

        private VSTestResult MakeTestResultFromLegacyXmlNode(XmlNode resultNode)
        {
            VSTestResult ourResult = GetBasicResult(resultNode);
            if (ourResult != null)
            {
                var node = resultNode.SelectSingleNode("failure") ?? resultNode.SelectSingleNode("reason");

                string message = node?.SelectSingleNode("message")?.InnerText;
                // If we're running in the IDE, remove any caret line from the message
                // since it will be displayed using a variable font and won't make sense.
                if (!string.IsNullOrEmpty(message) && NUnitTestAdapter.IsRunningUnderIDE)
                {
                    string pattern = NL + "  -*\\^" + NL;
                    message = Regex.Replace(message, pattern, NL, RegexOptions.Multiline);
                }

                ourResult.ErrorMessage = message;
                ourResult.ErrorStackTrace = node?.SelectSingleNode("stack-trace")?.InnerText;
            }

            return ourResult;
        }

        private VSTestResult GetBasicResult(XmlNode resultNode)
        {
            var vsTest = GetCachedTestCase(resultNode.GetAttribute("id"));
            if (vsTest == null) return null;

            var vsResult = new VSTestResult(vsTest)
            {
                DisplayName = vsTest.DisplayName,
                Outcome = GetTestOutcome(resultNode),
                Duration = TimeSpan.FromSeconds(resultNode.GetAttribute("duration", 0.0))
            };

            var startTime = resultNode.GetAttribute("start-time");
            if (startTime != null)
                vsResult.StartTime = DateTimeOffset.Parse(startTime, CultureInfo.InvariantCulture);

            var endTime = resultNode.GetAttribute("end-time");
            if (endTime != null)
                vsResult.EndTime = DateTimeOffset.Parse(endTime, CultureInfo.InvariantCulture);

            // TODO: Remove this when NUnit provides a better duration
            if (vsResult.Duration == TimeSpan.Zero && (vsResult.Outcome == TestOutcome.Passed || vsResult.Outcome == TestOutcome.Failed))
                vsResult.Duration = TimeSpan.FromTicks(1);

            vsResult.ComputerName = Environment.MachineName;

            XmlNode outputNode = resultNode.SelectSingleNode("output");
            if (outputNode != null)
                vsResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, outputNode.InnerText));

            var attachmentSet = ParseAttachments(resultNode);
            if (attachmentSet.Attachments.Count > 0)
                vsResult.Attachments.Add(attachmentSet);

            return vsResult;
        }

        /// <summary>
        /// Looks for attachments in a results node and if any attachments are found they
        /// are returned"/>
        /// </summary>
        /// <param name="resultNode">xml node for test result</param>
        /// <returns>attachments to be added to the test, it will be empty if no attachments are found</returns>
        private AttachmentSet ParseAttachments(XmlNode resultNode)
        {
            var attachmentSet = new AttachmentSet(new Uri(NUnitTestAdapter.ExecutorUri), "Attachments");

            foreach (XmlNode attachment in resultNode.SelectNodes("attachments/attachment"))
            {
                var path = attachment.SelectSingleNode("filePath")?.InnerText ?? string.Empty;
                var description = attachment.SelectSingleNode("description")?.InnerText;

                try
                {
                    // We only support absolute paths since we dont lookup working directory here
                    // any problem with path will throw an exception
                    var fileUri = new Uri(path, UriKind.Absolute);
                    attachmentSet.Attachments.Add(new UriDataAttachment(fileUri, description));
                }
                catch (UriFormatException ex)
                {
                    _logger.Warning($"Ignoring attachment with path '{path}' due to problem with path: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Ignoring attachment with path '{path}': {ex.Message}.");
                }
            }

            return attachmentSet;
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
                    return resultNode.GetAttribute("label") == "Ignored"
                        ? TestOutcome.Skipped
                        : TestOutcome.None;
                case "Warning":
                    return TestOutcome.Skipped;
                default:
                    return TestOutcome.None;
            }
        }

        TestOutcome GetAssertionOutcome(XmlNode assertion)
        {
            switch (assertion.GetAttribute("result"))
            {
                case "Passed":
                    return TestOutcome.Passed;
                case "Failed":
                case "Error":
                    return TestOutcome.Failed;
                case "Warning":
                    return TestOutcome.Skipped;
                case "Inconclusive":
                default:
                    return TestOutcome.None;
            }
        }

        #endregion
    }
}