using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class SimpleTests
    {
        [Test, ExpectPass]
        public void TestSucceeds()
        {
            Console.WriteLine("Simple test running");
            Assert.That(2 + 2, Is.EqualTo(4));
        }

        [Test, ExpectPass]
        public void TestSucceeds_Message()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
            Assert.Pass("Simple arithmetic!");
        }

        [Test, ExpectFailure]
        public void TestFails()
        {
            Assert.That(2 + 2, Is.EqualTo(5));
        }

        [Test, ExpectWarning]
        public void TestWarns()
        {
            Assert.Warn("This is a warning");
        }

        [Test, ExpectWarning]
        public void TestWarnsThreeTimes()
        {
            Assert.Warn("Warning 1");
            Assert.Warn("Warning 2");
            Assert.Warn("Warning 3");
        }

        [Test, ExpectFailure]
        public void TestWithThreeFailures()
        {
            Assert.Multiple(() =>
            {
                Assert.Fail("Failure 1");
                Assert.That(2 + 2, Is.EqualTo(5), "Failure 2");
                Assert.That(42, Is.GreaterThan(99), "Failure 3");
            });
        }

        [Test, ExpectFailure]
        public void TestWithTwoFailuresAndAnError()
        {
            Assert.Multiple(() =>
            {
                Assert.That(2 + 2, Is.EqualTo(5));
                Assert.That(42, Is.GreaterThan(99));
                throw new Exception("Throwing after two failures");
            });
        }

        [Test, ExpectFailure]
        public void TestWithFailureAndWarning()
        {
            Assert.Warn("WARNING!");
            Assert.Fail("FAILING!");
        }

        [Test, ExpectFailure]
        public void TestWithTwoFailuresAndAWarning()
        {
            Warn.Unless(2 + 2 == 5, "Math is too hard!");

            Assert.Multiple(() =>
            {
                Assert.That(2 + 2, Is.EqualTo(5));
                Assert.That(42, Is.GreaterThan(99));
            });
        }

        [Test, ExpectFailure]
        public void TestFails_StringEquality()
        {
            Assert.That("Hello" + "World" + "!", Is.EqualTo("Hello World!"));
        }

        [Test, ExpectInconclusive]
        public void TestIsInconclusive()
        {
            Assert.Inconclusive("Testing");
        }

        [Test, Ignore("Ignoring this test deliberately"), ExpectIgnore]
        public void TestIsIgnored_Attribute()
        {
        }

        [Test, ExpectIgnore]
        public void TestIsIgnored_Assert()
        {
            Assert.Ignore("Ignoring this test deliberately");
        }

        // Since we only run under .NET, test is always excluded
        [Test, ExpectSkip, Platform("Exclude=\"NET\"")]
        public void TestIsSkipped_Platform()
        {
        }

        [Test, ExpectSkip, Explicit]
        public void TestIsExplicit()
        {
        }

        [Test, ExpectError]
        public void TestThrowsException()
        {
            throw new Exception("Deliberate exception thrown");
        }

        [Test, ExpectPass]
        [Property("Priority", "High")]
        public void TestWithProperty()
        {
        }

        [Test, ExpectPass]
        [Property("Priority", "Low")]
        [Property("Action", "Ignore")]
        public void TestWithTwoProperties()
        {
        }

        [Test, ExpectPass]
        [Category("Slow")]
        public void TestWithCategory()
        {
        }

        [Test, ExpectPass]
        [Category("Slow")]
        [Category("Data")]
        public void TestWithTwoCategories()
        {
        }
    }
}
