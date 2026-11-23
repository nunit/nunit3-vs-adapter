# VSTest filter parser API design

## Purpose and status

This document describes the filter parser and NUnit XML model implemented on
`feature/v7-test-filter-parser`. It is intended to make the design and its integration boundary
clear during review.

The legacy implementation is now named `LegacyTestFilterParser`. It implements the same
`ITestFilterParser` interface and keeps its legacy behavior. By default the adapter uses
`DefaultTestFilterParser`, which tries `VSTestFilterParser` first and uses `LegacyTestFilterParser`
for a filter the VSTest parser rejects, so existing filters keep working. The `FilterParser` run
setting selects one parser on its own (see [Selecting the parser](#selecting-the-parser)).

This design follows the escape-aware parsing approach described in
[TestFilterParsing-v7.md](./TestFilterParsing-v7.md). The adapter's general filter path uses it by
default, but the MTP fast path (`FullyQualifiedNameFilterParser`) is not yet routed through it,
and that document's other MTP changes are not implemented.

## Public API

[`VSTestFilterParser`](../src/NUnitTestAdapter/TestFilterConverter/VSTestFilterParser.cs)
implements [`ITestFilterParser`](../src/NUnitTestAdapter/TestFilterConverter/ITestFilterParser.cs):

```csharp
public interface ITestFilterParser
{
    ITestFilter Parse(string filter);
}
```

`Parse` returns an `ITestFilter`. The `ToNUnitXml()` extension method, backed by
[`NUnitFilterXmlWriter`](../src/NUnitTestAdapter/TestFilterConverter/NUnitFilterXmlWriter.cs),
produces the NUnit filter XML. The parser
does not return an NUnit engine `TestFilter`, invoke the engine, or change the adapter's filter
selection path. A null input or invalid filter throws `TestFilterParserException`; syntax errors
include a position in the input.

The supporting model is in
`NUnit.VisualStudio.TestAdapter.TestFilterConverter.FilterModel`. Its contracts are
[`ITestFilter`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilter.cs),
[`ITestFilterExpression`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterExpression.cs),
[`ITestFilterTerm`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterTerm.cs),
[`ITestFilterFactor`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterFactor.cs),
[`ITestFilterGroup`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterGroup.cs),
[`ITestFilterNegation`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterNegation.cs),
[`ITestFilterCondition`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterCondition.cs),
and the
[`TestFilterComparison`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/TestFilterComparison.cs) enum.
Together they represent the filter as an expression, terms, factors and conditions:

```text
ITestFilter
  └── ITestFilterExpression (OR of terms)
        └── ITestFilterTerm (AND of factors)
              └── ITestFilterFactor
                    ├── ITestFilterGroup (nested expression)
                    ├── ITestFilterNegation (negated factor)
                    └── ITestFilterCondition (property, comparison, value)
```

The concrete model types take their contents in their constructors and expose read-only
properties. A condition's `TestFilterComparison` is `Exact` for `=` or `Contains` for `~`. The `!=`
and `!~` operators are represented as a condition wrapped in an `ITestFilterNegation`. The model
holds no operator syntax. Operators belong to a filter syntax, so each parser maps its own
symbols to the model. The VSTest and legacy parsers share the same syntax, and the internal
`VSTestFilterOperators.CreateCondition` builds the right factor from an operator symbol.

The model also holds no output format. Every node implements
[`ITestFilterNode`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterNode.cs),
whose `Accept` method calls the matching method of an
[`ITestFilterVisitor<T>`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterVisitor.cs).
A visitor gives the filter a meaning for one target, such as NUnit XML today or NUnit JSON or an
MTP evaluator later, and must handle every node type.

## Grammar and parsing

The parser implements this precedence and grouping:

```text
expression := term ( '|' term )*
term       := factor ( '&' factor )*
factor     := '(' expression ')' | condition
condition  := name op value | value
op         := '=' | '!=' | '~' | '!~'
```

An unescaped parenthesis is always a grouping delimiter; an escaped parenthesis is data. There
are no quoted strings, parenthetical-value heuristics, or `!(...)` group negation. A condition
with no property/operator defaults to `FullyQualifiedName` with the `~` (contains) operation.

Parsing has two logical passes:

1. Split on unescaped `(`, `)`, `&` and `|`, preserving each piece's source position.
2. Split each condition on unescaped `=`, `!=`, `~` and `!~`; trim the resulting name, operator
   and value boundaries, then unescape the parts.

The recognized escape characters are exactly `\`, `(`, `)`, `&`, `|`, `=`, `!` and `~`.
Escaping suppresses structural splitting in the first two passes; unescaping later yields the
literal character. An unsupported escape or trailing backslash is an error. Whitespace at
condition boundaries is trimmed, while whitespace inside a value is preserved.

Recursive descent parses the split tokens according to the grammar. Empty input/groups,
unbalanced parentheses, missing sides of `&` or `|`, missing condition properties/values,
unexpected tokens, and invalid escapes are rejected with `TestFilterParserException` and a
reported input position.

## NUnit XML mapping

`NUnitFilterXmlWriter` writes each model node as follows:

| Model node | XML behavior |
|---|---|
| `ITestFilter` | Wraps the expression in `<filter>`. |
| Expression | A single term is emitted directly; multiple terms use `<or>`. |
| Term | A single factor is emitted directly; multiple factors use `<and>`. |
| Group | Emits its nested expression without an additional wrapper. |
| Negation | Wraps its operand in `<not>`. |
| Condition | Emits the property element, with the regex attribute for a contains comparison. |

Condition property names map as follows:

| Filter property | NUnit XML |
|---|---|
| `FullyQualifiedName` | `<test>` |
| `Name` | `<name>` |
| `Category` or `TestCategory` | `<cat>` |
| Any other property, including `Priority` and `Property` | `<prop name='…'>` |

`~` and `!~` (a `Contains` comparison) set `re='1'` and escape regex metacharacters in the value,
because NUnit XML has no contains element. `!=` and `!~` wrap the property element in `<not>`.
The writer XML-escapes the value and generic property name; VSTest escape processing belongs to
the parser, not the writer.

For example:

```text
Input:  (Name=First|Name=Second)&Priority~a.b
Output: <filter><and><or><name>First</name><name>Second</name></or><prop name='Priority' re='1'>a\.b</prop></and></filter>
```

## Compatibility and integration boundary

This parser intentionally treats unescaped parentheses as grouping, so filters accepted by the
legacy parser as literal names may need escaping or may now be rejected. It also rejects malformed
structure rather than relying on permissive or ambiguous interpretation, and it has no `!(...)`
group negation. With the default `DefaultTestFilterParser`, such a filter goes to
`LegacyTestFilterParser` and gets the filter it got before. A filter both parsers accept gives the
same NUnit XML from either parser, as far as the conformance tests and the round-trip corpus show,
so the default is not expected to change which tests a filter selects.

The production call sites (`NUnit3TestDiscoverer` and `NUnitTestFilterBuilder`) take their parser
from `NUnitTestAdapter.FilterParser`. The test platform creates the discoverer and executor, so
that property, set in `Initialize` from the run settings, is the one place where the parser is
chosen; `NUnitTestFilterBuilder` receives it through its constructor.

### Selecting the parser

The `FilterParser` run setting selects the parser:

| Value | Parser |
|-------|--------|
| `Default` (the default) | `DefaultTestFilterParser`: `VSTestFilterParser`, then `LegacyTestFilterParser` for a filter it rejects |
| `VSTest` | `VSTestFilterParser` only; a filter it rejects is an error |
| `Legacy` | `LegacyTestFilterParser` only, the 6.x behavior |

In a `.runsettings` file:

```xml
<RunSettings>
  <NUnit>
    <FilterParser>VSTest</FilterParser>
  </NUnit>
</RunSettings>
```

With VSTest it can also be given on the command line, like any NUnit run setting:

```text
dotnet test --filter "FullyQualifiedName=A.B" -- NUnit.FilterParser=VSTest
```

`DefaultTestFilterParser` takes an ordered list of parsers, so another parser can be added to the
chain. Each parser that rejects the filter with a `TestFilterParserException` is logged at Info
level, with the filter and the reason, and the next parser is tried. When every parser rejects
the filter, the last parser's exception is thrown, so with the default order the error is the
legacy parser's, as before. Other exceptions are not caught.

`Microsoft.TestPlatform.Filter.Source` is used by tests as a VSTest reference; it is not added as
a product dependency. The parser owns tokenization and conversion to NUnit XML. The NUnit engine
filter is not constructed here, and existing adapter responsibilities such as explicit-test
exclusion and assembly selection limits remain outside this API.

## Verification in this branch

[`FilterModelTests`](../src/NUnitTestAdapterTests/TestFilterConverterTests/FilterModelTests.cs)
cover the model, the operator mapping and NUnit XML writing, including that the writer handles
every `TestFilterComparison`.
[`TestFilterParserConformanceTests`](../src/NUnitTestAdapterTests/TestFilterConverterTests/TestFilterParserConformanceTests.cs)
cover shared behavior against `LegacyTestFilterParser`,
intentional differences, malformed input and error positions, VSTest matching of representative
escaped filters, and selection of the round-trip full-name corpus from NUnit XML. The intended
comparison is not that the legacy parser and VSTest are universal oracles: compatibility cases
and deliberate grammar differences are kept separate.

[`DefaultTestFilterParserTests`](../src/NUnitTestAdapterTests/TestFilterConverterTests/DefaultTestFilterParserTests.cs)
cover the fallback order, the logging, which error is thrown, and which parser each `FilterParser`
value selects.

Making `VSTest` the default, without the legacy fallback, is still open. It would break filters
that only the legacy parser accepts, such as `FullyQualifiedName=A.B(1,2)` and `!(Name=A)`, so
it belongs in a major version, after the Info messages from the fallback show which filters people
use.

🤖 Made by @PanzerFowst with [GitHub Copilot](https://github.com/features/copilot)
