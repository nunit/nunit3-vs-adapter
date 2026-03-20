using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter;

/// <summary>
/// Filter for discovery scenarios (--list-tests with --filter).
/// Uses reflection to access GetTestCaseFilter on IDiscoveryContext since
/// the method exists on the concrete type but isn't exposed through the interface.
/// </summary>
public class VsTestFilterForDiscovery(IDiscoveryContext discoveryContext) : IVsTestFilter
{
    private static readonly List<string> SupportedProperties = ["FullyQualifiedName", "Name", "TestCategory", "Category", "Priority"];

    private readonly ITestCaseFilterExpression _filterExpression = GetFilterExpressionViaReflection(discoveryContext);

    public ITestCaseFilterExpression MsTestCaseFilterExpression => _filterExpression;

    public bool IsEmpty => _filterExpression == null || string.IsNullOrEmpty(_filterExpression.TestCaseFilterValue);

    public IEnumerable<TestCase> CheckFilter(IEnumerable<TestCase> tests)
    {
        return IsEmpty
            ? tests
            : tests.Where(t => _filterExpression.MatchTestCase(t, p => VsTestFilter.PropertyValueProvider(t, p))).ToList();
    }

    private static ITestCaseFilterExpression GetFilterExpressionViaReflection(IDiscoveryContext discoveryContext)
    {
        if (discoveryContext == null)
            return null;

        try
        {
            // IDiscoveryContext doesn't expose GetTestCaseFilter, but the concrete DiscoveryContext type has it.
            // Use reflection to call it, similar to how MSTest and xUnit adapters handle this.
            var method = discoveryContext.GetType().GetMethod(
                "GetTestCaseFilter",
                [typeof(IEnumerable<string>), typeof(Func<string, TestProperty>)]);

            if (method == null)
                return null;

            var result = method.Invoke(discoveryContext, [SupportedProperties, (Func<string, TestProperty>)VsTestFilter.PropertyProvider]);
            return result as ITestCaseFilterExpression;
        }
        catch
        {
            // If reflection fails, return null (no filtering)
            return null;
        }
    }
}