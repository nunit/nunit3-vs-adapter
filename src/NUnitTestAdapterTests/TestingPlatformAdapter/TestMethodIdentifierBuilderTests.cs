using System.Collections.Generic;
using NUnit.Framework;
using NUnit.VisualStudio.TestAdapter.TestingPlatformAdapter;

namespace NUnit.VisualStudio.TestAdapter.Tests.TestingPlatformAdapter;

/// <summary>
/// Issue #1504: parameter types must use the managed-name encoding (angle-bracket generic
/// arguments), not reflection's assembly-qualified Type.FullName. Otherwise VS RTD entries
/// never match the runtime entries.
/// See https://github.com/nunit/nunit3-vs-adapter/issues/1504#issuecomment-6062403432
/// </summary>
public class TestMethodIdentifierBuilderTests
{
    [TestCase(nameof(Issue1504Target.Plain), "System.Decimal")]
    [TestCase(nameof(Issue1504Target.NullableDecimal), "System.Nullable`1<System.Decimal>")]
    [TestCase(nameof(Issue1504Target.NullableBool), "System.Nullable`1<System.Boolean>")]
    [TestCase(nameof(Issue1504Target.GenericList), "System.Collections.Generic.List`1<System.Int32>")]
    [TestCase(nameof(Issue1504Target.NestedGeneric), "System.Collections.Generic.Dictionary`2<System.String,System.Collections.Generic.List`1<System.Nullable`1<System.Int32>>>")]
    public void ParameterTypesUseManagedNameEncoding(string methodName, string expected)
    {
        var type = typeof(Issue1504Target);

        var property = TestMethodIdentifierBuilder.Create(
            type.Assembly.Location,
            $"{type.FullName}.{methodName}(null)",
            type.FullName,
            methodName,
            includeFixtureArguments: false);

        Assert.That(property.ParameterTypeFullNames, Is.EqualTo([expected]));
    }

    /// <summary>
    /// Issue #1513: VS real-time discovery knows only the CLR type, so without trx the
    /// type name must not contain the fixture arguments. With trx the bridge builds the
    /// trx className from TypeName, so the fixture arguments are kept there (#1351).
    /// </summary>
    [TestCase(false, "Issue1513Fixture")]
    [TestCase(true, "Issue1513Fixture(A)")]
    public void TypeNameHasFixtureArgumentsOnlyForTrx(bool includeFixtureArguments, string expected)
    {
        var type = typeof(Issue1513Fixture);

        var property = TestMethodIdentifierBuilder.Create(
            type.Assembly.Location,
            $"{type.FullName}(A).{nameof(Issue1513Fixture.Test)}",
            type.FullName,
            nameof(Issue1513Fixture.Test),
            includeFixtureArguments);

        Assert.Multiple(() =>
        {
            Assert.That(property.Namespace, Is.EqualTo(type.Namespace));
            Assert.That(property.TypeName, Is.EqualTo(expected));
        });
    }
}

#pragma warning disable SA1402 // File may only contain a single type
public class Issue1504Target
{
    public void Plain(decimal value) { }
    public void NullableDecimal(decimal? value) { }
    public void NullableBool(bool? value) { }
    public void GenericList(List<int> value) { }
    public void NestedGeneric(Dictionary<string, List<int?>> value) { }
}

public class Issue1513Fixture
{
    public void Test() { }
}
#pragma warning restore SA1402
