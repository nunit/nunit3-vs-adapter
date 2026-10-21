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
        string className,
        string fixtureFullName,
        string methodName,
        bool includeFixtureArguments)
    {
        var assemblyFullName = AssemblyNameCache.GetOrAdd(
            assemblyPath,
            path => AssemblyName.GetAssemblyName(path).FullName);

        var ns = Namespace(className);
        var typeName = includeFixtureArguments && fixtureFullName != null
            ? FixtureTypeName(className, fixtureFullName)
            : StripNamespace(className, ns);

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

    // The fixture name without its namespace, e.g. Fixture("A") for Ns.Fixture("A").
    // Uses NUnit's fixture full name, not the test name, which can contain any text (#1513).
    internal static string FixtureTypeName(string className, string fixtureFullName)
        => StripNamespace(fixtureFullName, Namespace(className));

    // The namespace comes from the CLR class name, which has no fixture arguments.
    // Dots inside generic arguments ("Ns.Gen`1[[System.Int32, ...]]") are not namespace dots.
    private static string Namespace(string className)
    {
        var end = className.IndexOfAny(['[', '<', '(']);
        var dot = end < 0 ? className.LastIndexOf('.') : className.LastIndexOf('.', end);
        return dot >= 0 ? className.Substring(0, dot) : string.Empty;
    }

    private static string StripNamespace(string name, string ns)
        => ns.Length > 0 && name.StartsWith(ns + ".", StringComparison.Ordinal)
            ? name.Substring(ns.Length + 1)
            : name;

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
