# Test filter parsing — 6.x plan (non-breaking fixes)

**Status:** proposed
**Scope:** NUnit3TestAdapter 6.x
**Companion document:** [TestFilterParsing-v7.md](TestFilterParsing-v7.md) — the grammar rewrite
**Related:** [#505](https://github.com/nunit/nunit3-vs-adapter/issues/505) (parent), [#1405](https://github.com/nunit/nunit3-vs-adapter/issues/1405), [#1488](https://github.com/nunit/nunit3-vs-adapter/issues/1488), [#1489](https://github.com/nunit/nunit3-vs-adapter/pull/1489), [#1490](https://github.com/nunit/nunit3-vs-adapter/issues/1490), [#1491](https://github.com/nunit/nunit3-vs-adapter/pull/1491), [#1501](https://github.com/nunit/nunit3-vs-adapter/issues/1501)

## Purpose

This document lists the filter-parsing defects that can be fixed inside 6.x **without changing
the accepted filter grammar**. Everything that requires a grammar change — and therefore a
breaking change — is deliberately excluded and tracked in the v7 document instead.

The rule applied here: a 6.x fix may turn a crash into a correct result or into a clear error
message, but it must not change which filter strings are accepted or what they select.

## Background in one paragraph

The adapter converts a VSTest filter string into an NUnit filter XML document. The lexer that
does this (`src/NUnitTestAdapter/TestFilterConverter/Tokenizer.cs`) splits the filter on the
operator characters `= ~ ! ( ) & |` **without consulting a preceding backslash**, and then tries
to repair the damage with a parenthesis-balancing heuristic that reassembles parameterised test
names. That design is the root cause of the whole `FullyQualifiedName` family of issues under
[#505](https://github.com/nunit/nunit3-vs-adapter/issues/505), and replacing it is a v7 task. The items below are the subset that can be fixed now.

## Item 1 — Unbalanced `(` runs to OutOfMemory ([#1501](https://github.com/nunit/nunit3-vs-adapter/issues/1501))

`Tokenizer.CollectBalancedParentheticalExpression` loops `while (depth > 0)` and never checks for
end of input, so `GetChar()` returns `EOF_CHAR` forever while the `StringBuilder` keeps growing.

A filter naming a test whose display name ends in an unclosed `(` — which `TestCaseData.SetName`
allows and which VSTest itself discovers without complaint — takes the whole test host down:

```
An exception occurred while invoking executor 'executor://nunit3testexecutor/':
Insufficient memory to continue the execution of the program.
   at System.Text.StringBuilder.ExpandByABlock(Int32 minBlockCharCount)
   at NUnit.VisualStudio.TestAdapter.TestFilterConverter.Tokenizer.CollectBalancedParentheticalExpression
```

**Fix:** terminate the loop when `NextChar` is `EOF_CHAR`, even if `depth` never reaches zero.

**Behaviour after the fix:** the token ends at end of input. Whether the resulting filter then
matches the test is a grammar question and belongs to v7 — but the process no longer dies, and
the failure mode becomes "no test matches" or a `TestFilterParserException`, both of which are
recoverable.

**Non-breaking:** the loop currently has no terminating path for this input, so no existing
behaviour depends on it.

## Item 2 — Double-unescaping inside quoted arguments

`Tokenizer.CollectQuotedString` consumes a backslash at lex time:

```csharp
var ch = GetChar();

if (ch == '\\')
    ch = GetChar();   // the backslash is dropped here
else if (ch == '"')
    break;
sb.Append(ch);
```

Since [#1489](https://github.com/nunit/nunit3-vs-adapter/pull/1489), `TestFilterParser.UnEscape` calls `FilterHelper.Unescape` on the finished token, so
escape sequences inside a quoted argument are now unescaped **twice**. Verified against the
current `main`:

```
filter   FullyQualifiedName=Foo.Bar\("C:\\\\Temp"\)
expected <test>Foo.Bar("C:\\Temp")</test>
actual   <test>Foo.Bar("C:\Temp")</test>            -> silently matches nothing

filter   FullyQualifiedName=Foo.Bar\("a\"b"\)
expected <test>Foo.Bar("a\"b")</test>
actual   <test>Foo.Bar("a"b")</test>                -> silently matches nothing

filter   FullyQualifiedName=A.B.C("C:\\Temp")
actual   throws ArgumentException: Filter string 'A.B.C("C:\Temp")'
         includes unrecognized escape sequence
```

These affect any test whose arguments are strings containing a backslash or a quote, which NUnit
renders into the display name in escaped form.

The acceptance tests added in [#1489](https://github.com/nunit/nunit3-vs-adapter/pull/1489) do not catch this because they use the unquoted
`Backslash\(C:\\Temp\)` shape, where `CollectQuotedString` is never entered.

**Fix:** stop interpreting escapes in `CollectQuotedString`. Collect characters verbatim and let
the single `FilterHelper.Unescape` call in `TestFilterParser` be the only unescaping step. The
backslash still has to suppress the closing-quote check so that `\"` does not end the string
early — it just must not be removed from the collected text.

**Non-breaking:** the current output for these inputs is either a filter that matches nothing or
an unhandled exception. There is no working behaviour to preserve.

## Item 3 — `ArgumentException` from `FilterHelper.Unescape` escapes the adapter

`FilterHelper.Unescape` throws `ArgumentException` when it meets a backslash that is not followed
by one of `\ ( ) & | = ! ~`. Because the lexer splits before it unescapes, a correctly escaped
operator **outside** a parenthesised argument list leaves a dangling backslash at the end of a
token, and the exception reaches the user as an executor crash:

```
filter  FullyQualifiedName=A.B\|C
tokens  Word:FullyQualifiedName  Symbol:=  Word:A.B\  Symbol:|  Word:C
result  ArgumentException: Filter string 'A.B\' includes unrecognized escape sequence
```

The same happens for `\&`, and for a trailing `\)` that follows a completed argument list.

This is new since [#1489](https://github.com/nunit/nunit3-vs-adapter/pull/1489); before that commit `UnEscape` was a pair of `string.Replace` calls that
could not throw, and the same inputs produced a wrong-but-silent filter. Fixing the underlying
split is a v7 change, but the exception type should not be left as it is in a 6.x release.

**Fix:** catch `ArgumentException` around the `FilterHelper.Unescape` call and rethrow it as a
`TestFilterParserException` naming the offending token and its position, so it is reported as a
filter error rather than an adapter crash.

**Non-breaking:** only the exception type and message change. Both cases already fail.

## Item 4 — `FullyQualifiedNameFilterParser.Unescape` does not implement VSTest escaping

The MTP path in `NUnitTestFilterBuilder.ConvertVsTestFilterToNUnitFilterForMTP` uses a third,
independent unescape implementation built from `WebUtility.HtmlDecode` followed by
`Regex.Unescape`. Neither is the VSTest escaping scheme.

The correct property is that escaping a name and unescaping it again is lossless, which
`MtpFastPathUnescapeRoundTrips` asserts. Measured against the current `main`, that round trip
**holds** for most shapes, including names containing a literal backslash-n or backslash-t —
`FilterHelper.Escape` doubles the backslash, and `Regex.Unescape` then collapses the pair back
correctly. The one round-trip failure is the HTML entity:

```
Foo.Bar(a &amp; b)  -> Foo.Bar(a & b)     HtmlDecode corrupts an entity that is part of the name
```

So the practical exposure is narrower than the implementation suggests, and the remaining
hazards are all about input that is not perfectly escaped — which is exactly what some filter
producers emit:

```
Foo.Bar("a\nb")     -> Foo.Bar("a<real newline>b")     a lone backslash-n is interpreted
Foo.Bar(\d)         -> throws RegexParseException      rather than degrading gracefully
Foo.Bar(x)\         -> Foo.Bar(x)                      trailing backslash silently dropped
```

`Regex.Unescape` throwing is the worst of these, because it reaches the user as an adapter
crash rather than as a filter that selects nothing.

**Fix:** replace the body with `FilterHelper.Unescape`, guarded as in item 3.

**Non-breaking in intent, but worth a careful look.** `HtmlDecode` was presumably added for a
reason and the change should be validated against the MTP acceptance tests before it ships. If
that turns out to be load-bearing, defer the whole item to v7 and fold it into the single shared
parser described there — it is not worth a risky fix in a patch release.

## Explicitly out of scope for 6.x

- **[#1490](https://github.com/nunit/nunit3-vs-adapter/issues/1490), whitespace before `(`.** `GetWordOrFqn` only absorbs an argument list when `(`
  immediately follows the word characters, so `Fixture.Method (Case 1)` tokenises as two tokens.
  PR [#1491](https://github.com/nunit/nunit3-vs-adapter/pull/1491) proposed peeking past the whitespace. That patches one heuristic with another, and the
  author withdrew it on those grounds. The correct fix is the v7 grammar, where an unescaped `(`
  is always a grouping operator and whitespace inside a value is never significant.

- **Accepting unescaped filters that VSTest itself rejects.** Some producers emit filters without
  escaping — the Test Explorer filter in [#1405](https://github.com/nunit/nunit3-vs-adapter/issues/1405) is one. No correct parser can resolve those; they
  need either the producer fixed or the `ConvertMsFilterToNUnitFilter` fallback, which matches
  against the loaded test cases using the test platform's own matcher.

- **Any change to which filter strings are accepted.** That is the v7 discussion.

## Verification

The failing tests that specify these fixes already exist:

- `src/NUnitTestAdapterTests/TestFilterConverterTests/FilterRoundTripConformanceTests.cs` —
  the invariant, with every expectation derived from the test platform's own filter
  implementation rather than hand-written.
- `src/NUnitTestAdapterTests/TestFilterConverterTests/FilterParsingDefectTests.cs` — narrow
  demonstrations pinned to the method responsible for each defect.
- `src/NUnit.TestAdapter.Tests.Acceptance/FilterKnownDefectsTests.cs` — the same failures end
  to end, through a real `dotnet test` and `vstest` invocation.

Work item by item and turn them green; do not weaken an expectation without saying why.

For item 1, bound the test with a timeout — an assertion that never returns is not a useful
failure.

## Acknowledgement

Claude Opus 5 has been assisting in investigating and building these documents.
