using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

#if !NET462 && !NETSTANDARD
using System.Runtime.Loader;
#endif

using Microsoft.Testing.Platform.Extensions.Messages;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

internal static class TestMethodIdentifierBuilder
{
    private static readonly ConcurrentDictionary<string, string> AssemblyNameCache
        = new(StringComparer.OrdinalIgnoreCase);

    // Caches only the parameter types, keyed by (assemblyPath, clrClassName, methodName).
    // The TestMethodIdentifierProperty itself is not cached because typeName varies per
    // fixture instance (e.g. Tests(One) vs Tests(Two)) while the CLR class is the same.
    private static readonly ConcurrentDictionary<(string, string, string), string[]> ParamTypeCache
        = new();

    public static TestMethodIdentifierProperty Create(
        string assemblyPath,
        string fullyQualifiedName,
        string className,
        string methodName)
    {
        var assemblyFullName = AssemblyNameCache.GetOrAdd(
            assemblyPath,
            path => AssemblyName.GetAssemblyName(path).FullName);

        var classWithFixtureParams = ExtractClassFromFullName(fullyQualifiedName);
        if (string.IsNullOrEmpty(classWithFixtureParams))
            classWithFixtureParams = className;

        var dot = FindLastDotNotInParens(classWithFixtureParams);
        var ns = dot >= 0 ? classWithFixtureParams.Substring(0, dot) : string.Empty;
        var typeName = dot >= 0 ? classWithFixtureParams.Substring(dot + 1) : classWithFixtureParams;

        var paramTypes = ParamTypeCache.GetOrAdd(
            (assemblyPath, className, methodName),
            _ => GetMethodParameterTypes(assemblyPath, className, methodName));

        return new TestMethodIdentifierProperty(
            assemblyFullName,
            ns,
            typeName,
            methodName,
            methodArity: 0,
            parameterTypeFullNames: paramTypes,
            returnTypeFullName: "System.Void");
    }

    // Returns everything before the last '.' that is not inside parentheses.
    // "Ns.Tests(One).Test1"    -> "Ns.Tests(One)"   (fixture-parameterized)
    // "Ns.Tests.Test1(1)"      -> "Ns.Tests"         (method-parameterized)
    // "Ns.Tests.Test1"         -> "Ns.Tests"         (plain)
    private static string ExtractClassFromFullName(string fullName)
    {
        var dot = FindLastDotNotInParens(fullName);
        return dot >= 0 ? fullName.Substring(0, dot) : string.Empty;
    }

    private static int FindLastDotNotInParens(string s)
    {
        int depth = 0;
        for (int i = s.Length - 1; i >= 0; i--)
        {
            switch (s[i])
            {
                case ')':
                    depth++;
                    break;
                case '(':
                    depth--;
                    break;
                case '.' when depth == 0:
                    return i;
            }
        }
        return -1;
    }

    private static string[] GetMethodParameterTypes(string assemblyPath, string className, string methodName)
    {
        try
        {
#if !NET462 && !NETSTANDARD
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#else
            var assembly = Assembly.LoadFrom(assemblyPath);
#endif
            var type = assembly.GetType(className, throwOnError: false);
            var methods = type?.GetMethods().Where(m => m.Name == methodName).ToList();
            if (methods == null || methods.Count != 1)
                return Array.Empty<string>();
            return methods[0].GetParameters()
                .Select(p => p.ParameterType.FullName ?? string.Empty)
                .Where(n => n.Length > 0)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
