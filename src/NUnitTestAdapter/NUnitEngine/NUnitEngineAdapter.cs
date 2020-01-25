// ***********************************************************************
// Copyright (c) 2020-2020 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using NUnit.Engine;
// We use an alias so that we don't accidentally make
// references to engine internals, except for creating
// the engine object in the Initialize method.
using TestEngineClass = NUnit.Engine.TestEngine;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitEngineAdapter
    {
        NUnitResults Explore();
        void CloseRunner();
        NUnitResults Explore(TestFilter filter);
        NUnitResults Run(ITestEventListener listener, TestFilter filter);
        void StopRun();
    }

    public class NUnitEngineAdapter : INUnitEngineAdapter, IDisposable
    {
        private IAdapterSettings settings;
        private ITestLogger logger;
        private TestPackage package;
        private TestEngineClass TestEngine { get; }
        private ITestRunner Runner { get; set; }

        internal event Action<TestEngineClass> InternalEngineCreated;

        public NUnitEngineAdapter()
        {
            var engine = new TestEngineClass();
            InternalEngineCreated?.Invoke(engine);
            TestEngine = engine;
        }

        public void InitializeSettingsAndLogging(IAdapterSettings setting, ITestLogger testLog)
        {
            logger = testLog;
            settings = setting;
        }

        public void CreateRunner(TestPackage testPackage)
        {
            this.package = testPackage;
            Runner = TestEngine.GetRunner(package);
        }

        public NUnitResults Explore()
        {
            return new NUnitResults(Runner.Explore(TestFilter.Empty));
        }

        public NUnitResults Explore(TestFilter filter)
        {
            return new NUnitResults(Runner.Explore(filter));
        }

        public NUnitResults Run(ITestEventListener listener, TestFilter filter)
        {
            return new NUnitResults(Runner.Run(listener, filter));
        }

        public T GetService<T>()
            where T : class
        {
            return TestEngine.Services.GetService<T>();
        }

        public void StopRun()
        {
            Runner?.StopRun(true);
        }

        public void CloseRunner()
        {
            if (Runner == null)
                return;
            if (Runner.IsTestRunning)
                Runner.StopRun(true);

            Runner.Unload();
            Runner.Dispose();
        }

        public void Dispose()
        {
            CloseRunner();
            TestEngine?.Dispose();
        }
    }
}
