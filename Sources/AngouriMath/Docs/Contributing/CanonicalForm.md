# What canonical form means here, and how it differs from simplest

[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 1 asks for two things this
file is: a written specification of what canonical means for each node class, and **a stated
distinction between canonical and "simplest"**. Its item 65 says to take a position, write it down,
and let the engine be checked against it. The checking is `Sources/Tests/Harnesses/CanonCheck`, and every number below
came out of it rather than out of an argument.

Read [SimplificationContract.md](SimplificationContract.md) first if you are changing a rewrite.
That file is about whether a rewrite is *sound*. This one is about what shape the expression it is
handed is in, and what a caller may conclude from two expressions having the same shape.

---

## 1. The position, in one paragraph

**Canonical is about identity. Simplest is about presentation. They are different jobs, they want
different machinery, and conflating them is how a library ends up with neither.**

A *canonical form* is a function `c` on expressions such that `c(a)` and `c(b)` are the **identical
tree** whenever `a` and `b` denote the same mathematical object. Its whole purpose is that equality
becomes a structural comparison: you get to decide `a = b` by canonicalising both and comparing
nodes. It says nothing about whether the result is nice to look at.

A *simplest form* is the best-rated member of an equivalence class under a cost metric. The metric
is a caller's choice — smallest tree, fewest radicals, numerically stablest, most readable to a
student — so "simplest" is only defined relative to one, and two different metrics give two
different answers that are both right.

The library has one of each, and they are `InnerSimplified` and `Simplify`. Neither is currently
canonical; §3 measures how far off each is, and the answer is not the one the names suggest.

## 2. A complete canonical form does not exist, and that is a theorem rather than a gap

For the class of expressions AngouriMath accepts — rationals, `pi`, the exponential, the
trigonometric functions, `abs`, and composition — **deciding whether an expression is zero is
undecidable.** That is Richardson's theorem (Daniel Richardson, *Some undecidable problems involving
elementary functions of a real variable*, J. Symbolic Logic 33 (1968), 514–520), and since a
canonical form would decide zero-equivalence by canonicalising and comparing against `0`, no
canonical form exists for the whole language.

So the specification cannot be "canonicalise everything", and any roadmap item that reads that way
is asking for something that is not there. What it can be, and what the rest of this file is:

1. **A canonical form on a decidable sublanguage**, stated exactly, with the boundary written down.
2. **A normalisation everywhere else** — idempotent, order-independent, and cheap — which is not
   canonical and must not be relied on as though it were.
3. **A search** for a presentable form, which is `Simplify`, and which is explicitly not required to
   be canonical.

This is the classical three-way split — Joel Moses, *Algebraic simplification: a guide for the
perplexed*, CACM 14 (1971), 527–537 — and taking the position costs nothing except writing it down,
which is what tier 1 asked for.

## 3. Where the library actually stands, measured

Three properties, each of which a canonical form must have and none of which needs an oracle:

- **idempotence** — applying the form twice is applying it once;
- **order independence** — the operands of a commutative operator may be written either way round;
- **agreement** — two writings of one expression reach the same form.

