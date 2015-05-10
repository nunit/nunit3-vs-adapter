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
    [DefaultExecutorUri(NUnit3TestExecutor.ExecutorUri)]
    public sealed class NUnit3TestDiscoverer : NUnitTestAdapter, ITestDiscoverer
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

                TestConverter testConverter = null;
                NUnit3FrameworkDriver frameworkDriver = null;

                try
                {
                    frameworkDriver = GetDriver(sourceAssembly);
                    XmlNode loadResult = XmlHelper.CreateXmlNode(frameworkDriver.Load(sourceAssembly, new Dictionary<string, object>()));
                    if (loadResult.GetAttribute("runstate") == "Runnable")
                    {
                        XmlNode topNode = XmlHelper.CreateXmlNode(frameworkDriver.Explore(TestFilter.Empty));

                        testConverter = new TestConverter(TestLog, sourceAssembly);
                        int cases = ProcessTestCases(topNode, discoverySink, testConverter);
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
                    // Either the NUnit framework was not referenced by the test assembly
                    // or some other error occured. Not a problem if not an NUnit assembly.
                    TestLog.DependentAssemblyNotFoundWarning(ex.FileName, sourceAssembly);
                }
                catch (FileLoadException ex)
                {
                    // Attempts to load an invalid assembly, or an assembly with missing dependencies
                    TestLog.LoadingAssemblyFailedWarning(ex.FileName, sourceAssembly);
                }
                catch (TypeLoadException ex)
                {
                    if (ex.TypeName == "NUnit.Framework.Api.FrameworkController")
                        TestLog.SendWarningMessage("   Skipping NUnit 2.x test assembly");
                    else
                        TestLog.SendErrorMessage("Exception thrown discovering tests in " + sourceAssembly, ex);
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

            Info("discovering test", "finished");
        }

        #endregion

        #region Helper Methods

        private NUnit3FrameworkDriver GetDriver(string sourceAssembly)
        {
            var setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(sourceAssembly);
            var domain = AppDomain.CreateDomain("testDomain", null, setup);

            var settings = new Dictionary<string, object>();
            settings["ShadowCopyFiles"] = ShadowCopy;

            var driver = new NUnit3FrameworkDriver(domain);
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
