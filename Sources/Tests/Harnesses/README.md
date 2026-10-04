# Harnesses

Console programs that measure the library against properties a unit test cannot list case by
case: each generates its inputs, runs them, verifies every answer it gets, and writes a report.
Each exits non-zero when it finds a defect, or when a list of findings it keeps changes, and
`.github/workflows/Harnesses.yml` runs them on every change to the library.

| harness | asks | fails on |
|---|---|---|
| `BoundCheck` | does `Simplify` keep the value **at the boundary** — across a branch cut, off the real line, outside a principal interval, at a pole | any disagreement |
| `RootCheck` | is the root set **complete**, on polynomials built from known factors | a missing root, a returned value that is not a root, an exception |
| `CasBench` | does a corpus of problems with known answers still solve, every answer verified — an integral by differentiating it back | a wrong answer, an exception |
| `SimpSweep` | does a simplification keep the value of the expression it came from, at sampled real points | any disagreement |
| `RuleCheck` | does a rewrite rule set do what it **declares**: terminate alone and composed with the normalisation, and keep the value where it claims `Equivalence` | a set that does not settle, a `Sound` declaration a value falsifies |
| `CrashCheck` | does anything take the process down: every case in a child process of its own, so a stack overflow is a result rather than the end of the run | a crash, an exception that is not the library declining |
| `PropCheck` | does each transformation satisfy a property it must: `Simplify`, `Expand` and `Factorize` keep the value, `Differentiate` agrees with a difference quotient, `Integrate` differentiates back | any property that does not hold |
| `CanonCheck` | is there a **canonical form**: idempotence, order independence over commutative operators, and agreement between writings, for `InnerSimplified` and `Simplify` alike | a change to its findings, listed in `canoncheck-baseline.tsv` |
| `Confluence` | where two arms of one rule set both fire at a node, do they agree, or is the order of the arms load-bearing | a change to its conflicting pairs, listed in `confluence-baseline.tsv` |
| `DocSamples` | does every code sample in the wiki, and every annotated one on the website, compile, run, and print what its page says | a sample that does not compile, throws, or prints something else |

A timeout fails none of them: a shared runner is slower than the machine a budget was set on.

`CanonCheck` and `Confluence` report findings by design. `InnerSimplified` is a normalisation, and
does not reorder operands. Some rule sets depend on the order of their arms. So each commits its
list of findings beside it, and the run fails when the list changes in either direction: a finding
that appears, or one that goes away. To record an intended change, run the harness with
`HARNESS_UPDATE_BASELINE=1` and commit the list with the change. A finding whose case timed out
counts as unmeasured, not gone.

Run one by hand with `dotnet run -c Release --project Sources/Tests/Harnesses/BoundCheck`. Its
report goes to this folder, where it is ignored by git, or to the directory `HARNESS_REPORTS`
names. A report says which commit it measured: a report records a build, not a branch.

More harnesses exist. Each comes here once it gains a criterion that fails on a defect, or on a
change to the findings it lists, and never on the machine:
[#1256](https://github.com/asc-community/AngouriMath/issues/1256) lists them.
