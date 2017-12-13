Imports NUnit.Framework

Namespace NUnitTestDemo

    Public Class Theories

        <Datapoints>
        Private data As Integer() = {0, 1, 42}

        <Theory, ExpectPass>
        Public Sub Theory_AllCasesSucceed(a As Integer, b As Integer)
            Assert.That(a + b, Iz.EqualTo(b + a))
        End Sub

        <Theory, ExpectMixed>
        Public Sub Theory_SomeCasesAreInconclusive(a As Integer, b As Integer)
            Assume.That(b <> 0)
        End Sub

        <Theory, ExpectMixed>
        Public Sub Theory_SomeCasesFail(a As Integer, b As Integer)
            Assert.That(b <> 0)
        End Sub

    End Class

End Namespace
