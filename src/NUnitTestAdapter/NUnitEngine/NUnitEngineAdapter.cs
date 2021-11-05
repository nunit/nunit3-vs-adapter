// ***********************************************************************
// Copyright (c) 2020-2021 Charlie Poole, Terje Sandstrom
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
using System.IO;
using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Internal;

namespace NUnit.VisualStudio.TestAdapter.NUnitEngine
{
    public interface INUnitEngineAdapter
    {
        NUnitResults Explore();
        void CloseRunner();
        NUnitResults Explore(TestFilter filter);
        NUnitResults Run(ITestEventListener listener, TestFilter filter);
        void StopRun();

        T GetService<T>()
            where T : class;

        void GenerateTestOutput(NUnitResults testResults, string assemblyPath, string testOutputXmlFolder);
        string GetXmlFilePath(string folder, string defaultFileName, string extension);
    }

    public class NUnitEngineAdapter : INUnitEngineAdapter, IDisposable
    {
        private IAdapterSettings settings;
        private ITestLogger logger;
        private TestPackage package;
        private ITestEngine TestEngine { get; set; }
        private ITestRunner Runner { get; set; }

        internal event Action<ITestEngine> InternalEngineCreated;

        public bool EngineEnabled => TestEngine != null;

        public void Initialize()
        {
#if NET35
            var engineX = new TestEngine();
#else
            var engineX = TestEngineActivator.CreateInstance();
#endif
            InternalEngineCreated?.Invoke(engineX);
            TestEngine = engineX;
        }

        public void InitializeSettingsAndLogging(IAdapterSettings setting, ITestLogger testLog)
        {
            logger = testLog;
            settings = setting;
        }

        public void CreateRunner(TestPackage testPackage)
        {
            package = testPackage;
            Runner = TestEngine.GetRunner(package);
        }

        public NUnitResults Explore()
        {
            return Explore(TestFilter.Empty);
        }

        public NUnitResults Explore(TestFilter filter)
        {
            var timing = new TimingLogger(settings, logger);
            var results = new NUnitResults(Runner.Explore(filter));
            timing.LogTime("Execution engine discovery time ");
            return results;
        }

        public NUnitResults Run(ITestEventListener listener, TestFilter filter)
        {
            var timing = new TimingLogger(settings, logger);
            var results = new NUnitResults(Runner.Run(listener, filter));
            timing.LogTime("Execution engine run time ");
            return results;
        }

        public T GetService<T>()
            where T : class
        {
            var service = TestEngine.Services.GetService<T>();
            if (service == null)
            {
                logger.Warning($"Engine GetService can't create service {typeof(T)}.");
            }
            return service;
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

            try
            {
                Runner.Unload();
                Runner.Dispose();
            }
            catch (NUnitEngineUnloadException ex)
            {
                logger.Warning($"Engine encountered NUnitEngineUnloadException :  {ex.Message}");
            }
            Runner = null;
        }

        public void Dispose()
        {
            CloseRunner();
            TestEngine?.Dispose();
        }

        public void GenerateTestOutput(NUnitResults testResults, string assemblyPath, string testOutputXmlFolder)
        {
            if (!settings.UseTestOutputXml)
                return;

            string path = GetXmlFilePath(testOutputXmlFolder, Path.GetFileNameWithoutExtension(assemblyPath), "xml");
            var resultService = GetService<IResultService>();

            // Following null argument should work for nunit3 format. Empty array is OK as well.
            // If you decide to handle other formats in the runsettings, it needs more work.
            var resultWriter = resultService.GetResultWriter("nunit3", null);
            resultWriter.WriteResultFile(testResults.FullTopNode, path);
            logger.Info($"   Test results written to {path}");
        }

        public string GetXmlFilePath(string folder, string defaultFileName, string extension)
        {
            if (!settings.NewOutputXmlFileForEachRun)
            {
                // overwrite the existing file
                return Path.Combine(folder, $"{defaultFileName}.{extension}");
            }
            // allways create a new file
            int i = 1;
            while (true)
            {
                string path = Path.Combine(folder, $"{defaultFileName}.{i++}.{extension}");
                if (!File.Exists(path))
                    return path;
            }
        }
    }
}
