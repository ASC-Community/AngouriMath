# Transformations

`AngouriMath.Core.Transformations` is the layer the 1.x mathematical entry points sit on. It exists
so that an operation is a **value** — something with a name, a stated claim, and a way to be
composed — instead of only a method you call.

It is the first step of the rewrite-engine layer in
[#746](https://github.com/asc-community/AngouriMath/issues/746). It is deliberately small, and it is
**experimental**: the three concepts are meant to last, the catalogue will grow, and the signatures
may still move. The stable surface is `MathS` and the methods on `Entity`.

## Why it is not just an interface with `Apply`

Three things have to be said about a mathematical operation before it can be composed with another
one, and none of them fit in `Entity -> Entity`:

**What it claims.** `Simplify` returns another way of writing the same value. `Differentiate` returns
a different object. Both are `Entity -> Entity` and confusing them is how you get a test that
subtracts a derivative from its integrand and asserts zero. `TransformationRelation` is `Equivalence`
or `Derivation`, and a chain is an equivalence only if every link is.

**How well justified the claim is.** `Soundness` is `Sound`, `SoundUnderAssumptions` or `Heuristic`,
and composing takes the weaker of two — nothing composes upwards. The tier is **declared, not
checked**: it is a claim to argue with, not a guarantee, which is why the registry starts
conservative and why tightening a label needs an argument while loosening one does not.

**Whether it answered at all.** `ApplyCore` returns `null` for "I could not settle this", which is
the same distinction [AGENTS.md](../../../../AGENTS.md) draws between an unevaluated node and `NaN`.
`Transformation.Integration("x")` applied to `e ^ (x ^ 2)` has no answer; `Entity.Integrate` makes the
same claim in the shape its callers expect, by handing back an unevaluated `Integralf`.

## The pieces

| | |
|---|---|
| `Transformation` | the operation: `Name`, `Relation`, `Soundness`, `Apply`, and the static catalogue |
| `TransformationResult` | input, output-or-nothing, and which transformation ran. A struct, so routing an ordinary call through this layer allocates nothing |
| `RewriteRuleSet` | a named, attributed group of rewrites — `Name`, `Description`, `Relation`, `Soundness`, `ApplyOnce` |
| `RewriteRules` | the registry: every rule set the simplifier applies, explicitly listed, enumerable through `All` in a fixed order |
| `RewriteRecording` / `RewriteStep` | a scope that collects the rewrites which fired while it was open — off unless asked for, and free when off |
| `DerivationPath` / `DerivationStep` | the same recording read as a route: whole expressions, in order, from the input to the answer that was returned |

Composition is `Then`, `Repeat(n)` and `UntilStable(max)`. All three take their bound from the
caller: there is no unbounded rewrite loop anywhere in the layer, and `UntilStable` reports hitting
its bound as **no answer** rather than as the value it happened to be holding, so that a rule set
which does not converge is visible instead of silently truncated.

Registration is static and explicit. No assembly scanning, no `Activator`, no reflection — the layer
stays trimmable and NativeAOT-publishable, and `RewriteRules.All` is in an order that does not depend
on hashing or on which type loaded first.

## What already uses it

```
Entity.Simplify(level)     ->  Transformation.SimplificationAtLevel(level)  ->  Simplificator.Simplify
Entity.Expand(level)       ->  Transformation.ExpansionAtLevel(level)       ->  Entity.ExpandOverSum
Entity.Factorize(level)    ->  Transformation.FactorizationAtLevel(level)   ->  RewriteRules, composed
Entity.AsSingleFraction()  ->  Transformation.AsSingleFraction              ->  SingleQuotient.OverLeastCommonDenominator, each half tidied
Entity.Differentiate(x)    ->  Transformation.Differentiation(x)            ->  Entity.DifferentiateOnce
Entity.Integrate(x)        ->  Transformation.Integration(x)                ->  Integration.ComputeIndefiniteIntegral
Entity.Limit(x, to, side)  ->  Transformation.LimitAt(x, to, side)          ->  LimitFunctional.ComputeLimit
Simplificator.SimplifyChildren  ->  a composed chain of four registry entries
```

Two of these are real ports rather than wrappers. `Factorize` is no longer a method that names its
own rules: it is `PerfectSquare`, then `Factorization`, then a tidying pass, repeated `level` times,
built out of the registry. `SimplifyChildren` — which every stage of the simplification pipeline runs
— is a chain composed once, statically, out of four registry entries instead of a hand-written run of
`Replace` calls. Both produce exactly what they produced before.

Everything else is a thin adapter over the algorithm that was already there. **Nothing that worked
was rewritten to make the architecture tidier.**

## Recording what fired

```csharp
using var recording = RewriteRecording.Start();
var simplified = ((Entity)"a / (b / c)").Simplify();
foreach (var step in recording.Steps)
    Console.WriteLine(step);      // Common: a / (b / c) -> a * c / b
```

This is [#28](https://github.com/asc-community/AngouriMath/issues/28). Three things about it
are deliberate and worth keeping:

**Free when off.** With no recording open, applying a rule set costs one ambient read more
than it did before — per rule set, not per node — and allocates nothing. That is why it is a
scope rather than a setting: a setting is something a caller can leave on.

**Per flow**, like `MathS.Settings`, so a parallel caller records its own work and nobody
else's. It follows the call rather than the thread, so it survives an `await` and work started
under it reports to it. Recordings nest, and an inner one hides the outer until it closes.

**A step is a subexpression, not a snapshot.** A rewrite pass walks bottom-up and rewrites
nodes as it goes, so there is no moment at which a partly-rewritten whole expression exists
to photograph. #28's example shows whole-expression snapshots; those exist at the *boundaries*
of a pass and not inside one, which is what `DerivationStep` below is made of.

**A recording has three views, and they answer different questions.** `Steps` is every rewrite
that fired. `Derivation` is that list with the normalisation and the repeats taken out — 270
rewrites down to 5 on `x^(-1)/(y/z)` — which is what a reader asking *which identities were used*
wants. Both are drawn from every candidate the simplifier generated, including the ones it
discarded, so neither is a route. `PathFrom` is the third, and it is.

## Reading it as a path

```csharp
using var recording = RewriteRecording.Start();
var input = MathS.FromString("x^(-1)/(y/z)", useCache: false);
var answer = input.Simplify();
Console.WriteLine(recording.PathFrom(input, answer));
```

```
x ^ (-1) / (y / z)
  = 1 / x / (y / z)                               // InnerSimplified
  = 1 * x ^ (-1) * y ^ (-1) * (z ^ (-1)) ^ (-1)   // CanonicalOrder
  = 1 / x * 1 / y * 1 / (1 / z)                   // InnerSimplified
  = z / (x * y)                                   // SimplifyChildren
```

`DerivationPath.OfSimplifying(expression)` is the same thing in one call. Four things about it:

**It is a path, and that is a checkable claim.** `Steps[i].After` *is* `Steps[i + 1].Before`, the
first step starts at the input, and the last lands on the value `Simplify` returned — compared as
expressions, never as printed forms. `DerivationPathTest` asserts exactly that over eight shapes.

**The losing candidates are not on it.** The simplifier grows a family of candidates and keeps the
cheapest, so "the route" only exists once the winner is known; a step through a candidate that lost
is not a step towards the answer. They are excluded rather than marked, because a derivation is read
forwards and a marked dead end is something to skip. What the search produced and did not keep is
reported as `ExpressionsExplored`, so the path does not read as though it were the whole story.

**A step is a whole expression; the rewrites inside it are subexpressions.** That is the same fact
as before — a rewrite pass walks bottom-up and there is no moment at which a partly-rewritten whole
expression exists — resolved by putting the two grains in two types. `DerivationStep` is the pass,
and its `Rewrites` are the `RewriteStep`s that fired inside it, each naming the rule that did it.

**Not every step is a rule set.** `DerivationStep.RuleSet` is null for the ones that are not —
inner simplification, the boolean minimiser, a polynomial rearrangement, the tidying chain
`SimplifyChildren` runs — and null there is the same convention as `RewriteStep.Rule` being null
for a set with no addressable rules: no name is invented for something that has none.

Reconstructing this needs the simplifier to say what each of its stages turned into, since a rewrite
pass only ever records the subexpressions it changed. `Simplificator` does that now, and it costs one
ambient read per stage against a tree walk per stage — the same shape, and the same reason, as the
one `ApplyOnce` already did.

## What is deliberately not here

**`Solve` is not a transformation.** It consumes a *goal* — an equation, with a variable to solve for
— and produces a solution set, and the honest place for it is a tactic layer where a goal can become
subgoals. `Entity.Set` being an `Entity` means `Solve` would type-check as `Entity -> Entity`; that
is exactly why forcing it in would be a mistake, since it would compile while saying nothing true
about what the operation does. Same for `Isolate`, `Eliminate` and `Parametrize` when they arrive.

**There are no inverse transformations.** `Expand` and `Factor` are not inverses, and `Unsolve` is not
a well-defined operation. Where an inverse is mathematically meaningful there is room to add one; the
API does not invent symmetry that the mathematics does not have.

An individual *rule* is a different question, and one that has an answer: a rule both of whose sides
are patterns over the same holes can be read backwards, and thirteen of the fourteen rules written as
data can be. `k*p + k*q -> k*(p + q)` and its reverse are one rule read two ways; the Pythagorean
identity is not, because `1` does not say which angle it came from. See
[`ReversibleRules.md`](ReversibleRules.md) for what that requires, why the reversal is licensed, and
what it deliberately does not claim. None of the 405 rules in `RewriteRules.All` can be read
backwards, because their replacement is C# source text.

**`Heuristic` currently has no instance in the catalogue.** That is a statement about what is
registered so far, not a claim that nothing in the library guesses.

**Some rewrites still bypass the registry.** Every set the *simplifier* applies is registered, and
`Simplificator` reaches all of them through `RewriteRules` — that is what lets an account of what
`Simplify` did be a complete one, since a set reachable only by its method has no name to attribute a
step to. The equation and set solvers, the integrator and `TreeAnalyzer` still call `Patterns`
directly. `Patterns.TrigonometricToExponentialRules(from, to)` cannot become a registry entry as it
stands: it is parameterised by two variables, so it is a family of sets rather than one.

## Adding the next one

A new transformation built from rules that already exist is one line:

```csharp
public static Transformation Rationalization { get; }
    = Rewriting(RewriteRules.RationalizeDenominator).Then(InnerSimplification);
```

A new rule set is five, and registering it gets it enumeration, an identity, a soundness label, and
the tests that run over `RewriteRules.All` — including the one that asserts it reaches a fixed point
rather than rewriting in a cycle:

```csharp
public static RewriteRuleSet Power { get; } = new(
    nameof(Power),
    "Gathers and splits powers, roots and logarithms.",
    TransformationRelation.Equivalence,
    Soundness.SoundUnderAssumptions,
    Patterns.PowerRules);
```

Add it to `RewriteRules.All` in the same change — the list is explicit so that its order is a
decision rather than an accident. Where a set is parameterised by something with a small fixed range,
register one entry per value and add an `internal` chooser beside them (`CanonicalOrderAt`,
`CommonDenominatorAt` are the two): the alternative is an entry that cannot be enumerated, which
defeats the point of the list.

Inside the library, apply a set with `expression.Rewrite(RewriteRules.Power)` rather than
`RewriteRules.Power.ApplyOnce(expression)` — a sequence of rule sets otherwise reads inside out.

Two things to get right:

- **State the relation honestly.** If the output is not another way of writing the input, it is
  `Derivation`, and the equivalence property test will correctly leave it alone.
- **Do not claim `Sound` without an argument.** Every rule set shipped today is
  `SoundUnderAssumptions`, and the test over `RewriteRules.All` enforces that, so promoting one means
  changing that test and saying why in the same change.

## What the next step is

The unit the library *applies* is the rule set; the unit it can *name* is the single
`pattern -> replacement` line, and has been since
[#951](https://github.com/asc-community/AngouriMath/pull/951). Those lines are generated from the
`switch` that defines each set rather than transcribed beside it, so the `switch` stays the thing a
human edits and the thing the library calls, and the two cannot drift apart —
[#825](https://github.com/asc-community/AngouriMath/issues/825), and #746's item 50. Only
`RationalizeDenominator` has no addressable rules today, because it is a procedure rather than a
`switch`; `RewriteRuleSet.Rules` is empty there and `RewriteStep.Rule` is null, which is the
difference being reported rather than hidden.

The performance objection that used to sit here is settled and was backwards: #825 measures rules
bucketed by the node type they match at **as fast as** the hand-written `switch` on a
realistic-sized set and **2.3× faster** on a small one, at identical allocation. A large `switch`
over distinct node types compiles into that same type dispatch anyway.

So what is left, in dependency order:

- **The goal/tactic layer `Solve` belongs in** — #746's item 64. Nothing about a derivation changes
  for it; a tactic that fires is a step like any other, and it will want the same two grains.
- **Reversible trees** — [#273](https://github.com/asc-community/AngouriMath/issues/273)'s second
  half. The path is the recorder; what is not here is a *branch root* a failing method can be sent
  back to. `Simplify` never needs one, because it does not backtrack — it generates candidates and
  ranks them — so the case that wants it is the solvers, and it belongs with the tactic layer rather
  than in front of it.
- **A production caller for a rule read backwards** — the mechanism exists and nothing uses it. The
  consumer #746 tier 2 names is the inverse-pair table equality saturation needs, since a saturation
  engine keeps both results where this pipeline keeps one and so has to be told which rewrites undo
  each other. `RewriteRuleGrowth` does not answer that: it says which way a rule moves, not which
  rule is the inverse of which — though for a pair whose two directions are *declared*, the growth
  ceiling already withholds the expanding one (#1194), which is as much of the table as saturation
  has needed so far. The graph itself has its first production caller: the perfect-square collapse
  asks `Saturation.ProvesEqual` before it asks `Simplify` (#1201), measured as a caller that meets
  the standing performance condition rather than as a speed-up.
- **Rendering a step as a sentence** — #746's v5.0. Everything a sentence needs is now on a step
  except one thing: *why the rewrite is allowed*. `Soundness` is declared, not checked, and a rule
  that holds only under an assumption does not say which. That is the same gap
  [#721](https://github.com/asc-community/AngouriMath/issues/721) names, and it is the next piece of
  metadata worth adding rather than the next layer.
