using System;
using System.Configuration;
using System.IO;
using NUnit.Framework;

namespace NUnitTestDemo
{
    [Should("Pass")]
    public class ConfigFileTests
    {
        [Test]
        public static void ProperConfigFileIsUsed()
        {
            var expectedPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "NUnit3TestDemo.dll.config");
            Assert.That(expectedPath, Is.EqualTo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile));
        }

        [Test]
        public static void CanReadConfigFile()
        {
            Assert.That(ConfigurationManager.AppSettings.Get("test.setting"), Is.EqualTo("54321"));
        }

    }
}
