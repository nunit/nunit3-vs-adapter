#nullable enable

namespace NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel;

/// <summary>
/// Gives a test filter a meaning for one target, such as NUnit filter XML, by handling each node type.
/// </summary>
/// <typeparam name="T">The result of visiting a node.</typeparam>
public interface ITestFilterVisitor<out T>
{
    T VisitFilter(ITestFilter filter);

    T VisitExpression(ITestFilterExpression expression);

    T VisitTerm(ITestFilterTerm term);

    T VisitGroup(ITestFilterGroup group);

    T VisitNegation(ITestFilterNegation negation);

    T VisitCondition(ITestFilterCondition condition);
}
