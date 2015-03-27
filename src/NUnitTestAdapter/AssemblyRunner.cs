// ****************************************************************
// Copyright (c) 2013-2015 NUnit Software. All rights reserved.
// ****************************************************************

//#define LAUNCHDEBUGGER

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;
using NUnit.Engine.Drivers;

namespace NUnit.VisualStudio.TestAdapter
{
    /// <summary>
    /// The AssemblyRunner class executes tests in a single assembly
    /// </summary>
    public class AssemblyRunner : IDisposable
    {
        private readonly TestLogger logger;
        private readonly string assemblyName;

        private NUnit3FrameworkDriver driver;
        private TestFilter nunitFilter;
        private readonly List<TestCase> loadedTestCases;
        private readonly TestConverter testConverter;

        #region Constructors

        // This constructor is called by the others and is used directly for testing
        public AssemblyRunner(TestLogger logger, string assemblyName)
        {
            this.logger = logger;
            this.assemblyName = assemblyName;
            testConverter = new TestConverter(logger, assemblyName);
            loadedTestCases = new List<TestCase>();
            nunitFilter = TestFilter.Empty;
        }

        // This constructor is used when the executor is called with a list of test cases
        public AssemblyRunner(TestLogger logger, string assemblyName, IEnumerable<TestCase> selectedTestCases)
            : this(logger, assemblyName)
        {
            nunitFilter = MakeTestFilter(selectedTestCases);
        }

        private readonly ITfsTestFilter tfsFilter;

        // This constructor is used when the executor is called with a list of assemblies
        public AssemblyRunner(TestLogger logger, string assemblyName, ITfsTestFilter tfsFilter)
            : this(logger, assemblyName)
        {
            this.tfsFilter = tfsFilter;
        }

        private static TestFilter MakeTestFilter(IEnumerable<TestCase> testCases)
        {
            var testFilter = new StringBuilder("<filter><tests>");

            foreach (TestCase testCase in testCases)
                testFilter.AppendFormat("<test>{0}</test>", testCase.FullyQualifiedName.Replace("<", "&lt;").Replace(">", "&gt;"));

            testFilter.Append("</tests></filter>");

            return new TestFilter(testFilter.ToString());
        }

        #endregion

        #region Properties

        // TODO: Revise tests and remove
        public TestFilter NUnitFilter 
        {
            get { return nunitFilter; }
        }

        // TODO: Revise tests and remove
        public IList<TestCase> LoadedTestCases 
        {
            get { return loadedTestCases; }
        }

        // TODO: Revise tests and remove
        public TestConverter TestConverter
        { 
            get { return testConverter; }
        }

        #endregion

        #region Public Methods

        public void RunAssembly(IFrameworkHandle testLog)
        {
            try
            {
#if LAUNCHDEBUGGER
            System.Diagnostics.Debugger.Launch();
#endif
                if (TryLoadAssembly())
                {
                    using (NUnitEventListener listener = new NUnitEventListener(testLog, TestConverter))
                    {
                        try
                        {
                            driver.Run(listener, NUnitFilter);
                        }
                        catch (NullReferenceException)
                        {
                            // this happens during the run when CancelRun is called.
                            logger.SendDebugMessage("Nullref caught");
                        }
                    }
                }
                else
                {
                    logger.NUnitLoadError(assemblyName);
                }
            }
            catch (BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                logger.AssemblyNotSupportedWarning(assemblyName);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                logger.DependentAssemblyNotFoundWarning(ex.FileName, assemblyName);
            }
            catch (Exception ex)
            {
                logger.SendErrorMessage("Exception thrown executing tests in " + assemblyName, ex);
            }
        }

        public void CancelRun()
        {
            if (driver != null)
                driver.StopRun(true);
       }

        // Try to load the assembly and, if successful, populate
        // the list of all loaded assemblies. As a side effect
        // of calling TestConverter.ConvertTestCase, the converter's
        // cache of all test cases is populated as well. All
        // future calls to convert a test case may now use the cache.
        private bool TryLoadAssembly()
        {
            driver = GetDriver(assemblyName);
            XmlNode loadResult = XmlHelper.CreateXmlNode(driver.Load());
            if (loadResult.GetAttribute("runstate") != "Runnable")
                return false;

            logger.SendMessage(TestMessageLevel.Informational,string.Format("Loading tests from {0}",assemblyName));
            foreach (XmlNode testNode in XmlHelper.CreateXmlNode(driver.Explore(TestFilter.Empty)).SelectNodes("//test-case"))
                LoadedTestCases.Add(TestConverter.ConvertTestCase(testNode));

            if (tfsFilter==null || !tfsFilter.HasTfsFilterValue) 
                return true;
            var filteredTestCases = tfsFilter.CheckFilter(LoadedTestCases);
            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
            logger.SendMessage(TestMessageLevel.Informational, string.Format("TFS Filter detected: LoadedTestCases {0}, Filterered Test Cases {1}", LoadedTestCases.Count, testCases.Count()));
            nunitFilter = MakeTestFilter(testCases);

            return true;
        }

        private NUnit3FrameworkDriver GetDriver(string sourceAssembly)
        {
            var setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(sourceAssembly);
            var domain = AppDomain.CreateDomain("testDomain", null, setup);

            var settings = new Dictionary<string, object>();
            //settings["ShadowCopyFiles"] = ShadowCopy;

            var driver = new NUnit3FrameworkDriver(domain, sourceAssembly, settings);
            return driver;
        }

        #endregion

        #region IDisposable
        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (TestConverter != null)
                        TestConverter.Dispose();
                }
            }
            disposed = true;
        }

        ~AssemblyRunner()
        {
            Dispose(false);
        }
        #endregion
    }
}