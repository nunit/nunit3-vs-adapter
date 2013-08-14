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
    using System;

    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(NUnitTestExecutor.ExecutorUri)]
    public sealed class NUnitTestDiscoverer : NUnitTestAdapter, ITestDiscoverer, IDisposable
    {
        private TestConverter testConverter;

        #region ITestDiscoverer Members

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger messageLogger, ITestCaseDiscoverySink discoverySink)
        {
            testLog.Initialize(messageLogger);
            Info("discovering tests", "started");
            
            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            foreach (string sourceAssembly in sources)
            {
                testLog.SendDebugMessage("Processing " + sourceAssembly);

                TestRunner runner = new TestDomain();
                TestPackage package = new TestPackage(sourceAssembly);

                try
                {
                    if (runner.Load(package))
                    {
                        this.testConverter = new TestConverter(testLog, sourceAssembly);

                        int cases = ProcessTestCases(runner.Test, discoverySink);

                        testLog.SendDebugMessage(string.Format("Discovered {0} test cases", cases));
                    }
                    else
                    {
                        testLog.NUnitLoadError(sourceAssembly);
                    }
                }
                catch (System.BadImageFormatException)
                {
                    // we skip the native c++ binaries that we don't support.
                    testLog.AssemblyNotSupportedWarning(sourceAssembly);
                }

                catch (System.IO.FileNotFoundException ex)
                {
                    // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                    testLog.DependentAssemblyNotFoundWarning(ex.FileName, sourceAssembly);
                }
                catch (System.Exception ex)
                {
                    testLog.SendErrorMessage("Exception thrown discovering tests in " + sourceAssembly, ex);
                }
                finally
                {
                    runner.Unload();
                }
            }

            Info("discovering test","finished");
        }

        private int ProcessTestCases(ITest test, ITestCaseDiscoverySink discoverySink)
        {
            int cases = 0;

            if (test.IsSuite)
            {
                foreach (ITest child in test.Tests)
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
                    testLog.SendErrorMessage("Exception converting " + test.TestName.FullName, ex);
                }
            }

            return cases;
        }

        #endregion

        public void Dispose()
        {
            this.Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (testConverter!=null)
                    testConverter.Dispose();
            }
            testConverter = null;
        }
    }
}
