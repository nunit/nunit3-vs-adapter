// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

//#define LAUNCHDEBUGGER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;
using NUnit.Engine.Drivers;

namespace NUnit.VisualStudio.TestAdapter
{
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(NUnitTestExecutor.ExecutorUri)]
    public sealed class NUnitTestDiscoverer : NUnitTestAdapter, ITestDiscoverer
    {
        private static readonly Version VERSION_3_0 = new Version(3, 0);

        #region ITestDiscoverer Members

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger messageLogger, ITestCaseDiscoverySink discoverySink)
        {
#if LAUNCHDEBUGGER
            Debugger.Launch();
#endif
            TestLog.Initialize(messageLogger);

            if (RegistryFailure)
                TestLog.SendErrorMessage(ErrorMsg);

            Info("discovering tests", "started");

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            foreach (string sourceAssembly in sources)
            {
                TestLog.SendDebugMessage("Processing " + sourceAssembly);

                if (IsNUnit3TestAssembly(sourceAssembly))
                {
                    TestConverter testConverter = null;
                    try
                    {
                        var driver = GetDriver(sourceAssembly);
                        XmlNode loadResult = XmlHelper.CreateXmlNode(driver.Load());
                        if (loadResult.GetAttribute("runstate") == "Runnable")
                        {
                            XmlNode topNode = XmlHelper.CreateXmlNode(driver.Explore(TestFilter.Empty));

                            testConverter = new TestConverter(TestLog, sourceAssembly);
                            int cases = ProcessTestCases(topNode, discoverySink,testConverter);
                            TestLog.SendDebugMessage(string.Format("Discovered {0} test cases", cases));
                        }
                        else
                        {
                            TestLog.NUnitLoadError(sourceAssembly);
                        }
                    }
                    catch (BadImageFormatException)
                    {
                        // we skip the native c++ binaries that we don't support.
                        TestLog.AssemblyNotSupportedWarning(sourceAssembly);
                    }
                    catch (FileNotFoundException ex)
                    {
                        // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                        TestLog.DependentAssemblyNotFoundWarning(ex.FileName, sourceAssembly);
                    }
                    catch (FileLoadException ex)
                    {
                        // Attempts to load an invalid assembly, or an assembly with missing dependencies
                        TestLog.LoadingAssemblyFailedWarning(ex.FileName, sourceAssembly);
                    }
                    catch (Exception ex)
                    {

                        TestLog.SendErrorMessage("Exception thrown discovering tests in " + sourceAssembly, ex);
                    }
                    finally
                    {
                        if (testConverter != null)
                            testConverter.Dispose();
                    }
                }
            }

            Info("discovering test", "finished");
        }

        #endregion

        #region Helper Methods

        private bool IsNUnit3TestAssembly(string sourceAssembly)
        {
            var assembly = Assembly.ReflectionOnlyLoadFrom(sourceAssembly);

            foreach (var refAssembly in assembly.GetReferencedAssemblies())
            {
                if (refAssembly.Name == "nunit.framework")
                {
                    if (refAssembly.Version >= VERSION_3_0)
                        return true;

                    TestLog.SendDebugMessage("   Skipping NUnit 2.x assembly");
                    return false;
                }
            }

            TestLog.SendDebugMessage("   Skipping non-NUnit assembly");
            return false;
        }

        private NUnit3FrameworkDriver GetDriver(string sourceAssembly)
        {
            var setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(sourceAssembly);
            var domain = AppDomain.CreateDomain("testDomain", null, setup);

            var settings = new Dictionary<string, object>();
            settings["ShadowCopyFiles"] = ShadowCopy;

            var driver = new NUnit3FrameworkDriver(domain, sourceAssembly, settings);
            return driver;
        }

        private int ProcessTestCases(XmlNode topNode, ITestCaseDiscoverySink discoverySink, TestConverter testConverter)
        {
            int cases = 0;

            foreach (XmlNode testNode in topNode.SelectNodes("//test-case"))
            {
                try
                {
#if LAUNCHDEBUGGER
                    System.Diagnostics.Debugger.Launch();
#endif
                    TestCase testCase = testConverter.ConvertTestCase(testNode);
                    discoverySink.SendTestCase(testCase);
                    cases += 1;
                }
                catch (Exception ex)
                {
                    TestLog.SendErrorMessage("Exception converting " + testNode.GetAttribute("fullname"), ex);
                }
            }

            return cases;
        }

        #endregion
    }
}
