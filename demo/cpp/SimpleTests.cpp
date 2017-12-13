#include "ExpectedOutcomeAttributes.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
    public ref class SimpleTests
    {
	public:
        [Test, ExpectPass]
        void TestSucceeds()
        {
            Console::WriteLine("Simple test running");
            Assert::That(2 + 2, Is::EqualTo(4));
		}

        [Test, ExpectPass]
        void TestSucceeds_Message()
        {
            Assert::That(2 + 2, Is::EqualTo(4));
            Assert::Pass("Simple arithmetic!");
        }

        [Test, ExpectFailure]
        void TestFails()
        {
            Assert::That(2 + 2, Is::EqualTo(5));
        }

        [Test, ExpectWarning]
        void TestWarns()
        {
            Assert::Warn("This is a warning");
        }

        [Test, ExpectWarning]
        void TestWarnsThreeTimes()
        {
            Assert::Warn("Warning 1");
            Assert::Warn("Warning 2");
            Assert::Warn("Warning 3");
        }

        //[Test, ExpectFailure]
        //void TestWithThreeFailures()
        //{
        //    Assert::Multiple(() =>
        //    {
        //        Assert::Fail("Failure 1");
        //        Assert::That(2 + 2, Is::EqualTo(5), "Failure 2");
        //        Assert::That(42, Is::GreaterThan(99), "Failure 3");
        //    });
        //}

        //[Test, ExpectFailure]
        //void TestWithTwoFailuresAndAnError()
        //{
        //    Assert::Multiple(() =>
        //    {
        //        Assert.That(2 + 2, Is.EqualTo(5));
        //        Assert.That(42, Is.GreaterThan(99));
        //        throw new Exception("Throwing after two failures");
        //    });
        //}

        [Test, ExpectFailure]
        void TestWithFailureAndWarning()
        {
            Assert::Warn("WARNING!");
            Assert::Fail("FAILING!");
        }

        [Test, ExpectFailure]
        void TestWithTwoFailuresAndAWarning()
        {
            Warn::Unless(2 + 2 == 5, "Math is too hard!");

            //Assert::Multiple(() =>
            //{
            //    Assert::That(2 + 2, Is.EqualTo(5));
            //    Assert::That(42, Is.GreaterThan(99));
            //});
        }

        [Test, ExpectFailure]
        void TestFails_StringEquality()
        {
            Assert::That("Hello" + "World" + "!", Is::EqualTo("Hello World!"));
        }

        [Test, ExpectInconclusive]
        void TestIsInconclusive()
        {
            Assert::Inconclusive("Testing");
        }

        [Test, Ignore("Ignoring this test deliberately"), ExpectIgnore]
        void TestIsIgnored_Attribute()
        {
        }

        [Test, ExpectIgnore]
        void TestIsIgnored_Assert()
        {
            Assert::Ignore("Ignoring this test deliberately");
        }

#if !NETCOREAPP1_1
        // Since we only run under .NET, test is always excluded
        [Test, ExpectSkip, Platform("Exclude=\"NET\"")]
        void TestIsSkipped_Platform()
        {
        }
#endif

        [Test, ExpectSkip, Explicit]
        void TestIsExplicit()
        {
        }

        [Test, ExpectError]
        void TestThrowsException()
        {
            throw gcnew Exception("Deliberate exception thrown");
        }

        [Test, ExpectPass]
        [Property("Priority", "High")]
        void TestWithProperty()
        {
        }

        [Test, ExpectPass]
        [Property("Priority", "Low")]
        [Property("Action", "Ignore")]
        void TestWithTwoProperties()
        {
        }

        [Test, ExpectPass]
        [Category("Slow")]
        void TestWithCategory()
        {
        }

        [Test, ExpectPass]
        [Category("Slow")]
        [Category("Data")]
        void TestWithTwoCategories()
        {
        }
	};
}
