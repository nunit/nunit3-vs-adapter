// ***********************************************************************
// Copyright (c) 2011-2020 Charlie Poole, Terje Sandstrom
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

using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.Tests.Fakes;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class AdapterSettingsTests
    {
        private IAdapterSettings _settings;

        [SetUp]
        public void SetUp()
        {
            var testLogger = new TestLogger(new MessageLoggerStub());
            _settings = new AdapterSettings(testLogger);
            testLogger.InitSettings(_settings);
        }

        [Test]
        public void NullContextThrowsException()
        {
            Assert.That(() => _settings.Load((IDiscoveryContext)null), Throws.ArgumentNullException);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("<RunSettings />")]
        public void DefaultSettings(string xml)
        {
            _settings.Load(xml);
            Assert.Multiple(() =>
            {
                Assert.That(_settings.MaxCpuCount, Is.EqualTo(-1));
                Assert.Null(_settings.ResultsDirectory);
                Assert.Null(_settings.TargetFrameworkVersion);
                Assert.Null(_settings.TargetPlatform);
                Assert.Null(_settings.TestAdapterPaths);
                Assert.IsTrue(_settings.CollectSourceInformation);
                Assert.IsEmpty(_settings.TestProperties);
                Assert.Null(_settings.InternalTraceLevel);
                Assert.Null(_settings.WorkDirectory);
                Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(-1));
                Assert.That(_settings.DefaultTimeout, Is.EqualTo(0));
                Assert.That(_settings.Verbosity, Is.EqualTo(0));
                Assert.False(_settings.ShadowCopyFiles);
                Assert.False(_settings.UseVsKeepEngineRunning);
                Assert.Null(_settings.BasePath);
                Assert.Null(_settings.PrivateBinPath);
                Assert.NotNull(_settings.RandomSeed);
                Assert.False(_settings.SynchronousEvents);
                Assert.Null(_settings.DomainUsage);
                Assert.False(_settings.InProcDataCollectorsAvailable);
                Assert.IsFalse(_settings.DisableAppDomain);
                Assert.IsFalse(_settings.DisableParallelization);
                Assert.IsFalse(_settings.DesignMode);
                Assert.False(_settings.UseTestOutputXml);
            });
        }

        [Test]
        public void ResultsDirectorySetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><ResultsDirectory>./myresults</ResultsDirectory></RunConfiguration></RunSettings>");
            Assert.That(_settings.ResultsDirectory, Is.EqualTo("./myresults"));
        }

        [Test]
        public void MaxCpuCountSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><MaxCpuCount>42</MaxCpuCount></RunConfiguration></RunSettings>");
            Assert.That(_settings.MaxCpuCount, Is.EqualTo(42));
        }

        [Test]
        public void TargetFrameworkVersionSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><TargetFrameworkVersion>Framework45</TargetFrameworkVersion></RunConfiguration></RunSettings>");
            Assert.That(_settings.TargetFrameworkVersion, Is.EqualTo("Framework45"));
        }

        [Test]
        public void TargetPlatformSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>");
            Assert.That(_settings.TargetPlatform, Is.EqualTo("x86"));
        }

        [Test]
        public void TestAdapterPathsSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><TestAdapterPaths>/first/path;/second/path</TestAdapterPaths></RunConfiguration></RunSettings>");
            Assert.That(_settings.TestAdapterPaths, Is.EqualTo("/first/path;/second/path"));
        }

        [Test]
        public void CollectSourceInformationSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><CollectSourceInformation>False</CollectSourceInformation></RunConfiguration></RunSettings>");
            Assert.That(_settings.CollectSourceInformation, Is.EqualTo(false));
        }

        [Test]
        public void DisableAppDomainSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><DisableAppDomain>true</DisableAppDomain></RunConfiguration></RunSettings>");
            Assert.That(_settings.DisableAppDomain, Is.EqualTo(true));
        }

        [Test]
        public void DisableParallelizationSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><DisableParallelization>true</DisableParallelization></RunConfiguration></RunSettings>");
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(0));
        }

        [Test]
        public void UpdateNumberOfTestWorkersWhenConflictingSettings()
        {
            _settings.Load("<RunSettings><RunConfiguration><DisableParallelization>true</DisableParallelization></RunConfiguration><NUnit><NumberOfTestWorkers>12</NumberOfTestWorkers></NUnit></RunSettings>");

            // When there's a conflicting values in DisableParallelization and NumberOfTestWorkers. Override the NumberOfTestWorkers.
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(0));

            // Do not override the NumberOfTestWorkers when DisableParallelization is False
            _settings.Load("<RunSettings><RunConfiguration><DisableParallelization>false</DisableParallelization></RunConfiguration><NUnit><NumberOfTestWorkers>0</NumberOfTestWorkers></NUnit></RunSettings>");
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(0));

            // Do not override the NumberOfTestWorkers when DisableParallelization is not defined
            _settings.Load("<RunSettings><RunConfiguration></RunConfiguration><NUnit><NumberOfTestWorkers>12</NumberOfTestWorkers></NUnit></RunSettings>");
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(12));
        }

        [Test]
        public void DesignModeSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><DesignMode>true</DesignMode></RunConfiguration></RunSettings>");
            Assert.That(_settings.DesignMode, Is.EqualTo(true));
        }

        [Test]
        public void TestRunParameterSettings()
        {
            _settings.Load("<RunSettings><TestRunParameters><Parameter name='Answer' value='42'/><Parameter name='Question' value='Why?'/></TestRunParameters></RunSettings>");
            Assert.That(_settings.TestProperties.Count, Is.EqualTo(2));
            Assert.That(_settings.TestProperties["Answer"], Is.EqualTo("42"));
            Assert.That(_settings.TestProperties["Question"], Is.EqualTo("Why?"));
        }

        [Test]
        public void InternalTraceLevel()
        {
            _settings.Load("<RunSettings><NUnit><InternalTraceLevel>Debug</InternalTraceLevel></NUnit></RunSettings>");
            Assert.That(_settings.InternalTraceLevel, Is.EqualTo("Debug"));
        }

        [Test]
        public void WorkDirectorySetting()
        {
            _settings.Load("<RunSettings><NUnit><WorkDirectory>/my/work/dir</WorkDirectory></NUnit></RunSettings>");
            Assert.That(_settings.WorkDirectory, Is.EqualTo("/my/work/dir"));
        }

        /// <summary>
        /// Workdir not set, TestOutput is relative.
        /// </summary>
        [Test]
        public void TestOutputSetting()
        {
            _settings.Load("<RunSettings><NUnit><TestOutputXml>/my/work/dir</TestOutputXml></NUnit></RunSettings>");
            Assert.That(_settings.UseTestOutputXml);
            Assert.Multiple(() =>
            {
                Assert.That(_settings.TestOutputXml, Does.Contain(@"/my/work/dir"));
            });
        }

        /// <summary>
        /// Workdir set, and is absolute,  TestOutputXml is relative.
        /// </summary>
        [Ignore("Is not handled in the test executor, not in the test settings")]
        [Test]
        public void TestOutputSettingWithWorkDir()
        {
            _settings.Load(@"<RunSettings><NUnit><WorkDirectory>C:\Whatever</WorkDirectory><TestOutputXml>my/testoutput/dir</TestOutputXml></NUnit></RunSettings>");
            Assert.That(_settings.UseTestOutputXml, "Settings not loaded properly");
            Assert.Multiple(() =>
            {
                Assert.That(_settings.TestOutputXml, Does.Contain(@"\my/testoutput/dir"), "Content not correct");
                Assert.That(_settings.TestOutputXml, Does.StartWith(@"C:\"), "Not correct start drive");
                Assert.That(Path.IsPathRooted(_settings.TestOutputXml), Is.True, "Path not properly rooted");
            });
        }


        [Test]
        public void NumberOfTestWorkersSetting()
        {
            _settings.Load("<RunSettings><NUnit><NumberOfTestWorkers>12</NumberOfTestWorkers></NUnit></RunSettings>");
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(12));
        }

        [Test]
        public void DefaultTimeoutSetting()
        {
            _settings.Load("<RunSettings><NUnit><DefaultTimeout>5000</DefaultTimeout></NUnit></RunSettings>");
            Assert.That(_settings.DefaultTimeout, Is.EqualTo(5000));
        }

        [Test]
        public void ShadowCopySetting()
        {
            _settings.Load("<RunSettings><NUnit><ShadowCopyFiles>true</ShadowCopyFiles></NUnit></RunSettings>");
            Assert.True(_settings.ShadowCopyFiles);
        }


        [Test]
        public void ShadowCopySettingDefault()
        {
            _settings.Load("");
            Assert.That(_settings.ShadowCopyFiles, Is.False);
        }

        [Test]
        public void VerbositySetting()
        {
            _settings.Load("<RunSettings><NUnit><Verbosity>1</Verbosity></NUnit></RunSettings>");
            Assert.That(_settings.Verbosity, Is.EqualTo(1));
        }

        [Test]
        public void UseVsKeepEngineRunningSetting()
        {
            _settings.Load("<RunSettings><NUnit><UseVsKeepEngineRunning>true</UseVsKeepEngineRunning></NUnit></RunSettings>");
            Assert.True(_settings.UseVsKeepEngineRunning);
        }

        [Test]
        public void PreFilterIsDefaultOff()
        {
            Assert.That(_settings.PreFilter, Is.False);
        }

        [Test]
        public void PreFilterCanBeSet()
        {
            _settings.Load("<RunSettings><NUnit><PreFilter>true</PreFilter></NUnit></RunSettings>");
            Assert.That(_settings.PreFilter);
        }


        [Test]
        public void BasePathSetting()
        {
            _settings.Load("<RunSettings><NUnit><BasePath>..</BasePath></NUnit></RunSettings>");
            Assert.That(_settings.BasePath, Is.EqualTo(".."));
        }


        [Test]
        public void VsTestCategoryTypeSetting()
        {
            _settings.Load("<RunSettings><NUnit><VsTestCategoryType>mstest</VsTestCategoryType></NUnit></RunSettings>");
            Assert.That(_settings.VsTestCategoryType, Is.EqualTo(VsTestCategoryType.MsTest));
        }

        [Test]
        public void VsTestCategoryTypeSettingWithGarbage()
        {
            _settings.Load("<RunSettings><NUnit><VsTestCategoryType>garbage</VsTestCategoryType></NUnit></RunSettings>");
            Assert.That(_settings.VsTestCategoryType, Is.EqualTo(VsTestCategoryType.NUnit));
        }


        [Test]
        public void PrivateBinPathSetting()
        {
            _settings.Load("<RunSettings><NUnit><PrivateBinPath>dir1;dir2</PrivateBinPath></NUnit></RunSettings>");
            Assert.That(_settings.PrivateBinPath, Is.EqualTo("dir1;dir2"));
        }

        [Test]
        public void RandomSeedSetting()
        {
            _settings.Load("<RunSettings><NUnit><RandomSeed>12345</RandomSeed></NUnit></RunSettings>");
            Assert.That(_settings.RandomSeed, Is.EqualTo(12345));
        }

        [Test]
        public void DefaultTestNamePattern()
        {
            _settings.Load("<RunSettings><NUnit><DefaultTestNamePattern>{m}{a:1000}</DefaultTestNamePattern></NUnit></RunSettings>");
            Assert.That(_settings.DefaultTestNamePattern, Is.EqualTo("{m}{a:1000}"));
        }

        [Test]
        public void CollectDataForEachTestSeparately()
        {
            _settings.Load(@"
<RunSettings>
    <RunConfiguration>
        <CollectDataForEachTestSeparately>true</CollectDataForEachTestSeparately>
    </RunConfiguration>
    <InProcDataCollectionRunSettings>
        <InProcDataCollectors>
            <InProcDataCollector friendlyName='DummyCollectorName' uri='InProcDataCollector://NUnit/DummyCollectorName' />
        </InProcDataCollectors>
    </InProcDataCollectionRunSettings>
</RunSettings>");

            Assert.Null(_settings.DomainUsage);
            Assert.That(_settings.SynchronousEvents);
            Assert.That(_settings.NumberOfTestWorkers, Is.Zero);
            Assert.That(_settings.InProcDataCollectorsAvailable);
        }

        [Test]
        public void InProcDataCollector()
        {
            _settings.Load(@"
<RunSettings>
    <InProcDataCollectionRunSettings>
        <InProcDataCollectors>
            <InProcDataCollector friendlyName='DummyCollectorName' uri='InProcDataCollector://NUnit/DummyCollectorName' />
        </InProcDataCollectors>
    </InProcDataCollectionRunSettings>
</RunSettings>");

            Assert.Null(_settings.DomainUsage);
            Assert.False(_settings.SynchronousEvents);
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(-1));
            Assert.True(_settings.InProcDataCollectorsAvailable);
        }

        [Test]
        public void LiveUnitTestingDataCollector()
        {
            _settings.Load(@"
<RunSettings>
    <InProcDataCollectionRunSettings>
        <InProcDataCollectors>
            <InProcDataCollector friendlyName='DummyCollectorName' uri='InProcDataCollector://Microsoft/LiveUnitTesting/1.0' />
        </InProcDataCollectors>
    </InProcDataCollectionRunSettings>
</RunSettings>");

            Assert.Null(_settings.DomainUsage);
            Assert.True(_settings.SynchronousEvents);
            Assert.That(_settings.NumberOfTestWorkers, Is.Zero);
            Assert.True(_settings.InProcDataCollectorsAvailable);
        }

        [Test]
        public void WhereCanBeSet()
        {
            _settings.Load("<RunSettings><NUnit><Where>cat == SomeCategory and namespace == SomeNamespace or cat != SomeOtherCategory</Where></NUnit></RunSettings>");
            Assert.That(_settings.Where, Is.EqualTo("cat == SomeCategory and namespace == SomeNamespace or cat != SomeOtherCategory"));
        }

        [TestCase("None", TestOutcome.None)]
        [TestCase("Passed", TestOutcome.Passed)]
        [TestCase("Failed", TestOutcome.Failed)]
        [TestCase("Skipped", TestOutcome.Skipped)]
        public void MapWarningToTests(string setting, TestOutcome outcome)
        {
            var runsettings = $"<RunSettings><NUnit><MapWarningTo>{setting}</MapWarningTo></NUnit></RunSettings>";
            _settings.Load(runsettings);
            Assert.That(_settings.MapWarningTo, Is.EqualTo(outcome));
        }

        [TestCase("garbage")]
        public void MapWarningToTestsFailing(string setting)
        {
            var runsettings = $"<RunSettings><NUnit><MapWarningTo>{setting}</MapWarningTo></NUnit></RunSettings>";
            _settings.Load(runsettings);
            Assert.That(_settings.MapWarningTo, Is.EqualTo(TestOutcome.Skipped));
        }

        [Test]
        public void MapWarningToTestsDefault()
        {
            _settings.Load("");
        }

        [TestCase("Name", DisplayNameOptions.Name)]
        [TestCase("Fullname", DisplayNameOptions.FullName)]
        [TestCase("FullnameSep", DisplayNameOptions.FullNameSep)]
        [TestCase("invalid", DisplayNameOptions.Name)]
        public void MapDisplayNameTests(string setting, DisplayNameOptions outcome)
        {
            var runsettings = $"<RunSettings><NUnit><DisplayName>{setting}</DisplayName></NUnit></RunSettings>";
            _settings.Load(runsettings);
            Assert.That(_settings.DisplayName, Is.EqualTo(outcome));
        }

        [TestCase(":")]
        [TestCase("-")]
        [TestCase(".")]
        public void FullNameSeparatorTest(string setting)
        {
            var expected = setting[0];
            var runsettings = $"<RunSettings><NUnit><FullnameSeparator>{setting}</FullnameSeparator></NUnit></RunSettings>";
            _settings.Load(runsettings);
            Assert.That(_settings.FullnameSeparator, Is.EqualTo(expected));
        }

        [Test]
        public void TestDefaults()
        {
            _settings.Load("");
            Assert.Multiple(() =>
            {
                Assert.That(_settings.FullnameSeparator, Is.EqualTo(':'));
                Assert.That(_settings.DisplayName, Is.EqualTo(DisplayNameOptions.Name));
                Assert.That(_settings.MapWarningTo, Is.EqualTo(TestOutcome.Skipped));
            });
        }
    }
}
