// ***********************************************************************
// Copyright (c) 2011-2021 Charlie Poole, Terje Sandstrom
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using NUnit.Engine;
using NUnit.VisualStudio.TestAdapter.Dump;
using NUnit.VisualStudio.TestAdapter.Internal;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter
{
#if !NET462
    [FileExtension(".appx")]
#endif
    [FileExtension(".dll")]
    [FileExtension(".exe")]
    [DefaultExecutorUri(NUnit3TestExecutor.ExecutorUri)]
    [Category("managed")]
    public sealed class NUnit3TestDiscoverer : NUnitTestAdapter, ITestDiscoverer
    {
        private DumpXml dumpXml;

        #region ITestDiscoverer Members

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger messageLogger, ITestCaseDiscoverySink discoverySink)
        {
            Initialize(discoveryContext, messageLogger);
            CheckIfDebug();
            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test discovery starting");

            // Ensure any channels registered by other adapters are unregistered
            CleanUpRegisteredChannels();

            if (Settings.InProcDataCollectorsAvailable && sources.Count() > 1)
            {
                TestLog.Error("Unexpected to discover tests in multiple assemblies when InProcDataCollectors specified in run configuration.");
                Unload();
                return;
            }

            foreach (string sourceAssembly in sources)
            {
                string sourceAssemblyPath = Path.IsPathRooted(sourceAssembly) ? sourceAssembly : Path.Combine(Directory.GetCurrentDirectory(), sourceAssembly);
                TestLog.Debug("Processing " + sourceAssembly);
                if (Settings.DumpXmlTestDiscovery)
                {
                    dumpXml = new DumpXml(sourceAssemblyPath);
                }

                try
                {
                    var package = CreateTestPackage(sourceAssemblyPath, null);
                    NUnitEngineAdapter.CreateRunner(package);
                    var results = NUnitEngineAdapter.Explore();
                    dumpXml?.AddString(results.AsString());

                    if (results.IsRunnable)
                    {
                        int cases;
                        using (var testConverter = new TestConverterForXml(TestLog, sourceAssemblyPath, Settings))
                        {
                            var timing = new TimingLogger(Settings, TestLog);
                            cases = ProcessTestCases(results, discoverySink, testConverter);
                            timing.LogTime("Discovery/Processing/Converting:");
                        }

                        TestLog.Debug($"Discovered {cases} test cases");
                        // Only save if seed is not specified in runsettings
                        // This allows workaround in case there is no valid
                        // location in which the seed may be saved.
                        if (cases > 0 && !Settings.RandomSeedSpecified)
                            Settings.SaveRandomSeed(Path.GetDirectoryName(sourceAssemblyPath));
                    }
                    else
                    {
                        if (results.HasNoNUnitTests)
                        {
                            if (Settings.Verbosity > 0)
                                TestLog.Info("Assembly contains no NUnit 3.0 tests: " + sourceAssembly);
                        }
                        else
                        {
                            TestLog.Info("NUnit failed to load " + sourceAssembly);
                        }
                    }
                }
                catch (NUnitEngineException e)
                {
                    if (e.InnerException is BadImageFormatException)
                    {
                        // we skip the native c++ binaries that we don't support.
                        TestLog.Warning("Assembly not supported: " + sourceAssembly);
                    }
                    else
                    {
                        TestLog.Warning("Exception thrown discovering tests in " + sourceAssembly, e);
                    }
                }
                catch (BadImageFormatException)
                {
                    // we skip the native c++ binaries that we don't support.
                    TestLog.Warning("Assembly not supported: " + sourceAssembly);
                }
                catch (FileNotFoundException ex)
                {
                    // Either the NUnit framework was not referenced by the test assembly
                    // or some other error occurred. Not a problem if not an NUnit assembly.
                    TestLog.Warning("Dependent Assembly " + ex.FileName + " of " + sourceAssembly + " not found. Can be ignored if not an NUnit project.");
                }
                catch (FileLoadException ex)
                {
                    // Attempts to load an invalid assembly, or an assembly with missing dependencies
                    TestLog.Warning("Assembly " + ex.FileName + " loaded through " + sourceAssembly + " failed. Assembly is ignored. Correct deployment of dependencies if this is an error.");
                }
                catch (TypeLoadException ex)
                {
                    if (ex.TypeName == "NUnit.Framework.Api.FrameworkController")
                        TestLog.Warning("   Skipping NUnit 2.x test assembly");
                    else
                        TestLog.Warning("Exception thrown discovering tests in " + sourceAssembly, ex);
                }
                catch (Exception ex)
                {
                    TestLog.Warning("Exception thrown discovering tests in " + sourceAssembly, ex);
                }
                finally
                {
                    dumpXml?.DumpForDiscovery();
                    NUnitEngineAdapter?.CloseRunner();
                }
            }

            TestLog.Info($"NUnit Adapter {AdapterVersion}: Test discovery complete");

            Unload();
        }

        #endregion

        #region Helper Methods

        private int ProcessTestCases(NUnitResults results, ITestCaseDiscoverySink discoverySink, TestConverterForXml testConverterForXml)
        {
            int cases = 0;
            foreach (XmlNode testNode in results.TestCases())
            {
                try
                {
                    var testCase = testConverterForXml.ConvertTestCase(new NUnitEventTestCase(testNode));
                    discoverySink.SendTestCase(testCase);
                    cases += 1;
                }
                catch (Exception ex)
                {
                    TestLog.Warning("Exception converting " + testNode.GetAttribute("fullname"), ex);
                }
            }

            return cases;
        }

        private void CheckIfDebug()
        {
            if (!Settings.DebugDiscovery)
                return;
            if (!Debugger.IsAttached)
                Debugger.Launch();
        }
        #endregion
    }
}
