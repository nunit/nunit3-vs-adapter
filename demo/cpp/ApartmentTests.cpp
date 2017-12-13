using namespace System;
using namespace System::Threading;
using namespace NUnit::Framework;

namespace NUnitTestDemo
{
    [Apartment(ApartmentState::STA)]
    public ref class FixtureWithApartmentAttributeOnClass
    {
	public:
        [Test]
        void TestMethodInSTAFixture()
        {
            Assert::That(Thread::CurrentThread->GetApartmentState(), Is::EqualTo(ApartmentState::STA));
        }
	};

    public ref class FixtureWithApartmentAttributeOnMethod
    {
	public:
        [Test, Apartment(ApartmentState::STA)]
        void TestMethodInSTA()
        {
            Assert::That(Thread::CurrentThread->GetApartmentState(), Is::EqualTo(ApartmentState::STA));
        }
	};
}
