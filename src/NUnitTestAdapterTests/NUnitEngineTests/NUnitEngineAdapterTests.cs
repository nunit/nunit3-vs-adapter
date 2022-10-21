using NSubstitute;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    [TestFixture]
    public class NUnitEngineAdapterTests
    {
        [Test]
        public void TestXmlFileNameGeneration()
        {
            var logger = Substitute.For<ITestLogger>();
            var settings = new AdapterSettings(logger);
            settings.Load(@"<RunSettings><NUnit><WorkDirectory>c:\whatever</WorkDirectory><TestOutputXml>/my/work/dir</TestOutputXml></NUnit></RunSettings>");
            var sut = new NUnitEngineAdapter();
            sut.InitializeSettingsAndLogging(settings, logger);
            string path = sut.GetXmlFilePath("c:/", "assembly", "xml");
            Assert.That(path, Is.EqualTo("c:/assembly.xml"));
        }

        [Test]
        public void TestXmlFileNameGenerationNewOutputXmlFileForEachRun()
        {
            var logger = Substitute.For<ITestLogger>();
            var settings = new AdapterSettings(logger);
            settings.Load(@"<RunSettings><NUnit><WorkDirectory>c:\whatever</WorkDirectory><TestOutputXml>/my/work/dir</TestOutputXml><NewOutputXmlFileForEachRun>true</NewOutputXmlFileForEachRun></NUnit></RunSettings>");
            var sut = new NUnitEngineAdapter();
            sut.InitializeSettingsAndLogging(settings, logger);
            string path = sut.GetXmlFilePath("c:/", "assembly", "xml");
            Assert.That(path, Is.EqualTo("c:/assembly.1.xml"));
        }
    }
}
