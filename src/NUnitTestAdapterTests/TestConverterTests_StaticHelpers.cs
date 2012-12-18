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
    [Category("TestConverter")]
    public class TestConverterTests_StaticHelpers
    {
        [TestCase(ResultState.Cancelled, Result = TestOutcome.None)]
        [TestCase(ResultState.Error, Result = TestOutcome.Failed)]
        [TestCase(ResultState.Failure, Result = TestOutcome.Failed)]
        [TestCase(ResultState.Ignored, Result = TestOutcome.Skipped)]
        [TestCase(ResultState.Inconclusive, Result = TestOutcome.None)]
        [TestCase(ResultState.NotRunnable, Result = TestOutcome.Failed)]
        [TestCase(ResultState.Skipped, Result = TestOutcome.Skipped)]
        [TestCase(ResultState.Success, Result = TestOutcome.Passed)]
        public TestOutcome ResultStateToTestOutcome(NUnit.Core.ResultState resultState)
        {
            return TestConverter.ResultStateToTestOutcome(resultState);
        }
    }
}
