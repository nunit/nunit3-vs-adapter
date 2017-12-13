#include "ExpectedOutcomeAttributes.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::IO;
using namespace System::Reflection;
using namespace System::Text;
using namespace NUnit::Framework;
using namespace System::Diagnostics;

namespace NUnitTestDemo
{
    [ExpectPass]
    public class TextOutputTests
    {
	public:
        [Test]
        void WriteToConsole()
        {
            Console::WriteLine("This is Console line 1");
            Console::WriteLine("This is Console line 2\nThis is Console line 3");
        }

        [Test]
        void WriteToError()
        {
            Console::Error->WriteLine("This is Error line 1");
            Console::Error->WriteLine("This is Error line 2\nThis is Error line 3");
        }

        [Test]
        void WriteToTestContext()
        {
            TestContext::WriteLine("Line 1 to TestContext");
            TestContext::WriteLine("Line 2 to TestContext\nLine 3 to TestContext");
        }

        [Test]
        void WriteToTestContextOut()
        {
            TestContext::Out->WriteLine("Line 1 to TestContext.Out");
            TestContext::Out->WriteLine("Line 2 to TestContext.Out\nLine 3 to TestContext.Out");
        }

        [Test]
        void WriteToTestContextError()
        {
            TestContext::Error->WriteLine("Line 1 to TestContext.Error");
            TestContext::Error->WriteLine("Line 2 to TestContext.Error\nLine 3 to TestContext.Error");
        }

        [Test]
        void WriteToTestContextProgress()
        {
            TestContext::Progress->WriteLine("Line 1 to TestContext.Progress");
            TestContext::Progress->WriteLine("Line 2 to TestContext.Progress\nLine 3 to TestContext.Progress");
        }

        [Test]
        void WriteToTrace()
        {
            Trace::Write("This is Trace line 1");
            Trace::Write("This is Trace line 2");
            Trace::Write("This is Trace line 3");
        }

        [Test, Description("Displays various settings for verification")]
        void DisplayTestSettings()
        {
#if NETCOREAPP1_1
            Console::WriteLine("CurrentDirectory={0}", Directory.GetCurrentDirectory());
            Console::WriteLine("Location={0}", typeof(TextOutputTests).GetTypeInfo().Assembly.Location);
#else
            Console::WriteLine("CurrentDirectory={0}", Environment::CurrentDirectory);
            Console::WriteLine("BasePath={0}", AppDomain::CurrentDomain->BaseDirectory);
            Console::WriteLine("PrivateBinPath={0}", AppDomain::CurrentDomain->SetupInformation->PrivateBinPath);
#endif
            Console::WriteLine("WorkDirectory={0}", TestContext::CurrentContext->WorkDirectory);
            Console::WriteLine("DefaultTimeout={0}", NUnit::Framework::Internal::TestExecutionContext::CurrentContext->TestCaseTimeout);
        }

        [Test]
        void DisplayTestParameters()
        {
            if (TestContext::Parameters->Count == 0)
                Console::WriteLine("No TestParameters were passed");
            else
            for each (String^ name in TestContext::Parameters->Names)
                Console::WriteLine("Parameter: {0} = {1}", name, TestContext::Parameters->Get(name));
        }
	};
}
