Imports NUnit.Framework

Namespace NUnitTestDemo

    <TestFixture, ExpectPass>
    Public Class OneTimeSetUpTests

        Dim SetUpCount As Integer
        Dim TearDownCount As Integer

        <OneTimeSetUp>
        Public Sub BeforeTests()
            Assert.That(SetUpCount, Iz.EqualTo(0))
            Assert.That(TearDownCount, Iz.EqualTo(0))
            SetUpCount = SetUpCount + 1
        End Sub

        <OneTimeTearDown>
        Public Sub AfterTests()
            Assert.That(SetUpCount, Iz.EqualTo(1), "Unexpected error")
            Assert.That(TearDownCount, Iz.EqualTo(0))
            TearDownCount = TearDownCount + 1
        End Sub

        <Test>
        Public Sub Test1()
            Assert.That(SetUpCount, Iz.EqualTo(1))
            Assert.That(TearDownCount, Iz.EqualTo(0))
        End Sub

        <Test>
        Public Sub Test2()
            Assert.That(SetUpCount, Iz.EqualTo(1))
            Assert.That(TearDownCount, Iz.EqualTo(0))
        End Sub

    End Class

End Namespace