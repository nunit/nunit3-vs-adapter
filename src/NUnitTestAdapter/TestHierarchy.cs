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

using System.IO;
using Microsoft.TestPlatform.AdapterUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter;

/// <summary>
/// The TestCase.Hierarchy property: assembly, namespace, fixture and method, taken from NUnit.
/// When it is set, Test Explorer builds its tree from it. Without it, Test Explorer splits the
/// FullyQualifiedName at dots, also at dots inside the arguments (issue 1097).
/// </summary>
public static class TestHierarchy
{
    internal static readonly TestProperty HierarchyProperty = TestProperty.Register(
        HierarchyConstants.HierarchyPropertyId,
        HierarchyConstants.HierarchyLabel,
        typeof(string[]),
        TestPropertyAttributes.Immutable,
        typeof(TestCase));

    /// <summary>
    /// The hierarchy for a test, or null when NUnit gives no class or method for it.
    /// </summary>
    /// <param name="assemblyPath">The test assembly.</param>
    /// <param name="className">NUnit's class name, for example Ns.Outer+Inner.</param>
    /// <param name="fixtureName">NUnit's fixture name, for example Fixture(1) for a parameterized fixture.</param>
    /// <param name="methodName">NUnit's method name.</param>
    public static string[] Create(string assemblyPath, string className, string fixtureName, string methodName)
    {
        if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(methodName))
            return null;

        // The namespace ends at the last dot before a nested class or a generic argument list.
        int typeEnd = className.IndexOfAny(['+', '[']);
        int dot = className.LastIndexOf('.', typeEnd < 0 ? className.Length - 1 : typeEnd);

        var hierarchy = new string[HierarchyConstants.Levels.TotalLevelCount];
        hierarchy[HierarchyConstants.Levels.ContainerIndex] = Path.GetFileNameWithoutExtension(assemblyPath);
        hierarchy[HierarchyConstants.Levels.NamespaceIndex] = dot < 0 ? null : className.Substring(0, dot);
        hierarchy[HierarchyConstants.Levels.ClassIndex] = string.IsNullOrEmpty(fixtureName) ? className.Substring(dot + 1) : fixtureName;
        hierarchy[HierarchyConstants.Levels.TestGroupIndex] = methodName;
        return hierarchy;
    }

    /// <summary>
    /// Sets the hierarchy on the test case, when the UseHierarchyProperty setting is on.
    /// </summary>
    public static void Set(TestCase testCase, IAdapterSettings settings, string className, string fixtureName, string methodName)
    {
        if (!settings.UseHierarchyProperty)
            return;
        var hierarchy = Create(testCase.Source, className, fixtureName, methodName);
        if (hierarchy != null)
            testCase.SetPropertyValue(HierarchyProperty, hierarchy);
    }
}
