Imports NUnit.Framework

Namespace NUnitTestDemo

    Public Class ParameterizedTests

        <ExpectPass>
        <TestCase(2, 2, 4)>
        <TestCase(0, 5, 5)>
        <TestCase(31, 11, 42)>
        Public Sub TestCaseSucceeds(a As Integer, b As Integer, sum As Integer)
            Assert.That(a + b, Iz.EqualTo(sum))
        End Sub

        <ExpectPass>
        <TestCase(2, 2, ExpectedResult:=4)>
        <TestCase(0, 5, ExpectedResult:=5)>
        <TestCase(31, 11, ExpectedResult:=42)>
        Public Function TestCaseSucceeds_Result(a As Integer, b As Integer) As Integer
            Return a + b
        End Function

        <ExpectFailure>
        <TestCase(31, 11, 99)>
        Public Sub TestCaseFails(a As Integer, b As Integer, sum As Integer)
            Assert.That(a + b, Iz.EqualTo(sum))
        End Sub

        <ExpectWarning>
        <TestCase(31, 11, 99)>
        Public Sub TestCaseWarns(a As Integer, b As Integer, sum As Integer)
            Warn.Unless(a + b, Iz.EqualTo(sum))
        End Sub

        <ExpectWarning>
        <TestCase(31, 11, 99)>
        Public Sub TestCaseWarnsThreeTimes(a As Integer, b As Integer, answer As Integer)
            Warn.Unless(a + b, Iz.EqualTo(answer), "Bad sum")
            Warn.Unless(a - b, Iz.EqualTo(answer), "Bad difference")
            Warn.Unless(a * b, Iz.EqualTo(answer), "Bad product")
        End Sub

        <TestCase(31, 11, ExpectedResult:=99), ExpectFailure>
        Public Function TestCaseFails_Result(a As Integer, b As Integer) As Integer
            Return a + b
        End Function

        <TestCase(31, 11), ExpectInconclusive>
        Public Sub TestCaseIsInconclusive(a As Integer, b As Integer)
            Assert.Inconclusive("Inconclusive test case")
        End Sub

        <Ignore("Ignored test"), ExpectIgnore>
        <TestCase(31, 11)>
        Public Sub TestCaseIsIgnored_Attribute(a As Integer, b As Integer)

        End Sub

        <TestCase(31, 11, Ignore:="Ignoring this"), ExpectIgnore>
        Public Sub TestCaseIsIgnored_Property(a As Integer, b As Integer)

        End Sub

        <TestCase(31, 11), ExpectIgnore>
        Public Sub TestCaseIsIgnored_Assert(a As Integer, b As Integer)
            Assert.Ignore("Ignoring this test case")
        End Sub

#If Not NETCOREAPP1_1 Then
        <TestCase(31, 11, ExcludePlatform:="NET"), ExpectSkip>
        Public Sub TestCaseIsSkipped_Property(a As Integer, b As Integer)

        End Sub

        <Platform(Exclude:="NET"), ExpectSkip>
        <TestCase(31, 11)>
        Public Sub TestCaseIsSkipped_Attribute(a As Integer, b As Integer)

        End Sub
#End If

        <Explicit, ExpectSkip>
        <TestCase(31, 11)>
        Public Sub TestCaseIsExplicit(a As Integer, b As Integer)

        End Sub

        <TestCase(31, 11), ExpectError>
        Public Sub TestCaseThrowsException(a As Integer, b As Integer)
            Throw New Exception("Exception from test case")
        End Sub

        <TestCase(42, TestName:="AlternateTestName"), ExpectPass>
        Public Sub TestCaseWithAlternateName(x As Integer)

        End Sub

        <TestCase(42, TestName:="NameWithSpecialChar->Here")>
        Public Sub TestCaseWithSpecialCharInName(x As Integer)

        End Sub

        <Test>
        Public Sub TestCaseWithRandomParameter(<Random(1)> x As Integer)

        End Sub

    End Class

End Namespace
