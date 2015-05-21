// ****************************************************************
// Copyright (c) 2011-2015 NUnit Software. All rights reserved.
// ****************************************************************

using NUnit.Engine;
using System;

namespace NUnit.VisualStudio.TestAdapter
{
    public class EngineWrapper : MarshalByRefObject 
    {
      
        private readonly ITestEngine _engine = new TestEngine();

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

        public RunnerWrapper GetRunner(TestPackage package)
        {
            ITestRunner runner = _engine.GetRunner(package);
            return new RunnerWrapper(runner);
        }

        public void Initialize()
        {
            _engine.Initialize();
        }
    }
}
