using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class AdapterSettingsTests
    {
        private AdapterSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new AdapterSettings();
        }

        [Test]
        public void NullContextThrowsException()
        {
            Assert.That(() => _settings.Load((IDiscoveryContext)null), Throws.ArgumentNullException);
        }

        [Test]
        public void NullStringThrowsException()
        {
            Assert.That(() => _settings.Load((string)null), Throws.ArgumentNullException);
        }

        [Test]
        public void EmptyStringThrowsException()
        {
            Assert.That(() => _settings.Load(string.Empty), Throws.ArgumentException);
        }

        [Test]
        public void DefaultSettings()
        {
            _settings.Load("<RunSettings/>");
            Assert.Null(_settings.MaxCpuCount);
            Assert.Null(_settings.ResultsDirectory);
            Assert.Null(_settings.TargetFrameworkVersion);
            Assert.Null(_settings.TargetPlatform);
            Assert.Null(_settings.TestAdapterPaths);
            Assert.IsEmpty(_settings.TestProperties);
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
            Assert.That(_settings.MaxCpuCount, Is.EqualTo("42"));
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

        public void TestRunParameterSettings()
        {
            _settings.Load("<RunSettings><TestRunParameters><Parameter name='Answer' value=42/><Parameter name='Question' value='Why?'/></TestRunParameters></RunSettings>");
            Assert.That(_settings.TestProperties.Count, Is.EqualTo(2));
            Assert.That(_settings.TestProperties["Answer"], Is.EqualTo("42"));
            Assert.That(_settings.TestProperties["Question"], Is.EqualTo("Why?"));
        }
    }
}
