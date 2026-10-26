// ***********************************************************************
// Copyright (c) 2026 Charlie Poole, Terje Sandstrom
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
#if !NET462 && !NETSTANDARD
using System.Runtime.Loader;
#endif
using Microsoft.TestPlatform.AdapterUtilities.ManagedNameUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter;

/// <summary>
/// The TestCase.ManagedType and TestCase.ManagedMethod properties (VSTest RFC 0017): the test
/// method as the compiler sees it, for example ProgramTests and
/// TupleTests(System.Collections.Generic.IEnumerable`1&lt;System.ValueTuple`2&lt;System.Object,System.Object&gt;&gt;).
/// Visual Studio can use them to match a test to the method it finds in the source.
/// </summary>
public static class ManagedNames
{
    internal static readonly TestProperty ManagedTypeProperty = TestProperty.Register(
        "TestCase.ManagedType", "ManagedType", typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    internal static readonly TestProperty ManagedMethodProperty = TestProperty.Register(
        "TestCase.ManagedMethod", "ManagedMethod", typeof(string), TestPropertyAttributes.Hidden, typeof(TestCase));

    private static readonly ConcurrentDictionary<(string, string, string), (string Type, string Method)?> Cache = new();

    /// <summary>
    /// Sets both properties, when the method can be found by reflection and has no overloads.
    /// Discovery calls <see cref="KeepOnlyUnique"/> when all tests of the assembly are converted;
    /// execution calls <see cref="RemoveShared"/> with <see cref="FindShared"/> of the whole assembly.
    /// </summary>
    public static void Set(TestCase testCase, string className, string methodName)
    {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(methodName))
            return;
        var names = Cache.GetOrAdd((testCase.Source, className, methodName), key => Find(key.Item1, key.Item2, key.Item3));
        if (names == null)
            return;
        testCase.SetPropertyValue(ManagedTypeProperty, names.Value.Type);
        testCase.SetPropertyValue(ManagedMethodProperty, names.Value.Method);
    }

    /// <summary>
    /// Removes both properties from tests that share them with another test of the assembly: the test
    /// cases of a parameterized method, or the same method in each instance of a parameterized fixture.
    /// Visual Studio merges each of them into the one method it finds in the source, and keeps only
    /// the last one, so the others are lost from Test Explorer (issue 489).
    /// </summary>
    public static void KeepOnlyUnique(IEnumerable<TestCase> testCases)
    {
        var list = testCases.ToList();
        var shared = list
            .Select(NamesOf)
            .Where(n => n.Method != null)
            .GroupBy(n => n)
            .Where(g => g.Skip(1).Any())
            .Select(g => g.Key);
        RemoveShared(list, new HashSet<(string, string)>(shared));
    }

    /// <summary>
    /// The managed names that more than one test of the assembly has, from an explore of the whole
    /// assembly. Execution needs this: it converts only the selected tests, so one test case of a
    /// method with two looks like it has the names alone, and Visual Studio then does not show its
    /// result, because discovery gave that test no managed names.
    /// </summary>
    public static ISet<(string Type, string Method)> FindShared(string assemblyPath, IEnumerable<XmlNode> testCaseNodes)
    {
        var names = testCaseNodes
            .Select(node => Cache.GetOrAdd((assemblyPath, FixtureClassName(node), node.GetAttribute("methodname")), key => Find(key.Item1, key.Item2, key.Item3)))
            .Where(n => n != null)
            .Select(n => n.Value);
        return new HashSet<(string, string)>(names.GroupBy(n => n).Where(g => g.Skip(1).Any()).Select(g => g.Key));
    }

    /// <summary>
    /// Removes both properties from the tests that have one of the shared names.
    /// </summary>
    public static void RemoveShared(IEnumerable<TestCase> testCases, ISet<(string Type, string Method)> shared)
    {
        foreach (var testCase in testCases.Where(tc => shared.Contains(NamesOf(tc))))
        {
            testCase.RemovePropertyValue(ManagedTypeProperty);
            testCase.RemovePropertyValue(ManagedMethodProperty);
        }
    }

    private static (string Type, string Method) NamesOf(TestCase testCase) =>
        (testCase.GetPropertyValue(ManagedTypeProperty) as string, testCase.GetPropertyValue(ManagedMethodProperty) as string);

    /// <summary>
    /// The class of the fixture a test case is in, as the converters use it: for an inherited test, the
    /// test case's own class name is the base class.
    /// </summary>
    private static string FixtureClassName(XmlNode testCase)
    {
        for (var node = testCase.ParentNode; node != null; node = node.ParentNode)
        {
            if (node.GetAttribute("type") == "TestFixture")
                return node.GetAttribute("classname");
        }
        return testCase.GetAttribute("classname");
    }

    private static (string Type, string Method)? Find(string assemblyPath, string className, string methodName)
    {
        try
        {
#if !NET462 && !NETSTANDARD
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
#else
            var assembly = Assembly.LoadFrom(assemblyPath);
#endif
            var methods = assembly.GetType(className, throwOnError: false)?.GetMethods().Where(m => m.Name == methodName).ToList();
            if (methods is not { Count: 1 })
                return null;
            ManagedNameHelper.GetManagedName(methods[0], out var managedType, out var managedMethod);
            return (managedType, managedMethod);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
