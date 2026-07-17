# Filter & Execution Flow Investigation

## Scope

Two entry points: `RunTests(IEnumerable<string> sources)` and `RunTests(IEnumerable<TestCase> tests)`.
Three concrete scenarios (1 fixture, 3 tests):

- **A**: `dotnet test` — no filter
- **B**: `dotnet test --filter "FullyQualifiedName=MyNs.MyFixture.Test1"` — FQN filter, 1 test
- **C**: IDE run — VS passes a specific set of TestCase objects

---

## Q1: Is the pre-execution discovery result used?

Yes, in five distinct ways.

`RunAssembly` calls `NUnitEngineAdapter.Explore(filter)` before execution, converts the XML result into a
`DiscoveryConverter` object, and that object is threaded through everything that follows.

### 1 — Guard: skip execution if nothing is runnable

```csharp
// NUnit3TestExecutor.cs:643
if (discoveryResults.IsRunnable)
{
    var discovery = new DiscoveryConverter(TestLog, Settings);
    discovery.Convert(discoveryResults, assemblyPath);
    ...
    ea.Run(filter, discovery, this);
}
else
{
    TestLog.InfoNoRunnableTests(discoveryResults, assemblyPath);
}
```

If `Explore` returns no runnable tests, `Run` is never called.

### 2 — Guard: skip when `SkipExecutionWhenNoTests` is set

```csharp
// NUnit3TestExecutor.cs:655
if (!Settings.SkipExecutionWhenNoTests || discovery.AllTestCases.Any())
{
    ea.Run(filter, discovery, this);
}
```

Uses `discovery.AllTestCases`.

### 3 — Filter rebuild inside `CheckFilter` (Execution base class)

This is the most significant use. After `VsTestExecution.Run` (or `IdeExecution`) is entered, the filter is
passed through `CheckFilterInCurrentMode` → `CheckFilter`:

```csharp
// Execution.cs:94
protected TestFilter CheckFilter(TestFilter testFilter, IDiscoveryConverter discovery)
{
    if (discovery.NoOfLoadedTestCasesAboveLimit && !testFilter.IsCategoryFilter())
        return TestFilter.Empty;           // too many — run all

    if (discovery.NoOfLoadedTestCases == 0)
        return testFilter;                 // nothing found — keep original

    if (testFilter.IsCategoryFilter() || testFilter.IsPartitionFilter())
        return testFilter;                 // pass through unchanged

    // For everything else: REBUILD the filter from discovered test cases
    var filterBuilder = CreateTestFilterBuilder();
    if (discovery.HasExplicitTests && Settings.ExplicitMode == ExplicitModeEnum.None)
        return filterBuilder.FilterByList(discovery.GetLoadedNonExplicitTestCases());
    return filterBuilder.FilterByList(discovery.LoadedTestCases);
}
```

For a plain FQN filter: `Explore(filter)` discovers only the 1 matching test, so `discovery.LoadedTestCases`
contains just that 1 test. `CheckFilter` discards the original filter string and rebuilds it as a list-based
filter from those 1 discovered test case.

### 4 — VsTestExecution: filter rebuild from loaded test cases (non-NUnit-filter path)

```csharp
// VsTestExecution.cs:28
filter = Settings.DiscoveryMethod == DiscoveryMethod.Current
    ? Settings.UseNUnitFilter
        ? filterBuilder.ConvertVsTestFilterToNUnitFilter(vsTestFilter)    // no discovery
        : filterBuilder.ConvertMsFilterToNUnitFilter(vsTestFilter, discovery) // uses discovery.LoadedTestCases
    : filterBuilder.ConvertMsFilterToNUnitFilter(vsTestFilter, discovery.LoadedTestCases);
```

When `UseNUnitFilter=false`, the VS filter is evaluated against `discovery.LoadedTestCases` to produce the
final NUnit filter.

### 5 — Event listener: result converter comes from discovery

```csharp
// Execution.cs:40
var converter = CreateConverter(discovery);
using var listener = new NUnitEventListener(converter, nUnit3TestExecutor);
NUnitEngineAdapter.Run(listener, filter);
```

`CreateConverter(discovery)` returns either `discovery.TestConverter` or `discovery.TestConverterForXml`. This
converter maps NUnit engine events (test started, test result, etc.) back to VS `TestCase` objects. Without the
discovery, the adapter cannot match engine results to VS test IDs.

