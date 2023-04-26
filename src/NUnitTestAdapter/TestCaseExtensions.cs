using Microsoft.TestPlatform.AdapterUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace NUnit.VisualStudio.TestAdapter
{
    // ref: https://github.com/microsoft/testfx/blob/5773e71789f92fa3b179de0e0a40db32015c3b36/src/Adapter/MSTest.TestAdapter/Extensions/TestCaseExtensions.cs
    internal static class TestCaseExtensions
    {
        internal static readonly TestProperty ManagedTypeProperty = TestProperty.Register(
            id: ManagedNameConstants.ManagedTypePropertyId,
            label: ManagedNameConstants.ManagedTypeLabel,
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string),
            validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
            attributes: TestPropertyAttributes.Hidden,
            owner: typeof(TestCase));

        internal static readonly TestProperty ManagedMethodProperty = TestProperty.Register(
            id: ManagedNameConstants.ManagedMethodPropertyId,
            label: ManagedNameConstants.ManagedMethodLabel,
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string),
            validateValueCallback: o => !string.IsNullOrWhiteSpace(o as string),
            attributes: TestPropertyAttributes.Hidden,
            owner: typeof(TestCase));

        internal static readonly TestProperty HierarchyProperty = TestProperty.Register(
            id: HierarchyConstants.HierarchyPropertyId,
            label: HierarchyConstants.HierarchyLabel,
            category: string.Empty,
            description: string.Empty,
            valueType: typeof(string[]),
            validateValueCallback: null,
            attributes: TestPropertyAttributes.Immutable,
            owner: typeof(TestCase));

        /// <summary>
        /// The test name.
        /// </summary>
        /// <param name="testCase"> The test case. </param>
        /// <param name="testClassName"> The test case's class name. </param>
        /// <returns> The test name, without the class name, if provided. </returns>
        internal static string GetTestName(this TestCase testCase, string testClassName)
        {
            var fullyQualifiedName = testCase.FullyQualifiedName;

            // Not using Replace because there can be multiple instances of that string.
            var name = fullyQualifiedName.StartsWith($"{testClassName}.")
                ? fullyQualifiedName.Remove(0, $"{testClassName}.".Length)
                : fullyQualifiedName;

            return name;
        }

        internal static string GetManagedType(this TestCase testCase) => testCase.GetPropertyValue<string>(ManagedTypeProperty, null);

        internal static void SetManagedType(this TestCase testCase, string value) => testCase.SetPropertyValue(ManagedTypeProperty, value);

        internal static string GetManagedMethod(this TestCase testCase) => testCase.GetPropertyValue<string>(ManagedMethodProperty, null);

        internal static void SetManagedMethod(this TestCase testCase, string value) => testCase.SetPropertyValue(ManagedMethodProperty, value);

        internal static bool ContainsManagedMethodAndType(this TestCase testCase) => !string.IsNullOrWhiteSpace(testCase.GetManagedMethod()) && !string.IsNullOrWhiteSpace(testCase.GetManagedType());

        internal static string[] GetHierarchy(this TestCase testCase) => testCase.GetPropertyValue<string[]>(HierarchyProperty, null);

        internal static void SetHierarchy(this TestCase testCase, params string[] value) => testCase.SetPropertyValue(HierarchyProperty, value);
    }
}
