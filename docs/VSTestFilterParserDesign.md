# VSTest filter parser API design

## Purpose and status

This document describes the filter parser and NUnit XML model implemented on
`feature/v7-test-filter-parser`. It is intended to make the design and its integration boundary
clear during review.

`VSTestFilterParser` remains isolated from adapter production code. The legacy implementation is
now named `LegacyTestFilterParser`. It retains the existing XML-string API and legacy behavior.
Production call sites continue to use it, so the VSTest-conformant grammar is not yet exposed to
users.

This design follows the escape-aware parsing approach described in
[TestFilterParsing-v7.md](./TestFilterParsing-v7.md), but does not implement that document's
proposed adapter-wide routing or MTP changes.

## Public API

[`VSTestFilterParser`](../src/NUnitTestAdapter/TestFilterConverter/VSTestFilterParser.cs)
implements [`ITestFilterParser`](../src/NUnitTestAdapter/TestFilterConverter/ITestFilterParser.cs):

```csharp
public interface ITestFilterParser
{
    ITestFilter Parse(string filter);
}
```

`Parse` returns an `ITestFilter`, whose `ToXml()` method produces the NUnit filter XML. The parser
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
[`ITestFilterCondition`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterCondition.cs),
and
[`ITestFilterOperation`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/ITestFilterOperation.cs).
Together they represent the filter as an expression, terms, factors and conditions:

```text
ITestFilter
  └── ITestFilterExpression (OR of terms)
        └── ITestFilterTerm (AND of factors)
              └── ITestFilterFactor
                    ├── ITestFilterGroup (nested expression)
                    └── ITestFilterCondition (property, operation, value)
```

The concrete model types take their contents in their constructors and expose read-only
properties. The operation definitions are shared instances for `=`, `!=`, `~` and `!~`, described
by their symbol, whether they negate the result, and whether they use regex matching.

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

Each model node serializes its own part of the filter:

| Model node | XML behavior |
|---|---|
| `ITestFilter` | Wraps the expression in `<filter>`. |
| Expression | A single term is emitted directly; multiple terms use `<or>`. |
| Term | A single factor is emitted directly; multiple factors use `<and>`. |
| Group | Emits its nested expression without an additional wrapper. |
| Condition | Emits the property element, optional regex attribute, and optional negation. |

Condition property names map as follows:

| Filter property | NUnit XML |
|---|---|
| `FullyQualifiedName` | `<test>` |
| `Name` | `<name>` |
| `Category` or `TestCategory` | `<cat>` |
| Any other property, including `Priority` and `Property` | `<prop name='…'>` |

`~` and `!~` set `re='1'` and escape regex metacharacters in the value. `!=` and `!~` wrap the
property element in `<not>`. The value and generic property name are XML-escaped by
[`XmlEscaperHelper`](../src/NUnitTestAdapter/TestFilterConverter/FilterModel/XmlEscaperHelper.cs);
VSTest escape processing belongs to the parser, not the model.

For example:

```text
Input:  (Name=First|Name=Second)&Priority~a.b
Output: <filter><and><or><name>First</name><name>Second</name></or><prop name='Priority' re='1'>a\.b</prop></and></filter>
```

## Compatibility and integration boundary

This parser intentionally treats unescaped parentheses as grouping, so filters accepted by the
legacy parser as literal names may need escaping or may now be rejected. It also rejects malformed
structure rather than relying on permissive or ambiguous interpretation. That behavior is not
currently exposed to adapter users because no production call site selects this parser. The
production call sites continue to use `LegacyTestFilterParser`, which returns NUnit filter XML
and preserves legacy-specific behavior, including `Category` as a generic property and
`!(...)` group negation.

`Microsoft.TestPlatform.Filter.Source` is used by tests as a VSTest reference; it is not added as
a product dependency. The parser owns tokenization and conversion to NUnit XML. The NUnit engine
filter is not constructed here, and existing adapter responsibilities such as explicit-test
exclusion and assembly selection limits remain outside this API.

## Verification in this branch

[`FilterModelTests`](../src/NUnitTestAdapterTests/TestFilterConverterTests/FilterModelTests.cs)
cover model serialization and escaping.
[`TestFilterParserConformanceTests`](../src/NUnitTestAdapterTests/TestFilterConverterTests/TestFilterParserConformanceTests.cs)
cover shared behavior against `LegacyTestFilterParser`,
intentional differences, malformed input and error positions, VSTest matching of representative
escaped filters, and selection of the round-trip full-name corpus from NUnit XML. The intended
comparison is not that the legacy parser and VSTest are universal oracles: compatibility cases
and deliberate grammar differences are kept separate.

The eventual integration decision is still open: route production through the VSTest parser,
possibly using the legacy parser as a compatibility fallback, only after the grammar and
selection behavior are validated.

🤖 Made by @PanzerFowst with [GitHub Copilot](https://github.com/features/copilot)
