# Debugging the Adapter

## Debugger leaves current thread and jumps to MTP main routine

### Symptom

When single-stepping through adapter code in VS (or `dotnet test`), the debugger stops following
the current thread at a deterministic point — always the same line — and jumps into MTP's main
message-handling routine. In `dotnet test` this manifests as an abort instead of a thread switch.

### Root cause

`LogToDump` calls `TestLog.Debug(...)` (`NUnit3TestExecutor.cs:926`), where `TestLog` is the VS
test platform's `IFrameworkHandle`/`IMessageLogger`. Calling `SendMessage` through this interface
is a cross-thread call into the MTP infrastructure — it signals MTP's message pump on another
thread. The VS debugger's default behaviour is to follow thread switches, so it jumps to the MTP
dispatcher thread the moment that thread wakes up to handle the message.

The failure is deterministic because it always happens at the first `LogToDump` call that actually
invokes `TestLog` — typically `LogToDump("SetupPhase", ...)` at line 200 in the sources overload.

This is not a bug in the adapter code. It is the debugger's thread-following behaviour reacting to
a legitimate cross-thread message.

### Workarounds

**Freeze MTP threads (recommended)**

Open the Threads window in VS (Debug → Windows → Threads). Before stepping into the `LogToDump`
area, right-click each thread that is not your adapter thread and select Freeze. The debugger
cannot follow a frozen thread, so it stays on your thread.

**Pin your thread**

In the Threads window, right-click your current thread and select "Switch to Thread". VS will
prefer to stay on it when stepping.

**Set a breakpoint past the problem area**

If you only need to inspect state after the `LogToDump` calls, set a breakpoint on the next
statement of interest (e.g. `CreateTestFilterBuilder` at line 229 in the sources overload) and
let the run continue to it rather than single-stepping through `LogToDump`.

---

## `dotnet test` aborts the test host while debugging

### Symptom

The test host process is killed mid-session with an abort message from `dotnet test`, even though
no test has timed out individually.

### Root cause

The `.runsettings` setting `TestSessionTimeout` sets a wall-clock deadline for the entire test
session. The default in the `testcontextissues` runsettings is 10 000 ms (10 seconds), which is
far too short to survive any meaningful debugging session.

```xml
<TestSessionTimeout>10000</TestSessionTimeout>
```

When the debugger holds the testhost thread (breakpoints, single-stepping), the session clock
keeps running. Once the deadline passes, the test platform kills the process regardless of what
the debugger is doing.

### Fix

Set `TestSessionTimeout` to 1 000 000 ms (1 000 seconds) while debugging, or to `0` for no
limit at all:

```xml
<!-- 1 000 seconds — enough for any debugging session -->
<TestSessionTimeout>1000000</TestSessionTimeout>
```

Remember to restore a sensible value (or remove the element entirely to use the platform default)
before committing the runsettings file.
