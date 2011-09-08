// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;

namespace NUnit.VisualStudio.TestAdapter
{
    public static class NUnitTestHelper
    {
        #region ITest Extension Methods

        public static string GetClassName(this ITest test)
        {
            if (test == null)
                return null;

            if (test.TestType == "TestFixture")
                return test.TestName.FullName;

            Test t = test as Test;
            if (t != null && t.FixtureType != null)
                return t.FixtureType.FullName;

            return GetClassName(test.Parent);
        }

        public static string GetMethodName(this ITest test)
        {
            if (test == null)
                return null;
            
            if (!test.TestName.Name.EndsWith(")"))
                return test.TestName.Name;
            else
                return GetMethodName(test.Parent);
        }

        public static string GetSourceAssembly(this ITest test)
        {
            if (test == null)
                return null;

            if (test.TestType == "Assembly")
                return test.TestName.FullName;

            Test t = test as Test;
            if (t != null && t.FixtureType != null)
                return new Uri(t.FixtureType.Assembly.CodeBase).LocalPath;

            return GetSourceAssembly(test.Parent);
        }

        #endregion

        #region TestName Extension Methods

        public static string GetClassName(this TestName testName)
        {
            var className = testName.FullName;
            var name = testName.Name;

            if (className.Length > name.Length + 1)
                className = className.Substring(0, className.Length - name.Length - 1);

            return className;
        }

        public static string GetMethodName(this TestName testName)
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

        #endregion

        #region ResultState Extension Methods

        public static TestOutcome ToTestOutcome(this ResultState resultState)
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
