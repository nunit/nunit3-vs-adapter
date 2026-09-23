# Test filter parsing — v7 plan (grammar alignment)

**Status:** proposed
**Scope:** NUnit3TestAdapter 7.0 — contains a breaking change
**Companion document:** [TestFilterParsing-6.x.md](TestFilterParsing-6.x.md) — the non-breaking fixes
**Related:** [#505](https://github.com/nunit/nunit3-vs-adapter/issues/505) (parent), [#1405](https://github.com/nunit/nunit3-vs-adapter/issues/1405), [#1488](https://github.com/nunit/nunit3-vs-adapter/issues/1488), [#1489](https://github.com/nunit/nunit3-vs-adapter/pull/1489), [#1490](https://github.com/nunit/nunit3-vs-adapter/issues/1490), [#1491](https://github.com/nunit/nunit3-vs-adapter/pull/1491), [#1501](https://github.com/nunit/nunit3-vs-adapter/issues/1501)

## The goal, stated as an invariant

> Any test that `dotnet test --filter "<f>" --list-tests` lists must be runnable with
> `dotnet test --filter "<f>"`.

The adapter does not satisfy this today. Discovery goes through the test platform's own filter
implementation; execution goes through the adapter's parser. The two disagree, so tests are
visible but unrunnable. Every issue in category A of [#505](https://github.com/nunit/nunit3-vs-adapter/issues/505) is an instance of that disagreement.

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
  is why [#1488](https://github.com/nunit/nunit3-vs-adapter/issues/1488) appeared to be fixed by [#1489](https://github.com/nunit/nunit3-vs-adapter/pull/1489), which changed only the unescape step.
- An unbalanced `(` leaves the reassembly loop with no terminating condition ([#1501](https://github.com/nunit/nunit3-vs-adapter/issues/1501)).
- Whitespace before `(` defeats the adjacency test that decides whether a `(` starts an argument
  list or a grouping expression ([#1490](https://github.com/nunit/nunit3-vs-adapter/issues/1490)).
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
The Test Explorer filter in [#1405](https://github.com/nunit/nunit3-vs-adapter/issues/1405) is an example:

```
NUnitIssue.TestIssue.TestLength("This | That",False)
```

There is no parse of that string which recovers the intended test name, because the `|` is
ambiguous by construction. For these, the answer is `ConvertMsFilterToNUnitFilter`: rather than
parsing the filter, evaluate it against the loaded test cases with the platform's own
`MatchTestCase` and build an explicit list filter from the survivors. `NUnitTestFilterBuilder`
already falls back to this path, and v7 should keep it and make the fallback deliberate and
logged rather than incidental.

## The other half: the names we report have to be escapable

Fixing the parser addresses filters on the way *in*. There is a second failure mode that it
cannot touch, and [#1349](https://github.com/nunit/nunit3-vs-adapter/issues/1349) and
[#1377](https://github.com/nunit/nunit3-vs-adapter/issues/1377) are both instances of it.

Under the Microsoft Testing Platform the filter is unescaped by `FilterHelper.Unescape` inside
the VSTest bridge, in `ContextAdapterBase.GetTestCaseFilter`. We reach that from
`VsTestFilter.MsTestCaseFilterExpression`, which is a property that simply calls
`runContext.GetTestCaseFilter(...)`. When the incoming filter contains a malformed escape
sequence, the bridge throws `TestPlatformFormatException` there — **before `TestFilterParser`
is ever constructed**. No change to the tokenizer or the parser can affect this.

For [#1349](https://github.com/nunit/nunit3-vs-adapter/issues/1349) the test is:

```csharp
[TestCase("\"C:\\Path\\File.txt\"")]
[TestCase("C:\\Path\\File.txt\"")]
public void Test(string input) { }
```

and the filter that arrives is:

```
QuotesTests.Test\("C:\\\Path\\\File.txt\\""\)
```

That string is not a correct escaping of anything. `\\` consumes a pair, leaving `\P`, which is
not one of the recognised escapes, so `Unescape` throws exactly the error the issue reports.

The name itself is not the problem. Escaping NUnit's rendering of that test correctly gives:

```
QuotesTests.Test\("\\"C:\\\\Path\\\\File.txt\\""\)
```

which unescapes cleanly back to the original full name. So the defect is in the *construction*
of the filter, upstream of us. That the issue only reproduces when both `[TestCase]` attributes
are present points at the filter being built over a set of names rather than one, but the
mechanism has not been confirmed and a captured repro is needed before blaming a specific
component.

Three things follow for v7, none of which the parser rewrite provides:

1. **Treat round-trippability as a discovery-side invariant.** Every `FullyQualifiedName` we
   report should satisfy `Unescape(Escape(name)) == name`. That is a property we own and can
   test directly over the names discovery produces, rather than waiting for a filter to come
   back and fail. `FilterRoundTripConformanceTests` already asserts it for a corpus; the
   equivalent check belongs in discovery.

2. **Do not let the bridge's exception escape as an adapter crash.** `MsTestCaseFilterExpression`
   should catch `TestPlatformFormatException`, log the offending filter, and degrade — either to
   running everything or to a clear "this filter could not be read" message. Today the user sees
   a `StreamJsonRpc.RemoteInvocationException` stack with no indication that a filter is at
   fault.

3. **Establish where the malformed filter comes from.** Until that is known, the two issues
   cannot be closed, only worked around. This needs a captured filter string from a live Test
   Explorer session, not reasoning from the reports.

This is the seam between category A and category B in
[#505](https://github.com/nunit/nunit3-vs-adapter/issues/505): the name is mangled, but the
mangling happens while the *identity* is being turned into a filter, not while the filter is
being parsed.

## What this does and does not fix in [#505](https://github.com/nunit/nunit3-vs-adapter/issues/505)

An earlier draft of this document claimed the parser rewrite fixes category A outright. Writing
the conformance corpus disproved that: several category A issues round-trip through the current
parser perfectly well, so whatever is wrong with them is somewhere else. The breakdown below is
what the tests actually show, and is the honest version.

**Category A — FQN parsing and special characters (14 issues).**

- Fixed by the rewrite: [#1488](https://github.com/nunit/nunit3-vs-adapter/issues/1488)
  (escaped operators outside an argument list),
  [#1490](https://github.com/nunit/nunit3-vs-adapter/issues/1490) (space before `(`),
  [#876](https://github.com/nunit/nunit3-vs-adapter/issues/876) (spaces in a name with no
  argument list at all — a distinct shape from 1490, and one the parenthesis heuristic cannot
  even be blamed for).
- Fixed by bounding the loop, a 6.x item:
  [#1501](https://github.com/nunit/nunit3-vs-adapter/issues/1501).
- Needs XML validity, not parsing: [#761](https://github.com/nunit/nunit3-vs-adapter/issues/761).
  U+FFFF is not a legal XML character, so the emitted filter is a correct string that will not
  load as a document. Escaping the five XML metacharacters does not help.
- Not parser defects at all — these already round-trip correctly today, and now have passing
  tests proving it: [#1405](https://github.com/nunit/nunit3-vs-adapter/issues/1405),
  [#807](https://github.com/nunit/nunit3-vs-adapter/issues/807),
  [#1097](https://github.com/nunit/nunit3-vs-adapter/issues/1097),
  [#654](https://github.com/nunit/nunit3-vs-adapter/issues/654),
  [#1437](https://github.com/nunit/nunit3-vs-adapter/issues/1437). For 1437 this matches the
  issue's own dump, which shows a correctly built filter and zero tests discovered, so the defect
  is downstream in NUnit's `<test>` matching. For 1097 and 654 the Test Explorer class name comes
  from NUnit's `classname` attribute rather than from splitting the full name, so the `).`
  grouping problem is in another layer again. Each needs its own analysis.
- Thrown before the parser runs: [#1349](https://github.com/nunit/nunit3-vs-adapter/issues/1349)
  and [#1377](https://github.com/nunit/nunit3-vs-adapter/issues/1377) — see the section above.
- Unresolved here: [#782](https://github.com/nunit/nunit3-vs-adapter/issues/782) (tuple
  `TestCaseSource`, an identity problem),
  [#935](https://github.com/nunit/nunit3-vs-adapter/issues/935) (marked External), and
  [#742](https://github.com/nunit/nunit3-vs-adapter/issues/742), whose repro is a gist that no
  longer says which characters were involved.

**Category B — discovery/execution identity mismatch (6 issues).** Not fixed, and mostly not
related. These are about the full name the adapter *reports* at discovery versus what NUnit's own
filter engine matches — `SetName`, `TestFixtureSource`, phantom entries, seed drift. A correct
tokenizer does not touch them. The one point of contact is the round-trippability invariant
described in the previous section, which sits on the discovery side.

**Category C — filter semantics and selection rules (5 issues).** Not fixed, and not related.
`[Explicit]`, `AssemblySelectLimit` and `[Platform]` are policy questions about what a filter
should select, not about how it is read.

So the parser rewrite closes four issues outright and clears the ground under several more by
showing they are not parsing problems. That is less than "half of 505", and framing it as a fix
for all of category A would leave real defects without an owner.

## Suggested sequence

1. Land the 6.x items first. They are independent of the grammar and stop the crashes.
2. Build the conformance corpus and the equivalence test against `FilterExpressionWrapper`,
   asserting on the **current** parser. It will fail; the failures are the specification.
3. Replace the tokenizer, and make the corpus pass.
4. Point `FullyQualifiedNameFilterParser` at the same parser and delete its private unescape.
5. Make the `ConvertMsFilterToNUnitFilter` fallback explicit and logged, so a filter the adapter
   declines to parse degrades to matching rather than to an error.
6. Guard `VsTestFilter.MsTestCaseFilterExpression` so a malformed incoming filter is reported as
   a filter problem instead of an opaque RPC exception, and assert the round-trip invariant over
   discovered names.
7. Extend `FilterSpecialCharacterTests` with the quoted-argument and whitespace-before-`(` shapes,
   and note the `Name=Foo(1)` migration in the release notes.

## Acknowledgement

The analysis of the escaping model, and the argument that the special characters should be split
on before the blobs between them are unescaped, is due to @PanzerFowst in [#1501](https://github.com/nunit/nunit3-vs-adapter/issues/1501), following the
investigations in [#1488](https://github.com/nunit/nunit3-vs-adapter/issues/1488), [#1490](https://github.com/nunit/nunit3-vs-adapter/issues/1490) and the PRs [#1489](https://github.com/nunit/nunit3-vs-adapter/pull/1489) and [#1491](https://github.com/nunit/nunit3-vs-adapter/pull/1491).

Claude Opus 5 has been assisting in investigating and building these documents.
