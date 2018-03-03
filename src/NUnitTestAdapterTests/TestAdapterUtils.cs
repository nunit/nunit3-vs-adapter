// ***********************************************************************
// Copyright (c) 2018 Charlie Poole, Terje Sandstrom
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

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Engine.Services;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    internal static class TestAdapterUtils
    {
        public static ITestDiscoverer CreateDiscoverer()
        {
            var discoverer = new NUnit3TestDiscoverer();
            InitializeForTesting(discoverer);
            return discoverer;
        }

        public static ITestExecutor CreateExecutor()
        {
            var executor = new NUnit3TestExecutor();
            InitializeForTesting(executor);
            return executor;
        }

        private static void InitializeForTesting(NUnitTestAdapter adapter)
        {
#if NET46
            adapter.InternalEngineCreated += engine =>
            {
                engine.Services.Add(new SettingsService(true));
                engine.Services.Add(new DomainManager());
                engine.Services.Add(new ExtensionService());
                engine.Services.Add(new DriverService());
                engine.Services.Add(new RecentFilesService());
                engine.Services.Add(new ProjectService());
                engine.Services.Add(new RuntimeFrameworkService());
                engine.Services.Add(new DefaultTestRunnerFactory());
                engine.Services.Add(new TestAgency("TestAgency for " + TestContext.CurrentContext.Test.Name, 0));
                engine.Services.Add(new ResultService());
                engine.Services.Add(new TestFilterService());
            };
#endif
        }
    }
}
