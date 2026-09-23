# Test filter parsing — v7 plan (grammar alignment)

**Status:** proposed
**Scope:** NUnit3TestAdapter 7.0 — contains a breaking change
**Companion document:** [TestFilterParsing-6.x.md](TestFilterParsing-6.x.md) — the non-breaking fixes
**Related:** #505 (parent), #1405, #1488, #1489, #1490, #1491, #1501

## The goal, stated as an invariant

> Any test that `dotnet test --filter "<f>" --list-tests` lists must be runnable with
> `dotnet test --filter "<f>"`.

The adapter does not satisfy this today. Discovery goes through the test platform's own filter
implementation; execution goes through the adapter's parser. The two disagree, so tests are
visible but unrunnable. Every issue in category A of #505 is an instance of that disagreement.

## Why the current implementation is wrong

The test platform's filter grammar is defined in `Microsoft.TestPlatform.Filter.Source` — the
same package the adapter's test project already references, and the one
`Microsoft.Testing.Extensions.VSTestBridge` uses. It is three steps:

1. `FilterExpression.TokenizeFilterExpressionString` splits the expression on **unescaped**
   `(`, `)`, `&` and `|`. Escape tracking is a single `last == '\\'` check.
2. `Condition.TokenizeFilterConditionString` splits each condition on **unescaped** `=`, `!=`,
   `~` and `!~` into either one part (a bare value) or three.
3. `FilterHelper.Unescape` converts the escape sequences in the value. The recognised special
   characters are exactly `\ ( ) & | = ! ~`, and a backslash before anything else is an error.

Whitespace is only `Trim()`ed at token boundaries. There is no notion of a fully qualified name,
no parenthesis balancing, and no quoted strings. A value is an opaque blob delimited by unescaped
operators. When only a value is given, the defaults are `FullyQualifiedName` and `Contains`.

The adapter's `Tokenizer` does something structurally different. `WORD_BREAK_CHARS = "=~!()&|"`
and `IsWordChar` never look at a preceding backslash, so the value is split **before** escaping is
considered. `TokenKind.FQN`, `CollectBalancedParentheticalExpression` and `CollectQuotedString`
exist only to reassemble what that premature split broke apart.

That is the defect. It is not a collection of edge cases; it is one wrong layering, and the known
symptoms all follow from it:

- An escaped operator survives only when it happens to sit inside a balanced `(...)` group. This
  is why #1488 appeared to be fixed by #1489, which changed only the unescape step.
