Imports NUnit.Framework

Namespace NUnitTestDemo

    Public MustInherit Class InheritedTestBaseClass

        <Test>
        Public Sub TestInBaseClass()

        End Sub

    End Class

    Public Class InheritedTestDerivedClass
        Inherits InheritedTestBaseClass

    End Class

End Namespace