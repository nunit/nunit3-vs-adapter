using namespace System;
using namespace System::Collections::Generic;
using namespace System::Text;
using namespace System::Threading::Tasks;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
	public ref class ExpectPassAttribute : PropertyAttribute
    {
	public:
		ExpectPassAttribute() : PropertyAttribute("Expect", "Pass") { }
	};

    public ref class ExpectFailureAttribute : PropertyAttribute
    {
	public:
		ExpectFailureAttribute() : PropertyAttribute("Expect", "Failure") { }
	};

    public ref class ExpectWarningAttribute : PropertyAttribute
    {
	public:
		ExpectWarningAttribute() : PropertyAttribute("Expect", "Warning") { }
	};

    public ref class ExpectIgnoreAttribute : PropertyAttribute
    {
	public:
		ExpectIgnoreAttribute() : PropertyAttribute("Expect", "Ignore") { }
	};

    public ref class ExpectSkipAttribute : PropertyAttribute
    {
	public:
		ExpectSkipAttribute() : PropertyAttribute("Expect", "Skipped") { }
	};

    public ref class ExpectErrorAttribute : PropertyAttribute
    {
	public:
		ExpectErrorAttribute() : PropertyAttribute("Expect", "Error") { }
	};

    public ref class ExpectInconclusiveAttribute : PropertyAttribute
    {
	public:
		ExpectInconclusiveAttribute() : PropertyAttribute("Expect", "Inconclusive") { }
	};

    public ref class ExpectMixedAttribute : PropertyAttribute
    {
	public:
		ExpectMixedAttribute() : PropertyAttribute("Expect", "Mixed") { }
	};
}
