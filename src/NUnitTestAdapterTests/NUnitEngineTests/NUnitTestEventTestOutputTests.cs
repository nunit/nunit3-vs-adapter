using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.NUnitEngine;

namespace NUnit.VisualStudio.TestAdapter.Tests.NUnitEngineTests
{
    public class NUnitTestEventTestOutputTests
    {
        private const string OutputProgress =
            @"<test-output stream='Progress' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever]]></test-output>";

        private const string OutputOut =
            @"<test-output stream='Out' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever]]></test-output>";

        private const string OutputError =
            @"<test-output stream='Error' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[Whatever]]></test-output>";

        private const string BlankTestOutput =
            @"<test-output stream='Progress' testid='0-1001' testname='Something.TestClass.Whatever'><![CDATA[   ]]></test-output>";



        [Test]
        public void NUnitTestEventTestOutputTestWithProgress()
        {
            var sut = new NUnitTestEventTestOutput(XmlHelper.CreateXmlNode(OutputProgress));
            Assert.Multiple(() =>
            {
                Assert.That(sut.IsProgressStream);
                Assert.That(sut.IsErrorStream, Is.False);
                Assert.That(sut.IsNullOrEmptyStream, Is.False);
                Assert.That(sut.Stream, Is.EqualTo(NUnitTestEventTestOutput.Streams.Progress));
                Assert.That(sut.TestId, Is.EqualTo("0-1001"));
                Assert.That(sut.Content, Is.EqualTo("Whatever"));
                Assert.That(sut.TestName, Is.EqualTo("Something.TestClass.Whatever"));
            });
        }

        [Test]
        public void NUnitTestEventTestOutputTestWithError()
        {
            var sut = new NUnitTestEventTestOutput(XmlHelper.CreateXmlNode(OutputError));
            Assert.Multiple(() =>
            {
                Assert.That(sut.IsProgressStream, Is.False, "Progress stream failed");
                Assert.That(sut.IsErrorStream, Is.True, "Error stream failed");
                Assert.That(sut.IsNullOrEmptyStream, Is.False, "NullOrEmpty stream failed");
                Assert.That(sut.Stream, Is.EqualTo(NUnitTestEventTestOutput.Streams.Error), "Stream failed");
                Assert.That(sut.TestId, Is.EqualTo("0-1001"), "Id failed");
                Assert.That(sut.Content, Is.EqualTo("Whatever"), "Content failed");
                Assert.That(sut.TestName, Is.EqualTo("Something.TestClass.Whatever"), "Fullname failed");
            });
        }

        [Test]
        public void NUnitTestEventTestOutputTestWithBlank()
        {
            var sut = new NUnitTestEventTestOutput(XmlHelper.CreateXmlNode(BlankTestOutput));
            Assert.Multiple(() =>
            {
                Assert.That(sut.IsProgressStream, Is.True);
                Assert.That(sut.IsErrorStream, Is.False);
                Assert.That(sut.IsNullOrEmptyStream, Is.False);
                Assert.That(sut.Stream, Is.EqualTo(NUnitTestEventTestOutput.Streams.Progress));
                Assert.That(sut.TestId, Is.EqualTo("0-1001"));
                Assert.That(sut.Content.Length, Is.EqualTo(3));
                Assert.That(sut.TestName, Is.EqualTo("Something.TestClass.Whatever"));
            });
        }
    }
}