using System;
using System.Xml;
using NUnit.Engine;

namespace NUnit.VisualStudio.TestAdapter
{
    public class RunnerWrapper : MarshalByRefObject // , ITestRunner
    {
      
        private readonly ITestRunner _runner;

        public RunnerWrapper(ITestRunner runner)
        {
            _runner = runner;
        }

        public bool IsTestRunning
        {
            get { return _runner.IsTestRunning; }
        }

        public int CountTestCases(TestFilter filter)
        {
            return _runner.CountTestCases(filter);
        }

        public void Dispose()
        {
            _runner.Dispose();
        }

       

        internal string ExploreInternal(TestFilter filter)
        {
            return _runner.Explore(filter).OuterXml;
        }

        
        internal string LoadInternal()
        {
            return _runner.Load().OuterXml;
        }

       

        internal string RunInternal(ITestEventListener listener, TestFilter filter)
        {
            return _runner.Run(listener, filter).OuterXml;
        }

        

        public void StopRun(bool force)
        {
            _runner.StopRun(force);
        }
        
        public void Unload()
        {
            _runner.Unload();
        }
    }
}