---

## Q2 addendum: Is the filter always rebuilt as `FilterByList`?

No. `CheckFilter` (called from `CheckFilterInCurrentMode` in both execution modes) has three distinct branches
based on what the filter XML contains. The detection is a plain text search on `filter.Text`
(`src/NUnitTestAdapter/NUnitEngine/Extensions.cs:43`):

```csharp
public static bool IsCategoryFilter(this TestFilter filter) =>
    filter != TestFilter.Empty && filter.Text.Contains("<cat>");

public static bool IsPartitionFilter(this TestFilter filter) =>
    filter != TestFilter.Empty && filter.Text.Contains("<partition>");
```

### The three branches

```
CheckFilter(filter, discovery)
  ├─ IsCategoryFilter  → pass through unchanged
  │    EXCEPT: may AND in explicit-exclusion clause
  │    based on discovery.HasExplicitTests / IsExplicitRun
  ├─ IsPartitionFilter → pass through, always unchanged
  └─ everything else   → REBUILD as FilterByList(discovery.LoadedTestCases)
```

### Category filter (`dotnet test --filter "TestCategory=Smoke"`)

`TestFilterParser` maps `TestCategory=Smoke` → `<filter><cat>Smoke</cat></filter>`.

`CheckFilter` detects `<cat>` and **does not touch the filter**. It passes straight to `engine.Run` as-is.
Discovery is not used to rebuild it. The only discovery involvement is whether to AND in an explicit-exclusion
clause — consulted via `discovery.HasExplicitTests` and `discovery.IsExplicitRun`.

### Partition filter

Same: passes through unchanged always. Discovery is not consulted at all.

### FQN / name / property filter

No `<cat>` or `<partition>` in the XML → falls to the else branch → **always rebuilt** as
`FilterByList(discovery.LoadedTestCases)`. This is the only case where discovery is essential to the final
filter.

`discovery.LoadedTestCases` is populated by `DiscoveryConverter.Convert(discoveryResults, assemblyPath)`
(NUnit3TestExecutor.cs:646), where `discoveryResults` is the XML returned by `NUnitEngineAdapter.Explore(filter)`
at line 632 — the pre-execution discovery call. So the filter going into `engine.Run` is not the original
parsed VS expression. It is a fresh `<or><test>…` list built from the test cases the engine actually found
during pre-execution discovery.

### Implication for discovery

| Filter type       | Discovery essential for filter? | Filter rebuilt from LoadedTestCases? |
| ----------------- | :-----------------------------: | :----------------------------------: |
| Empty (run all)   | No                              | No                                   |
| Category (`<cat>`)| Minor (explicit-exclusion only) | No                                   |
| Partition         | No                              | No                                   |
| FQN / name / prop | **Yes**                         | **Yes**                              |

---

## Q2: What filter is sent to `engine.Run`?

The `Run` signature is: `NUnitEngineAdapter.Run(ITestEventListener listener, TestFilter filter)`.

Filters are NUnit XML. The general shape is `<filter>...</filter>` wrapping one of:

| XML element              | Meaning                                    |
| ------------------------ | ------------------------------------------ |
| `<test>FQN</test>`       | Exact full name match (= operator)         |
| `<test re='1'>pat</test>`| Regex full name match (~ operator)         |
| `<cat>name</cat>`        | Category match                             |
| `<prop name='X'>v</prop>`| Property match                             |
| `<or>…</or>`             | Logical OR of children                     |
| `<and>…</and>`           | Logical AND of children                    |
| `<not>…</not>`           | Logical NOT                                |
| *(empty)*                | `TestFilter.Empty` — run all               |

### Scenario A: `dotnet test` (no filter)

```
RunType = CommandLineCurrentNUnit (UseNUnitFilter=true) or CommandLineCurrentVSTest
```

1. `ConvertVsTestFilterToNUnitFilter(vsFilter)`: vsFilter expression is null/empty → returns `null`
2. `filter ??= builder.FilterByWhere(Settings.Where)`: no Where setting → `TestFilter.Empty`
3. `VsTestExecution.CheckVsTestFilter`: vsTestFilter is empty → returns filter unchanged
4. `VsTestExecution.CheckFilterInCurrentMode`: filter is `TestFilter.Empty` and vsTestFilter is empty →
   returns `TestFilter.Empty` unchanged

