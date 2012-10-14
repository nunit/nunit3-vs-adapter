// ****************************************************************
// Copyright (c) 2011 NUnit Software. All rights reserved.
// ****************************************************************

using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
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

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            // Filter out the sources which can have NUnit tests. 
            foreach (string sourceAssembly in SanitizeSources(sources))
            {
                TestRunner runner = new TestDomain();
                TestPackage package = new TestPackage(sourceAssembly);

                try
                {
                    if (runner.Load(package))
                    {
                        var testCaseMap = CreateTestCaseMap(runner.Test as TestNode);
                        this.testConverter = new TestConverter(sourceAssembly, testCaseMap);

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

        private Dictionary<string, NUnit.Core.TestNode> CreateTestCaseMap(TestNode topLevelTest)
        {
            var map = new Dictionary<string, NUnit.Core.TestNode>();
            AddTestCasesToMap(map, topLevelTest);

            return map;
        }

        private void AddTestCasesToMap(Dictionary<string, NUnit.Core.TestNode> map, TestNode test)
        {
            if (test.IsSuite)
                foreach (TestNode child in test.Tests)
                    AddTestCasesToMap(map, child);
            else
                map.Add(test.TestName.UniqueName, test);
        }

        #endregion
    }
}
