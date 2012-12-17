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

        private static readonly PropertyInfo traitsProperty;
        private static readonly MethodInfo traitsCollectionAdd;

        #region Constructors

        public TestConverter(string sourceAssembly, Dictionary<string, NUnit.Core.TestNode> nunitTestCases)
        {
            this.sourceAssembly = sourceAssembly;
            this.vsTestCaseMap = new Dictionary<string, TestCase>();
            this.nunitTestCases = nunitTestCases;
        }

        static TestConverter()
        {
            traitsProperty = typeof(TestCase).GetProperty("Traits");
            if (traitsProperty != null)
            {
                var traitCollectionType = traitsProperty.PropertyType;
                if (traitCollectionType != null)
                    traitsCollectionAdd = traitCollectionType.GetMethod("Add", new Type[] {typeof(string), typeof(string)});
            }
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
                return MakeTestCase(test.TestName);
            
            // Use the TestNode and cache the result
            var testCase = MakeTestCase(testNode);
            vsTestCaseMap.Add(test.TestName.UniqueName, testCase);
            return testCase;             
        }

        /// <summary>
        /// Makes a TestCase from a TestNode, adding
        /// navigation data if it can be found.
        /// </summary>
        public TestCase MakeTestCase(NUnit.Core.ITest testNode)
        {
            var testCase = MakeTestCase(testNode.TestName);

            if (this.DiaSession != null)
            {
                var navigationData = DiaSession.GetNavigationData(testNode.ClassName, testNode.MethodName);

                if (navigationData != null)
                {
                    testCase.CodeFilePath = navigationData.FileName;
                    testCase.LineNumber = navigationData.MinLineNumber;
                }
            }

            if (traitsCollectionAdd != null) // implies traitsProperty is not null either
            {
                object traitsCollection = traitsProperty.GetValue(testCase, new object[0]);
                if (traitsCollection != null)
                    AddTraits(testNode, traitsCollection);
            }

            return testCase;
        }

        /// <summary>
        /// Add traits using reflection, since the feature was not present
        /// in VS2012 RTM but was added in the first update.
        /// </summary>
        private static void AddTraits(NUnit.Core.ITest testNode, object traitsCollection)
        {
            if (testNode.Parent != null)
                AddTraits(testNode.Parent, traitsCollection);

            foreach (string propertyName in testNode.Properties.Keys)
            {
                object propertyValue = testNode.Properties[propertyName];

                if (propertyName == "_CATEGORIES")
                {
                    var categories = propertyValue as System.Collections.IEnumerable;
                    if (categories != null)
                        foreach (string category in categories)
                            traitsCollectionAdd.Invoke(traitsCollection, new object[] { "Category", category });
                }
                else if (propertyName[0] != '_') // internal use only
                    traitsCollectionAdd.Invoke(traitsCollection, new object[] { propertyName, propertyValue.ToString() });
            }
        }

        /// <summary>
        /// Makes a TestCase without source info from TestName.
        /// </summary>
        public TestCase MakeTestCase(TestName testName)
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
