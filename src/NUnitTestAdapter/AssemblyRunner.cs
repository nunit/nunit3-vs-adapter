// ****************************************************************
// Copyright (c) 2013 NUnit Software. All rights reserved.
// ****************************************************************

namespace NUnit.VisualStudio.TestAdapter
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

    using NUnit.Core;
    using NUnit.Core.Filters;
    using NUnit.Util;

    /// <summary>
    /// The AssemblyRunner class executes tests in a single assembly
    /// </summary>
    public class AssemblyRunner : IDisposable
    {
        private TestRunner runner = new TestDomain();
        private TestLogger logger;
        private string assemblyName;

        private TestFilter nunitFilter;
        private List<TestCase> loadedTestCases;
        private TestConverter testConverter;

        #region Constructors

        // This constructor is called by the others and is used directly for testing
        public AssemblyRunner(TestLogger logger, string assemblyName)
        {
            this.logger = logger;
            this.assemblyName = assemblyName;
            this.testConverter = new TestConverter(logger, assemblyName);
            this.loadedTestCases = new List<TestCase>();
            this.nunitFilter = TestFilter.Empty;
        }

        // This constructor is used when the executor is called with a list of test cases
        public AssemblyRunner(TestLogger logger, string assemblyName, IEnumerable<TestCase> selectedTestCases)
            : this(logger, assemblyName)
        {
            this.nunitFilter = MakeTestFilter(selectedTestCases);
        }

        // This constructor is used when the executor is called with a list of assemblies
        public AssemblyRunner(TestLogger logger, string assemblyName, TFSTestFilter tfsFilter)
            : this(logger, assemblyName)
        {
            if (tfsFilter.HasTfsFilterValue)
            {
                var filteredTestCases = tfsFilter.CheckFilter(this.LoadedTestCases);
                this.nunitFilter = MakeTestFilter(filteredTestCases);
            }
        }

        private static SimpleNameFilter MakeTestFilter(IEnumerable<TestCase> ptestCases)
        {
            var filter = new SimpleNameFilter();
            foreach (TestCase testCase in ptestCases)
            {
                filter.Add(testCase.FullyQualifiedName);
            }
            return filter;
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
            Debugger.Launch();
#endif
                if (TryLoadAssembly())
                {
                    var listener = new NUnitEventListener(testLog, this.TestConverter);

                    try
                    {
                        runner.Run(listener, NUnitFilter, true, LoggingThreshold.Off);
                    }
                    catch (NullReferenceException)
                    {
                        // this happens during the run when CancelRun is called.
                        logger.SendDebugMessage("Nullref caught");
                    }
                    finally
                    {
                        runner.Unload();
                    }
                }
                else
                {
                    logger.NUnitLoadError(assemblyName);
                }
            }
            catch (System.BadImageFormatException)
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
            if (this.runner != null && this.runner.Running)
                this.runner.CancelRun();
        }

        // Try to load the assembly and, if successful, populate
        // the list of all loaded assemblies. As a side effect
        // of calling TestConverter.ConvertTestCase, the converter's
        // cache of all test cases is populated as well. All
        // future calls to convert a test case may now use the cache.
        private bool TryLoadAssembly()
        {
            var package = new TestPackage(assemblyName);

            if (!runner.Load(package))
                return false;

            AddTestCases(runner.Test);

            return true;
        }

        // This method is public for testing purposes.
        // TODO: Test by actually loading an assembly and make it private
        public void AddTestCases(ITest test)
        {
            if (test.IsSuite)
                foreach (ITest child in test.Tests) this.AddTestCases(child);
            else
                this.LoadedTestCases.Add(this.TestConverter.ConvertTestCase(test));
        }

        #endregion

        #region IDisposable
        private bool _Disposed;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._Disposed)
            {
                if (disposing)
                {
                    if (this.TestConverter != null)
                        this.TestConverter.Dispose();
                }
            }
            this._Disposed = true;
        }

        ~AssemblyRunner()
        {
            this.Dispose(false);
        }
        #endregion
    }
}