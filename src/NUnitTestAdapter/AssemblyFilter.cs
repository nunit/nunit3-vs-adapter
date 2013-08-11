namespace NUnit.VisualStudio.TestAdapter
{
    using System;
    using System.Collections.Generic;

    using Microsoft.VisualStudio.TestPlatform.ObjectModel;

    using NUnit.Core;
    using NUnit.Core.Filters;

    /// <summary>
    /// This class containts the filters and maps necessary for filtering of test cases for a given assembly
    /// </summary>
    public class AssemblyFilter : IDisposable
    {
        public TestFilter NUnitFilter { get; protected set; }

        public IList<TestCase> VsTestCases
        {
            get
            {
                return this.vsTestCases;
            }
        }

        public Dictionary<string, TestNode> NUnitTestCaseMap
        {
            get
            {
                return this.nunitTestCaseMap;
            }
        }

        public string AssemblyName { get; private set; }

        // List of test cases used during execution
        private readonly List<TestCase> vsTestCases;

        // Map of names to NUnit Test cases
        private readonly Dictionary<string, NUnit.Core.TestNode> nunitTestCaseMap;

        private TestConverter testConverter;

        public TestConverter TestConverter
        {
            get
            {
                return this.testConverter
                       ?? (this.testConverter =
                           new TestConverter(this.AssemblyName, new Dictionary<string, TestNode>()));
            }
        }

        //private readonly Dictionary<string, TestNode> testDictionary;

        public AssemblyFilter(string assemblyName)
        {
            this.AssemblyName = assemblyName;
            this.NUnitFilter = TestFilter.Empty;
            this.vsTestCases = new List<TestCase>();
            this.nunitTestCaseMap = new Dictionary<string, NUnit.Core.TestNode>();
            //this.testDictionary = new Dictionary<string, TestNode>();
        }

        public AssemblyFilter(string assemblyName, TestFilter filter)
            : this(assemblyName)
        {
            this.NUnitFilter = filter;
        }
        
        public void AddTestCases(TestNode test)
        {
            if (test.IsSuite)
                foreach (TestNode child in test.Tests) this.AddTestCases(child);
            else
            {
                this.vsTestCases.Add(this.TestConverter.ConvertTestCase(test));
                this.nunitTestCaseMap.Add(test.TestName.UniqueName, test);
            }
        }

        internal virtual void ProcessTfsFilter()
        {

        }

        public static AssemblyFilter Create(string assemblyName, IEnumerable<TestCase> ptestCases)
        {
            var filter = new SimpleNameFilter();
            foreach (TestCase testCase in ptestCases)
            {
                filter.Add(testCase.FullyQualifiedName);
            }
            return new AssemblyFilter(assemblyName, filter);
        }



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
                    if (this.testConverter != null)
                        this.testConverter.Dispose();
                }
            }
            this._Disposed = true;
        }

        ~AssemblyFilter()
        {
            this.Dispose(false);
        }
        #endregion


    }
}