# Expression syntax

What `MathS.FromString` accepts, and what `Entity.Stringize` produces. The authority is
[`Core/Antlr/AngouriMath.g`](../../Core/Antlr/AngouriMath.g); this page is a reading of it. To
change the language, change the grammar and regenerate — see
[`ImproveParser.md`](../Contributing/ImproveParser.md).

## The contract

**Parsing what `Stringize` prints gives back the expression it printed.** That is the whole
point of the printed form, and it is enforced by `StringizeRoundTripTest`. A node whose usual
notation is not in the grammar prints as its function call instead: a lambda prints as
`lambda(x, x + 1)` and not `x -> x + 1`, because `->` is the implication operator and the arrow
form would silently come back as something else.

A lambda is now also *readable* as `x => x + 1`, and it still **prints** as `lambda(x, x + 1)`.
More than one spelling may be read; exactly one is printed, and it is the one this contract is
about.

A **codomain** is part of that. A node whose `Codomain` is not the one its type carries by default
prints inside `domain(...)`, so `domain(x, ZZ)` prints as `domain(x, ZZ)` and reads back narrowed
([#1022](https://github.com/asc-community/AngouriMath/issues/1022)); it is not decoration, since
`sqrt(-1)` is `i` and `domain(sqrt(-1), RR)` is `NaN`. The wrapper is printed only where it says
something, so the ordinary expression is unchanged — and the default is not the same for every node:
a variable and a matrix default to `Any`, `abs` and an interval to `RR`, every boolean node to `BB`,
each numeric literal to its own type's domain, and everything else to `CC`.

**`Any` is written too, and it is not a set.** `domain(abs(x), Any)` is how a node widened from a
narrower default prints, and it reads back widened
([#1048](https://github.com/asc-community/AngouriMath/issues/1048)). `Any` is a keyword in that one
position and nowhere else — `Any + 1` is arithmetic on a variable called `Any`, and there is no
`Entity` it denotes: `SpecialSet.Create(Domain.Any)` throws, deliberately. It says that a node
imposes no restriction, which is a fact about the node and not a collection of values
([#996](https://github.com/asc-community/AngouriMath/issues/996)).

One annotation still does not survive, and it is the parser's rather than the printer's: no input
at all yields a rational literal whose codomain is `CC`, because the pass that reads `1/2` as a
rational rather than a quotient
([#873](https://github.com/asc-community/AngouriMath/issues/873)) treats `CC` as "nobody annotated
this", which is what an un-annotated `/` carries.

**LaTeX output has a round trip too, and it is checked in someone else's repository.**
[CSharpMath.Evaluation](https://github.com/verybadcat/CSharpMath/blob/master/CSharpMath.Evaluation/Evaluation.cs)
reads LaTeX back into an `Entity` and says so in its own source — *"CSharpMath must handle all
LaTeX coming from AngouriMath or a bug is present!"* — so `Latexize` is free to use `\frac`,
`\bmod` and the rest, but changing what it emits can break a downstream project and no test here
will say so ([#822](https://github.com/asc-community/AngouriMath/issues/822)).

## Operators, loosest first

Everything on one line has the same precedence and groups to the left, except `^` and `provided`,
which group to the right.

Grouping is not decoration: an operator that is not associative prints the brackets it has, so
`a implies (b implies c)` prints with them and `(a implies b) implies c` prints without. Where the
operator *is* associative the brackets carry no mathematics and are not printed, so `x + (y + z)`
prints as `x + y + z` and reads back as `(x + y) + z` — the same number, a differently shaped tree.

| | operators | notes |
|---|---|---|
| 0 | `forall … :`, `exists … :`, `exists! … :`, `=>` | prefix binders whose body **runs to the end of the line**; see the quantifier paragraph and the lambda arrow below |
| 1 | `provided` | **groups to the right**: `a provided b provided c` is `a provided (b provided c)` |
| 2 | `implies` `->` | |
| 3 | `or` | |
| 4 | `xor` | |
| 5 | `and` `&` | |
| 6 | `not` | prefix |
| 7 | `=` `<>` `>` `>=` `<` `<=`, `≡` | chained: `a < b < c` means `a < b and b < c`; a trailing `(mod n)` makes the `=`/`≡` links congruences, see below |
| 8 | `in`, `subset` `⊆`, `superset` `⊇`, `divides` `\|` | one level; `a divides b` and `a \| b` are the statement that `b` is a whole multiple of `a`, defined over the integers and `NaN` elsewhere; `A subset B` is the statement that every member of `A` is a member of `B`, and `B superset A` is the same statement (see the sets section) |

**Congruence.** `a = b (mod n)` — also `a ≡ b (mod n)`, which the parser reads and `Stringize`
does not yet print — is the statement that `n` divides `a - b`: a relation between integers with
the modulus written once at the end of the line, which is how mathematics writes it and not the
remainder operator `a mod n`, which is a number. A chain `a ≡ b ≡ c (mod n)` is the conjunction of
its links. Decided for numbers (`5^6 = 1 (mod 7)` is `True`, `-1 = 9 (mod 10)` is `True`,
`37457 = 38201 (mod 10)` is `False`), `NaN` off the integers, and decided for symbols where the
difference is a polynomial every term of which the modulus divides: `(n - a)^2 = a^2 (mod n)` and
`(x + y)^5 = x^5 + y^5 (mod 5)` are `True`, while `(x + y)^4 = x^4 + y^4 (mod 4)` stays as written
because `6 x^2 y^2` is not a multiple of `4` for every `x, y`. Modulo `0` it is equality, and the
sign of the modulus is immaterial. `(mod n)` after an order comparison, or `≡` without a modulus,
is a parse error. In LaTeX, `a \equiv b \pmod{n}`. The one place implicit multiplication does not
apply: `b (mod n)` is not `b * (mod n)`, a bracket that opens on `mod` is a modulus. A linear
congruence is solved to a residue class — `"3 x = 11 (mod 7)".ToEntity().Solve("x")` is
`{ x in ZZ : x = 6 (mod 7) }`, `2 x = 3 (mod 6)` has none — a conjunction of congruences by the
Chinese remainder theorem, `x = 3 (mod 4) and x = 5 (mod 6)` giving `{ x in ZZ : x = 11 (mod 12) }`
and `x = 3 (mod 4) and x = 2 (mod 6)` nothing, and with a bounded interval the members are listed:
`x = 2 (mod 7) and 0 <= x and x < 30` is `{ 2, 9, 16, 23 }`. `MathS.NumberTheory.ModularInverse(3, 10)`
is `7`, the one representative in `[1, n - 1]`, and `null` where `a` and `n` share a factor. An
inequality with an exponential or a factorial over the whole numbers, `n in ZZ and 2^n > n^2`, is
solved to the members of a window from the least member and the tails the quantifier proves by
induction — `{ 0, 1 } \/ ZZ /\ [5; +oo)`, `n in ZZ+ and n! > 3^n` is `ZZ+ /\ [7; +oo)` — and where
a tail is not proved the window is not an answer, and the inequality solver refuses as before.
| 9 | `unite` `\/`, `setsubtract` `\` | one level, so `A \/ B \ C` is `(A \/ B) \ C` |
| 10 | `intersect` `/\` | |
| 11 | `+` `-` | |
| 12 | `*` `/` `mod` | one level, so `x * y mod z` is `(x * y) mod z` |
| 13 | `+` `-` | prefix |
| 14 | `^` | **groups to the right**: `a ^ b ^ c` is `a ^ (b ^ c)` |
| 15 | `!` | postfix factorial |

`%` is **not** an operator. It is left free to mean percent; the remainder is written `mod`.

`mod` is the floored remainder, `a - b * floor(a / b)`, which takes the sign of the **divisor**:
`(-7) mod 3` is 2 and `7 mod (-3)` is -2. This is the convention SymPy, Mathematica and Maxima
use, and the one under which the residues modulo n are the numbers from 0 to n - 1. C's `%`
truncates instead; it is a different operation and the library does not inherit it.

## Whitespace and comments

Newlines are skipped, so an expression may be written over several lines. `//` runs to the end of
the line and `/* … */` spans lines; both are dropped before parsing. A `//` comment is ended by a
newline or by the end of the input, so `x + 1 // done` and `x + 1 /* done */` are both `x + 1`. Until
[#1039](https://github.com/asc-community/AngouriMath/issues/1039) the newline was required and the
first of those was a parse error.

## Numbers and constants

`1`, `1.5`, `.5`, `1.`, `1/2`, `1e-9`, `i`, `e`, `pi`, `+oo`, `-oo`, `NaN`, `true`, `false`.

A trailing `i` makes the literal imaginary, exponent and all: `3i`, `1.5e3i`. `i` on its own is the
imaginary unit, and it is a **number** rather than a name — which is why `x i` is `x ^ i` and not a
product (below), why `ix` is an ordinary variable called `ix`, and why an operator that declares
`i` as an index has to say so.

Booleans are written `true` or `false`; `True` and `False` are read as well, because that is what
`Stringize` prints.

`NaN` is the value an undefined computation gives — `0/0` and `1/0` both reach it — and it is
spelled exactly that way, since only the exact word is reserved. A longer name containing it, such
as `NaNx` or `NaN_1`, is still a variable, because the lexer takes the longest match.

## Names

A name is **one or more letters**, optionally followed by `_` and one or more letters or digits:
`x`, `xy`, `x_1`, `x_a`, `α`, `ω_1`, `Θ_1`, `альфа`, `x_ω`. Letters are `a`–`z`, `A`–`Z`, Greek
(U+0370–U+03FF and U+1F00–U+1FFF), Cyrillic (U+0400–U+04FF) and the script `ℯ` (U+212F); no other
script is one, so `ﬁ` is a lexer error.

`pi` and `e` are the constants, and so are the spellings mathematics writes them in: `π` (U+03C0)
is `pi` and the script `ℯ` is `e`, so `sin(π)` is `0` and `ln(ℯ)` is `1`. The Cyrillic `е`
(U+0435) is a letter, not the constant, and so is the Cyrillic `х` (U+0445) beside the Latin `x`:
they are variables that print like `e` and `x`, and `х + x` has two
([#1260](https://github.com/asc-community/AngouriMath/issues/1260)).

Four things that look like names and are not:

| written | what it is |
|---|---|
| `_x`, `x_` | a lexer error. `_` may neither begin a name nor end one |
| `x_1_2` | a lexer error. At most one `_`, so subscripts do not nest ([#524](https://github.com/asc-community/AngouriMath/issues/524)) |
| `x1` | `x ^ 1`. A digit is not a letter, and a name beside a number is a power — see below |
| `sinx` | one name, spelled `sinx`. Letters written together are a single name, never a product of one-letter ones |

## Juxtaposition

Two things written next to each other have an operator inserted between them, and **which
operator depends on what comes second**:

| | inserted | |
|---|---|---|
| number, name or `)`, then a **name**, a function or `(` | `*` | `2x` is `2 * x`, `a(b + c)` is `a * (b + c)`, `x sin(x)` is `x * sin(x)`, `x(2)` is `x * 2` |
| number, name or `)`, then a **number** | `^` | `x2` is `x ^ 2`, `(x + 1)2` is `(x + 1) ^ 2`, `3 2` is `3 ^ 2` |

So `x2` is a square and `x(2)` is a product, and since `i` is a number, `x i` is `x ^ i` while
`2i x` is `2i * x`.

`MathS.Settings.ExplicitParsingOnly` turns the insertion off. Under it every row above is a
`MissingOperatorParseException` naming the two tokens it will not join, and only what is written
with an operator parses.

## Sets

| | |
|---|---|
| finite | `{ 1, 2, 3 }`, `{}` |
| interval | `[a; b]` closed, `(a; b)` open, `[a; b)` and `(a; b]` half-open |
| conditional | `{ x : x > 0 }` — the name before the `:` is declared, as under `sum` below |
| special | `RR` `CC` `ZZ` `QQ` `BB`, and the two subsets of `ZZ`: `ZZ*` = `{0, 1, 2, ...}`, `ZZ+` = `{1, 2, 3, ...}` |
| operations | `unite` `/\` … see the table above |
| power set | `powerset(A)` — the set of all subsets of `A`; listed for a finite `A` (`powerset({1, 2})` is `{ {}, {1}, {2}, {1, 2} }`, `powerset({})` is `{ {} }`), and for an infinite one an object that answers membership: `{1, 3} in powerset(ZZ)` is `True` |
| integer range | `ZZ /\ [1; 10]`, `ZZ+ /\ [-2; 3]`, `[0; 2.5] /\ ZZ*` — an integer set cut by a numeric interval lists its members (up to 4096 of them): `{ 1, 2, …, 10 }`, `{ 1, 2, 3 }`, `{ 0, 1, 2 }`. The `[n] = {1, …, n}` of combinatorics is `ZZ+ /\ [1; n]` |
| indexed union, intersection | `union(A_k, k in I)`, `intersection(A_k, k in I)` — `⋃_{k ∈ I} A_k` and `⋂_{k ∈ I} A_k`; the name is bound, as under `sum`. Folded over a listed index set (`union({k, 2 k}, k in ZZ+ /\ [1; 10])` is `{ 1, …, 10, 12, 14, 16, 18, 20 }`), and over any other an object whose membership goes through the quantifiers: `0 in intersection([0; 1/n), n in ZZ+)` is `True` and `0.1 in …` is `False`. Note that `i` is the imaginary unit, so an index is `k` or `n` |
| pattern | `{1, 2, ..., n}`, `{2, 4, ..., 2 n}`, `{5, 10, 15, ...}`, `{..., -1, 0}`, `{k - 2, ..., k + 2}` — also `…`; the dots name the arithmetic progression of whole numbers the shown terms determine (the step is the difference of the first two, or one when a single term is shown on each side), and the term after them names where it stops. Built from what exists: `ZZ /\ [1; n]`, a residue class cut by an interval, listed when the ends are numbers. Refused: terms with no common step (`{1, 4, 9, ...}` guesses nothing), a progression that is not of whole numbers, dots with nothing to determine them. In a sum or a product, `1 + 2 + ... + n` is `sum(k, k, 1, n)` and `1 * 2 * ... * n` is `product(k, k, 1, n)` |
| image, pre-image | `image(f(x), x in A)` is `{ f(x) : x in A }` — listed over a listed `A` (`image(x^2, x in {1, 2, 3})` is `{ 1, 4, 9 }`), an interval by interval arithmetic where the name occurs once (`image(9 c / 5 + 32, c in (0; 100))` is `(32; 212)`), and otherwise an object that answers membership; it is `union({f(x)}, x in A)`. `preimage(f(x), x in A, Y)` is `{ x in A : f(x) in Y }`, solved on evaluation where the membership is read: `preimage(x^2, x in RR, {1})` is `{ 1, -1 }` |
| complement | `complement(A, U)` — the complement of `A` relative to the universe `U`, which is `U \ A`; there is no universal set, so a complement is always relative to one |

**Subset.** `A subset B` — also `A ⊆ B`, which the parser reads and `Stringize` does not yet
print, and `B superset A` / `B ⊇ A`, which parse to the same node — is the statement that every
member of `A` is a member of `B`. It is the relation between two sets that `in` is between an
element and a set, and the two are different: `{1, 2} subset {1, 2, 3}` is `True` and
`{1, 2} in {1, 2, 3}` is `False`. It is decided from the shapes of the sets — member by member for
a finite set, along the chain `ZZ+ ⊂ ZZ* ⊂ ZZ ⊂ QQ ⊂ RR ⊂ CC` for the special sets, by the ends for
two intervals, by the set algebra for symbolic sets (`A /\ B subset A`, `A subset A \/ B`,
`A \ B subset A` are `True` of any `A` and `B`) — and, for a set builder, through the solver:
`{ x in RR : x^2 = 5 } subset ZZ` is `False`. A statement about sets: with a number or a truth
value on a side it is `NaN`. **Two sets are equal exactly when each is a subset of the other**,
and that is how `=` between sets is decided: `{ x in ZZ : x >= 1 } = ZZ+` is `True` and
`[0; 1] = [0; 2]` is `False`; a pair the subset decision leaves open stays written. `card(S)` counts
a listed set whose members are numbers or listed sets of them (`card({1, {}})` is `2`), and
`card(powerset(S))` is `2^card(S)`. The size of an infinite set is an aleph, `aleph(k)` (LaTeX
`\aleph_k`), or a power of 2 of one: `card(ZZ)`, `card(QQ)` and `card(PP)` are `aleph(0)`, and
`card(RR)` is `2^aleph(0)` — an aleph too, but which one is the continuum hypothesis, which ZFC does
not decide, so `2^aleph(0) = aleph(1)` stays as written. A sum or a product of sizes, one of them
infinite, is the larger (`aleph(0) + 1` is `aleph(0)`), and sizes compare by what ZFC proves:
`card(ZZ) < card(RR)` and `2^aleph(0) >= aleph(1)` are `True`, and a set is smaller than its power
set whatever it is, so `card(S) < card(powerset(S))` is `True`. A subset is no larger than its
superset. `card(ZZ) = +oo` stays as written: an infinity is not a size.

There is **no universal set** and no literal for one. A set that constrains nothing is the
conditional set `{ x : True }`, which prints, reads back, compares and answers membership like any
other; `Domain.Any` is a codomain and not a candidate for the job
([#996](https://github.com/asc-community/AngouriMath/issues/996)).

## Matrices and vectors

`[1, 2, 3]` is a column vector (3 by 1), `[[1, 2], [3, 4]]` a matrix, `[1, 2, 3]T` a transpose
(1 by 3). There is no empty vector: `[]` is not a value this library has
([#1028](https://github.com/asc-community/AngouriMath/issues/1028)), while the empty set `{}` is.

`(|x|)` is the absolute value of `x`, and prints back as `abs(x)`.

## Functions

Everything below is written `name(argument, ...)`.

**Trigonometric** — `sin` `cos` `tan` `cotan` `cot` `sec` `cosec` `csc`, and the inverses
`arcsin` `arccos` `arctan` `arccotan` `arccot` `arcsec` `arccosec` `arccsc`, each also spelled
`asin` `acos` `atan` `acotan` `acot` `asec` `acosec` `acsc`.

**Hyperbolic** — `sinh` `sh` `cosh` `ch` `tanh` `th` `cotanh` `coth` `cth` `sech` `sch`
`cosech` `csch`.

**Inverse hyperbolic** — the inverse of a hyperbolic function is an *area*, not an arc, so it is
`ar-` and not `arc-`. Each has a long spelling, an `a-` spelling and at least one short one:

| | | |
|---|---|---|
| sine | `arsinh` `asinh` `arsh` | `arcsinh` is **refused** |
| cosine | `arcosh` `acosh` `arch` | `arccosh` is **refused** |
| tangent | `artanh` `atanh` `arth` | `arctanh` is **refused** |
| cotangent | `arcotanh` `acotanh` `arcoth` `acoth` `arcth` | `arccotanh` is **refused** |
| secant | `arsech` `asech` `arsch` | `arcsech` is **refused** |
| cosecant | `arcosech` `acosech` `arcsch` `acsch` | `arccosech` is **refused** |

A refused spelling raises a parse error that names the accepted ones. It is refused rather than
ignored because an unknown name followed by a bracket is a product (above), so `arcsinh(x)` left
alone would quietly be a variable times `x`.

Both hyperbolic families are **rewritten as they are parsed** and are not nodes of their own:
`sinh(x)` becomes `(e ^ x - e ^ (-x)) / 2` and `arsinh(x)` becomes `ln(x + sqrt(x ^ 2 + 1))`.
So they do not print back as themselves — what round-trips is the expression, not the spelling.
The same goes for `cbrt(x)`, which is `x ^ (1/3)`, `sqr(x)`, which is `x ^ 2`, and `exp(x)`,
which is `e ^ x`.

**Powers and logarithms** — `sqrt` `cbrt` `sqr` `pow(a, b)` `exp` `ln` `log(base, x)`. `log` with
one argument is base 10, so `log(100)` is 2; `log10` and `log2` say it in the name.

**Other** — `abs` `signum` `sgn` `sign` `phi` `gamma` `factorial` (or postfix `!`); `prime(n)`
for the `n`-th prime (`prime(25)` is `97`, `NaN` off the positive whole numbers, left as written
for a symbol or an index past a million); `valuation(n, p)` for the `p`-adic valuation, the
exponent of the prime `p` in `n` (`valuation(12, 2)` is `2`, `valuation(12, 5)` is `0`, `+oo` at
`n = 0`, `NaN` where `p` is not prime; `v_p(n)` in LaTeX); `#S` (or `card(S)`) for the number of elements of a set, counted for a finite set of numbers and left as written for an infinite one; `#` binds like a function call, so a set expression needs parentheses (`#(A \/ B)`).

**Rounding** — `floor` `ceil` (`ceiling` is accepted on the way in and prints as `ceil`) `round`.
All three round a complex argument componentwise. `floor` and `ceil` go toward the infinities
rather than toward zero, so `floor(-3/2)` is `-2`; `round` goes to the **nearest even** on a tie,
so `round(1/2)` is `0` and `round(5/2)` is `2`, which is what Python, SymPy, Mathematica and
IEEE 754 all mean by rounding — and is *not* `floor(x + 1/2)`.

**Comparison and divisors** — `min(a, b, ...)` `max(a, b, ...)` `gcd(a, b, ...)` `lcm(a, b, ...)`.
All four take any number of arguments and fold. `min` and `max` compare only where the arguments
are ordered and are otherwise left as written. `gcd` and `lcm` compute over integers and rationals
— `gcd(1/2, 1/3)` is `1/6`, `lcm(1/2, 1/3)` is `1`, `lcm(4, 6)` is `12`, `lcm(0, n)` is `0` — and
leave the polynomial case alone.

**Combinatorics** — `binomial(n, k)`, "n choose k". A whole `k` is computed by the falling
factorial `n (n - 1) ... (n - k + 1) / k!` for any `n` — `binomial(5, 2)` is `10`,
`binomial(-1, 3)` is `-1`, `binomial(1/2, 2)` is `-1/8` — with `0` below `k = 0` and above a
non-negative whole `n`; a symbolic argument keeps the node, which prints as `\binom{n}{k}` in
LaTeX, and a `k` that is not whole evaluates numerically through the gamma function. A `sum`
over the coefficient is answered in closed form: the binomial theorem
(`sum(binomial(n, k) x^k y^(n - k), k, 0, n)` is `(x + y)^n`), a polynomial in the index beside
it (`sum(k binomial(n, k), k, 0, n)` is `n 2^(n - 1)`, read in the falling factorial basis),
Vandermonde's convolution (`sum(binomial(a, i) binomial(b, k - i), i, 0, k)` is
`binomial(a + b, k)`, and `sum(binomial(n, k)^2, k, 0, n)` is `binomial(2 n, n)`), the summation
identity (`sum(binomial(i, k), i, 0, n)` is `binomial(n + 1, k + 1)`), and the sums over the even
or the odd indices (`sum(binomial(n, 2 l), l, 0, floor(n/2))` is `2^(n - 1)` for `n >= 1`) — the
identities of chapter 8 of Sullivan and Mackey's *An Introduction to Proofs*, so that
`forall n in ZZ+ : sum(k binomial(n, k), k, 0, n) = n 2^(n - 1)` is `True`.

**Iverson bracket** — `iverson(P)`. `1` where the statement `P` holds and `0` where it does not,
which is how a count is written as a sum: `sum(iverson(k divides 12), k, 1, 12)` is `6`, the number
of divisors of `12`. A statement that is not decided keeps the node, which prints as `[\![P]\!]` in
LaTeX, and so does a number, which is not a statement. A sum of a bracket whose statement bounds the
index, fixes it, fixes its residue or makes it a perfect power is counted in closed form, however long the range, with `not`
and `or` counted by inclusion and exclusion: `sum(iverson(2 divides k or 3 divides k), k, 1, 1000)`
is `667`, and `sum(iverson(not 2 divides k and not 5 divides k), k, 1, n)` is
`n - floor(n/2) - floor(n/5) + floor(n/10)` for a whole `n >= 0`.

**Error functions** — `erf(z)` `erfc(z)` `erfi(z)`. The error function
`2/sqrt(pi) int_0^z e^(-t^2) dt`, its complement `1 - erf(z)`, and the imaginary error function
`-i erf(i z)`, which on the real line is `2/sqrt(pi) int_0^x e^(t^2) dt`. Each is entire, is
evaluated to the working precision anywhere in the complex plane, and is differentiated from its
definition: the derivative of `erf(x)` is `2 / sqrt(pi) * e ^ (-x ^ 2)`. `erf(0)` is `0`, `erfc(0)`
is `1`, and `erf(+oo)` is `1`. An equation in one of them is left as the set of `x` for which it
holds, since the library writes none of their inverses.

**Exponential and logarithmic integrals** — `Ei(z)` `li(z)`. The exponential integral
`Ei(z) = gamma + (ln z - ln(1/z))/2 + sum z^k/(k k!)`, which on the real line is the principal
value of `int_-oo^x e^t/t dt`, and the logarithmic integral `li(z) = Ei(ln z)`, which on `x > 0` is
the principal value of `int_0^x dt/ln t`. Both are real on the real line, `li` for `x >= 0`; the cut
of `Ei` is the negative real axis, on which it keeps its real value, and it is `i pi` more just above
it. Each is evaluated to the working precision anywhere in the complex plane and differentiated
from its definition: the derivative of `Ei(x)` is `e ^ x / x`, and of `li(x)` it is `1 / ln(x)`.
`Ei` is undefined at `0` and `li` at `1`; `li(0)` is `0`.

**Sine and cosine integrals** — `Si(z)` `Ci(z)` `Shi(z)` `Chi(z)`. The sine integral
`Si(z) = sum (-1)^k z^(2k + 1)/((2k + 1) (2k + 1)!)`, which on the real line is `int_0^x sin t/t dt`,
and the cosine integral `Ci(z) = gamma + ln z + sum_(k >= 1) (-1)^k z^(2k)/(2k (2k)!)`, which on
`x > 0` is `-int_x^oo cos t/t dt`; the hyperbolic `Shi` and `Chi` are the same sums without
the alternating sign, `int_0^x sinh t/t dt` and `gamma + ln x + int_0^x (cosh t - 1)/t dt`. `Si` and
`Shi` are entire, odd, and real on the real line. `Ci` and `Chi` take the cut of `ln z`, the negative
real axis, which keeps the value from above: for `x > 0`, `Ci(-x)` is `Ci(x) + i pi` and `Chi(-x)` is
`Chi(x) + i pi`. Each is evaluated to the working precision anywhere in the complex plane and
differentiated from its definition: the derivative of `Si(x)` is `sin(x) / x`, of `Ci(x)` it is
`cos(x) / x`, and of `Shi(x)` and `Chi(x)` it is `sinh(x) / x` and `cosh(x) / x`. `Ci` and `Chi` are
undefined at `0`; `Si(+oo)` is `pi / 2` and `Ci(+oo)` is `0`.

**Calculus** — `derivative(expr, var, order)`, `integral(expr, var)`,
`integral(expr, var, from, to)`, `limit(expr, var, dest)`, `limitleft(...)`, `limitright(...)`;
`sum(expr, var in set)` for a sum over a set; `max(expr, var in set)` and `min` for the extremum of
an expression over a set, `argmax` and `argmin` for the set of points where it is taken —
`max(a, b)` of two values is still the larger.
`min(S)` with one argument that is a set is its least member, `min(x, x in S)`, and the range
may carry conditions, `min(x, x in PP and x > 14)` being the least of `{ x in PP : x > 14 }`;
decided for `PP`, `ZZ+`, `ZZ*` and their cuts by an interval or bounds (`min(ZZ+)` is `1`,
`min(x, x in ZZ+ and x >= 7/2)` is `4`, `min(x, x in PP and x > 14 and x < 20)` is `17`), and
left as written where there is no least member. `max` the same way from above:
`max(x, x in PP and x < 14)` is `13`, the previous prime, `max(ZZ /\ (-oo; 5))` is `4`, and
`max(PP)` stays as written.

`derivative` takes an order and `integral` does not: `derivative(f, x, 2)` is the second
derivative, while `integral`'s third and fourth arguments are the bounds of a definite integral,
not a count. `integral(f, x, 2)` is therefore a parse error rather than the second antiderivative
— three arguments name neither form. This is deliberate; the iteration count was replaced by the
range in [#657](https://github.com/asc-community/AngouriMath/pull/657).

**Structural** — `piecewise(a provided p, b provided q)`, `lambda(param, body)`,
`apply(f, arg, ...)`, `domain(expr, set)`.

A lambda also has an arrow form, `param => body`, with several parameters written next to each
other: `a b => a + b` is `a => b => a + b`, which is `lambda(a, b, a + b)`. The arrow binds
looser than everything else and its body runs to the end, so `a => a + 3` is `a => (a + 3)`.
Every parameter must be a name — `a 3 => 3` is refused — and the body is read exactly as
`lambda(...)` reads it, so an index called `i` means the name and not the imaginary unit.

**Quantifiers.** `forall x in S : P`, `exists x in S : P` and `exists! x in S : P` — also `∀`,
`∃` and `∃!`, which the parser reads — are the statements that every, some, or exactly one member
of `S` satisfies `P`. The set is mandatory, because a statement is quantified over something:
`forall x : P` is refused. Several names may share a set, `forall x, y in RR : 2 x y <= x^2 + y^2`,
and several sets may be listed, `forall a in ZZ, b in ZZ+ : ...`; either way the quantifiers nest
from the left and print nested. The body runs to the end of the line, as a lambda arrow's does, so
a quantifier before a connective is bracketed: `(forall x in RR : x^2 >= 0) and Q`. After one —
`and`, `or`, `xor`, `implies`, `not` — it needs no brackets and takes the rest of the line there
too: `n >= 2 implies exists a in ZZ* : n = 2 a + 1` is `n >= 2 implies (exists a in ZZ* : n = 2 a + 1)`,
and `not forall x in S : P` is the negation of the whole statement.
The name is bound throughout the body and the set, so `forall x in RR : x < y` is a statement
about `y`. Decided over a finite set by evaluating the body at every member (`forall x in {1, 2,
3, 4} : exists y in {3, 4, 5, 6, 7, 8} : x + y = 7` is `True`), over `BB` as over `{True,
False}`, and over an infinite set by a counterexample (`forall n in ZZ : n^2 <= n^3` is `False` at
`n = -1`) or by solving where the solver reads the body (`forall x in RR : x^2 >= 0` is `True`,
`exists x in RR : x^2 + 1 = 0` is `False` and `exists x in CC : x^2 + 1 = 0` is `True`); left as
written otherwise, never guessed. A body that is a `piecewise` — what `sum` and `product` fold to,
`sum(k, k, 1, n)` being `(n + n^2)/2 provided n >= 0` — is read as the case whose condition holds
at every member, so `forall n in ZZ+ : sum(k, k, 1, n) = n (n + 1)/2` is `True`. An equation whose
sides differ by a polynomial that expands to nothing is `True` over any set, where a polynomial is
read over *atoms*: whatever is not a sum, product, quotient or whole power is one indeterminate,
a whole shift in an exponent is a factor (`2^(n + 1)` is `2 · 2^n`) and a factorial of a shifted
argument is the smaller one times the numbers between (`(n + 2)!` is `(n + 2)(n + 1) n!`), so
`forall n in ZZ+ : (n + 1)! + (n + 1)(n + 1)! = (n + 2)!` is `True`. And **a statement about a sum
or a product up to `n`, over the whole numbers from some least one, is decided by induction**:
`forall n in ZZ+ : sum(1/(k (k + 1)), k, 1, n) = n/(n + 1)` is `True` because it holds at `n = 1`
and, with the sum to `n + 1` unfolded to the sum to `n` plus one term and that sum replaced by
`n/(n + 1)`, the statement at `n + 1` is an identity; `forall n in ZZ+ /\ [2; +oo) : ...` starts at
`2`. A difference of quotients is put over one denominator, and the identity is claimed where the
denominator is not zero: decided over the set for the quantified name (`n + 1` is never `0` on
`ZZ+`), and stated as a condition for a free parameter (`forall n in ZZ+ : sum(1/(a k (k + 1)), k, 1, n) =
n/(a (n + 1))` is `True provided not a = 0`). Only the shape with one sum or product
alone on a side of an equation is read this way. A sum whose closed form has a case on a free
parameter — `q = 1` beside `n >= 0`, which is how `sum(q^i, i, 0, n - 1)` comes — is decided
**by cases on the parameter**: the statement under the hypothesis and under its negation, and it
holds provided the hypothesis where it holds there alone, so Sullivan and Mackey's Ex 5.2.6,
`forall n in ZZ+ : sum(q^i, i, 0, n - 1) = (q^n - 1)/(q - 1)`, is `True provided not q = 1` —
which is the book's own remark on why `q = 1` is excluded. **An inequality with an exponential or a factorial in `n`** is
decided by induction too, with the step read off a multiple of the hypothesis: writing it as
`P(n) >= 0`, `P(n + 1) = c P(n) + D` for a `c >= 0` (one, the base of an exponential, `n + 1`
beside a factorial) and a `D` whose sign is read from its shape — a positive base to any power, a
factorial, a polynomial in `n` decided over the set, sums and products of these — so `forall n in
ZZ+ /\ [5; +oo) : 2^n > n^2` is `True` (`2^(n + 1) - 2 · 2^n` leaves `n^2 - 2 n - 1`, positive
from `5`), `forall n in ZZ+ /\ [4; +oo) : 2^n > n^2` is `False` at `4`, and `forall n in ZZ+ :
n! >= 2^(n - 1)` is `True`. The whole numbers from `m` are `ZZ*` shifted by `m`, so a statement
over `ZZ+ /\ [4; +oo)` is decided as one about `4 + t` over `ZZ*`, whichever route reads it. A
divisibility or a congruence in a polynomial of `n` with whole coefficients repeats with the
modulus, so its residues decide it (`forall n in ZZ : 6 divides n^3 + 5 n` is `True` by six cases),
and with a power `a^n` of a whole base prime to the modulus among the terms it repeats with the
order of `a` as well: `forall n in ZZ+ : 3 divides 7^n - 4^n` is `True` by one residue, `forall n
in ZZ* : not (7 divides 2^n + 1)` by three — over the non-negative whole numbers, where the powers
are whole; a base sharing a factor with the modulus repeats only eventually and is left as written. `not`
passes through a quantifier by flipping it: `not forall x in S : P` is `exists x in S : not P`. In LaTeX, `\forall x \in S : P`. `forall`, `exists` and
`exists!` are keywords, so they are not names. **How a statement was decided** is recorded by
`ProofRecording`: inside `using var proof = ProofRecording.Start();` every decision made names its
rule — as Sullivan and Mackey name it, and with the Lean 4 tactic a checker would use — with its
sub-decisions one level below it, so the proof of `forall n in ZZ+ : sum(1/(k (k + 1)), k, 1, n) =
n/(n + 1)` reads as its base case, the identity of its step and the induction that closes them
(`proof.Written()`); what a route tried and abandoned is not among the steps. A decision already
made is not made again, so a proof is asked of a fresh entity (`MathS.FromString(text, useCache: false)`).

`domain` takes a special set — `CC` `RR` `QQ` `ZZ` `ZZ*` `ZZ+` `PP` `BB` — or the keyword `Any`, and
sets the *codomain* of whatever node it wraps, which is what makes the expression evaluate to
`NaN` outside it (`domain(x, ZZ+)` at `x = 0` is `NaN`). It applies to any node, not only a
variable, and it is what `Stringize` prints for one. `Any` is the one spelling here that is not a
set: it removes the restriction rather than naming values.

There is **no set called the natural numbers**: the name means `{0, 1, 2, ...}` to some authors
and `{1, 2, 3, ...}` to others, so following MathWorld's recommendation the two are spelled
`ZZ*` and `ZZ+` (`\mathbb{Z}^{*}`, `\mathbb{Z}^{+}`) and neither claims the word; `NN` is refused
with the two spellings rather than read as a variable. Membership is decided for numbers (`0 in
ZZ*` is `True`, `0 in ZZ+` is `False`, `-3 in ZZ*` is `False`) and left as written for a symbol.
**`PP` is the set of primes** `{2, 3, 5, 7, ...}` (`\mathbb{P}`, SymPy's `S.Primes`):
`7 in PP` is `True`, `8 in PP` and `1 in PP` are `False`, `x in PP` stays as written, and a
whole number past the machine word is left open rather than tested slowly. It is cut by an
interval to its members where there are few enough to list — `PP /\ [1; 30]` is
`{ 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 }`, so `card(PP /\ [1; n])` is the prime-counting function,
`25` at `100` — and `min(PP)` is `2`, `min(x, x in PP and x > 14)` the next prime, `17`
([#1450](https://github.com/asc-community/AngouriMath/issues/1450)). The eight special sets nest,
`PP ⊂ ZZ+ ⊂ ZZ* ⊂ ZZ ⊂ QQ ⊂ RR ⊂ CC` with `BB` beside them, and the set operators answer for two
of them: `ZZ unite QQ` is `QQ`, `BB intersect ZZ` is `{}`, `ZZ* \ ZZ+` is `{ 0 }`, `ZZ \ ZZ*` is
`{ x in ZZ : x < 0 }`, `ZZ+ \ PP` is `{ x in ZZ+ : not x in PP }` and `QQ \ ZZ` is
`{ x in QQ : not x in ZZ }`. The tokens take the sign: `ZZ*2` no longer reads as `ZZ * 2`.

**Refused by name** — `trunc` `conjugate`. AngouriMath has none of these, and each is
what some other CAS calls a function, so a caller reaches for it. Left alone they would be read as
products under the rule above and answer silently and wrongly; they raise a parse error naming the
function instead. `re` and `im` are the same case and are *not* refused, being short enough to be
somebody's variable.

## `sum` and `product`, which declare a name

`sum(body, name, from, to)` and `product(body, name, from, to)`. The **second** argument is the
name being declared and the first is the body it runs over, so `sum(i, i, 1, 10)` is
1 + 2 + … + 10 = 55, and `sum(k, k, 1, 10)` is the same number written over a different name.

Both bounds are inclusive and the step is one. An empty range is the operator's identity —
`sum(k, k, 5, 1)` is `0` and `product(k, k, 5, 1)` is `1` — and the range is written out only when
both bounds are integers and there are not too many terms.

A `sum` whose body is a **polynomial in the index** is answered in closed form instead of being
written out, so `sum(k, k, 1, n)` is `(n + n^2)/2` and `sum(k, k, 1, 100000)` is `5000050000`
rather than a hundred thousand terms. The answer carries the condition it needs — `to >= from - 1`
— because below that the range is empty and the polynomial is not zero; where the bounds are
concrete that condition is decidable and the answer is a number. A bound that is a number and not
a whole one still stays as written, since the index runs over the integers and
`sum(k, k, 1, 5/2)` is `1 + 2` rather than the polynomial at `5/2`.

A `sum` whose body is a **polynomial in the index times a power with the index in the exponent**
is answered in closed form too: `sum(k * 2^k, k, 1, n)` is `(n - 1) 2^(n + 1) + 2`, and
`sum((-1)^(k - 1) k^2, k, 1, n)` is `(-1)^(n - 1) n (n + 1)/2` — §5.3.4 Try 6 of Sullivan and
Mackey's *An Introduction to Proofs*. The discrete antiderivative is `q(k) r^k` with `q` a
polynomial of the same degree, found by one division by `r - 1` per coefficient, so a symbolic
ratio carries the case `r = 1` separately, where the sum is the polynomial's. To `+oo` the series
converges for `|r| < 1`: `sum(k / 2^k, k, 1, +oo)` is `2`, and `sum(k x^k, k, 0, +oo)` is
`x/(1 - x)^2 provided |x| < 1`.

A sum **to `+oo`** is answered where it converges — the geometric, exponential and binomial series
have closed forms — and, failing that, by the **nth-term test**: terms that do not tend to zero
mean no finite value, and a limit with a sign says which infinity, so `sum(2^k, k, 0, +oo)` and
`sum(k, k, 1, +oo)` are `+oo`. Terms that *do* tend to zero are left as written, because that is
the case the test says nothing about — `sum(1/k, k, 1, +oo)` diverges and `sum(1/k^2, k, 1, +oo)`
converges and neither is decided by the size of a term. A summand that can fail to exist at some
index is left alone too, since one undefined term makes the sum undefined rather than infinite.

A `product` gets the same treatment over the narrower class its shape allows: a **monomial** in
the index, since a product has no linearity to take a sum of terms apart with. So
`product(k, k, 1, n)` is `factorial(n)`, `product(k ^ 2, k, 1, n)` is `factorial(n) ^ 2`, and
`product(c, k, m, n)` is `c ^ (n - m + 1)`. Its condition is `to >= from` rather than
`to >= from - 1`, because at the empty range itself the closed form would be `c ^ 0` — undefined
at `c = 0`, where the empty product is `1`. Where the index appears in the body the lower bound
must be a concrete integer of at least one, `factorial` having no value at the negative integers;
`product(k, k, 0, n)` therefore stays as written.

`sum(body, name in set)` is a sum **over a set**: the body at each member of the set, each member
counted once, added up. `sum(x^2, x in {1, 2, 3})` is `14`. A sum over the roots of a polynomial is a
sum over its set of roots, `sum(w^2, w in { w : w^3 - 2w + 1 = 0 })`, which is `4`, and it needs no
index and no order, a set having neither. The terms are added up where the set is finite and its
members are known: a listed set of numbers, or roots that are rational, roots of a quadratic or roots
of a binomial, which is checked by counting them against the polynomial's number of distinct roots.
The roots of an irreducible cubic or quartic are not written out in radicals, which read no more
simply than the sum. A set whose members might coincide,
such as `{ a, b }`, and a set that is not finite are left as written. Evaluated to a number, roots
the solver cannot write are found numerically, all of them to the working precision or none, so
`sum(w^5, w in { w : w^5 + w + 3 = 0 })` evaluates to `-15`.

**The declared name means the name, and only inside the operator that declares it.** Two names
show this, because both mean something else in this language:

- `i` is the imaginary unit, so `sum(i, i, 1, 10)` is `55` and not a sum of imaginary units.
  Outside, it is the unit again: `sum(i, i, 1, 3) + i` is `6 + i`, and `sum(sqrt(-1) * i, i, 1, 10)`
  is `55i` — the factor is the unit, the index is the name. `2i` is a single number token, so
  `sum(2i, i, 1, 3)` is `12`: two times the index, three times over.
- `pi` and `e` are constants, so `product(pi, pi, 1, 4)` is `24` and `sum(e, e, 1, 3)` is `6`.

A declared name that outlives the operator declaring it is renamed, since `i` and `pi` are not
names the parser can produce: `derivative(pi ^ 2, pi)` is `2 * pi_1`.

Every binder in the language reads its name this way — `integral`, `derivative`, `limit`, `lambda`
and the set builder `{ x : … }` alike. `lambda` is the one that will not take a number: its
parameter is typed as a name, so `lambda(2, x)` is a parse error where `sum(k, 2, 1, 3)` is not.

## Where it is easy to be caught out

- **`^` groups to the right.** `2 ^ 2 ^ 3` is 256, not 64. Write `(2 ^ 2) ^ 3` for the other.
- **A name beside a number is a power.** `x2` is `x ^ 2`; `x(2)` is `x * 2`.
- **Letters run together are one name.** `sinx` is a variable called `sinx`, not `sin(x)` and not
  a product. Write the bracket.
- **An unknown name with a bracket is a product**, not an error: `f(x)` for an unknown `f` is
  `f * x`. That is what makes `a(b + c)` work, and it is why the names above are refused
  individually rather than by a general rule.
- **`%` is not the remainder.** Write `mod`.
- **Intervals use `;`**, not `,`: `[1; 2]`. `[1, 2]` is a vector.
- **`->` is implication**, not a lambda arrow. The lambda arrow is `=>`, and the two are
  different operators rather than spellings of one.
