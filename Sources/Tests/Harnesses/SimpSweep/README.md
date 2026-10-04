# simpsweep

Does `Simplify` preserve the value of an expression it was never shown?

`casbench` lists 117 problems and `propcheck` 151 expressions. Both are corpora, and a
corpus can only hold shapes its author thought of — which is the wrong shape of net for a
simplification defect. The two worst found in this repository were reachable only through
expressions nobody writes by hand: #715 through a quotient that l'Hôpital's rule builds,
and #744 through a polynomial `rootcheck` generated.

So this generates. Expressions are built up from a small alphabet to a bounded depth,
simplified, and the two forms compared numerically wherever both are defined and real.

    dotnet run -c release --project Sources/Tests/Harnesses/SimpSweep

Writes `simpsweep.md` and prints a summary line.

## What it checks, and what it does not

The property is the weakest one worth having and the one that cannot be argued with:
**wherever both forms are defined and real, they are the same number.** Nothing here says
the answer is tidier — that is the complexity metric's business and is not checkable this
way.

Two sample points are negative on purpose. A sweep evaluated only at positive points finds
none of what this found, and `propcheck` is exactly that sweep.

## Two things it had to be taught

- **A `NaN` is a failure only where the original has a value somewhere.** `1 / (x - x)` is
  undefined at every point and `NaN` is the honest answer to it. The first run reported
  eighteen of those as defects, and a harness that cries wolf is worse than no harness.
- **The tolerance is relative, not absolute.** The corpus reaches `e^(x^2)` at 4.19, which
  is 3e30, and an absolute tolerance of 1e-9 there is meaningless.

## What it found

10463 expressions, 31 disagreements, in three families — #751 (a plain sign error in
`k*a/a^(1/n)`) and #752 (two branch-cut rewrites, `sqrt(x^2) → x` and `sqrt(-x) → i·sqrt(x)`).
