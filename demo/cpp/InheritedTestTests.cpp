using namespace System;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
    public ref class InheritedTestBaseClass
    {
	public:
        [Test]
        void TestInBaseClass()
        {
        }
	};

    public ref class InheritedTestDerivedClass : InheritedTestBaseClass
    {
	};
}
