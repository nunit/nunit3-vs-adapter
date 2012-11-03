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

                        TestNode topNode = runner.Test as TestNode;
                        if (topNode != null)
                            ProcessTestCases(topNode, discoverySink, logger);
                    }
                }
                catch (System.BadImageFormatException)
                {
                    // we skip the native c++ binaries that we don't support.
                    logger.SendMessage(TestMessageLevel.Warning, "Assembly not supported: " + sourceAssembly);
                }
                catch (System.Exception ex)
                {
                    logger.SendMessage(TestMessageLevel.Error, "Exception thrown discovering tests in " + sourceAssembly);
                    logger.SendMessage(TestMessageLevel.Error, ex.ToString());
                }
                finally
                {
                    testConverter.Dispose();
                    runner.Unload();
                }
            }
        }

        private void ProcessTestCases(TestNode test, ITestCaseDiscoverySink discoverySink, IMessageLogger logger)
        {
            if (test.IsSuite)
            {
                foreach (TestNode child in test.Tests)
                    ProcessTestCases(child, discoverySink, logger);
            }
            else
            {
                try
                {
                    TestCase testCase = testConverter.ConvertTestCase(test);
                    discoverySink.SendTestCase(testCase);
                }
                catch (System.Exception ex)
                {
                    logger.SendMessage(TestMessageLevel.Error, "Exception converting " + test.TestName.FullName);
                    logger.SendMessage(TestMessageLevel.Error, ex.ToString());   
                }
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
