using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace NUnitTestDemo
{
    public class SimpleTests
    {
        [Test, Should("Pass")]
        public void TestSucceeds()
        {
            Console.WriteLine("Simple test running");
            Assert.That(2 + 2, Is.EqualTo(4));
        }

        [Test, Should("Pass")]
        public void TestSucceeds_Message()
        {
            Assert.That(2 + 2, Is.EqualTo(4));
            Assert.Pass("Simple arithmetic!");
        }

        [Test, Should("Fail")]
        public void TestFails()
        {
            Assert.That(2 + 2, Is.EqualTo(5));
        }

        [Test, Should("Fail")]
        public void TestFails_StringEquality()
        {
            Assert.That("Hello" + "World" + "!", Is.EqualTo("Hello World!"));
        }

        [Test, Should("Inconclusive")]
        public void TestIsInconclusive()
        {
            Assert.Inconclusive("Testing");
        }

        [Test, Ignore("Ignoring this test deliberately"), Should("Ignore")]
        public void TestIsIgnored_Attribute()
        {
        }

        [Test, Should("Ignore")]
        public void TestIsIgnored_Assert()
        {
            Assert.Ignore("Ignoring this test deliberately");
        }

        [Test, Should("Error")]
        public void TestThrowsException()
        {
            throw new Exception("Deliberate exception thrown");
        }

        [Test, Should("Pass")]
        [Property("Priority", "High")]
        public void TestWithProperty()
        {
        }

        [Test, Should("Pass")]
        [Property("Priority", "Low")]
        [Property("Action", "Ignore")]
        public void TestWithTwoProperties()
        {
        }

        [Test, Should("Pass")]
        [Category("Slow")]
        public void TestWithCategory()
        {
        }

        [Test, Should("Pass")]
        [Category("Slow")]
        [Category("Data")]
        public void TestWithTwoCategories()
        {
        }
    }
}