`Sources/Tests/Harnesses/CanonCheck` measures all three against each candidate, comparing **entities and not printed
strings**. The figures below are on `master` with the two defects of the following subsection fixed
— [#929](https://github.com/asc-community/AngouriMath/issues/929) and
[#930](https://github.com/asc-community/AngouriMath/issues/930), each landing separately. Without
them the first column's idempotence reads 1 rather than 0 and the last column's agreements read 6
rather than 4; nothing else moves. A harness report records a build, so regenerate it before quoting
it against a later one.

| | `InnerSimplified` | order, then normalise | **normalise, order, normalise** | `Simplify` |
|---|---|---|---|---|
| idempotence | 0 failed of 834 | 21 failed of 834 | **0 failed of 834** | 0 failed of 120 |
| order independence | 2024 failed of 2738 | 0 failed of 2738 | **0 failed of 2738** | 8 failed of 72 |
| listed agreements | 20 failed of 30 | 10 failed of 30 | 10 failed of 30 | 4 failed of 30 |

**None of the three is canonical, and no one of them is closest on every property.** The middle
column is the surprise and it is the useful one.

**The total order already exists and it works.** `RewriteRules.CanonicalOrderExact` sorts and groups
the operands of sums, products, conjunctions, disjunctions and set operations by the whole subtree,
and applying it makes order independence *perfect* — 0 failures of 2738, against 2024 without it.
There are three granularities: `CanonicalOrder` ignores constants so that `x` and `2 * x` group
together for collecting like terms, `CanonicalOrderCountingConstants` distinguishes them, and
`CanonicalOrderExact` compares whole subtrees, which is the one a canonical form wants. So the thing
a specification would normally have to invent is built; **what is missing is that the normalisation
does not run it.** `Simplify` does, which is most of why it agrees so much more often than
`InnerSimplified` — the names imply the opposite of the truth here.

**And the composition that has both properties is the third column, which is the canonicaliser this
tier was asking for.** Sorting and *then* normalising is not idempotent — 21 of 834 — but
normalising, sorting, and normalising again is idempotent and order-independent on everything tried:
**0 of 834 and 0 of 2738**.

The reason is worth knowing, because it is not a conflict between rules and nothing has to be
decided between them. **The sort's key depends on a node's class, and the normalisation changes
classes.** In `1/2 - x` the constant reaches the sort as `1 * 2 ^ (-1)`, a *product*, and is ordered
against `-x` as one; the normalisation then folds it to the number `1/2`, and the next sort — now
seeing a number — orders it the other way:

```
1 / 2 - x    sorted    ->  -x + 1 * 2 ^ (-1)    normalised  ->  -x + 1/2
-x + 1/2     sorted    ->  1/2 + -x                             ...and back again
```

So the sort was ordering a shape that was about to stop existing. Normalise first and it sorts what
the tree is actually going to be. `x + (-1/2)`, whose constant is already a number, was stable all
along — which is the control that makes this the explanation rather than a guess.

`Simplify`'s remaining 8 order failures are a different thing again: ties. When two candidates rate
equal the tie goes to whichever was generated first, which depends on the input order. What ranks
them is `MathS.Settings.ComplexityCriteria` — `SimplifiedRate`, a weighted count — and not the node
count that `Complexity` returns; ties between differently-shaped forms are common.

### The finding that matters most for anyone writing a rule

`(x + y) + a` and `x + (y + a)` **both print as `x + y + a` and are different trees** after
`InnerSimplified`. Associativity is normalised in the printer, not in the expression. A rule, a test
or a cache that compares printed forms will call them equal; one that compares entities will not.
Compare entities.

The canonicaliser of the third column *does* make them one tree — `a + x + y` — because the sort
works over commutative chains and so flattens as it sorts. That is worth knowing and does not soften
the warning: what almost everything in the library actually sees is `InnerSimplified`, where the two
trees are still two.

### Two defects it turned up, rather than decisions — both since fixed

- `cos(0 ^ y)` was not idempotent under `InnerSimplified`: the first pass left `-(-1)` at the head of
  the answer and the second folded it to `1`. `-(-1)` on its own folds immediately, so a rewrite was
  building it above already-normalised children and returning without re-normalising.
  [#930](https://github.com/asc-community/AngouriMath/issues/930).
- `cos(-x)`, `sin(-x)`, `tan(-x)` and `abs(-x)` were left alone by `Simplify` while `cos(-2 * x)`
  folded to `cos(2 * x)` — by accident, through the multiple-angle expansion rather than through
  parity, since that skips a coefficient of `-1`. So `sin(-x) + sin(x)` did not reach `0`.
  [#929](https://github.com/asc-community/AngouriMath/issues/929); with it the listed agreements
  above are 4 rather than 6.

Both were found on the first run, which is the argument for the harness rather than for the document.

## 4. What canonical means per node class

This is the **target**, not a description of today. Where the library already meets a line it is
marked; where it does not, the line is what `canoncheck` should eventually assert.

### Leaves

| class | canonical form |
|---|---|
| `Integer`, `Rational` | lowest terms, denominator positive; a rational with denominator one is an `Integer` — **met** |
| `Real`, `Complex` | a complex with zero imaginary part is a `Real`; likewise down the tower — **met** |
| `Variable` | itself; the name is the identity |
| `Boolean` | `True` or `False` |

### Commutative operators

| class | canonical form |
|---|---|
| `Sumf` | flat over nesting, operands in a total order, numeric operands folded into one leading term, a zero term dropped — **ordered by `CanonicalOrderExact`, which the normalisation does not run**; not flattened in the tree |
| `Mulf` | flat, ordered, numeric factors folded into one leading factor, a one factor dropped, a zero factor collapsing the product — as above |
| `Andf`, `Orf`, `Xorf` | flat, ordered, constants folded, duplicates dropped — ordered by the same rewrite |
| `Unionf`, `Intersectionf` | flat, ordered by the element order — ordered by the same rewrite |

A **total order on operands** is what makes ordering decidable, and the library has one:
`Patterns.SortRules` at three granularities, reached through `RewriteRules.CanonicalOrder`,
`CanonicalOrderCountingConstants` and `CanonicalOrderExact`. Measured, the exact one is total enough
to give order independence on every pair tried.

Both are met by composing what already exists in the right sequence — normalise, order, normalise —
which §3 measures at 0 failures on both properties and which is
`Transformation.Canonicalization`. **Nesting comes with the ordering**, because the sort works over
commutative *chains* rather than over one node, so it flattens as it sorts: `(x + y) + a` and
`x + (y + a)` both reach `a + x + y`, as the same tree.

What is *not* met is that `InnerSimplified` on its own does neither, and it is `InnerSimplified` that
every rule and every cache in the library sees.

### Operators that are sugar for a commutative one

| class | canonical form |
|---|---|
| `Minusf` | absent — `a - b` is `a + (-1) * b` |
| `Divf` | absent — `a / b` is `a * b ^ (-1)` |

**This is a position and it is not free.** It is the one that makes the commutative laws reachable:
while a difference is its own node, `x - y` and `x + (-y)` are different trees denoting one thing —
measured, both listed pairs disagree — and no amount of ordering sums fixes it, because one of them
is not a sum. The cost is that the printer must put subtraction and division back, or every output
regresses into `a + (-1) * b`. The printer already does exactly this for a negative coefficient,
which is why `(-1) * x` prints `-x`; the change is to stop the *tree* from having two shapes, not to
change what is shown.

**This is the one line here that needs a maintainer's yes before anybody implements it.** Removing
two node classes touches pattern matching, every `switch` over the hierarchy, the parser's output,
the exporters and `AddingNode.cs`, and it is a breaking change to anything matching on `Divf`. It is
written down as the position because a specification that ducks the question is not one; it is not
written down as a decision already taken.

### Powers

| class | canonical form |
|---|---|
| `Powf` | a numeric exponent in lowest terms; `x ^ 1` is `x`; `x ^ 0` is `1` **only where the base is known non-zero**, and otherwise carries the condition; a power of a power multiplies its exponents only where that is sound over the complex plane |

`x ^ 0` is the standing example of a canonicalisation that a naive specification gets wrong:
measured, the library answers `1 provided not x = 0`, and that is right. `0 ^ 0` is not `1`. A
canonical form that maps `x ^ 0` to `1` unconditionally has made the library answer wrongly to buy
itself a tidier rule, which
[AGENTS.md](../../../../AGENTS.md) forbids in the first line.

Likewise `sqrt(x)` and `x ^ (1/2)` **must** reach the same form — they denote the same principal
branch — and `(x ^ 2) ^ 3` and `x ^ 6` must not be assumed to, since `(x ^ a) ^ b` is `x ^ (a * b)`
only under conditions the contract file states.

### Functions

| class | canonical form |
|---|---|
| `Absf`, `Cosf`, and the even functions | the argument's own canonical form, with a leading negation removed — **not met**, see §3 |
| `Sinf`, `Tanf`, and the odd functions | a leading negation lifted out of the argument to the front of the node — **not met** |
| `Logf` | the base and the argument each canonical; no rewriting between bases, since that is a rule's decision and not a form's |
| everything else | children canonical, node unchanged |

### Nodes that are not values

| class | canonical form |
|---|---|
| `Providedf` | the condition itself canonical, and a `Providedf` never nested inside another — `(a provided p) provided q` is `a provided p and q` |
| `Derivativef`, `Integralf`, `Limitf` | children canonical; the node is a request, and a form must not evaluate it |
| `Setf` and the set classes | a finite set ordered and deduplicated — **met**, measured |

## 5. Where a canonical form is actually available: the polynomial sublanguage

§2 rules out canonicalising everything. It does not rule out canonicalising the part of the language
where zero-equivalence *is* decidable, and that part is large and useful: **rational functions over
`Q` in finitely many variables.** For those, a canonical form exists, is classical, and — since
[#918](https://github.com/asc-community/AngouriMath/pull/918) and
[#923](https://github.com/asc-community/AngouriMath/pull/923) — is now buildable out of parts the
library has:

- a quotient of two polynomials, each expanded and with a specified monomial order;
- divided through by their multivariate GCD, which `PolynomialGcd` computes;
- with the denominator made monic in the order, the sign carried in the numerator;
- and the coefficients rationals in lowest terms.

Two rational functions are equal exactly when this form is identical, so the equality question is
decided rather than searched. This is the concrete deliverable behind tier 1's "a specified
canonicaliser", and it is the piece to build first, because it is the piece that is *possible*.

The boundary has to be stated at the API rather than hidden: a caller asking to canonicalise
`sin(x) + 1` must be told that no canonical form is claimed, not handed a normalisation that looks
like one. **A form that is canonical on part of the language and silently approximate on the rest is
worse than no form at all**, because its whole value was that structural equality meant something.

## 6. What `Simplify` is, and what it is not required to be

`Simplify` searches: it generates candidates by applying rewrites and returns the best by
`MathS.Settings.ComplexityCriteria`. It is therefore

- **metric-relative** — a different criterion is a different answer, and that is intended;
- **not required to be canonical** — measured, it fails 8 of 72 order checks, all of them ties
  broken by generation order;
- **not required to be idempotent**, though measured it is, at 120 of 120;
- **not a decision procedure for equality**. `a.Simplify() == b.Simplify()` failing proves nothing
  at all. Use the residual — `(a - b).Simplify()` against zero — and even that is a semi-decision,
  by §2.

The one thing it *is* required to be is sound, which is [SimplificationContract.md](SimplificationContract.md).

## 7. How this is checked

`Sources/Tests/Harnesses/CanonCheck`. It builds expressions by growing a small grammar, then
checks idempotence and order independence generatively and a listed set of agreements by hand. It
runs all three over each candidate form, because the interesting facts are the differences between
the columns rather than any one number. The candidates are `InnerSimplified`, `CanonicalOrderExact`
followed by `InnerSimplified`, the same sort after the normalisation, and `Simplify`. The listed
pairs are each a claim this file makes or disclaims, so a disagreement there is a decision to take
rather than necessarily a defect.

CI runs it on every change to the library. Its findings are listed in `canoncheck-baseline.tsv`
beside it, and the run fails when that list changes, so a change to what is canonical is recorded
in the commit that makes it.

It compares entities. Nothing in it reads a printed form, which is deliberate: the associativity
finding in §3 is invisible to a string comparison and is the single most likely thing to be got
wrong by a test written in a hurry.

## 8. What is owed

1. **Where the canonicaliser runs.** It exists — `Transformation.Canonicalization`, built out of
   parts that already existed, measured idempotent and order-independent, with no rule changed — and
   nothing calls it. Offering it changes nothing for anyone. Putting it inside `InnerSimplified`
   moves every commutative operand order in every printed answer at once, which is a release of its
   own and a decision rather than an implementation.
2. The rational-function canonical form of §5, as an explicit operation with the boundary in its
   signature — [#934](https://github.com/asc-community/AngouriMath/issues/934). This is the one that
   needs building rather than composing, and the part that is missing is smaller and more specific
   than it sounds: the greatest common divisor is already there and already verifies itself, but
   **nothing in the library puts an expression over a common denominator**, so `1/x + 1/y` and
   `(x+y)/(x*y)` cannot be brought to a common form by any existing route. `Simplify` prefers the
   split one and will pull the combined one apart again.
3. `canoncheck` in CI once the counts are meant to be zero, which they are not yet. Until then it is
   a measurement, and its numbers belong in a commit message rather than in a gate.

The two defects §3 lists are fixed and are not on this list.
