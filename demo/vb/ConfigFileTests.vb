Imports System.Configuration
Imports System.IO
Imports NUnit.Framework

Namespace NUnitTestDemo

    <ExpectPass>
    Public Class ConfigFileTests
        <Test>
        Public Shared Sub ProperConfigFileIsUsed()
            Dim expectedPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "NUnit3TestDemo.dll.config")
            Assert.That(expectedPath, Iz.EqualTo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
        End Sub

        <Test>
        Public Shared Sub CanReadConfigFile()
            Assert.That(ConfigurationManager.AppSettings.Get("test.setting"), Iz.EqualTo("54321"))
        End Sub

    End Class

End Namespace
