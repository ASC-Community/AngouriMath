# docsamples

Compiles and runs every code sample in the [AngouriMath wiki](https://github.com/asc-community/AngouriMath/wiki),
and the annotated ones on the website, against the library in this checkout, and checks each
stated output against the one produced.

```sh
dotnet run -c Release --project Sources/Tests/Harnesses/DocSamples
dotnet run -c Release --project Sources/Tests/Harnesses/DocSamples -- --wiki=/path/to/a/wiki/clone
dotnet run -c Release --project Sources/Tests/Harnesses/DocSamples -- --site=/path/to/AngouriMathSite
```

The wiki is a separate repository, so the first run clones it into `wiki/` beside this file,
which git ignores, and later runs bring that clone up to date. Point `--wiki=` at a working copy
to check edits before pushing them. The website is read only when `--site=` names a working copy
of [AngouriMathSite](https://github.com/asc-community/AngouriMathSite); CI checks it out and
passes it.

Exit code is 0 when nothing fails, 1 when a sample does not compile, throws, or prints something
other than what its page says, and 2 when the harness itself could not run. A sample that does
not finish is reported and fails nothing.

## Why

The wiki is the library's documentation, and it lives in another repository. A rename lands in
the code while the pages keep the old name, and a reader finds out from a compile error. A stated
output goes stale the same way and more quietly: an integral that now carries its `+ C`, a
`Complex` that .NET 8 prints as `<9; 0>` rather than `(9, 0)`, a product whose factors print in
a new order.

Both are mechanically checkable, so they are checked here rather than reported by a reader.

## How a page is read

A fenced block tagged `cs` or `fs` is a sample. `cs` samples are compiled against
`Sources/AngouriMath`, `fs` samples against `Sources/Wrappers/AngouriMath.FSharp`.

Each C# sample becomes its own generated file, which keeps its `using` directives to
itself — two samples that import conflicting names both still compile. F# samples become
one module each in one file. The wiki states that `using AngouriMath;`,
`using static AngouriMath.MathS;` and `using static AngouriMath.Entity;` are implied in
every sample; those and `using System;` are supplied, and **anything else a sample needs it
must say**, which is how the missing `using System.Numerics;` and
`using AngouriMath.Extensions;` were found.

**What a page claims it prints** is the next bare (untagged) fence, and only when the line
before that fence is `Output:` — or `Prints:`, `Should print:`, `Will print:`, `Returns:`.
A bare fence used for anything else is never mistaken for an expectation. A sample that
documents its output in trailing comments on its `Console.WriteLine` lines is read the same
way, as long as *every* printing line carries one; a partly annotated sample is reported as
unchecked rather than checked against half of its output.

Four directives, written as HTML comments so they do not render:

| | |
|---|---|
| `<!-- amcheck:skip reason -->` | not compiled. For fragments and pseudo-code |
| `<!-- amcheck:compile reason -->` | compiled but not run. For samples that throw on purpose |
| `<!-- amcheck:continues -->` | appended to the sample before it, and its stated output appended to that sample's |
| `<!-- amcheck:nooutput reason -->` | the fence that follows is prose, not an expectation |

Every use of `skip` and `compile` is listed in the report with its reason, so what is not
being checked is visible rather than absent.

## What it does not check

- **Prose.** An API named in a sentence but never called by a sample is not checked. The
  renames in `BREAKING-CHANGES.md` were grepped for by hand instead; a name-checker over
  backticked identifiers would close this and does not exist yet.
- **The published package.** Samples compile against a *project reference* to the sources, so
  anything that differs between the tree and the `.nupkg` — a member public in one and not the
  other, a missing framework asset — is not covered here. Checked by hand once, on 2026-08-11,
  when `2.0.0` reached nuget.org: a fresh `dotnet new console`, `dotnet add package AngouriMath
  --version 2.0.0`, and the quickstart's own program compiled and printed `x + sin(y * x)` and
  `1 + cos(y * x) * y`; likewise for F# with `AngouriMath.FSharp`. Doing that on every run would
  mean waiting for a release, so it stays a manual check at release time.
- **The website's unannotated samples.** A `pre code` block on the site is checked only when an
  `amcheck` comment marks it as a sample, since most of them are shell, CMake or notebook lines.
  The report counts the blocks it did not check, so the coverage is not read as complete.
- **Ordering that happens to be stable.** `Alternate` sorts by a rate with ties, and the
  order within a tie is whatever the sort produced. If that shifts, this reports it as a
  mismatch, and the right response is to look at whether the sort should be made stable
  rather than to edit the page.
