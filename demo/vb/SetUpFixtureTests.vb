Imports NUnit.Framework

Namespace NUnitTestDemo.SetUpFixture

    <SetUpFixture>
    Public Class SetUpFixture
        Public Shared SetUpCount As Integer
        Public Shared TearDownCount As Integer

        <OneTimeSetUp>
        Public Sub BeforeTests()
            Assert.That(SetUpCount, Iz.EqualTo(0))
            SetUpCount = SetUpCount + 1
        End Sub

        <OneTimeTearDown>
        Public Sub AfterTests()
            Assert.That(TearDownCount, Iz.EqualTo(0))
            TearDownCount = TearDownCount + 1
        End Sub

    End Class

    <TestFixture, ExpectPass>
    Public Class TestFixture1

        <Test>
        Public Sub Test1()
            Assert.That(SetUpFixture.SetUpCount, Iz.EqualTo(1))
            Assert.That(SetUpFixture.TearDownCount, Iz.EqualTo(0))
        End Sub

    End Class

    <TestFixture, ExpectPass>
    Public Class TestFixture2

        <Test>
        Public Sub Test2()
            Assert.That(SetUpFixture.SetUpCount, Iz.EqualTo(1))
            Assert.That(SetUpFixture.TearDownCount, Iz.EqualTo(0))
        End Sub

    End Class

End Namespace
