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
    using System.Diagnostics;

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
            Logger = logger;
            Info("discovering tests", "started");
            
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
                        var nunitTestCaseMap = CreateNUnitTestCaseMap(runner.Test as TestNode);
                        this.testConverter = new TestConverter(sourceAssembly, nunitTestCaseMap,false);  // TFS Doesnt matter here

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

                catch (System.IO.FileNotFoundException ex)
                {
                    // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                    DependentAssemblyNotFoundWarning(ex.FileName, sourceAssembly);
                }
                catch (System.Exception ex)
                {
                    SendErrorMessage("Exception thrown discovering tests in " + sourceAssembly, ex);
                }
                finally
                {
                    if (testConverter!=null)
                        testConverter.Dispose();
                    runner.Unload();
                }
            }
            Info("discovering test","finished");
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
#if LAUNCHDEBUGGER
            Debugger.Launch();
#endif
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

        private Dictionary<string, NUnit.Core.TestNode> CreateNUnitTestCaseMap(TestNode topLevelTest)
        {
            var nunitTestCaseMap = new Dictionary<string, NUnit.Core.TestNode>();
            AddTestCasesToNUnitTestCaseMap(nunitTestCaseMap, topLevelTest);

            return nunitTestCaseMap;
        }

        private void AddTestCasesToNUnitTestCaseMap(Dictionary<string, NUnit.Core.TestNode> nunitTestCaseMap, TestNode test)
        {
            if (test.IsSuite)
                foreach (TestNode child in test.Tests)
                    AddTestCasesToNUnitTestCaseMap(nunitTestCaseMap, child);
            else
                nunitTestCaseMap.Add(test.TestName.UniqueName, test);
        }

        #endregion
    }
}
