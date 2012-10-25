// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Core;
using NUnit.Core.Builders;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class TestConverterTests_StaticHelpers
    {
        [TestCase(ResultState.Cancelled, Result = TestOutcome.None, Category = "TestConverter")]
        [TestCase(ResultState.Error, Result = TestOutcome.Failed, Category = "TestConverter")]
        [TestCase(ResultState.Failure, Result = TestOutcome.Failed, Category = "TestConverter")]
        [TestCase(ResultState.Ignored, Result = TestOutcome.Skipped, Category = "TestConverter")]
        [TestCase(ResultState.Inconclusive, Result = TestOutcome.None, Category = "TestConverter")]
        [TestCase(ResultState.NotRunnable, Result = TestOutcome.Failed, Category = "TestConverter")]
        [TestCase(ResultState.Skipped, Result = TestOutcome.Skipped, Category = "TestConverter")]
        [TestCase(ResultState.Success, Result = TestOutcome.Passed, Category = "TestConverter")]
        public TestOutcome ResultStateToTestOutcome(NUnit.Core.ResultState resultState)
        {
            return TestConverter.ResultStateToTestOutcome(resultState);
        }
    }
}