**Filter sent to `engine.Run`: `TestFilter.Empty` (run all 3 tests)**

### Scenario B: `dotnet test --filter "FullyQualifiedName=MyNs.MyFixture.Test1"`

```
RunType = CommandLineCurrentNUnit
```

**Step 1 — initial filter build (before Explore):**

`TestFilterParser.Parse("FullyQualifiedName=MyNs.MyFixture.Test1")`:
- `FullyQualifiedName=value` → `EmitFullNameFilter(EQ_OP, value)` → `<test>MyNs.MyFixture.Test1</test>`
- Wrapped: `<filter><test>MyNs.MyFixture.Test1</test></filter>`
- With `ExplicitMode=None`: combined with explicit exclusion →

```xml
<filter>
  <and>
    <test>MyNs.MyFixture.Test1</test>
    <not><prop name='Explicit'>true</prop></not>
  </and>
</filter>
```

**Step 2 — `Explore` is called with that filter.**

Only `Test1` is discovered. `discovery.LoadedTestCases = [Test1]`.

**Step 3 — `VsTestExecution.CheckVsTestFilter` (UseNUnitFilter=true):**

Calls `ConvertVsTestFilterToNUnitFilter(vsTestFilter)` again → same XML as step 1 (no change).

**Step 4 — `VsTestExecution.CheckFilterInCurrentMode` → `CheckFilter`:**

Filter is not Empty → `CheckFilter` is called. Filter is not a category or partition filter.
`discovery.LoadedTestCases = [Test1]` → calls `FilterByList([Test1])`:

```xml
<filter>
  <test>MyNs.MyFixture.Test1</test>
</filter>
```

(The explicit exclusion clause is gone; the filter is rebuilt purely from discovered test cases.)

**Filter sent to `engine.Run`:**

```xml
<filter><test>MyNs.MyFixture.Test1</test></filter>
```

### Scenario C: IDE run (TestCase overload, all 3 tests selected)

```
RunType = Ide
ExecutionFactory creates IdeExecution
```

1. `FilterByList(assemblyGroup)` with 3 test cases → `<filter><or><test>T1</test><test>T2</test><test>T3</test></or></filter>`
2. `Explore` is called with that filter → discovers all 3 tests
3. `IdeExecution.CheckFilterInCurrentMode`:
   - filter is not Empty → `CheckFilter(filter, discovery)` is called
   - `discovery.LoadedTestCases = [T1, T2, T3]`
   - Rebuilds via `FilterByList([T1, T2, T3])` → same OR list

**Filter sent to `engine.Run`:**

```xml
<filter><or><test>T1</test><test>T2</test><test>T3</test></or></filter>
```

If the number of test cases exceeds `AssemblySelectLimit` (default 1000):
- `IdeExecution.CheckFilterInCurrentMode` → `CheckFilter` → `discovery.NoOfLoadedTestCasesAboveLimit=true` → returns `TestFilter.Empty` instead

---

## Q3: Are test cases sent to `engine.Run`?

**No.** The engine run signature takes only `(ITestEventListener listener, TestFilter filter)`. There is no
test-case list parameter.

Test cases influence execution in exactly two ways, neither of which is passing them to `Run`:

### Way 1 — Converted to a filter (always)

In the `RunTests(IEnumerable<TestCase>)` path:

```csharp
// NUnit3TestExecutor.cs:373
var filter = filterBuilder.FilterByList(assemblyGroup);
RunAssembly(assemblyPath, assemblyGroup, filter, assemblyName);
```

`FilterByList` iterates the test cases, calls `filterBuilder.AddTest(tc.FullyQualifiedName)` for each, and
returns the resulting `TestFilter`. The filter — not the test cases — is what reaches the engine.

In the `RunTests(IEnumerable<string>)` path, `testCases` is `null` throughout. No conversion happens; the
filter is built from the VS run context or the NUnit Where setting.

### Way 2 — Pre-filtered via `PackageSettings.LOAD` (conditional, sources path only)

