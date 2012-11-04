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
            // Set the logger to use for messages
            SetLogger(logger);

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            // Filter out the sources which can have NUnit tests. 
            foreach (string sourceAssembly in sources)
            {
#if DEBUG
                SendInformationalMessage("Processing " + sourceAssembly);
#endif
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
                        {
                            int cases = ProcessTestCases(topNode, discoverySink);
#if DEBUG
                            SendInformationalMessage(string.Format("Discovered {0} test cases", cases));
#endif
                        }
                    }
                    else
                    {
                        NUnitLoadError(sourceAssembly);
                    }
                }
                catch (System.BadImageFormatException)
                {
                    // we skip the native c++ binaries that we don't support.
                    AssemblyNotSupportedWarning(sourceAssembly);
                }
                catch (System.Exception ex)
                {
                    SendErrorMessage("Exception thrown discovering tests in " + sourceAssembly, ex);
                }
                finally
                {
                    testConverter.Dispose();
                    runner.Unload();
                }
            }
        }

        private int ProcessTestCases(TestNode test, ITestCaseDiscoverySink discoverySink)
        {
            int cases = 0;

            if (test.IsSuite)
            {
                foreach (TestNode child in test.Tests)
                    cases += ProcessTestCases(child, discoverySink);
            }
            else
            {
                try
                {
                    TestCase testCase = testConverter.ConvertTestCase(test);
                    discoverySink.SendTestCase(testCase);
                    cases += 1;
                }
                catch (System.Exception ex)
                {
                    SendErrorMessage("Exception converting " + test.TestName.FullName, ex);
                }
            }

            return cases;
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
