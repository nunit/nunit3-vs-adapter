using System;
using System.Reflection;

using Microsoft.Testing.Platform.Extensions.Messages;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

internal static class TestMethodIdentifierBuilder
{
    public static TestMethodIdentifierProperty Create(string assemblyPath, string className, string methodName)
    {
        var assemblyFullName = AssemblyName.GetAssemblyName(assemblyPath).FullName;
        var dot = className.LastIndexOf('.');
        var ns = dot >= 0 ? className.Substring(0, dot) : string.Empty;
        var typeName = dot >= 0 ? className.Substring(dot + 1) : className;

        return new TestMethodIdentifierProperty(
            assemblyFullName,
            ns,
            typeName,
            methodName,
            methodArity: 0,
            parameterTypeFullNames: Array.Empty<string>(),
            returnTypeFullName: "System.Void");
    }
}
