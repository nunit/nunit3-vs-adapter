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
        private readonly TestLogger _logger;
        private readonly string _assemblyName;

        private NUnit3FrameworkDriver _frameworkDriver;
        private TestFilter _nunitFilter;
        private readonly List<TestCase> _loadedTestCases;
        private readonly TestConverter _testConverter;

        #region Constructors

        // This constructor is called by the others and is used directly for testing
        public AssemblyRunner(TestLogger logger, string assemblyName)
        {
            _logger = logger;
            _assemblyName = assemblyName;
            _testConverter = new TestConverter(logger, assemblyName);
            _loadedTestCases = new List<TestCase>();
            _nunitFilter = TestFilter.Empty;
        }

        // This constructor is used when the executor is called with a list of test cases
        public AssemblyRunner(TestLogger logger, string assemblyName, IEnumerable<TestCase> selectedTestCases)
            : this(logger, assemblyName)
        {
            _nunitFilter = MakeTestFilter(selectedTestCases);
        }

        private readonly ITfsTestFilter _tfsFilter;

        // This constructor is used when the executor is called with a list of assemblies
        public AssemblyRunner(TestLogger logger, string assemblyName, ITfsTestFilter tfsFilter)
            : this(logger, assemblyName)
        {
            _tfsFilter = tfsFilter;
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
            get { return _nunitFilter; }
        }

        #endregion

        #region Public Methods

        public void RunAssembly(IFrameworkHandle testLog, bool shadowCopy)
        {
            try
            {
#if LAUNCHDEBUGGER
            System.Diagnostics.Debugger.Launch();
#endif
                if (TryLoadAssembly(shadowCopy))
                {
                    using (NUnitEventListener listener = new NUnitEventListener(testLog, _testConverter))
                    {
                        try
                        {
                            _frameworkDriver.Run(listener, NUnitFilter);
                        }
                        catch (NullReferenceException)
                        {
                            // this happens during the run when CancelRun is called.
                            _logger.SendDebugMessage("Nullref caught");
                        }
                    }
                }
                else
                {
                    _logger.NUnitLoadError(_assemblyName);
                }
            }
            catch (BadImageFormatException)
            {
                // we skip the native c++ binaries that we don't support.
                _logger.AssemblyNotSupportedWarning(_assemblyName);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Probably from the GetExportedTypes in NUnit.core, attempting to find an assembly, not a problem if it is not NUnit here
                _logger.DependentAssemblyNotFoundWarning(ex.FileName, _assemblyName);
            }
            catch (Exception ex)
            {
                _logger.SendErrorMessage("Exception thrown executing tests in " + _assemblyName, ex);
            }
            finally
            {
                _frameworkDriver.Unload();
            }
        }

        public void CancelRun()
        {
            if (_frameworkDriver != null)
                _frameworkDriver.StopRun(true);
       }

        // Try to load the assembly and, if successful, populate
        // the list of all loaded assemblies. As a side effect
        // of calling TestConverter.ConvertTestCase, the converter's
        // cache of all test cases is populated as well. All
        // future calls to convert a test case may now use the cache.
        private bool TryLoadAssembly(bool shadowCopy)
        {
            _frameworkDriver = GetDriver(_assemblyName, shadowCopy);
            XmlNode loadResult = XmlHelper.CreateXmlNode(_frameworkDriver.Load());
            if (loadResult.GetAttribute("runstate") != "Runnable")
                return false;

            _logger.SendMessage(TestMessageLevel.Informational,string.Format("Loading tests from {0}",_assemblyName));
            foreach (XmlNode testNode in XmlHelper.CreateXmlNode(_frameworkDriver.Explore(TestFilter.Empty)).SelectNodes("//test-case"))
                _loadedTestCases.Add(_testConverter.ConvertTestCase(testNode));

            if (_tfsFilter==null || !_tfsFilter.HasTfsFilterValue) 
                return true;
            var filteredTestCases = _tfsFilter.CheckFilter(_loadedTestCases);
            var testCases = filteredTestCases as TestCase[] ?? filteredTestCases.ToArray();
            _logger.SendMessage(TestMessageLevel.Informational, string.Format("TFS Filter detected: LoadedTestCases {0}, Filterered Test Cases {1}", _loadedTestCases.Count, testCases.Count()));
            _nunitFilter = MakeTestFilter(testCases);

            return true;
        }

        private NUnit3FrameworkDriver GetDriver(string sourceAssembly, bool shadowCopy)
        {
            var setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(sourceAssembly);
            var domain = AppDomain.CreateDomain("testDomain", null, setup);

            var settings = new Dictionary<string, object>();
            settings["ShadowCopyFiles"] = shadowCopy;

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
                    if (_testConverter != null)
                        _testConverter.Dispose();
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