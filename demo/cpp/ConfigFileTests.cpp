#include "ExpectedOutcomeAttributes.h"

using namespace System;
using namespace System::Configuration;
using namespace System::IO;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
    [ExpectPass]
    public class ConfigFileTests
    {
	public:
        [Test]
        static void ProperConfigFileIsUsed()
        {
            String^ expectedPath = Path::Combine(TestContext::CurrentContext->TestDirectory, "NUnit3TestDemo.dll.config");
            Assert::That(expectedPath, Is::EqualTo(AppDomain::CurrentDomain->SetupInformation->ConfigurationFile));
        }

        [Test]
        static void CanReadConfigFile()
        {
            Assert::That(ConfigurationManager::AppSettings->Get("test.setting"), Is::EqualTo("54321"));
        }

	};
}
