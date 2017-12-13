Imports System.Threading
Imports NUnit.Framework

Namespace NUnitTestDemo

    <Apartment(ApartmentState.STA)>
    Public Class FixtureWithApartmentAttributeOnClass

        <Test>
        Public Sub TestMethodInSTAFixture()
            Assert.That(Thread.CurrentThread.GetApartmentState(), Iz.EqualTo(ApartmentState.STA))
        End Sub

    End Class

    Public Class FixtureWithApartmentAttributeOnMethod

        <Test, Apartment(ApartmentState.STA)>
        Public Sub TestMethodInSTA()
            Assert.That(Thread.CurrentThread.GetApartmentState(), Iz.EqualTo(ApartmentState.STA))
        End Sub

    End Class

End Namespace
