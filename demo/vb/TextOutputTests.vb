Imports NUnit.Framework

Namespace NUnitTestDemo

    <ExpectPass>
    Public Class TextOutputTests

        <Test>
        Public Sub WriteToConsole()
            Console.WriteLine("This is Console line 1")
            Console.WriteLine("This is Console line 2\nThis is Console line 3")
        End Sub

        <Test>
        Public Sub WriteToError()
            Console.Error.WriteLine("This is Error line 1")
            Console.Error.WriteLine("This is Error line 2\nThis is Error line 3")
        End Sub

        <Test>
        Public Sub WriteToTestContext()
            TestContext.WriteLine("Line 1 to TestContext")
            TestContext.WriteLine("Line 2 to TestContext\nLine 3 to TestContext")
        End Sub

        <Test>
        Public Sub WriteToTestContextOut()
            TestContext.Out.WriteLine("Line 1 to TestContext.Out")
            TestContext.Out.WriteLine("Line 2 to TestContext.Out\nLine 3 to TestContext.Out")
        End Sub

        <Test>
        Public Sub WriteToTestContextError()
            TestContext.Error.WriteLine("Line 1 to TestContext.Error")
            TestContext.Error.WriteLine("Line 2 to TestContext.Error\nLine 3 to TestContext.Error")
        End Sub

        <Test>
        Public Sub WriteToTestContextProgress()
            TestContext.Progress.WriteLine("Line 1 to TestContext.Progress")
            TestContext.Progress.WriteLine("Line 2 to TestContext.Progress\nLine 3 to TestContext.Progress")
        End Sub

        <Test>
        Public Sub WriteToTrace()
            Trace.Write("This is Trace line 1")
            Trace.Write("This is Trace line 2")
            Trace.Write("This is Trace line 3")
        End Sub

        <Test, Description("Displays various settings for verification")>
        Public Sub DisplayTestSettings()
#If NETCOREAPP1_1 Then
            Console.WriteLine("CurrentDirectory={0}", Directory.GetCurrentDirectory())
            Console.WriteLine("Location={0}", typeof(TextOutputTests).GetTypeInfo().Assembly.Location)
#Else
            Console.WriteLine("CurrentDirectory={0}", Environment.CurrentDirectory)
            Console.WriteLine("BasePath={0}", AppDomain.CurrentDomain.BaseDirectory)
            Console.WriteLine("PrivateBinPath={0}", AppDomain.CurrentDomain.SetupInformation.PrivateBinPath)
#End If
            Console.WriteLine("WorkDirectory={0}", TestContext.CurrentContext.WorkDirectory)
            Console.WriteLine("DefaultTimeout={0}", NUnit.Framework.Internal.TestExecutionContext.CurrentContext.TestCaseTimeout)
        End Sub

        <Test>
        Public Sub DisplayTestParameters()
            If (TestContext.Parameters.Count = 0) Then
                Console.WriteLine("No TestParameters were passed")
            Else
                For Each name As String In TestContext.Parameters.Names
                    Console.WriteLine("Parameter: {0} = {1}", name, TestContext.Parameters.Get(name))
                Next
            End If
        End Sub

    End Class

End Namespace