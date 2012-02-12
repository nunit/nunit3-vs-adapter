// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Core;
using NUnit.Util;

namespace NUnit.VisualStudio.TestAdapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(NUnitTestExecutor.ExecutorUri)]
    public sealed class NUnitTestDiscoverer : NUnitTestAdapter, ITestDiscoverer
    {
        private TestConverter testConverter;

        #region ITestDiscoverer Members

        public void DiscoverTests(IEnumerable<string> sources, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            // Filter out the sources which can have NUnit tests. 
            List<string> testAssemblies = SanitizeSources(sources);
            if (testAssemblies.Count == 0)
                return;

            testConverter = new TestConverter();

            TestPackage package = new TestPackage("", testAssemblies);

            TestRunner runner = new MultipleTestDomainRunner();

            try
            {
                if (runner.Load(package))
                {
                    foreach (TestNode test in runner.Test.Tests)
                    {
                        SendTestCase(test, discoverySink);
                    }
                }
            }
            catch (System.BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
            }
            finally
            {
                testConverter.Dispose();
                runner.Unload();
            }
        }

        private void SendTestCase(TestNode test, ITestCaseDiscoverySink discoverySink)
        {
            if (test.IsSuite)
            {
                foreach (TestNode child in test.Tests)
                    SendTestCase(child, discoverySink);
            }
            else
            {
                TestCase testCase = testConverter.ConvertTestCase(test);
                discoverySink.SendTestCase(testCase);
            }
        }

        #endregion

    }
}
