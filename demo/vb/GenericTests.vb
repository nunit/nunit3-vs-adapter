Imports NUnit.Framework

Namespace NUnitTestDemo

    <TestFixture(GetType(Integer))>
    Public Class GenericTests(Of T)

        <Test, ExpectPass>
        Public Sub TestIt()

        End Sub

    End Class

    <ExpectPass>
    <TestFixture(GetType(ArrayList))>
    <TestFixture(GetType(List(Of Integer)))>
    Public Class GenericTests_IList(Of TList As {IList, New})

        Dim list As IList

        <SetUp>
        Public Sub CreateList()
            list = New TList()
        End Sub

        <Test>
        Public Sub CanAddToList()
            list.Add(1)
            list.Add(2)
            list.Add(3)
            Assert.AreEqual(3, list.Count)
        End Sub

    End Class

End Namespace