- An unbalanced `(` leaves the reassembly loop with no terminating condition (#1501).
- Whitespace before `(` defeats the adjacency test that decides whether a `(` starts an argument
  list or a grouping expression (#1490).
- Whether `(` is an operator or a value character depends on what precedes it, not on whether it
  is escaped — so the same character means two different things depending on context.

### Measured against the current `main`

Each of these filters is escaped the way the [character escaping
rules](https://learn.microsoft.com/dotnet/core/testing/selective-unit-tests?pivots=nunit#character-escaping)
prescribe, and each is accepted by the test platform at discovery:

```
FullyQualifiedName=A.B\|C
  -> ArgumentException: Filter string 'A.B\' includes unrecognized escape sequence

FullyQualifiedName=A.B\&C
  -> ArgumentException: Filter string 'A.B\' includes unrecognized escape sequence

FullyQualifiedName=Foo.Bar\("C:\\\\Temp"\)
  -> <test>Foo.Bar("C:\Temp")</test>          one backslash short, matches nothing

FullyQualifiedName=Foo.Bar\("a\"b"\)
  -> <test>Foo.Bar("a"b")</test>              backslash lost, matches nothing

FullyQualifiedName=My.Test.Fixture.Method \(Case 1\)
  -> TestFilterParserException: Unexpected FQN '\(Case 1\)' at position 42

FullyQualifiedName=Foo.Bar\(
  -> OutOfMemoryException
```

And the escaped-parenthesis case that does work:

```
FullyQualifiedName=A.B.C\(1\)
  -> <test>A.B.C(1)</test>
```

only works by coincidence: an escaped `(`...`)` pair balances exactly like an unescaped one, so
the balancing heuristic happens to consume the same span of text that a correct parser would.

## Target design

Replace the hand-written lexer with one that implements the grammar above, and route every filter
entry point through it.

There are two ways to get there.

### Option A — consume `Microsoft.TestPlatform.Filter.Source` in the product

The package is already a test-only dependency. Referencing it from `NUnit.TestAdapter` and
driving `TokenizeFilterExpressionString` plus `Condition.Parse` yields `(name, operation, value)`
triples that can be translated directly into NUnit filter XML. Discovery and execution then agree
by construction, because both are running the same code.

The cost is that those types are `[Embedded] internal`, and the package README states that the
only supported usage is `FilterExpressionWrapper` together with `MatchTestCase`. Using the
tokenizer directly is therefore unsupported and could break on a package update.

### Option B — own escape-aware tokenizer, with conformance tests

Write the tokenizer ourselves — it is roughly sixty lines, and the reference implementation is
public — and prove equivalence with a test that asserts our token split equals
`FilterExpressionWrapper`'s over a corpus of filter strings. The test project already references
the package, so the harness for this exists.

**Recommendation: Option B.** The implementation is small and the semantics are fully specified,
so the risk is in drift rather than in the code, and a conformance test is a better guard against
drift than a dependency on internals we are told not to use. Option A remains a reasonable
fallback if the conformance corpus proves hard to keep honest.

Either way, the output stage is unchanged: the existing `EmitFullNameFilter`,
`EmitCategoryFilter`, `EmitNameFilter` and `EmitPropertyFilter` already produce the right NUnit
XML, including regex escaping for the `~` and `!~` operators.

### Consolidate the three parsers

There are currently three independent implementations of filter handling:

| Location | Used by |
| --- | --- |
| `TestFilterConverter/Tokenizer.cs` + `TestFilterParser.cs` | `dotnet test --filter`, VSTest |
| `TestFilterConverter/FullyQualifiedNameFilterParser.cs` | the MTP fast path |
| `NUnitTestFilterBuilder.ConvertMsFilterToNUnitFilter` | legacy discovery, and as a fallback |

The second has its own unescape built from `HtmlDecode` and `Regex.Unescape`, which is not the
VSTest scheme at all and mangles test names containing a literal backslash-n, a literal HTML
entity, or a trailing backslash. Details are in the 6.x document, item 4.

In v7 the first two should share one escape-aware parser. The third is not a parser — it matches
against the already-discovered test cases using the platform's own matcher — and should be kept
deliberately, as the compatibility fallback described below.

## The breaking change, and how to bound it

Making the grammar correct means the adapter stops accepting filters that the test platform
itself rejects. In particular, `--filter "Name=Foo(1)"` with unescaped parentheses currently
works by accident and would have to be written `--filter "Name=Foo\(1\)"`.

Two things make this narrower than it first looks:

- Anything routed through `Condition.Parse` — that is, discovery — **already** rejects the
  unescaped form. Users hitting this are relying on execution being more lenient than discovery,
  which is precisely the inconsistency this work removes.
- The escaped form is the documented one and already works today, so a migration exists and can
  be stated in one line of release notes.

What genuinely cannot be fixed by a correct parser is a *producer* that emits unescaped filters.
The Test Explorer filter in #1405 is an example:

```
NUnitIssue.TestIssue.TestLength("This | That",False)
```

There is no parse of that string which recovers the intended test name, because the `|` is
ambiguous by construction. For these, the answer is `ConvertMsFilterToNUnitFilter`: rather than
parsing the filter, evaluate it against the loaded test cases with the platform's own
`MatchTestCase` and build an explicit list filter from the survivors. `NUnitTestFilterBuilder`
already falls back to this path, and v7 should keep it and make the fallback deliberate and
logged rather than incidental.

## What this does and does not fix in #505

- **Category A — FQN parsing and special characters (13 issues).** Fixed. This is the single root
  cause, and #1405, #1488, #1490 and #1501 are all instances of it.
- **Category B — discovery/execution identity mismatch (5 issues).** Not fixed, and not related.
  Those are about the FQN string the adapter *reports* at discovery versus what NUnit's own filter
  engine matches — `SetName`, `TestFixtureSource`, phantom entries. A correct tokenizer does not
  touch them, and they need their own analysis.
- **Category C — filter semantics and selection rules (5 issues).** Not fixed, and not related.
  `[Explicit]`, `AssemblySelectLimit` and `[Platform]` are policy questions about what a filter
  should select, not about how it is read.

So this work closes roughly half of #505. Framing it as a fix for all of #505 would be wrong and
would leave the other two categories without an owner.

## Suggested sequence

1. Land the 6.x items first. They are independent of the grammar and stop the crashes.
2. Build the conformance corpus and the equivalence test against `FilterExpressionWrapper`,
   asserting on the **current** parser. It will fail; the failures are the specification.
3. Replace the tokenizer, and make the corpus pass.
4. Point `FullyQualifiedNameFilterParser` at the same parser and delete its private unescape.
5. Make the `ConvertMsFilterToNUnitFilter` fallback explicit and logged, so a filter the adapter
   declines to parse degrades to matching rather than to an error.
6. Extend `FilterSpecialCharacterTests` with the quoted-argument and whitespace-before-`(` shapes,
   and note the `Name=Foo(1)` migration in the release notes.

## Acknowledgement

The analysis of the escaping model, and the argument that the special characters should be split
on before the blobs between them are unescaped, is due to @PanzerFowst in #1501, following the
investigations in #1488, #1490 and the PRs #1489 and #1491.