```csharp
// NUnitTestAdapter.cs:213
if (Settings.PreFilter && testCases != null)
{
    var prefilters = new List<string>();
    foreach (var testCase in testCases)
    {
        int end = testCase.FullyQualifiedName.IndexOfAny(['(', '<']);
        prefilters.Add(end > 0 ? testCase.FullyQualifiedName.Substring(0, end) : testCase.FullyQualifiedName);
    }
    package.Settings[PackageSettings.LOAD] = prefilters;
}
```

When `Settings.PreFilter=true` AND `testCases != null`, the test class names (FQN up to the first `(` or `<`)
are added to the NUnit engine `TestPackage` as a `LOAD` hint. This tells the engine which types to load when
setting up the runner, **before** `Explore` or `Run` is called. It is an optimization, not a selection.

In Scenario B (`dotnet test --filter ...`), `testCases` is `null` (sources overload), so `PreFilter` has no
effect at all.

### Summary table

| Scenario                                    | Test cases to `engine.Run`? | Selection mechanism          |
| ------------------------------------------- | :-------------------------: | ----------------------------|
| A: `dotnet test` (no filter)                | No                          | `TestFilter.Empty`          |
| B: `dotnet test --filter FullyQualifiedName=...` | No                     | `<test>FQN</test>` XML      |
| C: IDE TestCase overload                    | No                          | `<or><test>…</test>…</or>` XML built from FQNs |
| C with PreFilter=true                       | No (but LOAD hint set)      | As above + type pre-load    |

---

## Call chain summary

```
RunTests(sources or testCases)
  └─ [sources] build filter from VsTestFilter / NUnit Where / empty
  └─ [testCases] FilterByList(testCases) → filter
  └─ RunAssembly(assemblyPath, testCases, filter, ...)
       ├─ CreateTestPackage(assemblyPath, testCases)   ← LOAD hint if PreFilter
       ├─ NUnitEngineAdapter.CreateRunner(package)
       ├─ NUnitEngineAdapter.Explore(filter)           ← PRE-EXECUTION DISCOVERY
       ├─ DiscoveryConverter.Convert(discoveryResults) ← builds LoadedTestCases
       └─ ExecutionFactory.Create() → IdeExecution | VsTestExecution
            └─ Execution.Run(filter, discovery, executor)
                 ├─ [VsTest] CheckVsTestFilter(filter, discovery, vsTestFilter)
                 │     may rebuild filter from discovery.LoadedTestCases
                 ├─ CheckFilterInCurrentMode(filter, discovery)
                 │     → CheckFilter(filter, discovery)
                 │         may rebuild filter as FilterByList(discovery.LoadedTestCases)
                 ├─ CreateConverter(discovery)          ← event mapping from discovery
                 └─ NUnitEngineAdapter.Run(listener, filter)   ← ACTUAL ENGINE CALL
                      └─ Runner.Run(listener, filter)
```

---

## Key files

| File                                                         | Role                                      |
| ------------------------------------------------------------ | ------------------------------------------|
| `src/NUnitTestAdapter/NUnit3TestExecutor.cs:173`             | `RunTests(sources)` entry point           |
| `src/NUnitTestAdapter/NUnit3TestExecutor.cs:335`             | `RunTests(testCases)` entry point         |
| `src/NUnitTestAdapter/NUnit3TestExecutor.cs:576`             | `RunAssembly` — orchestrates discovery+run|
| `src/NUnitTestAdapter/NUnitTestAdapter.cs:191`               | `CreateTestPackage` — LOAD hint           |
| `src/NUnitTestAdapter/NUnitTestFilterBuilder.cs`             | All filter construction methods           |
| `src/NUnitTestAdapter/TestFilterConverter/TestFilterParser.cs`| VS filter → NUnit XML translation         |
| `src/NUnitTestAdapter/ExecutionProcesses/Execution.cs`       | Base `Run` + `CheckFilter`                |
| `src/NUnitTestAdapter/ExecutionProcesses/IdeExecution.cs`    | IDE `CheckFilterInCurrentMode`            |
| `src/NUnitTestAdapter/ExecutionProcesses/VsTestExecution.cs` | VSTest `CheckVsTestFilter` + mode check   |
| `src/NUnitTestAdapter/ExecutionProcesses/ExecutionFactory.cs`| Picks `IdeExecution` vs `VsTestExecution` |
