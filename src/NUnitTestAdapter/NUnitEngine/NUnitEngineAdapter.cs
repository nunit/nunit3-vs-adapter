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
        void Close();
        NUnitResults Explore(TestFilter filter);
        NUnitResults Run(ITestEventListener listener, TestFilter filter);
        void StopRun();
    }

    public class NUnitEngineAdapter : INUnitEngineAdapter, IDisposable
    {
        private readonly IAdapterSettings settings;
        private readonly ITestLogger logger;
        private readonly TestPackage package;
        private TestEngineClass TestEngine { get; }
        private readonly ITestRunner runner;

        internal event Action<TestEngineClass> InternalEngineCreated;

        private NUnitEngineAdapter(IAdapterSettings settings, ITestLogger logger)
        {
            this.logger = logger;
            this.settings = settings;
        }

        public NUnitEngineAdapter(TestPackage package, IAdapterSettings settings, ITestLogger testLog) : this(settings, testLog)
        {
            this.package = package;
            var engine = new TestEngineClass();
            InternalEngineCreated?.Invoke(engine);
            TestEngine = engine;
            runner = TestEngine.GetRunner(package);
        }

        public NUnitResults Explore()
        {
            return new NUnitResults(runner.Explore(TestFilter.Empty));
        }

        public NUnitResults Explore(TestFilter filter)
        {
            return new NUnitResults(runner.Explore(filter));
        }

        public NUnitResults Run(ITestEventListener listener, TestFilter filter)
        {
            return new NUnitResults(runner.Run(listener, filter));
        }

        public void StopRun()
        {
            runner?.StopRun(true);
        }

        public void Close()
        {
            if (runner == null)
                return;
            if (runner.IsTestRunning)
                runner.StopRun(true);

            runner.Unload();
            runner.Dispose();
        }

        public void Dispose()
        {
            Close();
            TestEngine?.Dispose();
        }
    }
}
