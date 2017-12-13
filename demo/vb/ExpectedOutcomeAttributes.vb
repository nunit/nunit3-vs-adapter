Imports NUnit.Framework

Namespace NUnitTestDemo

    Public Class ExpectPassAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Pass")
        End Sub

    End Class


    Public Class ExpectFailureAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Failure")
        End Sub

    End Class

    Public Class ExpectWarningAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Warning")
        End Sub

    End Class

    Public Class ExpectIgnoreAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Ignore")
        End Sub

    End Class

    Public Class ExpectSkipAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Skipped")
        End Sub

    End Class

    Public Class ExpectErrorAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Error")
        End Sub

    End Class

    Public Class ExpectInconclusiveAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Inconclusive")
        End Sub

    End Class

    Public Class ExpectMixedAttribute
        Inherits PropertyAttribute

        Public Sub New()
            MyBase.New("Expect", "Mixed")
        End Sub

    End Class

End Namespace