// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using NUnit.Engine;
using System;
using System.Xml;

namespace NUnit.VisualStudio.TestAdapter
{
    [Serializable]
    internal class EngineWrapper : MarshalByRefObject, ITestEngine
    {
        [NonSerialized]
        private ITestEngine _engine = new TestEngine();

        public InternalTraceLevel InternalTraceLevel
        {
            get { return _engine.InternalTraceLevel; }
            set { _engine.InternalTraceLevel = value; }
        }

        public IServiceLocator Services
        {
            get { return _engine.Services; }
        }

        public string WorkDirectory
        {
            get { return _engine.WorkDirectory; }
            set { _engine.WorkDirectory = value; }
        }

        public void Dispose()
        {
            _engine.Dispose();
        }

        public ITestRunner GetRunner(TestPackage package)
        {
            ITestRunner runner = _engine.GetRunner(package);
            return new RunnerWrapper(runner);
        }

        public void Initialize()
        {
            _engine.Initialize();
        }
    }

    [Serializable]
    public class RunnerWrapper : MarshalByRefObject, ITestRunner
    {
        [NonSerialized]
        private ITestRunner _runner;

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

        public XmlNode Explore(TestFilter filter)
        {
            throw new NotImplementedException("Not used by the test adapter");
        }

        internal string ExploreInternal(TestFilter filter)
        {
            return _runner.Explore(filter).OuterXml;
        }

        public XmlNode Load()
        {
            throw new NotImplementedException("Not used by the test adapter");
        }

        internal string LoadInternal()
        {
            return _runner.Load().OuterXml;
        }

        public XmlNode Reload()
        {
            throw new NotImplementedException("Not used by the test adapter");
        }

        public XmlNode Run(ITestEventListener listener, TestFilter filter)
        {
            throw new NotImplementedException("Not used by the test adapter");
        }

        internal string RunInternal(ITestEventListener listener, TestFilter filter)
        {
            return _runner.Run(listener, filter).OuterXml;
        }

        public ITestRun RunAsync(ITestEventListener listener, TestFilter filter)
        {
            // The returned ITestRun won't be serializable, so don't use
            throw new NotImplementedException("Not used by the test adapter");
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

    internal static class XmlNodeExtensions
    {
        public static XmlNode ToXml(this string xml)
        {
            var doc = new XmlDocument();
            var fragment = doc.CreateDocumentFragment();
            fragment.InnerXml = xml;
            doc.AppendChild(fragment);
            return doc.FirstChild;
        }
    }
}
