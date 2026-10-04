# crashcheck

Puts every case through a **child process of its own**, so that a crash is a result rather than
the end of the run.

```sh
dotnet run -c Release --project Sources/Tests/Harnesses/CrashCheck              # writes Sources/Tests/Harnesses/crashcheck.md, or HARNESS_REPORTS/crashcheck.md
dotnet run -c Release --project Sources/Tests/Harnesses/CrashCheck -- --operation=simplify
dotnet run -c Release --project Sources/Tests/Harnesses/CrashCheck -- --case="simplify::x * ln(x)"   # one case, in process
```

Exit code is 1 when a case crashed or threw an exception that is not the library declining,
and 0 otherwise. A case that did not finish is reported but does not fail the run: on a shared
runner that is as often the runner's speed as the library's.

## Why a process per case

**A stack overflow cannot be caught in .NET.** It takes the process down, and a test runner that
dies mid-suite reports nothing about the tests it had not reached — so the suite can be green while
the library is not, and the green tick is evidence about neither.

Anything that wants to find that class has to be able to lose a process and carry on. Hence one
child per case: slow, and the only construction that works.

The other harnesses here all run in one process and therefore all share the same blind spot. Any
of them can be *ended* by the first crash it meets; none of them can *report* one.

## Where the cases come from

**The expressions are built, not listed.** Every concrete `Entity` node the library has is found
by reflection and instantiated, so a node added later is covered without anyone remembering this
directory — which matters because a node's first appearance in a release has twice been the thing
that crashed (`floor`/`ceil` in #829, and #704 before it). Each instance is printed and parsed
back, since that is how it reaches the child; a node whose printed form does not parse is itself
a finding, and the library's own `StringizeRoundTripTest` is where that belongs.

The node types reflection **cannot** build are named in the report rather than passed over. Most
are covered by the written shapes instead: a `Matrix`, an `Interval`, a `Piecewise` and the
special sets all need a constructor argument that is not an `Entity`.

**The written shapes are chosen for what has already killed something**: recursion that feeds
itself (`x * ln(x)`, integration by parts' own crash), a rewrite whose output re-matches its own
pattern, binders and conditions where #878 found a condition escaping its scope, nodes whose
evaluation reaches for a limit which reaches back for evaluation, degenerate arithmetic, and
deep nesting that turns a linear pass into an exponential one.

## The five verdicts

| | |
|---|---|
| **ok** | returned |
| **declined** | threw an `AngouriMathBaseException` — the library saying it will not answer, which is legitimate |
| **unexpected** | threw anything else. A `NullReferenceException` out of a CAS is a defect however it is spelled |
| **timeout** | did not finish. Worse than declining, better than answering wrongly, and it has to be visible |
| **crash** | the child died — stack overflow, runtime abort, killed by a signal |

The last three are counted against the library. `declined` is not: refusing is an answer, and
[AGENTS.md](../../AngouriMath/AGENTS.md) is explicit that it beats a wrong one.

## What it costs

One `dotnet` start per case, so roughly a second and a half each — the run is minutes, not
seconds, and it is meant to be run deliberately rather than on every edit. `--operation=` narrows
it to one pipeline while you are working on that pipeline.

The child applies its own 15-second cap so that it can report a hang as a timeout itself; the
parent's 20-second cap is the backstop for a child too far gone to report anything.
