Imports NUnit.Framework

Namespace NUnitTestDemo

    Public Class SimpleTests

        <Test, ExpectPass>
        Public Sub TestSucceeds()
            Console.WriteLine("Simple test running")
            Assert.That(2 + 2, Iz.EqualTo(4))
        End Sub

        <Test, ExpectPass>
        Public Sub TestSucceeds_Message()
            Assert.That(2 + 2, Iz.EqualTo(4))
            Assert.Pass("Simple arithmetic!")
        End Sub

        <Test, ExpectFailure>
        Public Sub TestFails()
            Assert.That(2 + 2, Iz.EqualTo(5))
        End Sub

        <Test, ExpectWarning>
        Public Sub TestWarns()
            Assert.Warn("This is a warning")
        End Sub

        <Test, ExpectWarning>
        Public Sub TestWarnsThreeTimes()
            Assert.Warn("Warning 1")
            Assert.Warn("Warning 2")
            Assert.Warn("Warning 3")
        End Sub

        <Test, ExpectFailure>
        Public Sub TestWithThreeFailures()
            Assert.Multiple(
                Sub()
                    Assert.Fail("Failure 1")
                    Assert.That(2 + 2, Iz.EqualTo(5), "Failure 2")
                    Assert.That(42, Iz.GreaterThan(99), "Failure 3")
                End Sub
            )
        End Sub

        <Test, ExpectFailure>
        Public Sub TestWithTwoFailuresAndAnError()
            Assert.Multiple(
                Sub()
                    Assert.That(2 + 2, Iz.EqualTo(5))
                    Assert.That(42, Iz.GreaterThan(99))
                    Throw New Exception("Throwing after two failures")
                End Sub
            )
        End Sub

        <Test, ExpectFailure>
        Public Sub TestWithFailureAndWarning()
            Assert.Warn("WARNING!")
            Assert.Fail("FAILING!")
        End Sub

        <Test, ExpectFailure>
        Public Sub TestWithTwoFailuresAndAWarning()
            Warn.Unless(2 + 2 = 5, "Math is too hard!")

            Assert.Multiple(
                Sub()
                    Assert.That(2 + 2, Iz.EqualTo(5))
                    Assert.That(42, Iz.GreaterThan(99))
                End Sub
            )
        End Sub

        <Test, ExpectFailure>
        Public Sub TestFails_StringEquality()
            Assert.That("Hello" + "World" + "!", Iz.EqualTo("Hello World!"))
        End Sub

        <Test, ExpectInconclusive>
        Public Sub TestIsInconclusive()
            Assert.Inconclusive("Testing")
        End Sub

        <Test, Ignore("Ignoring this test deliberately"), ExpectIgnore>
        Public Sub TestIsIgnored_Attribute()

        End Sub

        <Test, ExpectIgnore>
        Public Sub TestIsIgnored_Assert()
            Assert.Ignore("Ignoring this test deliberately")
        End Sub

#If Not NETCOREAPP1_1 Then
        'Since we only run under .NET, test Is always excluded
        <Test, ExpectSkip, Platform(Exclude:="NET")>
        Public Sub TestIsSkipped_Platform()

        End Sub
#End If

        <Test, ExpectSkip, Explicit>
        Public Sub TestIsExplicit()

        End Sub

        <Test, ExpectError>
        Public Sub TestThrowsException()
            Throw New Exception("Deliberate exception thrown")
        End Sub

        <Test, ExpectPass>
        <PropertyAttribute("Priority", "High")>
        Public Sub TestWithProperty()

        End Sub

        <Test, ExpectPass>
        <PropertyAttribute("Priority", "Low")>
        <PropertyAttribute("Action", "Ignore")>
        Public Sub TestWithTwoProperties()

        End Sub

        <Test, ExpectPass>
        <Category("Slow")>
        Public Sub TestWithCategory()

        End Sub

        <Test, ExpectPass>
        <Category("Slow")>
        <Category("Data")>
        Public Sub TestWithTwoCategories()

        End Sub

    End Class

End Namespace
