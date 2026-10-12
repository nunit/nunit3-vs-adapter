using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

#if !NET462 && !NETSTANDARD
using System.Runtime.Loader;
#endif

using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.TestPlatform.AdapterUtilities.ManagedNameUtilities;

namespace NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

internal static class TestMethodIdentifierBuilder
{
    private static readonly ConcurrentDictionary<string, string> AssemblyNameCache
        = new(StringComparer.OrdinalIgnoreCase);

    // Caches only the method signature, keyed by (assemblyPath, clrClassName, methodName).
    // The TestMethodIdentifierProperty itself is not cached because typeName varies per
    // fixture instance (e.g. Tests(One) vs Tests(Two)) while the CLR class is the same.
    private static readonly ConcurrentDictionary<(string, string, string), (string MethodName, int Arity, string[] ParameterTypes)> SignatureCache
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

        var signature = SignatureCache.GetOrAdd(
            (assemblyPath, className, methodName),
            _ => GetMethodSignature(assemblyPath, className, methodName));

        return new TestMethodIdentifierProperty(
            assemblyFullName,
            ns,
            typeName,
            signature.MethodName,
            signature.Arity,
            signature.ParameterTypes,
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

    // Parameter types must use the managed-name encoding (e.g. System.Nullable`1<System.Decimal>),
    // which is what VS real-time discovery produces, not reflection's Type.FullName (#1504).
    private static (string MethodName, int Arity, string[] ParameterTypes) GetMethodSignature(string assemblyPath, string className, string methodName)
    {
        var fallback = (methodName, 0, Array.Empty<string>());
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
                return fallback;
            ManagedNameHelper.GetManagedName(methods[0], out _, out var managedMethod);
            ManagedNameParser.ParseManagedMethodName(managedMethod, out var name, out var arity, out var parameterTypes);
            return (name, arity, parameterTypes ?? Array.Empty<string>());
        }
        catch
        {
            return fallback;
        }
    }
}
