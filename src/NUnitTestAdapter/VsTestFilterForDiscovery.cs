using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace NUnit.VisualStudio.TestAdapter;

/// <summary>
/// Filter for discovery scenarios (--list-tests with --filter).
/// Uses reflection to access GetTestCaseFilter on IDiscoveryContext since
/// the method exists on the concrete type but isn't exposed through the interface.
/// </summary>
public class VsTestFilterForDiscovery(IDiscoveryContext discoveryContext, ITestLogger logger) : IVsTestFilter
{
    private static readonly List<string> SupportedProperties = ["FullyQualifiedName", "Name", "TestCategory", "Category", "Priority"];

    private readonly ITestCaseFilterExpression _filterExpression = GetFilterExpressionViaReflection(discoveryContext, logger);

    public ITestCaseFilterExpression MsTestCaseFilterExpression => _filterExpression;

    public bool IsEmpty => _filterExpression == null || string.IsNullOrEmpty(_filterExpression.TestCaseFilterValue);

    public IEnumerable<TestCase> CheckFilter(IEnumerable<TestCase> tests)
    {
        return IsEmpty
            ? tests
            : tests.Where(t => _filterExpression.MatchTestCase(t, p => VsTestFilter.PropertyValueProvider(t, p))).ToList();
    }

    private static ITestCaseFilterExpression GetFilterExpressionViaReflection(IDiscoveryContext discoveryContext, ITestLogger logger)
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
            {
                logger?.Debug($"VsTestFilterForDiscovery: GetTestCaseFilter method not found on {discoveryContext.GetType().FullName}. Filter will be disabled.");
                return null;
            }

            var result = method.Invoke(discoveryContext, [SupportedProperties, (Func<string, TestProperty>)VsTestFilter.PropertyProvider]);
            return result as ITestCaseFilterExpression;
        }
        catch (AmbiguousMatchException ex)
        {
            logger?.Debug($"VsTestFilterForDiscovery: Ambiguous match for GetTestCaseFilter on {discoveryContext.GetType().FullName}. Filter will be disabled. {ex.Message}");
            return null;
        }
        catch (TargetInvocationException ex)
        {
            logger?.Debug($"VsTestFilterForDiscovery: GetTestCaseFilter invocation failed on {discoveryContext.GetType().FullName}. Filter will be disabled. {ex.InnerException?.Message ?? ex.Message}");
            return null;
        }
        catch (TargetParameterCountException ex)
        {
            logger?.Debug($"VsTestFilterForDiscovery: Parameter mismatch calling GetTestCaseFilter on {discoveryContext.GetType().FullName}. Filter will be disabled. {ex.Message}");
            return null;
        }
        catch (MethodAccessException ex)
        {
            logger?.Debug($"VsTestFilterForDiscovery: Access denied calling GetTestCaseFilter on {discoveryContext.GetType().FullName}. Filter will be disabled. {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            logger?.Warning($"VsTestFilterForDiscovery: Unexpected exception getting filter expression from {discoveryContext.GetType().FullName}. Filter will be disabled. {ex.ToString()}");
            return null;
        }
    }
}