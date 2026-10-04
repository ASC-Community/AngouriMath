# Behavioural and breaking changes

AngouriMath puts mathematical correctness ahead of backward compatibility. An API that returns the
wrong answer is not an asset to preserve, so when the two disagree the answer changes — see
[AGENTS.md](AGENTS.md). This file is the other half of that bargain: every place where the same
input now gives a different result, what it gives instead, and why.

Most of what follows is a wrong answer becoming a right one. That is still a breaking change if you
built on the wrong one, and the entries say so plainly rather than filing it under "fixed".

Each entry gives the value on the previous release and the value now, both measured on a build
rather than taken from the diff, with the issue and the pull request it came from. **Silent** marks
a change where the call still succeeds and quietly returns something else — those are the ones to
read first.

---

## Spellings known to be at risk — nothing here has changed yet

Every other entry in this file is something that **has** happened, measured on two builds. This
short section is the opposite: input that parses today, is already known to read as something other
than what it says, and is therefore a poor thing to build on. It exists so that the warning reaches
a caller reading this file rather than only the people reading the issue tracker.

The docket of changes waiting for a major version — including these, with the migration argument
and whatever gets decided — is
[#1019](https://github.com/asc-community/AngouriMath/issues/1019). Deliberately not restated here,
so that there is one list rather than two that drift apart.

Measured on `94ba5ff6`:

| Written | Reads today as | |
|---|---|---|
| `{ x \| x > 0 }` | `{ x divides x > 0 }` — a `FiniteSet` of **one** element, that element a divisibility | ordinary set-builder notation, read as a one-element set: `\|` is divisibility now (see *`\|` is divisibility, and was disjunction* below), and on `281e0d0c` the element was the disjunction `x or x > 0` |
| `a != b` | `a! = b` — the factorial of `a`, equated to `b` | `!=` is not a token, so the lexer takes `!` as the postfix factorial and `=` as equality ([#1225](https://github.com/asc-community/AngouriMath/issues/1225)) |

Both are well-formed, silent, and unrelated to what was written. Until they are settled, write
`{ x : x > 0 }` for a set builder, which is what the library prints, and `<>` rather than `!=`.
A not-equal written `<>` is read as `not (a = b)`, and printed that way.

---

## Unreleased — since 2.5.0

### `|` is divisibility, and was disjunction

**Read this one first.** It is the only entry here that changes what existing input *means* rather
than what an answer looks like, and it changes it **silently**: an expression that used `|` still
parses, and answers something else.

| | Was | Is |
|---|---|---|
| `"2 \| 6".ToEntity()` | `2 or 6`, a disjunction of two numbers | `2 divides 6`, which evaluates to `True` |
| `"x > 0 \| x < -1".ToEntity()` | `x > 0 or x < -1` | `x > 0 divides x and 0 divides x < -1` — divisibility binds tighter than a comparison, so the operands regroup |
| `"A \| B".ToEntity()` on booleans | `A or B` | `A divides B`, which is `NaN` — divisibility is a statement about integers |
| `"{ x \| x > 0 }".ToEntity()` | a one-element `FiniteSet` holding `x or x > 0` | a one-element `FiniteSet` holding `(x divides x) > 0` |

**`or` is unaffected and always was the primary spelling** — it is what the library prints, so a
round-tripped expression never contained a `|` in the first place. Replacing `|` with `or` restores
the old reading exactly, everywhere.

`a | b` is the statement that `b` is a whole multiple of `a`, the same node `a divides b` has built
since [#1220](https://github.com/asc-community/AngouriMath/pull/1220), and it takes that
precedence. It is defined over the integers and is `NaN` over anything else, the way an inequality
is over a non-real number. `0 | 0` is `True`; `0 | b` is otherwise `False`.

**Why now, and why in one step.** `|` was the one spelling in the grammar that means something else
in mathematics than we read it as — it is divides, "such that", "given", and the delimiter in
`|x|`, and none of those is disjunction, which is `∨`. Proposed with a two-release migration
through a parse error; the maintainer's decision on
[#1212](https://github.com/asc-community/AngouriMath/issues/1212) was to make the change directly,
since semantic versioning admits it.

Measured on the suite: **7 of 8,525 tests** referenced the old reading — five boolean-solver inputs
written `A | B`, and two that existed to record this spelling as at risk.

### The last digits of an evaluated sine, cosine, tangent, arcsine or arctangent

**Silent, and in the hundredth digit.** `EvalNumerical` at the default hundred digits gives the
correctly rounded value of these functions now, where it gave the last digits of a series that
had stopped a few units short. Measured on `3a5e2798` and on this change, against mpmath at a
hundred and thirty digits:

| | Was | Is |
|---|---|---|
| `sin(1)` | `…435430526957` (2 units below the true value in the last place) | `…435430526959` (exact) |
| `cos(1)` | `…560871830896` (3 above) | `…560871830893` (exact) |
| `tan(1)` | `…965202242983` (1 below) | `…965202242984` (exact) |
| `sin`, `cos`, `tan` of `3 ± 2i` | 16 to 21 units off in one part | within 2 |
| `arcsin`, `arccos` of `3 ± 2i` | 3 units off in the real part, 14 in the imaginary | 5 and 14 -- the imaginary part is the composition's own rounding, and the real part is two units worse |
| `(3 + 2i)^(3 + 2i)` | 289 units off | 289 -- the complex power's own path, untouched |

Of the twenty values pinned in `NumericDigits`, seventeen moved towards the true value or stayed,
and the complex arcsine's real part moved two units away. A caller comparing a hundred-digit
result to a pinned string will see the last digits differ; a caller comparing to a tolerance
will not. [#1338](https://github.com/asc-community/AngouriMath/issues/1338).

### The last digits of an evaluated logarithm, exponential, real power or complex power

**Silent, and in the hundredth digit.** `ln`, `log`, `e^x`, a real base to a real power and the
complex power and inverse trigonometric functions that are built on them are series in fixed
point now rather than PeterO's `Log`, `Exp` and `Pow`, and `e^x` is the exponential of `x` rather
than the constant's hundred digits raised to `x`. Measured on `eb2a5ac2` and on this change,
against mpmath at a hundred and forty digits, in units of the last of a hundred places:

| | Was | Is |
|---|---|---|
| `ln(pi)` | 5.3 off | 0.3 |
| `log(-2, 3 + 2i)` | 7.7 in the real part, 33.5 in the imaginary | 0.7 and 0.5 |
| `arcsin`, `arccos` of `3`, `4`, `i`, `3 ± 2i` | 5 to 28 off in the imaginary part | within 1.4 |
| `arctan(3 + 2i)` | 33 off in the imaginary part | 3.1 |
| `2^i` | 1.3 and 1.2 | 0.3 and 0.2 |
| `(3 ± 2i)^(3 ± 2i)` | 22 to 289 off | 0.5 to 5.6 |
| `e^700` | 97 correct digits | 100 |

Nineteen of the values pinned in `NumericDigits` moved, every one towards the true value. A caller
comparing a hundred-digit result to a pinned string will see the last digits differ; a caller
comparing to a tolerance will not. A value within ten to the minus fifty of an integer -- half
the working digits, or `MathS.Settings.PrecisionErrorZeroRange` where a caller has set it -- is
still that integer, so `e^(-123.456)` is 0 as it was.
[#1338](https://github.com/asc-community/AngouriMath/issues/1338).

### The factorial far from zero, and a quotient by a number whose modulus is nearly whole

The numerical factorial is Spouge's approximation, and it holds right of zero. Used left of zero it
lost its digits as the argument neared `-a`, where `a` is a tenth over the precision's digits. Past
`-a` it had none: at 30 digits `(-38.0785)!` came out `-3.1e-35`, where it is `7.03e-43`. Left of
zero it now takes the reflection formula, `x! = pi x/(sin(pi x) (-x)!)`. Its working context also
bounded the exponent where the caller's did, so an intermediate outside that range lost its digits
though the value fit. And the complex division read a divisor's modulus through `Abs()`, which
turns the downcasting on, so even with it off a modulus within its tolerance of a small rational
was that rational: `75 + 316.22776601683796i`, whose modulus is `325 + 2.6e-17`, divided as by 325.
The factorial's series, which runs with the downcasting off, cancels such quotients almost
entirely, which made `(316.22776601683796i)!` 24 orders of magnitude out. `Abs()` now makes the
modulus under the downcasting setting in force, so with it off the modulus is as it is, and so is
the division's; with it on, the rounding is the setting's own and stays
([#1614](https://github.com/asc-community/AngouriMath/issues/1614),
[#1615](https://github.com/asc-community/AngouriMath/issues/1615)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(400.5)!".EvalNumerical()` | `+oo` | `1.2818906666758219960…E+870` |
| `"(316.22776601683796i)!".EvalNumerical()` | `7.23303935E-191 + 2.200942072E-190i` | `0`: the value, `-8.33E-215 - 7.33E-216i`, is below the default precision's floor |
| `"1/(75 + 316.22776601683796i)"`, downcasting off | `0.00071005917159763313609… - 0.00299387234098781500591…i` | `0.00071005917159763302214990… - 0.00299387234098781452548…i` |
| `"(-38.0785)!"` at 30 digits, downcasting off | `-3.13914586443552289958547548164E-35` | `7.03406751543900910211608330734E-43` |
| `"(-60.5)!"` at 30 digits, downcasting off | `5.86118478907722232671451280188E-81` | `2.93059239453861116335725639905E-81` |
| `"abs(75 + 316.22776601683796i)"`, downcasting off | `325` | `325.000000000000026076735749…` |

### A rational function with symbols in it beside a root of a linear is split into partial fractions first

**Answers where there were none.** `1/(x (1 + x^2) sqrt(a + b x))` was declined, while
`1/(x sqrt(a + b x))` and `x/((1 + x^2) sqrt(a + b x))`, its two partial fractions beside the root,
were each answered: under `v = sqrt(a + b x)` it is a rational function over a sextic with symbols in
it, which nothing factors. A rational function with symbols in it and two or more factors below the bar,
beside a power that is not whole of a linear, is now split over its factors as written -- the whole part
of the power among them, so that each term stands over a root -- and integrated term by term. It is what
the tangent substitution makes of Rubi's 4.3.2.1, `cot(x)/sqrt(a + b tan(x))`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x*(1 + x^2)*sqrt(a + b*x))".ToEntity().Integrate("x")` | `integral(...)` | a piecewise of arctangents and a logarithm in `sqrt(a + b x)` |
| `"cot(x)/sqrt(a + b*tan(x))".ToEntity().Integrate("x")` | `integral(...)` | the same in `tan(x)` |
| `"cot(x)^2/(a + b*tan(x))^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | the same, beside a power of `a + b tan(x)` |

### A quotient of polynomials is split into coprime blocks before a root is peeled off

`SolveByPartialFractions` tried `TrySplitOffRationalRoot` before `TrySplitIntoCoprimeParts`. Both
succeed on a denominator with two repeated factors, so the first one decided, and it is the one that
hands on an **expanded** remainder — every level of the recursion then re-factorised what the level
above had just factored. Swapping them is
[#1235](https://github.com/asc-community/AngouriMath/issues/1235).

**The value of every antiderivative is unchanged**, and each was checked by differentiating it back
and comparing numerically at five points; the worst relative disagreement is 4e-16. What moves is
the printed form of the logarithmic terms, and it moves in one direction: a logarithm of a repeated
factor now keeps the power inside it.

| | Was | Is |
|---|---|---|
| `"1/((1+x)^3*(2+x)^3)".ToEntity().Integrate("x")`, the term over the first factor | `6 * ln(x + 1)` | `3 * ln(x ^ 2 + 2 * x + 1)` |
| `"1/((1+x)^2*(2+x)^2)".ToEntity().Integrate("x")` | `(-2) * ln(x + 1)` | `-ln(x ^ 2 + 2 * x + 1)` |
| `"1/(x*(-4+x^2)^4)".ToEntity().Integrate("x")`, the term over `x - 2` | `-1/512 * ln(x + -2)` | `-1/1024 * ln(x ^ 2 + (-4) * x + 4)` |

`3 * ln((x+1)^2)` and `6 * ln(x+1)` are the same number wherever the second is real, and the first
is **also real for `x < -1`**, where the logarithm of the bare factor is not. So a caller reading the
answer on the far side of a root gets a real expression where it used to get a complex one.

**It is a large speed-up rather than a small one**, which is why it is worth a changed form:

| | Was | Is |
|---|--:|--:|
| `1/((1+x)^3*(2+x)^4)` | 29,256 ms | 1,447 ms |
| `1/((2+x)^3*(3+x)^4)` | 29,179 ms | 551 ms |
| `1/(x*(-4+x^2)^4)` | 8,472 ms | 1,878 ms |
| `1/((1+x)^3*(2+x)^3)` | 1,343 ms | 492 ms |

### Two square roots of linears with one slope are rationalised together

`sqrt(L1)` and `sqrt(L2)` with `L1 - L2` a constant are rational in their sum
`v = sqrt(L1) + sqrt(L2)`: the difference is `(L1 - L2)/v`, and `x` is rational in `v`. An inverse
function of the difference was declined where it is elementary: Charlwood's
`arcsin(sqrt(1 + x) - sqrt(x))`, after five seconds, and Rubi's 5.3.7 has the arctangent's powers
of x beside it. Under `v` each is an inverse function of `1/v` beside a rational function of `v`,
which parts closes, and written back through `1/v = (sqrt(L1) - sqrt(L2))/(L1 - L2)` the answer
holds the difference as the integrand does. Only where the sum or the difference stands inside an
inverse trigonometric function or a logarithm: a rational function of the two roots is the radical
rules' already
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"asin(sqrt(1 + x) - sqrt(x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `arcsin(sqrt(1 + x) - sqrt(x))` |
| `"x^3*atan(-sqrt(x)+sqrt(1+x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in the arctangent |

### Rational functions of `x` and `x^n` with symbols in them are taken apart

**Answers where there were none.** The partial fractions took apart a denominator with symbols in it
only where its factors were linear or quadratic, so `1/(x (x^3 + c))` was declined where
`1/(x (x^3 + 2))` was answered. Now:
- a power of `x` beside a block `B` whose constant term is not zero is split off: the first `k`
  terms of the power series of `N/B` at 0 are the part over `x^k`, and the rest is a polynomial
  over `B` exactly;
- a power of another linear `g + h x` beside such a block is split the same way at the linear's
  root, where the coefficients of `N` and `B` are read by Taylor's formula;
- blocks that are each a polynomial in one power `x^n` are split in `u = x^n`, where they are linear
  or quadratic, for `n >= 3`, or `n = 2` with two blocks or more; one block quadratic in `u` is two
  at its roots, as `a + b x^2 + c x^4` already was;
- a quotient with `x` below a bar inside it, `1/(a + b/x)`, is written over one bar first, and the
  power of `x` that cleared the bars is taken out of both sides again;
- a product of sums in `x^n` with a symbol for `n` is written out, as a power of one already was.

Rubi's `x^m (a + b x^n)^p` and `(a + b x^n)^p (c + d x^n)^q`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x*(x^3 + c))".Integrate("x")` | left unevaluated | `ln(x)/c - ln(c x^3 + c^2)/(3 c)` |
| `"1/(x^4*(a + b*x^3))".Integrate("x")` | left unevaluated | `-1/(3 a x^3) - b ln(x)/a^2 + b ln(a^2 b x^3 + a^3)/(3 a^2)` |
| `"1/(x*(a + b*x^5))".Integrate("x")` | left unevaluated | `ln(x)/a - ln(a b x^5 + a^2)/(5 a)` |
| `"1/((x - c)*(a + b*x^3))".Integrate("x")` | left unevaluated | `ln(x - c)/(a + b c^3)`, and logarithms and an arctangent at the binomial's roots |
| `"1/((g + h*x)*(a + b*x^3))".Integrate("x")` | left unevaluated | `h^3 ln(g + h x)/(a h^3 - b g^3)`, and the same |
| `"1/(x^2*(x^3 + c))".Integrate("x")`, and `(c + d x + e x^2)/(x^2 (a + b x^3))` | left unevaluated | a logarithm and an arctangent in `c^(1/3)`, beside `-1/(c x)` |
| `"1/((a+b*x^3)*(c+d*x^3))".Integrate("x")`, and `x/((a + b x^3)(c + d x^3))` | left unevaluated | logarithms and arctangents in `(-a/b)^(1/3)` and `(-c/d)^(1/3)`, over `a d - b c` |
| `"1/((a+b*x^4)*(c+d*x^4))".Integrate("x")` | left unevaluated | arctangents and logarithms of each binomial's roots, over `a d - b c` |
| `"1/(a+b*x^3+c*x^6)".Integrate("x")`, `(1 + x^4)/(1 + b x^4 + x^8)` | left unevaluated | the same, at the two roots of the quadratic in `x^n`, over `sqrt(b^2 - 4 a c)` |
| `"1/(x^4*(d+e*x^2)*(a+c*x^4))".Integrate("x")` | left unevaluated | powers of `x`, an arctangent over `d + e x^2`, and the biquadratic's arctangents |
| `"1/(a+b/x)".Integrate("x")` | left unevaluated | `x/a - b ln(a x + b)/a^2` |
| `"1/(2+3/x^2)^3".Integrate("x")`, `1/(a + b/x^3)` | left unevaluated | an antiderivative |
| `"(a+b*x^n)*(c+d*x^n)^3".Integrate("x")` | left unevaluated | written out, eight powers of `x` integrated: `a c^3 x + ... + b d^3 x^(4 n + 1)/(4 n + 1)` |

### A whole power of a quotient with a symbol in it is integrated as the quotient of the powers

**Answers where there were none, and a slowdown since 2.5.0 undone.** `(c/(a + c x^2))^2` was
declined, after twenty-six seconds, while `c^2/(a + c x^2)^2`, the same function, was answered at
once: the table reads the quotient of the powers and nothing read the power of the quotient. It is
written so now wherever the quotient is of two polynomials in `x` with a symbol in them and what
it leaves above the bar is a monomial, the table's `x^m/(a + b x^n)^p`; that is exact for a whole
power. That is
also how the substitution `u = x^2` writes `x/(a + c x^4)^2` since 2.5.0, having simplified
`1/(a/c + u^2)^2`, and that integrand took twenty seconds where 2.5.0 took a tenth of one
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(c/(a + c*x^2))^2".ToEntity().Integrate("x")` | `integral(...)`, after a minute | a quotient and an arctangent or a logarithm, by the sign of `a c` |
| `"(1/(a + c*x^2))^2".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"(x/(a + c*x^2))^2".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"x/(a + c*x^4)^2".ToEntity().Integrate("x")` | the answer, in 0.1 s; 22 s on the unreleased master | the answer, in 0.06 s |

### A polynomial with symbols in it that is a binomial or an even quartic in a shifted variable is integrated in it

**Answers where there were none.** `1/(c^2 x^3 + 3 b c x^2 + 3 b^2 x + 3 a b)` was left unevaluated,
although its denominator is `((c x + b)^3 + 3 a b c - b^3)/c`, which the rule for a binomial reads at
once: nothing factors a polynomial with a symbol among its coefficients. A polynomial below the bar
of the third to the sixth degree, with a symbol in it, that written in `y = x + s` with
`s = a_(n-1)/(n a_n)` is a binomial, or a quartic even in `y`, is integrated in `y`. Rubi's 1.3.1
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(3*a*b + 3*b^2*x + 3*b*c*x^2 + c^2*x^3)".ToEntity().Integrate("x")`, and its square | `integral(...)` | two logarithms and an arctangent in `x + b/c` |
| `"x/(a + 8*x - 8*x^2 + 4*x^3 - x^4)".ToEntity().Integrate("x")`, and `1` over it | `integral(...)` | the antiderivative in `x - 1` |

### The hyperbolic functions have antiderivatives, and so does anything rational in `e^(k x)`

An integrand rational in `e^(k x)` becomes a rational function of one variable under
`u = e^(k x)`, and nothing was doing that. `e^x/(1 + e^x)` was answered — its numerator happens to
be the derivative of its denominator, which is what the general substitution looks for — and
`1/(1 + e^x)` was not, which is the shape of the gap.

**The hyperbolic functions are the visible half of it.** They are not nodes in this library:
`tanh(x)` is built as `(e^(2x) - 1)/(e^(2x) + 1)` and `sech(x)` as `2/(e^x + e^(-x))`. So they were
rational in an exponential all along, and none of the four had an antiderivative.

| | Was | Is |
|---|---|---|
| `"tanh(x)".Integrate("x")`, and `coth`, `sech`, `csch` | `integral(tanh(x), x)` — left unevaluated | the antiderivative |
| `"1/(1 + e^x)".Integrate("x")` | `integral(1 / (1 + e ^ x), x)` — left unevaluated | the antiderivative |
| `"1/(e^x + e^(-x))".Integrate("x")` | left unevaluated | the antiderivative, `arctan(e^x)` up to the form below |
| `"e^(3*x)/(e^x + 1)".Integrate("x")`, and every quotient whose exponentials have a common slope | left unevaluated | the antiderivative |

**No previously-answered integral changes**, `e^x`, `x*e^x` and `e^x/(1 + e^x)` included: the rule
runs after everything that answers a problem in its own terms. Every antiderivative above was
checked by differentiating it back and comparing numerically at five points.

**The answers come out in the exponential, and unfolded.** `∫sech(x) dx` is `2 arctan(e^x)` and
prints as `4 * arctan(2 * e ^ x / 2) / 2`; `∫tanh(x) dx` is `ln(cosh(x))` and prints in `e^(2x)`.
That is the rational integrator's own shape — it writes a logarithm as `ln((2ax + b - D)/(2ax + b + D))`
and leaves the coefficient arithmetic standing — and it is what `1/(x*(x+1))` has always printed as
too. A correct antiderivative in an unhelpful form, where there was none at all.

`e^(x^2)` is still declined, and correctly: its exponent is not linear and it has no elementary
antiderivative.

### A polynomial over a power of a binomial past the cube is integrated

**Answers where there were none.** `P(x)/(a + b x^n)^k` with symbols in the binomial, `n >= 3`, was
answered by the Hermite reduction for `k = 2` and `k = 3` only: from the fourth power on, its
linear system grew past what it takes. Each monomial of `P` is now taken down a power at a time by
the classical recurrence, `int x^m/B^j = x^(m+1)/(a n (j - 1) B^(j-1)) - (m + 1 - n (j - 1))/(a n (j - 1)) int x^m/B^(j-1)`,
and what reaches the first power is integrated once over the binomial, at its roots of unity. The
square and the cube keep the Hermite reduction's answers. Rubi's 1.1.3.8 `P(x) (c x)^m (a + b x^n)^p`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(a + b*x^4)^4".ToEntity().Integrate("x")` | `integral(...)` | three algebraic terms over powers of the binomial, and logarithms and arctangents at its roots |
| `"(c + d*x)/(a + b*x^4)^4".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"(c + d*x)/(a + b*x^3)^4".ToEntity().Integrate("x")` | `integral(...)` | the same at the roots of `a + b x^3` |
| `"x^2/(a + b*x^5)^5".ToEntity().Integrate("x")` | `integral(...)` | four algebraic terms, and the integral of `x^2/(a + b x^5)` |

### A polynomial over a power of a trinomial past the square is integrated

`P(x)/(a + b x^n + c x^(2n))^k`, a polynomial over a power of a trinomial in `x^n` with symbols in
it, was left unevaluated past the square, which the Hermite reduction answers in one linear solve.
Now each monomial is taken down a power at a time by the trinomial's recurrence, two monomials a
power up each step, and what reaches the first power is integrated once over the trinomial. Rubi's
1.2.2.2 and 1.2.2.4 have these by the dozen
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^4/(a + b*x^2 + c*x^4)^3".ToEntity().Integrate("x")` | `integral(...)` | odd polynomials over `(a + b x^2 + c x^4)^2` and over it, and arctangents at its roots |
| `"1/(a + b*x^2 + c*x^4)^3".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"x/(a + b*x^3 + c*x^6)^2".ToEntity().Integrate("x")` | `integral(...)` | a polynomial over `a + b x^3 + c x^6`, and the integral of a polynomial over it |

### A polynomial over a power of one linear with a symbol in it is written in powers of the linear

**Answers where there were none.** `t^9/(a + b t)^8` is a polynomial and eight powers of `1/(a + b t)`,
and was declined by 2.5.0. On the unreleased master it was answered after forty-six seconds: divided
out and decomposed with the symbols in it, it went round the substitution search a dozen levels deep.
With nothing else below the bar and a symbol in the linear, the polynomial is written in powers of
the linear at its root now, and each piece is the table's
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"t^9/(a + b*t)^8".ToEntity().Integrate("t")` | `integral(...)` | powers of `a/b + t` and a logarithm, in 0.03 s |
| `"x^4/(a + b*sqrt(x))^8".ToEntity().Integrate("x")` | `integral(...)` | the same in `sqrt(x)`, in 0.5 s; a minute on the unreleased master |

### A polynomial over a power of a quadratic with a sum of symbols in it is reduced

**Answers where there were none.** The reduction of `N(x)/Q(x)^n` divides `N` by the quadratic a
power at a time. With a sum of symbols among the quadratic's coefficients, the remainder it computed
held the terms the division had taken away, written two ways, and read as of the second degree it
stopped the reduction: `x^4/(d x^2 + b c - a d)^5` was declined where `x^4/(d x^2 + k)^5` was
answered. The remainder the division reports is taken where the computed one is not linear, and an
improper fraction is declined before the first division rather than after the last. This is what
`t = sqrt(a + b x)` makes of Rubi's 1.1.1.2 `(a + b x)^(m/2)/(c + d x)^n`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^4/(d*x^2 + b*c - a*d)^5".ToEntity().Integrate("x")`, and `x^6` over the fourth power | `integral(...)` | the antiderivative |
| `"(1 - b*x^2)^3/(c*(1 - b*x^2) + a*d*x^2)^3".ToEntity().Integrate("x")` | no answer within a minute | the antiderivative |
| `"(a + b*x)^(5/2)/(c + d*x)^4".ToEntity().Integrate("x")` | `integral(...)` | the antiderivative |

### A decline is held at the depth it was made at

**Answers where there were none.** The integrator remembers what it has worked out, declines
included, and a rule may answer the question asked and the level or two below it and decline the
same integrand deeper. A decline made at depth three was held under a key that told the question
asked from the rest and nothing more, and was served at depth two: the search for
`1/(x^(3/2) (a + c x^4))` declines the root's substitution `2/(w^2 (a + c w^8))` at depth three and
asks it again at depth two, and the integral was declined. The memo is kept per thread, so a decline
made deeper in one integral was served the same way to the next. A decline is held at the depth it
was made at now, and an answer at every depth
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x^(3/2)*(a + c*x^4))".ToEntity().Integrate("x")` | `integral(...)` | the antiderivative |
| `"1/((a + b/x^3)*x^5)".ToEntity().Integrate("x")` | `integral(...)` | the antiderivative |
| `"1/(x^(7/2)*(a + b*x^2 + c*x^4))".ToEntity().Integrate("x")` | `integral(...)` | the antiderivative |

### The Hermite reduction's system is solved in one order whatever the spelling

**Faster, and not a different value.** The reduction of a rational integrand with a repeated factor
below the bar solves one linear system for the rational part, and took its rows in the order the
powers of `x` were met, which follows how the factors are written. With symbols in the coefficients
the elimination's pivots followed that order, and in one order the solution came out as quotients of
polynomials of the thirty-sixth degree in the symbols that nothing reduced:
`x/((1 + x^2)^3 (2 a x + b (x^2 + 1)))` took 42 s, and with `(x^2 + 1)^3` below the bar 1 s. The rows
are ordered by their power of `x` now. Where the order mattered, the antiderivative's coefficients can
come out reduced where they were not.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x/((1 + x^2)^3*(2*a*x + b*(x^2 + 1)))".ToEntity().Integrate("x")` | `integral(...)` | the antiderivative, in about half a second, as with `(x^2 + 1)^3` |

### A symbolic parameter no longer stops a rational integrand being integrated

`1/(8 + x^3)` and `1/(16 - x^4)` are answered at once. `1/(a^3 + x^3)` and `1/(a^4 - x^4)` were not,
and the parameter is the whole difference: the rational rules read a denominator as a polynomial
**over the rationals**, so `a^3` is not a coefficient they can work with.

Two things were in the way, and both are fixed here.

**A constant factor inside the denominator stayed there.** `a * (1/(1 + x^3))` was taken apart and
`1/(a*(1 + x^3))` — the same number — was not, because the branches that take a factor out of a
quotient each want the whole of one side free of the variable.

**And the integrand is scaled by its parameter**, `x = c t`, which puts integer coefficients back;
the parameter comes out as a constant factor and the answer is read at `t = x/c`. Homogeneity is
checked rather than assumed, so an integrand the scale does not separate is declined.

| | Was | Is |
|---|---|---|
| `"1/(a^3 + x^3)".Integrate("x")`, and `1/(a^4 - x^4)`, `x^2/(a^4 + x^4)` | left unevaluated | the antiderivative |
| `"1/(x^3*(a^3 + x^3))".Integrate("x")`, and the family `1/(x^k (a^n ± x^n))` | left unevaluated | the antiderivative |
| `"1/(a*(1 + x^3))".Integrate("x")`, and every constant factor inside a denominator | left unevaluated | the antiderivative |

Every one of them was checked by differentiating it back with the parameter pinned and comparing at
five points. **No previously-answered integral changes**: the scaling runs last of the rewrites,
because unlike the others it fires on an integrand nothing is wrong with.

`x/((a^2 + x^2)*(b^2 + x^2))` is still declined, and deliberately — two parameters means scaling by
one leaves the other, and the sub-problem could then be scaled again without end.

**Where the answer is not defined.** `c = 0` is not a scaling, and what this produces —
`a^(-2) G(x/a)` for `1/(a^3 + x^3)` — has no value there. That is the honest report for a
substitution that does not exist rather than a wrong answer, and the integrand at `a = 0` is a
different function (`1/x^3`), answered on its own if asked that way.

### A power of `1 - t^2` or `1 + t^2` beside a power of a symbolic quadratic is split, the one over its linear factors

The half-angle tangent writes `sec(x)^4/(a + b sin(x))^2` as
`2 (1 + t^2)^5/((1 - t^2)^4 (a t^2 + 2 b t + a)^2)`. The split over symbolic linear factors reads
the linears it finds written, and `1 - t^2` is written as a quadratic, so these went to the Hermite
reduction, whose one linear solve swelled past anything it could use. A rational factor that splits
is now written in its linear factors where a symbolic one stands beside it. The constant taken out of
`(a t^2 + 2 b t + a)^2` also left the monic quadratic as `a/a + 2 b/a t + t^2`, which the split
could not read; it is `1 + 2 b/a t + t^2` now. And the split's numerator over a quadratic block,
computed modulo that quadratic, came from the other factors written out as a polynomial and read
back, where `(4 b^2 - 2 a^2)/a^2` expanded to an `a^0` the reader declined: it is reduced from their
coefficients now, and quadratics alone -- `(1 + t^2)^5` beside a symbolic square, which
`cos(x)^6/(a + b sin(x))^2` becomes -- are split the same way where a symbolic one is repeated.
And the table's division of a numerator by a symbolic quadratic, a power at a time, read its
remainder as the unexpanded difference `N - q Q`, which looks cubic, and under a condition on the
leading coefficient once expanded, and declined: it is read expanded and bare now.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sec(x)^4/(a+b*sin(x))^2".Integrate("x")`, Rubi's 4.1.1.2 row 493 | `integral(...)` | the antiderivative |
| `"1/((1-x^2)^3*(a*x^2+2*b*x+a)^2)".Integrate("x")` | `integral(...)` | the antiderivative |
| `"(1+x^2)^2/((1-x^2)^2*(a*x^2+2*b*x+a)^2)".Integrate("x")` | `integral(...)`, after 5 s | the antiderivative |
| `"2*(1+x^2)^5/((1-x^2)^4*(a*x^2+2*b*x+a)^2)".Integrate("x")` | past a 20 s budget | the antiderivative |
| `"cos(x)^6/(a+b*sin(x))^2".Integrate("x")`, Rubi's 4.1.1.2 row 489 | `integral(...)` | the antiderivative |
| `"cos(x)^4*csc(x)/(a+b*sin(x))^2".Integrate("x")`, Rubi's 4.1.2.2 row 1447 | `integral(...)` | the antiderivative |
| `"2*(1-x^2)^5/((1+x^2)^4*(a-a*x^2+2*b*x)^2)".Integrate("x")` | past a 20 s budget | the antiderivative |
| `"tan(x)^4/(a+b*csc(x))".Integrate("x")` | `integral(...)` | the antiderivative, 2,719 characters, where the unreleased master answered in 520,864 |
| `"sin(x)/(a+b*sin(x))^3".Integrate("x")`, Rubi's 4.1.2.1 row 242 | `integral(...)` | the antiderivative |
| `"4*x*(1+x^2)/(a*x^2+2*b*x+a)^3".Integrate("x")` | `integral(...)` | the antiderivative |

### `x^2` over a three-quarter power of a quadratic binomial beside another is integrated where that is elementary

**Answers where there were none.** `x^2/((A + B x^2)^(3/4) (C + D x^2))` at `B C - 2 A D = 0`, Rubi's
1.1.2.4, is the difference of the two functions whose sum answers `1/((A + B x^2)^(1/4) (C + D x^2))`
at that ratio (the entry below), with another constant, and was declined. It is answered by the
signs of `A` and `B` now, each form checked at points in its sign case
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2/((a - b*x^2)^(3/4)*(2*a - b*x^2))".ToEntity().Integrate("x")` | `integral(...)` | an arctangent and an inverse hyperbolic tangent, by the signs of `a` and `b` |
| `"x^2/((-2 + 3*x^2)*(-1 + 3*x^2)^(3/4))".ToEntity().Integrate("x")` | `integral(...)` | `(arctan(u) - artanh(u))/(3 sqrt(6))`, `u = sqrt(3) x/(sqrt(2) (-1 + 3 x^2)^(1/4))` |
| `"x^2/((2 - 3*x^2)^(3/4)*(4 - 3*x^2))".ToEntity().Integrate("x")` | `integral(...)` | an arctangent and an inverse hyperbolic tangent |

### A cube or fourth root of a quadratic binomial beside another is integrated where that is elementary

**Answers where there were none.** `1/((A + B x^2)^(1/3) (C + D x^2))` is an elliptic integral
except where `B C + 3 A D = 0` or `B C - 9 A D = 0`, and `1/((A + B x^2)^(1/4) (C + D x^2))` except
where `B C - 2 A D = 0`. At those ratios it is arctangents and logarithms of quotients in the root,
as Rubi's 1.1.2.3 gives them, and it was left unevaluated. It is answered there now, with symbols
in the constants too: by the sign of `B/A` for the cube root, and by the signs of `A` and `B` for
the fourth. With `A` and `B` both positive, the fourth root's answer jumps at `x = 0`, and is an
antiderivative on each side of it, as Rubi's is
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/((1 - x^2)^(1/3)*(3 + x^2))".ToEntity().Integrate("x")` | `integral(...)` | arctangents and logarithms in `x` and `(1 - x^2)^(1/3)` |
| `"1/((1 + x^2)^(1/3)*(9 + x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same in `(1 + x^2)^(1/3)` |
| `"1/((a + b*x^2)^(1/3)*(3*a - b*x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same in `(a + b x^2)^(1/3)`, by the sign of `b/a` |
| `"1/((a + b*x^2)^(1/3)*(9*a + b*x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same, by the sign of `b/a` |
| `"1/((a + b*x^2)^(1/4)*(2*a + b*x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same in `(a + b x^2)^(1/4)`, by the signs of `a` and `b` |
| `"1/((2 + 3*x^2)^(1/4)*(4 + 3*x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same in `(2 + 3 x^2)^(1/4)` |
| `"1/((-2 + 3*x^2)*(-1 + 3*x^2)^(1/4))".ToEntity().Integrate("x")` | `integral(...)` | `-(arctan(u) + artanh(u))/(2 sqrt(6))`, `u = sqrt(3) x/(sqrt(2) (-1 + 3 x^2)^(1/4))` |

### A product of powers of the trigonometric functions, some not whole, is integrated as sine and cosine powers

**Answers where there were none.** `(a sin(x))^(5/2) sqrt(b sec(x))` was declined, and so were
`(d csc(x))^(3/2) sqrt(c sec(x))`, `cos(x)^(7/2)/sin(x)^(7/2)` and the rest of the products of
powers of the trigonometric functions of one argument, each of which is `sin^M cos^N` times a constant
on every interval where both are continuous. With the exponents adding up to an even number that is
`tan^M (1 + tan^2)^(-(M + N)/2)`, a function of the tangent, and with `M` or `N` an odd whole number
the plain product, which the substitution by the other function answers: Chebyshev's three cases.
It is integrated so now, and the answer is the integrand times the antiderivative of that form over
the form, which holds on every such interval, the constants inside the roots whatever their signs.
Rubi's 4.1.0 to 4.6.0 ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(a*sin(x))^(5/2)*sqrt(b*sec(x))".ToEntity().Integrate("x")` | `integral(...)` | the integrand times an antiderivative in `sqrt(tan(x))` over `tan(x)^(5/2) (1 + tan(x)^2)^(-1)` |
| `"cos(x)^(7/2)/sin(x)^(7/2)".ToEntity().Integrate("x")` | `integral(...)` | the same over `tan(x)^(-7/2)` |
| `"(b*tan(x)^3)^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | the same over `tan(x)^(9/2)` |
| `"(d*sec(x))^(5/2)*sqrt(b*tan(x))".ToEntity().Integrate("x")` | `integral(...)` | the integrand times an antiderivative in `sqrt(sin(x))` over `sin(x)^(1/2) cos(x)^(-3)` |

### The third case of a binomial differential is right for a negative `x` too

**Answers where there were none.** Chebyshev's third case, `x^m (a + b x^n)^(p/q)` with
`(m + 1)/n + p/q` whole, is integrated under `x = 1/y`, and came in after 2.5.0, which declined these.
On the unreleased master it wrote its root back as `(b + a/x^n)^(1/q)`, which is that root for a
positive `x`, and for a negative one only when `q` is odd: `1/(1 + x^4)^(5/4)` came out as
`1/(1 + 1/x^4)^(1/4)`, an even function, whose derivative is the integrand's negative for every
negative `x`. The root is written
back as `(a + b x^n)^(1/q)/x^(n/q)` now, which has the same `q`-th power everywhere, and the answers
are right on both sides of zero ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(1 + x^4)^(5/4)".ToEntity().Integrate("x")` | `integral(...)` | `1 / ((1 + x ^ 4) ^ (1/4) / x)`, which is `x/(1 + x^4)^(1/4)` |
| `"x^2/(1 + x^4)^(3/4)".ToEntity().Integrate("x")` | `integral(...)` | logarithms and an arctangent of `(1 + x^4)^(1/4)/x` |
| `"x^6*(3 + 4*x^4)^(1/4)".ToEntity().Integrate("x")` | `integral(...)` | the same in `(3 + 4 x^4)^(1/4)/x` |

### A rational function of the sine and cosine with symbols in it is integrated by the half angle

`sin(x)^2/(a + b cos(x))` was left unevaluated while `sin(x)^2/(2 + 3 cos(x))` was answered. Under
`t = tan(x/2)` every sine and cosine brings a `1 + t^2` below the bar, and the one the numerator
shares with the denominator was divided out of the denominator as a whole, which multiplied it out:
a sextic with a symbol in every coefficient, which nothing splits. It is taken off the factor
where it stands now, and the rest of the denominator stays factored. And the secant, tangent,
cosecant and cotangent are written in the sine and cosine wherever a sum holds one of the two, so
that `sec(x)^2/(a + b sin(x))` is rational in them rather than declined at once
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sin(x)^2/(a + b*cos(x))".ToEntity().Integrate("x")` | `integral(...)` | a piecewise antiderivative in `tan(x/2)` |
| `"sec(x)^2/(a + b*sin(x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(x/2)` |
| `"tan(x)^4/(a + a*cos(x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(x/2)` |
| `"csc(x)^2/(a + a*cos(x))".ToEntity().Integrate("x")` | `integral(...)` | `(tan(x/2)^3/12 + tan(x/2)/2 - 1/(4 tan(x/2)))/a` |

### A rational function with complex coefficients is integrated through its real and imaginary parts

**Answers where there were none.** `1/((1 + i x)^2 (1 + x^2))` was declined: the rational
integrator reads coefficients that are rational numbers, and `i` is not one. Times the conjugate
of its denominator over itself, `N/D = N D'/(D D')` with `D'` the polynomial whose coefficients are
those of `D` conjugated, the denominator has rational coefficients and the numerator is `P + i S`
with `P` and `S` rational: the integral is that of `P/(D D')` and `i` times that of `S/(D D')`. A
product of symbols common to a polynomial's coefficients, `a + i a x`, comes out with the
constants. The tangent substitution makes such a function of Rubi's `(a + i a tan(c + d x))^n`, by
the hundred in its 4.3.2.1, 4.3.3.1 and 4.3.1.2; their answers are in `tan(c + d x)`, on each
interval between the poles of the tangent, as the substitution's are
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/((1 + i*x)^2*(1 + x^2))".ToEntity().Integrate("x")` | `integral(...)` | a rational function of `x` and an arctangent |
| `"1/(a + i*a*tan(x))^2".ToEntity().Integrate("x")` | `integral(...)` | the same in `tan(x)` |
| `"tan(x)^3/(a + i*a*tan(x))^4".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"(A + B*tan(x))/(a + i*a*tan(x))^2".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"1/(a + i*a*tan(c + d*x))^3".ToEntity().Integrate("x")` | `integral(...)` | the same in `tan(c + d x)` |

### An integrand with the imaginary unit in it is not simplified by the rule that scales the variable

**Answers where there were none.** The rule that scales the variable by the integrand's one symbol
simplified the scaled integrand three times, to separate the whole power of the scale it is
homogeneous of. With the imaginary unit beside the symbol that search ran past any budget: on the
unreleased master `((1 + i a x)/(1 - i a x))^(-5/4)/x^2` took more than two minutes to be declined,
and the rules after this one never had their turn on integrands that it held up. Such an integrand is
inner-simplified now, and declined in seconds where the parts do not separate
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sec(c + d*x)^3/(a + i*a*tan(c + d*x))".ToEntity().Integrate("x")` | `integral(...)` | logarithms of `tan((c + d x)/2) ± 1` and a quotient |
| `"tan(c + d*x)^(8/3)/(a + i*a*tan(c + d*x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(c + d x)^(1/3)` |
| `"1/(a + b*csc(c + d*x)^2)^4".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative, by the signs of `a` and `b` |
| `"x^4*e^(2*i*atan(a + b*x))".ToEntity().Integrate("x")` | `integral(...)` | a polynomial, logarithms and arctangents of `a + b x` |
| `"sec(c + d*x)^2/(a + i*a*tan(c + d*x))".ToEntity().Integrate("x")` | `ln(i a d tan(c + d x) + a d)/(i a d)` | `ln(i tan(c + d x) + 1)/(i a d)`, the same up to a constant |

### `NaN` was returned as the antiderivative of something that has one

**A wrong answer, not a missing one.** `1/(a*x^2)` came back as `NaN + C`, and `NaN` is this
library's way of saying the thing does not exist. It does exist: it is `-1/(a x)`.

| | Was | Is |
|---|---|---|
| `"1/(a*x^2)".Integrate("x")` | `NaN + C` | the antiderivative, `-1/(a x)` up to form |
| `"1/((a + b)*x^2)".Integrate("x")`, and every symbolic constant times `x^2` | `NaN + C` | the antiderivative |
| `"1/(a*x^2 + b*x^2)".Integrate("x")`, and the same collected differently | `NaN + C` | the antiderivative |

The rule for a constant over a quadratic answers with a piecewise over the discriminant, and one of
its branches is for `a = 0`, where the denominator is linear. With `b` and `c` both zero that branch
computes `k/0`. A **numeric** `a` makes the branch decidably false and it is dropped before the
`NaN` can spread, which is why `1/(2*x^2)` was always right; a symbolic `a` leaves it standing, and
one `NaN` case takes the whole piecewise with it.

The branch is now left out entirely when the denominator has neither a linear nor a constant term,
because there it asks about an expression that does not exist — with `a`, `b` and `c` all zero the
denominator is identically zero and there is no integrand to integrate. What is left is the
zero-discriminant arm, which gives the right answer.

Each of these was checked by differentiating it back with the parameters pinned and comparing at
four points. Nothing that already had an antiderivative changes.

### A radical function of whole powers of `x` written through its logarithm says where its answer holds

**Answers where there were none, each with the condition it holds under.** `1/csch(2 ln(x))^(1/2)`
is `sqrt(sinh(2 ln(x)))`, which is `sqrt((x^2 - x^(-2))/2)` and real on both sides of zero; 2.5.0
declined it. On the unreleased master it was integrated under `t = ln(x)`, where the rules take
`e^t` to be positive, and the answer was right for a positive `x` and wrong for every negative one.
Such an answer is given `provided x > 0` now, and `provided c x^n > 0` for a logarithm of `c x^n`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/csch(2*ln(x))^(1/2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative, `provided x > 0` |
| `"csch(2*ln(x))^(-3/2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative, `provided x > 0` |
| `"1/csch(2*ln(c*x))^(1/2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative, `provided c x > 0` |
| `"1/sech(2*ln(x))^(1/2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative, `provided x > 0` |

### `e^(k acoth(a x))` beside a power of `c - a c x` is integrated

**Improvement, not silent.** `e^(3 acoth(a x))/(c - a c x)^3` and `e^(2 acoth(a x)) sqrt(c - a c x)/x`
were left unevaluated. The radical substitution leaves coefficients that are zero as values and not
as written, `-6 a^3 + 6 a^3` and `1/c c - 1`, and the long division and the rule for a biquadratic
read them as nonzero. They are now decided over one bar, expanded, and at pinned symbols. Rubi's
7.4.2 ([#1665](https://github.com/asc-community/AngouriMath/issues/1665)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(3*acoth(a*x))/(c-a*c*x)^3".Integrate("x")`, and over `(c - a c x)^4` | left unevaluated | an antiderivative in `sqrt((a x + 1)/(a x - 1))` |
| `"e^(2*acoth(a*x))*sqrt(c-a*c*x)/x".Integrate("x")`, and over `x^2` | left unevaluated | an antiderivative in `sqrt(c - a c x)`, by cases on the sign of `c` |

### A trigonometric function of an imaginary multiple of a logarithm is integrated in exponentials

**Answers where there were none.** `tan(a + i ln(x))` and `sin(a + ln(c x^2) sqrt(-1/4))` were
declined: the closed form for a power of the variable times a sine or cosine of a logarithm divides
by `(m + 1)^2 + B^2`, which an imaginary `B` can make zero, and Rubi's 4.7.5 is built on exactly
that. `e^(i (a + b ln(u)))` is `e^(i a)` times a real power of `u` when `b` is imaginary, so each
such function is a sum or quotient of powers of `u`, and is written so now when `b` is an imaginary
number; the zero is a power `1/x`, whose integral is a logarithm. A symbolic `b`,
`sqrt(-1/n^2)`, is left alone, since whether the zero is there turns on the sign of `n`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"tan(a + i*ln(x))".ToEntity().Integrate("x")` | `integral(...)` | a rational function of `x` and `e^(i a)`, with a logarithm |
| `"sin(a + ln(c*x^2)*sqrt(-1/4))".ToEntity().Integrate("x")` | `integral(...)` | powers of `x` and `c x^2` and a logarithm, through `e^(i a)` |
| `"1/cos(a - 2*i*ln(c*x))^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | an arctangent and a logarithm of a root, through `e^(i a)` |

### `e^(n i arctan(a x))` to a power that is not whole is integrated

**Answers where there were none.** `e^(n i arctan(L))` is written algebraically, as
`(1 + i L)^n (1 + L^2)^(-n/2)`, and for an `n` that is not whole those are two radicals of different
orders that nothing reads: `x^2 e^(3/2 i arctan(a x))` was left unevaluated. For such an `n` it is
written `((1 + i L)/(1 - i L))^(n/2)`, the same on the principal branch for a real `L`, one power of a
quotient of linears, which the substitution for that reads. Rubi's 5.3.6
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(3/2*i*arctan(a*x))*x^2".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `((1 + i a x)/(1 - i a x))^(1/4)` |
| `"x^3/e^(3/2*i*arctan(a*x))".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"e^(1/3*i*arctan(x))*x".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `((1 + i x)/(1 - i x))^(1/6)` |

### `NaN` again, from an exponent that was read as written rather than as a number

**A wrong answer, and a second one of the same kind.** `(a^2 + 2abx^2 + b^2x^4)^3/x^7` came back as
`NaN + C` while `(a + bx^2)^6/x^7` — the same integrand with its base not written out — was
answered. Unrelated to the entry above: that one was the quadratic rule's `a = 0` branch, and this
reproduces with it fixed.

| | Was | Is |
|---|---|---|
| `"(a^2 + 2*a*b*x^2 + b^2*x^4)^3/x^7".Integrate("x")` | `NaN + C` | the antiderivative |
| `"(a^2 + 2*a*b*x^2 + b^2*x^4)^3/x^9".Integrate("x")` | `NaN + C` | the antiderivative |
| `"(a^2 + 2*a*b*x + b^2*x^2)^3/x^7".Integrate("x")`, the same shape in one variable | `NaN + C` | the antiderivative |

The power rule recognised the exponent `-1` — the one case where the antiderivative is a logarithm
rather than a power — only when it was spelled as the integer. It also returned `x^(p + 1)/(p + 1)`
with that sum left standing, and repeated integration by parts hands an antiderivative back in as
an integrand, so a later round arrived carrying `x^(-3 + 1 + 1)`. That is `x^(-1)` and wants the
logarithm; read as written it is not `-1`, so the power rule applied and produced
`x^(-3 + 1 + 1 + 1)/(-3 + 1 + 1 + 1)`, which is `x^0/0`. The rule was feeding itself the one input
it could not read.

The exponent is now normalised both when it is read and when it is written back out. A **symbolic**
exponent is unaffected: `int x^n dx` is still `x^(n+1)/(n+1)`, since whether an undecidable `n` is
`-1` is not something this decides — what changed is an exponent that *is* decidable and was being
read as though it were not.

Each of these was checked by differentiating it back with the parameters pinned and comparing at
four points. The Rubi sample is unchanged at 231 of 463 with no wrong answers, so nothing that
already had an antiderivative moves.

### `NaN` a fourth time, from an exponent that is `-1` only as a value

**A wrong answer.** `x^(-1 - 3n) (a + b x^n)^3` came back as a sum whose last term was `x^0/0`, so
that it had no value anywhere, and `x^(-n/n)` as `NaN + C`. Expanded, the integrand's last term is
`x^(-1 - 3n + 3n)`, which is `x^(-1)` for every `n`; `InnerSimplified` keeps `-3n + 3n` as two
terms, and the power rule, which reads `-1` once the exponent is a number (the entry above), read
this one as written and divided by its zero. It asks whether the exponent plus one vanishes as a
value now, and gives the logarithm where it does. A symbolic exponent that is `-1` for one value
only is the power rule's as before. Rubi's 1.1.3.2 has four of these
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^(-1 - 3*n)*(a + b*x^n)^3".ToEntity().Integrate("x")` | three powers and `x^0/0`, no value anywhere | the three powers and `b^3 ln(x)` |
| `"x^(-1 - 3*n + 3*n)".ToEntity().Integrate("x")` | `x^(-1 - 3n + 3n + 1)/(-1 - 3n + 3n + 1)`, no value anywhere | `ln(x)` |
| `"x^(-n/n)".ToEntity().Integrate("x")` | `NaN` | `ln(x)` |
| `"1/(a*(b*x^m)^n)^(1/(m*n))".ToEntity().Integrate("x")` | `integral(...)` | `(a b^n)^(-1/(m n)) ln(x)`, provided `b > 0` and `a b^n > 0` |

### `NaN` a third time, from a quadratic with a coefficient off the real line

The integrals of `1/sqrt(Q)`, `1/Q` and `1/Q^n` over a quadratic `Q` choose their form by the sign
of the leading coefficient or of the discriminant, and had no arm for one that is not real: `i < 0`
and `i > 0` are each `NaN`, the complex numbers not being ordered, and a piecewise with no arm that
holds is `NaN` with them. With a symbol beside the imaginary unit the arms stayed standing, and none
of them holds for a real value of it but zero. Where the sign was real and the rest of the quadratic
was not, the arcsine was taken, and it differentiates back to minus the integrand on part of the
line. Such a quadratic now takes the arctangent or the logarithm, which use nothing about their root
but `sqrt(q)^2 = q` ([#1598](https://github.com/asc-community/AngouriMath/issues/1598)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x^2 + i)".Integrate("x")`, and `1/(x^2 + i)^2`, `1/sqrt(i*x^2 + 1)`, `sqrt(i*x^2 + 1)` | `NaN + C` | `2 arctan(2x/sqrt(4i))/sqrt(4i) + C`, and the antiderivatives |
| `"1/sqrt(i*a*x^2 + 1)".Integrate("x")` | a piecewise over `i a = 0`, `i a < 0` and `i a > 0`, `NaN` for every real `a` but zero | the same with an arm `provided not i a in RR`, the logarithm |
| `"1/sqrt(-x^2 + (1 + i)*x + 1)".Integrate("x")` | `-arcsin((-2x + 1 + i)/sqrt(4 + 2i)) + C`, whose derivative is minus the integrand below `x = -2` | `ln(-2x + 1 + i + 2i sqrt(-x^2 + (1 + i) x + 1))/i + C` |
| `"1/sqrt(-x^2 + i)".Integrate("x")` | `-arcsin(-2x/sqrt(4i)) + C` | `ln(-2x + 2i sqrt(-x^2 + i))/i + C`, an antiderivative as before, in the logarithm |
| `"1/(sqrt(x)*sqrt(1 + i*x))".Integrate("x")`, and Rubi 4.3.2.1's `sqrt(tan(c + d x))/(a + i a tan(c + d x))^(5/2)` | `integral(...)` | the antiderivative |

### Trigonometric functions of multiples of one linear argument are written in it

`csc(a + b x) csc(2a + 2b x)^2` was left unevaluated, and `csc(1 + x) csc(2 + 2x)^2` ran twenty
seconds, where `csc(x) csc(2x)^2` was answered at once. The rule that brings several arguments to
one reads a numeric slope, and writes an offset out by the addition formula, which put `sin(2)` and
`cos(2)` into every coefficient. Where every argument is a rational multiple of one linear
`p + q x`, the integrand is written in `u = p + q x` first, and its arguments are multiples of
`u` alone ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"csc(a+b*x)*csc(2*a+2*b*x)^2".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `cos(a + b x)` |
| `"csc(1+x)*csc(2+2*x)^2".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `cos(1 + x)` |
| `"sin(a+b*x)^3/sin(2*a+2*b*x)^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(a + b x)` |

### A function of one linear under a power inside a sum is written in that linear

`x^3/(a + b (c + d x)^3)` was left unevaluated, and `(c + d x)^4/(a + b (c + d x)^3)` ran for more
than eighty seconds. Written in `x`, the sum is a polynomial whose roots are those of a binomial
moved and scaled, which the partial fractions do not read. Where one linear `c + d x` stands under
the powers inside sums, and `x` elsewhere only in sums, products and powers, the integrand is
written in `u = c + d x` first. Rubi's 1.1.3.2, 1.2.3.2 and 1.3.1 write whole sections so
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^3/(a + b*(c + d*x)^3)".ToEntity().Integrate("x")` | `integral(...)` | powers and logarithms of `c + d x` and of a quadratic in it, and an arctangent |
| `"(c + d*x)^4/(a + b*(c + d*x)^3)".ToEntity().Integrate("x")` | ran for more than eighty seconds | `(c + d x)^2/(2 b)`, and the same logarithms and arctangent |
| `"(c*m + d*m*x)^3/(a + b*(c + d*x)^3)^2".ToEntity().Integrate("x")` | `integral(...)` | `m^3` times a rational function of `c + d x`, the logarithms and the arctangent |
| `"(c + d*x)^4/(a + b*(c + d*x)^2 + k*(c + d*x)^4)^2".ToEntity().Integrate("x")` | `integral(...)` | a rational function of `c + d x` and arctangents |

### A root of a square inside a sum is integrated on each side of the square's zero

**Answers where there were none.** `1/(1 + (x^2)^(3/2))` is `1/(1 + x^3)` for a positive `x` and
`1/(1 - x^3)` for a negative one; 2.5.0 declined it. The unreleased master wrote the root as
`sgn(x) x^3` and took the sign out in front of the integral, which holds for a factor of the
integrand and not inside a sum, and its answer was the first one's on both sides of zero. A sign
goes in front only where every occurrence of the root is a factor now; otherwise the integrand is
integrated with the sign 1 and with it -1, and the answer is the piecewise of the two on the sign of
the root's linear. The same where a square factor is taken out of a root: `x/(x + sqrt(x^6))` is
`1/(1 + x^2)` for a positive `x` and `1/(1 - x^2)` for a negative one, and was `sgn(x) arctan(x)`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(1+(x^2)^(3/2))".ToEntity().Integrate("x")` | `integral(...)` | the integral of `1/(1 + x^3)` where `x > 0`, of `1/(1 - x^3)` otherwise |
| `"1/(1+sqrt(x^2))".ToEntity().Integrate("x")` | `integral(...)` | `ln(x + 1)` where `x > 0`, `-ln(1 - x)` otherwise |
| `"1/(2+sqrt(x^2+2*x+1))".ToEntity().Integrate("x")` | `integral(...)` | `ln(x + 3)` where `x + 1 > 0`, `-ln(1 - x)` otherwise |
| `"x/(x+sqrt(x^6))".ToEntity().Integrate("x")` | `integral(...)` | `arctan(x)` where `x > 0`, a logarithm of `(1 + x)/(1 - x)` otherwise |

### A root of an even power is the modulus, and is no longer read as the power

**Wrong answers, silent.** `(u^2)^(3/2)` is `|u|^3`, and two readers took it for `u^3`: the
polynomial parser read `(x^q)^v` as `x^(q v)` for any `v`, and the integrator's `sin^p cos^q`
reader read `(sin^2)^(3/2)` as `sin^3`. Through `Simplify`'s long division the first made
`u^2` of `(u^2)^(3/2)/u` and `-1` of `sqrt(u^2)/(-u)`, wrong for every negative `u`; through
the second, six of Rubi's family-4 integrands were integrated as if the sign of the function
were positive on the whole line.
[#1387](https://github.com/asc-community/AngouriMath/issues/1387)

| | Was | Is |
|---|---|---|
| `"(u^2)^(3/2)/u".Simplify()` | `u^2 provided not u = 0` | `(u^2)^(3/2)/u`, unchanged |
| `"sqrt(u^2)/(-u)".Simplify()` | `-1 provided not u = 0` | unchanged |
| `"(u^3)^2/u".Simplify()`, a whole outer power | `u^5 provided not u = 0` | the same |
| `"x*sqrt(sin(x)^2)".Integrate("x")` | `-x cos(x) + sin(x)`, the integral of `x sin(x)` | unevaluated |
| `"(csc(x)^2)^(3/2)".Integrate("x")`, `"1/(csc(x)^2)^(7/2)".Integrate("x")` | the integral of `csc^3`, of `sin^7` | unevaluated |
| `"(a*sin(x)^2)^(5/2)".Integrate("x")`, `"1/sqrt(a*cot(x)^2)".Integrate("x")` | the integral of `a^(5/2) sin^5`, of `tan/sqrt(a)` | the antiderivative, with `sgn(sin x)` and `sgn(tan x)` in it |
| `"(b*tan(x)^3)^(3/2)".Integrate("x")`, an odd inner power | the integral of `b^(3/2) tan^(9/2)`: right where the tangent is positive, which is where the integrand is real, and off the principal branch where it is negative (`(u^3)^(3/2)` is `i` at `u = -1`, `u^(9/2)` is `-i`) | unevaluated |
| `"sec(c+d*x)^(3/2)/(b*sec(c+d*x))^(5/2)".Integrate("x")` | `sin(c+d*x)/(b^(5/2) d)`, right for `b > 0` | unevaluated |
| `"asin(sqrt(1 - x^2))/sqrt(1 - x^2)".Integrate("x")` | `-arcsin(sqrt(1 - x^2))^2/2`, right for `x > 0` and off by a sign for `x < 0` | unevaluated |
| `"asin(x/a)^(3/2)/sqrt(a^2 - x^2)".Integrate("x")` | `arcsin(x/a)^(5/2)/(5/2)`, right for `a > 0` | unevaluated |

A whole outer power still multiplies, since `(x^q)^3` is `x^q x^q x^q` however `x` is signed; a
fractional one over a first power is still read, `(a x)^(3/2)` as `a^(3/2) x^(3/2)`, the generic
reading every rule uses; over an odd power it is refused too, since `(u^3)^(3/2)` and `u^(9/2)`
differ at every negative `u`, and the tangent row is that reading integrated. The tangent,
`asin(x/a)` and `sec` rows are the cost — each right where the integrand is real, or for a
positive symbol, and only there: `a sqrt(1 - x^2/a^2)` was being collected to `sqrt(a^2 - x^2)` through
`(a^2)^(1/2) = a`, which is the same identity taken on a symbol, and it is not taken on a symbol
either now; and the secant quotient was simplified to `cos/(b^(5/2) d)` through
`(1/cos)^(3/2) = cos^(-3/2)`, which is wrong for every negative cosine and cancelled against the
same wrong step on the denominator. On the 1774-problem independent suites this is 1707 to 1705, the two `asin` rows, with the sign error gone.

### A power of x comes out of a fractional power of a sum whose every term has it

`(a x^j + b x^n)^p` with a fractional `p`, the power of x common to every term inside the power,
was integrated only where the sum factored over the rationals. Now `x^j` comes out:
`(a x^j + b x^n)^p` is `K x^(j p) (a + b x^(n - j))^p` with `K = (a x^j + b x^n)^p / (x^(j p) (a + b x^(n - j))^p)`,
which is constant wherever x and the sum are not zero and goes in front of the integral. For an even
whole `j` and a whole `j p` it is `sgn(x)^(j p)`. An odd whole `j` out of a sum of whole powers is
left as it was, since the sum is real on both sides of 0 and `x^(j p)` is not. Rubi's 1.1.4.2 and
1.1.4.3, `(c x)^m (a x^j + b x^n)^p` ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/sqrt(a*x^2 + b*x^5)".ToEntity().Integrate("x")` | `integral(...)` | `sgn(x)` times the integral of `1/(x sqrt(a + b x^3))` |
| `"x*sqrt(x^2*(a + b*x^3))".ToEntity().Integrate("x")` | `(a + b x^3)^(3/2)/(9/2 b)`, wrong where `x < 0` | `sgn(x) (a + b x^3)^(3/2)/(9/2 b)` |
| `"1/(x*sqrt(b*x^(2/3) + a*x))".ToEntity().Integrate("x")` | `integral(...)` | `K` times an antiderivative in `sqrt(b + a x^(1/3))` |
| `"x/sqrt(1 + 1/(c*x)^2)".ToEntity().Integrate("x")` | `integral(...)` | `sgn(x) (x sqrt(x^2 + 1/c^2)/2 - ln(2 x + 2 sqrt(x^2 + 1/c^2))/(2 c^2))`, for any `c` but 0 |

### A rational function of a sine over a symbolic quadratic in it is integrated over the two roots

**Improvement, not silent.** `sin(x)/(a + b sin(x) + c sin(x)^2)` and everything rational in
`sin(x)` and `cos(x)^2` over that quadratic came back unevaluated, or ran past any budget:
under the half-angle the quadratic in the sine is a quartic in `t` with a symbol in every
coefficient, which nothing factors, and the substitution `u = sin(x)` wants a cosine above the
bar that an even power does not give. Written in `s = sin(x)` the integrand is a rational function
of `s`, divided down and split over the written factors as any is, and the piece over the
quadratic is taken by the two roots `r = (-b ± sqrt(b^2 - 4ac))/(2c)`, each
`1/(sin(x) - r)` being `-2 atan((r tan(x/2) - 1)/sqrt(r^2 - 1))/sqrt(r^2 - 1)` for any complex
`r` but `±1` — a form checked by differentiation, not a piecewise on the sign of a root, which has
no value for the conjugate pair the ordinary coefficients give. At `±1`, which `a - a sin(x)^2` has
for every `a`, the quadratic under the half-angle is a square and the piece is `2/(r tan(x/2) - 1)`
([#1588](https://github.com/asc-community/AngouriMath/issues/1588)). The cosine the same way, with
`-2 atan(tan(x/2)/σ)/((1 + r) σ)`, `σ = sqrt((r - 1)/(r + 1))`. On each interval between the
poles of `tan(x/2)`, as every half-angle answer.

| | Was | Is |
|---|---|---|
| `"1/(a+b*sin(x)+c*sin(x)^2)".Integrate("x")` | unevaluated | the two arctangents over `sqrt(b^2 - 4ac)` |
| `"sin(x)^4/(a+b*sin(x)+c*sin(x)^2)".Integrate("x")` | unevaluated after more than three minutes | `(x - sin(2x)/2)/(2c) + b cos(x)/c^2 + (b^2 c - a c^2) x/c^4 + …` in 70 ms |
| `"csc(x)^2/(a+b*sin(x)+c*sin(x)^2)".Integrate("x")`, `"sec(x)^2/…"` | unevaluated | `-cot(x)/a + …`, the blocks over `sin^2` and `1 - sin^2` beside the two roots |
| `"1/(a+b*cos(x)+c*cos(x)^2)".Integrate("x")` | unevaluated | the cosine form |
| `"csc(x)^2/(a-a*sin(x)^2)".Integrate("x")` | unevaluated | `-cot(x)/a` and two terms rational in `tan(x/2)` |

### A rational function of the sine and cosine with symbols in it is no longer simplified under the half angle

**Answers where there were none.** Under `t = tan(x/2)` the integrand was simplified before it was
integrated, and with symbols in it the simplifier's search grew past any budget:
`sec(x)^2/(a + b cos(x))^3` spent about forty seconds in it and `sec(x)^2/(a + b cos(x))^4` more than two minutes,
where integrating what it returned takes a tenth of a second. A rational function of `t` with a
symbol in it is now put over one bar with its whole powers of products written as products of
powers, and not simplified further; one without a symbol, or with a root of `t` in it, is
simplified as before
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sec(x)^2/(a + b*cos(x))^3".ToEntity().Integrate("x")` | `integral(...)` | a piecewise antiderivative in `tan(x/2)` |
| `"sec(x)^2/(a + b*cos(x))^4".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"sec(c + d*x)^2/(a + b*cos(c + d*x))^4".ToEntity().Integrate("x")` | `integral(...)` | the same in `tan((c + d x)/2)` |

### A polynomial times an odd half power of a quadratic with compound coefficients is answered shortly

**Answers where there were none, and short ones where the unreleased master's were not.**
`sqrt(a + b x + g^2 x^2/f^2)` was declined by 2.5.0. The unreleased master answers it by reducing
a polynomial times an odd half power of the quadratic with one linear solve in the coefficients,
and carried as written, a quotient or a product of symbols among them came back in answers of 40,000
to 480,000 characters, after up to eight seconds, which took up to a minute more to differentiate
back. Each such coefficient is named for the solve now and written back in what it gives, and the
answers are a thousand characters, in a tenth of a second
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(a + b*x + g^2*x^2/f^2)".ToEntity().Integrate("x")` | `integral(...)` | the root times a linear, and an arcsine or a logarithm by the sign of `g^2/f^2` |
| `"(a + b*x + c*x^2/f)^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | the same with the power |
| `"x^2*sqrt(a*f + b*x + c*x^2/f)".ToEntity().Integrate("x")` | `integral(...)` | the same with a cubic in front of the root |

### A linear over a quadratic beside the root of another quadratic is integrated

`(g + h x)/(A sqrt(B))`, `A` and `B` two different quadratics, was left unevaluated wherever a
coefficient was a symbol, and so was what the tangent substitution makes of Rubi's
`trig^m (a + b tan + c tan^2)^(p/2)`: `1/sqrt(a + b tan(x) + c tan(x)^2)` is
`1/((1 + t^2) sqrt(a + b t + c t^2))`. It is closed now, as two arctangents of a linear over
`sqrt(B)` whose coefficients hold `sqrt(P^2 - E F)`, `P = A2 B0 - A0 B2`, `E = A2 B1 - A1 B2` and
`F = A1 B0 - A0 B1` -- the root Rubi's 1.2.1.6 calls `q`; where the ratio under a piece's root is a
negative number the piece is written as the hyperbolic arctangent it is. A rational function
beside the root with such a quadratic, a power of a linear or a power of `B` below the bar is
taken apart into pieces each closed the same way
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/sqrt(a + b*tan(x) + c*tan(x)^2)".ToEntity().Integrate("x")` | `integral(...)` | two arctangents of a linear in `tan(x)` over the root |
| `"(g + h*x)/((d + k*x + f*x^2)*sqrt(a + b*x + c*x^2))".ToEntity().Integrate("x")` | `integral(...)` | two arctangents of a linear over the root |
| `"cot(x)^3/(a + b*tan(x) + c*tan(x)^2)^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative |
| `"(2 + x)/((2 + 4*x - 3*x^2)*(1 + 3*x + 2*x^2)^(3/2))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative |
| `"1/((x^2 + 1)*sqrt(x^2 + x + 1))".ToEntity().Integrate("x")` | `integral(...)` | a logarithm and an arctangent |

### A rational function of `x^n` beside a power of `x` and the root of a binomial is integrated

**Answers where there were none.** `x/((a + b x^3)^(2/3) (c + d x^3))` and `x^4 (a + b x^3)^(1/3)/(c + d x^3)`,
Rubi's 1.1.3.4, were declined, while the same without the power of `x` in front was answered under
`u = x/(a + b x^n)^(1/n)`. That substitution takes `x^j (a + b x^n)^(k - (j + 1)/n)` beside a rational
function of `x^n` to `u^j` times a rational function of `u`, for any `j` below `n`, and is taken for
those now. Each answer holds on both sides of zero, an odd root of a negative being real here
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x/((a + b*x^3)^(2/3)*(c + d*x^3))".ToEntity().Integrate("x")` | `integral(...)` | logarithms and an arctangent of `x (a + b x^3)^(-1/3)` |
| `"x^4*(a + b*x^3)^(1/3)/(c + d*x^3)".ToEntity().Integrate("x")` | `integral(...)` | the same, beside a rational function of it |
| `"x/((1 + x^3)^(2/3)*(2 + x^3))".ToEntity().Integrate("x")` | `integral(...)` | the same with numbers |

### A rational function of the tangent of a linear argument with symbols in it is not simplified under the linear substitution

**Answers where there were none.** The substitution search reaches `tan(g + h x)` through `u = g + h x`,
and simplified the quotient of the integrand by `h` in `u` before asking for its integral: with the
nine symbols of Rubi's 4.3.4.2 in it that took about half a minute, for an expression that is the
integrand with its argument renamed and holds nothing to simplify. Under a linear candidate, where
the integrand in `u` is a function of the tangent alone and holds no imaginary unit, the quotient
is handed on as it is
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(a + b*tan(g + h*x))*(A + B*tan(g + h*x) + K*tan(g + h*x)^2)/(c + d*tan(g + h*x))^2".ToEntity().Integrate("x")` | `integral(...)` after 33 s | the antiderivative, in 4.5 s |
| `"(c + d*tan(g + h*x))^3*(A + B*tan(g + h*x) + K*tan(g + h*x)^2)/(a + b*tan(g + h*x))^3".ToEntity().Integrate("x")` | no answer within a minute and a half | the antiderivative, in 18 s |

### A rational function of `x^n` beside a power of a symbolic binomial is integrated without a search

**Answers where there were none.** A rational function of `x^n` beside `(c + d x^n)^(k - 1/n)` is a
rational function of `u = x/(c + d x^n)^(1/n)`, and the integrand in `u` was simplified before it
was integrated. With symbols in it the search for a simpler form grew with the power of the
binomial: `1/((a + b x^2)^(5/2) (1 + x^2))` took 5 s on the unreleased master, the `7/2` power ran
past 30 s, and both were declined on 2.5.0. What the search found was the factor the substitution
leaves on both sides of the bar, which is cancelled as it is written now, the other factors left as
they are written ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/((a + b*x^2)^(5/2)*(1 + x^2))".ToEntity().Integrate("x")` | `integral(...)` | powers of `x/sqrt(a + b x^2)`, and an arctangent or a logarithm by the sign of `a - b`, in 0.6 s |
| `"1/((a + b*x^2)^(7/2)*(1 + x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same, in 0.7 s |
| `"1/((a + b*x^2)^(9/2)*(1 + x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same, in 1 s |
| `"x^2/((a + b*x^2)^(7/2)*(c + d*x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same by the sign of `c (a d - b c)`, in 1 s |

### A binomial differential with symbols in it, or a power of `x` that is not whole, is integrated

**Answers where there were none.** Chebyshev's theorem says when `x^m (a + b x^n)^(p/q)` has an
elementary antiderivative, and it is about the exponents only. The rule for it read whole `m` and
`n` and rational numbers for `a` and `b`, and declined the rest. It reads rational `m` and `n` and
anything free of `x` for `a` and `b` now, and a power of a multiple of `x`, `(c x)^(5/2)`, as
`x^(5/2)` times `(c x)^(5/2)/x^(5/2)`, which is constant on either side of zero. Where `m + 1 +
n (p/q + 1)` is zero the answer is the one product of powers it is. With a symbol in the
coefficients the rule is asked after the rules for a root of a quadratic and for a rational
function of `x^n` beside the root of its binomial, which answer what they share with it more
shortly. Rubi's 1.1.2.2 and 1.1.3.2, `(c x)^m (a + b x^n)^p`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2/(a + b*x^4)^(3/4)".ToEntity().Integrate("x")` | `integral(...)` | logarithms and an arctangent of `(a + b x^4)^(1/4)/x` |
| `"x^6*(a + b*x^4)^(1/4)".ToEntity().Integrate("x")` | `integral(...)` | the same, beside a rational function of it |
| `"x^(7/3)*(a + b*x^2)^(1/3)".ToEntity().Integrate("x")` | `integral(...)` | a rational function, two logarithms and an arctangent of `(a + b x^2)^(1/3)/x^(2/3)`, written in `x^(1/3)` |
| `"(c*x)^(5/2)/(a - b*x^2)^(3/4)".ToEntity().Integrate("x")` | `integral(...)` | `(c x)^(5/2)/x^(5/2)` times the same in `(a - b x^2)^(1/4)/x^(1/2)` |
| `"(a - b*x^2)^(1/4)/(c*x)^(15/2)".ToEntity().Integrate("x")` | `integral(...)` | powers of `(a - b x^2)^(1/4)/(c x)^(1/2)` |

Of Rubi's 3,163 problems in those two files with an answer in functions the library has, 96 more
are answered than on the unreleased master, and none fewer. Nine that the unreleased master answered
are written otherwise: a rule ahead of this one takes the integrand apart and asks a sub-integral
that this one answers now, so the rule ahead finishes where it used to decline. Four of the nine come
out shorter and five longer -- `1/((c x)^(5/3) (a + b x^2)^(2/3))` was
`-3/(2 a) x (c x)^(-5/3) (a + b x^2)^(1/3)` there, and is the same function written in `x^(1/3)`.
2.5.0 declined all nine.

### A power of a multiple of a quadratic's derivative beside a power of the quadratic is a binomial

**Improvement, not silent.** `(b d + 2 c d x)^m (a + b x + c x^2)^p`, with a power that is not whole,
was declined or ran out of time. Under `t = b d + 2 c d x` the quadratic is
`(t^2/d^2 - (b^2 - 4 a c))/(4 c)`, so the integrand is a binomial in `t`. That binomial is asked with
its constant term named by a symbol of its own, since with `a - b^2/(4 c)` written in it the rules for
a binomial ran out of time too. Rubi's 1.2.1.2
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(a+b*x+c*x^2)^(3/2)/(b*d+2*c*d*x)^3".Integrate("x")` | `integral(...)` | the antiderivative |
| `"(a+b*x+c*x^2)^(1/2)/(b*d+2*c*d*x)^7".Integrate("x")` | `integral(...)` | the antiderivative |
| `"(b*d+2*c*d*x)^(5/2)/(a+b*x+c*x^2)^3".Integrate("x")` | `integral(...)` | the antiderivative |

### A polynomial over a power of a quadratic beside the root of another is integrated

`P/(A^k sqrt(B))`, `A` and `B` two different quadratics and `k` at least two, was left
unevaluated. So were Rubi's `sqrt(a + a sec(x))/(c + d sec(x))^2` and its kin, which the
half-angle tangent writes as `(1 - t^2)^(3/2)/((1 + t^2)((c + d) + (d - c) t^2)^2)`. They are
reduced a power at a time: `L/(A^j sqrt(B))`, with `L` linear, is the derivative of
`(p + q x) sqrt(B)/A^(j - 1)` plus a quadratic over `A^(j - 1) sqrt(B)`, from five linear
equations. The reduction goes down to the closed form over `A` of the entry above. With symbols
in both quadratics, the cube's coefficients grow past what the reduction takes, and it declines
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(g + h*x)/((d + k*x + f*x^2)^2*sqrt(a + b*x + c*x^2))".ToEntity().Integrate("x")` | `integral(...)` | a linear times the root over the quadratic, and two arctangents |
| `"(7 + 13*x)/((5 + x + 2*x^2)^3*sqrt(2 + x + 3*x^2))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative |
| `"sqrt(a + a*sec(x))/(c + d*sec(x))^2".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(x/2)` |

### A polynomial beside the square roots of two linears with symbols in them is integrated

`P(x) sqrt(c + d x) sqrt(e + f x)`, a polynomial beside the roots of two linears with symbols in
them or over their product, was left unevaluated, and so was the same over a power of a third
linear. These are Rubi's 1.1.1.6, `P(x) (a + b x)^m (c + d x)^n (e + f x)^p`. They are now
integrated by undetermined coefficients. The answer is a rational function times the two roots,
a logarithm of their quotient, and an arctangent of the quotient where a third linear is below
the bar. Where the slopes are numbers of opposite signs, the logarithm is written as the
arctangent it is ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(c + d*x)*sqrt(e + f*x)".ToEntity().Integrate("x")` | `integral(...)` | `(x/2 + (d e + c f)/(4 d f)) sqrt(c + d x) sqrt(e + f x)`, and a logarithm in `sqrt(f/d) sqrt(c + d x)/sqrt(e + f x)` |
| `"(A + B*x + C*x^2)*sqrt(c + d*x)*sqrt(e + f*x)".ToEntity().Integrate("x")` | `integral(...)` | a cubic times the two roots, and the same logarithm |
| `"(A + B*x + C*x^2)*sqrt(c + d*x)/((a + b*x)^3*sqrt(e + f*x))".ToEntity().Integrate("x")` | `integral(...)` | the roots over powers of `a + b x`, a logarithm, and an arctangent |
| `"x^2*sqrt(a + 2*x)*sqrt(c - 3*x)".ToEntity().Integrate("x")` | `integral(...)` | a cubic times the two roots, and an arctangent of `sqrt(6)/2 sqrt(a + 2 x)/sqrt(c - 3 x)` |

### A polynomial over a power of a linear beside the root of a quadratic is integrated

`P(x) Q^(m/2)/(g + h x)^k`, a polynomial over a power of a linear beside a half-odd power of a
quadratic `Q`, was left unevaluated wherever the linear was below the bar to a power past the
first, and with a symbol in the linear or in `Q` even to the first. These are Rubi's 1.2.1.9, and
with a constant or linear `P`, 1.2.1.2 to 1.2.1.4. They are now integrated by undetermined
coefficients, as the roots of two linears are. The answer is a rational function times `sqrt(Q)`,
a logarithm or an arcsine by the sign of the leading coefficient of `Q`, and a logarithm or an
arctangent by the sign of `Q` at the linear's root. Where the linear shares a root with `Q`, there
is no third term. Without the linear, the answers are as they were
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/((g + h*x)*sqrt(a + c*x^2))".ToEntity().Integrate("x")` | `integral(...)` | a logarithm in `(a h - c g x)/(sqrt(a h^2 + c g^2) sqrt(a + c x^2))` where `a h^2 + c g^2 > 0`, and an arctangent where it is negative |
| `"(d + k*x + f*x^2)*sqrt(a + c*x^2)/(g + h*x)^4".ToEntity().Integrate("x")` | `integral(...)` | powers of `1/(g + h x)` times `sqrt(a + c x^2)`, a logarithm or an arcsine, and the same as above |
| `"(2 + x + 3*x^2 - x^3 + 5*x^4)*sqrt(3 - x + 2*x^2)/(5 + 2*x)^5".ToEntity().Integrate("x")` | `integral(...)` | powers of `1/(5 + 2 x)` times the root, and two logarithms |
| `"(A + B*x + F*x^2)*sqrt(d^2 - m^2*x^2)/(d + m*x)^2".ToEntity().Integrate("x")` | `integral(...)` | a rational function times the root, and an arcsine |
| `"sqrt(1 - x^2)/(1 + x)^2".ToEntity().Integrate("x")` | `integral(...)` | `-2 sqrt(1 - x^2)/(1 + x) + arcsin(-x)` |
| `"sqrt(x^2 + 1)/x^2".ToEntity().Integrate("x")` | `integral(...)` | `-sqrt(x^2 + 1)/x + ln(2 sqrt(x^2 + 1) + 2 x)` |

### A power of the quadratic below the bar beside its root is integrated

And with a power of the quadratic below the bar beside its root, `P(x)/((g + h x)^k Q^(n + 1/2))`,
which was left unevaluated as well. The answer is powers of `1/(g + h x)` and linears over powers
of `Q`, times `sqrt(Q)`, and the same two integrals that are not algebraic
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/((x + 2)^2*(x^2 + 1)^(3/2))".ToEntity().Integrate("x")` | `integral(...)` | `((4 + 3 x)/(25 (x^2 + 1)) - 1/(25 (x + 2))) sqrt(x^2 + 1)`, and a logarithm in `(2 - 4 x)/(2 sqrt(5) sqrt(x^2 + 1))` |
| `"(1 + 3*x + 4*x^2)/((1 + 2*x)^3*(2 + 3*x^2)^(5/2))".ToEntity().Integrate("x")` | `integral(...)` | linears over `(2 + 3 x^2)^2` and `2 + 3 x^2` and powers of `1/(1 + 2 x)`, times the root, and a logarithm |
| `"(d + k*x + f*x^2)/((g + h*x)^2*(a + c*x^2)^(3/2))".ToEntity().Integrate("x")` | `integral(...)` | the same in the symbols, and a logarithm or an arctangent by the sign of `a h^2 + c g^2` |

### An even root of a quotient with an odd power below the bar keeps its sign below zero

**Wrong answers on the unreleased master, right ones now.** `sqrt((1 + x)/x^3)` is real for `x < -1` as
well as for `x > 0`, and was integrated as `sqrt(1 + x) x^(-3/2)`, which it is for `x > 0` only: below
`-1` the two differ by a sign, and the answer was wrong at every point there. The check that decides
whether an even root of a quotient may be written apart counted a factor below the bar as though it
were above it; it counts against those above it now, and where the root does not come apart, a linear's
odd power below it comes out with its sign in front of the answer, `|1 + x|` being `sgn(1 + x) (1 + x)`.
2.5.0 declined these ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt((1 + x)/x^3)".ToEntity().Integrate("x")` | `integral(...)` | `sgn(x)` times the antiderivative for `x > 0`, right on both sides |
| `"sqrt(x/(1 + x)^3)".ToEntity().Integrate("x")` | `integral(...)` | the same with `sgn(1 + x)` |
| `"((1 + x)/x^3)^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | the same as the first |

### A function comes out of a fractional power of its even power with its sign

**Improvement, not silent.** The entry two above made `(sin(x)^2)^(3/2)` the modulus `|sin(x)|^3`
and left three of Rubi's integrands unevaluated for it, and answered two more in powers of the
modulus through the substitutions. The modulus is the function times its sign, and the sign is a
constant between the function's zeros: `(a sin(x)^2)^(5/2)` is `a^(5/2) sgn(sin(x)) sin(x)^5`, the
sign comes out in front of the integral, and what is left is a whole power any rule reads. An even
power of a real function is not negative, so `(c q)^p = c^p q^p` holds for the principal powers
whatever `c` is, and nothing is assumed about the constant — checked at `a = -3` as well as
`a = 2`. Whole products of the exponents only, at the top of the integration only, and after the
rule that does the same for a polynomial under a square root, whose answers are as they were.
[#1387](https://github.com/asc-community/AngouriMath/issues/1387)

| | Was | Is |
|---|---|---|
| `"x*sqrt(sin(x)^2)".Integrate("x")` | unevaluated | `sgn(sin(x)) (sin(x) - x cos(x))` |
| `"(csc(x)^2)^(3/2)".Integrate("x")`, `"1/(csc(x)^2)^(7/2)".Integrate("x")` | unevaluated | `sgn(csc(x))` times the integral of `csc^3`, of `sin^7` |
| `"(sin(x)^2)^(3/2)".Integrate("x")` | `(2/3 tan(x/2)^5 + 2 tan(x/2)^3) ((2 tan(x/2))^2)^(1/2)/((tan(x/2)^2 + 1)^3)` | `sgn(sin(x)) (cos(x)^3/3 - cos(x))` |
| `"(a*sin(x)^2)^(5/2)".Integrate("x")` | `sgn(sin(x)) a^(5/2) (...)` in `(1 - abs(sin(x))^2)^(1/2)` over `sgn(cos(x))` | `sgn(sin(x)) a^(5/2) (2 cos(x)^3/3 - cos(x) - cos(x)^5/5)` |
| `"1/sqrt(a*cot(x)^2)".Integrate("x")` | `-sgn(tan(x)) ln(1/sqrt(1 + abs(tan(x))^2))/sqrt(a)` | `sgn(cot(x)) ln(1 + tan(x)^2)/(2 sqrt(a))` |
| `"(b*tan(x)^2)^(5/2)".Integrate("x")` | `sgn(tan(x)) b^(5/2) ((abs(tan(x))^2)^2/2 - abs(tan(x))^2 + ln(abs(tan(x))^2 + 1))/2` | the same with `tan(x)` for `abs(tan(x))` |

### A power of the cotangent that is not whole below the bar is integrated beside the tangent

**Answers where there were none.** `1/(cot(x)^(7/2) (a + b tan(x))^(3/2))` was declined while
`tan(x)^(7/2)/(a + b tan(x))^(3/2)` was answered: the substitution `u = tan(x)` wrote the cotangent
as `1/u`, and nothing read a power of `1/u` that is not whole below the bar. Such a power is written
as the tangent's above it now, `1/cot(x)^p = tan(x)^p/K`, with `K = cot(x)^p tan(x)^p` dividing the
answer: `K` is 1 where the tangent is positive and a constant on every interval where it keeps its
sign, so the answer holds where the tangent is negative too. Only below the bar and beside the
tangent; a power of the cotangent above it, or alone, keeps its answer. Rubi's 4.3.2.1 and 4.3.3.1
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(cot(x)^(7/2)*(a + b*tan(x))^(3/2))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `sqrt(tan(x))` and `sqrt(a + b tan(x))`, over `K` |
| `"1/(cot(x)^(5/2)*(a + i*a*tan(x))^(5/2))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `sqrt(tan(x))/sqrt(1 + i tan(x))`, over `K` |
| `"(A + B*tan(x))/(cot(x)^(3/2)*(a + i*a*tan(x)))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `sqrt(tan(x))`, over `K` |

### A fractional power of a product of trigonometric functions keeps its sign

**Wrong answers, silent.** The `sin^p cos^q` reader took a power of a product with an even
denominator as the product of the powers: `(sin(x) tan(x))^(3/2)` as `sin(x)^3 cos(x)^(-3/2)`,
where it is `|sin(x)|^3 cos(x)^(-3/2)`. After 2.5.0 the integrator answered it so, with the wrong
sign wherever the sine is negative and the cosine positive, where the integrand is real. What was
read is the integrand only up to a constant on each interval where the factors keep their signs,
so the answer is now multiplied by that constant, the integrand over what was read, and is the
integrand's antiderivative wherever both are defined. An odd denominator is read as before, as the
real root. Rubi's 4.7.7.

| | Was (2.5.0) | Now |
|---|---|---|
| `"(sin(x)*tan(x))^(3/2)".Integrate("x")` | `integral((sin(x) * tan(x)) ^ (3/2), x)` | `(sin(x) tan(x))^(3/2)/(sin(x)^3 cos(x)^(-3/2))` times `2/sqrt(cos(x)) + 2/3 cos(x)^(3/2)` |
| `"(sin(x)*tan(x))^(1/2)".Integrate("x")` | `integral(sqrt(sin(x) * tan(x)), x)` | `sqrt(sin(x) tan(x))/(sin(x)/sqrt(cos(x)))` times `-2 sqrt(cos(x))` |

### A shared symbol among a denominator sum's coefficients comes out in front of it

**Improvement, not silent.** `tan(x)/(a + a csc(x))` came back unevaluated while
`tan(x)/(1 + csc(x))` was answered: under `u = sin(x)` the denominator is `(a u + a)(1 - u^2)`,
a symbolic linear beside a rational quadratic that shares its root, which the symbolic split
declines, and under the half-angle it is a sextic with `a` in every coefficient, which nothing
factors. A written sum below the bar whose coefficients share a symbol is now that symbol times
a polynomial over the rationals — `(a u + a)` is `a (u + 1)` — before the denominator is
refactored, so the repeated factor is found and the rational machinery answers; and the lowest
power of the variable every term holds comes out the same way.

| | Was | Is |
|---|---|---|
| `"tan(x)/(a + a*csc(x))".Integrate("x")` | unevaluated | `(-1/(2 (sin(x) + 1)) + 3/4 ln(sin(x) + 1) + 1/4 ln(sin(x) - 1))/(-a)` |
| `"sec(x)^2/(a + a*csc(x))".Integrate("x")` | unevaluated | in `tan(x/2)`, over `a` |
| `"x^2/((a*x + a)*(1 - x^2))".Integrate("x")` | unevaluated | the partial fractions over `(x + 1)^2 (1 - x)`, over `a` |
| `"cot(x)^3/(a + b*csc(x))".Integrate("x")` | unevaluated | `-1/(b sin(x)) - a ln(sin(x))/b^2 + (b^2 - a^2) ln(a sin(x) + b)/(a b^2)`, once `a u^4 + b u^3` is read as `u^3 (a u + b)` |

### Symbolic factors of a denominator that share a factor are written over it

**Improvement, not silent.** Partial fractions over factors with symbols in them read the written
factors as coprime and squarefree, and the refactoring after them reads rational coefficients only.
A quadratic that shares a root with a linear beside it, or that is a square, was read as an
irreducible quadratic and declined. The written factors are now taken apart over their greatest
common divisors, in every variable, with one another and each with its derivative, and a factor
shared with the numerator is cancelled. Rubi's 1.2.1.2
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/((d+e*x)*(a*d*e+(c*d^2+a*e^2)*x+c*d*e*x^2)^2)".Integrate("x")` | unevaluated | the partial fractions over `(d + e x)^3 (a e + c d x)^2` |
| `"(d+e*x)^6/(a*d*e+(c*d^2+a*e^2)*x+c*d*e*x^2)^4".Integrate("x")` | no answer in 90 s | the antiderivative of `(d + e x)^2/(a e + c d x)^4` |
| `"1/((d+e*x)^2*(c*d^2+2*c*d*e*x+c*e^2*x^2)^3)".Integrate("x")` | unevaluated | `-1/(7 c^3 e^8 (d/e + x)^7)` |
| `"x^2/((a+b*x)*(a^2+2*a*b*x+b^2*x^2))".Integrate("x")` | unevaluated | the partial fractions over `(a + b x)^3` |

### Fractional powers of `a ± a sin` are integrated by the half angle at which they are squares

**Improvement, not silent.** `(A + C sin(y)^2)/((c - c sin(y))^(3/2) sqrt(a + a sin(y)))` and
its kin ran past any budget: the substitution search has no radical of a trigonometric function
it can rationalise, and the closed rule for `(a + b sin)^n` with `a^2 = b^2` reads one such power
alone. `1 + sin(y)` is `2 sin(u)^2` and `1 - sin(y)` is `2 cos(u)^2` for `u = y/2 + pi/4`
(`1 ± cos(y)` the same with `u = y/2`), so each such power is a whole power of a sine or cosine
of `u` times its sign — a constant between the zeros, written in front — and what is left is
rational in `sin(u)` and `cos(u)`, which the rules answer. Exact for any `a`: `1 + sin(y)` is not
negative, so `(a q)^p = a^p q^p` for the principal powers whatever `a` is; `a^2 = b^2` is decided,
not assumed, and a lone positive power still goes to the closed rule first, which answers it in
one form across the zeros.

| | Was | Is |
|---|---|---|
| `"(A + C*sin(x)^2)/((c - c*sin(x))^(3/2)*sqrt(a + a*sin(x)))".Integrate("x")` | unevaluated after the budget | `sgn(cos(u)) sgn(sin(u)) (…)` in `u = x/2 + pi/4`, half a second |
| `"(a + a*sin(x))^(1/2)/(c - c*sin(x))^(5/2)".Integrate("x")` | unevaluated | `sgn(sin(u)) sgn(cos(u)) sqrt(2a)/(4 c^(5/2)) (…)` |
| `"sqrt(a + a*sin(x))".Integrate("x")` | unevaluated (`a^2 = a^2` is not decided by evaluation) | `-2 sqrt(2a) sgn(sin(u)) cos(u)` |
| `"sqrt(1 + sin(x))".Integrate("x")` | `-2 cos(x)/sqrt(1 + sin(x))` | unchanged, the closed rule's |

### A half-odd power of the secant beside the cosine is integrated as the cosine's

**Answers where there were none.** `sec(x)^(3/2)/sqrt(1 + cos(x))` was declined while
`1/(cos(x)^(3/2) sqrt(1 + cos(x)))`, which it is wherever the cosine is positive, was answered: the
substitutions that read a function of the cosine read the secant as `1/cos` to a whole power only.
A half-odd power of the secant beside the cosine is written as the cosine's on the other side of
the bar now, `sec(x)^p = K/cos(x)^p`, with `K = sec(x)^p cos(x)^p` in front of the answer: `K` is 1
where the cosine is positive and -1 where it is negative, so the answer holds on both sides. The
same for the cosecant beside the sine. Rubi's 4.2.2.1, 4.2.3.1 and 4.2.4.2
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sec(x)^(3/2)/sqrt(1 + cos(x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(x/2)`, times `K` |
| `"1/((a + a*cos(x))^(7/2)*sec(x)^(7/2))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(x/2)`, over `K` |
| `"(A + c*cos(x)^2)*sec(x)^(11/2)*sqrt(a + a*cos(x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(x/2)`, times `K` |
| `"sqrt(csc(x))*sqrt(a + a*sin(x))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `tan(x/2)`, times `csc(x)^(1/2) sin(x)^(1/2)` |

### Half-odd powers of `a ± a sec` and `a ± a csc` are integrated by the half-angle tangent

Rubi's `(a + b sec)^m (d sec)^n` files with `a^2 = b^2` hold about a thousand problems with a
half-odd `m`, and none was answered, `sqrt(1 + sec(x))` included: the half angle at which
`a + a cos(y)` is a square leaves a root of the cosine below the bar here. Under
`t = tan(y/2)`, `1 + sec(y)` is `2/(1 - t^2)`, `1 - sec(y)` is `-2 t^2/(1 - t^2)` and `sec(y)` is
`(1 + t^2)/(1 - t^2)`, so beside a power of the secant and anything rational in the sine and
cosine the whole is a rational function of `t` beside one root, of `1 - t^2` or of `1 + t^2`.
Two roots, which a half-odd power of the secant beside a whole one of `1 ± sec` makes, are
elliptic and still declined. The cosecant's the same way through the complement, and a root of
`1 - sec(y)` carries `sgn(tan(y/2))`, constant between its zeros. Each root written apart is exact
where `cos(y)` is positive; beyond it two roots can make the integrand real while each has turned
its sign on its own, so an answer through two says `provided cos(y) >= 0`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(1 + sec(x))".ToEntity().Integrate("x")` | `integral(...)` | an arctangent in `tan(x/2)` |
| `"sec(x)/sqrt(a + a*sec(x))".ToEntity().Integrate("x")` | `integral(...)` | `2 arcsin(tan(x/2))/sqrt(2a)` |
| `"1/(sec(x)^(3/2)*sqrt(1 + sec(x)))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative through a root of `1 + tan(x/2)^2` |
| `"sqrt(a - a*sec(x))".ToEntity().Integrate("x")` | `integral(...)` | a logarithm in `tan(x/2)`, times `sgn(tan(x/2))` |
| `"sqrt(1 + csc(x))".ToEntity().Integrate("x")` | `integral(...)` | an arctangent in `tan(pi/4 - x/2)` |

### An even power of the secant or the cosecant under a root is written in the tangent

**Answers where there were none.** `sqrt(a + b csc(x)^2)`, `(a + b sec(x)^2)^(3/2)` and their kin,
Rubi's 4.5.7 and 4.6.7, were declined, while `sqrt(a + b tan(x)^2)` and `sqrt(a + b cot(x)^2)` were
answered through `u = tan(x)`. That substitution writes an even power of the secant or the
cosecant in the tangent, `sec^2 = 1 + tan^2` and `csc^2 = (1 + tan^2)/tan^2`, but went on to do so
only where the tangent, or a sine or a cosine under a root, was already in the integrand. An even
power of the secant or the cosecant under a root goes on now too. The cosecant's answers carry
`sgn(tan(x))`, from the root of `1/tan(x)^2`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(a + b*csc(x)^2)".ToEntity().Integrate("x")` | `integral(...)` | arctangents in `sqrt((a + b) tan(x)^2 + b)`, times `sgn(tan(x))` |
| `"(a + b*csc(c + d*x)^2)^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | the same in `tan(c + d x)` and an algebraic part, by the signs of `a` and `b` |
| `"sqrt(a + b*sec(x)^2)".ToEntity().Integrate("x")` | `integral(...)` | arctangents in `tan(x)/sqrt(a + b (1 + tan(x)^2))` |
| `"1/sqrt(a + b*sec(x)^2)".ToEntity().Integrate("x")` | `integral(...)` | an arctangent or a logarithm in the same, by the sign of `a` |
| `"sqrt(1 + csc(x)^2)".ToEntity().Integrate("x")` | `integral(...)` | logarithms and an arctangent in `sqrt(2 tan(x)^2 + 1)`, times `sgn(tan(x))` |
| `"1/sqrt(-1 + csc(x)^2)".ToEntity().Integrate("x")` | `integral(...)` | `sgn(tan(x)) ln(1 + tan(x)^2)/2` |

### The rational canonical form keeps the points where the expression has no value

**Wrong answers, silent.** `CanonicalizeAsRationalFunction` and `Transformation.RationalCanonicalization` kept the condition a
cancelled factor owes, `x/x` being `1 provided not x = 0`, and dropped the one that dividing by a
quotient owes: `1/(1/x)` was `x`, which is `0` at zero, where `1/(1/x)` has no value. The
denominator a quotient turns over is now kept as a condition too, and so is the denominator under a
numerator that vanishes: `0/x` is `0 provided not x = 0`. The condition is written canonically,
because it is part of a form whose point is that equal trees mean equal functions: the square-free
part of what was excluded, without what the reduced denominator still excludes. So `x^2/x^2` and
`x/x` meet, and a cancelled factor that the denominator still has is not repeated. The form now
gathers through the first step of `AsSingleFraction`, so the two agree on what a single quotient is
and on where it has a value. [#1618](https://github.com/asc-community/AngouriMath/issues/1618).
Both columns measured on a build, `v2.5.0` against this change.

| `….ToEntity().CanonicalizeAsRationalFunction()` of | Was (2.5.0) | Is |
|---|---|---|
| `"1/(1/x)"` | `x` | `x provided not x = 0` |
| `"1/(1 + 1/x)"` | `x / (x + 1)` | `x / (x + 1) provided not x = 0` |
| `"(a/b)/(c/d)"` | `a * d / (b * c)` | `a * d / (b * c) provided not d = 0` |
| `"0/x"` | `0` | `0 provided not x = 0` |
| `"x^2/x^2"` | `1 provided not x ^ 2 = 0` | `1 provided not x = 0` |
| `"(x^2 - 1)/(x^2 + 2x + 1) + 1/(x + 1)"` | `x / (x + 1) provided not x ^ 2 + 2 * x + 1 = 0` | `x / (x + 1)` |

### A partial-fraction coefficient with symbols in it is in lowest terms, its rational content included

**Improvement, not silent.** The decomposition over written factors with symbols among their
coefficients put each coefficient in lowest terms by the polynomial gcd, which normalizes its
divisor to whole coprime coefficients and so never cancels a number: `-1024/(1024 a)` stayed as
it was. Newton's iteration for the inverse modulo a power of a quadratic squares its iterate every
round, so `tanh(x)^6/(a + a sech(x))` — `1/(u (1 + u^2)^6 (a (1 + u^2) + 2 a u))` under
`u = e^x` — had integers of twelve hundred digits by the third round and spent its minute in
their gcd. The rational content of the denominator now goes up into the numerator, and the values
the elimination solves for are put in lowest terms before the terms are built, where
`csch(x)^5/(a + b cosh(x))` after its Hermite reduction had two thousand nodes of `a` and `b` in
every term and each went round the chain without returning. And a constant factor in front of a
rational denominator is not a symbolic coefficient: `a (1 + u^2) + 2 a u` with its content out is
`a` times `(1 + u)^2`, which the refactoring over the rationals reads, and the answer is a page
shorter than the symbolic split's.

| | Was | Is |
|---|---|---|
| `"tanh(x)^6/(a + a*sech(x))".Integrate("x")` | unevaluated after two minutes | `(-1/6 - 25/1536 e^x - … + 3/512 e^(10x))/(a (e^(2x) + 1)^5 (e^x + 1)) + ln(e^x)/a - …`, 2 s |
| `"csch(x)^5/(a + b*cosh(x))".Integrate("x")` | unevaluated | a piecewise on the sign of `a^2 - b^2`, its rational part `(1/4 (3a^3 - 7ab^2) e^x + 2b^3 e^(2x) + …)/(a^4 - 2a^2 b^2 + b^4)` over the factors, 1.3 s |
| `"1/(u*(u^2 + 1)^6*(a*(u^2 + 1) + 2*a*u))".Integrate("u")` | unevaluated | `(17/24 - 829/1536 u + … - 273/512 u^10)/(a (u^2 + 1)^5 (u + 1)) + ln(u)/a - 7/64 ln(u + 1)/a - 57/128 ln(u^2 + 1)/a - 329/512 arctan(u)/a` |
| `"u^5/((u^2 - 1)^5*(2*a*u + b*(u^2 + 1)))".Integrate("u")` | unevaluated | the same piecewise in `u`, its coefficients `1/256 (3a^3 - 7ab^2)/(a^4 - 2a^2 b^2 + b^4)` and the like rather than two thousand nodes each |

### A logarithm of a nested quotient is written over one bar under the exponential substitution

**Improvement, not silent.** `atanh(tanh(a + b x))` reaches the integrator as
`1/2 ln((1 + (w - 1)/(w + 1))/(1 - (w - 1)/(w + 1)))` in `w = e^(2(a + b x))`, and under `u = w`
that logarithm is `1/2 ln(u)`; but the combining on the way into the substitution reaches the
quotient the integrand is and not one inside a function, the simplifier combines nothing (#1239),
and the substitution search compared its candidates against the nested spelling and never saw
the `u`. A logarithm whose argument is a rational function of `u` with a quotient nested in it is
now written over one bar in lowest terms, where that is smaller, before the question in `u` is
asked. Only under that substitution; `Simplify` still combines nothing.

| | Was | Is |
|---|---|---|
| `"1/sqrt(atanh(tanh(a + b*x)))".Integrate("x")` | unevaluated after 49 s | `2 (1/2 ln(e^(2 b x + 2 a)))^(1/2)/(1/2)/(2 b)`, 0.3 s |
| `"atanh(tanh(a + b*x))^(1/2)".Integrate("x")` | unevaluated | `2 (1/2 ln(e^(2 b x + 2 a)))^(3/2)/(3/2)/(2 b)` |

### A conjunction drops a repeated conjunct, and an equality against a strict sign of the same quantity is `False`

Evaluation of `and` no longer writes a conjunct twice, and decides `q = 0 and q > 0` (and `q = 0
and q < 0`, and `q = 0 and not q = 0`) to `False` — everywhere, not only over the reals, since the
equality is `False` wherever `q` is not real and `False and anything` is `False`. Two order
comparisons, `q > 0 and q < 0`, are left to `Simplify`, which decides them under `q in RR` as
before ([#876](https://github.com/asc-community/AngouriMath/issues/876)). What this is for: a
piecewise combined with a piecewise joins every case's predicate to every other's, and with the
repeats and contradictions kept the cases multiplied — `(c - c/(a^2 x^2))^(9/2) / e^(3 acoth(a x))`
reached 3^12 cases and 8 GB inside integration by parts
([#1414](https://github.com/asc-community/AngouriMath/issues/1414)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `((x > 0 and y > 0) and x > 0).Evaled` | `x > 0 and y > 0 and x > 0` | `x > 0 and y > 0` |
| `(q = 0 and q > 0).Evaled` | as written | `False` |
| `(q = 0 and not q = 0).Evaled` | as written | `False` |
| `(x < 0 and x = 0).Simplify()` | `False provided x in RR` | `False` — the condition was over-strong, one conjunct is false wherever `x` is |
| `(x > 0 and x > 0).Evaled` | `x > 0` | `x > 0` (unchanged) |

### Two piecewises split on one quantity combine sign by sign

A sum or product of two piecewises still joins every case's condition to every other's, and now
drops two more kinds of pair. A quantity is read up to a constant factor, so `f = 0 and not 2 f = 0`
is `False`. And `q > 0 and q < 0`, which is `NaN` off the real line rather than `False`, goes where
every case after it tests `q` too: off the real line each of those is false or undecided, so no case
it was keeping from being reached could be. Where a later case tests something else it stays. Sums
of the answers the radical substitution gives for `x sqrt(1 + d x) sqrt(1 + f x)` had reached
32,769 cases ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `piecewise(1 provided q = 0, 2 provided q > 0, 3 provided q < 0) + piecewise(10 provided q = 0, 20 provided q > 0, 30 provided q < 0)`, `.InnerSimplified` | nine cases | three: `11 provided q = 0`, `22 provided q > 0`, `33 provided q < 0` |
| `piecewise(1 provided f = 0, 2 provided not 2 * f = 0) + piecewise(10 provided 3 * f = 0, 20 provided not f = 0)`, `.InnerSimplified` | four cases | two |
| the same with a third case `provided p > 0` in each | nine cases | nine, unchanged |
| `"x*sqrt(1 + d*x)*sqrt(1 + f*x)".Integrate("x")`, and with `x^2`, or `A + B x + K x^2`, for `x` | left unevaluated | the antiderivative, a piecewise of three cases |

### A union with an empty interval is the other set

**Wrong answer fixed.** Two intervals were joined wherever one ended where the other began, before
either was asked whether it was empty. `[1; 0]` is empty, and it ends at 0 where `[0; 1]` begins,
so `[0; 1] \/ [1; 0]` was joined into `{ 1 }`, and `[3; 1] \/ [1; 2]` into `[3; 2]`, which is
empty. An empty interval now adds nothing. With ends that are not numbers either interval may be
empty -- `[a; b]` is, when `a` is above `b` -- so two such intervals are no longer joined unless an
end is infinite, and `[a; b] \/ [b; c]`, which was `[a; c]`, is left as written.
[#1634](https://github.com/asc-community/AngouriMath/issues/1634). Both columns measured on a
build, `v2.5.0` against this change.

| `"….".ToEntity().Simplify()` of | Was (2.5.0) | Is |
|---|---|---|
| `[0; 1] \/ [1; 0]` | `{ 1 }` — wrong | `[0; 1]` |
| `[3; 1] \/ [1; 2]` | `[3; 2]`, which is empty — wrong | `[1; 2]` |
| `[a; b] \/ [b; a]` | `{ b }` — wrong | `[a; b] \/ [b; a]` |
| `[a; b] \/ [b; c]` | `[a; c]` — wrong where `a > b` or `b > c` | `[a; b] \/ [b; c]` |
| `(-oo; x] \/ [x; +oo)` | `RR` | `RR` (unchanged: neither can be empty) |

### `ZZ*` and `ZZ+` are the non-negative and the positive integers

`ZZ*` = `{0, 1, 2, ...}` and `ZZ+` = `{1, 2, 3, ...}` are special sets, spelled as MathWorld spells
them, and there is deliberately no set named after the natural numbers
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)). Two inputs change:

| Input | Was (2.5.0) | Now |
|---|---|---|
| `0 in ZZ*` | `UnhandledParseException` | `True` |
| `ZZ*2` | `ZZ * 2` (a product of a set and a number) | `UnhandledParseException` — `ZZ*` is a token; `ZZ * 2` with the space still reads as before |
| `domain(x, ZZ*)` | `UnhandledParseException` | a node over the codomain `Domain.NonNegativeInteger`; `domain(x, ZZ+)` over `Domain.PositiveInteger`, both narrower than `Integer` |
| `NN`, `3 in NN` | the variable `NN` | `InvalidArgumentParseException` naming `ZZ*` and `ZZ+` |

`Domain` has the two new members `PositiveInteger` and `NonNegativeInteger` between `Boolean` and
`Integer`, so the numeric values of `Integer` and everything above it have moved; nothing in the
library reads the numbers, and JSON carries the name.

A set builder written with its membership in the name position, `{ x in ZZ : x < 0 }`, used to
keep `x in ZZ` *as* its bound name, so nothing substituted for `x` and membership was never
decided; the name is now `x` and the membership is the first conjunct of the predicate, and the
set prints as it was written:

| Input | Was (2.5.0) | Now |
|---|---|---|
| `-3 in { x in ZZ : x < 0 }` | as written | `True` |
| `{ x in ZZ : x < 0 } = { x : x in ZZ and x < 0 }` (as entities) | unequal | equal |

And the set operators now answer for two special sets, which nest as `ZZ+ ⊂ ZZ* ⊂ ZZ ⊂ QQ ⊂ RR ⊂ CC`
with `BB` beside them, where 2.5.0 left every such expression as written:

| Input | Was (2.5.0) | Now |
|---|---|---|
| `ZZ unite QQ` | `ZZ \/ QQ` | `QQ` |
| `ZZ intersect RR` | `ZZ /\ RR` | `ZZ` |
| `BB intersect ZZ` | `BB /\ ZZ` | `{}` |
| `RR \ RR` | `RR \ RR` | `{}` |
| `QQ \ ZZ` | `QQ \ ZZ` | `{ x in QQ : not x in ZZ }` |
| `ZZ \ ZZ*` | — | `{ x in ZZ : x < 0 }`; `ZZ* \ ZZ+` is `{ 0 }` |

### `a = b (mod n)` is a congruence

The statement that `n` divides `a - b`, written with the modulus once at the end as mathematics
writes it, is a node ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)); `≡` is
read as well. Neither spelling parsed before.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `5^6 = 1 (mod 7)` | `UnhandledParseException` | `True` |
| `(n - a)^2 = a^2 (mod n)` | `UnhandledParseException` | `True` under `Simplify` |
| `x ≡ y (mod 3)` | `UnhandledParseException` | the congruence, printed `x = y (mod 3)` |
| `x < y (mod 3)` | `UnhandledParseException` | `InvalidArgumentParseException` |
| `5 mod 3` | `2` | `2` (unchanged: the operator is a number, the relation is a statement) |

### `forall`, `exists` and `exists!` are quantifiers, and were names

`forall x in S : P`, `exists x in S : P` and `exists! x in S : P` — also `∀`, `∃`, `∃!` — are the
statements that every, some, or exactly one member of `S` satisfies `P`, decided over a finite set
by evaluation, over an infinite one by a counterexample or by the solver, and left as written
otherwise ([#1409](https://github.com/asc-community/AngouriMath/issues/1409),
[#225](https://github.com/asc-community/AngouriMath/issues/225)). The three words are keywords
now, which is the one silent change: a variable spelled `forall` or `exists` no longer parses.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall x in RR : x^2 >= 0` | `UnhandledParseException` | `True` |
| `exists x in RR : x^2 + 1 = 0` | `UnhandledParseException` | `False`; over `CC`, `True` |
| `forall x in {1, 2, 3, 4} : exists y in {3, 4, 5, 6, 7, 8} : x + y = 7` | `UnhandledParseException` | `True` |
| `∀ x in RR : x^2 >= 0` | `UnhandledParseException` | `True` |
| `forall x : x^2 >= 0` | `UnhandledParseException` | `InvalidArgumentParseException`: the set is mandatory |
| `forall + 1` | `forall + 1`, a variable named `forall` | `UnhandledParseException` |
| `exists(x)` | `exists * x` | `UnhandledParseException` |

### A quantifier after a connective takes the rest of the line

`P implies forall x in S : Q`, and the same after `and`, `&`, `or`, `xor` and `->`, parses as
`P implies (forall x in S : Q)`: the quantifier's body runs to the end of the line, as it does at
the start of one, and as `not forall x in S : P` already did. It was a parse error, and the brackets
were needed, so Sullivan and Mackey's §5.5.3 Try 3 now reads as they write it
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"forall n in ZZ : n >= 2 implies exists a in ZZ* : exists b in ZZ* : n = 2 a + 3 b".ToEntity().Simplify()` | `UnhandledParseException` (a parse error on master too) | `True` |

### A floor or a ceiling is read with what the quantifiers establish

Under a quantifier, the floor and the ceiling of an expression in the bound name are rewritten
by what the quantifier establishes about it. A whole argument is its own floor and ceiling. A
whole term comes out of either. A negated argument turns one into the other. And the ceiling of
a real argument that is not whole is one above its floor. Sullivan and Mackey's Prob 1.5.6 asks
for `floor(x) + floor(1 - x)` and its kin "depending on x", and each comes out as one statement
for every whole `x` and one for every other real `x`, such as
`forall x in RR \ ZZ : floor(x) + floor(1 - x) = 0`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)). A member of a set builder
`{ y in S : P }` is known to be in `S` and to satisfy `P` there, and a body that comes out `True`
holds for every member of any set, and one that comes out `False` for none, where it was left as
written when the set's members could not be read. The nodes are taken componentwise on the
complex plane, so the last rule asks for a real argument: at `x = i/2` the ceiling is `i` and the
floor `0`. Outside a quantifier nothing is assumed, and `floor(x + 1)` is left as it was. The cases
may be written as a disjunction, `P or not x in ZZ`, which is read as `x in ZZ implies P`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall x in ZZ : floor(x) + floor(1 - x) = 1` | `UnhandledParseException` (quantifiers are new since; left as written when they arrived) | `True` |
| `forall x in RR : not x in ZZ implies floor(x) + ceil(x) = 2 floor(x) + 1` | `UnhandledParseException` | `True` |
| `forall x in RR \ ZZ : floor(x) + floor(1 - x) = 0` | `UnhandledParseException` | `True` |
| `forall x in { x in RR : sin(x) > 0 } : True` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |
| `exists x in A \/ B : False` | `UnhandledParseException` (left as written when quantifiers arrived) | `False` |
| `forall x in RR : floor(x) + ceil(x) = 2 x or not x in ZZ` | `UnhandledParseException` | `True` |

### A matrix compiles

**Addition, not silent.** Under the LINQ compiler a `Matrix` node compiles to a
`GenTensor<T, TWrapper>` over its elements' type, with the wrappers `DoubleOperations`,
`SingleOperations`, `ComplexOperations`, `Int32Operations`, `Int64Operations` and
`BigIntegerOperations` in `AngouriMath.Core.Compilation.IntoLinq`, and a sum, difference, product,
quotient or whole power with a matrix in it to GenericTensor's operation of that name
([#526](https://github.com/asc-community/AngouriMath/issues/526)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"[[x, 2 x], [x + 1, x^2]]".ToEntity().Compile<double, GenTensor<double, DoubleOperations>>("x")` | `UncompilableNodeException` | a delegate; at `3` the matrix `[[3, 6], [4, 9]]` |
| `"A * B".ToEntity().Compile<GenTensor<…>, GenTensor<…>, GenTensor<…>>("A", "B")` | `UncompilableNodeException` | the matrix product |
| `"[[1, 0]] * [[a, b], [c, d]] * [[0], [1]]".ToEntity().Compile<double, double, double, double, double>("a", "b", "c", "d")` | `2` at `(1, 2, 3, 4)` — simplified to the scalar first | unchanged |
### A product is written letters before functions

`Simplify` wrote `y * sin(x)` as `sin(x) * y` and `x * e^x` as `e ^ x * x`: a product came out in
the order the simplifier sorts by, which puts a sine before every letter. By hand the number comes
first, then the letters and their powers, then the functions: DLMF 5.5.1 is `Γ(z + 1) = z Γ(z)`,
and ISO 80000-2 lets a function's argument go without parentheses (`sin nπ`), so a letter written
after a function reads as part of its argument. The answer of `Simplify` is now written that way:
the number, the letters and their powers, then everything else -- a function, an exponential such
as `e^x` or `2^n`, a sum -- each kind in the order it had, with a leading number taken into a
quotient over a number (`2 / s`, not `2 * 1 / s`). Only the answer is reordered: the simplifier
still sorts a sine before a letter, because the rules that combine factors meet them as neighbours
in that order, and `InnerSimplified` and `Differentiate` return that order as before.
[#1628](https://github.com/asc-community/AngouriMath/issues/1628), Algebrite's issue 29 from
[#180](https://github.com/asc-community/AngouriMath/issues/180). Both columns measured on a build,
`v2.5.0` against this change.

| `"….".ToEntity().Simplify()` of | Was (2.5.0) | Is |
|---|---|---|
| `sin(x) * y` | `sin(x) * y` | `y * sin(x)` |
| `x * e ^ x` | `e ^ x * x` | `x * e ^ x` |
| `2 * sin(x) * y` | `2 * sin(x) * y` | `2 * y * sin(x)` |
| `a / sin(x)` | `csc(x) * a` | `a * csc(x)` |
| `a * log(x) + y * sin(x)` | `sin(x) * y + a * log(10, x)` | `y * sin(x) + a * log(10, x)` |
| `2 * x * sin(x) * cos(x)` | `sin(2 * x) * x` | `x * sin(2 * x)` |
| `derivative(arcsin(2 * x), x)` | `1 / sqrt(1 - (2 * x) ^ 2) * 2` | `2 / sqrt(1 - (2 * x) ^ 2)` |

### `π` is `pi` and the script `ℯ` is `e`

`π` (U+03C0) was a free variable, a Greek letter like any other, so `sin(π)` did not simplify; the
script `ℯ` (U+212F) was not a letter and did not parse. Both are now spellings of the constants, as
mathematics writes them: `π` is `pi`, one object with it, and `ℯ` is `e`. A caller who used `π` as a
variable's name now has the constant. The Cyrillic `е` and `х` stay letters, variables that print
like `e` and `x` ([#1260](https://github.com/asc-community/AngouriMath/issues/1260)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sin(π)".ToEntity().Simplify()` | `sin(π)` | `0` |
| `"cos(π / 3)".ToEntity().Simplify()` | `cos(π / 3)` | `1/2` |
| `"ln(ℯ)".ToEntity().Simplify()` | `UnhandledParseException` | `1` |

### `lcm` is a function, and a congruence is solved to a residue class

`lcm(a, b, ...)` is a node, `Entity.Lcmf`, by the conventions of `gcd`: non-negative, `0` where
either argument is `0`, `lcm(a/b, c/d) = lcm(a, c) / gcd(b, d)`, and left as written for symbols.
It was refused by name. A linear congruence in one unknown is solved by `Solve` to the residue
class `{ x in ZZ : x = r (mod n) }`, two classes meet by the Chinese remainder theorem — the
non-coprime case included — and a class cut down by a bounded interval is the finite set of its
members; `MathS.NumberTheory.ModularInverse(a, n)` is the representative in `[1, n - 1]` or
`null`. A quantified statement over the whole numbers whose body is a divisibility or a congruence
of polynomials is decided by its residues, and a polynomial equation with no solution modulo a
small `m` is decided to have none ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).
Every spelling but `lcm` was a parse error in 2.5.0, the congruence node being new since.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `lcm(4, 6)` | `UnrecognizedFunctionParseException` | `12` |
| `lcm(1/2, 1/3)`, `lcm(0, 5)`, `lcm(x, y)` | `UnrecognizedFunctionParseException` | `1`, `0`, `lcm(x, y)` |
| `"3 x = 11 (mod 7)".ToEntity().Solve("x")` | `UnhandledParseException` | `{ x in ZZ : x = 6 (mod 7) }` |
| `"x = 1 (mod 2) and x = 1 (mod 3) and x = 2 (mod 5) and 250 <= x and x <= 300".ToEntity().Solve("x")` | `UnhandledParseException` | `{ 277 }` |
| `"x = 3 (mod 4) and x = 2 (mod 6)".ToEntity().Solve("x")` | `UnhandledParseException` | `{}` |
| `forall n in ZZ : 6 divides n^3 + 5 n` | `UnhandledParseException` | `True` |
| `exists x, y in ZZ : 3 x^2 - 5 y^2 = 1` | `UnhandledParseException` | `False` |

### Floors and ceilings of `n` over a whole number are decided over its residues

A comparison between floors and ceilings of linear functions of `n` and a linear part in `n`
repeats with the least common multiple of their denominators where the drifts cancel: over a
period, a floor of `(p/q) n + b` rises by as much as the linear terms do. The residues then decide
it, as they decide a divisibility. `forall n in ZZ : floor(n/2) + ceil(n/2) = n` and Hermite's
`floor(n/3) + floor((n + 1)/3) + floor((n + 2)/3) = n` are `True`, where they were left as written
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall n in ZZ : floor(n/2) + ceil(n/2) = n` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |
| `exists n in ZZ : floor(n/2) + ceil(n/2) = n + 1` | `UnhandledParseException` (left as written when quantifiers arrived) | `False` |

### A sum of whole multiples is decided: `exists a, b in ZZ* : n = p a + q b`

What an equation `n = p a + q b` leaves of `b` is a divisibility and a sign: `(n - p a)/q in ZZ*` is
`q divides n - p a and n - p a >= 0`, for a whole `n - p a`. A statement that repeats in `a` and only
gets harder as `a` grows has its least witness below the period, so `exists a in ZZ*` of it is one of
the first period's members; and a statement whose comparisons settle past a threshold, with the rest
repeating, is decided by the members up to one period past it. So Sullivan and Mackey's coins and
special lands are decided (Prob 2.7.8, Prob 5.7.4, §5.5.3 Try 3): every `n` from `14` is `3 a + 8 b`
and `13` is not, every `n` from `24` is `4 a + 9 b` and `23` is not
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `exists a in ZZ* : exists b in ZZ* : 13 = 3 a + 8 b` | `UnhandledParseException` (left as written when quantifiers arrived) | `False` |
| `forall n in ZZ : n >= 14 implies (exists a in ZZ* : exists b in ZZ* : n = 3 a + 8 b)` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |

### An equation in a remainder is solved to its whole family

`f mod a = r` has one solution per period, and the solver had no way to write that. On 2.5.0
`(x mod 4) + 1 = 3` answered `{ }` -- no solution, for an equation with infinitely many. Where
the numeric fallback reached it, it answered with the few solutions it found in its window:
`(2x + 1) mod 4 = 3` was `{ -5, -3, -1, 1, 3, 5 }`, and 7 is a solution as well. The remainder
is now inverted the way the trigonometric functions are, with a whole parameter: `f = r + a n`
where `r` is a value the floored remainder takes -- `[0, a)` for a positive `a`, `(a, 0]` for a
negative one -- and no solution where it is not. A divisor or value that is not a number is left
unsolved.
[#1629](https://github.com/asc-community/AngouriMath/issues/1629), Algebrite's issue 87 from
[#180](https://github.com/asc-community/AngouriMath/issues/180). Both columns measured on a build,
`v2.5.0` against this change.

| `"….".ToEntity().Solve("x")` of | Was (2.5.0) | Is |
|---|---|---|
| `(x mod 4) + 1 = 3` | `{ }` | `{ 2 + 4 * n_1 }` |
| `(2x + 1) mod 4 = 3` | `{ -5, -3, -1, 1, 3, 5 }`, six of infinitely many | `{ (3 + 4 * n_1 - 1) / 2 }`, every odd number |
| `x mod (-4) = -1` | `{ }` | `{ -1 + 4 * n_1 }` |
| `x mod 4 = 5` | `{ }` | `{ }` (unchanged: modulo 4 the remainder never takes 5) |

### The modulus of a whole number is a whole number

With the facts in scope making `s` a whole number, `|s| in ZZ` and `|s| in ZZ*` are `True`, and
`|s| in ZZ+` is `not s = 0`, which the quantifiers then decide. So Sullivan and Mackey's Ex 7.2.6,
where `f(z) = |2 z + 1|` is a function from `ZZ` to their `N`, is `True`, since `2 z + 1 = 0` has no
whole root, while `|z|` fails at `0` and `|z^2 - 4|` at `2`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall z in ZZ : abs(2 z + 1) in ZZ+` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |
| `forall z in ZZ : abs(z - 3) in ZZ*` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |

### An antiderivative built through an even root says where the root is real

The linear-radical substitution, `u = (a x + b)^(1/q)`, and the general substitution for an even
root both take a power of `u` out of a radical on the strength of `u` being a non-negative real —
`sqrt(u^2 (1 + u))` as `u sqrt(1 + u)` — which it is only where the radical is. Beyond the
radicand's zero `u` is imaginary, and the integrand can still be real there: Rubi's
`x sqrt(c - a c x) / e^(3 atanh(a x))` above `a x = 1` is the product of two imaginary factors,
and the answer built for a real `u` is not its antiderivative — 14% off at `x = 0.59` with
`a = 3.1`, `c = 0.7`, and it shipped as an answer. An answer that used the step now carries the
condition, `provided a x + b >= 0`: `NaN` beyond it, which is no claim, and unchanged where it
holds. An answer the step did not touch — `x/sqrt(1 + x)`, `1/(sqrt(x) - x^(-1/3))` — is as it
was ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x * sqrt(c - a*c*x) / e^(3 * atanh(a*x))".Integrate("x")` | `integral(…)` — left unevaluated, with `e` evaluated to a hundred digits inside it | an antiderivative in `sqrt(c - a c x)`, `provided c - a * c * x >= 0` |
| `"1/sqrt(x + x^(3/2))".Integrate("x")` | `integral(1 / sqrt(x + x ^ (3/2)), x)` — left unevaluated | `2 * sqrt(1 + sqrt(x)) / (1/2) + C provided x >= 0` |

### An answer that read the sign of an even root says where the root is real

The general substitution for an even root, `u = t^(1/q)`, writes a sign or a modulus of `u` that a
rule below it put in as the root itself, `sgn(u) = 1` and `|u| = u`, which is so only where the
root is real. Beyond the radicand's zero `u` is imaginary, and the integrand can still be real
there, a product of two imaginary factors: `(1 + sec(x))^(5/2) sqrt(cos(x))` under `t = cos(x)` and
`u = sqrt(t)` has its power of a quotient taken apart as `(1 + u^2)^(5/2)/|u|^5`, and its answer
was not the integrand's antiderivative wherever the cosine is negative. An answer that had a sign
or a modulus of `u` now carries `provided t >= 0`, as the entry above conditions the factoring.
Rubi's 4.5.3.1:640 was wrong on that region
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(1+sec(x))^(5/2)*sqrt(cos(x))".Integrate("x")` | `integral((1 + sec(x)) ^ (5/2) * sqrt(cos(x)), x)` — left unevaluated | an antiderivative with `sgn(sin(x))` in it, `provided cos(x) >= 0` |

### An answer that took an even root real without reading its sign says where it holds

The same step's answer is checked where the rules below read no sign of `u` and still took it real:
`SolveByCombiningRadicals` writes `sqrt(1 + 1/u^2) sqrt(1 - u^4)` as
`sqrt(u^4 - u^8 + u^2 - u^6)/u^2`, which reads `sqrt(u^4)` as `u^2`, the other root wherever `u` is
imaginary. Where the integrand holds a second even root, as it must to be real where the radicand
is negative, the answer is differentiated and compared with the integrand where the integrand is
real. Where it is not the antiderivative there, it now carries `provided t >= 0` as well.
`sqrt(cos(x))/sqrt(a + a sec(x))` and six of its kin in Rubi's 4.5.1.2 and 4.5.3.1 were answered
negated wherever the cosine is negative; the half-angle rule above answers those seven first now,
and this is the same answer reached through the even root
([#1581](https://github.com/asc-community/AngouriMath/issues/1581)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(x)/(sqrt(1/x + 1)*sqrt(1 - x^2))".Integrate("x")` | `integral(...)` | an antiderivative in `sqrt(x)`, `provided x >= 0` |

### A product of powers of quotients of linears is written over each linear once

**Improvement, not silent.** `e^atanh(a x) sqrt(c - c/(a x))` is
`sqrt((1 + a x)/(1 - a x)) sqrt(c (a x - 1)/(a x))`, in which the linear `1 - a x` stands twice, under
two roots, and the substitution search took one root at a time and ran out of time. The integrand is
now written over each linear once, `K sqrt(a x + 1)/sqrt(x)`, where `K` is the integrand over that
product: its logarithmic derivative is zero, so it is constant on every interval where it is defined,
and it stands in front of the answer as Rubi's does. Each sign the linears take where the integrand is
real is an arm of a piecewise, saying where it holds. Rubi's 7.3.6 and 7.4.2
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^atanh(a*x)*sqrt(c-c/(a*x))".Integrate("x")` | left unevaluated | the antiderivative, a piecewise with an arm for each interval where it is real |
| `"1/(e^atanh(a*x)*(c-c/(a*x))^(3/2))".Integrate("x")` | left unevaluated | the same |
| `"e^(2*acoth(a*x))/(c-c/(a^2*x^2))^2".Integrate("x")` | left unevaluated | `K` times the antiderivative of `x^4/((a x + 1)(a x - 1)^3)` |
| `"x*sqrt(c - a*c*x)/e^(3*atanh(a*x))".Integrate("x")` | left unevaluated | the antiderivative on both sides of `a x = 1`, where `master` gave one `provided c - a c x >= 0` |
| `"sqrt(x)/(sqrt(1/x + 1)*sqrt(1 - x^2))".Integrate("x")` | left unevaluated | the antiderivative on both sides of `x = 0`, where `master` gave one `provided x >= 0` |

### A quadratic that shares a root with a linear beside it is written over it

**Answers where there were none.** In Rubi's 1.2.1.2 and 1.2.1.4 a quadratic under a root stands
beside a power of a linear that divides it: `a d h + (c d^2 + a h^2) x + c d h x^2` is
`(d + h x)(a h + c d x)`. Nothing split a quadratic with symbols in it over a root it shares, so it
was read as irreducible and the integral was declined or ran out of time. A quadratic beside a
linear under a root now takes the linear's root where it vanishes there, and its other root by
the sum of the two. Each linear is then written once, as in the entry above
([#718](https://github.com/asc-community/AngouriMath/issues/718)), and the two roots of the shared
linear are one whole power of it. That is done only where it leaves two linears or fewer. Beside a
whole power of the linear, as in `(d + h x)^3 sqrt(a d h + ...)`, or beside a third linear, as in
`x sqrt(a d h + ...)/(d + h x)`, the quadratic is read whole, as before.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(d + h*x)^(7/2)/(a*d*h + (c*d^2 + a*h^2)*x + c*d*h*x^2)^(3/2)".Integrate("x")` | left unevaluated | `K` times a sum of powers of `sqrt(a h + c d x)`, `provided a h + c d x >= 0` |
| `"(d + h*x)^(3/2)*(a*d*h + (c*d^2 + a*h^2)*x + c*d*h*x^2)^(1/2)".Integrate("x")` | left unevaluated | `K` times a polynomial in `sqrt(a h + c d x)`, `provided a h + c d x >= 0` |
| `"sqrt(d + h*x)/sqrt(a*d*h + (c*d^2 + a*h^2)*x + c*d*h*x^2)".Integrate("x")` | left unevaluated | `K` times `2 sqrt(a h + c d x)/(c d)`, `provided a h + c d x >= 0` |

### The inverse hyperbolic tangent's answer is given on its domain, and says so

`1/((1 - a^2 x^2)^(5/2) artanh(a x))` was left unevaluated, although the substitution `a x = tanh(u)`
answers it: `(Chi(3 u) + 3 Chi(u))/(4 a)` for `u = artanh(a x)`. The answer was checked past
`|a x| < 1` as well, where `sqrt(1 - a^2 x^2)` is not `sech(u)`, and declined for failing there.
Past `a x = 1` the integrand is not real either, so the answer is now given on the domain and
carries `provided 1 - (a x)^2 > 0`, as an answer through an even root says where the root is real.
Where the integrand is real past `a x = 1` too, through two imaginary factors cancelling, the
answer is still declined, and a route that answers everywhere can. Rubi's 7.3.4
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/((1-a^2*x^2)^(5/2)*atanh(a*x))".ToEntity().Integrate("x")` | `integral(...)` | `(Chi(3 u) + 3 Chi(u))/(4 a) provided 1 - (a x)^2 > 0`, for `u = artanh(a x)` |
| `"1/((1-a^2*x^2)^(3/2)*atanh(a*x))".ToEntity().Integrate("x")` | `integral(...)` | `Chi(u)/a provided 1 - (a x)^2 > 0` |
| `"x/((1-a^2*x^2)^(3/2)*atanh(a*x)^2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `Chi`, `provided 1 - (a x)^2 > 0` |
| `"x*atanh(a*x)^3/(1-a^2*x^2)^(3/2)".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative, `provided 1 - (a x)^2 > 0` |

### `e^atanh(a x)` beside a half-integer power of a linear is integrated where its answer holds

**Improvement, not silent.** `e^atanh(a x) (c - a c x)^p` for a half-integer `p` was left
unevaluated. Past `a x = 1` both factors are imaginary and their product is real, and the answers
the rules give have the other sign there, so each now says where it holds rather than claiming the
whole line. Rubi's 7.3.6 ([#1655](https://github.com/asc-community/AngouriMath/issues/1655),
[#1664](https://github.com/asc-community/AngouriMath/issues/1664)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^atanh(a*x)*sqrt(c - a*c*x)".Integrate("x")` | `integral(...)` | `2 (c (1 + a x))^(3/2)/(3 a c) provided c - a c x >= 0`, up to the form |
| `"e^atanh(x)*sqrt(1 - x)".Integrate("x")` | `integral(...)` | `(1 + x)^(3/2)/(3/2) provided (1 + x)/(1 - x) >= 0` |
| `"e^atanh(x)/sqrt(1 - x)".Integrate("x")` | `integral(...)` | an antiderivative, `provided (1 + x)/(1 - x) >= 0` |

### A logarithm of `e^x` named as a linear is asked about where the name is zero

**Improvement, not silent.** `1/(x ln(e^x))` was left unevaluated. The integrator names `ln(e^x)`
as `x + c` and answers the generic case, which divides by `c`; on the real line `ln(e^x)` is `x`,
so `c` is zero there. The case `c = 0` is now asked as well, and is the answer's first arm. Rubi's
3.5 and 7.3.7 ([#1666](https://github.com/asc-community/AngouriMath/issues/1666)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x*ln(e^x))".Integrate("x")` | `integral(...)` | `-1/x` where `ln(e^x) - x = 0`, the generic answer elsewhere |
| `"cot(x)/ln(e^sin(x))".Integrate("x")` | `integral(...)` | `-1/sin(x)` where `ln(e^sin(x)) - sin(x) = 0`, the generic answer elsewhere |
| `"1/atanh(tanh(a + b*x))".Integrate("x")` | no answer in 30 s | `ln(2 (a + b x))/b` where the name is zero, the generic answer elsewhere |

### A zero imaginary part is on the real axis, whatever its sign

**Silent, with the downcasting off.** A decimal's negative zero — `0E-100 * -0.43` is `-0E-102`
— is an artefact of the exponent arithmetic and not a limit from below, but the phase read
`-pi` off its sign the way `Math.Atan2` does, and with `DowncastingEnabled` off, where
`Complex.Create` keeps `-1.54 - 0i` a `Complex`, a power went through the polar form on the
wrong side of the negative axis: `(-1.54)^(-1/2)` was `+0.80 i` with the setting off and
`-0.80 i` with it on. The setting decided a branch, and a numerical check that turns it off
called a right antiderivative wrong. Now a number on the negative axis has argument `pi` for
either zero, a complex with an exactly zero imaginary part takes the real paths of `Pow`, and
the square root's sign is read from a strictly negative imaginary part
([#1378](https://github.com/asc-community/AngouriMath/issues/1378)). Nothing changes with
the downcasting on.

| Input, under `MathS.Settings.DowncastingEnabled.As(false, …)` | Was (2.5.0) | Now |
|---|---|---|
| `"(2*1.44*(1/sqrt(-5.286))^2-1)^(-1/2)".ToEntity().Evaled` | `5.5500952471953097…` | `-0.804560841963042…i`, as with the downcasting on |
| `"((1/sqrt(-5.286))*(2*1.44*(1/sqrt(-5.286))^2-1)^(-1/2))^(-3)".ToEntity().Evaled` | `23.335336258472899…` | `-23.335336258472899…`, as with the downcasting on |
### `subset` is a statement between sets, sets are equal by double containment, and `powerset` is a function

`A subset B` — also `A ⊆ B`, and `B superset A` / `B ⊇ A` for the same node — is the statement,
`Entity.Set.Subsetf`, that every member of `A` is a member of `B`: the relation between sets that
`in` is between an element and a set. It is decided from the shapes of the two sets — member by
member for a listed set, along `ZZ+ ⊂ ZZ* ⊂ ZZ ⊂ QQ ⊂ RR ⊂ CC` for the special sets, by the ends for
two intervals, by the set algebra for symbolic sets, and through the solver for a set builder —
and is `NaN` with a number or a truth value on a side. **`=` between two sets is decided by
double containment**, so a pair of sets that are not the same node no longer stays written when
one is inside the other or one is not. `powerset(A)`, `Entity.Set.Powersetf`, is the set of all
subsets of `A`: listed for a finite `A`, and for an infinite one an object whose membership is the
subset test; `card` counts it as `2^card(A)`, and counts a listed set whose members are listed sets
too ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)). `subset`, `superset` and
`powerset` are keywords now, and were names.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `{1, 2} subset {1, 2, 3}` | `subset` was a variable, and the input a juxtaposition | `True` |
| `{ x in RR : x^2 = 5 } subset ZZ` | — | `False` |
| `A /\ B subset A` | — | `True` |
| `{ x in ZZ : x >= 1 } = ZZ` | `{ x in ZZ : x >= 1 } = ZZ` — left as written (`ZZ+` did not parse in 2.5.0; `= ZZ+` is `True` now) | `False` |
| `[0; 1] = [0; 2]` | `[0; 1] = [0; 2]` — left as written | `False` |
| `{1, 2} = {1, 2, 3}` | `{ True, False }` — the comparison distributed over the members | `False` |
| `{1, x} = {1, 2}` | `{ True, False, x = 1, x = 2 }` | `{ 1, x } = { 1, 2 }` — one statement, left as written |
| `powerset({1, 2})` | `UnrecognizedFunctionParseException` | `{ {  }, { 2 }, { 1 }, { 1, 2 } }` |
| `{1, 3} in powerset(ZZ)` | `UnrecognizedFunctionParseException` | `True` |
| `card({1, {}})` | `#{ 1, {  } }` — left as written | `2` |

### An interval meets a union of any number of pieces

An interval met a union piece by piece only where the union had two pieces, each an interval or
a listed set. The solution set of `x^2 - 3 x + 2 >= 0` comes out in three,
`{ 2 } \/ (-oo; 1] \/ (2; +oo)`, so its meeting with `(1; 2)` was left as written, and the two set
builders of Sullivan and Mackey's §3 Try 8 were not found equal. Any union built of such pieces is
met piece by piece now ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `{ x in RR : x^2 - 3 x + 2 >= 0 } = { y in RR : y <= 1 or y >= 2 }` | left as written | `True` |
| `"({ 2 } \/ (-oo; 1] \/ (2; +oo)) /\ (1; 2)".ToEntity().Evaled` | left as written | `{ }` |

### Two unions of intervals meet piece by piece

A union of intervals and listed sets met an interval piece by piece, and another such union not
at all: the meeting was left as written. That is what a conjunction of two inequalities is solved
to, so a statement decided through one was left as written as well. Two such unions now meet
piece by piece. So Sullivan and Mackey's Prob 4.11.22 is decided both ways: for positive `x`,
`(x^2 - 4)/(x^2 + 1) > 1 - 1/x` implies `(x^2 + 4)/(x^2 + 1) < 1 + 1/x`, and the converse fails at
`3` ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"((0; 1) \/ (3; 5)) /\ ((-1; 2) \/ (4; 6))".ToEntity().Evaled` | left as written | `(0; 1) \/ (4; 5)` |
| `forall x in RR : x > 0 implies ((x^2 - 4)/(x^2 + 1) > 1 - 1/x implies (x^2 + 4)/(x^2 + 1) < 1 + 1/x)` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |

### A union of numbers is written as its pieces

A union of numeric intervals and listed real numbers is written as its disjoint pieces: the listed
numbers first, then the intervals in increasing order, a point at an open end closing it and an
empty interval dropped. Pieces met pairwise before, and only when they were neighbours, so the
inequality solver's answer to `x^2 >= x` kept its roots beside the open intervals they close. Two
ways of writing one set of numbers now come out the same
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2 >= x".Solve("x")` | `{ 0, 1 } \/ (-oo; 0) \/ (1; +oo)` | `(-oo; 0] \/ [1; +oo)` |
| `"(0; 2) \/ ((1; 3) \/ (5; 6))".ToEntity().Evaled` | `(0; 2) \/ (1; 3) \/ (5; 6)` | `(0; 3) \/ (5; 6)` |

### The size of an infinite set is an aleph, and sizes compare

`card` of an infinite set is an aleph, `aleph(k)`, or a power of 2 of one: `card(ZZ)`, `card(QQ)`
and `card(PP)` are `aleph(0)`, and `card(RR)` is `2^aleph(0)`, an aleph but not a named one, since
which one it is is the continuum hypothesis, which ZFC does not decide: `2^aleph(0) = aleph(1)`
stays as written. `card(powerset(S))` is `2^card(S)`. A sum or a product of sizes, one of them
infinite, is the larger, and a difference or a quotient of sizes has no value and is `NaN`, as
`log(0)` is. Sizes compare by what ZFC proves, and a set is smaller than its power set whatever it
is ([#1409](https://github.com/asc-community/AngouriMath/issues/1409), Sullivan and Mackey's §7.6).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"card(ZZ)".ToEntity().Simplify()` | `#ZZ` | `aleph(0)` |
| `"card(ZZ) = card(QQ)".ToEntity().Simplify()` | `#ZZ = #QQ` | `True` |
| `"card(ZZ) < card(RR)".ToEntity().Simplify()` | `#ZZ < #RR` | `True` |
| `"aleph(0) + 1".ToEntity().Simplify()` | `1` — `aleph(0)` was read as `aleph * 0` | `aleph(0)` |

### An integer range is listed, and a family of sets has a union and an intersection

`ZZ /\ [1; 10]` lists its members now — an integer set cut by a numeric interval with at most
4096 members in it is the finite set of them, the `[n] = {1, …, n}` of combinatorics being
`ZZ+ /\ [1; n]`; the reals and the complex numbers cut by an interval are the interval, and the
rationals stay as written. `union(A_k, k in I)` and `intersection(A_k, k in I)` are
`Entity.Set.IndexedUnionf` and `Entity.Set.IndexedIntersectionf`, binders like `sum`: folded over
a listed index set, and over any other an object whose membership is decided by the quantifiers
(`0 in intersection([0; 1/n), n in ZZ+)` is `True`). `complement(A, U)` is `U \ A`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)). `union`, `intersection` and
`complement` are keywords now, and were names.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `ZZ /\ [1; 10]` | `ZZ /\ [1; 10]` — left as written | `{ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }` |
| `RR /\ [0; 1]` | `RR /\ [0; 1]` — left as written | `[0; 1]` |
| `union({k, 2 k}, k in {1, 2, 3})` | `UnhandledParseException` — `union` was a name, and `union(` a juxtaposition that does not parse | `{ 1, 2, 4, 3, 6 }` |
| `0.1 in intersection([0; 1/n), n in ZZ+)` | `UnhandledParseException` | `False` |
| `complement({1, 2}, {1, 2, 3})` | `UnhandledParseException` | `{ 3 }` |

### A set builder over a set of numbers is listed where that takes no search

`{ x in S : p }` was left as written unless `p` came out a truth value, or was a membership
`f(x) in Y`. Now it is listed, or written as intervals, where that takes no search: a listed `S` is
filtered member by member, and over the reals, an interval or a set of whole numbers, `S` is met with
the solutions of `p` where `p` compares rational functions of `x` of degree at most four, with numbers
for coefficients, or is an equation in moduli of linear functions. A set of whole numbers is met only
where that comes out listed, so `{ x in ZZ : x > 5 }` stays as written, as does anything with a
parameter or a function the solvers would have to search. Sullivan and Mackey's §3.3.3-4, §3.3.7 Q6,
Prop 3.9.6 and Prob 3.11.4 list as the book lists them
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"{ x in RR : x^2 - 2 = 0 }".Simplify()` | as written | `{ -sqrt(2), sqrt(2) }` |
| `"{ x : x in ZZ and x^2 < 10 }".Simplify()` | as written | `{ -3, -2, -1, 0, 1, 2, 3 }` |
| `"{ x in { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } : x >= 7 }".Simplify()` | as written | `{ 7, 8, 9, 10 }` |
| `"{ x in ZZ+ : x + 8/x <= 6 }".Simplify()` | `UnhandledParseException` (`ZZ+` is new since) | `{ 2, 3, 4 }` |

### An interval whose ends are reversed meets nothing

`(3; 1)` holds no number, and membership in it was decided so, but its meeting with another interval
was the other interval: the intersection returned it where the union rightly does. It is empty
now, and a closed interval at one point meets the other in that point or in nothing.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(3; 1) /\ [0; 5]".ToEntity().InnerSimplified` | `[0; 5]` — wrong | `{ }` |

### A power of an exponential over a polynomial is integrated to the exponential integral

**Answers where there were none.** `(F^(g (e + f x)))^n` with a symbol for `F` or `n` was read by no rule
below a polynomial: `(F^x)^n/x` and Rubi's 2.2, `(a + b (F^(g (e + f x)))^n)^p/(c + d x)^m`, were
declined. Its logarithmic derivative is the constant `n g f ln F`, so it is a constant times
`e^(n g f ln(F) x)` wherever it is differentiable, and the answer is the exponential integral's with
that constant written back as `(F^(g (e + f x)))^n e^(-n g f ln(F) x)`. It holds for every `F`: a
negative one makes `(F^x)^n` differ from `F^(n x)` past each point where `F^x` crosses the negative
axis, and the constant carries that. A whole power is `F^(n u)` exactly and is written so
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(F^x)^2/x".ToEntity().Integrate("x")` | `integral(...)` | `Ei(2 ln(F) x)` |
| `"(F^x)^n/x".ToEntity().Integrate("x")` | `integral(...)` | `(F^x)^n e^(-n ln(F) x) Ei(n ln(F) x)` |
| `"(a + b*(F^(g*(e + f*x)))^n)/(c + d*x)".ToEntity().Integrate("x")` | `integral(...)` | a logarithm and an exponential integral |
| `"(a + b*(F^(g*(e + f*x)))^n)^3/(c + d*x)^3".ToEntity().Integrate("x")` | `integral(...)` | powers of `c + d x` and exponential integrals |

### An exponential of a multiple of a logarithm is integrated as the power it is

`e^(k ln(q))` is `q^k` — the definition of the principal power, for every complex `q` other than
zero — and that is the spelling the parser gives every inverse hyperbolic function: `acoth(a x)`
is `1/2 ln((a x + 1)/(a x - 1))`. So `e^acoth(a x) x^3` arrived at the integrator as an
exponential of a logarithm, which no exponential rule reads, and is `x^3 sqrt((a x + 1)/(a x - 1))`,
which the radical substitution answers. The integrand is not simplified before the rules see it
(the simplifier has folded this shape since #1430), so the fold is a route of the integrator now,
with the multiplier gathered from the whole product in the exponent
([#718](https://github.com/asc-community/AngouriMath/issues/718), Rubi's 7.4.2).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(2*acoth(2*x))*(3 - 12*x^2)^2".Integrate("x")` | `integral(…)` — left unevaluated | `(144 * x ^ 5 / 5 + 144 * x ^ 4 / 4 + (-36) * x ^ 2 / 2 + (-9) * x provided not 2 * x + -1 = 0) + C` |
| `"e^(1/3*acoth(x))*x^2".Integrate("x")` | `integral(…)` — left unevaluated | an antiderivative in `((x + 1)/(x - 1))^(1/6)` |

### `Solve` no longer answers the empty set for a statement it cannot read

**Wrong answer fixed.** `"x^2 in (0; 1)".Solve("x")` was `{}` — and so was every `f(x) in S` with
anything but a bare `x` on the left, and every statement the solver had no arm for: the arm at the
end of the dispatch answered the empty set, which claims there is no such `x`. A membership is
solved now — the members of a listed set each as an equation, an interval as its bounds, each
strict or not as the end is — and a statement that is not read is the set of `x` with the
property, left as written. A quantifier that put the negated body to the solver read that
emptiness as a proof: `forall x in RR : x^2 in ZZ` was `True`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409), Sullivan and Mackey's pre-images).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2 in (0; 1)".Solve("x")` | `{}` — wrong | `(-1; 0) \/ (0; 1)` |
| `"x^2 in {1, 4}".Solve("x")` | `{}` — wrong | `{ 1, -1, 2, -2 }` |
| `"x^2 in ZZ".Solve("x")` | `{}` — wrong | `{ x : x ^ 2 in ZZ }`, left as written |
| `forall x in RR : x^2 in ZZ` | `UnhandledParseException` (quantifiers are new since) | `False` |
### An equation between moduli is left unsolved over the complex numbers, and solved by cases over the reals

An expression real for every complex `x`, with `x` only ever inside a modulus, is one real condition
on the two real unknowns `x` is made of, so what solves it is a curve: `abs(x - 2) = abs(x - 3)` holds
on the whole line `Re x = 5/2`. **Wrong answer fixed**: Newton's method found points of such a curve
and answered them as the solution set, and `abs(x - 1) + abs(x - 2) = 5` missed its real root `4`
for points of an ellipse. It is now left as the set of the `x` satisfying it. With the codomain set
to `RR`, an equation in moduli of linear functions is solved by cases between the kinks, where each
modulus has one sign: `abs(x - 2) = abs(x - 3)` is `{ 5/2 }` (Sullivan and Mackey's Prob 1.5.16), and
`abs(x - 1) + abs(x - 2) = 1` is `[1; 2]` ([#1573](https://github.com/asc-community/AngouriMath/issues/1573)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"abs(x - 2) = abs(x - 3)".ToEntity().Solve("x")` | `{ 2.4999… - 53.004…i, 2.4999… + 53.004…i, … }` | `{ x : abs(x - 2) = abs(x - 3) }` |
| the same, with `MathS.Settings.Codomain` set to `RR` | `{ 2.4999… - 53.004…i, 2.4999… + 53.004…i, … }` | `{ 5/2 }` |
| `"abs(x - 1) + abs(x - 2) = 5".ToEntity().Solve("x")`, codomain `RR` | `{ -1, 3.9997… - 0.0345…i, 3.9997… + 0.0345…i, … }` | `{ -1, 4 }` |

### `...` is the pattern operator

`{1, 2, ..., n}`, `{2, 4, ..., 2 n}`, `{5, 10, 15, ...}`, `{..., -1, 0}`, `1 + 2 + ... + n` and
`1 * 2 * ... * n` parse — also with `…` — as the arithmetic progression of whole numbers the shown
terms determine, up to the term after the dots: a set is `ZZ /\ [a; z]` for step one and a residue
class cut by the interval otherwise, listed when the ends are numbers; a sum is `sum(k, k, 1, n)`
and a product `product(k, k, 1, n)`, with `a + d k` as the term for another step. Shown terms with
no common step, a progression that is not of whole numbers, and dots with nothing to determine
them are refused with a message saying which. The smaller half of
[#1437](https://github.com/asc-community/AngouriMath/issues/1437); the general term with an index
shown and the argument list of symbolic length wait for v3. Every one of these was a parse error.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `{1, 2, ..., 10}` | `UnhandledParseException` | `{ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }` |
| `{1, 2, ..., n}` | `UnhandledParseException` | `ZZ /\ [1; n]` |
| `7 in {1, 3, 5, ...}` | `UnhandledParseException` | `True` |
| `1 + 2 + ... + 100` | `UnhandledParseException` | `5050` |
| `1 * 2 * ... * n` | `UnhandledParseException` | `product(k, k, 1, n)`, which is `n!` for `n >= 1` |
| `{1, 4, 9, ..., n}` | `UnhandledParseException` | `InvalidArgumentParseException`, naming the term off the progression |

### A listed solution set is filtered by a decided condition

`"x^2 = 4 and not x = 2".Solve("x")` is `{ -2 }`; it was the conjunction left as written, because
#1036 taught the solver not to intersect a listed set with a condition a search had left open
(1 is a root of `x^6 + x y + 1 = 0` only when `y` is -2). The two are told apart now: a member
at which the condition is decided is kept or dropped, and the conjunction stays written the
moment one member is undecided, so #1036's case is as it was. Decided by evaluation, and by
simplification where evaluation leaves it open, so that `forall u in RR : forall v in RR :
2 u + 1 = 2 v + 1 implies u = v` — the injectivity of a linear map, Def 7.4.6 of the reference
of [#1409](https://github.com/asc-community/AngouriMath/issues/1409) — is `True`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2 = 4 and not x = 2".Solve("x")` | `{ x : x ^ 2 = 4 and not x = 2 }` | `{ -2 }` |
| `"x^2 = 4 and x > 0".Solve("x")` | `{ 2 }` | `{ 2 }` — unchanged, both sides being settled |
| `"x^6 + x*y + 1 = 0 and x - 1 = 0".Solve("x")` | `{ x : … }`, as written | unchanged |

### An undecided member of an intersection stays in it, and a conjunction keeps its second equation

**Wrong answers, silent.** A member of a listed set whose membership in the other set could not be
decided was kept in their intersection unconditionally: `{ y } /\ { 2 }` was `{ y }`, which is
wrong at every `y` but 2. `Solve` meets the solution sets of a conjunction's two sides, so an
equation with a parameter in it was dropped: `x = y and x = 2` solved to `{ y }`. Such a member now
stays intersected with the other set, beside the members that are decided
([#1680](https://github.com/asc-community/AngouriMath/issues/1680)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"{ y } /\ { 2 }".ToEntity().Evaled` | `{ y } \/ {  }` | `{ y } /\ { 2 }` |
| `"{ y, 3 } /\ { 3 }".ToEntity().Evaled` | `{ y } \/ { 3 }` | `{ 3 } \/ { y } /\ { 3 }` |
| `"x = y and x = 2".Solve("x")` | `{ y } \/ {  }`, which is `{ 1 }` at `y = 1` | `{ y } /\ { 2 }`, which is empty at `y = 1` |
| `"x + y = 3 and x - y = 1".Solve("x")` | `{ 3 - y } \/ {  }` | `{ 3 - y } /\ { 1 + y }`: `{ 2 }` at `y = 1`, and empty elsewhere |
| `"x^2 = 4 and x = y".Solve("x")` | `{ 2, -2 } \/ {  }` | `{ 2, -2 } /\ { y }` |
### `image` and `preimage` of a set under an expression

`image(f(x), x in A)` is `{ f(x) : x in A }` — `union({f(x)}, x in A)` — listed over a listed
`A`, an interval by interval arithmetic where `x` occurs once in `f`, intervals read off the
critical points and the one-sided limits where `f` is a quotient of polynomials with rational
coefficients and `A` is made of intervals (§7.3.5 Try 1's `x/(1 + x)` on `RR \ {-1}` is
`RR \ {1}`), and a membership object otherwise; `preimage(f(x), x in A, Y)` is
`{ x in A : f(x) in Y }`, and a set builder of that shape is solved on evaluation where the
membership is read (a listed `Y`, an interval)
([#1409](https://github.com/asc-community/AngouriMath/issues/1409), Sullivan and Mackey's §7.3). Both
names are keywords now, and were names.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `image(9 c / 5 + 32, c in (0; 100))` | `UnhandledParseException` | `(32; 212)` |
| `image(x^2/(1 + x^2), x in RR)` | `UnhandledParseException` | `[0; 1)` |
| `image(x/(1 + x), x in RR \ {-1})` | `UnhandledParseException` | `(-oo; 1) \/ (1; +oo)` |
| `preimage(x^2, x in RR, {1})` | `UnhandledParseException` | `{ 1, -1 }` |
| `{ x in ZZ : x^2 in {1, 4} }` | `{ x in ZZ : x ^ 2 in { 1, 4 } }` — left as written | `{ 1, -1, 2, -2 }` |

### A quotient of polynomials is solved as an inequality

`"x/(1 + x) > 1".Solve("x")` threw `NotSufficientlySupportedException`; it is `(-oo; -1)`. A
quotient `N/D > 0` holds exactly where `N D > 0`, since `D^2` is positive wherever the quotient is
defined and the product is zero at its poles, and the product is a polynomial the solver reads. So
`preimage(x/(1 + x), x in RR \ {-1}, (1; +oo))` is `(-oo; -1)`, where Sullivan and Mackey's
§7.3.5 Try 1 gives `(-1, oo)`. A pre-image whose inequality the solver cannot read, `sin(x) > 0`,
stays the set builder it is, where its evaluation threw. A set difference meets another set through
its minuend, `(A \ B) /\ C = (A /\ C) \ B`, and an interval meets a union of intervals piece by
piece. With both, a quantifier over the reals decides an implication between two quotients: §4.9.5's
`x > 0 implies ((x - 3)/(x + 2) > 1 - 1/x implies (x + 3)/(x + 2) < 1 + 1/x)` is `True`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x/(1 + x) > 1".Solve("x")` | `NotSufficientlySupportedException` | `(-oo; -1)` |
| `"(RR \ {-1}) /\ (-oo; -1)".ToEntity().Evaled` | `(RR \ { -1 }) /\ (-oo; -1)` | `(-oo; -1)` |
| `"preimage(sin(x), x in RR, (0; 1))".ToEntity().Evaled` | `UnhandledParseException`, and `NotSufficientlySupportedException` since `preimage` was added | the set builder, unsolved |

### An inequality in which `x` cancels holds everywhere or nowhere

`"x + 1 > x".Solve("x")` threw `InvalidCastException`, and so did every inequality whose `x`
cancels, `"3 > 1"` among them: the root of `0 x + 1` is not a number, and the interval from it
simplified to one. A quantifier that asked the solver threw with it. A slope or a leading
coefficient that is zero now answers the whole line or nothing.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x + 1 > x".Solve("x")` | `InvalidCastException` | `RR` |
| `"x^2 < x^2 - 2".Solve("x")` | `InvalidCastException` | `{ }` |
| `forall x in RR : x + 1 > x` | `UnhandledParseException` (`InvalidCastException` since quantifiers arrived) | `True` |

### A statement equal to a truth value is solved as the statement or its negation

**Wrong answer fixed.** `"(x > 2) = (3 > 1)".Solve("x")` was `{ }`, and so was every equation
between a statement and a truth value: the arm for an equation of numbers subtracted the two
sides, which claimed there was no root. A statement equal to `True` is the statement, equal to
`False` its negation, and two statements equal are equivalent
([#1549](https://github.com/asc-community/AngouriMath/issues/1549)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(x > 2) = (3 > 1)".Solve("x")` | `{ }` — wrong | `(2; +oo)` |
| `"(x = 1) = true".Solve("x")` | `{ }` — wrong | `{ 1 }` |
| `"(x > 0) = (x > 1)".Solve("x")` | `UncompilableNodeException` | `(1; +oo) \/ (-oo; 0]` |

### An infinite base has its whole powers, and surjectivity is decided through the image

`(-oo)^3` was `NaN` — a claim that the value does not exist — and so were `(-oo)^2` and
`(-oo)^(-1)`, while `(+oo)^2` was `+oo`: the finite-base arms did not apply and the polar form
had nothing to say. They are the extended reals' powers now, `-oo`, `+oo` and `0`, so the image of
`(-oo; +oo)` under a cube is `(-oo; +oo)`; and `forall b in B : exists a in A : f(a) = b` — surjectivity
onto `B`, Sullivan and Mackey's Def 7.4.1, [#1409](https://github.com/asc-community/AngouriMath/issues/1409)
— is decided as `B` lying in the image of `A` under `f` where that image evaluates.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `(-oo)^3` | `NaN` — wrong | `-oo` |
| `(-oo)^2` | `NaN` — wrong | `+oo` |
| `forall b in RR : exists a in RR : a^3 = b` | `UnhandledParseException` (quantifiers are new since; left as written when they arrived) | `True` |
| `forall b in RR : exists a in RR : e^a = b` | `UnhandledParseException` | `False` |

### One-to-one is decided for a quotient of polynomials on an interval

`forall a in S : forall b in S : f(a) = f(b) implies a = b` — `f` one-to-one on `S`, Sullivan and
Mackey's Def 7.4.1 — is decided for a quotient of polynomials on an interval by the sign of its
derivative between the zeros inside: strictly monotone is one-to-one, and a turn is not. The cube
is one-to-one on the reals although its derivative is zero at 0; the square is on `[0; +oo)`. On the
whole numbers only a yes carries over from the interval around them
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall a in RR : forall b in RR : a^3 = b^3 implies a = b` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |
| `forall a in [0; +oo) : forall b in [0; +oo) : a^2 = b^2 implies a = b` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |
| `forall a in RR : forall b in RR : a^3 - 3 a = b^3 - 3 b implies a = b` | `UnhandledParseException` | `False` |

### A family of intervals with monotone ends is the interval between their extremes

`union(A_n, n in I)` and `intersection(A_n, n in I)` of intervals whose ends are quotients of
polynomials in the index, monotone over its range, are the interval between the extremes of the
ends: an intersection always, a union where the family is a chain or the index runs over an
interval. An end is closed where the family's ends are and the extreme is reached, or, in an
intersection, where it is only approached. Sullivan and Mackey's §3.9.5 Try 8–10: the intersection
of `(-1/n; 1/n)` over `ZZ+` is `{ 0 }`, the union of `(x; x + 1)` over `(0; 1)` is `(0; 2)`, and
the union of `[0; (n - 1)/n)` is `[0; 1)`, which is the intersection of `(-1/n; 1)`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `intersection((-1/n; 1/n), n in ZZ+)` | `UnhandledParseException` (left as written when families arrived) | `{ 0 }` |
| `union((x; x + 1), x in (0; 1))` | `UnhandledParseException` (left as written when families arrived) | `(0; 2)` |
| `union([0; (n - 1)/n), n in ZZ+) = intersection((-1/n; 1), n in ZZ+)` | `UnhandledParseException` | `True` |

### The binomial coefficient's identities, and its sums in closed form

Pascal's rule, the chairperson identity and the symmetry are rewrite rules, each in the direction
that collects (`binomial(n - 1, k) + binomial(n - 1, k - 1)` → `binomial(n, k)`,
`n binomial(n - 1, k - 1)` → `k binomial(n, k)`, `binomial(n, n - k)` → `binomial(n, k)`), so the
identities are `True` as statements of symbols; a sum written with `binomial(n, k)` is the binomial
theorem read backwards, as the factorial spelling already was (`sum(binomial(n, k) x^k y^(n - k), k, 0, n)`
is `(x + y)^n` for `n >= 0`). **Wrong answer fixed** in the alternating sum, in both spellings:
`sum(binomial(n, k) (-1)^k, k, 0, n)` is `1` at `n = 0` and `0` above, and the closed form `0^n`
carried a condition the piecewise read as no value, so the sum was `0` at `n = 0`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409), Sullivan and Mackey's §8.4).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"binomial(n, k) = binomial(n, n - k)".ToEntity().Simplify()` | `UnhandledParseException` — `binomial` is new since, and was left as written when it arrived | `True` |
| `"binomial(n - 1, k) + binomial(n - 1, k - 1)".ToEntity().Simplify()` | `UnhandledParseException` | `binomial(n, k)` |
| `"sum(binomial(n, k), k, 0, n)".ToEntity().Evaled` | `UnhandledParseException` | `piecewise((2 ^ n) provided (n >= 0), 0 provided True)` |
| `"sum(n! / (k! (n - k)!) (-1)^k, k, 0, n)".ToEntity().Evaled` | `piecewise(0 provided True)` — wrong at `n = 0` | `piecewise(1 provided (n = 0), 0 provided True)` |

### `iverson(P)` is the Iverson bracket, and was a product

`iverson(P)` is `1` where the statement `P` holds and `0` where it does not, and is kept as written
where `P` is not decided, so a count is a sum of brackets: `sum(iverson(k divides 12), k, 1, 12)` is
`6`. A number is not a statement and is left as written; a statement with no truth value, such as
`i > 0`, gives `NaN`. It prints as `[\![P]\!]` in LaTeX and as a `Piecewise` for SymPy
([#1478](https://github.com/asc-community/AngouriMath/issues/1478)). In 2.5.0 the name was read as a
variable multiplying its argument, and a product with a truth value is `NaN`.

A sum of a bracket is counted in closed form, however long the range, where the statement bounds the
index, fixes it at a point, fixes its residue or makes it a perfect power, with `not` and `or` counted by inclusion and
exclusion: the number of whole numbers from `L` to `U` in the class `r` modulo `m` is
`floor((U - r)/m) - floor((L - 1 - r)/m)`, and never below zero.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"iverson(2 > 1)".ToEntity().Simplify()` | `NaN` — `iverson * (2 > 1)` | `1` |
| `"iverson(x > 0)".ToEntity().Simplify()` | `iverson * (x > 0)` | `iverson(x > 0)` |
| `"sum(sum(sum(iverson(r + b + g = 4), g, 0, 3), b, 0, 3), r, 0, 3)".ToEntity().Simplify()` | `NaN` | `12` |
| `"sum(iverson(2 divides k or 3 divides k), k, 1, 1000)".ToEntity().Simplify()` | `sum(iverson * (2 divides k or 3 divides k), k, 1, 1000)` | `667` |
| `"sum(sum(sum(iverson(a + b + c = 20), c, 0, 20), b, 0, 20), a, 0, 20)".ToEntity().Simplify()` | did not finish in two minutes | `231` |

### A statement about a sum up to `n` is decided by induction

`forall n in ZZ+ : sum(f, k, a, n) = g` is proved from the least member of the set: the statement
there, and the statement at `n + 1` with the sum unfolded by one term and the sum to `n` replaced
by `g`, decided as an identity. A sum the summation already folds is read through the piecewise
its closed form comes as, and an identity is read over atoms — `2^n`, `n!`, `(-1)^n`, `sin(x)` —
with a whole shift in an exponent or a factorial's argument unfolded, and a difference of
quotients put over one denominator — claimed where that denominator is not zero, which is decided
over the set for the quantified name and stated as a condition for a free parameter. An
inequality with an exponential or a factorial is decided by induction as well, the step read off
a multiple of the hypothesis (`P(n + 1) = c P(n) + D` with `c >= 0` and `D >= 0` by a sign
calculus), and the whole numbers from `m` are read as `ZZ*` shifted by `m`. A statement that
fails at the least member is `False`; a closed form with a case on a free parameter is left as
written
([#1409](https://github.com/asc-community/AngouriMath/issues/1409), Sullivan and Mackey's chapter 5).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"forall n in ZZ+ : sum(k, k, 1, n) = n (n + 1) / 2".ToEntity().Evaled` | `UnhandledParseException` — quantifiers are new since; left as written when they arrived, the body being a `piecewise` | `True` |
| `"forall n in ZZ+ : sum(1 / (k (k + 1)), k, 1, n) = n / (n + 1)".ToEntity().Evaled` | `UnhandledParseException` | `True`, by induction |
| `"forall n in ZZ+ : sum(k k!, k, 1, n) = (n + 1)!".ToEntity().Evaled` | `UnhandledParseException` | `False`, at `n = 1` |
| `"forall n in ZZ+ : (n + 1)! + (n + 1) (n + 1)! = (n + 2)!".ToEntity().Evaled` | `UnhandledParseException` | `True` |
| `"forall x in RR : (sin(x) + 1)^2 = sin(x)^2 + 2 sin(x) + 1".ToEntity().Evaled` | `UnhandledParseException` | `True` |
| `"forall n in ZZ+ /\\ [2; +oo) : n = n".ToEntity().Evaled` | `UnhandledParseException` | `True` — the set is known not to be empty |
| `"forall n in ZZ+ : sum(1 / (a k (k + 1)), k, 1, n) = n / (a (n + 1))".ToEntity().Evaled` | `UnhandledParseException` | `True provided not a = 0` |
| `"forall n in ZZ+ /\\ [5; +oo) : 2^n > n^2".ToEntity().Evaled` | `UnhandledParseException` | `True` |
| `"forall n in ZZ+ : n! >= 2^(n - 1)".ToEntity().Evaled` | `UnhandledParseException` | `True` |
| `"forall n in ZZ+ /\\ [4; +oo) : n^2 - 2 n - 1 > 0".ToEntity().Evaled` | `UnhandledParseException` | `True` — decided about `4 + t` over `ZZ*` |

### An implication needs its conclusion's condition only where its assumption holds

`a implies (b provided c)` was `(a implies b) provided c`, the condition lifted over the whole
implication as it is over other nodes. But where `a` fails the implication holds whatever the
conclusion is, so the condition is needed only where `a` holds: it is
`(a implies b) provided (a implies c)`, and a condition among `a`'s conjuncts is no condition at
all. `not x = 0 implies x/x = 1` was left undefined at `0`, where it holds
([#1409](https://github.com/asc-community/AngouriMath/issues/1409), Sullivan and Mackey's Prob 1.5.6).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"not x = 0 implies x / x = 1".ToEntity().Simplify()` | `True provided not x = 0` | `True` |
| `"not x = 0 implies (y / y = 1)".ToEntity().Simplify()` | `True provided not y = 0` | `True provided not x = 0 implies not y = 0` |
| `forall x in RR : not x = 0 implies x / x = 1` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |

### `acsch(c x)` and `asech(c x)` are integrated with a symbolic `c`, and `|c|` differentiates to `0` in `x`

Three defects behind one symptom: `x acsch(2 x)` was answered and `x acsch(c x)` declined. The
parser spells the inverse hyperbolic cosecant `ln(1/(c x) + sqrt(1/(c x)^2 + 1))`, and (1) `(c x)^2`
was read as a power of something that is not a polynomial where `(2 x)^2` had been folded to
`4 x^2` — a whole power of a product with a symbolic constant beside the variable is now written
as the product of the powers, as a rule of its own; (2) the root of `(1 + c^2 x^2)/(c^2 x^2)` was
not written apart as `sqrt(1 + c^2 x^2)/(|c| |x|)` because `c^2` is not a number — a parameter's
even power is taken for positive now, and the answer says so, `provided c^2 > 0`, which holds
exactly for a real non-zero `c`; (3) the step by parts differentiated `|c|` to
`derivative(|c|, x)`, since the modulus rule asks whether its argument is real before it asks
whether the argument mentions the variable — **the modulus, the sign, the floor, the ceiling,
the rounding, a limit, a sum, a product, an applied function and any node with no rule of its
own differentiate to `0` where they do not mention the variable**, with no condition, which also
removes the `provided not c in ZZ` that `floor(c) + x` carried. A node with a rule of its own
keeps it: `d/dy piecewise(x provided a, 1/x)` still says where `1/x` exists. And a negative fractional power is put below the bar before two
quotients are cancelled as polynomials in their radicals, so that `q^(-1/2)` above and `sqrt(q)`
below are one indeterminate. Rubi's 7.5.1 and 7.6.1
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"abs(c) * x".ToEntity().Differentiate("x")` | `derivative(abs(c), x) * x + abs(c)` | `abs(c)` |
| `"floor(c) + x".ToEntity().Differentiate("x")` | `1 provided not c in ZZ` | `1` |
| `"c!".ToEntity().Differentiate("x")` | `derivative(c!, x)` | `0` |
| `"x * acsch(c * x)".ToEntity().Integrate("x")` | `integral(x * ln(1 / (c * x) + sqrt(1 / (c * x) ^ 2 + 1)), x)` | an antiderivative, `provided c^2 > 0` |
| `"x^2 * asech(c * x)".ToEntity().Integrate("x")` | `integral(...)`, the same | an antiderivative |
| `"x / sqrt(1 + 1/(c*x)^2)".ToEntity().Integrate("x")` | `integral(x / sqrt(1 + 1 / (c * x) ^ 2), x)` | an antiderivative, `provided c^2 > 0` |

### A conditional expression is differentiated where its condition holds

**Answers where there were none.** `F provided P` is differentiated with `P` known to hold, and
so is each case of a piecewise. Under a strict bound such as `x > 0`, a root of `x` is real, so
the sign, the modulus, `max` and `min` of an expression in it are differentiated as on the real
line, where without the bound they are left as written: for `x < 0` the roots are complex. The
answer is still claimed only where `P` holds.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sgn(x^(1/3) + 2) provided x > 0".ToEntity().Differentiate("x")` | `derivative(sgn(x ^ (1/3) + 2), x) provided x > 0` | `0 provided x > 0 and not x ^ (1/3) + 2 = 0` |
| `"abs(sqrt(x) - 1) provided x > 0".ToEntity().Differentiate("x")` | `derivative(abs(sqrt(x) - 1), x) provided x > 0` | `sgn(sqrt(x) - 1) * 1/2 * x ^ (-1/2) provided x > 0 and not sqrt(x) - 1 = 0` |
| `"max(sqrt(x), 1) provided x > 0".ToEntity().Differentiate("x")` | `derivative(max(sqrt(x), 1), x) provided x > 0` | `(1/2 * x ^ (-1/2) + sgn(sqrt(x) - 1) * 1/2 * x ^ (-1/2)) / 2 provided x > 0 and not sqrt(x) - 1 = 0` |
| `"piecewise(abs(sqrt(x) - 1) provided x > 0, 0)".ToEntity().Differentiate("x")` | `piecewise((derivative(abs(sqrt(x) - 1), x)) provided (x > 0), 0 provided True)` | the same with that case's derivative written out |

### `arsech` and `arcsch` below the bar are integrated

`1/(x^2 (a + b arcsch(c x)))` and `1/(x^4 (a + b arsech(c x)))` were left unevaluated. The
substitution that undoes an inverse hyperbolic function read `arsinh`, `arcosh` and `artanh` off
their logarithms and not these two, which are the `arsinh` and the `arcosh` of `1/L`. Under
`L = csch(u)` or `L = sech(u)` a power of `x` below the bar is a polynomial in `cosh(u)` and
`sinh(u)` over a power of `a + b u`, which the hyperbolic sine and cosine integrals and `Ei`
answer. Rubi's 7.5.1, 7.5.2 and 7.6.1
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x^2*(a+b*acsch(c*x)))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `Ei` |
| `"1/(x^4*(a+b*asech(c*x)))".ToEntity().Integrate("x")` | `integral(...)` | an antiderivative in `Chi` and `Shi` |
| `"1/(e^asech(a*x)*x^5)".ToEntity().Integrate("x")` | `integral(...)` | a polynomial in `1/(a x) - sqrt(1/(a x)^2 - 1)` and its reciprocal |

### `n in ZZ and 2^n > n^2` is solved to a set, and was a refusal

An inequality with an exponential or a factorial over the whole numbers is solved to the members
of a window from the least member and the rays the quantifier proves by induction, upwards and,
over `ZZ`, downwards; a tail it does not prove leaves the refusal in place, since a window is
evidence about the window ([#1409](https://github.com/asc-community/AngouriMath/issues/1409),
Sullivan and Mackey's Ex 5.3.2 and Prob 5.7.2, 5.7.8–9).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"n in ZZ and 2^n > n^2".ToEntity().Solve("n")` | `NotSufficientlySupportedException` — "Only polynomial inequalities are supported" | `{ 0, 1 } \/ ZZ /\ [5; +oo)` |
| `"n in ZZ+ and n! > 3^n".ToEntity().Solve("n")` | `NotSufficientlySupportedException` | `ZZ+ /\ [7; +oo)` |
| `"n in ZZ and n^3 < 3^(n - 1)".ToEntity().Solve("n")` | `NotSufficientlySupportedException` | `ZZ /\ (-oo; 0] \/ ZZ /\ [6; +oo)` |
| `"n in ZZ and 2^n >= n^2 - 3 n + 3".ToEntity().Solve("n")` | `NotSufficientlySupportedException` | the same — the tail from `1` is not proved, and the window is not guessed from |

And a divisibility or a congruence with a power of a whole base in the quantified name is decided
by its residues too, over the non-negative whole numbers: `a^n` modulo `m` repeats with the order
of `a` where `a` is prime to `m`, so `forall n in ZZ+ : 3 divides 7^n - 4^n` is `True` by one
residue and `forall n in ZZ* : not (7 divides 2^n + 1)` by three (Sullivan and Mackey's §5.2.4 Try 3
and Ex 5.3.7). A base sharing a factor with the modulus, or a negative exponent over `ZZ`, is left
as written.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall n in ZZ+ : 3 divides 7^n - 4^n` | `UnhandledParseException` | `True` |
| `forall n in ZZ* : not (7 divides 2^n + 1)` | `UnhandledParseException` | `True` |
| `forall n in ZZ+ : 5 divides 7^n - 1` | `UnhandledParseException` | `False` |

### Nested quantifiers are decided through listed witnesses and witness terms

`exists z in S : P`, where the solver lists the `z` at which `P` holds and the quantifiers around
it make each of them a whole number, is the statement that one of them is in `S`: in `ZZ*` a whole
`s` is where `s >= 0`, in `ZZ+` where `s >= 1`. Sullivan and Mackey's Prob 4.11.6,
`forall x in ZZ : forall y in ZZ : exists z in ZZ* : x - y = z or y - x = z`, is `True` so: the
witnesses are `x - y` and `y - x`, and `x - y < 0 and y - x < 0` has no solution
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)). That last step needed two rays
with symbolic ends to meet: `(a; +oo) /\ (-oo; b)` is `(a; b)`, and `(x; +oo) /\ (-oo; x)` is
empty, where both were left as written. And `exists x : forall y : P` is false where a term in
`x`, in the inner set for every `x`, fails `P` at every `x`, and `forall x : exists y : P` true where
one holds `P` at every `x`: Prob 4.11.5, `exists x in RR : forall y in RR : x^2 - y^2 >= 0`, fails at
`y = x^2 + 1`. The terms are a fixed few polynomials, so a statement none of them decides is left
as it was. A comparison of two polynomials that differ by a number says the same of every member:
`x^2 + 1 > x^2` is `1 > 0` at each, which is what the term `x^2 + 1` asks.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall x in ZZ : forall y in ZZ : exists z in ZZ* : x - y = z or y - x = z` | `UnhandledParseException` (quantifiers are new since; left as written when they arrived) | `True` |
| `exists x in RR : forall y in RR : x^2 - y^2 >= 0` | `UnhandledParseException` (left as written when quantifiers arrived) | `False` |
| `"(x; +oo) /\ (-oo; x)".ToEntity().InnerSimplified` | as written | `{ }` |
| `exists y in RR : forall x in RR : y = x^3` | `UnhandledParseException` (left as written when quantifiers arrived) | `False` |
| `forall x in RR : x^2 + 1 > x^2` | `UnhandledParseException` (left as written when quantifiers arrived) | `True` |

### `PP` is the set of primes, and `min(S)` the least member of a set

`PP` is a special set beside `ZZ+` — the primes `{2, 3, 5, 7, ...}`, `\mathbb{P}` in LaTeX,
`S.Primes` in SymPy, `Domain.Prime` as a codomain — and was a variable named `PP`, so
`7 in PP` stays as written no more. Membership is decided for a whole number by trial division
and left open past the machine word; `PP /\ [a; b]` lists its members where there are few
enough, which makes `card(PP /\ [1; n])` the prime-counting function; `PP ⊂ ZZ+` heads the
chain. `min(S)` with one argument that is a set is the least member of the set, where
`min(PP)` was `PP` itself — a one-argument `min` was the argument — and `min(x, x in S and P)`
is the least member of `{ x in S : P }`, where it was left as written; both decided for `PP`,
`ZZ+`, `ZZ*` and their cuts by an interval or a bound, so `min(x, x in PP and x > 14)` is the next
prime, `17` ([#1450](https://github.com/asc-community/AngouriMath/issues/1450)); and for a condition
that is not a bound by trying the members in order from the least, so Sullivan and Mackey's Prob
8.9.3, the least `n` with `n binomial(n - 1, 2) >= 14`, is `5`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"7 in PP".ToEntity().Evaled` | `7 in PP` — `PP` a variable | `True` |
| `"PP subset ZZ".ToEntity()` | `UnhandledParseException` — `subset` is new since | `True` on evaluation |
| `"min(PP)".ToEntity().Evaled` | `PP` | `2` |
| `"min(x, x in PP and x > 14)".ToEntity().Evaled` | as written | `17` |
| `"card(PP intersect [1; 100])".ToEntity().Evaled` | `#(PP /\ [1; 100])` | `25` |
| `"PP intersect [1; 30]".ToEntity().Evaled` | as written | `{ 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 }` |
| `"min(ZZ+)".ToEntity().Evaled` | `ZZ+` | `1` |
| `"max(x, x in PP and x < 14)".ToEntity().Evaled` | as written | `13` |
| `"min(x, x in ZZ+ and x^2 > 50)".ToEntity().Evaled` | `UnhandledParseException` — `ZZ+` is new since | `8` |
| `"prime(25)".ToEntity().Evaled` | `prime * 25` — juxtaposition of a variable `prime` | `97`; `prime(n)` is the `n`-th prime, `NaN` off the positive whole numbers |
| `"valuation(12, 2)".ToEntity().Evaled` | `UnrecognizedFunctionParseException` | `2`; `valuation(n, p)` is the `p`-adic valuation, `+oo` at `0` and `NaN` off the primes |

### Over the primes, a repeating statement is decided by the modulus's prime factors and units

A statement that repeats modulo `M`, asked of the primes past some bound, is its value at each prime
factor of `M` past the bound and at each unit modulo `M`: every prime past those factors is a unit,
and every unit is the residue of infinitely many primes, by Dirichlet's theorem. So
`forall p in PP : p > 3 implies 24 divides p^2 - 1` is `True` while the same from `p > 2` is `False`
at `3`, Sullivan and Mackey's Prob 6.7.5 and its erratum, and `exists p in PP : p > 10 and
p = 3 (mod 4)` is `True` ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).
A comparison keeps one truth value past its root, so the primes below the last root are asked
one by one, and a comparison may stand anywhere in the statement: every prime is `2`, or `1` or `3`
modulo `4`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall p in PP : p > 3 implies 24 divides p^2 - 1` | `UnhandledParseException` (left as written when `PP` arrived) | `True` |
| `forall p in PP : p = 1 (mod 4) or p = 3 (mod 4) or p = 2` | `UnhandledParseException` (left as written when `PP` arrived) | `True` |

### `atanh(tanh(a + b x))` under an integral is linear in `x`, and was an atom no rule read

An inverse hyperbolic function of a hyperbolic one is spelled by the parser as a logarithm of a
nested quotient: `atanh(tanh(a + b x))` is `1/2 ln((1 + T)/(1 - T))` with `T = (E - 1)/(E + 1)`
and `E = e^(2(a + b x))`, and `acoth(tanh(a + b x))` is `1/2 ln(1 + 2/(T - 1))`. No integration
rule read the nested quotient. It is now cancelled with `E` for an indeterminate, to
`1/2 ln(E)` and `1/2 ln(-E)`; and a logarithm whose derivative in `x` is a constant `k` is
`k x + c` for a constant `c`, which the integrator names and writes back as `ln(E) - k x` in the
answer — the same `b x - atanh(tanh(a + b x))` Rubi's answers carry. The logarithm is not
unwrapped to `a + b x`, which it equals only on a strip
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^m*atanh(tanh(a+b*x))^3".Integrate("x")` | no answer in two minutes | the antiderivative in 0.1 s, in `ln(e^(2(a + b x))) - 2 b x` |
| `"1/(x*atanh(tanh(a+b*x)))".Integrate("x")` | left unevaluated | the antiderivative |
| `"acoth(tanh(a+b*x))/x^2".Integrate("x")` | no answer in two minutes | `b/2 ln(x^2) - (ln(-e^(2(a + b x))) - 2 b x)/(2 x)`, up to the form |
| `"1/(x*ln(e^(2*(a+b*x))))".Integrate("x")` | left unevaluated | the antiderivative |
### A set-builder whose predicate is `NaN` is the empty set, and was `NaN`

`{ x : 1/0 = x }` evaluated to `NaN`, and so did `{ x : x subset 2 }` once `subset` became a
keyword — a subset of a number is not a statement about any `x`. A predicate that is `NaN` holds
nowhere, so the set it describes is empty; the solver already answered `{ }` for `1/0 = x`, and
`Solve` on `x subset 2` cast the `NaN` set to a set and threw an `InvalidCastException`, which
the crash harness found. Both now agree with the solver
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"{ x : 1/0 = x }".ToEntity().Evaled` | `NaN` | `{ }` |
| `"x subset 2".ToEntity().Solve("x")` | `x * subset ^ 2` had no solution: `{ }` — `subset` is new since | `{ }`, where the unreleased keyword threw |

### `asech(a + b x)` and `acsch(a + b x)` beside a power of `x` are integrated

By parts they leave `1/(x (a + b x)^2 sqrt(1/(a + b x)^2 - 1))`, and the chain never read it:
taking the remainder's constants out had carried it past the rules scoped to the question asked
or one below it, the root beside the polynomial made Yun's factorisation expand
`x (a + b x)^2`, the power was rewritten monic beside a root that kept `(a + b x)`, the
radicand stood in two orders, and an even power of a linear below the bar was not written
apart. Each fixed in its place; an even power of a polynomial `P` below the bar is now written
apart as `sgn(P) P` with `provided P^2 > 0` — only where the root is a factor of the integrand
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"asech(a+b*x)/x^2".Integrate("x")` | left unevaluated | the antiderivative, in `sgn(a + b x)`, `provided (a + b x)^2 > 0` |
| `"x*asech(a+b*x)".Integrate("x")` | left unevaluated | the antiderivative |
| `"acsch(a+b*x)/x^2".Integrate("x")` | left unevaluated | the antiderivative |
| `"1/(x*(a+b*x)^2*sqrt(1/(a+b*x)^2-1))".Integrate("x")` | left unevaluated | the antiderivative |

### A sum of a polynomial times a power of the index is answered in closed form

`sum(k 2^k, k, 1, n)` was left as written; it is `(n - 1) 2^(n + 1) + 2`, by the discrete
antiderivative `q(k) r^k` with `q` a polynomial of the summand's degree, found by one division
by `r - 1` per coefficient. A symbolic ratio carries the case `r = 1` separately, where the
summand is the polynomial, and to `+oo` the series converges for `|r| < 1`. §5.3.4 Try 6 of
Sullivan and Mackey's *An Introduction to Proofs*, `sum((-1)^(k - 1) k^2, k, 1, n)`, is the
row that asked for it. And a quantified statement whose body is a piecewise with a case on a
parameter -- `q = 1 and n >= 0`, which is how the sum comes -- is decided by cases on the
parameter, so their Ex 5.2.6 is answered with the condition the book asks about
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sum(k*2^k, k, 1, n)".ToEntity().Evaled` | as written | `((n - 1) 2^(n + 1) + 2) provided n >= 0`, `0` otherwise |
| `"sum((-1)^(k-1)*k^2, k, 1, n)".ToEntity().Evaled` | as written | `(-1)^(n - 1) n (n + 1)/2`, up to the form |
| `"sum(k/2^k, k, 1, +oo)".ToEntity().Evaled` | as written | `2` |
| `"forall n in ZZ+ : sum(q^i, i, 0, n-1) = (q^n - 1)/(q - 1)".ToEntity().Evaled` | `UnhandledParseException` -- quantifiers are new since | `True provided not q = 1` |

### A product of powers of one base, and a polynomial times `k!`, in closed form

`product(2^k, k, 0, n - 1)` was left as written; it is `2^(n (n - 1)/2)`, the base to the sum of
the exponents, `c^s c^t = c^(s + t)`, which holds on the principal branch as well, every power
being `e^(s ln c)` with the one `ln c`. And `sum(k k!, k, 1, n)` is `(n + 1)! - 1`: a polynomial
`P(k)` beside `k!` is summed where `P(k) k! = T(k + 1) - T(k)` for `T(k) = Q(k) k!` with a
polynomial `Q`, which one triangular solve finds or rules out. `sum(k!, k, 0, n)`, the left
factorial, has none and stays. Probs 1.3.12 and 2.7.17 of Sullivan and Mackey's
*An Introduction to Proofs* ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"product(2^k, k, 0, n - 1)".ToEntity().Evaled` | as written | `2^((n - 1)/2 + (n - 1)^2/2) provided n >= 1`, `1` otherwise |
| `"sum(k*factorial(k), k, 1, n)".ToEntity().Evaled` | as written | `((n + 1)! - 1) provided n >= 0`, `0` otherwise |

### A closed form reads as the book writes it: multiplied out where simpler, over a bound on its name

The sum of the first `n` odd numbers, `sum(2 k + 1, k, 0, n - 1)`, came out as
`2 n + (n - 1)^2 - 1 provided n - 1 >= -1`, and Sullivan and Mackey write it `n^2`. A polynomial
sum's closed form is now multiplied out where that rates simpler, so it is `n^2`, and Prob 5.7.16's
`sum(2 k + 1, k, n, 2 n - 1)` is `3 n^2`, while `sum(k, k, 1, n)` keeps `(n + n^2)/2`. And the
condition that the range is not empty, written as the ends left it, is now a bound on the one name
the ends are about, `n >= 0`. With two names it is left as it was, `n >= m - 1`
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sum(2 k + 1, k, 0, n - 1)".Simplify()` | `piecewise((2 * n + (n - 1) ^ 2 - 1) provided (n - 1 >= -1), 0 provided True)` | `piecewise((n ^ 2) provided (n >= 0), 0 provided True)` |

### A sum over a set: `sum(f, x in S)`

`sum` took a range and nothing else, so a sum over a set had no spelling: `sum(f, x in S)` was a
parse error asking for the four arguments of the range form. `SumOverSetf` reads the second
argument as `variable in set` and binds the variable over the term and the set, as `max(f, t in S)`
does: spelt `sum(f, x in S)`, printed `\sum_{x \in S} f` in LaTeX, and built by
`MathS.Sum(f, x, S)`. The range form `sum(f, k, from, to)` is untouched. A sum over the roots of a
polynomial is a sum over its set of roots, `sum(f(w), w in { w : p(w) = 0 })`, with no index and no
order.

The terms are added up where the set is finite and its members are known: a listed set of numbers,
distinct by value, and the roots of a polynomial where every one is rational, a root of a quadratic
or a root of a binomial. That is checked by counting them against the degree of the square-free
part, so a root the solver missed leaves the sum as written rather than one term short. The roots of
an irreducible cubic or quartic are not written out in radicals, which read no more simply than the
sum. Evaluated to a number, the roots it cannot
write are found to the working precision by the Durand–Kerner iteration, all of them or none. A set
with a symbol among its members, whose members may coincide, and a set that is not finite are left
as written. [#1285](https://github.com/asc-community/AngouriMath/issues/1285). Both columns
measured on a build, `v2.5.0` against this change.

| | Was (2.5.0) | Is |
|---|---|---|
| `"sum(x^2, x in {1, 2, 3})".ToEntity()` | `FunctionArgumentCountException`: *sum should have exactly 4 arguments but 2 arguments are provided* | `14` once simplified |
| `"sum(w^2, w in { w : w^3 - 2w + 1 = 0 })".ToEntity().Simplify()` | the same exception | `4` |
| `"sum(w^5, w in { w : w^5 + w + 3 = 0 })".ToEntity().Evaled` | the same exception | `-15`, from the five roots found numerically |
| `"sum(k, k in ZZ intersect [1; 10])".ToEntity().Simplify()` | the same exception | `55`, as `sum(k, k, 1, 10)` is |
| `"sum(x, x in {a, b})".ToEntity().Simplify()` | the same exception | `sum(x, x in { a, b })`, since `a` and `b` may be equal |
| `"sum(k, k, 1, 10)".ToEntity().Simplify()` | `55` | the same |

### A condition on a name a binder binds stays in the binder

**Fix.** `DomainCondition` conjoined every child's condition, a binder's body included, so a
summation's, a product's or a definite integral's condition named its index as though it were free,
and a sum over a set's did the same: the product rule leaves `0 * sum(...)`, which keeps it, and the
derivative of `2 sum(ln(x - w), w in { w : w^3 + w + 1 = 0 })` was `… provided not x - w = 0`, which
`EvalNumerical` could not decide. The condition is required at every value the name ranges over
now: a conjunction over a few numbers, a resultant over the roots of a polynomial, and `forall`
over the range otherwise. It is not left out, which would give `0 * sum(1/(k - x), k, 1, n)` a value
at `x = 1` ([#1632](https://github.com/asc-community/AngouriMath/issues/1632)). A definite integral's
condition is required between its limits, in either order. That is sufficient for a value and not
necessary: `ln(t)` over `[0; 1]` converges, to -1, and its condition reads it as undefined, since the
integrand has none at 0. Nothing is given a value it does not have that way. The integral itself,
where it is worked out, is not affected.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sum(1/(k - x), k, 1, 3)".ToEntity().DomainCondition` | `not k - x = 0` | `not 1 - x = 0 and not 2 - x = 0 and not 3 - x = 0` |
| `"sum(1/(k - x), k, 1, n)".ToEntity().DomainCondition` | `not k - x = 0` | `forall k in { k : k in ZZ and 1 <= k and k <= n } : not k - x = 0` |
| `"integral(1/(t - x), t, 0, 1)".ToEntity().DomainCondition` | `not t - x = 0` | `forall t in [0; 1] : not t - x = 0` |

### A rational function whose residues are of degree three or more is integrated over its poles

**Wider.** The logarithmic part of a rational integral is a logarithm at each pole, weighted by the
residue there. Where the residues are rational or roots of a quadratic they were written out, as
logarithms and arctangents. Where they lie in a field of degree three or more, the rational
integrator declined, and `1/(x^3 + x + 1)` was left unevaluated. It is written now as a sum over the
poles, `sum(A(r)/D'(r) ln(x - r), r in { r : E(r) = 0 })`, with the node above: `E` is the factor
of the denominator whose poles have those residues, the gcd of `D` and `D'^n R(A/D')` over the
rationals. The integrand of [#1285](https://github.com/asc-community/AngouriMath/issues/1285),
`sqrt(x)/(1 + x + x^4)`, is `2u^2/(1 + u^2 + u^8)` under `u = sqrt(x)`, and is answered through it.

**Only where nothing else answers.** The integral is asked again with sums over roots allowed once
the whole of it has come back unevaluated, so an integrand another rule writes in closed form keeps
that form. Asked with the other rules, the sum answered each of Jeffrey's three terms of
`(-1 + 4 cos(x) + 5 cos(x)^2)/(-1 - 4 cos(x) - 3 cos(x)^2 + 4 cos(x)^3)` with a sum over the roots of
a sextic, where the whole is one arctangent. A denominator with a symbol among its coefficients is
still declined. Both columns measured on a build, `v2.5.0` against this change.

| | Was (2.5.0) | Is |
|---|---|---|
| `"1/(x^3 + x + 1)".Integrate("x")` | `integral(1 / (x ^ 3 + x + 1), x)` | `sum(1 / (3 * r ^ 2 + 1) * ln(x - r), r in { r : r ^ 3 + r + 1 = 0 }) + C` |
| `"sqrt(x)/(1 + x + x^4)".Integrate("x")` | `integral(sqrt(x) / (1 + x + x ^ 4), x)` | `sum(2 * r ^ 2 / (8 * r ^ 7 + 2 * r) * ln(x ^ (1/2) - r), r in { r : r ^ 8 + r ^ 2 + 1 = 0 }) + C` |
| `"1/(1 - x^4 + x^8)".Integrate("x")` | `integral(1 / (1 - x ^ 4 + x ^ 8), x)` | `sum(1 / (8 * r ^ 7 + (-4) * r ^ 3) * ln(x - r), r in { r : r ^ 8 + -r ^ 4 + 1 = 0 }) + C` |
| `"1/(x^4 + a*x + 1)".Integrate("x")` | `integral(1 / (x ^ 4 + a * x + 1), x)` | the same |

### `(a + b asech(c x))/(d + e x)^2` is integrated, and a root written apart with `|x|` no longer needs a parity

Rubi's 7.5.1 with a symbolic linear below the bar ran for ten minutes without an answer. By
parts leaves `1/(x^2 (d + e x) sqrt(1/(c x)^2 - 1))`, which was handed on as a product to the
power `-1` that no radical rule reads (a constant over a product is now the reciprocal with the
constant in front), had its `(c x)^2` distributed one level down past the rule that writes the
root apart (the same question now), and, written apart with `|x|` for `x > 0`, was declined
for having no parity to extend by — `sgn(x)^m` stands in for it now, exact for a whole `m`. A
root inside a logarithm's argument is left as written, since the logarithm is an atom to
every rule ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(a+b*asech(c*x))/(d+pe*x)^2".Integrate("x")` | left unevaluated | the antiderivative, in `sgn(x)`, `provided c^2 > 0` |
| `"(a+b*asech(c*x))/x^7".Integrate("x")` | left unevaluated | the antiderivative |
| `"e^(2*asech(a*x))/x^2".Integrate("x")` | left unevaluated | the antiderivative, in `sgn(x)` |
| `"x*(a+b*asech(c*x))/(d+pe*x^2)^2".Integrate("x")` | left unevaluated | the antiderivative |

### Powers of `asech(a x)` and `acsch(a x)` beside a power of `x` are integrated, by rounds of parts

`x^3 asech(a x)^2` was left as written. One step of parts leaves `x^2 asech(a x)/sqrt(1/(a x)^2 - 1)`,
and the step on that -- asked two levels below the question -- is now offered where everything
beside the differentiated factor is algebraic with a root in it; the root over a monomial is
written apart at any depth; a sign of a real-valued factor is constant between its zeros and
goes in front of the antiderivative of the rest; and a root written apart with `|x|` answers
with `sgn(x)` in front rather than in `|x|`, which a further step could not read. A square of
a root is normalised to the power it is, which is what stopped Euler's substitution answering
the remainder of `acsch(c x)^2/x^4` wrongly along the way
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^3*asech(a*x)^2".Integrate("x")` | left unevaluated | the antiderivative, `provided a^2 > 0` |
| `"asech(a*x)^3/x^2".Integrate("x")` | left unevaluated | the antiderivative |
| `"(a+b*acsch(c*x))^2/x^4".Integrate("x")` | left unevaluated | the antiderivative |
| `"x^2/sqrt(1/(a^2*x^2)-1)".Integrate("x")` | left unevaluated | `sgn(x) |a| (2 (1 - a^2 x^2)^(3/2)/3 - 2 sqrt(1 - a^2 x^2))/(2 a^4)` up to the form -- with `sgn(x)` in front, where an answer written in `|x|` used to be given for such shapes |

### The binomial sums of chapter 8 are answered in closed form

`sum(k binomial(n, k), k, 0, n)` was left as written; it is `n 2^(n - 1)`, and with it a
polynomial of any degree beside the coefficient (in the falling factorial basis, by the
chairperson identity), Vandermonde's convolution, `sum(binomial(n, k)^2, k, 0, n) = binomial(2n, n)`,
the summation identity `sum(binomial(i, k), i, 0, n) = binomial(n + 1, k + 1)`, and the sums over
the even or the odd indices, each `2^(n - 1)` — Prop 8.4.4, Thm 8.4.6, Ex 8.3.11 and Probs
8.9.16 and 8.9.34 of Sullivan and Mackey's *An Introduction to Proofs*
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sum(k*binomial(n,k), k, 0, n)".ToEntity().Evaled` | `UnhandledParseException` — `binomial` is new since | `n 2^(n - 1) provided n >= 0`, `0` otherwise |
| `"sum(n!/(k!*(n-k)!)*k, k, 0, n)".ToEntity().Evaled` | as written | the same |
| `"sum(binomial(n,k)^2, k, 0, n)".ToEntity().Evaled` | parse error | `binomial(2 n, n)` |
| `"sum(binomial(i,k), i, 0, n)".ToEntity().Evaled` | parse error | `binomial(n + 1, k + 1)` |

### More binomial sums are closed, and identities between coefficients are decided

`sum(binomial(n, i) binomial(n - i, k - i), i, 0, k)` was left as written; it is `2^k binomial(n, k)`
by the trinomial revision, and `sum(binomial(n, i) binomial(i, k), i, k, n)` is
`2^(n - k) binomial(n, k)`. The parallel summation `sum(binomial(r + i, i), i, 0, n)` is
`binomial(r + n + 1, n)`, and Vandermonde's convolution along the upper indices,
`sum(binomial(j, a) binomial(m - j, b), j, 0, m)`, is `binomial(m + 1, a + b + 1)`. A quantified
identity between coefficients whose upper indices differ by whole numbers is read over the lowest of
them by Vandermonde's identity, and a product of coefficients is read in factorials, so Probs 8.9.20
to 8.9.22 and the trinomial revision of §8.4.5 are `True`. Two nested quantifiers of one kind are
taken in the other order where a closed form's range condition is about the name bound outside.
Probs 8.9.15 and 8.9.18 to 8.9.22 and §8.4.5 of Sullivan and Mackey's *An Introduction to Proofs*
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sum(binomial(n,i)*binomial(n-i,k-i), i, 0, k)".ToEntity().Evaled` | `UnhandledParseException` — `binomial` is new since | `2^k binomial(n, k) provided k >= 0`, `0` otherwise |
| `"sum(binomial(r+i,i), i, 0, n)".ToEntity().Evaled` | parse error | `binomial(1 + n + r, n) provided n >= 0`, `0` otherwise |
| `"forall n in ZZ+ : forall k in ZZ+ : binomial(n,k) - binomial(n-2,k) = 2*binomial(n-2,k-1) + binomial(n-2,k-2)".ToEntity().Evaled` | parse error — quantifiers are new since as well | `True` |

### A power of `x` beside a function of a symbolic power of `x` is integrated by substituting the power

`x^(n - 1) e^(x^n)` with a symbolic `n` was left as written: it is `e^(x^n)/n`, under `u = x^n`,
and with it `x^(k n - 1) g(x^n)` for a whole `k`, which is `u^(k - 1) g(u)/n` -- Rubi's 6.5.2 and
6.6.2, where the power in front is written `(e x)^(n - 1)`, which is `e^(n - 1) x^(n - 1)` for a
positive `e` and the answer says so. And a square root of a perfect square is the modulus at
every depth: `1/sqrt(1 + csch(x)^2)` under `u = tanh(x)` is `sqrt(u^2)/(u^2 - 1)`, which the rule
for a root of a quadratic beside a linear read as a root of a quadratic and answered with a
logarithm of zero -- wrong on `master` since the hyperbolic entry above, unevaluated in 2.5.0,
and in no release -- and is `sgn(u) u/(u^2 - 1)`
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^(n-1)*e^(x^n)".Integrate("x")` | left unevaluated | `e^(x^n)/n` |
| `"(pe*x)^(-1+n)*(a+b*csch(c+d*x^n))".Integrate("x")` | left unevaluated | the antiderivative, `provided pe > 0` |
| `"(pe*x)^(-1+n)/(a+b*sech(c+d*x^n))^2".Integrate("x")` | left unevaluated | the antiderivative, by cases on `a` and `b` |
| `"1/sqrt(1+csch(x)^2)".Integrate("x")` | left unevaluated | `-sgn(tanh(x)) ln(tanh(x)^2 - 1)/2`, in exponentials |
| `"x^4/sech(2*ln(c*x))^(1/2)".Integrate("x")` | left unevaluated | `sgn(x) (1 + c^4 x^4)^(3/2)/(6 sqrt(2) c^5)` up to the form, `provided c^4 > 0` |

### A product of powers is read as its own raised product's derivative without a sum beside it

**Improvement, not silent.** An integrand that is a constant times the derivative of its powers
with some of them raised by one is answered by that product. The rule wanted a sum beside the powers
and a power of `x` among them, and read the sum's quotient through `f'/f`, which with two symbolic
powers of polynomials ran past a minute. Where a power is not a whole one it now reads a product
with no sum, and `x` raised from no power at all, and the sum as a polynomial
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x^2*sqrt(a*x+b*x^4))".Integrate("x")` | left unevaluated | `-2 sqrt(a x + b x^4)/(3 a x^2)` |
| `"(a+b*x^n+c*x^(2*n))^p*(a+b*(1+n+n*p)*x^n+c*(1+2*n*(1+p))*x^(2*n))".Integrate("x")`, Rubi's 1.2.3.5 | no answer in 30 s | `x (a + b x^n + c x^(2 n))^(p + 1)` |
| Rubi 1.3.2's `x (a + b x + c x^2)^m (d + e x + f x^2 + g x^3)^n` times the sum its derivative leaves | no answer in 90 s | `x^2 (a + b x + c x^2)^(m + 1) (d + e x + f x^2 + g x^3)^(n + 1)` |

### A root of a rational function of `coth(x)` is integrated by the hyperbolic half-angle

`coth(x)^3/sqrt(a + b coth(x)^2)` was a search past the budget. The rational-in-`tanh` route
declined the half-angle -- `u = tanh(x)` for an integrand written in `e^(2x)` -- whenever a
root was in the integrand, because for `sqrt(sinh(x))` the half makes a root of `2u/(1 - u^2)`
where `u = e^x` answers with a root of a polynomial. With a root it now takes the half where
every radicand comes out over a power of `u`: `a + b coth(x)^2` is `(b + a u^2)/u^2`, whose root
is `sqrt(b + a u^2)/|u|`, and `|u|^k` for the bare square is `sgn(u) u^k` at every depth. Rubi's
6.4.7, nine rows ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"coth(x)^3/sqrt(a+b*coth(x)^2)".Integrate("x")` | left unevaluated | the antiderivative, in `sgn(tanh(x))` and `sqrt(b + a tanh(x)^2)`, by cases on `a + b` |
| `"coth(x)*(a+b*coth(x)^2)^(3/2)".Integrate("x")` | left unevaluated | the antiderivative |
| `"coth(x)/(a+b*coth(x)^2)^(3/2)".Integrate("x")` | left unevaluated | the antiderivative |
| `"(a+b*coth(x)^2)^(3/2)*tanh(x)^2".Integrate("x")` | left unevaluated | the antiderivative |

### A substitution that would write a root of a negated quantity through `i` keeps the root

`sqrt(a + b sech(x)) tanh(x)^5` ran past two minutes. Under `u = tanh(x)` a substitution the
search tried wrote `sqrt(1 - u^2)`, simplified one level, as `i sqrt(u^2 - 1)`: that is the other
root wherever `u^2 < 1`, which is everywhere `tanh` goes, and the answer's derivative was off at
every real point. The search now keeps a candidate's integrand as written where simplifying it
would bring in the imaginary unit, and passes over a candidate whose integrand holds it all the
same. With the root as it was, the cube is answered too
([#1370](https://github.com/asc-community/AngouriMath/issues/1370)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(a+b*sech(x))*tanh(x)^5".Integrate("x")` | ran past two minutes | the antiderivative, in `sqrt(a + b sqrt(1 - tanh(x)^2))`, written in exponentials |
| `"sqrt(a+b*sech(x))*tanh(x)^3".Integrate("x")` | ran past five minutes | the antiderivative, the same way |

### A rational function of the hyperbolic functions odd in the sine or the cosine is integrated by the other

`sinh(x)^3/(a + b cosh(x)^2)` was a search past the budget: under `u = e^x` it is a rational
function of a symbolic palindromic quartic, and under `u = tanh(x/2)` of a symbolic polynomial
of degree eight. It is `(u^2 - 1)/(a + b u^2)` under `u = cosh(x)` -- Bioche's first two rules,
which the trigonometric functions already had: odd in the hyperbolic sine, `u = cosh` with
`sinh^2 = u^2 - 1`; odd in the hyperbolic cosine, `u = sinh` with `cosh^2 = u^2 + 1`. The six
hyperbolic functions are read back from their exponential spellings for any linear argument,
a written power is handed on as the power it is, and a root of a polynomial in the two even
in the function is admitted. Rubi's 6.1.7, 6.2.7, 6.3.7, 6.5.7 and 6.6.3
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sinh(x)^3/(a+b*cosh(x)^2)".Integrate("x")` | left unevaluated | `cosh(x)/b - (a + b) arctan(sqrt(b) cosh(x)/sqrt(a))/(b sqrt(a b))` up to the form, by cases on `a` and `b` |
| `"cosh(c+d*x)/(a+b*tanh(c+d*x)^2)".Integrate("x")` | left unevaluated | the antiderivative, in `sinh(c + d x)` |
| `"sech(c+d*x)/(a+b*sinh(c+d*x)^2)^(3/2)".Integrate("x")` | left unevaluated | the antiderivative |
| `"csch(x)^5/(a+b*cosh(x)^2)".Integrate("x")` | left unevaluated | the antiderivative |
| `"sinh(x)^3/(1+cosh(x)^2)".Integrate("x")` | left unevaluated | `cosh(x) - 2 arctan(cosh(x))`, in exponentials -- where `master` between the releases gave it through `u = e^x`, in `arctan(2 e^x/sqrt(12 - 8 sqrt(2)))` and its like |

### A fractional power of `a ± a cosh(y)` is integrated by the half angle at which it is a square

`x^2 sqrt(a + a cosh(c + d x))` was left as written: no substitution rationalises a root of a
hyperbolic function beside a power of `x`. `1 + cosh(y)` is `2 cosh(y/2)^2`, so the root is
`sqrt(2a) cosh(y/2)` with no sign to write, the hyperbolic cosine being positive; and
`1 - cosh(y)` is `-2 sinh(y/2)^2`, so `(a - a cosh(y))^(3/2)` is
`(-2a)^(3/2) sgn(sinh(y/2)) sinh(y/2)^3`, the sign a constant between the zeros that comes out
in front -- the identity `sqrt(a + a sin(y))` already had. Exact for any `a`, the constants
`2a` and `-2a` kept whole under the power. Rubi's 6.2.1 and 6.2.5
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2*sqrt(a+a*cosh(c+d*x))".Integrate("x")` | left unevaluated | `sqrt(2a) (2 x^2 sinh((c + d x)/2)/d - 8 x cosh((c + d x)/2)/d^2 + 16 sinh((c + d x)/2)/d^3)` up to the form, in exponentials |
| `"x*(a+a*cosh(x))^(3/2)".Integrate("x")` | left unevaluated | the antiderivative |
| `"cosh(x)/sqrt(a-a*cosh(x))".Integrate("x")` | left unevaluated | the antiderivative, with `sgn(sinh(x/2))` in front |
| `"(A+B*cosh(x))/(a-a*cosh(x))^(5/2)".Integrate("x")` | left unevaluated | the antiderivative, with `sgn(sinh(x/2))` in front |

### A rational function of `e^y` with a symbolic denominator is integrated by the hyperbolic half angle

`csch(x)^2/(a + b sinh(x))^2` was a search past eighty seconds: under `u = e^x` its denominator
is a symbolic quartic nothing factors. Under `u = tanh(x/2)` -- the hyperbolic half angle, which
the route for rational functions of the hyperbolic tangent took only where every exponential was
written with an even factor -- it is a rational function whose written factors the symbolic
partial fractions answer. It is taken now for a root-free integrand of a plain `e^y` as well,
where the denominator's shape depends on a symbol: read as a polynomial in `v = e^y`, a
coefficient that is not a number times the first, and two distinct factors of `v` below the bar.
Both conditions are measured: `tanh(x)^6/(a + a sech(x))`, whose denominator is `a` times one
the rational integrator factors over the rationals, answers a page shorter under `u = e^x`, and
`e^asinh(a + b x)/x^4` leaves `e^u(e^u + e^-u)/(e^u - e^-u - 2a)^4`, a single symbolic quadratic
to the fourth whose roots the quadratic formula gives, in a second there against thirty here.
Rubi's 6.1.5 and 6.6.3 ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"csch(x)^2/(a+b*sinh(x))^2".Integrate("x")` | left unevaluated | the antiderivative, by cases on `a` and `b` |
| `"tanh(x)^4/(a+b*csch(x))".Integrate("x")` | left unevaluated | the antiderivative |
| `"csch(x)^2/(a+b*csch(x))".Integrate("x")` | left unevaluated | the antiderivative |

### Hyperbolic powers two apart whose coefficients kill the reduction's residual are integrated

`cosh(x)^(5/2) - 3 sqrt(cosh(x))/5` was left as written, and neither of its terms has an
elementary antiderivative -- both are elliptic. Their combination has one. From the reduction
`int cosh^p = sinh cosh^(p - 1)/p + (p - 1)/p int cosh^(p - 2)`, a sum of powers two apart is
elementary exactly when the walk from the highest exponent down carries nothing past the lowest,
and the answer is what the walk accumulated; the hyperbolic sine's reduction subtracts where the
cosine's adds. The chain may be several steps long -- Rubi's
`x/sech(x)^(7/2) - 5 x sqrt(sech(x))/21` closes over two, its coefficient being `(5/7)(1/3)` --
and one linear factor in front is taken by parts against the same antiderivative, `x F - int F`
with `int F` the next power over `p^2`. Rubi's 6.1.1, 6.2.1, 6.5.1 and 6.6.1
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"cosh(x)^(5/2)-3/5*cosh(x)^(1/2)".Integrate("x")` | left unevaluated | `2 sinh(x) cosh(x)^(3/2)/5`, in exponentials |
| `"x/sech(x)^(3/2)-1/3*x*sqrt(sech(x))".Integrate("x")` | left unevaluated | `2 x sinh(x) sqrt(cosh(x))/3 - 4 cosh(x)^(3/2)/9` up to the form |
| `"x/sech(x)^(7/2)-5/21*x*sqrt(sech(x))".Integrate("x")` | left unevaluated | the antiderivative, over two reduction steps |
| `"x/csch(x)^(3/2)+1/3*x*sqrt(csch(x))".Integrate("x")` | left unevaluated | the antiderivative |

### An exponential of a hyperbolic sine or cosine beside a function of it over its derivative is integrated

**Answers where there were none.** `e^(n cosh(a + b x)) tanh(a + b x)` is `Ei(n cosh(a + b x))/b`, as
`e^(n cos(x)) tan(x)` is `-Ei(n cos(x))`, which was answered; the hyperbolic one was declined, after
seconds, and so were `e^(n sinh(a + b x)) sinh(2 (a + b x))` and the same with half the argument in the
exponent, Rubi's 6.7.1. A hyperbolic function arrives as exponentials, and the substitution that reads
the sine or cosine as a node had none to read. Written in `w = e^L`, the integrand beside the outer
exponential over the derivative of its sine or cosine is now asked as a rational function of
`u = w + s/w`, twice that sine or cosine; where it is one, it is integrated in `u` and written back
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"exp(n*cosh(a + b*x))*tanh(a + b*x)".ToEntity().Integrate("x")` | `integral(...)` | `Ei(n cosh(a + b x))/b`, written as exponentials |
| `"exp(n*sinh(a + b*x))*sinh(2*(a + b*x))".ToEntity().Integrate("x")` | `integral(...)` | elementary in `sinh(a + b x)` and its exponential |
| `"exp(n*cosh(1/2*(a + b*x)))*sinh(a + b*x)".ToEntity().Integrate("x")` | `integral(...)` | the same in `cosh((a + b x)/2)` |

### A hyperbolic function of a logarithm is integrated, the exponent folded structurally

`tanh(ln(x))` was left as written. The library spells `tanh(y)` with `e^(2y)`, so a hyperbolic
function of a logarithm is an exponential whose exponent is a *sum* holding one logarithm --
`e^(a + b ln(q))` -- and the rule that folds an exponential of a logarithm read only a product,
`e^(k ln(q))`. It reads the exponent structurally now: `e^(u + v)` is `e^u e^v`, `e^(k u)` is
`(e^u)^k` for an `x`-free `k`, `e^(u/d)` likewise, and `e^(ln q)` is `q`, so
`a + b ln(c x^n)` folds to `e^a (c x^n)^b` and its negation -- which the same function writes
below the bar -- to the reciprocal. Composed exponents are flattened as they fold: `n (ln(q)/2)`
is `q^(n/2)` and not `(sqrt(q))^n`, whose nesting made
`e^(n acoth(a x))/(c - a^2 c x^2)^4` a search of fifty seconds where the flat form is declined in
three. One logarithm in the exponent, since a difference of two folds to a power of a quotient of
quotients that nothing below reads. Rubi's 6.3.2 and 6.5.3
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"tanh(ln(x))".Integrate("x")` | left unevaluated | `x - 2 arctan(x)` up to the form |
| `"sinh(2+3*ln(x))".Integrate("x")` | left unevaluated | `e^2 x^4/8 - x^(-2)/(4 e^2)` up to the form |
| `"sech(a+2*ln(c/x^(1/2)))^3".Integrate("x")` | left unevaluated | the antiderivative |
| `"x*tanh(a+2*ln(x))^2".Integrate("x")` | left unevaluated | the antiderivative |

### An exponential times a trigonometric at the same frequency no longer divides by zero, and a phase is expanded

`x e^(-i x) cos(x)` was answered `NaN` at every point of its domain. The closed form
`int e^(a x)(c cos(b x) + d sin(b x)) = e^(a x)(...)/(a^2 + b^2)` is guarded against the resonance
`a = ±i b`, where the product is a sum of two exponentials and the formula divides by zero -- but
the guard tested whether `a^2 + b^2` *evaluates* to a number, and with a symbolic frequency
`(-i f)^2 + f^2` is `-f^2 + f^2`, which `InnerSimplified` does not collect. So the guard passed,
the formula divided by a zero it could not see, and the answer was `NaN`: a wrong answer, not a
decline. Both places that test it -- the table pattern and the polynomial route -- see the
resonance symbolically now.

With that fixed, two capabilities: a **phase** in the trigonometric's argument is expanded by the
angle-sum identity where an exponential stands beside it (`cos(f x + p)` is
`cos(p) cos(f x) - sin(p) sin(f x)`; the rule reads one frequency and no phase, an exponential's
own offset being a constant factor where a trigonometric's is not), and **`A + i A tan(z)` below
the bar** is written as `A e^(i z)/cos(z)`, with `A + i A cot(z)` as `i A e^(-i z)/sin(z)` --
exactly, since `cos z + i sin z` is `e^(i z)`. Beside a polynomial that is the shape the closed
rule answers, where the imaginary unit in the coefficient is read by nothing else. Rubi's 4.3.10,
4.4.1 and 4.4.10 ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x*e^(-i*x)*cos(x)".Integrate("x")` | left unevaluated | `x^2/4 + e^(-2 i x)(1 + 2 i x)/8` up to the form -- where `master` between the releases answered `NaN` |
| `"x*e^(2*x)*cos(3*x+1)".Integrate("x")` | left unevaluated | the antiderivative |
| `"x/(2+2*i*tan(x))".Integrate("x")` | left unevaluated | the antiderivative |
| `"(c+d*x)/(a+i*a*tan(pe+f*x))".Integrate("x")` | left unevaluated | the antiderivative |

### A tangent above an imaginary tangent below is written in sines and cosines, and those as exponentials

**Answers where there were none.** `tan(c + d x)/(a + i a tan(c + d x))` ran past the corpus's budget
and `1/(a + i a tan(c + d x))` was declined. With `A + i A tan(z)` below the bar written as
`A e^(i z)/cos(z)`, a tangent of `z` above it stayed one, so `tan(z) cos(z)` was handed on as
written, and with `z = c + d x` the phase was expanded into `sin(c)` and `cos(c)` beside `d x`. The
tangent or cotangent above is written in sines and cosines now, what the identity puts below
cancels against it, and the sines and cosines of `z` that are left are written as the exponentials
they are, so that the whole is a rational function of `e^(i z)`. Only where nothing of `x` stands
beside them but exponentials of linears and polynomials in `x`: `sec(z)^8/(a + i a tan(z))^4` and
`sqrt(tan(z))/(a + i a tan(z))^(5/2)` are read through the tangent as before. Rubi's 4.3.2.1 and
4.4.2.1 ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"tan(c+d*x)/(a+i*a*tan(c+d*x))".Integrate("x")` | `integral(...)` | `x` and `e^(-2 i (c + d x))`, over `a` |
| `"1/(a+i*a*tan(c+d*x))".Integrate("x")` | `integral(...)` | the same |
| `"tan(c+d*x)^3/(a+i*a*tan(c+d*x))^4".Integrate("x")` | `integral(...)` | `x` and powers of `e^(-2 i (c + d x))` |
| `"cot(c+d*x)/(a+i*a*cot(c+d*x))".Integrate("x")` | `integral(...)` | `x` and `e^(2 i (c + d x))` |
| `"tan(c+d*x)^2/(a+i*a*tan(c+d*x))".Integrate("x")` | `integral(...)` | a power of `e^(-i (c + d x))` and logarithms of `e^(+/-2 i (c + d x)) + 1` |

### A symbolic constant factor no longer stops the rounds of parts

`F^(c (a + b x)) sin(d + pe x)^3` was left as written. The exponent's constant part becomes a
factor -- `F^(a c)` -- and the by-parts loop stops when the factor it carries differentiates to
nothing; but `F^(a c)` differentiates to a *conditional* zero, `0 provided not F = 0 or a c > 0`,
and the loop compared against the number, so it carried on until it ran out of steps and
declined. The derivative is read bare now: the conditions are the constant's own and travel with
it in the answer, and what the loop asks is whether the degree has fallen to nothing. Rubi's
4.7.6 and 6.1.5 ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"F^(c*(a+b*x))*sin(d+pe*x)^3".Integrate("x")` | left unevaluated | the antiderivative |
| `"F^(c*(a+b*x))*sin(d+pe*x)*cos(d+pe*x)".Integrate("x")` | left unevaluated | the antiderivative |

### A polynomial may stand beside a root of `a ± a cos(y)`

`x^3 sqrt(a + a cos(c + d x))` was left as written. The half angle at which `a ± a cos(y)` is a
square answers the root alone, and it substitutes the angle -- so anything else that mentioned
the variable stopped it, a polynomial among them. What is left of the variable is the variable
itself: with `u` half the angle (a quarter turn further for the sine's identity), `x` is
`(2u - offset)/rate`, so the polynomial is a polynomial in `u`, and what is handed on is one
times a cosine of `u`, which the closed rules answer. Rubi's 4.2.10
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^3*sqrt(a+a*cos(c+d*x))".Integrate("x")` | left unevaluated | the antiderivative, with `sgn(cos((c + d x)/2))` in front |
| `"x^3*sqrt(a-a*cos(x))".Integrate("x")` | left unevaluated | the antiderivative |
| `"x^2*sqrt(a+a*sin(x))".Integrate("x")` | left unevaluated | the antiderivative |

### A root of a quadratic in the tangent is rotated until the quadratic has no linear term

`1/sqrt(1 + 2 tan(x) + 3 tan(x)^2)` was left as written. Under `x = y + arctan(m)` the tangent
becomes `(tan(y) + m)/(1 - m tan(y))` -- the Möbius map that preserves `1 + tan^2`, which is the
factor the tangent substitution's `dx` brings -- and the quadratic's linear coefficient becomes
`b(1 - m^2) + 2(c - a)m`, zero for a root of `b m^2 + 2(a - c)m - b`, whose discriminant
`4((a - c)^2 + b^2)` is never negative. The rotated quadratic `A + C tan(y)^2` goes through the
tangent substitution to `1/((1 + t^2) sqrt(A + C t^2))`, which was already answered. The radicand
is *assembled* -- `A = a + bm + cm^2`, `C = am^2 - bm + c` -- rather than substituted into, since
writing the quotient inside the root leaves a nesting nothing downstream reduces; and a
`sgn(1 - m tan(y))` comes out in front, the modulus that `sqrt(N/(1 - mS)^2)` leaves.

A numeric rotation only: with symbolic coefficients `m` is a nested surd, the rotated quadratic is
written in it, and the rational integrator is handed a quotient over that field -- two minutes and
no answer, where the integrand is declined in under a second unrotated, so the symbolic shape
keeps its decline ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/sqrt(1+2*tan(x)+3*tan(x)^2)".Integrate("x")` | left unevaluated | the antiderivative, with `sgn(1 - m tan(x - arctan(m)))` in front |
| `"tan(x)/sqrt(2+tan(x)+tan(x)^2)".Integrate("x")` | left unevaluated | the antiderivative |
| `"tan(x)^2/(2+tan(x)+tan(x)^2)^(3/2)".Integrate("x")` | left unevaluated | the antiderivative |

### A power of the variable below the bar beside a root of a quadratic

`1/(x^2 sqrt(4 + 3x + 2x^2))` was left as written, while `1/(x sqrt(...))` came out: the single
factor was read and the repeated one was not. Under `x = 1/t` the integrand is
`-sgn(t) t^(n - 1)/sqrt(q0 t^2 + q1 t + q2)` -- a polynomial over the root of the quadratic read
the other way round, which the rules for those answer. That is what one round of parts against
`arccos(a + b x)/x^4` leaves, so the inverse trigonometric rows over a power of `x` come with it.

The radicand is assembled rather than substituted into, as `Q(1/t)` is `R(t)/t^2` and writing the
quotient inside the root leaves a nesting nothing reduces; the modulus that leaves is a `sgn(t)`
in front, which at `t = 1/x` is `sgn(x)`. `SolveByReciprocalSubstitution` is the same substitution
for a different shape -- a palindromic quartic, where the reciprocal maps the quartic to itself --
and reads nothing here ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x^3*sqrt(4+3*x+2*x^2))".Integrate("x")` | left unevaluated | the antiderivative |
| `"1/(x^2*sqrt(1-(a+b*x)^2))".Integrate("x")` | left unevaluated | the antiderivative |
| `"acos(a+b*x)/x^4".Integrate("x")` | left unevaluated | the antiderivative |

### A perfect square written with symbols is read as one

`(A + B x)(d + e x)/(a^2 + 2 a b x + b^2 x^2)^(5/2)` was left as written: the radicand is
`(a + b x)^2`, and the power is `|a + b x|^5`, but three conditions between the rule and the
rewrite each said no. The discriminant `4 a^2 b^2 - 4 a^2 b^2` is a zero
`InnerSimplified` does not collect, so the square read as an ordinary quadratic. The leading
coefficient `b^2` is not a *number*, though it is positive for a real parameter, which the answer
now says as `provided b^2 > 0`. And the rule's own bound on how large a radicand it will read --
there so that it costs nothing at every level of the descent -- is lifted at the top, where any
half-odd power of a square is the power of the modulus; below it the square root only, since a
sign written for a substitution's variable is a factor the rest of the search has to carry.

The sign goes in front of the integral rather than into the integrand, which is what every rule
here that writes one does: left inside, a rule below differentiated it, and
`sqrt(a^2 + 2 a b x + b^2 x^2) sqrt(c + e x + d x^2)` threw
`CannotEvalException: derivative(sgn(...))` rather than answering
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(A+B*x)*(d+pe*x)/(a^2+2*a*b*x+b^2*x^2)^(5/2)".Integrate("x")` | left unevaluated | the antiderivative, `provided b^2 > 0` |
| `"sqrt(a^2+2*a*b*x+b^2*x^2)*sqrt(c+pe*x+d*x^2)".Integrate("x")` | left unevaluated | the antiderivative |
| `"(a^2+2*a*b*x+b^2*x^2)^(5/2)".Integrate("x")` | the antiderivative, through the substitution search | `sgn(a + b x) (a + b x)^6 |b|^5/(6 b)` up to the form |

### Any power of a square written out is the power of its root, in any power of `x`

**Answers where there were none.** A square written out, `A + B w + C w^2` with `B^2 = 4 A C`,
was read as the modulus of its root only under half an odd power, and only in a whole power of
`x`. A symbolic power, `(a^2 + 2 a b x + b^2 x^2)^p`, or a power such as `3/4`, was declined, and
so was the square in `x^n` or in `x^(1/3)`. Now the power of the square is the power of its root
`L = w + B/(2 C)` times `(A + B w + C w^2)^p / L^(2p)`. That factor is constant wherever `L` is
not zero, and it goes in front of the integral. These are Rubi's 1.2.1.2, 1.2.2.2 and 1.2.3.2
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2*(a^2 + 2*a*b*x^3 + b^2*x^6)^p".Integrate("x")` | left unevaluated | `(a^2 + 2 a b x^3 + b^2 x^6)^p (x^3 + a/b)/(3 (2 p + 1))`, as that factor times a power of `x^3 + a/b` |
| `"(a + b*x)*(d + e*x)^3*(a^2 + 2*a*b*x + b^2*x^2)^p".Integrate("x")` | left unevaluated | the factor times powers of `x + a/b` |
| `"(a^2 + 2*a*b*x^2 + b^2*x^4)^(3/4)".Integrate("x")` | left unevaluated | the factor times the antiderivative of `(x^2 + a/b)^(3/2)` |
| `"x*sqrt(a^2 + 2*a*b*x^n + b^2*x^(2*n))".Integrate("x")` | left unevaluated | `sqrt(a^2 + 2 a b x^n + b^2 x^(2n))/(x^n + a/b)` times `a x^2/(2 b) + x^(n + 2)/(n + 2)` |
| `"x*(a^2 + 2*a*b*x^(1/3) + b^2*x^(2/3))^p".Integrate("x")` | left unevaluated | the factor times powers of `x^(1/3) + a/b` |

### The root of a quadratic that is a square is the modulus of a linear, and was read as any quadratic

**Wrong answers, silent.** The table's entry for `k/sqrt(Q)` and `sqrt(Q)` reads a quadratic by the
sign of its leading coefficient: an arcsine over the root of the discriminant, or the logarithm of
`2 a x + b + 2 sqrt(a) sqrt(Q)`. Where `Q` is a square the discriminant is zero, and that logarithm
is of zero beyond the root. So `1/sqrt(x^2 + 2x + 1)` had no value for any `x < -1`, where it is
`1/|x + 1|`, and with a negative leading coefficient there was no value anywhere. The root of a
square is now the modulus of its linear, `sqrt(a (x + h)^2) = sqrt(a) |x + h|`, for a leading
coefficient of either sign ([#1670](https://github.com/asc-community/AngouriMath/issues/1670)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/sqrt(x^2+2*x+1)".Integrate("x")` | `ln(2 x + 2 + 2 sqrt(x^2 + 2 x + 1))`, `ln(0)` for every `x < -1` | `sgn(x + 1) ln(x + 1)` |
| `"sqrt(x^2+2*x+1)".Integrate("x")` | `(2 x + 2) sqrt(x^2 + 2 x + 1)/4 provided not 2 x + 2 + 2 sqrt(x^2 + 2 x + 1) = 0`, which says nothing for `x <= -1` | `sgn(x + 1) (x^2/2 + x)` |
| `"1/sqrt(-4-4*x-x^2)".Integrate("x")`, and `sqrt(-4 - 4 x - x^2)` | `NaN` | `sgn(x + 2) ln(i x + 2 i)/i`, and `i sgn(x + 2) (x^2/2 + 2 x)` |
| `"1/sqrt(-a^2-2*a*b*x-b^2*x^2)".Integrate("x")` | a piecewise whose arcsine divides by `sqrt(0)`, `NaN` for every real `b` but zero | a logarithm over `sqrt(-b^2)`, `provided b^2 > 0` |
| `"1/(x*sqrt(a^2+2*a*b*x+b^2*x^2))".Integrate("x")`, the same with `-a^2 - 2 a b x - b^2 x^2`, or with `d + e x` for `x` | left unevaluated | the antiderivative, `provided b^2 > 0` |

### A root of a square in a fractional power of `x` is the modulus of a linear in that power

**Answers where there were none.** A sum of powers of `x` that are whole multiples of one
fractional `k`, such as `a^2 + 2 a b x^(1/3) + b^2 x^(2/3)`, is read as a polynomial in `w = x^k`,
and the root of a square in it as the modulus of a linear in `w`, as the root of a square
quadratic is. `w` is real for a positive `x`, and the answer says so, `provided x > 0`. Rubi's
1.2.3.2 ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(a^2+2*a*b*x^(1/3)+b^2*x^(2/3))".Integrate("x")` | left unevaluated | `sgn(x^(1/3) + a/b) sqrt(b^2) (3/4 x^(4/3) + a/b x) provided x > 0 and b^2 > 0` |
| `"1/sqrt(a^2+b^2*x+2*a*b*sqrt(x))".Integrate("x")` | left unevaluated | `sgn(sqrt(x) + a/b) (2 sqrt(x) - 2 a/b ln(a/b + sqrt(x)))/sqrt(b^2)`, under the same condition |
| `"(a^2+b^2/x^(2/5)+2*a*b/x^(1/5))^(5/2)".Integrate("x")`, Rubi 1.2.3.2 #659 | left unevaluated | an antiderivative in `x^(-1/5)`, under the same condition |
| `"(1+2*x^(1/2)+x)^(3/2)".Integrate("x")` | left unevaluated | `2 (x/2 + x^(3/2) + 3 x^2/4 + x^(5/2)/5) provided x > 0` |

### A sine or cosine of the reciprocal of a linear beside a power of it is integrated under `u = 1/L`

**Answers where there were none.** `sin(a + b/x)/x^3` was declined, while under `u = 1/x` it is
`-u sin(a + b u)`, one step by parts. A trigonometric function of `a + b/L^k`, `L` a linear, beside
`L^m` with `m <= -3` is a polynomial in `u = 1/L` times the function, and is integrated so and written
back. Rubi's 4.1.12 and 4.2.12, `(e x)^m (a + b sin(c + d x^n))^p` for a negative `n`. The other
powers were answered already and are answered as before
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sin(a + b/x)/x^3".ToEntity().Integrate("x")` | `integral(...)` | a sine and a cosine of `a + b/x` |
| `"cos(a + b/x)/x^4".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"sin(a + b/x^2)/x^5".ToEntity().Integrate("x")` | `integral(...)` | the same, of `a + b/x^2` |
| `"sin(a + b/(c + d*x))/(c + d*x)^3".ToEntity().Integrate("x")` | `integral(...)` | the same, of `a + b/(c + d x)` |

### A power of the variable times a sine or cosine of a logarithm, and a power of a monomial

`x^2 sin(a + b ln(c x^n))` and `(c x^n)^b` were both left as written. Two rules, each exact:

**The trigonometric of a logarithm** is a closed form rather than a search. Two rounds of parts
close on the integrand -- the sine's remainder is the cosine's integral and the cosine's is the
sine's -- so the pair is *solved*: `int x^m sin(L) dx` is
`x^(m + 1)((m + 1) sin(L) - B cos(L))/((m + 1)^2 + B^2)` wherever `L' = B/x` for a constant `B`,
which `a + b ln(c x^n)` is with `B = b n`; the cosine's is the same with
`(m + 1) cos(L) + B sin(L)`. The hyperbolic twin needs no rule -- the library writes `sinh` as
exponentials, which fold against the logarithm -- and the sine and cosine are nodes that fold
against nothing.

**A power of a monomial** is distributed: `(c x^n)^b` is `c^b x^(n b)`, for a positive `c`, and
only where `n` is not whole. With a whole `n` the integrand is real at a negative `x` too, and
there `(c x^n)^b` is a power of `|x|`, not of `x`: `(2 u^3)^(3/2)` is `2^(3/2) sgn(u) u^(9/2)`,
which the rules for a root of an even power already answer with its sign
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2*sin(a+b*ln(c*x^n))".Integrate("x")` | left unevaluated | `x^3(3 sin(L) - b n cos(L))/(9 + b^2 n^2)` |
| `"cos(a+b*ln(c*x^n))".Integrate("x")` | left unevaluated | `x(cos(L) + b n sin(L))/(1 + b^2 n^2)` |
| `"(c*x^n)^b".Integrate("x")` | left unevaluated | `c^b x^(n b + 1)/(n b + 1) provided c > 0` |

### A constant comes out of a fractional power beside another power of the same function

`sqrt(b sec(x))/sec(x)^(7/2)` was left as written. The two powers of the secant are the same
function to a total power of `-3`, which the rules for a power of a secant answer -- but they
cannot see it while one of them is written over `b sec(x)` rather than over `sec(x)`.

`(c f)^b` is `c^b f^b` for a **positive** real `c` and any `f`: both sides pick up the same phase
where `f` is negative, so the rewrite needs nothing assumed about `f`, and where `c` carries
symbols the answer says `provided c > 0`. That condition is not decoration. 2.5.0 answered
`sec(x)^(3/2)/(b sec(x))^(5/2)` without it, and that answer is **wrong wherever `b` and the
secant are both negative** -- at `b = -3` its derivative is `+0.0267i` against the integrand's
`-0.0267i` at `x = 2`, while the two agree at `x = 0.5`.
[#1388](https://github.com/asc-community/AngouriMath/pull/1388) withdrew the unconditional
reading for that reason, and this brings the shape back with the condition it owes.

Where it is asked, and what it takes, is what keeps it from costing anything elsewhere:

- **After the rules that answer the same shapes for any real constant.** A function comes out
  of its even power with its sign -- `(a sin(x)^2)^(5/2)` is `a^(5/2) sgn(sin x) sin(x)^5` for
  every real `a`, an even power being never negative -- and `sqrt(a + a sin(x))` is integrated
  by the half angle at which `1 + sin(x)` is a square, again for every real `a`. Asked before
  them, this answered both `provided a > 0` and lost the negative half of the line. It is asked
  after them, and before the substitution search, which spends the budget on the roots.
- **A single factor in which `x` enters only through trigonometric functions.** The constant
  comes out only where the rest of the base is one factor like `sec(x)` or `1 - sin(x)^2`, with
  the constant a sum writes in every term read as well: `a - a sin(x)^2` is `a (1 - sin(x)^2)`.
  The rewritten integrand is asked again at the same depth, so a rewrite that does not bring the
  answer closer multiplies the search at every level below it. Over a product it would rewrite
  the question into one no easier -- `(cos(x)^11 sin(x)^13)^(-1/4)` measured at ten seconds
  against forty-seven -- and over a hyperbolic function, which is written in exponentials, it
  finds a `2` in `csch(x) = 1/((e^x - e^(-x))/2)` under every substitution below it:
  `x/csch(x)^(3/2)`, declined in seven seconds, was not declined in ninety.

The power of the variable is unchanged and still splits only where its exponent is not whole
(the entry above), because `(2 u^3)^(3/2)` is `2^(3/2) sgn(u) u^(9/2)` and the sign belongs to
the rules for a root of an even power.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(b*sec(c+d*x))/sec(c+d*x)^(7/2)".Integrate("x")` | left unevaluated | `sqrt(b)(sin(y) - sin(y)^3/3)/d provided b > 0` |
| `"sec(c+d*x)^(3/2)/(b*sec(c+d*x))^(5/2)".Integrate("x")` | `sin(y)/(b^(5/2) d)`, wrong for a negative `b` where the secant is negative | `sin(y)/(b^(5/2) d) provided b > 0` |
| `"sqrt(a-a*sin(x)^2)*tan(x)^6".Integrate("x")` | left unevaluated | `sqrt(a)` times the antiderivative of `sqrt(1 - sin(x)^2) tan(x)^6`, `provided a > 0` |
| `"(a+a*sin(x))^(3/2)/(c+d*sin(x))^(5/2)".Integrate("x")` | left unevaluated | `a^(3/2)` times a closed form, `provided a > 0` |

`y` is `c + d x`. Rubi's 4.1.0, 4.1.2, 4.1.7, 4.2.0, 4.3.0 and 4.5.0: family 4 goes from 290
to 309 of 422 with five fewer timeouts, and family 6 from 377 to 378; no row is lost in families
1, 4, 5, 6 or 7, and the 1774-problem suite stays at 1707 with no wrong answer.

### A power of `a cos + b sin` with symbolic coefficients is integrated under the tangent

`sec(x)^2/(a cos(x) + b sin(x))^4` was left as written after ten minutes, where
`sec(x)^2/(2 cos(x) + 3 sin(x))^4` took a tenth of a second. Both are quotients of
homogeneous polynomials in the sine and cosine, which `t = tan(x)` turns into a rational
function, and the rule that does it read the denominator by expanding it first: with numbers
the expanded quartic in `t` is `(2 + 3t)^4` again as soon as the rational integrator factors
it, and with symbols it is `a^4 + 4a^3 b t + 6a^2 b^2 t^2 + 4a b^3 t^3 + b^4 t^4`, which nothing
factors back. A whole power of a homogeneous factor is now read as that power of it, so the
substitution hands on `(1 + t^2)^2/(a + b t)^4`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sec(x)^2/(a*cos(x)+b*sin(x))^4".Integrate("x")` | left unevaluated | a rational function of `tan(x)` |
| `"cos(x)/(a*cos(x)+b*sin(x))^3".Integrate("x")` | left unevaluated | a rational function of `tan(x)` |

Rubi's 4.7.2, `trig^m (a trig + b trig)^n`: 199 to 215 of 290, no row lost, 27 timeouts to 20.

### An odd power of `a cos + b sin` is rotated into one cosine

`1/(a cos(x) + b sin(x))^3` was left as written. The tangent turns a quotient of homogeneous
polynomials in the sine and cosine into a rational function only when the degrees differ by an
even number; by an odd number it leaves a root of `1 + t^2`, and every row of that kind was
declined. `a cos(y) + b sin(y)` is `R cos(u)` for `R = sqrt(a^2 + b^2)` and a shifted argument
`u`, so the quotient becomes one over a power of one cosine, with `cos(y)` and `sin(y)` above it
written as `(a cos(u) - b sin(u))/R` and `(b cos(u) + a sin(u))/R` -- shapes the rules for powers
of the sine and cosine answer at once. The way back goes through `cos(u) = (a cos(y) + b sin(y))/R`
and `sin(u) = (a sin(y) - b cos(y))/R`, not through the phase, whose quadrant would depend on the
signs of `a` and `b`.

Only where the sum is below the bar and nothing else there holds `x`, and only for sines and
cosines: a secant above puts a cosine below, which the rotation turns into one more such sum,
and those rows want partial fractions over the two linear factors instead.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(a*cos(x)+b*sin(x))^3".Integrate("x")` | left unevaluated | in `a cos(x) + b sin(x)`, `a sin(x) - b cos(x)` and `sqrt(a^2 + b^2)` |
| `"cos(x)/(a*cos(x)+b*sin(x))^2".Integrate("x")` | left unevaluated | the same, with a logarithm |
| `"1/(2*cos(x)+3*sin(x))^3".Integrate("x")` | left unevaluated | the same with `sqrt(13)` |

Rubi's 4.7.2: 215 to 237 of 290, no row lost, 20 timeouts to 16.

### `A cos + i A sin` below the bar is written as the exponential it is

`cos(x)^2/(a cos(x) + i a sin(x))^3` was declined after twelve seconds. `A cos(y) + i A sin(y)` is
`A e^(i y)`, and `A cos(y) - i A sin(y)` is `A e^(-i y)`; written so, the integrand is an
exponential times a power of a cosine, which the closed rules answer at once. As a sum of a
cosine and a sine it has no real rotation -- `A^2 + (i A)^2` is zero -- so the rule for
`a cos + b sin` has nothing to do with it. A whole power of the product written is split,
`(A e^(i y))^n` as `A^n e^(i n y)`, since the rule that distributes such powers takes a symbolic
constant and leaves a number: `cos(x)^2/(2 e^(i x))^3` was declined where
`cos(x)^2/(a e^(i x))^3` was not.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"cos(x)^2/(a*cos(x)+i*a*sin(x))^3".Integrate("x")` | left unevaluated | exponentials of `-3 i x` times `cos(2x)`, `sin(2x)` |
| `"1/(a*cos(x)+i*a*sin(x))^3".Integrate("x")` | left unevaluated | `a^(-3) e^(-3 i x)/(-3 i)` |

A secant above the bar is still declined: `sec(x)^2 e^(-3 i x)` is rational in `e^(i x)` with an
imaginary rate, which nothing answers yet. Rubi's 4.7.2: 237 to 249 of 290, no row lost, 16
timeouts to 14.

### A power of a constant multiple of the radicand `arcosh` holds is written over that radicand

`arcosh(a x)^2/sqrt(1 - a^2 x^2)` and `x (a + b arcosh(c x))/(d - c^2 d x^2)^3` ran out of time.
`arcosh(c x)` is `ln(c x + sqrt(c^2 x^2 - 1))`, and the substitution `u = arcosh(c x)` wants the
root its derivative carries, `sqrt(c^2 x^2 - 1)`; beside it the integrand holds a constant multiple
of that radicand, `d - c^2 d x^2 = -d (c^2 x^2 - 1)`, which nothing read as one. A whole power of
the multiple is now the product of the powers, and a root `(lambda M)^(k/2)` is `K^k M^(k/2)` with
`K = sqrt(lambda M)/sqrt(M)` -- plus or minus `i`, or `sqrt(lambda)`, constant on every interval
where it is defined -- which stands in front of the answer the way Rubi writes it. The answer
is checked where the integrand is real as well as where it is not: `arcosh(2 x)^2/sqrt(1 - 4 x^2)`
is real on `(-1/2, 1/2)`, where `arcosh(2x)` is `i arccos(2x)`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"arcosh(a*x)^2/sqrt(1-a^2*x^2)".Integrate("x")` | left unevaluated | `arcosh(a x)^3/(3a)` over `sqrt(1 - a^2 x^2)/sqrt(a^2 x^2 - 1)` |
| `"x*(a+b*arcosh(c*x))/(d-c^2*d*x^2)^3".Integrate("x")` | left unevaluated | in `arcosh(c x)` and powers of `c^2 x^2 - 1` |

Rubi's 7.2.4 and 7.2.5, all 277 problems that count: 127 to 255, no row lost, 121 timeouts to 18.

### Two linear factors whose product is a multiple of the radicand `arsinh` holds are written as that radicand

`(a + b arsinh(c x)) sqrt(d + i c d x) sqrt(f - i c f x)` ran out of time. The two linear factors
multiply to `d f (1 + c^2 x^2)`, a constant multiple of the radicand `arsinh(c x) =
ln(c x + sqrt(c^2 x^2 + 1))` holds -- the entry above's case, spelled as two factors. `L1^p L2^q`
with `p - q` whole is now `L1^(p - q) L1^q L2^q`, and `L1^q L2^q` is `K^k M^(k/2)` for `q = k/2`,
with `K = sqrt(L1) sqrt(L2)/sqrt(M)` in front of the answer, constant wherever it is defined.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(a+b*asinh(c*x))*sqrt(d+i*c*d*x)*sqrt(f-i*c*f*x)".Integrate("x")` | left unevaluated | in `arsinh(c x)` and `1 + c^2 x^2`, with `K` in front |
| `"(d+i*c*d*x)^(5/2)*(a+b*asinh(c*x))*sqrt(f-i*c*f*x)".Integrate("x")` | left unevaluated | the same, with `(d + i c d x)^2` beside it |

Rubi's 7.1.4 and 7.1.5, all 376 problems that count: 308 to 345, no row lost, 56 timeouts to 27.

### The radicand an inverse sine or cosine holds is read as one

`(d + c d x)^(1/2) (f - c f x)^(3/2) (a + b arcsin(c x))` ran out of time. `arcsin(u)` holds a
radicand without writing it -- its derivative is `u'/sqrt(1 - u^2)` -- so beside `arcsin(c x)`
the factors `d + c d x` and `f - c f x`, whose product is `d f (1 - c^2 x^2)`, are the case of the
two entries above with `1 - c^2 x^2` for the radicand. That radicand is now read from an inverse
sine or cosine of a linear, and two factors whose product is the radicand itself -- `sqrt(1 + c x)
sqrt(1 - c x)` beside `arccos(c x)` -- are written as its one root too. A single base that is a
multiple of it, `(d - c^2 d x^2)^(3/2)`, is written over it the same way, with
`K = sqrt(d - c^2 d x^2)/sqrt(1 - c^2 x^2)` in front, which is `sqrt(d)` for a positive `d`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(d+c*d*x)^(1/2)*(f-c*f*x)^(3/2)*(a+b*asin(c*x))".Integrate("x")` | left unevaluated | in `arcsin(c x)` and `1 - c^2 x^2`, with `K` in front |
| `"(d-c^2*d*x^2)^(3/2)*(a+b*asin(c*x))".Integrate("x")` | left unevaluated | the same |

Rubi's 5.1.4, 5.1.5, 5.2.4 and 5.2.5, all 504 problems that count: 405 to 463, no row lost, 77
timeouts to 19.

### The special functions are integrated by parts, and so is what leads to one

Integration by parts differentiates each of the nine special functions now, where it meets one
beside a power of `x`, as it does a logarithm or an inverse trigonometric function: each has an
elementary derivative. `Ei`, `li`, `Si`, `Ci`, `Shi` and `Chi` of a linear argument are integrated
alone, against 1, as `erf` already was
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)). An integrand holding one of
these functions had no reading in 2.5.0, which the entries for the functions themselves record. An
elementary integrand whose antiderivative is reached through one is answered where it was not:

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"(a+b*ln(c*x^n))/(x^2*(d+e*ln(f*x^m)))".Integrate("x")`, Rubi's 3.1.5 row 213 | `integral(...)` | an antiderivative in `Ei`, provided `f > 0` and `e^d f^e > 0` |

### A negative power of the variable is not a polynomial to integration by parts

`MathS.TryPolynomial` reads `x^(-1)` as a monomial of degree -1, and integration by parts against a
polynomial differentiated it until it reached `0`, which a negative power never does:
`e^(2x) x^(-1)` ran the process out of memory, where `e^(2x)/x` is `Ei(2x)`
([#1646](https://github.com/asc-community/AngouriMath/issues/1646)). By parts takes a polynomial
with no negative degree now, and past it, the power is the factor differentiated, which leaves the
quotient the exponential integral rules read. Only what ran away or ran out of time changes: Rubi's
independent suites and its families 2, 4 and 8 are answered as they were.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(2*x)*x^(-1)".Integrate("x")` | out of memory: `OutOfMemoryException` under a 2 GB heap | `1 / x * e ^ (2 * x) / 2 - -1/2 * (-1 / x * e ^ (2 * x) + 2 * Ei(2 * x)) + C`, which is `Ei(2 * x) + C` |
| `"e^x*x^(-2)".Integrate("x")` | `integral(e ^ x * x ^ (-2), x)` | `e ^ x * -1 / x - -Ei(x) + C` |

### A special function beside its derivative is a substitution

`e^(c - b^2 x^2) erf(b x)^n`, `Ei(b x) e^(b x)/x` and `Si(b x) sin(b x)/x` are each a power of a
special function beside its derivative, and `u = erf(b x)`, `u = Ei(b x)` or `u = Si(b x)` writes
them as a power of `u`. None of the nine special functions was a candidate for that substitution.
Each is one now, as the logarithm is, wherever its derivative can be the differential
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)). An integrand holding one of
these functions had no reading in 2.5.0, which the entries for the functions themselves record, and
no integrand without one changes: Rubi's independent suites and a sample of its families 1 to 7,
2726 problems, are answered alone as they were.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(c - b^2*x^2)*erf(b*x)".Integrate("x")` | `UnrecognizedFunctionParseException`: there is no function `erf` | `e ^ c * pi ^ (1/2) * erf(b * x) ^ 2 / 2 / (2 * b) + C` |
| `"e^(b*x)*Ei(b*x)/x".Integrate("x")` | `Ei * e ^ (b * x) + C`, with `Ei` a variable | `Ei(b * x) ^ 2 / 2 + C` |
| `"Si(b*x)*sin(b*x)/x".Integrate("x")` | `Si * -cos(b * x) + C`, with `Si` a variable | `Si(b * x) ^ 2 / 2 + C` |

### A square of a special function, and one beside a power of `x` and its elementary derivative, are integrated by parts

`x Si(b x) sin(b x)` was left unevaluated where `Si(b x) sin(b x)` was not: by parts it is
`Si(b x)` against `x sin(b x)`, whose integral needs parts of its own, and the integral of that
factor was taken with parts off. It is integrated by the polynomial's parts now, beside a special
function of a linear argument, and what is left, `sin(b x)/x` times sines and cosines, by parts in
turn. A square of one of `b x` is two rounds of parts, and what the first round leaves comes back
as one product over a sum -- `(b x Ei(b x) - e^(b x)) e^(b x)/(b x)` for `Ei(b x)^2` -- that no rule
reads whole: its terms are asked one at a time now
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)). An integrand holding one of
these functions had no reading in 2.5.0, which the entries for the functions themselves record, and
no integrand without one changes: Rubi's independent suites and a sample of its families 1 to 7,
2726 problems, are answered alone as they were.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x*Si(b*x)*sin(b*x)".Integrate("x")` | a polynomial in a variable `Si` | an antiderivative in `Si(b x)`, `Ci(2 b x)` and `ln(x)` |
| `"Ei(b*x)^2".Integrate("x")` | `Ei * (b * x) ^ 3 / 3 / b + C`, with `Ei` a variable | an antiderivative in `Ei(b x)` and `Ei(2 b x)` |
| `"x*erf(b*x)^2".Integrate("x")` | `UnrecognizedFunctionParseException`: there is no function `erf` | an antiderivative in `erf(b x)` |

### A constant of integration is matched to a linear divisor with a symbol in it

By parts against a polynomial chooses the polynomial's antiderivative so that what the other
factor's derivative divides by divides it too, and that was done only where the division left a
number over. `a + b x` leaves a symbol: `x^2/2` over `a + b x` leaves `a^2/(2 b^2)`, so the
remainder of `x Shi(a + b x)^2` kept `x^2/(a + b x)` and nothing read it. The antiderivative less
its value at `-a/b` is taken now, written as `a + b x` times the quotient, so that the linear
cancels in the remainder ([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).
`x Shi(a + b x)^2`, `x Chi(a + b x)^2` and `x Ei(a + b x)^2` are answered. An integrand holding one of
these functions had no reading in 2.5.0, which the entries for the functions themselves record, and
no integrand without one changes: Rubi's independent suites and a sample of its families 1 to 7
are answered alone as they were. Where by parts already answered a special function of `a + b x`
beside a power of `x`, the answer is written with the same constant: `x Ei(a + b x)` now reads
`Ei(a + b x) (a + b x)(x/(2b) - a/(2b^2)) - ...`, the same function as before.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x*Shi(a+b*x)^2".Integrate("x")` | `a ^ 2 * Shi * x ^ 2 / 2 + a * b * 2 * Shi * x ^ 3 / 3 + b ^ 2 * Shi * x ^ 4 / 4 + C`, with `Shi` a variable | an antiderivative in `Shi(a + b x)` and `Ei(2 (a + b x))`, `Ei(-2 (a + b x))` |
| `"x*Ei(a+b*x)^2".Integrate("x")` | `a ^ 2 * Ei * x ^ 2 / 2 + a * b * 2 * Ei * x ^ 3 / 3 + b ^ 2 * Ei * x ^ 4 / 4 + C`, with `Ei` a variable | an antiderivative in `Ei(a + b x)` and `Ei(2 (a + b x))` |

### A square of a special function of a shifted argument is integrated by parts

The remainder of a special function's square was asked term by term only for an argument `b x`.
For `a + b x`, the remainder divided by the linear until the constant of integration was matched to
it; with that matched, the terms are the case without the offset, and the asking is offered for any
linear argument ([#1501](https://github.com/asc-community/AngouriMath/issues/1501)). The exception
is an error function beside anything but itself, whose derivative is a Gaussian of the shifted
argument, so a polynomial beside it stays in every term: `(c + d x) erf(a + b x)^2` is still
declined. An integrand holding one of these functions had no reading in 2.5.0, which the entries for
the functions themselves record, and no integrand without one changes: Rubi's independent suites and
a sample of its families 1 to 7 are answered alone as they were.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"Shi(a+b*x)^2".Integrate("x")` | `Shi * (a + b * x) ^ 3 / 3 / b + C`, with `Shi` a variable | an antiderivative in `Shi(a + b x)`, `Ei(2 (a + b x))` and `Ei(-2 (a + b x))` |
| `"x^2*Ei(a+b*x)^2".Integrate("x")` | `a ^ 2 * Ei * x ^ 3 / 3 + a * b * 2 * Ei * x ^ 4 / 4 + b ^ 2 * Ei * x ^ 5 / 5 + C`, with `Ei` a variable | an antiderivative in `Ei(a + b x)` and `Ei(2 (a + b x))` |
| `"erf(a+b*x)^2".Integrate("x")` | `UnrecognizedFunctionParseException`: there is no function `erf` | an antiderivative in `erf` |

### A logarithm of `x` is matched to a logarithm of a multiple of `x` below the bar

By parts on `li(b x)/x` takes the antiderivative of `1/x`, and with `ln(x)` the remainder was
`b ln(x)/ln(b x)`, which nothing read. `ln(b x)` is as much an antiderivative of `1/x`, and taken so,
the logarithm the derivative of `li(b x)` divides by cancels: `li(b x)/x` is `li(b x) ln(b x) - b x`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)). `li` had no reading in 2.5.0,
which its own entry records, and no integrand without it changes: Rubi's independent suites, its
family 3 at twenty a file and a sample of the others are answered alone as they were.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"li(b*x)/x".Integrate("x")` | `li * b * x + C`, with `li` a variable | `li(b * x) * ln(b * x) - b * x + C` |

### A special function of a logarithm of a monomial is integrated, through `Ei` of a complex argument

`(e x)^m Si(d (a + b ln(c x^n)))` is by parts against `(e x)^m`, and what is left is a power of `x`
times `sin(d (a + b ln(c x^n)))/(a + b ln(c x^n))`. Two rules were missing on the way
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)):

- **A function of one logarithm of a monomial.** A power of `x` times `G(ln(c x^n))` is integrated
  under `t = ln(c x^n)`, with `x^(m + 1)` written as `K e^((m + 1) t/n)` and
  `K = x^(m + 1) (c x^n)^(-(m + 1)/n)`, whose derivative is 0 wherever it is defined. That is an
  antiderivative wherever the integrand is real. For an even `n` that includes negative `x`, where
  `ln(c x^n)` is not `ln(c) + n ln(x)`. By parts leaves `(d x)^m x/ln(b x)` of `(d x)^m li(b x)`,
  and this rule answers it with `Ei((m + 2) ln(b x))`.
- **An exponential beside a sine or cosine, over linears.** Each sine and cosine is written as
  exponentials, and each term is the exponential integral of a complex argument. The two conjugate
  terms add up to a real value.

Both columns measured on a build, `v2.5.0` against this change.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(2*x)*sin(x)/x".Integrate("x")` | `integral(e ^ (2 * x) * sin(x) / x, x)` | `(-1/2 * i) * Ei((2 + i) * x) + 1/2 * i * Ei((2 - i) * x) + C` |
| `"x*sin(ln(x))/ln(x)".Integrate("x")` | `integral(x * sin(ln(x)) / ln(x), x)` | `(-1/2 * i) * Ei((2 + i) * ln(x)) + 1/2 * i * Ei((2 - i) * ln(x)) + C` |
| `"cos(a+b*ln(c*x^n))^2".Integrate("x")` | `integral(cos(a + b * ln(c * x ^ n)) ^ 2, x)` | an antiderivative in `sin` and `cos` of `2 (a + b ln(c x^n))`, beside `x (c x^n)^(-1/n)` |

### Exponentials below the bar beside a power of a linear are integrated to the exponential integral

**Answers where there were none.** `1/(x e^(2x))` was declined where `e^(-2x)/x` was answered, and so
were `1/((c + d x)(a + a tanh(e + f x)))` and its powers, Rubi's 6.3.1, and the same with `coth`, 6.4.1:
the rules for an exponential over a linear read it above the bar. Written in the exponential every
other one is a whole power of, exponentials whose denominator is then a power of that one alone are a
sum of its powers -- `1/(1 + tanh(z))` is `(1 + e^(-2z))/2` -- and each term over the linear is an
exponential integral. Where anything else stays below the bar, `1/(x (1 + e^x))`, the integral is not
of this kind and is declined as before ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(x*exp(2*x))".ToEntity().Integrate("x")` | `integral(...)` | `Ei(-2 x)` |
| `"1/(x*(1 + tanh(x)))".ToEntity().Integrate("x")` | `integral(...)` | `ln(x)/2 + Ei(-2 x)/2` |
| `"1/((c + d*x)*(a + a*tanh(e + f*x)))".ToEntity().Integrate("x")` | `integral(...)` | a logarithm and an exponential integral |
| `"1/((c + d*x)^2*(a + a*coth(e + f*x))^3)".ToEntity().Integrate("x")` | `integral(...)` | powers of `c + d x` and exponential integrals |

### An exponential or a hyperbolic function over several linears is split into partial fractions over them

`e^x/(x (x + 1))` was left unevaluated, where `e^x/x` and `e^x/(x + 1)` were each answered with
the exponential integral: the rule for an exponential of a linear over a power of a linear read
one linear below the bar, and the rule for `sinh` and `cosh` did the same. The quotient is split
into partial fractions over its linears now, as the rule for a sine or a cosine already split it,
and each term is the one-linear question: `e^x/(x (x + 1))` is `Ei(x) - Ei(x + 1)/e`. It is also
what by parts leaves of `Ei(a + b x)/x^2`, `e^(a + b x)/((a + b x) x)`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)). The rule for a sine or a cosine
declined a polynomial over linears with a symbol among their coefficients, `x sin(x)/((c + d x)(x - 2))`,
for dividing a fraction that was proper already; neither rule divides one now. Both columns
measured on a build, `v2.5.0` against this change.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^x/(x*(x+1))".Integrate("x")` | `integral(e ^ x / (x * (x + 1)), x)` | `Ei(x) + -1 / e * Ei(x + 1) + C` |
| `"sinh(x)/(x*(x+1))".Integrate("x")` | `integral((e ^ x - e ^ (-x)) / 2 / (x * (x + 1)), x)` | `1/2 * (2 * Shi(x) + ((-1) / e + e) * Chi(x + 1) + ((-1) / e - e) * Shi(x + 1)) + C` |
| `"x^3*e^(2*x)/((x+1)*(x-2))".Integrate("x")` | `integral(x ^ 3 * e ^ (2 * x) / ((x + 1) * (x - 2)), x)` | `e ^ (2 * x) * (-1/4 + 1/2 * x) + e ^ (2 * x) / 2 + 1/3 * e ^ (-2) * Ei(2 * (x + 1)) + 8/3 * e ^ 4 * Ei(2 * (x - 2)) + C` |
| `"x*sin(x)/((c+d*x)*(x-2))".Integrate("x")` | `integral(x * sin(x) / ((c + d * x) * (x - 2)), x)` | in `Si` and `Ci` of `(c + d x)/d` and `x - 2` |

### An exponential, hyperbolic or trigonometric function over a quadratic or a binomial is split at its roots

**Answers where there were none.** `sin(c + d x)/(a + b x^2)` was answered, the rule for a sine or a
cosine writing the quadratic as its leading coefficient times the linears of its two roots, and
`cosh(c + d x)/(a + b x^2)` was declined, as was `e^x/(x^2 + x + 1)`: the rule for an exponential or
`sinh` and `cosh` over several linears read linears only. It writes a quadratic the same way now,
and both rules write a binomial `a + b x^3` or `a + b x^4` as the linears of its roots,
`(a/b)^(1/n)` times the n-th roots of -1. The roots are complex where the discriminant is negative,
and the integrals at conjugate arguments add up to a real answer. An exponential that is not of a linear is not split:
`sinh(sqrt((1 - a x)/(1 + a x)))/(1 - a^2 x^2)` is still answered whole, by its substitution. And
both rules divided a numerator past the degree by the linears of the roots, which with a symbol in
the quadratic left a remainder in the squares of its roots that nothing read: `x^2 sin(c + d x)/(a + b x^2)`
was declined. They divide by the denominator as it is written now. Rubi's 6.2.2,
`(e x)^m (a + b x^n)^p cosh(c + d x)`, its 4.1.11, and exponentials in its 2.3
([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"cosh(c + d*x)/(a + b*x^2)".ToEntity().Integrate("x")` | `integral(...)` | `Chi` and `Shi` of `d (x -/+ sqrt(-4 a b)/(2 b))` |
| `"x*sinh(c + d*x)/(a + b*x^2)".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"cosh(c + d*x)/(x*(a + b*x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same, and `Chi(d x)` and `Shi(d x)` |
| `"x^3*cosh(c + d*x)/(a + b*x^2)".ToEntity().Integrate("x")` | `integral(...)` | `e^(c + d x)` and `e^(-c - d x)` times linears, and the same |
| `"cosh(x)/(x^2 - 2)".ToEntity().Integrate("x")` | `integral(...)` | `Chi` and `Shi` of `x -/+ sqrt(2)` |
| `"e^(c + d*x)/(a + b*x^2)".ToEntity().Integrate("x")` | `integral(...)` | `Ei` of `d (x -/+ sqrt(-4 a b)/(2 b))` |
| `"e^x/(x^2 + x + 1)".ToEntity().Integrate("x")` | `integral(...)` | `Ei` of `x + (1 -/+ sqrt(-3))/2` |
| `"x^2*sin(c + d*x)/(a + b*x^2)".ToEntity().Integrate("x")` | `integral(...)` | `cos(d x)` and `sin(d x)`, and `Si` and `Ci` of `d (x -/+ sqrt(-4 a b)/(2 b))` |
| `"sin(c + d*x)/(a + b*x^3)".ToEntity().Integrate("x")` | `integral(...)` | `Si` and `Ci` of `d` times `x` less each root of `a + b x^3` |
| `"cosh(c + d*x)/(a + b*x^3)".ToEntity().Integrate("x")` | `integral(...)` | `Chi` and `Shi` the same |
| `"e^x/(a + b*x^4)".ToEntity().Integrate("x")` | `integral(...)` | `Ei` of `x` less each root of `a + b x^4` |

### An inverse trigonometric function below the bar is integrated to the sine and cosine integrals

`1/arcsin(x)` was left unintegrated. Under the substitution that undoes the inverse function,
`x = sin(u)` for `arcsin(x)`, `x^m/(a + b arcsin(c x))^n` is a polynomial in the sine and the cosine
over a power of `a + b u`, which the sine and cosine integrals answer. That substitution was taken
only where it removed a radical, an exponential of the inverse function or a power of it, and is
now taken where the inverse function, or a function of it alone, is below the bar, before the
general substitution search, which does not find it. The tangent, cotangent, secant and cosecant it
leaves are read as quotients of the sine and the cosine
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/asin(x)".Integrate("x")` | `integral(1 / arcsin(x), x)` | `Ci(arcsin(x)) + C` |
| `"x/asin(x)".Integrate("x")` | `integral(x / arcsin(x), x)` | `1/2 * Si(2 * arcsin(x)) + C` |
| `"1/(a+b*asin(c*x))".Integrate("x")` | `integral(1 / (a + b * arcsin(c * x)), x)` | `Si` and `Ci` of `(a + b arcsin(c x))/b` |
| `"x/((c+a^2*c*x^2)^2*atan(a*x))".Integrate("x")` | `integral(x / ((c + a ^ 2 * c * x ^ 2) ^ 2 * arctan(a * x)), x)` | `1/2 * Si(2 * arctan(a * x)) / (a ^ 2 * c ^ 2) + C` |

### The arcsecant beside a root of `x^2 - 1` is integrated right below `-1` too

**Answers where there were none.** Under `x = sec(u)`, the substitution that undoes the arcsecant,
`sqrt(x^2 - 1)` is `|tan(u)|`, and on the arcsecant's range the tangent has the sign of `x`. It was
written `tan(u)`, so that on the unreleased master `x arcsec(x)/sqrt(x^2 - 1)` and its kin were
answered right for `x > 1` and with the integrand's negative for a derivative at every `x < -1`,
where they are as real; 2.5.0 declined them. The root is `tan(u) sgn(x)` now, as the cosecant's and
the cotangent's carry their signs, and a first power of the arcsecant beside it is by parts. The sign
written back is the argument's, which is that of `x` only for a positive slope and no constant:
`arcsec(x + 3)` has the sign of `x + 3` ([#718](https://github.com/asc-community/AngouriMath/issues/718)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x*asec(x)/sqrt(-1 + x^2)".ToEntity().Integrate("x")` | `integral(...)` | right for `x > 1` and for `x < -1` |
| `"asec(x)/(x^2*sqrt(-1 + x^2))".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"asec(x)^2/(x*sqrt(x^2 - 1))".ToEntity().Integrate("x")` | `integral(...)` | the same |
| `"asec(x + 3)^2/((x + 3)*sqrt((x + 3)^2 - 1))".ToEntity().Integrate("x")` | `integral(...)` | right for `x > -2` and for `x < -4` |

### A perfect square in a power of the variable is read as one

`sqrt(a^2 + 2 a b x^2 + b^2 x^4) sqrt(c + e x + d x^2)` was left unevaluated. The radicand is
`(a + b x^2)^2`, so its root is the modulus `|a + b x^2|`, but the rule that reads a root of a
perfect square read only a quadratic in `x`. It now reads the same quadratic in a power of `x`,
`a x^(2k) + b x^k + c`, and puts `sgn(x^k + h)` in front of the integral, as it does for `k = 1`.
There is no sign where `k` is even and `h` is a positive number, since `x^k + h` is then
positive. The shift `h` is simplified where it holds a symbol: `2 a b/(2 b^2)` is `a/b`, for
`k = 1` as well.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(c+pe*x+d*x^2)*sqrt(a^2+2*a*b*x^2+b^2*x^4)".Integrate("x")` | left unevaluated | the antiderivative, with `sgn(x^2 + a/b) sqrt(b^2)` in front |
| `"x/sqrt(a^2+2*a*b*x^3+b^2*x^6)".Integrate("x")` | left unevaluated | logarithms and an arctangent in `(a/b)^(1/3)`, with `sgn(x^3 + a/b)` in front |
| `"(1+2*x^2+x^4)^(3/2)".Integrate("x")` | left unevaluated | `x + 3 x^3/3 + 3 x^5/5 + x^7/7`, with no sign |

Rubi's 1.2.2.2, 1.2.2.4, 1.2.2.7 and 1.2.3.2, the 574 problems that count with a perfect square
written with symbols: 182 to 388, no row lost, 19 timeouts to 9.

### A linear below the bar that divides the radicand is written over it

`x^2/((d + e x) sqrt(d^2 - e^2 x^2))` was left unevaluated. The linear divides the radicand,
`d^2 - e^2 x^2 = (d + e x)(d - e x)`, so `1/(d + e x)` is `(d - e x)/(d^2 - e^2 x^2)`, and the
integrand is the polynomial `x^2 (d - e x)` over `(d^2 - e^2 x^2)^(3/2)`, which the rules for a
polynomial beside a half-odd power of a quadratic answer. The rules that read a linear beside a
root take it as a pole to split off, and this one has nothing to split off: its residue is the
radicand at its root, zero. A whole power `L^(-n)` below the bar, where `P = L M`, is now
`M^n P^(-n)`, exactly, for the question as asked.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2/((d+pe*x)*sqrt(d^2-pe^2*x^2))".Integrate("x")` | left unevaluated | the antiderivative |
| `"x^2*sqrt(1-a^2*x^2)/(1-a*x)^5".Integrate("x")` | left unevaluated | the antiderivative, in `x/sqrt(1 - a^2 x^2)` |
| `"sqrt(a*d*pe+(c*d^2+a*pe^2)*x+c*d*pe*x^2)/(x*(d+pe*x))".Integrate("x")` | left unevaluated | the antiderivative |

Rubi's 1.2.1.4, all 632 problems that count: 250 to 409, no row lost, 61 timeouts to 67. The six
new timeouts have a radicand of four symbols beside a power of `x`, and went from a quick decline
to one past the budget.

### Two proportional linears under roots are read as one radical

`sin(a + b (c + d x)^(1/3))/(c e + d e x)^(1/3)` ran past two minutes. The two linears under the
roots are proportional, `c e + d e x = e (c + d x)`, and the rule for roots of two different
linears, which rationalises by `t = ((a x + b)/(c x + d))^(1/q)`, has nothing to rationalise when
their determinant `a d - b c` is zero. A power of the second is now written as the ratio's power
times the same power of the first, the generic reading of a root of a product, and the integrand
is the one-linear question; a ratio known to be negative under an even root is not taken, since
there the two differ by a sign
([#1386](https://github.com/asc-community/AngouriMath/issues/1386)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sin(a+b*(c+d*x)^(1/3))/(c*g+d*g*x)^(1/3)".Integrate("x")` | ran past two minutes | `3 ((c + d x)^(1/3) (-cos(a + b (c + d x)^(1/3)))/b + sin(a + b (c + d x)^(1/3))/b^2)/(d g^(1/3))` |

### A whole power of a perfect square in a power of the variable is written as a power of its root

`1/(a^2 + 2 a b x^2 + b^2 x^4)^2` was left unevaluated. The quartic is `b^2 (x^2 + a/b)^2`, so a
whole power of it is `(b^2)^n (x^2 + a/b)^(2n)`, exactly and with no sign, and the rational
integrator reads a power of `x^2 + a/b` where it declined the quartic. It is written so at the
question asked, for a square written out as a sum with a symbol in it and `x` below the bar. It
is not written so in a polynomial, or where the square is already written as a power. Answers
that came out in the expanded square before now come out in `x^2 + a/b`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(a^2+2*a*b*x^2+b^2*x^4)^2".Integrate("x")` | left unevaluated | the antiderivative, in `x^2 + a/b` |
| `"x/(a^2+2*a*b*x^2+b^2*x^4)^3".Integrate("x")` | a piecewise on `b^2 = 0`, in `2 b^2 x^2 + 2 a b` | `-(x^2 + a/b)^(-5)/(10 (b^2)^3)` |
| `"x^2*(a^2+2*a*b*x^3+b^2*x^6)^(-2)".Integrate("x")` | a piecewise on `b^2 = 0`, in `2 b^2 x^3 + 2 a b` | `-(b^2)^(-2) b^3/(9 (a + b x^3)^3)` |

Rubi's 1.2.2.2, 1.2.2.4, 1.2.2.7 and 1.2.3.2, the 574 problems that count with a perfect square
written with symbols: 388 to 409, no row lost.

### The integrator's check evaluates in intervals

The integrator accepts a candidate answer when its derivative agrees with the integrand at a few
points, with the symbols pinned to numbers. It evaluated both sides in hundred-digit decimals with
the downcasting off. There, a decimal below `1e-16` divides as zero and one below about `1e-199`
is zero, so a correct answer whose derivative passed through such a value was turned away and a
later route answered instead. The check now evaluates in intervals, which have neither limit and
read no setting ([#1019](https://github.com/asc-community/AngouriMath/issues/1019), item 10).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(c*(a+b*x))*(cosh(a*c+b*c*x)^2)^(3/2)".Integrate("x")` | left unevaluated | the antiderivative, in `tanh(a c + b c x)` -- where `master` between the releases gave it in exponentials, the route the old check allowed |

Rubi's suites against `master`: the 1774-problem suite, 4.7.2, 4.3.10 and 4.4.10, 7.1.4 to 7.2.5,
and samples of families 1, 4, 5, 6 and 7. That is 4056 problems, with one answer changed (the row
above). One problem is lost to the 5 s budget: `(c + d x^2)^(5/2)/(x^2 (a + b x^2)^2)`, which
`master` solved in 5.3 s, is now answered correctly by a slower route that the old check turned
away.

### A quantifier over the whole numbers reads the bounds in its hypothesis, and residues written with `mod`

`forall k in ZZ : (0 < k and k < 7) implies 7 divides binomial(7, k)` was left as written, because no
route read a bound on the name out of an implication's hypothesis. The conjuncts of the hypothesis
that mention only the name, and that the solver reads, now cut the set down. Where the cut is
listed, or is the whole numbers from a least one, the statement is decided over it, with the other
conjuncts kept. `exists` and `exists!` read their conjunction the same way. And
`forall a in ZZ : a^7 mod 7 = a mod 7` was left as written while `a^7 = a (mod 7)` was decided. An
equation or an order comparison between residues `p mod m` of polynomials with whole coefficients
repeats with the moduli, since `mod` is the floored remainder, so the residues decide it too.
These are Sullivan and Mackey's Prob 8.9.24–26 at a given prime
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)). Quantifiers were a parse error
in 2.5.0.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall k in ZZ : (0 < k and k < 7) implies 7 divides binomial(7, k)` | `UnhandledParseException` | `True` |
| `exists! k in ZZ : (0 < k and k < 7) and 3 divides k and 2 divides k` | `UnhandledParseException` | `True` |
| `forall n in ZZ : n >= 5 implies 2^n > n^2` | `UnhandledParseException` | `True` |
| `forall a in ZZ : a^7 mod 7 = a mod 7` | `UnhandledParseException` | `True` |
| `forall a, b in ZZ : (a + b)^5 mod 5 = (a^5 + b^5) mod 5` | `UnhandledParseException` | `True` |

### A quantifier's facts are read inside its body

`forall p in PP : forall k in ZZ : (0 < k and k < p) implies p divides binomial(p, k)` was left as
written. The claim holds because `p` is prime and `0 < k < p`, and only the quantifiers around it
say so. A quantifier now hands those facts down while it decides its body: the set its name ranges
over, and, while the claim is simplified, the conjuncts of the hypothesis. The rule for
`p divides binomial(p, k)` reads them. Nothing decided with them leaves the quantifier:
`p divides binomial(p, k)` on its own is still left as written. This is Sullivan and Mackey's
Prob 8.9.24 for every prime ([#1409](https://github.com/asc-community/AngouriMath/issues/1409)).
Quantifiers were a parse error in 2.5.0.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall p in PP : forall k in ZZ : (0 < k and k < p) implies p divides binomial(p, k)` | `UnhandledParseException` | `True` |
| `forall k in ZZ : forall p in PP : (0 < k and k < p) implies p divides binomial(p, k)` | `UnhandledParseException` | `True` |
| `forall p in PP : forall k in ZZ+ : k < p implies p divides binomial(p, k)` | `UnhandledParseException` | `True` |

### Fermat's little theorem and the freshman's dream, for every prime

`forall p in PP : forall a in ZZ : a^p = a (mod p)` was left as written. Under a quantifier that
establishes `p` prime, `p divides d` now holds where writing each power `(u + v)^p` in `d` as
`u^p + v^p` leaves nothing: the binomial theorem adds only multiples of `binomial(p, k)` with
`0 < k < p`, each a multiple of `p`. A congruence modulo a symbol, or an equation of residues, over
a row of whole numbers holds where it holds at one member and the difference of its sides changes
by a multiple of the modulus from each member to the next. For Fermat's little theorem that change
is `(a + 1)^p - a^p - 1`. These are Sullivan and Mackey's Prob 8.9.25 and 8.9.26
([#1409](https://github.com/asc-community/AngouriMath/issues/1409)). Quantifiers were a parse error
in 2.5.0.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `forall p in PP : forall a in ZZ : a^p = a (mod p)` | `UnhandledParseException` | `True` |
| `forall p in PP : forall a in ZZ : a^p mod p = a mod p` | `UnhandledParseException` | `True` |
| `forall p in PP : forall a, b in ZZ : (a + b)^p = a^p + b^p (mod p)` | `UnhandledParseException` | `True` |

### An identity only the simplifier shows is solved to every value

`sqrt(x)^2 = x` was answered with the whole starting grid of Newton's method, 191 numbers. Nothing
analytical read it, and its residual is zero wherever it is evaluated, so every start converged at
once and each was kept. Where every analytical route has declined, the equation is now simplified
once before Newton's method is tried, and an unconditional zero is answered with `CC`, as
`x + 1 = x + 1` already was. A zero with a condition is not taken, and goes on to Newton's method as
before: `(x^2 - 1)/(x - 1) - x - 1` is zero for every `x` but 1, where `CC` would be wrong
([#1420](https://github.com/asc-community/AngouriMath/issues/1420)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sqrt(x)^2 = x".ToEntity().Solve("x")` | `{ -10, -49/5, -48/5, …, -8 - 8i, … }`, 191 numbers | `CC` |
| `"sin(x)^2 + cos(x)^2 = 1".ToEntity().Solve("x")` | the same 191 numbers | `CC` |

### The floors, the rounding and the extremes compile, and the solver's numerical search declines what it cannot represent

`floor`, `ceil`, `round`, `max` and `min` had no compiled form, and `max` and `min` had no
derivative. So `Compile` threw `UncompilableNodeException` on them, and so did `Solve`, whose last
resort, Newton's method, compiles the expression and its derivative. They compile now, through
`Compile` and `Compile<TIn, TOut>` alike, and they agree with the interpreter:

- `floor`, `ceil` and `round` are taken componentwise on a complex number, and `round` rounds a half
  to even;
- `max` and `min` of two numbers that differ and are not both real have no value, since those are
  not ordered;
- `max` and `min` differentiate where their arguments are real, as `|f|` does.

The search still declines what has no compiled form, so `binomial(x, 3/2) = x/2` is left unsolved.
It now also declines a set of roots that are not isolated: where the equation holds midway between
two of them, the points are samples of a continuum and not the set
([#1603](https://github.com/asc-community/AngouriMath/issues/1603),
[#1420](https://github.com/asc-community/AngouriMath/issues/1420)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"floor(x) = x/2 + 1/3".ToEntity().Solve("x")` | `UncompilableNodeException` | `{ 1.333… }`: 4/3, by Newton's method |
| `"max(x, 1) = 2*x".ToEntity().Solve("x")` | `UncompilableNodeException` | `{ 1/2 }` |
| `"max(x, 0) = x".ToEntity().Solve("x")` | `UncompilableNodeException` | `{ x : max(x, 0) = x }` |
| `"abs(x) - x = 0".ToEntity().Solve("x")` | `{ 0, 1/5, 2/5, …, 10 }`, 51 numbers | `{ x : abs(x) - x = 0 }` |
| `"floor(x)".Compile("x")`, and `ceil`, `round`, `max`, `min` | `UncompilableNodeException` | compiled |
| `"max(x, 1)".ToEntity().Differentiate("x")` | `derivative(max(x, 1), x)` | `(1 + sgn(x - 1)) / 2 provided not x - 1 = 0` |

### The compiled factorial is exact at whole numbers and finite to `170!`, and the compiled totient has no value at a fraction

Both compilers now evaluate the factorial, the gamma function and Euler's totient the same way,
and `Compile<TIn, TOut>` has a form for them where it had none
([#1607](https://github.com/asc-community/AngouriMath/issues/1607)). The compiled factorial was
Lanczos's approximation everywhere, and its power overflowed before the value did: from just past
`141!`, about `2e243`, it compiled to NaN, up to `170!`. At a whole number it was off from the
integer by a rounding, which a compilation over integers would truncate. It is now the exact
factorial rounded once, up to `170!`; infinite past it; NaN at the negative whole numbers, the
poles; and the same approximation elsewhere, taken in logarithms so that it overflows only where
the value does. The compiled totient truncated a fraction to the whole number below it. There it
now has no value, as the interpreter's has none.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x!".Compile("x").Call(4)` | `23.999999999999996` | `24` |
| `"x!".Compile("x").Call(0)` | `0.9999999999999998` | `1` |
| `"x!".Compile("x").Call(142)`, and on to `170` | NaN | `2.695364137888163E+245` |
| `"phi(x)".Compile("x").Call(2.5)` | `1` | NaN |
| `"x!".ToEntity().Compile<double, double>("x")`, and `gamma`, `phi` | `UncompilableNodeException` | compiled |

### An equation the solver cannot invert is left unsolved, not answered with no roots

`x! = 6` was answered `{ }`, a claim that it has no roots, and it has 3. The solver isolates `x`
by inverting the function around it, and for a factorial, a binomial coefficient, `mod` by a
divisor that is not a number, `gcd`, `lcm`, `min`, `max`, `phi`, `prime`, the valuation, a sum,
a product, a limit, a set with `x` inside it and a few more, the inversion had no way to write
the preimage and returned none. Such an equation is now left unsolved, as the set of `x` for
which it holds, the way a statement the solver has no arm for already was. Roots found beside it
are kept. A value these functions provably never take still has no roots: the factorial is the
gamma function one along, which has no zeros, so `x! = 0` is still `{ }`, and so is
`arcsin(x) = 5`.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x! = 6".Solve("x")` | `{ }` | `{ x : x! = 6 }` |
| `"x mod a = 1".Solve("x")` | `{ }` | `{ x : x mod a = 1 }` |
| `"gcd(x, 4) = 2".Solve("x")` | `{ }` | `{ x : gcd(x, 4) = 2 }` |
| `"max(x, 1) = 3".Solve("x")` | `{ }` | `{ x : max(x, 1) = 3 }` |
| `"phi(x) = 4".Solve("x")` | `{ }` | `{ x : phi(x) = 4 }` |
| `"binomial(x, 2) = 3".Solve("x")` | `UnhandledParseException` | `{ x : binomial(x, 2) = 3 }` |

### An equation in a set, a cardinality or a power set of the unknown is left unsolved, and threw

`Solve` threw `NotSufficientlySupportedException` for an equation whose unknown sits under a set
operation or a cardinality, where the inverter's contract is to decline and leave the equation
unsolved, as the equations in the entry on inverses no node writes are. A power set of the unknown
was answered, and wrongly: the answer held `x`
([#1544](https://github.com/asc-community/AngouriMath/issues/1544)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"card(x) = 3".ToEntity().Solve("x")` | throws `NotSufficientlySupportedException` | `{ x : #x = 3 }` |
| `"(x /\\ {1, 2}) = {1}".ToEntity().Solve("x")` | throws `NotSufficientlySupportedException` | `{ x : x /\ { 1, 2 } = { 1 } }` |
| `"(x \\ {1}) = {2}".ToEntity().Solve("x")` | throws `NotSufficientlySupportedException` | `{ x : x \ { 1 } = { 2 } }` |
| `"powerset(x) = {{}, {1}}".ToEntity().Solve("x")` | `{ { {  }, { 1 / x } } }` | `{ x : powerset(x) = { {  }, { 1 } } }` |

### A cost model that throws is not taken as declining a candidate

Equality saturation called the cost model inside a catch-all and took an exception for the model
declining the candidate it was pricing: a model that threw on everything returned the input as it
was, and a defect in one looked like a decline. The exception now reaches the caller. A model
declines a candidate by answering `NaN`, as it always could. A cancellation inside a rule's
right-hand side reaches the caller the same way, where it was taken for the rule declining
([#1546](https://github.com/asc-community/AngouriMath/issues/1546)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `Transformation.EqualitySaturation(WorkBudget.Unlimited, new CostModel("throws", "", e => throw new InvalidOperationException())).ApplyOrKeep("x + 0 + y*1")` | `x + 0 + y * 1` | throws `InvalidOperationException` |
| The same with `e => double.NaN` for the cost | `x + 0 + y * 1` | `x + 0 + y * 1` |

### `erf`, `erfc` and `erfi` are functions

**Addition, and two silent misreadings fixed.** The error function `erf(z) = 2/sqrt(pi) int_0^z
e^(-t^2) dt`, its complement `erfc(z) = 1 - erf(z)` and the imaginary error function
`erfi(z) = -i erf(i z)` are nodes (`MathS.Erf`, `MathS.Erfc`, `MathS.Erfi`), the first of #1501's
special functions. Each is differentiated, printed in LaTeX as `\operatorname{erf}` and so on, and
evaluated to the working precision anywhere in the complex plane (`Entity.Number.Erf`, `Erfc`,
`Erfi`). They compile, through `Compile` and `Compile<TIn, TOut>`, in double precision
([#1607](https://github.com/asc-community/AngouriMath/issues/1607)). So an equation the solver's
numerical search reaches, `erf(x) = x/2 + 1/7`, is answered with the roots it finds, while one that
isolates `erf`, `erf(x) = 1/2`, is left unsolved rather than answered with no roots. `erf`
was refused by name, but `erfc` and `erfi` were not, so each was read as a product with an
undeclared variable.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"erf(x)"` | `UnrecognizedFunctionParseException` | `erf(x)` |
| `"erfc(x)"` | `erfc * x` | `erfc(x)` |
| `"erfi(x)"` | `erfi * x` | `erfi(x)` |
| `"erfc(1)".Simplify()` | `erfc` | `erfc(1)`, and `0.157299207050285…` evaluated |
| `"erf(x)".Differentiate("x")` | `UnrecognizedFunctionParseException` | `2 / sqrt(pi) * e ^ (-x ^ 2)` |
| `"erf(x)".Compile("x")`, and `erfc`, `erfi` | `UnrecognizedFunctionParseException`; for `erfc` and `erfi`, `UncompilableNodeException`: not among the variables | compiled |
| `"erf(x) = x/2 + 1/7".ToEntity().Solve("x")` | `UnrecognizedFunctionParseException` | its roots near `-2.283`, `0.235` and `1.679`, and more off the real line |

### `Ei` and `li` are functions

**Addition, and two silent misreadings fixed.** The exponential integral
`Ei(z) = gamma + (ln z - ln(1/z))/2 + sum z^k/(k k!)` and the logarithmic integral `li(z) = Ei(ln z)`
are nodes (`MathS.Ei`, `MathS.Li`), the next of #1501's special functions. On the real line `Ei(x)`
is the principal value of `int_-oo^x e^t/t dt` and `li(x)` that of `int_0^x dt/ln t`, both real, as
SymPy's, mpmath's and Mathematica's are. Each is differentiated, printed in LaTeX as
`\operatorname{Ei}` and `\operatorname{li}`, and evaluated to the working precision anywhere in the
complex plane (`Entity.Number.Ei`, `Li`). Neither name was refused, so each was read as a product
with an undeclared variable
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"Ei(x)"` | `Ei * x` | `Ei(x)` |
| `"li(2)".Simplify()` | `2 * li` | `li(2)`, and `1.045163780117492…` evaluated |
| `"Ei(1)".Simplify()` | `Ei` | `Ei(1)`, and `1.895117816355936…` evaluated |
| `"Ei(x)".Differentiate("x")` | `Ei` | `e ^ x / x` |
| `"Ei(x)".Compile("x")`, and `li` | `UncompilableNodeException`: `Ei` is not among the variables | compiled ([#1607](https://github.com/asc-community/AngouriMath/issues/1607)) |

### The exponential and logarithmic integrals are integrated to

`e^x/x` was left unintegrated. An exponential of a linear over a whole power of a linear, beside a
polynomial, is now integrated to `Ei` under `u = c + d x`, where each term is `u^m e^(k u)`: `Ei(k u)`
for `m = -1`, and by parts toward it below. The Gaussian's odd negative moments end at
`int e^(A x^2)/x = Ei(A x^2)/2`. A power of `x` over a whole power of `A + B ln(c x^r)` is integrated to
`Ei` under `t = A + B ln(c x^r)`, and so is one over a logarithm of a power of a linear, or of
`d + e x^m`. And `Ei(ln y)` is `li(y)`, by `li`'s definition, so `int 1/ln(x) dx` is `li(x)`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^x/x".Integrate("x")` | `integral(e ^ x / x, x)` | `Ei(x) + C` |
| `"x*e^(-1/x^2)".Integrate("x")` | `integral(x * e ^ ((-1) / x ^ 2), x)` | the antiderivative, with `Ei` |
| `"1/ln(x)".Integrate("x")` | `integral(1 / ln(x), x)` | `li(x) + C` |
| `"x^m/(a + b*ln(c*x^n))".Integrate("x")` | `integral(x ^ m / (a + b * ln(c * x ^ n)), x)` | the antiderivative, with `Ei` |

### `Si`, `Ci`, `Shi` and `Chi` are functions

**Addition, and four silent misreadings fixed.** The sine and cosine integrals
`Si(z) = sum (-1)^k z^(2k + 1)/((2k + 1) (2k + 1)!)` and
`Ci(z) = gamma + ln z + sum_(k >= 1) (-1)^k z^(2k)/(2k (2k)!)`, and the hyperbolic `Shi` and `Chi`,
the same sums without the alternating sign, are nodes (`MathS.Si`, `MathS.Ci`, `MathS.Shi`,
`MathS.Chi`), the next of #1501's special functions. On the real line `Si(x)` is `int_0^x sin t/t dt`
and `Shi(x)` is `int_0^x sinh t/t dt`, both real; `Ci` and `Chi` take the cut of `ln z`, so that
`Ci(-x) = Ci(x) + i pi` for `x > 0`, as SymPy's, mpmath's and Mathematica's do. Each is
differentiated, printed in LaTeX as `\operatorname{Si}` and so on, and evaluated to the working
precision anywhere in the complex plane (`Entity.Number.Si`, `Ci`, `Shi`, `Chi`). None of the names
was refused, so each was read as a product with an undeclared variable
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"Si(x)"` | `Si * x` | `Si(x)` |
| `"Ci(2)".Simplify()` | `2 * Ci` | `Ci(2)`, and `0.4229808287748649…` evaluated |
| `"Chi(1)".Simplify()` | `Chi` | `Chi(1)`, and `0.8378669409802082…` evaluated |
| `"Si(x)".Differentiate("x")` | `Si` | `sin(x) / x` |
| `"Si(x)".Compile("x")`, and `Ci`, `Shi`, `Chi` | `UncompilableNodeException`: `Si` is not among the variables | compiled ([#1607](https://github.com/asc-community/AngouriMath/issues/1607)) |

### A quadratic below a sine or a cosine is split over its two roots

`sin(x)/(1 + x^2)` was left unintegrated. A quadratic below the bar, beside sines and cosines of a
linear, is its leading coefficient times the linears of its two roots, complex where its
discriminant is negative, and each term of the partial fractions over them is the one-linear
question: `Si` and `Ci` of `x - r`. Where the roots are a conjugate pair so are the two terms, and
their sum is real on the real line
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"cos(x)/(x^2-1)".Integrate("x")` | `integral(cos(x) / (x ^ 2 - 1), x)` | `Si` and `Ci` of `x - 1` and of `x + 1` |
| `"sin(x)/(1+x^2)".Integrate("x")` | `integral(sin(x) / (1 + x ^ 2), x)` | `Si` and `Ci` of `x - i` and of `x + i` |
| `"sin(c+d*x)/(a+b*x^2)".Integrate("x")` | `integral(sin(c + d * x) / (a + b * x ^ 2), x)` | `Si` and `Ci` of `d (x ∓ sqrt(-4 a b)/(2 b))` |

### The sine and cosine integrals are integrated to

`sin(x)/x` was left unintegrated. A polynomial in sines and cosines of a linear, beside a
polynomial, over powers of linears is now integrated to `Si` and `Ci`: each monomial of the sines
and cosines is a sum of sines and cosines of multiples of the argument, and under `u` = the linear
each term is `u^m sin(q u)` or `u^m cos(q u)`, which is `Si(q u)` or `Ci(q u)` for `m = -1` and by
parts toward them below. Several linears are split into partial fractions over them first, and an
argument `a + b x^r` beside a power of `x` is the same question under `u = x^r`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sin(x)/x".Integrate("x")` | `integral(sin(x) / x, x)` | `Si(x) + C` |
| `"sin(x)^2/x".Integrate("x")` | `integral(sin(x) ^ 2 / x, x)` | `1/2 * ln(x) + -1/2 * Ci(2 * x) + C` |
| `"sin(x)/(x*(1+x))".Integrate("x")` | `integral(sin(x) / (x * (1 + x)), x)` | `Si(x) + -cos(1) * Si(1 + x) + sin(1) * Ci(1 + x) + C` |
| `"sin(a+b/x)/x".Integrate("x")` | `integral(sin(a + b / x) / x, x)` | `-(cos(a) * Si(b * 1 / x) + sin(a) * Ci(b * 1 / x)) + C` |

### The hyperbolic sine and cosine integrals are integrated to

`sinh(x)/x` was left unintegrated. `sinh` and `cosh` arrive as exponentials, and a polynomial in
exponentials of a linear, beside a polynomial, over a whole power of a linear is now integrated to
`Shi` and `Chi`. Under `u` = the linear each term is `u^m e^(k u)`, the exponential integral's, and
two terms of opposite rates pair: `A Ei(k u) + B Ei(-k u) = (A + B) Chi(k u) + (A - B) Shi(k u)`, up
to a constant ([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"sinh(x)/x".Integrate("x")` | `integral((e ^ x - e ^ (-x)) / 2 / x, x)` | `Shi(x) + C` |
| `"cosh(x)/x".Integrate("x")` | `integral((e ^ x + e ^ (-x)) / 2 / x, x)` | `Chi(x) + C` |
| `"sinh(x)^2/x".Integrate("x")` | `integral(((e ^ x - e ^ (-x)) / 2) ^ 2 / x, x)` | `-1/2 * ln(x) + 1/2 * Chi(2 * x) + C` |
| `"cosh(a+b*x)/(c+d*x)".Integrate("x")` | `integral((e ^ (a + b * x) + e ^ (-(a + b * x))) / 2 / (c + d * x), x)` | `Chi` and `Shi` of `b (c + d x)/d`, with `cosh(a - b c/d)/d` and `sinh(a - b c/d)/d` in front, written in exponentials |

### An inverse hyperbolic tangent over a power of `1 - a^2 x^2` is integrated to the hyperbolic sine and cosine integrals

`x/((1 - a^2 x^2)^2 artanh(a x))` ran out of time. Under `x = tanh(u)/a` it is
`sinh(u) cosh(u)/(a^2 u)`, but the quadratic reached the substitution divided through by its
leading coefficient, as `1/(-a^2) + x^2`, and was not recognised as a multiple of `1 - a^2 x^2`. It
is now, and these integrate to `Shi` and `Chi` of `2 artanh(a x)`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x/((1-a^2*x^2)^2*atanh(a*x))".Integrate("x")` | `integral(x / ((1 - a ^ 2 * x ^ 2) ^ 2 * 1/2 * ln((1 + a * x) / (1 - a * x))), x)` | `Shi(2 artanh(a x))/(2 a^2)`, with `artanh` written as its logarithm |
| `"1/((1-a^2*x^2)^2*atanh(a*x))".Integrate("x")` | `integral(1 / ((1 - a ^ 2 * x ^ 2) ^ 2 * 1/2 * ln((1 + a * x) / (1 - a * x))), x)` | `(ln(artanh(a x)) + Chi(2 artanh(a x)))/(2 a)`, likewise |

### A definite integral is the limit at a bound where its antiderivative is undefined

`integral(x * e^(-x), x, 0, +oo)` was `NaN`, this library's way of saying a value does not
exist. It exists, and it is `1`. A definite integral substituted each bound into the
antiderivative, and `-(x + 1) e^(-x)` at `+oo` is `-oo * 0`; `x ln(x) - x` at `0` is `0 * -oo`.
At a bound where the substitution is `NaN`, the value is now the antiderivative's limit there,
taken from inside the range: from the left at `+oo`, from the right at `-oo`, and at a finite
bound from the side the other bound is on. Nothing the substitution answered changes, and an
antiderivative with no limit at the bound still gives `NaN`, as `-cos(x)` does at `+oo`
([#1507](https://github.com/asc-community/AngouriMath/issues/1507)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `integral(x * e^(-x), x, 0, +oo)` | `NaN` | `1` |
| `integral(e^(-x) * sin(x), x, 0, +oo)` | `NaN` | `1/2` |
| `integral(x^3 * e^(-x^2), x, 0, +oo)` | `integral(e ^ (-x ^ 2) * x ^ 3, x, 0, +oo)` | `1/2` |
| `integral(ln(x), x, 0, 1)` | `NaN` | `-1` |
| `integral(x * ln(x), x, 0, 1)` | `NaN` | `-1/4` |
| `integral(1/x^2, x, 0, 1)` | `NaN` | `+oo` |
| `integral(sin(x), x, 0, +oo)` | `NaN` | `NaN` |

### The Gaussian and the error functions are integrated

`e^(-x^2)` was left unintegrated, and its antiderivative is `sqrt(pi)/2 erf(x)`. An exponential
of a quadratic is now integrated to an error function of the square it completes, written with
`erfi` and a real root where the square's coefficient is decidably positive. Beside an even power
of `x`, on either side of 0, it is integrated by parts down to that, and `erf`, `erfc` and `erfi`
themselves by parts against 1. A symbolic exponent is answered for the generic case, as
`F^(a x)/(a ln F)` already was ([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(-x^2)".Integrate("x")` | `integral(e ^ (-x ^ 2), x)` | `sqrt(pi) / 2 * erf(x) + C` |
| `"e^(x^2)".Integrate("x")` | `integral(e ^ x ^ 2, x)` | `sqrt(pi) / 2 * erfi(x) + C` |
| `"x^2*e^(-x^2)".Integrate("x")` | `integral(x ^ 2 * e ^ (-x ^ 2), x)` | `x * e ^ (-x ^ 2) / (-2) + 1/2 * sqrt(pi) / 2 * erf(x) + C` |
| `"f^(a+b*x^2)".Integrate("x")` | `integral(f ^ (a + b * x ^ 2), x)` | `f ^ a * sqrt(pi) / (2 * sqrt(-b * ln(f))) * erf(sqrt(-b * ln(f)) * x) + C` |
| `"erf(x)".Integrate("x")` | `UnrecognizedFunctionParseException` | `x * erf(x) + e ^ (-x ^ 2) / sqrt(pi) + C` |
| `integral(e^(-x^2), x, -oo, +oo)` | `integral(e ^ (-x ^ 2), x, -oo, +oo)` | `sqrt(pi)` |

### A definite integral across a pole diverges, and a jump in its antiderivative is left out

`integral(1/x^2, x, -1, 1)` was `-2`, a negative number for a positive integrand: the
antiderivative `-1/x` was taken at the two bounds, and the pole between them was never seen.
Where the bounds have numeric values, the range is now split where the antiderivative can break,
and each piece takes its limit at a break from its own side. The breaks are the zeros of a
denominator, of a logarithm's argument, and of the cosine or sine under a tangent, secant,
cotangent or cosecant, among others. A pole the integral diverges across gives `+oo` or `-oo`,
and pieces diverging in opposite directions have no value. A jump where the integrand is
continuous, like the half-angle antiderivative's at `pi`, is left out. That needs the limit of an
arctangent, arccotangent or arccosecant whose argument diverges, which is now answered too
([#1508](https://github.com/asc-community/AngouriMath/issues/1508)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `integral(1/x^2, x, -1, 1)` | `-2` | `+oo` |
| `integral(1/(x - 2)^2, x, 0, 3)` | `-3/2` | `+oo` |
| `integral(tan(x)^2, x, 0, 3)` | `tan(3) - arctan(tan(3))` | `+oo` |
| `integral(1/x, x, -1, 1)` | `-ln(-1)` | `NaN` |
| `integral(1/(x^2 - 4), x, -3, 3)` | `ln(1/25) / 4` | `NaN` |
| `integral(1/(2 + cos(x)), x, 0, 2 * pi)` | `integral(1 / (2 + cos(x)), x, 0, 2 * pi)` | `2/3 * sqrt(3) * pi` |
| `integral(1/(2 + cos(x)), x, 0, pi)` | `integral(1 / (2 + cos(x)), x, 0, pi)` | `sqrt(3) * pi / 3` |
| `limitleft(arctan(tan(x / 2)), x, pi)` | `limitleft(arctan(tan(x / 2)), x, pi)` | `pi / 2` |

### A limit at infinity whose terms cancel around a slowly varying factor is answered

`lim e^x (exp(1/x + e^(-x)) - exp(1/x))` as `x → ∞` is the example Gonnet and Gruntz introduce
their algorithm with, and the implementation left it as written. The algorithm's steps were right,
and the series engine then read a leading term where there is none. It took a constant term off by
subtracting it, and divided a leading coefficient by itself, which left `1/x - 1/x` and `x/x - 1` at
`w^0`; it also did not know `e^(1/x) - e^(1/x)` for zero. The constant term is now removed exactly,
the normalised leading coefficient is `1` exactly, and the zero test collects like terms and reads a
conditional zero -- `0 provided not x = 0` -- as zero.
[#353](https://github.com/asc-community/AngouriMath/issues/353). Both columns measured on a build,
`v2.5.0` against this change.

| `"….".ToEntity().Limit("x", "+oo")` of | Was (2.5.0) | Is |
|---|---|---|
| `e ^ x * (e ^ (1/x + e ^ (-x)) - e ^ (1/x))` | left as written | `1` |
| `e ^ x * (e ^ (2/x + e ^ (-x)) - e ^ (2/x))` | left as written | `1` |
| `e ^ (2 * x) * (e ^ (1/x + e ^ (-2 * x)) - e ^ (1/x))` | left as written | `1` |
| `e ^ (x + e ^ (-x)) - e ^ x` | `1` | the same |

### With the downcasting off, integration and limits compute on exact numbers

With `DowncastingEnabled` off, `1/(1 + c^2*x^2)` integrated to `NaN + C` and `limit(sin(c*x)/x, x, 0)`
was `NaN`, where the default setting answers `arctan(c x)/c` and `c`. The zero test compared
against the literal `0`, and with the downcasting off no zero was equal to it. A whole number
parsed with the setting off was a decimal, so `x^2` was not a whole power to the integrator.
Integration and limits now compute as with the downcasting on, whatever the caller's setting, on
the input's decimals read as the exact rationals they are. With it on, nothing changes. With it
off, a decimal comes back as the rational it holds: `0.1 x` integrates to `1/10 * x ^ 2 / 2 + C`,
where it was `0.1 * x ^ 2 / 2 + C`
([#1490](https://github.com/asc-community/AngouriMath/issues/1490)).

| Input, with the downcasting off | Was (2.5.0) | Now |
|---|---|---|
| `"1/(1 + c^2*x^2)".Integrate("x")` | `NaN + C` | the antiderivative |
| `"(a + b*arcsin(c*x))/sqrt(d - c^2*d*x^2)".Integrate("x")` | `integral((a + b * arcsin(c * x)) / (d - c ^ 2 * d * x ^ 2) ^ (1/2), x)` | the antiderivative |
| `limit(sin(c*x)/x, x, 0)` | `NaN` | `c` |
| `limit((1 - cos(c*x))/x^2, x, 0)` | `NaN` | `c ^ 2 / 2` |

### `Complexity` counts to `int.MaxValue` and stops there

`Entity.Complexity` threw `OverflowException` for an expression larger than `int.MaxValue` as a
tree, which an expression built by sharing reaches while it is small in memory: thirty-one squarings
of `x` are 32 nodes, and `2^32 - 1` as a tree. It now saturates at `int.MaxValue`, and the bounds
that ask it whether a value is too large to go on with decline such a value, where they threw out of
the operation that asked ([#1600](https://github.com/asc-community/AngouriMath/issues/1600)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `Complexity` of `x` squared thirty-one or sixty times | `OverflowException` | `int.MaxValue` |
| `Complexity` of `x` squared thirty times | `2147483647` | `2147483647`, the number of nodes, as below the bound |

### With the downcasting off, a comparison of real values is decided

With `DowncastingEnabled` off, `cos(3)` evaluates to a complex number whose imaginary part is zero,
and a comparison of one was `NaN`: inequalities were decided only between numbers of the real
type. A number with no imaginary part is now compared as the real it is, so `cos(3) >= 0` is
`false` under either setting, and a condition an answer states on a sign, `provided cos(x) >= 0`,
can be evaluated at a point. A comparison with a value off the real line is still `NaN`.

| Evaluated, with the downcasting off | Was (2.5.0) | Now |
|---|---|---|
| `cos(3) >= 0` | `NaN` | `false` |
| `sin(4) < 0` | `NaN` | `true` |
| `cos(1) > cos(3)` | `NaN` | `true` |

### A half-odd power of a logarithm beside a power of `x` is integrated

`x sqrt(ln(x))` was left unintegrated. `x^p F^n`, where `F` is `ln x` or `A + B ln(c x^r)` and
`n` is half an odd number, is now integrated onto the Gaussian's moments under `t = sqrt(F)`.
A polynomial beside it is summed a power at a time, and the logarithm of a power of a linear is
the same question under `u = d + e x`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x*sqrt(ln(x))".Integrate("x")` | `integral(x * sqrt(ln(x)), x)` | the antiderivative, with `erfi` |
| `"sqrt(ln(a*x^n))".Integrate("x")` | `integral(sqrt(ln(a * x ^ n)), x)` | the antiderivative, with `erfi` |
| `"x^2/ln(x)^(3/2)".Integrate("x")` | `integral(x ^ 2 / ln(x) ^ (3/2), x)` | the antiderivative, with `erfi` |
| `"(f + g*x)*(a + b*ln(c*(d + k*x)^n))^(3/2)".Integrate("x")` | `integral((f + g * x) * (a + b * ln(c * (d + k * x) ^ n)) ^ (3/2), x)` | the antiderivative |

### The Gaussian with a linear term is integrated beside a power of `x` or a polynomial

`x^2 e^(-x^2 + x)` was left unintegrated. `x^m P(x) F^(a x^2 + b x + c)` with `b` not zero is now
integrated at the square's centre, `u = x + b/(2a)`, where `x^m P(x)` is a polynomial in `u`.
Each of its powers is a moment of the Gaussian, and an odd one ends at the elementary
`e^(A u^2)/(2A)`. The hyperbolic functions of a quadratic beside a power of `x` come out too,
since they are these exponentials
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2*e^(-x^2 + x)".Integrate("x")` | `integral(x ^ 2 * e ^ (-x ^ 2 + x), x)` | the antiderivative, with `erf` |
| `"x^3*f^(c*(a + b*x)^2)".Integrate("x")` | `integral(x ^ 3 * f ^ (c * (a + b * x) ^ 2), x)` | the antiderivative |
| `"(p + q*x)^2*f^(a + b*x + c*x^2)".Integrate("x")` | `integral((p + q * x) ^ 2 * f ^ (a + b * x + c * x ^ 2), x)` | the antiderivative |
| `"x^2*sinh(a + b*x + c*x^2)".Integrate("x")` | `integral(x ^ 2 * (e ^ (a + b * x + c * x ^ 2) - e ^ (-(a + b * x + c * x ^ 2))) / 2, x)` | the antiderivative |

### A Gaussian below the bar is read as the exponential of its negated exponent

`e^(-x^2)/x^2` was integrated by the Gaussian's moments, and `1/(e^(x^2) x^2)`, the same function,
was not: the rule for the moments took the exponential from above the bar only. By parts writes
the factor beside `erf(b x)` in `erf(b x)/(e^(b^2 x^2) x^2)` the second way, and that integral was
declined for want of it ([#1501](https://github.com/asc-community/AngouriMath/issues/1501)). Both
columns measured on a build, `v2.5.0` against this change; `erf` had no reading in 2.5.0, which its
own entry records.

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"1/(e^(x^2)*x^2)".Integrate("x")` | `integral(1 / (e ^ x ^ 2 * x ^ 2), x)` | `-1 / x * e ^ (-x ^ 2) + (-2) * pi ^ (1/2) / 2 * erf(x) + C` |
| `"erf(b*x)/(e^(b^2*x^2)*x^2)".Integrate("x")` | `UnrecognizedFunctionParseException`: there is no function `erf` | an antiderivative in `erf` and `Ei(-2 b^2 x^2)` |

### An exponential of a polynomial beside the polynomial's derivative is integrated

`e^(a + b x + c x^2) (b + 2 c x) sqrt(a + b x + c x^2)` was left unintegrated. `G^P k P' f(P)`, with
`P` a polynomial of degree two or more and `P'` a factor of its own up to a constant, is now
integrated under `u = P`, as `k G^u f(u)`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(a + b*x + c*x^2)*(b + 2*c*x)*(a + b*x + c*x^2)^(1/2)".Integrate("x")` | `integral(e ^ (a + b * x + c * x ^ 2) * (b + 2 * c * x) * sqrt(a + b * x + c * x ^ 2), x)` | the antiderivative, with `erfi` |
| `"e^(a + b*x + c*x^2)*(b + 2*c*x)/(a + b*x + c*x^2)^(3/2)".Integrate("x")` | `integral(e ^ (a + b * x + c * x ^ 2) * (b + 2 * c * x) / (a + b * x + c * x ^ 2) ^ (3/2), x)` | the antiderivative, with `erfi` |

### An exponential of a quadratic in `1/x` or `1/(c + d x)` is integrated

`e^(-1/x^2)` was left unintegrated. `L^m F^(A/L^2 + B/L + C)`, with `L = c + d x` and a whole `m`,
is now integrated under `u = 1/L`, where it is the Gaussian beside a power of `u`. The hyperbolic
functions of `a + b/x^2` come out too, since they are these exponentials. An odd power that ends at
the exponential integral, as `x e^(-1/x^2)` does, is still left unintegrated
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(-1/x^2)".Integrate("x")` | `integral(e ^ ((-1) / x ^ 2), x)` | the antiderivative, with `erf` |
| `"f^(a + b/x^2)*x^2".Integrate("x")` | `integral(f ^ (a + b / x ^ 2) * x ^ 2, x)` | the antiderivative |
| `"f^(a + b/(c + d*x)^2)/(c + d*x)^4".Integrate("x")` | `integral(f ^ (a + b / (c + d * x) ^ 2) / (c + d * x) ^ 4, x)` | the antiderivative |
| `"sinh(a + b/x^2)".Integrate("x")` | `integral((e ^ (a + b / x ^ 2) - e ^ (-(a + b / x ^ 2))) / 2, x)` | the antiderivative |

### Exponentials of quadratics beside sines, cosines and hyperbolic functions of quadratics are integrated

`e^(x^2) sin(x)` was left unintegrated. A product of exponentials of polynomials of degree at most
two, whole powers of sines, cosines and hyperbolic functions of such polynomials, and a polynomial
is now integrated wherever some exponent is a quadratic. Each sine or cosine is written as
exponentials of `i` times its argument, and every term of the product multiplied out is the
Gaussian's. So the answer for a real integrand with a sine or cosine in it is written with the
imaginary unit, as Rubi's is: `e^(x^2) sin(x)` integrates to
`-i/2 e^(1/4) sqrt(pi)/2 erfi(x + i/2) + i/2 e^(1/4) sqrt(pi)/2 erfi(x - i/2)`. A sine or cosine of a
quadratic alone, or beside a polynomial only, is left to the rules that answered it before
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(x^2)*sin(x)".Integrate("x")` | `integral(e ^ x ^ 2 * sin(x), x)` | the antiderivative, with `erfi` |
| `"f^(a + b*x + c*x^2)*cos(d + p*x + q*x^2)".Integrate("x")` | `integral(f ^ (a + b * x + c * x ^ 2) * cos(d + p * x + q * x ^ 2), x)` | the antiderivative, with `erf` |
| `"f^(a + c*x^2)*cosh(d + p*x)^2".Integrate("x")` | `integral(f ^ (a + c * x ^ 2) * ((e ^ (d + p * x) + e ^ (-(d + p * x))) / 2) ^ 2, x)` | the antiderivative |
| `"x^2*sinh(a + b*x + c*x^2)^2".Integrate("x")` | `integral(x ^ 2 * ((e ^ (a + b * x + c * x ^ 2) - e ^ (-(a + b * x + c * x ^ 2))) / 2) ^ 2, x)` | the antiderivative |
| `"2^(x^2)*3^(x + 1)".Integrate("x")` | `integral(2 ^ x ^ 2 * 3 ^ (x + 1), x)` | the antiderivative, with `erfi` |

### A power of `x` beside exponentials of quadratics without a linear part is integrated

`x^2 sinh(a + b x^2)^3` was left unintegrated. A polynomial, or a power of `x` above or below the bar,
beside exponentials of quadratics is now integrated where a term's exponent has no linear part: its
moments are taken about 0, and a negative even power is one of them. A negative odd power, which
ends at the exponential integral, is still left unintegrated
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"x^2*sinh(a + b*x^2)^3".Integrate("x")` | `integral(x ^ 2 * ((e ^ (a + b * x ^ 2) - e ^ (-(a + b * x ^ 2))) / 2) ^ 3, x)` | the antiderivative |
| `"sinh(a + b*x^2)^3/x^2".Integrate("x")` | `integral(((e ^ (a + b * x ^ 2) - e ^ (-(a + b * x ^ 2))) / 2) ^ 3 / x ^ 2, x)` | the antiderivative |

### An exponential of a quadratic in a logarithm beside a power of `x` is integrated

`e^(ln(x)^2)` was left unintegrated. `x^p G^(Q(L))`, where `L` is `ln(c x^r)` and `Q` a quadratic
with a square term, is now integrated under `t = L`, where it is the Gaussian with a linear term. A
polynomial beside it is summed a power at a time, and the logarithm of a power of a linear is the
same question under `u = d + e x`
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"e^(ln(x)^2)".Integrate("x")` | `integral(e ^ ln(x) ^ 2, x)` | the antiderivative, with `erfi` |
| `"x^2*F^(f*(a + b*ln(c*x^n))^2)".Integrate("x")` | `integral(x ^ 2 * F ^ (f * (a + b * ln(c * x ^ n)) ^ 2), x)` | the antiderivative |
| `"(d*g + k*g*x)^m*F^(f*(a + b*ln(c*(d + k*x)^n))^2)".Integrate("x")` | `integral((d * g + k * g * x) ^ m * F ^ (f * (a + b * ln(c * (d + k * x) ^ n)) ^ 2), x)` | the antiderivative |
| `"(g + h*x)^3*F^(f*(a + b*ln(c*(d + k*x)^n)^2))".Integrate("x")` | `integral((g + h * x) ^ 3 * F ^ (f * (a + b * ln(c * (d + k * x) ^ n) ^ 2)), x)` | the antiderivative |

### A power of `x` one less than a half-odd multiple of the power inside is integrated

`f^(a + b x^n) x^(-1 + 5n/2)` was left unintegrated. `x^(k n - 1) g(x^n)`, with a symbolic `n`, was
integrated under `u = x^n` for a whole `k` of 1 or more. For `k` half an odd number, of either sign,
it is now integrated under `u = x^(n/2)`, where `g(x^n)` is `g(u^2)`: beside an exponential of `x^n`
that is a moment of the Gaussian
([#1501](https://github.com/asc-community/AngouriMath/issues/1501)).

| Input | Was (2.5.0) | Now |
|---|---|---|
| `"f^(a + b*x^n)*x^(-1 + 5/2*n)".Integrate("x")` | `integral(f ^ (a + b * x ^ n) * x ^ (-1 + 5/2 * n), x)` | the antiderivative |
| `"f^(a + b*x^n)*x^(-1 - 1/2*n)".Integrate("x")` | `integral(f ^ (a + b * x ^ n) * x ^ (-1 - 1/2 * n), x)` | the antiderivative |
| `"x^(-1 + 1/2*n)*sinh(a + b*x^n)".Integrate("x")` | `integral(x ^ (-1 + 1/2 * n) * (e ^ (a + b * x ^ n) - e ^ (-(a + b * x ^ n))) / 2, x)` | the antiderivative |

### `binomial(n, k)` is a function

**Addition, not silent.** The binomial coefficient is a node, `Entity.Binomialf`, spelled
`binomial(n, k)` and `MathS.Binomial(n, k)`. A whole `k` is computed by the falling factorial
`n (n - 1) … (n - k + 1)/k!` for any `n` — negative and rational ones included — with `0` below
`k = 0` and above a non-negative whole `n`; `binomial(n, 0)` is `1` for any `n`; a symbolic
argument keeps the node, which is the spelling Pascal's identity and the binomial theorem read
rather than a ratio of three factorials; a `k` that is not whole evaluates numerically through the
gamma function, and has no value where `n` is a negative whole number, which its domain condition
says. LaTeX is `\binom{n}{k}`, SymPy is `sympy.binomial`.

| | Was | Is |
|---|---|---|
| `"binomial(5, 2)".ToEntity()` | `UnhandledParseException` | `10` on simplification |
| `"binomial(-1, 3)".ToEntity().Simplify()` | — | `-1` |
| `"binomial(1/2, 2)".ToEntity().Simplify()` | — | `-1/8` |
| `"binomial(n, k)".ToEntity().Simplify()` | — | `binomial(n, k)` |

First item of [#1409](https://github.com/asc-community/AngouriMath/issues/1409), specified in
[#809](https://github.com/asc-community/AngouriMath/issues/809)'s survey.

### A condition the answer states on its own is no longer repeated beside it

**Improvement, not silent.** `Simplify` returned `(1 - ln(x))/x^2 provided not x = 0` for the
derivative of `ln(x)/x`, and `(1 + ln(x)) x^x provided not x = 0` for that of `x^x`. The quotient
by `x^2`, the `ln(x)` and the `0^0` are each undefined at zero already, so the condition excluded
a point the expression never reached and said twice what the expression says once. A `provided`
whose condition the expression proves on its own — whose own domain condition, read in the
ambient codomain (`DomainConditionIn`), already excludes the zeros of the condition's `e`: a
conjunct `not d = 0` or `d > 0` with `d` being `e` itself, a positive power of it, a product
holding it, a sine, tangent, arcsine or arctangent of such a thing, a polynomial in the variable
`e` is with no constant term, or a polynomial that is zero at the root of a linear `e`, or a
disjunction every side of which does, as `x^x`'s `not x = 0 or x > 0` — comes back without that
conjunct. Decided by the domain condition rather than by the shape of the node, so it follows
the codomain: when [#217](https://github.com/asc-community/AngouriMath/issues/217) gives `1/0`
a value under the codomains that admit a complex infinity, the quotient's domain condition
changes there and the condition beside `1/x` stops being redundant, with no change to the rule. Only the answer is read this way, and only its top: inside a piecewise a predicate
decides which case is taken and stays; inside the simplifier's own search a `provided` is what
rules read as "this candidate needs a condition" and stays too. A condition the expression does
not state is kept — `x/x` simplifies to `1 provided not x = 0` as before, since `1` no longer
says it. [#1394](https://github.com/asc-community/AngouriMath/issues/1394)

| | Was | Is |
|---|---|---|
| `"ln(x)/x".Differentiate("x").Simplify()` | `(1 - ln(x))/x^2 provided not x = 0` | `(1 - ln(x))/x^2` |
| `"x^x".Differentiate("x").Simplify()` | `(1 + ln(x)) x^x provided not x = 0` | `(1 + ln(x)) x^x` |
| `"x^n".Differentiate("x").Simplify()` | `x^n n/x provided not x = 0` | `x^n n/x` |
| `"1/(x^2 + x) provided not x = 0 and not x + 1 = 0".Simplify()` | unchanged | `1/(x^2 + x)` |
| `"tan(x) provided not cos(x) = 0".Simplify()`, `"sin(x)/x provided not x = 0".Simplify()` | unchanged | `tan(x)`, `sin(x)/x` |
| `"(x + sin(x))/x".Limit("x", "+oo")` | unevaluated | `1`, by the squeeze theorem on `sin(x)/x` once the division no longer carries the condition between the terms |

### The Pythagorean pair is found anywhere in a sum

**Improvement, not silent.** `Simplify` knew `sin(t)^2 + cos(t)^2` as a node of two operands, and
sorted a sum so that the pair landed adjacent — which made them one node only when nothing sorted
*before* them. `a + sin(t)^2 + cos(t)^2` became `1 + a`; `sqrt(2) + sin(t)^2 + cos(t)^2` stayed as
written, and so did the README's own example. The n-ary reading of the identity, which finds the
two squares among any number of operands, now runs beside the trigonometric rules.

| | Was | Is |
|---|---|---|
| `"sqrt(2) + sin(t)^2 + cos(t)^2".Simplify()` | unchanged | `1 + sqrt(2)` |
| `"1/2 + sin(pi/4) + (sin(3x)^2 + cos(3x)^2)".Simplify()` | `(sqrt(2) + 1)/2 + cos(3x)^2 + sin(3x)^2` | `3/2 + sqrt(2)/2` |

---

## 2.5.0 — since 2.4.0

Released 2026-09-09. This heading was renamed from “Unreleased” *before* the tag was cut, which is
the one thing the 2.4.0 section below records against itself: those entries sat under “Unreleased”
while 2.4.0 was tagged and published, so a reader on that version could not tell from this file what
they had.

### At a glance

| Silent? | What | Was | Is |
|---|---|---|---|
| **Silent** | `"domain(1/2, CC)".ToEntity()`, and every quotient of two integer literals annotated with the codomain its node type does not default to | `1/2` — the annotation dropped, equal to the unannotated literal | `1/2` carrying `Codomain = Complex`, which prints and reads back as `domain(1/2, CC)` |
| | `"a in (a / 3; 3a)".ToEntity().Simplify()`, and every denominator but 2 | `a in (a / 3; 3 * a)` — left as written | `a > 0` |
| | `MathS.Matrix(...).Determinant` on a symbolic matrix of polynomials | `a * d + -b * c`, Laplace's nested expansion | `a * d - b * c`, expanded — the same value, no larger |
| | the same on a numeric matrix past 10x10 | did not return | 11x11 in 2 ms, 30x30 in 22 ms |
| **Silent** | `MathS.Equations(...).Solve(...)` on a system neither internal path can finish | ran without a bound — cyclic-6 exceeded 20 s | `NotSufficientlySupportedException`, naming both paths |
| | the same with `MathS.Settings.Budget` set below what the solve needs | answered anyway, the fall-through having no budget | raises |
| | `"a in (a / 2; 0)".ToEntity().Simplify()`, and every interval demanding both signs | left as written | `False provided a in RR` |
| | `"x^2/(x^4 + 1)".Integrate("x")`, and every quotient whose denominator is a biquadratic irreducible over `Q` | `integral(x ^ 2 / (x ^ 4 + 1), x)` — left unevaluated | the antiderivative, over the real quadratic factors |
| | `"sqrt(x)/(1 + x^2)".Integrate("x")`, and every integrand a fractional power of the variable makes rational | `integral(sqrt(x) / (1 + x ^ 2), x)` — left unevaluated | the antiderivative |
| | `"sqrt(tan(x))".Integrate("x")`, and every integrand that is a function of `tan(x)` alone and rational in it | `integral(sqrt(tan(x)), x)` — left unevaluated | the antiderivative |
| | `"x^2/(x + 1)".Integrate("x")`, and every improper quotient of polynomials | `integral(x ^ 2 / (x + 1), x)` — left unevaluated | `x ^ 2 / 2 + -x + ln(x + 1) + C` |
| | `MathS.Equations("2*x - 4*y - 12").Solve("x", "y")`, and every linear system with fewer equations than unknowns | `WrongNumberOfArgumentsException` | `[[6 + 2 * t_1, t_1]]` — the family of all its solutions |
| **Silent** | `"not (x = 1)".ToEntity().Solve("x")`, and every negation | `{  }` — no value satisfies it | `{ x : not x = 1 }` |
| **Silent** | `"not (x > 1)".ToEntity().Solve("x")`, and every negated comparison | `{  }` | `(-oo; 1]` |
| **Silent** | `"(x = 1) implies (x = 2)".ToEntity().Solve("x")`, and every implication | `{ 2 } \/ BB` — truth values in the solution set of a numeric question | `{ x : not x = 1 }` |
| | `"domain((-oo; +oo), Any) = RR".ToEntity().Solve("x")`, and every unbounded interval widened to `Any` | `NotSufficientlySupportedException: There is no special set for domain Any` | `{  }` |
| **Silent** | `RewriteRules.DivisionPreparing.Rules[0].Name`, and every rule of the twenty-seven sets now described from their data form | `Mulf(var any1, Divf(Integer(1), var any2))` — the `switch` arm's rendered pattern | `reciprocal-factor-becomes-a-quotient` |
| | `RewriteRules.ExpandFactorialDivisions.Rules.Count`, and `FactorizeFactorialMultiplications` | `8` | `3` — the same rewrites, five of the eight arms being one commutative pattern |
| | `RewriteRules.Boolean.Rules.Count` | `36` | `20` — a commutative pattern finds a shared operand wherever it sits |
| | `RewriteRules.NumericNeat.Rules.Count`, and `Factorization` 22 -> 11 | `16` | `11` |
| | `RewriteRules.Trigonometric.Rules.Count` | `43` | `33` |
| | `RewriteRules.Power.Rules.Count` | `35` | `31` |
| | `RewriteRules.Common.Rules.Count` | `100` | `62` |
| | `RewriteRules.All.Sum(set => set.Rules.Count)` | `407` | `313` |
| **Loud** | `"max(x, y in S)".ToEntity()`, and `argmax`/`argmin` of any arguments | `max(x, y in S)` — the two-value max of `x` and the membership statement; `argmax(f, t in S)` was a parse error | `max(f(t), t in S)` reads as the largest value over the set: `max(sin(t)^3 cos(t), t in [0; pi/2])` is `3 sqrt(3) / 16`, and `argmax` and `argmin` are the points where it is taken |
| **Silent** | `RewriteRules.ExpandFactorialDivisions.Rules[0].Growth` | `Collects` — guessed from string length | `Unknown`; whether it collects depends on the offsets |
| **Silent** | `RewriteStep.Soundness` on a rewrite whose rule declares a tier — `RewriteRules.SetOperator` on `A /\ A`, and every rewrite of the nineteen sets described from their data form | `SoundUnderAssumptions` — its rule set's tier, which is the minimum over every rule in the set | `Sound` — the rule's own |
| **Silent** | `DerivationStep.Soundness` | its rule set's tier | the weakest tier any rewrite that actually fired inside the step holds at |
| **Silent** | `"5 - (0; 1)".ToEntity().Simplify()`, and every interval subtracted from something | `(5; 4)` — a left end above its right, so the empty set | `(4; 5)` |
| **Silent** | `"4.5 in (5 - (0; 1))".ToEntity().Simplify()` | `False` | `True` |
| **Silent** | `"5 - [0; 1)".ToEntity().Simplify()` | `[5; 4)` — the openness left where it was | `(4; 5]` |
| | `"3 * [2; 3]".ToEntity().Simplify()`, and every interval scaled by a constant | `3 * [2; 3]` — left alone | `[6; 9]` |
| | `"[2; 3] / 2".ToEntity().Simplify()` | `[2; 3] / 2` | `[1; 3/2]` |
| **Silent** | `"[2; 3) * (-1)".ToEntity().Simplify()` | `[2; 3) * (-1)` | `(-3; -2]` — reflected, ends and openness both |
| | `"0 - [0; 1)".ToEntity().Simplify()` | `-[0; 1)` | `(-1; 0]` |
| | `"ln((0; 1))".ToEntity().Simplify()`, and every interval under a function monotone on it — `ln`, `log`, `e^`, `b^`, whole and unit-fraction powers, `abs`, `arctan`, `arcsin`, `arccos` | `ln((0; 1))` — left alone | `(-oo; 0)`; `e^[0; 1]` is `[1; e]`, `(-2; 1]^2` is `[0; 4)`, `abs((-1; 2])` is `[0; 2]`, `arctan([0; 1])` is `[0; pi / 4]`, `sqrt([0; 4])` is `[0; 2]` |
| | `"(1; 2) * (3; 4)".ToEntity().Simplify()`, and every product or quotient of two intervals with finite numeric ends | `(1; 2) * (3; 4)` — left alone | `(3; 8)`; `(1; 2) / (3; 4)` is `(1/4; 2/3)`; a divisor containing zero stays as written |
| | `"2 / (0; 1)".ToEntity().Simplify()`, and every constant over an interval that does not contain zero | `2 / (0; 1)` — left alone | `(2; +oo)` |
| | `"2 * x + 4 * a".ToEntity().Factorize()`, and every sum whose whole coefficients share a divisor | `2 * x + 4 * a` — left alone | `2 * (x + 2 * a)` |
| | `Transformation.Factorization.Name` | `… then polynomial-factorization` | `… then polynomial-factorization then numeric-content` |
| | `"sin(x)^2 + 2 sin(x) + 1".ToEntity().Factorize()`, and every polynomial in one subexpression of its variable | `sin(x) ^ 2 + 2 * sin(x) + 1` — left alone | `(sin(x) + 1) ^ 2`; `sin(x) + sin(x)^3` is `sin(x) * (sin(x) ^ 2 + 1)` |
| **Silent** | `"arccotan(-1)".ToEntity().Simplify()`, and every negative argument the inverse-trigonometric table knows | `3/4 * pi` — the textbook range, and **not equal to `arccotan(-1)`**, whose value is `-pi/4` | `-1/4 * pi` |
| | `"e ^ ln(x)".ToEntity().Simplify()`, and every exponential of a natural logarithm | `e ^ ln(x)` — left as written | `x` |
| | `"pi ^ log(pi, x)".ToEntity().Simplify()`, and every constant raised to a logarithm of itself | `pi ^ log(pi, x)` — left as written | `x` |
| | `"a ^ log(a, x)".ToEntity().Simplify()`, and every symbolic base raised to a logarithm of itself | `a ^ log(a, x)` — left as written | `x provided not a = 0 and not a = 1` — the logarithm's own domain condition, which the rewrite would otherwise lose ([#994](https://github.com/asc-community/AngouriMath/issues/994)) |
| | `"e ^ (2 * ln(x))".ToEntity().Simplify()`, and every number or constant raised to a multiple of a logarithm of itself | `e ^ (2 * ln(x))` — left as written | `x ^ 2`; `e ^ (-3/2 * ln(u))` is `u ^ (-3/2)`, `2 ^ (k * log(2, x))` is `x ^ k`, `pi ^ (k * log(pi, x))` is `x ^ k` |
| | `"a ^ (k * log(a, x))".ToEntity().Simplify()`, and every symbolic base raised to a multiple of a logarithm of itself | `a ^ (log(a, x) * k)` — left as written | `x ^ k provided not a = 0 and not a = 1` ([#994](https://github.com/asc-community/AngouriMath/issues/994)) |
| **Silent** | `MathS.Ln("x").Substitute("e", 3)`, and `"exp(x)".ToEntity().Substitute("e", 3)` | `log(3, x)` and `3 ^ x` — the base of `ln` and `exp` was reached as a mention of `e` | `ln(x)` and `exp(x)` — it is Euler's number in the operator's definition, not a mention; `log(e, x)` and `e ^ x`, where the writer named `e`, still become `log(3, x)` and `3 ^ x` |
| | `"a => a + 3".ToEntity()`, and every lambda written with an arrow | `UnhandledParseException` | `lambda(a, a + 3)` |
| | `"sqrt(5 + 2 * sqrt(6))".ToEntity().Simplify()`, and every nested radical whose discriminant is a rational square | `sqrt(5 + 2 * sqrt(6))` — left as written | `sqrt(3) + sqrt(2)` |
| | `"sum(k, k, 1, n)".ToEntity().Simplify()`, and every summation whose body is a polynomial in the index | `sum(k, k, 1, n)` — carried | `piecewise((n + n ^ 2) / 2 provided n >= 0, 0)` |
| | `"sum(k, k, 1, 100000)".ToEntity().Simplify()`, and every concrete range past a hundred terms | `sum(k, k, 1, 100000)` — carried | `5000050000` |
| | `"product(k, k, 1, n)".ToEntity().Simplify()`, and every product whose body is a monomial in the index | `product(k, k, 1, n)` — carried | `piecewise(n! provided n >= 1, 1)` |
| | `"1/(x^2 + 1)^2".Integrate("x")`, and every proper rational function over a repeated irreducible quadratic | `integral(1 / (1 + x ^ 2) ^ 2, x)` — left unevaluated, after 2.9 s | `arctan(x) / 2 + C + 1/2 * x / (x ^ 2 + 1)`, in 0.11 s |
| | `"x^2/(x^2 + 2)^2".Integrate("x")`, and every polynomial numerator over one | `integral(x ^ 2 / (x ^ 2 + 2) ^ 2, x)` — left unevaluated, after 93 s | the antiderivative, in 0.57 s |
| | `"1/(x^2 - 1)^2".Integrate("x")`, and every repeated quadratic whose roots are real | `C - ln(x + -1) / 4 + ln(1 + 2 * x + x ^ 2) / 8 + -1/2 * x / (x ^ 2 + -1)` | `C - ln(1 + (-2) / (x + 1)) / 4 + -1/2 * x / (x ^ 2 + -1)` — the same value, one logarithm rather than two |
| **Silent** | `"1/x - 1/x".ToEntity().Simplify()`, and every difference of a term from itself where that term can be undefined | `0`, including at `x = 0` where neither side has a value | `0 provided not x = 0` |
| **Silent** | `"(x + 1)!/(x + 1)!".ToEntity().Simplify()`, and every cancelled quotient whose repeated part can be undefined | `1 provided not (1 + x)! = 0` | `1 provided 1 + x in RR and (1 + x >= 0 or not 1 + x in ZZ)` — the same value everywhere, a condition that says why |
| | `"ln(x)/ln(x)".ToEntity().Simplify()`, and every quotient divided out by a divisor that can be undefined | `1 provided not ln(x) = 0` — which is `1` at `x = 0`, where the quotient has no value | `1 provided not ln(x) = 0 and not x = 0` |
| | `Transformation.CanonicalizationOverGraph(budget).ApplyOrKeep("(x + y) * (x - y)")`, and every input one of `Common`'s sixteen newly declared rules collects | `(-y + x) * (x + y)` — not the tree `x ^ 2 - y ^ 2` reaches | `-y ^ 2 + x ^ 2` — one tree for both |
| | the same on `x + x`, and `x + x * y` | `x + x`, `x * y + x` — left as written | `2 * x`, `(1 + y) * x` |
| **Silent** | `RewriteRules.Common.Rules[i].Growth`, for sixteen rules | `Unknown` | `Collects` for fourteen, `Rearranges` and `Expands` for the reciprocal-factor pair |
| **Silent** | what `RewriteRuleGrowth.Collects` promises | fewer nodes for every input | never more, and fewer for some — every rule that was `Collects` still is |
| | `Transformation.CanonicalizationOverGraph(budget).ApplyOrKeep("(2 / x) ^ 3 * x")`, and its like with a power of the denominator | `(2 * 1 / x) ^ 3 * x` — left as written | `8 * x ^ (-2)`; `(2 / x) ^ 3 * x ^ 2` is `8 * 1 / x` |
| **Silent** | `RewriteRules.Power.Rules[i].Growth` for nine rules, and `Factorization`'s for four | `Unknown` | `Collects` |
| | `Transformation.CanonicalizationOverGraph(budget).ApplyOrKeep("(x ^ 2) ^ 3 - x ^ 6")`, and every input whose repeated term only appears once a number inside it folds | `-x ^ 6 + x ^ 6` — the rule pass after the graph folded the power, too late for the graph to see the two terms as one | `0` |
| **Silent** | `"(i/x)/abs(i/x)".Limit("x", +oo)`, and every quotient of an expression by its own modulus whose phase is not zero | `1` | `i` — the phase; `-i` at `-oo`, `-1` for the square, `3/5 + 4/5 * i` for `(3 + 4i) * x` |
| **Silent** | `"limit((2 + i * x) / abs(2 + i * x), x, +oo)".Simplify()`, and `((1+i)/x)^2/abs((1+i)/x)^2` at `+oo` | `0`; `1` | left unevaluated — both are `i` |
| **Silent** | `"abs(i * x)".Differentiate("x")`, and every modulus of a numeric multiple with an exact modulus | `sgn(i * x) * i provided not i * x = 0` — which is `-sgn(x)` | `sgn(x) provided not x = 0` |
| **Silent** | `"abs(2 + i * x)".Differentiate("x")`, `sgn(2 + i * x)`, and every modulus or sign of an argument not shown real — a symbol's included | `sgn(2 + i * x) * i provided …`, `0 provided …` | left unevaluated |
| | `"abs(i * x)".Simplify()`, and every modulus of a numeric multiple with an exact modulus off the real line | `abs(i * x)` — left as written | `abs(x)` |
| **Silent** | `"0 * (i * +oo)".Evaled`, and every whole zero times a complex infinity | `0` | `NaN` |
| | `RewriteRules.Common.Rules.Count`, and `RewriteRules.All` by growth | `62`; 124 / 46 / 31 / 123 | `65`; 124 / 49 / 31 / 123 |
| | `"(x - b) / (x + a) + c / (x + a)".SolveEquation("x")`, and every equation the replacement machinery solves whose condition is spelled from the equation as written | `{ -(-b + c) provided not a + -(-b + c) = 0 }` | `{ -(-b + c) provided not -(-b + c) + a = 0 }` — the same set, the condition's terms in the order the equation had |
| | `"sum(x^k / k!, k, 0, +oo)".ToEntity().Simplify()`, and every summation to `+oo` of a polynomial in the index times a power with the index in the exponent over a factorial of the index | `sum(x ^ k / k!, k, 0, +oo)` — left as written | `e ^ x`; `sum(3^(k+2) * (k^2 + k + 1) / (k + 3)!, k, 0, +oo)` is `4/3 * e ^ 3 - 41/6` |
| | `"sum(N! / (k! * (N - k)!) * x^k, k, 0, N)".ToEntity().Simplify()`, and every binomial sum with a power or a cosine or sine of the index as its weight | `sum(N! / (k! * (N - k)!) * x ^ k, k, 0, N)` — left as written | `piecewise((1 + x) ^ N provided N >= 0, 0)`; with `cos(k * pi / 3)`, `piecewise(2 ^ N * cos(pi / 6) ^ N * cos(N * pi / 6) provided N >= 0, 0)` |
| | `"ln(4/3) + ln(16/9) / 2".ToEntity().Simplify()`, and every sum of logarithms of rational literals one of which is a perfect power of another | `ln(4/3) + ln(16/9) / 2` — left as written | `2 * ln(4/3)`; the integral it came from, `integral((2x^2+x+1)/(x^3+x^2+x+1), x, 3/4, 4/3)`, answers `ln(16/9)` |
| | `"sum(x^k, k, 0, +oo)".ToEntity().Simplify()`, and every summation of a power with the index in the exponent times something free of the index, to a bound or to `+oo` | left as written | `1 / (1 - x) provided abs(x) < 1`; `sum(2^(-k), k, 0, +oo)` is `2`, `sum(x^k, k, 0, n)` is a piecewise with the ratio 1 and the empty range as cases of their own, and `integral(2^(-floor(x)), x, 0, +oo)` is `2` |
| | `"sum(2^k, k, 0, +oo)".ToEntity().Simplify()`, and every summation to `+oo` whose terms do not tend to zero and whose limit has a sign | left as written | `+oo`, or `-oo` where the limit is negative; `sum(k, k, 1, +oo)` is `+oo` and `integral(floor(x)^floor(x), x, 1, +oo)` is `+oo`. A vanishing limit, a limit that does not exist, and a pole in the index are each still left as written |
| **Loud** | `"divides".ToEntity()`, and every expression with a name spelt `divides` in it | the variable `divides`; `2 divides` was `2 * divides` and `3 divides 12` was `3 * divides ^ 12` | a parse error — `divides` is the keyword of the new statement `a divides b`, which reads `3 divides 12` as the statement that 12 is a multiple of 3 |
| **Silent** | `"card(x)".ToEntity()` and `"#x"`, and every expression with a name spelt `card` followed by a parenthesis, or a `#` | `card * x` — a variable times the parenthesis; `#` was a parse error; `card({ 1, 2, 3 })` evaluated to the set `{ card, card * 2, card * 3 }` | `#x`, the number of elements of the set `x` (`card( )` accepted, `#` canonical); `#{ 1, 2, 3 }` is `3` |
| **Silent** | `"integral(piecewise(2x provided x <= 1/2, 2 - 2x), x, 0, 1)".ToEntity().Simplify()`, and every definite integral of a piecewise whose conditions mention the variable | `1` — the first case's antiderivative at both ends | `1/2` — split at the case boundaries; `piecewise(x provided x < 1, x^2)` from 0 to 2 was `piecewise(2 provided x < 1, 8/3)`, with the integration variable still in it, and is `17/6` |
| **Silent** | `"integral(x - floor(x), x, 0, 3)".ToEntity().Simplify()`, and every definite integral over numeric bounds whose integrand has `floor(x)` or `ceil(x)` in it | `0` — an antiderivative that took the floor for a constant across its jumps | `3/2` — split at the jumps; `(x - floor(x))^2` from 0 to 4 was `0` and is `4/3` |
| **Silent** | `"integral((x - floor(x))^2, x, 0, n)".ToEntity().Simplify()`, and every such integral with a symbolic bound or a condition against a symbol | `(n - floor(n)) ^ 3 / 3` — wrong for every whole `n` but 0; `piecewise(x provided x < a, 0)` from 0 to 2 was `piecewise(2 provided x < a, 0)` | left as written |
| **Silent** | `"sum(piecewise(k provided k < a, 0), k, 0, 5)".ToEntity().Simplify()`, and every piecewise carrying a case whose predicate entails an earlier one | 32 cases, most of them unreachable | 6 — the unreachable ones are dropped, and the value at every `a` is unchanged |
| | `"integral((x - floor(x)) / floor(x)!, x, 1, +oo)".ToEntity().Simplify()`, and `integral(floor(x), x, 0, 5)` | left as written | `(e - 1) / 2`, `10` |
| **Silent** | `Determinant`, `Inverse` and `Adjugate` of a symbolic matrix, and the elementwise operators, called from more than one thread | a well-formed but **wrong** entity carrying another computation's values — 38 of 40 determinants disagreed with their single-threaded selves; the elementwise operators threw on a corrupted dictionary | the same answer a single thread gets. Unchanged on one thread; the determinant and inverse now serialise across threads where the polynomial elimination declines the matrix |

### A piecewise case that can never be reached is dropped

A piecewise takes its **first** matching case, so a case whose predicate entails an earlier one is
unreachable: wherever the later predicate holds the earlier one holds too, and the earlier case is
taken. That was already applied where two predicates were *equal*; it is now applied wherever the
entailment can be proven, which is what `2 < a` after `1 < a` is.

Where it shows most is a binder distributed over a piecewise, which produces one case per **subset**
of the conditions — `2^n` of them, the great majority unreachable. Summing
`piecewise(k provided k < a, 0)` over `k` from 0 to 5 gave **32 cases**; six of them are reachable,
which is one per interval that `a` can fall in, and six is what it gives now.

The test is one-directional and deliberately incomplete: entailment between arbitrary predicates is
not decidable, so the answers are "proven" and "not proven", and only the first drops a case. A
missed entailment costs a longer answer; a wrong one would delete a reachable case and change the
value, so nothing here is a heuristic. Comparisons of one expression against numbers are compared
by their bounds, a conjunction is at least as strong as either half, and a disjunction is at most as
strong as both. `>` after `>=` at the same bound is **kept**, the two differing exactly at the
endpoint. Question I.3 of [#1212](https://github.com/asc-community/AngouriMath/issues/1212). Both
columns measured on a build, `c2ab5da1` against this change.

| | Was | Is |
|---|---|---|
| `"sum(piecewise(k provided k < a, 0), k, 0, 5)".ToEntity().Simplify()` | 32 cases | 6 — one per interval `a` can fall in, and the same value at every `a` |
| `"sum(piecewise(k provided k < a, 0), k, 0, 2)".ToEntity().Simplify()` | `piecewise(3 provided (1 < a and 2 < a), 1 provided (1 < a), 2 provided (2 < a), 0 provided True)` | the same without `2 provided (2 < a)`, which `1 < a` had already caught |
| `"piecewise(1 provided x > 1, 2 provided x > 2, 3)"`, and the same with `x < 2` before `x < 1` | 3 cases | 2 |
| `"piecewise(1 provided x > 1, 2 provided x > 1 and x < 5, 3)"` | 3 cases | 2 — a conjunction is at least as strong as either half |
| `"piecewise(1 provided x > 2, 2 provided x > 1, 3)"`, `piecewise(1 provided x > 1, 2 provided x >= 1, 3)` | 3 cases | the same — a looser bound after a tighter one is reachable, and so is `>=` after `>` at one bound |

### A cancelled quotient says its operand is defined, not only non-zero

`a / a = 1` and `(keep * c) / c = keep` attached `provided c is not zero`. That excludes the zero of
`c` and not the points where `c` has **no value at all**, and where `c` has none the quotient has none
either — while the cancelled result does.

```
"ln(x) / ln(x)".Simplify()   was  1 provided not ln(x) = 0    at x = 0: 1, and the quotient is NaN
```

Both now conjoin the operand's own `DomainCondition`, the way `k - k = 0` does since
[#1169](https://github.com/asc-community/AngouriMath/issues/1169).
[#1174](https://github.com/asc-community/AngouriMath/issues/1174)

**Only one shape's printed answer moves, and its value does not.** `(x + 1)!/(x + 1)!` carries the
factorial's domain instead of `not (1 + x)! = 0`. Measured at x = -4, -3, -2, -1, 0, 2 and -3/2, the
old condition and the new one agree with the unsimplified quotient at every point — `NaN` at the
three gamma poles and `1` elsewhere. The old form was adequate there by accident, since `(1 + x)!` is
`NaN` at a pole and a comparison against `NaN` is not `True`; the new one states the reason rather
than relying on it. `x / x`, `sin(x) / sin(x)`, `(x * y) / (x * y)` and every other operand that
cannot be undefined fold the added clause away and are unchanged.

**And the polynomial division carries its divisor's domain too.** `n / d = quotient + remainder` is
only the quotient where `d` has a value: `ln(x) / ln(x)` divides out to `1 + 0 / ln(x)`, which is `1`
at `x = 0` while the quotient it came from is undefined there, because the remainder term carries
only `ln(x) != 0`. That candidate is the one `Simplify` rated best, so it was what a caller actually
saw. With it fixed:

```
"ln(x) / ln(x)".Simplify()   was  1 provided not ln(x) = 0    at x = 0: 1
                             is   1 provided not ln(x) = 0 and not x = 0    at x = 0: NaN
```

Ordinary divisions are untouched, because a divisor that cannot be undefined has a domain condition
of `True` and it folds away — `(x^2 - 1)/(x - 1)`, `(x^3 + 1)/(x + 1)`, `x^2 / x` and `x^3 / x^2` all
answer exactly what they did. `x!/x!` moves the same way `(x + 1)!/(x + 1)!` does, and for the same
reason; measured at x = -4, -3, -2, -1, 0, 1, 3, 1/2 and -1/2, old and new agree at every point.

### A term subtracted from itself says what it assumes

`k - k = 0` was declared `Sound`, which means it holds for every value the pattern admits with
nothing assumed. It assumed one: that `k` has a value.

```
"1/x - 1/x".Simplify()          was  0                    is  0 provided not x = 0
"ln(x) - ln(x)".Simplify()      was  0                    is  0 provided not x = 0
"x^(-2) - x^(-2)".Simplify()    was  0                    is  0 provided not x = 0
```

At `x = 0` the left side is undefined and `0` is not, so the rewrite invented an answer. Its sibling
for the same shape, `a / a = 1`, has always attached `provided a is not zero`; this one attached
nothing ([#1169](https://github.com/asc-community/AngouriMath/issues/1169)).

**Nothing else moves.** The condition is the operand's own `DomainCondition`, which is trivially
true for anything that cannot be undefined and folds away — `x - x`, `sin(x) - sin(x)`,
`(x + 1) - (x + 1)`, `x * y - x * y` and `sqrt(x) - sqrt(x)` all still answer a bare `0`, and
`x - x + y` still answers `y`. Only the three shapes above gain a condition, and each of them is one
that was wrong.

Found by `RuleEffectsMeasuredTest.NoSoundRuleChangesWhereItsResultHasAValue`, which substitutes a
real for every free variable — **zero among them** — and compares whether both sides still evaluate
to something finite.

### A repeated quadratic denominator is integrated

`1/(x^2 + 1)^2` had no antiderivative, while `1/(x^2 - 1)^2` and `1/(x^2 + 2x + 1)^2` both did — a
denominator with real roots comes apart into linear factors and never reached the gap. What was
missing was the irreducible case
([#180](https://github.com/asc-community/AngouriMath/issues/180)).

```
"1/(x^2 + 1)^2".Integrate("x")     was  integral(1 / (1 + x ^ 2) ^ 2, x)
                                    is  arctan(x) / 2 + C + 1/2 * x / (x ^ 2 + 1)
"x^2/(x^2 + 2)^2".Integrate("x")   was  integral(x ^ 2 / (x ^ 2 + 2) ^ 2, x)
                                    is  (sqrt(2) * arctan(sqrt(2) * x / 2) * 2 + (-4) * x / (x ^ 2 + 2)) / 8
                                          + C provided not x ^ 2 + 2 = 0
```

The condition on that second one is the library's own and it is true: the antiderivative has no
value where the denominator vanishes. It is attached to some of these answers and not others, and I
did not pin down what decides which — recorded as observed rather than explained.

Writing `Q` for the quadratic, `u` for its derivative `2ax + b` and `D` for `4ac - b^2`, the
identity `u^2 = 4aQ - D` turns the derivative of `u/Q^(m-1)` into an equation for the integral of
`1/Q^m`, which is unrolled down to the single power the table already answered. It is an identity in
`a`, `b` and `c` rather than a fact about signs, so one line serves both signs of the discriminant;
`D = 0` is the sole exclusion, and the division by `D` is why. A numerator of higher degree is
divided by `Q` first.

**A denominator with real roots now takes this route rather than partial fractions.** The value is
unchanged and the form is shorter — one logarithm where there were two — which is why the
node-count metric that ranks candidates selects it. No test pinned the old spelling.

**The cost, measured rather than estimated.** Where the integrand is rational this is between 26 and
165 times faster, because `TryStandardIntegrals` runs before every search and a shape answered
outright never enters the candidate exploration. Where the integrand is a transcendental function
over a repeated quadratic — `sin(x)/(x^2 + 1)^2`, `e^x/(x^2 + 1)^2`, `ln(x)/(x^2 + 1)^2`, none of
which has an elementary antiderivative — the same search now takes **3.4 to 10.8 times longer to
conclude nothing**, because the sub-integral it used to fail on immediately now succeeds and
integration by parts carries on from a larger expression. Both arms measured on one machine, master
and branch, the same way:

| | master | now |
|---|---|---|
| `1/(x^2 + 1)^2` | 2879 ms, unevaluated | **111 ms, answered** |
| `x^2/(x^2 + 2)^2` | 92 990 ms, unevaluated | **565 ms, answered** |
| `1/(x^4 + 1)^2` | 3811 ms | 3870 ms — the base is not quadratic, and nothing changes |
| `ln(x)/(x^2 + 1)^2` | 15 034 ms, unevaluated | 51 072 ms, unevaluated |
| `sin(x)/(x^2 + 1)^2` | 26 492 ms, unevaluated | 114 851 ms, unevaluated |
| `e^x/(x^2 + 1)^2` | 7944 ms, unevaluated | 85 683 ms, unevaluated |

That last group is a growth in integration by parts rather than in this rule, which those integrands
never match; it is filed separately rather than fixed here, since bounding by-parts is a change to
machinery every integral goes through.

### A nested radical comes apart

`sqrt(5 + 2*sqrt(6))` is a radical under a radical, and it is two plain ones added.

```
"sqrt(5 + 2 * sqrt(6))".Simplify()   was  sqrt(5 + 2 * sqrt(6))   is  sqrt(3) + sqrt(2)
"sqrt(7 - 4 * sqrt(3))".Simplify()   was  as written              is  2 - sqrt(3)
"sqrt(9 + 4 * sqrt(5))".Simplify()   was  as written              is  sqrt(5) + 2
"sqrt(11 + 6 * sqrt(2))".Simplify()  was  as written              is  3 + sqrt(2)
"sqrt(6 - 2 * sqrt(5))".Simplify()   was  as written              is  sqrt(5) - 1
```

Squaring `sqrt(x) + sqrt(y)` gives `x + y + 2*sqrt(x*y)`, so matching that against
`a + b*sqrt(c)` makes `x` and `y` the roots of `t^2 - a*t + b^2*c/4`. They are rational exactly
when `a^2 - b^2*c` is the square of a rational, and that is the whole test — decidable in exact
arithmetic rather than a search. The sign of `b` chooses the sum or the difference, since squaring
either gives `a + |b|*sqrt(c)`.

**No condition is attached and none is owed.** A non-negative `a` and a non-negative discriminant
are required before anything is built, and together they make the radicand, `x` and `y` all
non-negative — so what fires is an identity between real numbers with no branch chosen. A negative
`a` is refused rather than conditioned.

**A radicand with no rational split is left as written**, `sqrt(1 + sqrt(2))` among them, and so
is anything that is not `a + b*sqrt(c)`.

**Where the denesting is longer, `Simplify` keeps the nested form.** `sqrt(2 + sqrt(3))` does come
apart — to `(sqrt(6) + sqrt(2))/2` — and the nested spelling is the shorter of the two, so that is
what comes back. The rule offers the alternative; the selection is by size, as it is everywhere
else.

One radicand is a boundary rather than a rule: `sqrt(3 + 2*sqrt(2))` is answered by the rule —
applying the set gives `sqrt(2) + 1`, the shorter form — and `Simplify` nonetheless returns the
nested one. There is a test pinning what happens; the cause is not established, and the obvious
suspect was measured and ruled out.

[#717](https://github.com/asc-community/AngouriMath/issues/717).

### The extremum of an expression over a set, as a binder

An expression had no way to name its largest value over a set. `max` and `min` took two values and
returned the larger or smaller, and `max(f(t), t in S)` parsed as the two-value `max` of `f(t)` and
the membership statement `t in S` — a category error that stayed unevaluated. `argmax` and `argmin`
were not functions at all. Four binders now read the second argument as `variable in set` and bind
the variable over the expression and the set: `Maximumf`, `Minimumf`, `Argmaxf`, `Argminf`, spelt
`max(f, t in S)`, `min`, `argmax`, `argmin`, printed `\max_{t \in S} f` and
`\operatorname{argmax}_{t \in S} f` in LaTeX. The two-value `max(a, b)` is untouched, and a second
argument that is not `variable in set` — `max(x, y)`, or `max(x, 1 in S)` — stays the two-value form.

The value is answered over a finite set of numbers, by evaluating at each and comparing, and over a
closed interval with numeric ends for an expression whose extrema are all stationary — sums,
products, whole non-negative powers, sines, cosines, exponentials with a positive base — by
comparing the closed endpoints with the derivative's zeros inside, a periodic family of them
enumerated where it lands in the interval. The best candidate is checked against the expression
sampled along the interval, and the question is left as written where a sample beats it, since the
solver's list of zeros is not guaranteed complete. An open endpoint is not a candidate, so
`max(x, x in [0; 1))` has no maximum and stays as written; a symbolic set or end, a pole, a kink,
and a set with a symbol in it are all left as written. `argmax` and `argmin` return the set of
points. Question I.6 of [#1212](https://github.com/asc-community/AngouriMath/issues/1212). Both
columns measured on a build, `27ed5b53` against this change.

| | Was | Is |
|---|---|---|
| `"max(sin(t)^3 * cos(t), t in [0; pi/2])".ToEntity().Simplify()` | `max(sin(t)^3 * cos(t), t in [0; pi/2])` — a two-value `max` with a membership statement, unevaluated | `3/16 * sqrt(3)` |
| `"argmax(sin(t)^3 * cos(t), t in [0; pi/2])".ToEntity()` | a parse error — `argmax` was not a function | `{ pi/3 }` once simplified |
| `"max(x^3 - 3x, x in [-2; 2])".ToEntity().Simplify()`, and `min` | a two-value `max`/`min` with a membership statement | `2`, `-2` |
| `"max(x^2, x in { 1, -3, 2 })".ToEntity().Simplify()`, `argmax` of the same | `max(x^2, x in { 1, -3, 2 })` | `9`, `{ -3 }` |
| `"max(1, 2)".ToEntity().Evaled`, `max(x, y)` | `2`, `max(x, y)` | the same — the two-value form is untouched |
| `"max(x, x in [0; 1))".ToEntity().Simplify()` | a two-value `max` with a membership statement | left as written — the value at an open end is not attained |

### A polynomial summand is summed in closed form

A summation wrote itself out term by term where the bounds were concrete and there were fewer
than a hundred terms, and was carried otherwise. So a symbolic bound had no answer, and neither
did a long concrete range — both now do, where the body is a polynomial in the index.

```
"sum(k, k, 1, n)".Simplify()        was  sum(k, k, 1, n)
                                    is   piecewise((n + n ^ 2) / 2 provided n >= 0, 0)

"sum(k, k, 1, 100000)".Simplify()   was  sum(k, k, 1, 100000)
                                    is   5000050000
```

`sum(k^2, k, 1, n)` and `sum(k^3, k, 1, n)` come with it, as does any polynomial summand by
linearity, a coefficient that does not mention the index — `sum(a*k^2 + b*k + c, k, 1, n)` — and a
symbolic *lower* bound, `sum(k, k, m, n)` being `S(n) - S(m - 1)` like any other.

**The condition is the entry.** `sum(k, k, 1, n)` is **not** `(n + n^2)/2` for every `n`. At
`n = -2` the range is empty, and this library answers an empty range with the operator's identity
— `sum(k, k, 5, 1)` is `0`, which has its own test — while the polynomial there is `1`. The
identity holds exactly where `to >= from - 1`, so that is what is attached, with the empty-range
value as the other branch. Where the bounds are concrete the condition is decidable and the whole
thing collapses to a number, which is why the long range above is an integer and not a piecewise.

SymPy prints the bare polynomial for the same input and is not making a mistake: it reads a
reversed range as the negated sum over the flipped one, under which the identity needs no
condition. The condition is what this library's different convention costs, and **code that
expected a bare polynomial from `Simplify` gets a `Piecewise`.**

**A bound that is a number and not a whole one is still carried.** The index runs over the
integers, so `sum(k, k, 1, 5/2)` is `1 + 2`; the polynomial continued to `5/2` is `35/8`, which
answers a different question. `+oo` is refused the same way, so an infinite series is untouched.

**A product is unchanged, whatever its body.** `product(k, k, 1, n)` is still carried, and
`factorial(n)` would be a wrong answer for it rather than a missing one: the empty product is `1`
at every `n < 1`, and `factorial` is undefined at the negative integers. Answering it needs the
same kind of condition the sum now carries, and is not done here. *It is done in the entry below,
which gives it that condition.*

No Bernoulli numbers are involved. The sum of a degree-`d` polynomial is a polynomial of degree
`d + 1`, so `d + 2` of its values determine it, and those values are short sums computed directly;
interpolating them recovers the coefficients exactly in rational arithmetic.

[#717](https://github.com/asc-community/AngouriMath/issues/717).

### A monomial body is multiplied in closed form

The same for `product`, with the narrower reach a product has: a sum of two terms is the sum of
their sums, and a product of two terms is not the product of their products in any way that
helps. What separates is the body that **is** one term.

```
"product(k, k, 1, n)".Simplify()      was  product(k, k, 1, n)
                                      is   piecewise(n! provided n >= 1, 1)

"product(2, k, 1, 500)".Simplify()    was  product(2, k, 1, 500)
                                      is   3273390607896141870013189696827599152216642046043064789483291368096133796404674554883270092325904157150886684127560071009217256545885393053328527589376
```

`product(k^2, k, 1, n)` is `(n!)^2`, `product(2 * k, k, 1, n)` is `n! * 2^n`, and a constant body
needs no factorial at all — `product(c, k, m, n)` is `c^(n - m + 1)`, symbolic lower bound and
all.

**The condition is `to >= from`, where the sum's is `to >= from - 1`,** and the one point between
them is the whole reason. At the empty range the closed form is `c^0`, which is `1` for every `c`
except zero and undefined there, while the empty product is `1` for every `c` including zero.
Giving that point to the identity branch keeps a value from becoming an undefinedness, and costs
nothing, since both branches say `1` there.

**A lower bound that is not a concrete integer of at least one is declined** where the index is in
the body, rather than conditioned. `product(k, k, a, b)` is `b!/(a-1)!` only for `a >= 1`; below
that the range runs through zero so the product is `0`, while `(a-1)!` is undefined. That cannot
share a branch with the empty-range case, because `a < 1` does not make the range empty — a
piecewise reading "identity otherwise" would be wrong there. So `product(k, k, 0, n)` and
`product(k, k, m, n)` stay as written.

**What is not one term is carried**, `product(k + 1, k, 1, n)` included. As for the sum, a bound
that is a number and not a whole one is carried too.

[#717](https://github.com/asc-community/AngouriMath/issues/717).

### A lambda is written with an arrow as well as a call

`a => a + 3` was a parse error and is now the same entity as `lambda(a, a + 3)`. Several
parameters are the curried form the plan in
[#495](https://github.com/asc-community/AngouriMath/issues/495) specifies — `a b => a + b` is
`a => b => a + b`, which is `lambda(a, b, a + b)`.

```
"a => a + 3".ToEntity()                          was  UnhandledParseException
                                                 is   lambda(a, a + 3)
"a b => a + b".ToEntity()                        is   lambda(a, lambda(b, a + b))
"apply(apply(a b => a + b, 1), 2)".Simplify()    is   3
```

**Nothing that parsed before parses differently.** `=` followed by `>` was not a token and not a
parse, so no reading of any valid input has changed; `>=`, `->`, `=` and the rest are untouched,
and there are tests pinning them. The `Lambda` node, beta reduction and currying were all already
there — this is the syntax for them.

**The arrow is read, not printed.** A lambda still prints as `lambda(x, x + 1)`, which is what
keeps the round trip the printed form promises: several spellings may be read, exactly one is
printed.

**Every parameter must be a name**, which is what the plan says. `a 3 => 3` is refused, and so
are `2 => 3` and `x + 1 => 2`. Those raised `UnhandledParseException` before and now raise
`InvalidArgumentParseException` — still invalid, differently named. Code catching the parse
exception by type around input like that will not catch this one.

An index called `i` is the name rather than the imaginary unit, matching `lambda(i, i + 1)`,
which it gets by reading its parameters through the same `Binding` the call form uses
([#976](https://github.com/asc-community/AngouriMath/issues/976)).

**Not in this change:** the rest of that plan's syntax — `f a b` for `apply(apply(f, a), b)`,
`sin x` without brackets, and `sin (x)` with a space. Each of those changes what juxtaposition
means, which is the decision [#286](https://github.com/asc-community/AngouriMath/issues/286) is
about, and none of them is free the way the arrow is.

[#495](https://github.com/asc-community/AngouriMath/issues/495).

### The exponential of a natural logarithm folds

`"e ^ ln(x)".ToEntity().Simplify()` was `e ^ ln(x)` and is `x`; so are `e ^ ln(2 * x)`,
`e ^ ln(x + 1)` and `e ^ ln(sin(x))`. Nothing that had a value changes value.

The identity was not missing. `2 ^ log(2, x)` has simplified to `x` throughout, and the rule that
does it could not reach `e`: `ln(a)` is stored as `log(e, a)`, `e` is a `Constant` rather than a
`Number`, and the pattern binds its base with `Any<Number>`, so the base never matched however the
logarithm was written. That is
[#994](https://github.com/asc-community/AngouriMath/issues/994) — every logarithm carrying a constant
it does not mention — showing up as a missing simplification rather than as a printed one.

Nothing is assumed. `b ^ log(b, a) = a` needs `ln(b)` to be non-zero, and `e` is decidably neither
`0` nor `1`, which is exactly what the numeric arm cannot say about an arbitrary `Number` — so a
symbolic base stays refused, and `1 ^ log(1, x)` is still `NaN` rather than `x`. It holds off the
positive reals, on the principal branch: at `a = -3`, `ln(-3)` is `ln(3) + i*pi` and
`e ^ (ln(3) + i*pi)` is `-3`. At `a = 0` no definedness moves either, since this library reads
`ln(0)` as `-oo` and `e ^ (-oo)` as `0`, so both sides are `0`. It is labelled
`SoundUnderAssumptions` rather than `Sound` because that last part is a branch convention.

**No ODE changes.** The issue was filed believing this cost
[#241](https://github.com/asc-community/AngouriMath/issues/241)'s solver its integrating factor. It
does not: `OrdinaryDifferentialEquation` carries its own `ExponentialOf` helper that already folds
`e ^ ln(u)`, so the eight first-order linear equations measured across this change come back
**byte-identical**. *An earlier version of this entry went one step further and said the rule makes
that helper redundant, with removing it left as its own change. It does not, and the removal was
measured rather than done: the solver asks `InnerSimplified`, which does not carry the rewrite rules,
so `e ^ ln(x)` reaches it unfolded. Deleting the helper fails three `OrdinaryDifferentialEquationTest`
cases, deleting only its `e ^ ln(u)` arm fails two, and calling `Simplify` at the call site instead
still fails one — because nothing in the library folds `e ^ (k ln u)` to `u ^ k`, which is the shape
an antiderivative of `k/x` gives. The helper stays, and its doc comment now records why.*

[#1138](https://github.com/asc-community/AngouriMath/issues/1138).

### Every rule set describes the rules it runs

`RewriteRuleSet.Rules` is what the registry reports a set is made of, and for most of the library's
life it came from `RuleRegistryGenerator` reading the `switch` that defined the set. Twenty-seven of
the thirty sets stopped running that `switch` some releases ago — they run
`MatchedRuleSet.ApplyHere` — and went on describing it. Thirteen of them now describe what they run.

**All twenty-seven of them**, over six changes. Thirteen had **no described arm at all**, so repointing them could only add
metadata: `CollapseMultipleFractions`, the three `CommonDenominator` sets, `DivisionPreparing`,
`ExpandFactorialDivisions`, `ExpandMultipleAngle`, `ExpandTrigonometric`, `Expansion`,
`FactorizeFactorialMultiplications`, `NormalTrigonometricForm`, `PhiFunction` and
`PolynomialLongDivision`. Six more are **one arm to one rule**, so their existing descriptions carry
across unchanged and the rules that had none gain one: `CollapseTrigonometricFunctions`,
`InvertNegativeMultipliers`, `InvertNegativePowers`, `PerfectSquare`, `PolynomialGcdCancellation` and
`SetOperator`. `RationalizeDenominator` was already reading its data form. And `Boolean` is the first
where the identities were **written** rather than carried across: its comments named the laws
(De Morgan, absorption, contraposition) where an identity was wanted, so all twenty were read off the
rules' own patterns and replacements. `NumericNeat` and `Factorization` follow it, their comments
already being identities and needing only the arrow turned into an equals sign.

Nothing is left on the `switch` that does not run it. The three `CanonicalOrder` sets still *run*
theirs, so describing it is not a mismatch and they are not targets.
the `CanonicalOrder` family — still *run* their `switch`, so describing it is not a mismatch.

Four things move, and only the second changes a count:

| | Was | Is |
|---|---|---|
| `Rules[i].Name` | the arm's rendered pattern, `Mulf(var any1, Divf(Integer(1), var any2))` | the rule's name, `reciprocal-factor-becomes-a-quotient` |
| `Rules.Count`, for the two factorial sets | `8` | `3` |
| `Rules[i].Description` | `null` for 38 of these arms and set for 8 | the identity, `a * (1 / b) = a / b`, for all 59 |
| `Rules[i].Soundness` | `null` — an arm declares no tier | the rule's own tier |

**The three counts that change are not rewrites lost.** They are arms the data form writes once.
`Boolean`'s thirty-six become twenty: eight arms of distributivity are two rules, because a
commutative pattern finds the shared operand wherever it sits, and absorption's four-arms-each is one
rule twice. `ExpandFactorialDivisions` and `FactorizeFactorialMultiplications` are eight arms each
written as three, the other five being one rewrite spelled once for each side a factorial can sit on.
`NumericNeat`'s sixteen are eleven, six of them being three rules written once per side a negative
factor can sit on; `Factorization`'s twenty-two are eleven for the same reason, and
`Trigonometric`'s forty-three are thirty-three, `Power`'s thirty-five are thirty-one, and
`Common`'s hundred are sixty-two — the largest collapse, and the same cause: four orientations of a
shared-factor rule are one commutative pattern on each side of the sum. Every other repointed set is
one arm to one rule. Across the registry, 407 becomes **313** while the number of described rules
goes from **95 to 292** — which is every rule the registry now reports.

`Rules[i].Growth` also stops being a guess. `AsAddressable` used to infer it by comparing the lengths
of the two rendered pattern strings — the only thing available to a generator reading source text —
and the exact node count disagrees 23 times in 322. Mostly it corrects a wrong answer; for
`ExpandFactorialDivisions` it replaces one with `Unknown`, which is right, since a quotient of
factorials collects when the offsets are one apart and expands when they are five.

`PatternSource` changes too, from the C# the arm was written in to the pattern the matcher holds —
`Mulf(var a, Divf(1, var b))` for the same rule. Both are source text for reading; neither is
something to match against.

### A step is justified by the rule that fired, not by the set it came from

`RewriteStep.Soundness` read `RuleSet.Soundness` and nothing else, so every rewrite of a set reported
the same tier. A set's tier is the **minimum** over its rules, and one conditional rule is enough to
make a set of a hundred report as conditional: all thirty sets in the registry declare
`SoundUnderAssumptions`, while **181 of the 322 rules written as data are `Sound`** — they hold for
every complex argument, with nothing assumed.

A rewrite now reports its rule's own tier where the rule has one, and its set's where it has not:

```csharp
using var recording = RewriteRecording.Start();
RewriteRules.SetOperator.ApplyOnce(@"A /\ A".ToEntity());
recording.Dispose();
recording.Steps[0].Soundness           // was SoundUnderAssumptions, is Sound
recording.Steps[0].RuleSet.Soundness   // still SoundUnderAssumptions, and correctly so
```

The fallback is not a claim. A rule read off a `switch` declares no tier, so `RewriteRule.Soundness`
is `null` there and the set's tier is what is known — see *Nineteen rule sets describe the rules they
run* above for which sets carry per-rule tiers today.

`DerivationStep.Soundness` changes for the same reason and one more: it is now the weakest tier any
rewrite that **actually fired** inside the step holds at, rather than its set's minimum over rules
the step may never have reached. A pass of nine unconditional rewrites and one conditional one is
still a conditional pass; a pass of ten unconditional ones now says so.

### A negative `arccotan` is negative

`arccotan` here is `arctan(1/x)`, with range `(-pi/2, pi/2]` — **not** the textbook `(0, pi)`. The
inverse-trigonometric table read it as the textbook `pi/2 - arctan(x)`. The two agree on every
positive argument and on nothing negative, so a closed form came back that was not the value:

```csharp
"arccotan(-1)".ToEntity().EvalNumerical()   // -0.7853981633974483…, which is -pi/4
"arccotan(-1)".ToEntity().Simplify()        // was 3/4 * pi, is -1/4 * pi
```

`Simplify` and `EvalNumerical` disagreeing about a constant is the sharpest form this kind of defect
takes, and it reached every table value with a negative argument: `arccotan(-sqrt(3))` was `5/6 * pi`
and is `-1/6 * pi`, `arccotan(-(2 + sqrt(3)))` was `11/12 * pi` and is `-1/12 * pi`.

**The rule for `arctan(x) + arccotan(x)` had the convention right all along** — it answers `pi/2` for
a non-negative argument and `-pi/2` for a negative one, which is
[#887](https://github.com/asc-community/AngouriMath/issues/887). The table's docstring claimed to
take "the same reading of it" and took the opposite one; the comment asserting agreement is what let
the disagreement stand. The two now agree, and `ArccotanTableSignTest` measures the range at a
positive argument, a negative one and zero rather than recalling it.

`arccos` uses the same complement helper and is **unaffected**: its range is `[0, pi]` and `arcsin`'s
is `[-pi/2, pi/2]`, so `pi/2 - arcsin(x)` holds for every argument. That is asserted too, so that a
later tidy-up cannot merge the two paths back together.

### Subtracting an interval turns it round

`Minusf` slid an interval's ends without swapping them, so subtracting one produced an interval whose
left end was above its right — which is empty:

```csharp
"5 - (0; 1)".ToEntity().Simplify()          // was (5; 4), is (4; 5)
"4.5 in (5 - (0; 1))".ToEntity().Simplify() // was False, is True
```

The second line is the one that matters: a wrong answer reached through the operation an interval
exists for.

**The openness is the half that is easy to miss.** `5 - (0; 1]` is `[4; 5)` — the *excluded* 1 becomes
the excluded lower end 4, and the *included* 0 becomes the included upper end 5. Swapping the ends and
leaving the flags where they were would give `(4; 5]`: right about the width and wrong at both ends,
which no test on the printed form alone would catch. `IntervalSubtractionTest` asserts membership as
well for that reason.

An interval *minus* a number was always right and stays right — `(0; 1) - 5` is `(-5; -4)`, and
nothing turns round because nothing is being reflected.

**What this does not fix**, and the boundary is asserted rather than left implicit: `0 - x` is negated
by an earlier arm, so `0 - [0; 1)` is `-[0; 1)` and stops there. Negating an interval is multiplying
one, and `Mulf` has no interval case at all — `(0; 1) * 2` is left alone too. That is
[#322](https://github.com/asc-community/AngouriMath/issues/322)'s remaining half, and it needs a sign
analysis this does not: a negative multiplier turns the interval round exactly as subtraction does,
and a zero one collapses it to a point.

### An interval scaled by a constant is an interval

`Sumf` and `Minusf` had interval cases and `Mulf` and `Divf` had none, so `(0; 1) + 1` answered
`(1; 2)` while `(0; 1) * 2` was handed back. Three rows of `Core/Sets/Arithmetics` recorded that
asymmetry as the expected behaviour, directly beneath the two addition rows that answer.

```csharp
"3 * [2; 3]".ToEntity().Simplify()   // was 3 * [2; 3], is [6; 9]
"[2; 3] / 2".ToEntity().Simplify()   // was [2; 3] / 2, is [1; 3/2]
```

**A negative factor reflects the interval**, so its ends swap and their openness swaps with them —
exactly as subtracting one does:

| | Is |
|---|---|
| `[2; 3) * (-1)` | `(-3; -2]` |
| `(2; 3] / (-1)` | `[-3; -2)` |
| `0 - [0; 1)` | `(-1; 0]` |

The last of those is not a subtraction at all: `0 - x` is negated by an earlier arm, so it reaches
`Mulf` as `-1 * [0; 1)` and is answered there. It is the case the subtraction fix above had to leave
out, and it comes back for free.

**An unknown sign is answered by not answering.** `(0; 1) * k` for a symbolic `k` is one interval
when `k` is positive and the reflected one when it is negative, so picking either would be choosing
which; it is left unevaluated, which is what an unevaluated node means.

Two boundaries, asserted rather than left to be discovered. `(0; 1) * 0` still answers the number `0`
rather than the set `{ 0 }` — that arm is over every `Entity` and not only intervals, and moving it
would change matrices and finite sets with it. And `2 / (0; 1)` is left alone: a constant over an
interval straddling zero is two unbounded pieces rather than one interval, so there is no `Interval`
to answer with.

This is [#322](https://github.com/asc-community/AngouriMath/issues/322)'s arithmetic half. Its body
says it "will be possible once we implement quantifiers"; scaling needs none — it is monotone in the
factor's sign and in nothing else. Applying a non-monotonic function to an interval, which is the
other half, is still open: `ln((0; 1))` is `(-oo; 0)` because `ln` increases, but `sin((0; 7))` needs
to know where the turning points are.

### `Factorize` takes out a common whole factor

`2x + 2a` has been collected under plain `Simplify` for some time — the factorisation rules take out a
factor that appears *identically* in every term. A factor that is only a common **divisor** was not,
and still is not, because `2 * (x + 2 * a)` is a node larger than `4 * a + 2 * x` and
`Entity.SimplifiedRate` will not choose it.

`Factorize` now does, which is
[#195](https://github.com/asc-community/AngouriMath/issues/195)'s "forcefully… but not peacefully":

```csharp
"2 * x + 4 * a".ToEntity().Factorize()          // was 2 * x + 4 * a, is 2 * (x + 2 * a)
"4 * x + 6 * y + 10 * z".ToEntity().Factorize() // is 2 * (2 * x + 3 * y + 5 * z)
"2 * x - 4 * a".ToEntity().Factorize()          // is 2 * (x - 2 * a)
"2 * x + 4 * a".ToEntity().Simplify()           // unchanged: 4 * a + 2 * x
```

**`Simplify` is untouched**, and that is asserted rather than assumed — the peaceful behaviour is
what a caller who did not ask to factorise still gets.

Whole numbers only, and a positive content. `x/2 + a/3` has a common divisor too, but taking `1/6` out
puts a quotient outside the sum rather than a factor, which is a different rewrite. And the sign stays
inside: `-2x - 4a` is `2 * (-x - 2a)` rather than `-2 * (x + 2a)`, because which of those is wanted is
a second question.

A term whose coefficient is not a whole number stops the whole sum rather than contributing a 1: the
content of `2x + a/3` is not 1, it is a thing this does not compute, and answering 1 would say there
was nothing to take.

`Transformation.NumericContentExtraction` is the step on its own, and `Transformation.Factorization`'s
`Name` gains it — a chain names its parts.

### A negation is no longer answered with the empty set

`StatementSolver.Solve` had arms for equality, the connectives, the four comparisons, membership,
`provided` and `piecewise` — and none for `not`, so every negation fell through to `Set.Empty`.
The empty set is a positive claim, *no x satisfies this*, and it was false of all of them
([#1127](https://github.com/asc-community/AngouriMath/issues/1127)). This is the defect
[#1036](https://github.com/asc-community/AngouriMath/issues/1036) fixed for equations, left
standing for negation.

**Was** — every one of these, and 1 is not a solution of the first while 0 is:

```
"not (x = 1)".ToEntity().Solve("x")             {  }
"not (x > 1)".ToEntity().Solve("x")             {  }
"not (x >= 1)".ToEntity().Solve("x")            {  }
"not not (x = 1)".ToEntity().Solve("x")         {  }
"not (x > 1 or x < -1)".ToEntity().Solve("x")   {  }
"not (x in RR)".ToEntity().Solve("x")           {  }
```

**Is** — the negation pushed inward as far as there is an arm for it, and named as a set-builder
where there is not:

```
"not (x = 1)".ToEntity().Solve("x")             { x : not x = 1 }
"not (x > 1)".ToEntity().Solve("x")             (-oo; 1]
"not (x >= 1)".ToEntity().Solve("x")            (-oo; 1)
"not not (x = 1)".ToEntity().Solve("x")         { 1 }
"not (x > 1 or x < -1)".ToEntity().Solve("x")   [-1; 1]
"not (x in RR)".ToEntity().Solve("x")           { x : not x in RR }
```

A negated comparison is answered as the comparison it is, which is what
`RewriteRules.InequalityEquality` already says; a negated connective is pushed inward by De Morgan,
which is the direction that reaches an arm. What neither reaches is answered as written.

### An implication is solved without naming a universe

`Solve` answered `a implies b` with `Codomain \ solve(a) \/ solve(b)`, taking the complement
inside the **statement node's** codomain. That is `Boolean` for every implication, so a numeric
question came back with a solution set containing `True` and `False`
([#996](https://github.com/asc-community/AngouriMath/issues/996)). A `TODO` on the line asked for a
universal set to subtract from instead; neither is needed, because *the values of `x` where `a`
does not hold* is `{ x : not a }`, which names no universe at all.

**Was** — the domain in the answer is the codomain of the `implies` node, not anything the question
was asked over:

```
"(x = 1) implies (x = 2)".ToEntity().Solve("x")     { 2 } \/ BB
"x > 1 implies x > 0".ToEntity().Solve("x")         BB \ (1; +oo) \/ (0; +oo)
"A implies B".ToEntity().Solve("A")                 BB \ { True }
```

**Is** — a set-builder for the antecedent's complement, united with what the consequent settles:

```
"(x = 1) implies (x = 2)".ToEntity().Solve("x")     { x : not x = 1 }
"x > 1 implies x > 0".ToEntity().Solve("x")         { x : not x > 1 } \/ (0; +oo)
"A implies B".ToEntity().Solve("A")                 { A : not A }
```

The boolean row carries the same information it did before — `BB \ { True }` and `{ A : not A }`
are the same set — without asserting that `A` ranges over `BB`. The implication solver is no more
complete than it was: `solve(b, x)` is still empty where `b` does not mention `x`, so
`A implies True` is `{ A : not A }` rather than `BB`, as it was before.

### An unbounded interval over no constraint is left as written

`(-oo; +oo)` simplifies to the domain it is an interval of. Widened to `Domain.Any` there is no such
domain — `Any` is a codomain and not a set — and asking for one threw out of `Solve` on input a
caller can write ([#996](https://github.com/asc-community/AngouriMath/issues/996)).

**Was**

```
"domain((-oo; +oo), Any) = RR".ToEntity().Solve("x")
    NotSufficientlySupportedException: There is no special set for domain Any
```

**Is** — the interval is left alone, and the statement is solved:

```
"domain((-oo; +oo), Any) = RR".ToEntity().Solve("x")    {  }
"domain((-oo; +oo), Any)".ToEntity().Simplify()         domain((-oo; +oo), Any)
"(-oo; +oo)".ToEntity().Simplify()                      RR                        unchanged
"domain((-oo; +oo), CC)".ToEntity().Simplify()          CC                        unchanged
```

### An annotated rational literal keeps the annotation, `CC` included

A codomain is a property of a node rather than a node of its own, so `domain(1/2, CC)` has to become
one `Rational` carrying `Complex` — and it did not. The pass that reads a quotient of two integer
literals as the rational it denotes ([#873](https://github.com/asc-community/AngouriMath/issues/873))
ran over the **finished** tree, by which point `Complex` means two things at once: it is what an
unannotated `Divf` carries by default, and it is what `domain(x, CC)` asks for. The pass read it as
the first and dropped it.

| | before | now |
|---|---|---|
| `"domain(1/2, CC)".ToEntity().Codomain` | `Rational` — the annotation gone | `Complex` |
| `"domain(1/2, CC)".ToEntity() == "1/2".ToEntity()` | `true` | `false` |
| `(1/2).WithCodomain(Complex).Stringize().ToEntity()` | `1/2`, `Rational` — printed one node, read back another | round-trips |
| `"domain(1/2, RR)".ToEntity()` | `Real`, correct | unchanged |
| `"1/2".ToEntity()` | a `Rational`, `Codomain = Rational` | unchanged |
| `"domain(4/2, CC)".ToEntity()` | a `Divf`, `Codomain = Complex` | unchanged |
| `"domain(1/2, CC)".ToEntity().Evaled` | `1/2` — the annotation erased by evaluating | `domain(1/2, CC)`, carried through |
| the same, `.Simplify()` | `1/2` | `domain(1/2, CC)` |
| the same, `.Latexize()` | `\frac{1}{2}` | `{\left(\frac{1}{2}\right)}_{\mathbb{C}}` |
| `"domain(1/2, CC) + 1".ToEntity().Evaled` | `3/2` | unchanged |

**The fix is where the fold happens, not what it reads.** `domain(...)`'s own parser rule now folds
its argument before annotating it, so the two meanings of `Complex` never meet: an annotation lands
on a node that is already the right shape, and the sweep over the rest of the tree — which is the
only other place the fold runs — can hand back a bare `Rational` and read no codomain at all. It is
sound to do so because those two are the only routes, `domain(...)` being the one syntax that
annotates anything and the sweep meeting only what it did not annotate.

`Complex` on a rational literal is a **widening** — a rational is a complex number — so nothing that
was defined becomes `NaN` and no value changes: `domain(1/2, CC) + 1` evaluates to `3/2` as before,
and `domain(1/2, ZZ)` is still `NaN`. What changes is equality, and everything that follows from the
node genuinely carrying the annotation now — `Evaled` and `Simplify` hand it back instead of erasing
it, and `Latexize` writes the subscript. That is the point: an annotation the caller wrote is no
longer indistinguishable from one they did not.

This was the second of the two gaps
[#1048](https://github.com/asc-community/AngouriMath/issues/1048) recorded, and the last entry in
`CodomainSurvivesPrintingTest.StillUnparseable`. That dictionary is now empty and still asserted in
both directions, so every writable domain on every node type reads back as itself, and a gap added
to it later fails the day it closes.

### An interval bounded by its own element is decided, and `Common` terminates

`a in (a / 2; 2 * a)` was `a > 0` and `a in (a / 3; 3 * a)` was left as written. The difference was
not the mathematics — it was which candidate survived `Simplify`'s pruning.

`ParaphraseInterval` writes a membership out as two comparisons with zero, and the difference it
compares was left as a two-term sum: `a - a / 3` came back as `a + -a / 3`, which no rule about a
sign can read. It is collected now, and the positive factor is divided out where the comparison is
built rather than later — because `Simplify` prunes by `SimplifiedRate`, and
`2/3 * a > 0 and 2 * a > 0` rates **26** against the membership's **25**, one point worse, so it was
discarded before anything could take it to `a > 0`, which rates **8**. The `n = 2` case answered
only because `1/2 * a > 0 and a > 0` happens to rate **24**.

| | before | now |
|---|---|---|
| `"a in (a / 2; 2a)".Simplify()` | `a > 0` | unchanged |
| `"a in (a / 3; 3a)".Simplify()` | `a in (a / 3; 3 * a)` | `a > 0` |
| `"a in (a / 4; 4a)".Simplify()`, and every denominator through 8 | left as written | `a > 0` |
| `"a in (a / 2; 3a)".Simplify()` | `a in (a / 2; 3 * a)` | `a > 0` |
| `"a in (a / 7; 5a)".Simplify()` | left as written | `a > 0` |
| `"a in (a / 2; 0)".Simplify()` | `a in (a / 2; 0)` | `False provided a in RR` |
| `"a in (-2a; -a/2)".Simplify()` | left as written | `False provided a in RR` |
| `"a in (a / 2; 2a + 1)".Simplify()` | `a > 0 and 1 + a > 0` | unchanged |
| `"a in (1; 2)".Simplify()`, `"3 in (1; 5)"`, `"x in [0; 1]"`, `"a in (b; c)"` | unchanged | unchanged |

The two `False` rows are answers where there were none: `a / 2 < a < 0` wants `a > 0` and `a < 0` at
once, and so does `-2a < a < -a / 2`. The condition is there because the ordering is a claim about
reals.

**And `RewriteRules.Common` reaches a fixed point.** It was the library's only non-terminating rule
set: a three-cycle on `-x * 1/2`, `Mulf(-1/2, x)` to `Mulf(-1, Divf(x, 2))` to `Divf(Mulf(-1, x), 2)`
and back — three trees printing as two strings. Two of the three rules turning it are exact inverses
on that shape: one reads `(-1 * x) / 2` as a numeric factor to collect, giving `-1/2 * x`, and the
other reads that back as `-(x / 2)`. The first now declines a factor of `-1`, which is the sign
rather than a number to collect.

The positive case never cycled, and the reason says why the guard is where it is: `x / 2` is a
quotient over a *leaf*, so it does not re-enter the collection rule's pattern at all. The loop
existed only because a negation is spelled as a product.

`Simplify` bounded its own iteration and never hung, so no caller saw the cycle — but a rule set is
public, a caller may apply one by itself, and `Common` did not terminate when applied that way.
`RuleSetTerminationTest.NeverSettle` is now empty and still asserted in both directions.

**Nothing else moved.** `-x * 1/2` is `-1/2 * x` and `x * 1/2` is `x / 2`, both as before; the guard
changes which rewrites are available, not which answer wins. The two halves of this entry ship
together because the first is what makes the second free: the guard alone cost three interval shapes
that were being answered by coincidence, and the collection restores them along with the rest of the
family ([#1056](https://github.com/asc-community/AngouriMath/issues/1056)).

### The determinant is computed by fraction-free elimination where it can be

`Matrix.Determinant` expanded by Laplace, which is `O(n!)`. For a fully symbolic matrix that is
optimal — the determinant genuinely has `n!` terms, and no algorithm returns it smaller in expanded
form. For a numeric one it is pure waste: the answer is a single number and `O(n^3)` work suffices.

Bareiss' fraction-free elimination now runs wherever the entries are polynomials over the rationals,
and Laplace answers everything else. What decides it is not the size but whether the entries can be
read, settled per matrix by trying.

**The ceiling this removes**, both arms built from source on one machine:

| | before | now |
|---|---|---|
| numeric 8×8 | 382 ms | under 1 ms |
| numeric 10×10 | 14 415 ms | 2 ms |
| numeric 11×11 | did not return in four minutes | 2 ms |
| numeric 12×12 | did not return | 3 ms |
| numeric 20×20 | did not return | 11 ms |
| numeric 30×30 | did not return | 22 ms |

**The printed form of a symbolic determinant changes**, because an elimination produces an expanded
polynomial where Laplace produces a nested expansion. The value is the same and the expression is no
larger in any case measured:

| | before | now |
|---|---|---|
| `"[[a, b], [c, d]]"` | `a * d + -b * c` | `a * d - b * c` |
| `"[[x, 1, 0], [1, x, 1], [0, 1, x]]"` | `x * (x ^ 2 + -1) + -x` | `x ^ 3 - 2 * x` |
| `"[[a, b, 1], [c, d, 2], [1, 2, 3]]"` | `a * (d * 3 + -4) + -b * (c * 3 + -2) + c * 2 + -d` | `3 * a * d - 4 * a - 3 * b * c + 2 * b + 2 * c - d` |
| `"[[x, 1], [1, x]]"` | `x ^ 2 + -1` | unchanged |
| `"[[1/2, 1/3], [1/4, 1/5]]"` | `1/60` | unchanged |

**No condition is introduced, and that is the point.** An ordinary Gaussian elimination leaves its
pivots as literal divisions, so its answer is undefined wherever a pivot vanishes — at points where
the determinant is perfectly well defined. That was
[#992](https://github.com/asc-community/AngouriMath/issues/992), and it is why Laplace was chosen.
Bareiss divides as well, but each division is by the *previous* pivot and is exact: the quotient is a
determinant of a minor, so it is back in the ring. Here it is exact **and checked** — the arithmetic
happens in `MultivariatePolynomial`, which has no quotients to leave behind, and a division that does
not come out returns null and sends the caller to Laplace.

**What is declined**, and answered by Laplace exactly as before: an entry that is not a polynomial
over the rationals (`sin(x)`, `1 / x`, `2 ^ x`), a matrix in more than eight indeterminates, and a
matrix mentioning `e` or `pi` — a constant is a value rather than an indeterminate, and this ring
cannot hold one.

The two algorithms were compared on 300 generated matrices where both apply, as a difference
simplified to zero rather than as trees, with **no disagreements**
([#999](https://github.com/asc-community/AngouriMath/issues/999)).
### Solving a system is bounded whichever internal path takes it

`Solve` on a system tries a triangularising path first, which bounds itself, and hands what it
declines to an elimination in radicals, which had **no budget at all**. So the same call was bounded
or unbounded depending on which internal path accepted it — cyclic-6 exceeded twenty seconds with
nothing to stop it — and that is worse to debug than either extreme.

The whole call now draws on one budget. Where it runs out, `NotSufficientlySupportedException` is
raised, naming both paths and both ways to ask for more.

| | before | now |
|---|---|---|
| `solvesys[a,b,c,d,f,g]` cyclic-6 | exceeded 20 s, no bound beyond it | declines, returning in about two minutes |
| `solvesys[a,b,c,d,e]` of `a^4-1 … e^4-1` | 1024 solutions | unchanged |
| `solvesys[a,b,c,d]` cyclic-4 | 158 solutions | unchanged |
| `x + y = 3, x - y = 1` and every system that answered | unchanged | unchanged |
| the same with `MathS.Settings.Budget` set below what the solve needs | answered anyway | raises |

**Nothing that answered stops answering.** That was the risk, and it was measured before the change
rather than argued about: the 1024-solution system is one the triangularising path declines on its
quotient-dimension cap, and it declines it **in 8 milliseconds** — so bounding the whole call leaves
the elimination essentially the whole budget. "The fast path declined" does not mean "hopeless", and
this is what keeps that true.

**The default is two ceilings, and both are measured.** A step is one candidate solution the
elimination explores, which is what compounds — each elimination turns the next level's coefficients
into nested radicals. The systems that answer explore very few: a symbolic 2×2 takes 2, cyclic-4
takes 8, and the largest that answers at all takes 341. Cyclic-5 passes 100 000 without finishing. So
the step ceiling is 10 000, with thirty-fold headroom over anything known to work.

The clock is a backstop rather than the bound, and it is deliberately loose at sixty seconds, because
a step can be arbitrarily expensive — cyclic-4 spends five seconds in eight of them. A tight clock
was tried at five seconds and makes the same system answer or decline depending on what else the
machine is doing, which is a worse failure than a slow answer.

**The bound is cooperative and is checked once per branch**, so a call can overshoot by the cost of
the branch that was running when the budget ran out. That is `BudgetLedger`'s stated design: an
algorithm that does not ask cannot be bounded, and a bound enforced from outside is a thread abort in
the middle of a rewrite. It is a bound where there was none, not a tight one.

**A budget recording now sees two outcomes per solve** rather than one, named `Gröbner` and
`SolveSystem`. What stopped each is reported separately, which is the thing
[#896](https://github.com/asc-community/AngouriMath/issues/896) said a caller could not previously
find out.

### `Collects` promises never larger, and `Common` declares sixteen growths

`RewriteRuleGrowth.Collects` used to mean the replacement is written with fewer nodes than the
pattern for every input. It now means **never more, and fewer for some** — `Rearranges` is still
the same size for every input and `Expands` still larger for every input. Every rule that was
`Collects` still is, and every count written beside one ("exactly −2", "at most −4") still holds; the
promise is weaker only where nothing relied on the strong one. The reason is the commonest
collecting shape in the library: a hole matched twice and written once beside one new node,
`a + a = 2 * a`, `a * a = a ^ 2`, `a + a * b = a * (1 + b)`, which is `1 − |a|` — exactly as large
at a leaf and smaller everywhere else. Under the strict reading it could not be declared at all,
and thirteen of `Common`'s rules sat at `Unknown` for it.

Sixteen of `Common`'s rules declare a growth now, so `RewriteRules.Common.Rules[i].Growth` changes
for them, and `Saturation.RulesUpTo(Rearranges)` — the ceiling `Transformation.CanonicalizationOverGraph`
runs at by default — admits fifteen more rules, 157 of 324 where it was 142. What that changes on
ordinary input is what those rules do: a repeated term collects over the graph where it used to be
left as written, and the difference of squares reaches the tree its expansion reaches.
`Entity.Canonicalize()` runs `Transformation.Canonicalization`, the rule pass rather than the graph,
and answers exactly as before on every input below.

| | Was | Is |
|---|---|---|
| `Transformation.CanonicalizationOverGraph(budget).ApplyOrKeep("(x + y) * (x - y)")` | `(-y + x) * (x + y)` — not the tree `x ^ 2 - y ^ 2` reaches, `-y ^ 2 + x ^ 2` | `-y ^ 2 + x ^ 2` — one tree for both |
| the same on `(x + 1) * (x - 1)` | `(1 + x) * (-1 + x)` | `-1 + x ^ 2` |
| the same on `x + x` | `x + x` — left as written | `2 * x` |
| the same on `x + x * y` | `x * y + x` — reordered, not collected | `(1 + y) * x` |
| `RewriteRules.Common.Rules[i].Growth`, for sixteen rules | `Unknown` | `Collects` for fourteen; `Rearranges` for `a-reciprocal-rational-factor-is-a-division`, `Expands` for its negated twin |
| `RewriteRules.Common.Rules` by growth — collects / rearranges / expands / unknown | 21 / 14 / 5 / 22 | 35 / 15 / 6 / 6 |
| `RewriteRules.All` by growth | 97 / 45 / 30 / 152 | 111 / 46 / 31 / 136 |
| what `RewriteRuleGrowth.Collects` promises | fewer nodes for every input | never more, and fewer for some |

### `Power` and `Factorization` declare the rest of the collecting family

The entry above named the relatives of the `1 − |a|` shape in `Power` and `Factorization` as next.
Nine rules of `Power` — `a ^ n * a`, `a / a ^ n`, `a ^ n / a`, `a ^ n * (a * rest)`,
`(c / a) ^ d * a`, `(c / a) ^ d * a ^ e`, `a / b / b`, `a / b ^ n / b`, `a / b / b ^ n` — and four of
`Factorization` — `k + k * q`, `k + k`, `k - k * q`, `k * q - k` — declare `Collects`, each on its
own count; three more of `Power` stay `Unknown` with the reason written beside the rule. The safe
ceiling admits 170 of 324 rules where it admitted 157.

On ordinary input little moves, and for a measured reason: `x ^ 2 * x` reaches `x ^ (2 + 1)` over
the graph, which is no smaller at a leaf, and no safe rule folds the exponent, so the input is kept;
and `Factorization`'s four have twins in `Common` that were declared in the entry above, so
`a + a * b` was already `(1 + b) * a`. What does move is the power of a reciprocal beside its own
denominator, whose exponents fold as the replacement is built.

| | Was | Is |
|---|---|---|
| `Transformation.CanonicalizationOverGraph(budget).ApplyOrKeep("(2 / x) ^ 3 * x")` | `(2 * 1 / x) ^ 3 * x` — left as written | `8 * x ^ (-2)` |
| the same on `(2 / x) ^ 3 * x ^ 2` | `(2 * 1 / x) ^ 3 * x ^ 2` | `8 * 1 / x` |
| the same on `x ^ 2 * x`, `x / x ^ 2`, `x ^ 2 / x`, `x / y / y`, `a + a * b`, `a + a` | `x ^ 2 * x`, `1 / x ^ 2 * x`, `1 / x * x ^ 2`, `(1 / y) ^ 2 * x`, `(1 + b) * a`, `2 * a` | the same |
| `RewriteRules.Power.Rules` by growth — collects / rearranges / expands / unknown | 13 / 6 / 1 / 13 | 22 / 6 / 1 / 4 |
| `RewriteRules.Factorization.Rules` by growth | 4 / 0 / 2 / 5 | 8 / 0 / 2 / 1 |
| `RewriteRules.All` by growth | 111 / 46 / 31 / 136 | 124 / 46 / 31 / 123 |

### The e-graph folds a rational on insertion

`EGraph.Add` folds an arithmetic operator over two rational leaves into that number's class, as
it already folded a neutral element: `1 + 1` and `2` are one e-class from the moment either is
inserted, `3 / 3` is `1`'s, `2 ^ 10` is `1024`'s. Only where the value is itself a rational —
`1 / 0` and `2 ^ (1/2)` stay as written — only for a whole exponent no larger than 4096 in
magnitude, and never for a node carrying a codomain of its own. Found by running the safe ceiling
over 3,630 generated shapes: 54 did not saturate within 20,000 steps and every one was
constant-only arithmetic, because with nothing folding a number the rearranging rules respell it
for ever. With the fold, 7 do not — a rational coefficient beside a variable, which stalls at a
bounded graph with the right extraction rather than running away — and the run takes 24 seconds
where it did not finish in 590.

**Almost nothing public moves**, because `Transformation.CanonicalizationOverGraph` runs the rule
pass on either side of the graph and that pass already folds constants: `sqrt(2) * sqrt(3)`,
`x + 3 / 3`, `2 ^ 1000` and `1 / 0` answer exactly as before. What moves is an input the graph
could only settle by seeing a folded number *while saturating*.

| | Was | Is |
|---|---|---|
| `Transformation.CanonicalizationOverGraph(budget).ApplyOrKeep("(x ^ 2) ^ 3 - x ^ 6")` | `-x ^ 6 + x ^ 6` | `0` |
| the same on `sqrt(2) * sqrt(3)`, `x + 3 / 3`, `2 - 0 + 0 * 2`, `(x + 1) * (x - 1)`, `2 ^ 1000`, `1 / 0`, `2 ^ (1/2)` | `sqrt(6)`, `1 + x`, `2`, `-1 + x ^ 2`, the 302-digit integer, `NaN`, `sqrt(2)` | the same |

### The limit of `z / |z|` carries the phase of `z`

`(i / x) / abs(i / x)` at `+oo` answered `1`, whatever the phase; it is `i`
([#1186](https://github.com/asc-community/AngouriMath/issues/1186)). The derivative of a modulus was
`sgn(f) * f'` whatever `f` took values in, and that is the derivative of `|f|` only for a
real-valued `f`: `|i / x|` is `1 / |x|`, whose derivative is `-sgn(x) / x^2`, where the formula
gave `+sgn(x) / x^2`. l'Hopital's rule read that derivative and answered `1` for every phase, and
three things behind it agreed with the wrong answer: a whole zero times a complex infinity
evaluated to `0` where `0 * oo` is `NaN`, so a candidate `(2 + i * x) * abs(2 + i * x)^(-1)`
answered `0`; a complex infinity did not count as infinite, so `i * abs(x) / x` was never read as
oo/oo; and the descent's unread `0 / abs(0)` was handed back as `NaN`, the claim that there is no
limit.

The derivative of `abs(f)` and of `sgn(f)` is answered where `f` is shown real on the real line —
the variable, real constants, sums, products, quotients and whole powers of those, their
trigonometric functions, a sign, and any modulus — and left unevaluated otherwise: a symbol is
complex until something says otherwise. A numeric factor off the real line whose modulus is exact
comes out first, `|i * x| = |x|` and `|(3 + 4i) * x| = 5 * |x|`, which is also a `Common` rule now
(`a-modulus-takes-a-numeric-factor-out` and its dividend and divisor forms, `Sound`, `Rearranges`;
the modulus is multiplicative on the whole plane, so nothing is assumed). Zero times a complex
infinity is `NaN`. A complex infinity is infinite. And an indeterminate form that reaches the
public `Limit` — a shape assembled out of the parts' limits that evaluates to `NaN` without any
part being `NaN` — is withheld rather than reported, so the limit stays a question; inside the
machinery it is kept, because it is what stops the search. Both columns measured on a build,
`10ca691f` against this change.

| | Was | Is |
|---|---|---|
| `"(i/x)/abs(i/x)".Limit("x", +oo)` | `1` | `i` |
| the same at `-oo` | `1` | `-i` |
| `"(i/x)^2/abs(i/x)^2".Limit("x", +oo)`, and at `0` | `1` | `-1` |
| `"(3 + 4i) * x / abs((3 + 4i) * x)".Limit("x", +oo)` | `1` | `3/5 + 4/5 * i` |
| `"((1+i)/x)^2/abs((1+i)/x)^2".Limit("x", +oo)` | `1` | left unevaluated — the answer is `i`, and `\|1 + i\|` is not a rational number, so the factor stays inside |
| `"limit((2 + i * x) / abs(2 + i * x), x, +oo)".Simplify()` | `0` | left unevaluated — the answer is `i` |
| `"i * abs(x) / x".Limit("x", +oo)` | left unevaluated | `i` |
| `"abs(i * x)".Differentiate("x")` | `sgn(i * x) * i provided not i * x = 0` — which is `-sgn(x)` | `sgn(x) provided not x = 0` |
| `"abs(i / x)".Differentiate("x")` | `sgn(i / x) * (-i) / x ^ 2 provided not i / x = 0` — which is `+sgn(x) / x^2` | `-(sgn(x) provided not x = 0) / abs(x) ^ 2` |
| `"abs(2 + i * x)".Differentiate("x")`, and `abs(a * x)` for a symbol `a` | `sgn(2 + i * x) * i provided not 2 + i * x = 0`, `sgn(a * x) * a provided not a * x = 0` | left unevaluated |
| `"sgn(2 + i * x)".Differentiate("x")`, and `sgn(a * x)` | `0 provided not 2 + i * x = 0`, `0 provided not a * x = 0` | left unevaluated |
| `"abs(i * x)".Simplify()`, and `abs(x / i)` | `abs(i * x)`, `abs((-i) * x)` | `abs(x)` |
| `"0 * (i * +oo)".Evaled`, and `(2 + i * +oo) * 0` | `0` | `NaN` |
| `"x / abs(x)".Limit("x", 0)`, `abs(x) / x` at `+oo`, `sin(x)` at `+oo`, `x ^ 2 / abs(x) ^ 2` at `0`, `"abs(x + 2)".Differentiate("x")` | `NaN`, `1`, `NaN`, `1`, `sgn(x + 2) provided not x + 2 = 0` | the same |
| `RewriteRules.Common.Rules.Count` | `62` | `65` |
| `RewriteRules.All` by growth — collects / rearranges / expands / unknown | 124 / 46 / 31 / 123 | 124 / 49 / 31 / 123 |
| `Saturation.SafeRules.Count` | `170` | `173` |

### The solver tries the equation as written before asking for its alternatives

Two changes inside `SolveEquation`, made for speed and measured for answers. The replacement
machinery — solve for a subtree, then solve the subtree for `x` — used to ask
`Entity.Alternate(4)` for every spelling of the equation before trying any of them, at every
depth of its own recursion; that is a whole level-4 simplification, the same search `Simplify`
runs, and in the ordinary case the first spelling it produced was the equation itself. It now
tries the equation as it stands first and asks for the alternatives only if that does not settle
it; a spelling that comes back as a condition rather than a finite set is kept as a fallback and
the next spelling is tried, which is what keeps `x^4 * x^y - 2` answered with a root. And the
two polynomial-factoring attempts every equation is offered — a rational root to split off, an
irreducible factorisation — read the tree before they expand it: a sine is not a monomial, and
`cos(x) + sin(x) - r` with `r` a page of radicals was being expanded twice at each of eighteen
visits to be told so.

Every solve in the suite answers as before but one, whose condition is spelled from the equation
as written rather than from its resimplified form — the same set with its terms in the other
order. Measured by the gate on one machine, bytes and time per call:

| | Was | Is |
|---|---|---|
| `"(x - b) / (x + a) + c / (x + a)".SolveEquation("x")` | `{ -(-b + c) provided not a + -(-b + c) = 0 }` | `{ -(-b + c) provided not -(-b + c) + a = 0 }` |
| `SolveHard` — the benchmark's quartic in `sin(cos(x) + sin(x) + c)` | 859,268,416 B, 700 ms | 11,818,936 B, 109 ms |
| `SolveMediumHard` | 94,415,256 B, 66.8 ms | 1,435,934 B, 13.9 ms |
| `SolveMedium`, `SolveEasyMedium` | 662,923 B, 456 µs; 97,335 B, 29.7 µs | 452,901 B, 387 µs; 65,232 B, 19.2 µs |
| `SimplifyHard`, `SimplifyEasy` — the same pre-check, reached through `Simplify`'s factoring candidates | 733,188,536 B, 371 ms; 116,291 B, 71.2 µs | 680,457,400 B, 339 ms; 110,051 B, 66.4 µs |

### A series in a power over a factorial is summed in closed form

`sum(x^k / k!, k, 0, +oo)` was left as written; it is `e^x`. The whole family
`sum(p(k) * c^(k + s) / (k + a)!, k, m, +oo)` — a polynomial in the index, a power with the index in
its exponent, a factorial of the index plus a whole number — is `e^c` times a polynomial in `c`:
shift the index so the factorial is `j!`, and `j^n * c^j / j!` sums to `e^c` times the Touchard
polynomial `T_n(c)`, the Stirling numbers of the second kind carrying `j^n` into falling
factorials. A lower bound above the shifted zero subtracts finitely many exact terms. The series
converges for every `c`, so nothing is attached. The first of the orientation-week questions of
[#1212](https://github.com/asc-community/AngouriMath/issues/1212); question two's series is the
second row. Both columns measured on a build, `e2476ac5` against this change.

| | Was | Is |
|---|---|---|
| `"sum(3^(k+2) * (k^2 + k + 1) / (k + 3)!, k, 0, +oo)".ToEntity().Simplify()` | left as written | `4/3 * e ^ 3 - 41/6` |
| `"sum(1 / (2 * n!), n, 1, +oo)".ToEntity().Simplify()` | left as written | `(e - 1) / 2` |
| `"sum(x^k / k!, k, 0, +oo)".ToEntity().Simplify()`, and `sum(k * x^k / k!, k, 0, +oo)` | left as written | `e ^ x`, `e ^ x * x` |
| `"sum(1 / k, k, 1, +oo)"`, `sum(k!, k, 0, +oo)`, `sum(x^k / k!, k, n, +oo)`, `sum(3^(2k) / k!, k, 0, +oo)` | left as written | the same — not of the shape, or a symbolic lower bound, or a slope other than one in the exponent |

### A binomial sum with a power or a trigonometric weight is closed

`sum(N! / (k! (N - k)!) x^k, k, 0, N)` was left as written for a symbolic `N`; it is `(1 + x)^N`,
the binomial theorem read backwards, and with `x^k y^(N-k)` it is `(x + y)^N`. With `cos(k t)`
the sum is the real part of `(1 + e^(it))^N`, and `1 + e^(it) = 2 cos(t/2) e^(it/2)` exactly, so it is
`(2 cos(t/2))^N cos(N t/2)`; with `sin(k t)` the same with the sine. Both are algebra on the two
conjugate sums and hold for every complex `t`. The top factorial is optional, since
`sum(x^k / (k! (N - k)!))` is `(1 + x)^N / N!`, and a concrete one has folded into a number before
the factors are read. A symbolic `N` carries `N >= 0` with the empty-range branch, for the reason
`sum(k, k, 1, n)` does. Question II.3 of
[#1212](https://github.com/asc-community/AngouriMath/issues/1212). Both columns measured on a
build, `e2476ac5` against this change.

| | Was | Is |
|---|---|---|
| `"sum(N! / (k! * (N - k)!) * cos(k * pi / 3), k, 0, N)".ToEntity().Simplify()` | left as written | `piecewise(2 ^ N * cos(pi / 6) ^ N * cos(N * pi / 6) provided N >= 0, 0)` — `3^(N/2) cos(N pi / 6)` |
| `"sum(N! / (k! * (N - k)!) * x^k, k, 0, N)".ToEntity().Simplify()`, and with `x^k * y^(N - k)` | left as written | `piecewise((1 + x) ^ N provided N >= 0, 0)`, `piecewise((x + y) ^ N provided N >= 0, 0)` |
| `"sum(300! / (k! * (300 - k)!), k, 0, 300)".ToEntity().Simplify()`, and every concrete range past the hundred-term expansion | left as written | `2 ^ 300`, the 91-digit integer |
| `"sum(N! / (k! * (N - k)!) * k, k, 0, N)"`, and a trigonometric weight beside a power, a lower bound other than 0, a coefficient whose `N` is not the upper bound | left as written | the same |

### A logarithm of a perfect power takes the exponent out

`ln(4/3) + ln(16/9) / 2` was left as written; it is `2 ln(4/3)`. The rule that reads `log(a, b^n)` as
`n log(a, b)` could not see a rational literal that is a power without saying so, and `16/9` is
`(4/3)^2`. A logarithm of a positive rational literal that is a perfect power is now offered as the
exponent times the logarithm of the root — the largest exponent that fits, read exactly on numerators
and denominators within 64 bits. A positive root only: `-8` is `(-2)^3`, but `ln(-8)` is
`ln 8 + i pi` where `3 ln(-2)` is `3 ln 2 + 3 i pi`, so a negative root is refused. The form is longer
on its own, so alone a literal stays as written; it is what lets logarithms of related literals
collect. Question I.5 of [#1212](https://github.com/asc-community/AngouriMath/issues/1212). Both
columns measured on a build, `60545afa` against this change.

| | Was | Is |
|---|---|---|
| `"ln(4/3) + ln(16/9) / 2".ToEntity().Simplify()` | `ln(4/3) + ln(16/9) / 2` | `2 * ln(4/3)` |
| `"integral((2x^2+x+1)/(x^3+x^2+x+1), x, 3/4, 4/3)".ToEntity().Simplify()` | `ln(4/3) + ln(16/9) / 2` | `ln(16/9)` |
| `"ln(16/9)"`, `ln(64)`, `ln(8) - 3 * ln(2)`, `log(3, 81) / 4` | `ln(16/9)`, `ln(64)`, `0`, `1` | the same |
| `RewriteRules.Power.Rules.Count`, and `RewriteRules.All` by growth | `31`; 124 / 49 / 31 / 123 | `32`; 124 / 49 / 32 / 123 |

### A series whose terms do not vanish is answered `+oo` rather than left as written

`sum(2^k, k, 0, +oo)` and `sum(k, k, 1, +oo)` were left as written. They have no finite value, and
saying nothing was the only thing missing: the **nth-term test** settles them. Terms that do not
tend to zero mean the series diverges, and the *sign* of the limit says which infinity — where the
terms tend to a positive `L` they are eventually all above `L / 2`, so the partial sums pass every
bound. A negative limit gives `-oo` the same way, and the finitely many terms before that point are
finite and cannot change it.

**A limit of zero is declined, and that is the point of the test rather than a gap in it.** It is
exactly the case the nth-term test says nothing about: `sum(1/k, k, 1, +oo)` diverges and
`sum(1/k^2, k, 1, +oo)` converges, and the terms of both tend to 0. A limit that does not exist is
declined too — `sum((-1)^k, k, 0, +oo)` has no value rather than an infinite one, and telling "the
limit does not exist" apart from "the limit was not computed" is not something to infer from a
failed computation. **And a summand that can fail to exist at some index is declined**, because one
undefined term makes the sum undefined rather than infinite: `sum(k / (k - 5), k, 0, +oo)` has terms
tending to 1, and the term at `k = 5` does not exist. Rather than hunt for poles, the index is
allowed only where none can arise, so a division by anything containing it is refused outright.

This runs last, after every closed form, so a series that converges is summed rather than tested.
Asked for on the review of [#1218](https://github.com/asc-community/AngouriMath/pull/1218), where
`integral(floor(x)^floor(x), x, 1, +oo)` splits into `sum(n^n, n, 1, +oo)` and was left as written
for want of it. Part of [#1212](https://github.com/asc-community/AngouriMath/issues/1212). Both
columns measured on a build, `102911be` against this change.

| | Was | Is |
|---|---|---|
| `"sum(2^k, k, 0, +oo)".ToEntity().Simplify()`, `sum(k, k, 1, +oo)`, `sum(k^2 + 1, k, 0, +oo)`, `sum(n^n, n, 1, +oo)` | left as written | `+oo` |
| `"sum(-k, k, 1, +oo)".ToEntity().Simplify()`, `sum(-2^k, k, 0, +oo)` | left as written | `-oo` |
| `"integral(floor(x)^floor(x), x, 1, +oo)".ToEntity().Simplify()` | left as written | `+oo` |
| `"sum(1/k, k, 1, +oo)"`, `sum(1/k^2, k, 1, +oo)`, `sum((-1)^k, k, 0, +oo)`, `sum(k / (k - 5), k, 0, +oo)` | left as written | the same — a vanishing limit, a limit that does not exist, and a pole in the index are each declined |
| `"sum(2^(-k), k, 0, +oo)"`, `sum(x^k / k!, k, 0, +oo)`, `sum(2^k, k, 0, 5)` | `2`, `e^x`, `63` | the same — what converges is summed, and a finite range is a finite sum |

### A geometric series is summed in closed form

`sum(x^k, k, 0, +oo)` was left as written, and so was every other sum of a power with the index in
the exponent: `sum(2^(-k), k, 0, +oo)`, `sum(x^k, k, 0, n)`, and `sum(2^k, k, 0, 200)`, whose range is
past the hundred terms that are expanded one by one. A summand that is `C * b^(m k + s)` — a base
free of the index, an exponent that is the index times a whole number plus something free of it,
and every other factor free of the index — is now summed as a geometric series with the ratio
`b^m`. Between two bounds the sum is `C b^s (r^a - r^(b + 1)) / (1 - r)` where the range is not
empty and the ratio is not 1, the number of terms times the constant where the ratio is 1, and `0`
for an empty range; a numeric ratio picks its branch and a symbolic one is offered all three as a
piecewise, the way a polynomial summand is offered the empty range. To `+oo` the series converges
exactly when `|r| < 1`: a numeric ratio outside that is left as written — infinite or without a
value, and which is not this reader's to say — and a symbolic ratio carries `provided abs(r) < 1`.
A polynomial factor in the index, `k x^k`, is not this family and stays as written. The sum the
step integral of `2^(-floor(x))` leaves, from question I.2 of
[#1212](https://github.com/asc-community/AngouriMath/issues/1212). Both columns measured on a
build, `27ed5b53` against this change.

| | Was | Is |
|---|---|---|
| `"sum(2^(-k), k, 0, +oo)".ToEntity().Simplify()`, `sum(1 / 2^k, k, 1, +oo)`, `sum((-1/2)^k, k, 0, +oo)` | left as written | `2`, `1`, `2/3` |
| `"sum(x^k, k, 0, +oo)".ToEntity().Simplify()`, and from 1 | left as written | `1 / (1 - x) provided abs(x) < 1`, `x / (1 - x) provided abs(x) < 1` |
| `"sum(x^k, k, 0, n)".ToEntity().Simplify()` | left as written | `piecewise((n + 1) provided (x = 1 and n >= -1), ((1 - x ^ (n + 1)) / (1 - x)) provided (n >= -1), 0 provided True)` |
| `"sum(2^k, k, a, n)".ToEntity().Simplify()`, and `sum(2^k, k, 0, 200)` | left as written | `piecewise((2 ^ (n + 1) - 2 ^ a) provided (n >= a - 1), 0 provided True)`, `2^201 - 1` as a number |
| `"integral(2^(-floor(x)), x, 0, +oo)".ToEntity().Simplify()` | left as written | `2` |
| `"sum(2^k, k, 0, +oo)"`, `sum((-1)^k, k, 0, +oo)`, `sum(k * (1/2)^k, k, 0, +oo)`, `sum(2^k, k, 0, 5)` | left as written; `63` | the same |

### `a divides b` is a statement, and `divides` a keyword

Divisibility had no spelling: `b mod a = 0` states it for integers, but nothing parsed as, printed
as, or was a divisibility statement. `Dividesf` is one, beside membership: `a divides b` in the
grammar at the level of `in`, so its operands are arithmetic and its result a statement; the same
words back out; `a \mid b` in LaTeX; `sympy.Eq(sympy.Mod(b, a), 0)` in the SymPy export;
`Entity.Divides` and `MathS.NumberTheory.Divides` in code. Decided for integers, `0 divides b`
exactly when `b` is 0; `NaN` over a number that is not an integer, the way an inequality is over a
non-real number; carried when symbolic. The cost is the word: `divides` is a keyword now, so a
variable of that name no longer parses, and `2 divides` — which was the product `2 * divides` — is
a parse error rather than a different expression. Agreed on
[#1212](https://github.com/asc-community/AngouriMath/issues/1212) after `a | b` was found to be
`a or b` already. Both columns measured on a build, `27ed5b53` against this change.

| | Was | Is |
|---|---|---|
| `"divides".ToEntity()` | the variable `divides` | a parse error |
| `"2 divides".ToEntity()`, `"divides + 1"` | `2 * divides`, `divides + 1` | a parse error, the same |
| `"3 divides 12".ToEntity()`, and `.Evaled` | `3 * divides ^ 12` | `3 divides 12`, `True` |
| `"a divides b".ToEntity()` | `a * divides * b` | `a divides b`, a `Dividesf` |
| `"5 divides 12".ToEntity().Evaled`, `0 divides 0`, `0 divides 5`, `3 divides 5/2` | products with a variable in them | `False`, `True`, `False`, `NaN` |
### `card(S)` is the number of elements of a set

A set had no count: a finite set knows its `Count` in code, and nothing in the language asked for
it. `Cardf` is a function of a set, spelt canonically as the prefix `#` — `#S`, `#{ 1, 2, 3 }`,
`#(A \/ B)` — with `card( )` accepted on input; `#` binds as tightly as a function call, so `#S + 1`
is `(#S) + 1` and a set expression takes parentheses. Printed `#S`, `\#S` in LaTeX (not `|S|`, which
reads back as a modulus), `len(S)` in the SymPy export, `MathS.Sets.Card` in code. Counted where the
count is known: a finite set whose elements are numbers, since `{x, 1}` has two elements unless `x`
is 1 and a set with a symbol in it is not counted until its elements are distinct; and an interval
with numeric ends that is one point or none. Left as written for a proper interval and for `RR`,
`ZZ` and the rest — an infinite set has a cardinality this library has no number for, and `+oo`
would say `[0; 1]` and `ZZ` have the same size — and for a set builder, which is not enumerated. The
cost is that `#` and `card(` are grammar now, so `card(x)` no longer reads as the product of a
variable and a parenthesis and `#` is no longer free; a bare `card` is still a variable. Agreed on
[#1212](https://github.com/asc-community/AngouriMath/issues/1212), `#` chosen as canonical on the
review of [#1221](https://github.com/asc-community/AngouriMath/pull/1221). Both columns measured on a
build, `27ed5b53` against this change.

| | Was | Is |
|---|---|---|
| `"card({ 1, 2, 3 })".ToEntity()`, and `.Evaled` | `card * { 1, 2, 3 }`, `{ card, card * 2, card * 3 }` | `#{ 1, 2, 3 }`, `3` |
| `"card(S)".ToEntity()`, `card(x) + 1`, `#S` | `card * S`, `card * x + 1`, a parse error (`#` was free) | `#S`, `#x + 1`, `#S` — a `Cardf` |
| `"card({ })".ToEntity().Evaled`, `card({ 1, 1, 2 })`, `card({ 1, 2 } \/ { 2, 3 })`, `card([1; 1])`, `card([2; 1])` | products with a variable in them | `0`, `2`, `3`, `1`, `0` |
| `"card({ x, 1 })".ToEntity().Simplify()`, `card(RR)`, `card([0; 1])`, `card({ x : x > 0 })` | products with a variable in them | `#{ x, 1 }`, `#RR`, `#[0; 1]`, `#{ x : x > 0 }` — left as written |
| `"card".ToEntity()`, `card * 2` | the variable `card`, `card * 2` | the same |

### An integral across a jump is split at the jumps, never taken through an antiderivative

`F(b) - F(a)` is the integral only where `F` is continuous on `[a, b]`, and the definite integral
evaluated an antiderivative at its bounds whatever the integrand did in between. Three shapes jump:
a piecewise whose conditions mention the variable, whose antiderivative the generic argument
expansion built case by case and then handed back with the integration variable still inside it —
the tent map `piecewise(2x provided x <= 1/2, 2 - 2x)` over `[0, 1]` answered `1` (the first case's
antiderivative at both ends), and `piecewise(x provided x < 1, x^2)` over `[0, 2]` answered
`piecewise(2 provided x < 1, 8/3)`; a `provided` on the variable, distributed the same way; and
`floor(x)` or `ceil(x)`, taken for constants across their jumps — `x - floor(x)` from 0 to 3 answered
`0`, `(x - floor(x))^2` from 0 to 4 answered `0` where it is `4/3`, and from 0 to a symbolic `n`
answered `(n - floor(n))^3 / 3`, wrong for every whole `n` but 0.

An integrand that can jump never goes through an antiderivative between two bounds now. A
piecewise or a `provided` with finitely many breakpoints — conditions comparing the variable with
numbers — has the range cut at the ones inside it, and on each piece is the case that holds at the
piece's midpoint, integrated through the antiderivative and added; a `provided` whose condition
fails on a piece makes the integral undefined and it is left as written. A floor or ceiling has
infinitely many, evenly spaced: between whole bounds the integral is a sum over unit intervals, on
each of which the floor is `n`, the ceiling `n + 1` and `x` is `n + t`, and the summation's closed
forms answer the sum; a numeric bound that is not whole contributes the piece up to the nearest
whole number. A condition against a symbol or a symbolic bound is left as written, since the pieces
depend on where the jumps fall. Offered only where every piece resolves. Question I.2 of
[#1212](https://github.com/asc-community/AngouriMath/issues/1212), generalised on the review of
[#1215](https://github.com/asc-community/AngouriMath/pull/1215). Both columns measured on a build,
`4fd6dda5` and `60545afa` against this change.

| | Was | Is |
|---|---|---|
| `"integral(piecewise(2x provided x <= 1/2, 2 - 2x), x, 0, 1)".ToEntity().Simplify()` | `1` | `1/2` |
| `"integral(piecewise(x provided x < 1, x^2), x, 0, 2)".ToEntity().Simplify()` | `piecewise(2 provided (x < 1), (8/3) provided True)` | `17/6` |
| `"integral(piecewise(1 provided x < 0, 2 provided x < 1, 3), x, -1, 2)".ToEntity().Simplify()` | `piecewise(3 provided (x < 0), 6 provided (x < 1), 9 provided True)` | `6` |
| `"integral(piecewise(x provided x < a, 0), x, 0, 2)"`, and `integral(piecewise(2x provided x <= 1/2, 2 - 2x), x, 0, t)` | `piecewise(2 provided (x < a), 0 provided True)`, `piecewise((t ^ 2) provided (x <= 1/2), (2 * t - t ^ 2) provided True)` | left as written |
| `"integral(x provided x > 0, x, 0, 1)"`, and the same from -1 to 1 | `1/2 provided x > 0`, `0 provided x > 0` | `1/2`; left as written |
| `"integral(sgn(x), x, -1, 2)"`, `integral(abs(x - 1), x, 0, 3)` | `1`, `5/2` | the same — the antiderivatives of a sign and a modulus are continuous |
| `"integral(x - floor(x), x, 0, 3)".ToEntity().Simplify()` | `0` | `3/2` |
| `"integral((x - floor(x))^2, x, 0, 4)".ToEntity().Simplify()` | `0` | `4/3` |
| `"integral((x - floor(x))^2, x, 0, n)".ToEntity().Simplify()` | `(n - floor(n)) ^ 3 / 3` | left as written; with `n = 6` substituted, `2` |
| `"integral((x - floor(x)) / floor(x)!, x, 1, +oo)".ToEntity().Simplify()` | left as written | `(e - 1) / 2` |
| `"integral(floor(x), x, 0, 5)"`, `integral(floor(x) * x, x, 0, 3)`, `integral(floor(x) / (floor(x) + 1)!, x, 0, +oo)` | left as written | `10`, `13/2`, `1` |
| `"integral(floor(x), x, 1/2, 2)"`, `integral(x - floor(x), x, 0, 5/2)`, `integral(ceil(x) - x, x, 1/2, 2)` | left as written | `1`, `9/8`, `5/8` |
| `"integral(floor(x^2), x, 0, 2)"` | left as written | the same — a floor of something other than the variable is not a step this reads. `integral(2^(-floor(x)), x, 0, +oo)` was left as written here for want of a geometric series, and is `2` as of the section above |

### Matrix operations called from more than one thread answer correctly

`MathS.Multithreading` documents concurrent use as supported, and three caches did not honour it.
Each was a table grown under a lock and read outside one, so a reader could see a new count against
an old array and come back with **a different element** — a well-formed number, silently wrong, with
nothing thrown. The factorial cache
([#1227](https://github.com/asc-community/AngouriMath/pull/1227)) and the prime cache
([#1229](https://github.com/asc-community/AngouriMath/pull/1229)) are AngouriMath's own. The third
is not: GenericTensor 1.0.4 hands **every caller the same scratch matrix** for a given size and then
writes into it, reported as [GenericTensor#40](https://github.com/asc-community/GenericTensor/issues/40)
and guarded here in [#1230](https://github.com/asc-community/AngouriMath/pull/1230).

**A single-threaded caller sees no change at all**, which is why there is no was/is table: the same
input produced these values before and produces them now. What changes is that a concurrent caller
now gets them too. Measured on `c2c8eb83`, forty 4x4 matrices with non-polynomial entries, each
computed once sequentially and then rebuilt and recomputed under `Parallel.For`:

| | disagreements, before |
|---|---|
| `Determinant` | 38 of 40 |
| `Inverse` | 18 of 40 |
| `Adjugate` | 11 of 40 |
| `m1 + m2`, `m1 - m2`, `PointwiseMultiplication` | threw, on a corrupted `Dictionary` |

**The one thing that is visible to a working program is throughput.** `Determinant`, `Inverse` and
`Adjugate` now serialise across threads where the polynomial elimination declines the matrix and the
Laplace fallback is taken — a matrix of polynomials never reaches it. The elementwise operators do
**not** serialise: the set of compiled loops this library can ask for is fixed at three, so they are
filled once and read without a lock thereafter.

The guard is a stand-in and says so in its own doc comment. It should be deleted when a GenericTensor
release carrying [GenericTensor#41](https://github.com/asc-community/GenericTensor/pull/41) exists.

## 2.4.0 — since 2.3.0

Released 2026-08-28. These entries sat under “Unreleased” while 2.4.0 was tagged and
published, so a reader on that version could not tell from this file what they had. They
are under their own heading now, split at the tag rather than from memory: an entry is
here if it was in this file at `v2.4.0` and above if it was added after.

### At a glance

| Silent? | What | Was | Is |
|---|---|---|---|
| **Silent** | `"limit(t * b, t, 0)".ToEntity().FreeVariables`, and every limit | `{ t, b }` — the variable it approaches along counted as free | `{ b }` |
| **Silent** | `MathS.Polynomials.Factor("4 * x2 - 4 * y2", "x")`, and every multivariate polynomial whose content is a bare constant | `(x + y) * (x - y)` — **not equal to what it factored** | `4 * (x + y) * (x - y)` |
| | `"2 * x3 - 2".ToEntity().Factorize()`, and every polynomial whose content the rules take out | `2 * (x ^ 3 - 1)` — the remainder left whole | `2 * (x - 1) * (x ^ 2 + x + 1)` |
| | `"3^(x+1) - 2^(x-1)".ToEntity().SolveEquation("x")`, and every equation between two powers of numeric bases | `{ ln(0.04674569822628630438865471319331845734268426895141601562 ^ (1 / ln(2))) }` — a `double` promoted to a decimal | `{ -(ln(3) + ln(2)) / (ln(3) + -ln(2)) }` |
| **Silent** | `MathS.Abs("x").WithCodomain(Domain.Any).Stringize()`, and every node widened to `Any` from a narrower default | `abs(x)` — reads back as `Real`, losing the widening | `domain(abs(x), Any)` |
| | `new Entity[0].SumAll()`, and `Sumf.Sum` on an empty list | `AngouriBugException: At least 1 child required` | `0` |
| | `new Entity[0].MultiplyAll()`, and `Mulf.Multiply` on an empty list | `AngouriBugException` | `1` |
| | `MathS.Vector()` and `new Entity[0].ToVector()` | `IndexOutOfRangeException` — outside the documented hierarchy | `InvalidMatrixOperationException` |
| | `"x3 - 1".ToEntity().Factorize()`, and every polynomial no rewrite rule has a rule for | `x ^ 3 - 1` — handed back whole | `(x - 1) * (x ^ 2 + x + 1)` |
| | `Entity.DomainConditionIn(Domain)` | did not exist | the domain of definition for a **stated** reading, through the whole tree |
| | `MathS.Polynomials.Factor("(y * x3 + 1) * (x4 - y3)", "x")`, and bivariate polynomials whose leading coefficient in the main variable is a polynomial | `null` — a refusal | `(x ^ 4 - y ^ 3) * (x ^ 3 * y + 1)` |
| | `MathS.Polynomials.Factor("x7 - y7", "x")`, and bivariate polynomials whose substituted image over-factors | `null` — a refusal | `(x - y) * (x ^ 6 + x ^ 5 * y + … + y ^ 6)` |
| | `MathS.Polynomials.Factor("x2 + y2 + z2 + w2 + 1", "x")`, and polynomials in enough variables generally | `null` — a refusal | the polynomial itself, meaning it does not factor |
| **Silent** | `"x! = 0".ToEntity().Simplify()` | `False`, including at `x = -1` where `x!` has a pole and the statement is `NaN` | `False provided x in RR and (x >= 0 or not x in ZZ)` |
| **Silent** | `"x! / x!".ToEntity().Simplify()`, and the same for any factorial over itself | `1`, including at `x = -1` where the quotient is `NaN` | `1 provided not x! = 0` |
| **Silent** | `"(y < x) or (x = y)".ToEntity().Simplify()`, and three more disjunctions of a comparison with an equality written the other way round | `x <= y` — False at `x = 3, y = 2` where the input is True | `x >= y` |
| **Silent** | `"x6 + x y + 1 = 0".ToEntity().Solve("x")`, and every equation no solver settles | `{  }` — there are no roots | `{ x : 1 + x ^ 6 + x * y = 0 }` — these are the roots, whichever they are |
| **Silent** | `"(x - 1) * (x6 + x y + 1) = 0".ToEntity().Solve("x")` | `{ 1 }` | `{ 1 } \/ { x : 1 + x ^ 6 + x * y = 0 }` |
| **Silent** | `"x6 + x y + 1 = 0 and x - 1 = 0".ToEntity().Solve("x")` | `{  }` | `{ x : x ^ 6 + x * y + 1 = 0 and x - 1 = 0 }` |
| **Silent** | `"sum(k, k, 1, n)".ToEntity().FreeVariables`, and `product` | `{ k, n }` — the bound index counted as free | `{ n }` |
| **Silent** | `"integral(t * b, t, 0, 1)".ToEntity().FreeVariables`, and every integral with limits | `{ b, t }` | `{ b }` |
| | `"x ^ 3 - x > 0".ToEntity().Solve("x")`, and every polynomial inequality of degree three or more | `NotSufficientlySupportedException: Only linear and quadratic polynomial inequalities are supported` | `(-1; 0) \/ (1; +oo)` — the solution set |
| **Silent** | `"a implies (b implies c)".ToEntity().Stringize()` | `a implies b implies c`, which reads back as `(a implies b) implies c` | `a implies (b implies c)` |
| | `"(a implies b) implies c".ToEntity().Stringize()` | `(a implies b) implies c` | `a implies b implies c` |
| **Silent** | `@"A \ (B \ C)".ToEntity().Stringize()`, and `Latexize` | `A \ B \ C` / `A \setminus B \setminus C` | `A \ (B \ C)` / `A \setminus \left(B \setminus C\right)` |
| **Silent** | `@"A \ (B \/ C)".ToEntity().Stringize()`, and `Latexize` | `A \ B \/ C` / `A \setminus B \cup C` | `A \ (B \/ C)` / `A \setminus \left(B \cup C\right)` |
| **Silent** | `@"A \/ (B \ C)".ToEntity().Stringize()`, and `Latexize` | `A \/ B \ C` / `A \cup B \setminus C` | `A \/ (B \ C)` / `A \cup \left(B \setminus C\right)` |
| **Silent** | `"(x provided p) provided q".ToEntity().Stringize()`, and `Latexize` | `x provided p provided q`, which reads back as `x provided (p provided q)` | `(x provided p) provided q` |
| **Silent** | `"a in (b in c)".ToEntity().Stringize()`, and `Latexize` | `a in b in c` / `a \in b \in c` | `a in (b in c)` / `a \in \left(b \in c\right)` |
| **Silent** | `"x * (y mod z)".ToEntity().Stringize()`, and `Latexize` | `x * y mod z` / `x y \bmod z` | `x * (y mod z)` / `x \left(y \bmod z\right)` |
| **Silent** | `"-1 * (y mod z)".ToEntity().Stringize()` | `-y mod z` | `-(y mod z)` |
| | any expression mixing a number with a `Complex` argument, `Compile`d in a NativeAOT app — `"x + 1".Compile<Complex, Complex>("x")` | `UncompilableNodeException: ... The binary operator Add is not defined for the types 'System.Numerics.Complex' and 'System.Numerics.Complex'` | the compiled function, answering as it does under the JIT |
| | `Compile` to a nullable integral return type in a NativeAOT app | `AngouriBugException: IsNaN method expected for type System.Double`, which took the process down | the compiled function |
| **Silent** | an app publishing with `PublishTrimmed` or NativeAOT | `AngouriMath.dll` was copied in whole, being unmarked | it is trimmed with the rest, since the assembly now declares `IsTrimmable` |
| **Silent** | `"domain(x, ZZ)".ToEntity().Stringize()`, and `ToString`, and `EntityJsonConverter` | `x`, which reads back with `Codomain = Any` | `domain(x, ZZ)`, which reads back narrowed |
| **Silent** | `"domain(sqrt(-1), RR)".ToEntity().Stringize()` | `sqrt(-1)`, which evaluates to `i` when read back | `domain(sqrt(-1), RR)`, which evaluates to `NaN` |
| **Silent** | `"domain(x, ZZ)".ToEntity().Latexize()` | `x` | `{\left(x\right)}_{\mathbb{Z}}` |
| **Silent** | `"x - domain(x, ZZ)".ToEntity().Simplify()`, and any sum mixing a node with a narrowed codomain and the same node without | `0` — the two were collected as one monomial | `x - domain(x, ZZ)`, left alone |
| | every node type's own `Stringize()` and `Latexize()` overrides — `Entity.Sumf.Stringize()` and 129 more | declared on each node | declared once on `Entity`; still callable on every node, and an assembly compiled against 2.3.0 keeps working without a rebuild |
| | `"x + 1 // done".ToEntity()`, and any input whose last line ends in a `//` comment | `UnhandledParseException: extraneous input '/'` | `x + 1` — the comment is skipped, as the block form already was |
| | `MathS.Polynomials.Factor("x * y + y", "x")`, and any polynomial whose coefficients in the named variable share a common divisor | `null` — a refusal | `y * (x + 1)` |
| | `MathS.Polynomials.SquareFreePart("(x - y) ^ 2 * (x + y)", "x")`, and any polynomial in more than one variable | `null` — a refusal | `x ^ 2 - y ^ 2` |
| | `MathS.Polynomials.Factor("x ^ 2 - y ^ 2", "x")`, and polynomials in two variables of small enough bidegree | `null` — a refusal | `(x + y) * (x - y)` |
| | a `switch` over `RewriteRuleGrowth` with no default arm | compiled | does not compile — there is a fourth value, `Unknown` |
| **Silent** | `RewriteRules.RationalizeDenominator.Rules` | `[]` — the registry could not read the set | its two rules, addressable and named |
| **Silent** | `RewriteRules.Power.ApplyOnce("ln(1 / x)")`, and every `log(_, 1/_)` and `log(1/_, _)` whose argument is not decidably a positive real | `-ln(x)`, which is wrong on the negative reals | `ln(1 / x)`, left alone |
| **Silent** | `RewriteRules.Boolean.ApplyOnce("a and b or a")`, and two more orientations of absorption | left alone — the arm for that orientation was never written | `a` |

### An equation nothing settled is no longer answered with the empty set

`Solve` and `SolveEquation` returned an empty `FiniteSet` for two different things: an equation
shown to have no roots, and an equation every solver in the chain declined. The empty set is a
positive claim — *no x satisfies this* — so the second of those was a wrong answer rather than a
graceful failure ([#1036](https://github.com/asc-community/AngouriMath/issues/1036),
[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 4).

**Was** — indistinguishable, and false for the second column:

```
"x6 + x y + 1 = 0".ToEntity().Solve("x")            {  }        six roots for every y
"sin(x) + x + y = 0".ToEntity().Solve("x")          {  }
"e^x + x + y = 0".ToEntity().Solve("x")             {  }
"x6 + x y + 1".ToEntity().SolveEquation("x")        {  }
"e^x = 0".ToEntity().Solve("x")                     {  }        genuinely none
"abs(x) = -1".ToEntity().Solve("x")                 {  }        genuinely none
```

**Is** — the equation itself, as the set of the x that satisfy it. It names the same set and
asserts of it only what was established:

```
"x6 + x y + 1 = 0".ToEntity().Solve("x")            { x : 1 + x ^ 6 + x * y = 0 }
"sin(x) + x + y = 0".ToEntity().Solve("x")          { x : sin(x) + x = -y }
"e^x + x + y = 0".ToEntity().Solve("x")             { x : e ^ x + x = -y }
"x6 + x y + 1".ToEntity().SolveEquation("x")        { x : 1 + x ^ 6 + x * y = 0 }
"e^x = 0".ToEntity().Solve("x")                     {  }        unchanged
"abs(x) = -1".ToEntity().Solve("x")                 {  }        unchanged
```

Turning Newton's method off leaves nothing numerical either, and those answers move the same way.
A search over finitely many starting points inside a bounded region finding nothing is a fact about
the search:

```
using var _ = MathS.Settings.AllowNewton.Set(false);

"x5 + 3x + 1 = 0".ToEntity().Solve("x")             was {  }   is { x : x ^ 5 + 3 * x = -1 }
"sin(x) * x - 3 = 0".ToEntity().Solve("x")          was {  }   is { x : sin(x) * x = 3 }

"x + sqrt(x^0.1 + a) + c".ToEntity().SolveEquation("x")
    was {  }   is { x : sqrt(a + x ^ (1/10)) + c + x = 0 }
"(x + 6)^(1/6) + x + x3 + a".ToEntity().SolveEquation("x")
    was {  }   is { x : (6 + x) ^ (1/6) + x + x ^ 3 = -a }
"2 ^ (x sin(x)) + 4 ^ (x sin(x)) + c".ToEntity().SolveEquation("x")
    was {  }
    is  { x : sin(x) * x = ln(((-1 - sqrt(1 - 4 * c)) / 2) ^ (1 / ln(2)))
              or sin(x) * x = ln(((-1 + sqrt(1 - 4 * c)) / 2) ^ (1 / ln(2))) }
```

The last of those is the shape of it: the exponential solver did settle the equation in
`2 ^ (x sin(x))`, and it was the inversion of `x * sin(x)` that had nothing to say. What comes back
now is the half that was solved, with the half that was not left standing as a condition.

An answer that is partly settled keeps the part that is, so a product one of whose factors was
solved comes back as a union rather than as the factor's roots alone:

```
"(x - 1) * (x6 + x y + 1) = 0".ToEntity().Solve("x")
    was  { 1 }
    is   { 1 } \/ { x : 1 + x ^ 6 + x * y = 0 }

"x6 + x y + 1 = 0 or x - 1 = 0".ToEntity().Solve("x")
    was  { 1 }
    is   { 1 } \/ { x : 1 + x ^ 6 + x * y = 0 }

"x6 + x y + 1 = 0 implies x - 1 = 0".ToEntity().Solve("x")
    was  { 1 } \/ BB
    is   { 1 } \/ (BB \ { x : 1 + x ^ 6 + x * y = 0 })
```

A conjunction with an unsettled side is answered as the conjunction. Intersecting a finite set with
a condition keeps the elements whose membership could not be decided, so taking the intersection
here would have replaced one false claim with another — `{ 1 }` says 1 solves
`x^6 + x*y + 1 = 0`, and it does so only at `y = -2`:

```
"x6 + x y + 1 = 0 and x - 1 = 0".ToEntity().Solve("x")
    was  {  }
    is   { x : x ^ 6 + x * y + 1 = 0 and x - 1 = 0 }
```

**What this means for callers.** `Solve` has always been typed `Set` and has always been able to
return an `Interval`, a `ConditionalSet` or a union; what changes is how often it does. Code that
casts the result to `FiniteSet`, or that reads an empty result as "no solutions", has to say which
of the two it means:

```csharp
var answer = equation.Solve(x);
if (answer is FiniteSet roots)      { /* these are all of them */ }
else if (answer.IsSetEmpty)         { /* shown to have none */ }
else                                { /* not settled, or not finite */ }
```

Nothing that solved before stops solving, and no equation that was shown to have no roots gains
any. Solving `x2 - 4 = 0`, `sin(x) = 1/2`, `x2 + 1 = 0` and the system solver's answers are
unchanged, as is `Solve` on an inequality.

### A bound index is not a free variable

`FreeVariables` knew about two binders — `Lambda` and the set builder — and about no others. A
summation and a product bind their index, so `sum(k, k, 1, n)` is a function of `n` alone, and a
definite integral binds its variable between its limits. Both reported the bound name as free.

Measured on a build of 2.3.0 and a build of this branch:

| input | 2.3.0 | now |
|---|---|---|
| `sum(k, k, 1, n)` | `{ k, n }` | `{ n }` |
| `product(k, k, 1, n)` | `{ k, n }` | `{ n }` |
| `integral(t * b, t, 0, 1)` | `{ b, t }` | `{ b }` |
| `integral(t * b, t)` | `{ b, t }` | `{ b, t }` |
| `derivative(t * b, t)` | `{ b, t }` | `{ b, t }` |
| `lambda(x, x + y)` | `{ y }` | `{ y }` |
| `{ k : k > a }` | `{ a }` | `{ a }` |

The index is bound over the **bounds** as well as the body, which is what
[`Binding`](Sources/AngouriMath/Core/Binding.cs) already says of itself: the name a binder is handed
is honoured throughout it, through the summand and the bounds. So `sum(k, k, k, n)` is `{ n }` too.

**The last three rows are unchanged on purpose, and the distinction is the interesting part.** An
indefinite integral does not bind: the antiderivative of `t * b` over `t` is `b * t ^ 2 / 2 + C`,
which is still a function of `t`. Neither does a derivative — `d/dt` denotes a function of `t`. They
look like the same shape as a summation and are not, and a later sweep that completes the binder list
by adding them would make them wrong. `FreeVariablesTest` pins all of it, including the two that must
stay put.

**`Vars` and `VarsAndConsts` do not change.** They mean every name *occurring*, bound ones included —
`"sum(k, k, 1, n)".ToEntity().Vars` is still `{ k, n }` — which is what their own XML example has
always documented, listing a lambda's parameter under *variables and constants*. Occurring and free
are different questions and the three properties answer them separately.

[#1019](https://github.com/asc-community/AngouriMath/issues/1019).

### A polynomial inequality of degree three or more is answered

**Was** — every univariate polynomial inequality above degree two was refused outright, whatever its
coefficients and however easily it factored:

```
"x ^ 3 - x > 0".ToEntity().Solve("x")
    NotSufficientlySupportedException: Only linear and quadratic polynomial inequalities are
    supported; this one is of a higher degree
```

**Is** — the solution set, where the real roots can be established completely:

```
"x ^ 3 - x > 0".ToEntity().Solve("x")                   (-1; 0) \/ (1; +oo)
"x ^ 3 - x >= 0".ToEntity().Solve("x")                  { 0, 1, -1 } \/ (-1; 0) \/ (1; +oo)
"(x - 1) ^ 2 * (x + 2) > 0".ToEntity().Solve("x")       (-2; 1) \/ (1; +oo)
"x ^ 4 - 5 * x ^ 2 + 4 > 0".ToEntity().Solve("x")       (-oo; -2) \/ (-1; 1) \/ (2; +oo)
"x ^ 3 - 2 * x + 1 > 0".ToEntity().Solve("x")           ((-1 - sqrt(5)) / 2; (-1 + sqrt(5)) / 2) \/ (1; +oo)
"x ^ 3 - 2 > 0".ToEntity().Solve("x")                   (2 ^ (1/3); +oo)
"x ^ 5 - 5 * x ^ 3 + 4 * x > 0".ToEntity().Solve("x")   (-2; -1) \/ (0; 1) \/ (2; +oo)
"x ^ 4 + 1 > 0".ToEntity().Solve("x")                   (-oo; +oo)
"x ^ 4 - 2 > 0".ToEntity().Solve("x")                   (-oo; -2 ^ (1/4)) \/ (2 ^ (1/4); +oo)
"x ^ 4 - 10 * x ^ 2 + 1 > 0".ToEntity().Solve("x")      (-oo; -sqrt((10 + 4 * sqrt(6)) / 2))
                                                          \/ (-sqrt((10 - 4 * sqrt(6)) / 2); sqrt((10 - 4 * sqrt(6)) / 2))
                                                          \/ (sqrt((10 + 4 * sqrt(6)) / 2); +oo)
```

A polynomial has one sign on each open interval between consecutive real roots, so the answer is the
union of the intervals where that sign is positive. What makes it an answer rather than a guess is
that the list of real roots is *complete*: the polynomial is written as a product of powers of
irreducibles over `Q`, verified to multiply back, and the number of real roots of each irreducible
factor is read off its **discriminant** — two where a quadratic factor's is positive and none where
it is negative, three and one respectively for a cubic, and — for a quartic — the discriminant
with the two auxiliary quantities of the standard criterion, a negative discriminant meaning two
real roots and a positive one four or none. A missed root would merge two intervals of
opposite sign and report the wrong half of one as the solution, so this is the difference between
the feature and a wrong answer.

**And the refusal that remains is a different one.** The gap is no longer degree; it is an
irreducible factor of degree five or more, where there is no criterion for the number of real roots
and no formula for the roots either. So the message changed too:

```
"x ^ 5 - x - 1 > 0".ToEntity().Solve("x")
    NotSufficientlySupportedException: Only polynomial inequalities are supported, and of those
    only the ones whose real roots can be established completely: linear and quadratic with any
    coefficients, and higher degrees where the coefficients are rational and no irreducible factor
    is of degree five or more
```

Code that caught `NotSufficientlySupportedException` still catches it; code that matched on the
message text does not. Linear and quadratic inequalities are untouched, including the symbolic
coefficients and the case splits on their signs, which the sign table does not do — it takes only
rational coefficients, and only from degree three up.

[#746](https://github.com/asc-community/AngouriMath/issues/746) item 43.

### A printed operator that is not associative keeps its brackets

`Stringize` is the library's own input format: parsing what it prints has to give back the
expression it printed, and that is what [`StringizeRoundTripTest`](Sources/Tests/UnitTests/Convenience/StringizeRoundTripTest.cs)
enforces. Six operators broke it, five of them in the same way — the printer left the **right**
operand unbracketed at its own precedence level, while the grammar folds that level to the left, so
the printed text came back re-associated. The sixth, `provided`, is the mirror: it is the one infix
operator the grammar folds to the **right**, so there it is the *left* operand that mis-associates.

Four of the six are not associative, so the re-association changed the answer and not merely
the shape. Measured on a build of 2.3.0 and a build of this branch:

| input | 2.3.0 printed | 2.3.0 read that back as | value moved |
|---|---|---|---|
| `false implies (true implies false)` | `False implies True implies False` | `(False implies True) implies False` | `True` → `False` |
| `{ 1, 2, 3 } \ ({ 2, 3 } \ { 3 })` | `{ 1, 2, 3 } \ { 2, 3 } \ { 3 }` | `({ 1, 2, 3 } \ { 2, 3 }) \ { 3 }` | `{ 1, 3 }` → `{ 1 }` |
| `{ 1, 2 } \/ ({ 3 } \ { 1, 2 })` | `{ 1, 2 } \/ { 3 } \ { 1, 2 }` | `({ 1, 2 } \/ { 3 }) \ { 1, 2 }` | `{ 1, 2, 3 }` → `{ 3 }` |
| `{ 1, 2 } \ ({ 2 } \/ { 3 })` | `{ 1, 2 } \ { 2 } \/ { 3 }` | `({ 1, 2 } \ { 2 }) \/ { 3 }` | `{ 1 }` → `{ 1, 3 }` |
| `2 * (3 mod 2)` | `2 * 3 mod 2` | `(2 * 3) mod 2` | `2` → `0` |

On this branch each of those prints its brackets and reads back as itself, so the value column
no longer moves.

`implies` also changes in the other direction, because its printer had the rule the wrong way
round rather than missing: it bracketed the **assumption** — the operand that never needs it under
a left fold — and not the conclusion. So `(a implies b) implies c`, which used to print as
`(a implies b) implies c`, now prints flat as `a implies b implies c`. Both read back as the same
expression; the brackets were redundant, and printing them is what made the genuinely ambiguous
case look no different.

`\/` and `mod` are bracketed only against the operator that makes them ambiguous, because both
share a precedence level with an operator that is not associative while being associative
themselves:

- `A \/ (B \/ C)` still prints `A \/ B \/ C`; `A \/ (B \ C)` now prints `A \/ (B \ C)`.
- `x * (y * z)` still prints `x * y * z` and `x * (y / z)` still prints `x * y / z`;
  `x * (y mod z)` now prints `x * (y mod z)`.
- `-1 * (y mod z)` printed `-y mod z` and now prints `-(y mod z)`.

`provided` is bracketed on the other side, and it is the case where the value is *not* the test.
`(x provided p) provided q` and `x provided (p provided q)` are both `x` exactly when `p` and `q`
hold, so no answer moves — and they are different expressions, which is what the round trip is
about. It printed flat and read back as the right-nested one.

**What has not changed, deliberately.** An operator that *is* associative still prints flat, so
`x + (y + z)` still prints `x + y + z`, and likewise for `*`, `and`, `or`, `xor`, `unite` and
`intersect`. Those come back as a different `Entity` — a left-nested tree instead of
a right-nested one — and as the same number, because the bracketing carries no mathematics. The
alternative would print every expanded polynomial as a right-nested pile of parentheses:
`"(a + b + c + d) ^ 2".ToEntity().Expand()` prints, on both versions,
`d ^ 2 + 2 * c * d + c ^ 2 + 2 * b * d + 2 * b * c + b ^ 2 + 2 * a * d + 2 * a * c + 2 * a * b + a ^ 2`,
and its sum is right-nested throughout. `StringizeRoundTripTest` pins that decision so that it
stays one.

**LaTeX.** `Latexize` moves for the four set and `mod` cases above and **not** for `implies`.
[CSharpMath.Evaluation](https://github.com/verybadcat/CSharpMath/blob/master/CSharpMath.Evaluation/Evaluation.cs),
which reads our LaTeX back, folds `\cup`, `\setminus`, `\in`, `\cdot` and `\bmod` to the left at
the same precedences this grammar uses — so those needed the same brackets — but folds `\to` to the
**right**, which is the usual convention for implication and is what `Latexize` was already
bracketing for. The change only ever adds `\left(`/`\right)` groups, which CSharpMath already
parses, so nothing downstream needs a matching change
([#822](https://github.com/asc-community/AngouriMath/issues/822)).

### A limit binds the variable it approaches along

`FreeVariables` learned about a summation, a product and a definite integral in
[#1045](https://github.com/asc-community/AngouriMath/pull/1045), and about `limit` not at all —
that commit names the indefinite integral and the derivative as deliberate exclusions and does
not mention it.

| | before | now |
|---|---|---|
| `"limit(t * b, t, 0)".ToEntity().FreeVariables` | `{ t, b }` | `{ b }` |
| `"limit(t, t, b)".ToEntity().FreeVariables` | `{ t, b }` | `{ b }` |
| `"limitleft(t * b, t, 0)".ToEntity().FreeVariables` | `{ t, b }` | `{ b }` |
| `"limit(t, t, 0)".ToEntity().FreeVariables` | `{ t }` | `{ }` |

The reason the indefinite integral and the derivative do not bind is exactly what makes a limit
bind: an antiderivative of `t * b` over `t` is `b * t ^ 2 / 2 + C` and `d/dt` denotes a function
of `t`, both still functions of the variable — while **a limit never is**. `lim(t, t, 0)` is `0`,
and no limit's value depends on the name it approaches along. The destination is where the
dependence goes, so it is bound over too: `lim(t, t, b)` is a function of `b` alone.

`Vars` and `VarsAndConsts` are untouched, as they were in #1045 — they mean every name occurring
([#989](https://github.com/asc-community/AngouriMath/issues/989)).

### A leading coefficient that is a polynomial no longer stops the lift

The Hensel lift below declined when the leading coefficient in the main variable was not a
constant: the lifted factors' leading coefficients have to be known before the lift to keep them
polynomials rather than power series. That is Wang's leading-coefficient problem, and it is
avoided here rather than solved — `L^(n-1) f(z/L, y) = Σ a_i L^(n-1-i) z^i` is a polynomial,
because `n - 1 - i` is never negative below the leading term, and it is **monic** in `z`, because
the leading term contributes `L · L^(-1)`. A monic polynomial has a constant leading coefficient,
which is the case the lift already handled. A factor comes back as `h(L·x, y)` with the content
that substitution introduced divided out.

| | before | now |
|---|---|---|
| `Factor("(y * x3 + 1) * (x4 - y3)", "x")` | `null` | `(x ^ 4 - y ^ 3) * (x ^ 3 * y + 1)` |
| `Factor("(y * x + 1) * (x7 - y7)", "x")` | `null` | three factors |
| `Factor("(y * x2 + 1) * (x6 - y6)", "x")` | `null` | five factors |
| `Factor("(y2 * x3 + 1) * (x5 - y2)", "x")` | `null` | `null` — past the growth bound below |
| `Factor("(y * x + 1) * (x + y)", "x")` | `(x * y + 1) * (x + y)` | unchanged — the substitution already reached it |

That last-but-one row is a **bound rather than a limitation of the method**. The monic form
carries `L^(n-1-i)`, so its degree in the auxiliary variable exceeds the original's by up to
`(n-1)·deg L`. Measured: a growth of 6 costs 376 ms and a growth of 7 costs 1 ms, while a growth
of 14 costs **63 seconds** — so growth past 12 is refused. A refusal is a legitimate answer and a
sixty-three second one is not, which is the same judgement the recombination's own cap makes.

### A factorisation multiplies back to what it factored

`MathS.Polynomials.Factor` dropped a constant content when the polynomial had more than one
variable, so the product it returned was **not equal to the polynomial it factored**.

| | before | now |
|---|---|---|
| `Factor("4 * x2 - 4 * y2", "x")` | `(x + y) * (x - y)` | `4 * (x + y) * (x - y)` |
| `Factor("2 * x2 - 2 * y2", "x")` | `(x + y) * (x - y)` | `2 * (x + y) * (x - y)` |
| `Factor("6 * x4 - 6 * y4", "x")` | three factors, no `6` | `6 * (x + y) * (x ^ 2 + y ^ 2) * (x - y)` |
| `Factor("3 * x * y + 3 * y", "x")` | `y * 3 * (x + 1)` | unchanged — the content is not a constant |
| `Factor("2 * x2 - 2", "x")` | `2 * (x + 1) * (x - 1)` | unchanged — univariate |

`KroneckerFactorization.Factor` documents its result as *"each of positive degree in the main
variable"*, so a constant content is deliberately not among the factors it returns and the caller
must reinstate it. `MathS.Polynomials.Kronecker` assembled the factors into a product and never
did. Only "multivariate **and** the content is a pure number" landed in the gap; a content with a
variable in it goes down a different path that reinstates it, and a univariate polynomial never
reaches the substitution.

The content is recovered by dividing the polynomial by the assembled product, which is what the
rest of this layer does with a claim it could get wrong — a quotient that is missing or is not a
constant means the product does not account for the polynomial, and then there is no answer to
give. Everything here was already checked by exact division, but that check is on the individual
*factors*; nothing compared the assembled product against the input
([#1092](https://github.com/asc-community/AngouriMath/issues/1092)).
### `Factorize` reaches inside a product the rules already made

The change below asks the polynomial layer only where the rewrite rules said nothing, so that an
answer the rules already gave is never replaced. Declining a **product** outright was too broad:
the rules take a numeric content out and hand back `2 * (x ^ 3 - 1)`, and the remainder is exactly
the shape that change is about.

| | before | now |
|---|---|---|
| `"2 * x3 - 2".Factorize()` | `2 * (x ^ 3 - 1)` | `2 * (x - 1) * (x ^ 2 + x + 1)` |
| `"3 * x6 - 3".Factorize()` | `3 * (x ^ 6 - 1)` | five factors |
| `"5 * x7 - 5".Factorize()` | `5 * (x ^ 7 - 1)` | `5 * (x - 1) * (x ^ 6 + … + 1)` |
| `"y * (x3 - 1)".Factorize()` | `y * (x ^ 3 - 1)` | `y * (x - 1) * (x ^ 2 + x + 1)` |
| `"2 * x4 - 10 * x2 + 8".Factorize()` | `2 * (x + 1) * (x + 2) * (x - 2) * (x - 1)` | unchanged |
| `"x * y + x".Factorize()` | `x * (1 + y)` | unchanged |

Each factor of the product is asked separately instead of the product being handed over whole, so
every factor the rules found survives and only the ones they could not split are split. Found by
asking the property [#1092](https://github.com/asc-community/AngouriMath/issues/1092) is about —
that the answer multiplies back to its input — of a row that satisfied it and was still less
factored than it should be.
### An equation between two powers of numeric bases is answered exactly

`3 ^ (x+1) = 2 ^ (x-1)` has the exact root `-ln(6) / ln(3/2)`. The multiplicative solver reached it
by dividing one exponent by the other; for two different integer bases that ratio is
`ln(3)/ln(2)` — irrational — so `InnerSimplified` settled it to a decimal and everything after was
numeric. The answer agreed with the exact one to seventeen significant figures and diverged, which
is the signature of a `double` promoted to a decimal rather than a number that was computed.

| | before | now |
|---|---|---|
| `"3^(x+1) - 2^(x-1)".SolveEquation("x")` | `{ ln(0.0467456982262863043886547131933184573426842689514160156250 ^ (1 / ln(2))) }` | `{ -(ln(3) + ln(2)) / (ln(3) + -ln(2)) }` |
| `"3^(x+1) - 2^x".SolveEquation("x")` | a decimal | `{ -ln(3) / (ln(3) + -ln(2)) }` |
| `"5^(2x) - 7^(x+3)".SolveEquation("x")` | `{ }` — **no answer at all** | `{ 3 * ln(7) / (2 * ln(5) + -ln(7)) }` |
| `"3^x - 2^x".SolveEquation("x")` | `{ 0 }` | unchanged |
| `"2^(2x) - 5·2^x + 4".SolveEquation("x")` | `{ 0, 2 }` | unchanged |

Taking logarithms has no such step: `a ^ p = b ^ q` is `p ln a = q ln b` for positive real `a` and
`b`, which the analytical solver answers exactly. Both bases must be **decidably positive reals**,
which is what makes the step an equivalence rather than a branch choice; anything else declines and
the multiplicative path still gets its turn, so this only ever adds answers
([#1007](https://github.com/asc-community/AngouriMath/issues/1007)).
### A widened codomain is written out, so `Stringize` round-trips it

`Domain.Any` had no spelling: the second argument of `domain(...)` had to be one of the five
special sets, and none of them means "no restriction". So a node **widened** to `Any` from a
narrower default printed as though it had not been, and reading that back gave the default.

| | before | now |
|---|---|---|
| `MathS.Abs("x").WithCodomain(Any).Stringize()` | `abs(x)` | `domain(abs(x), Any)` |
| the same, reparsed | `Real` — the widening was lost | `Any` |
| `MathS.Abs("x").WithCodomain(Integer).Stringize()` | `domain(abs(x), ZZ)` | unchanged |
| `"Any + 1"` | parses, `Any` is a variable | unchanged |

`Any` is **not** a set literal — there is still no node for "no restriction", and
`SpecialSet.Create(Domain.Any)` still throws for it. It is read in the second argument of
`domain(...)` and nowhere else, which commits to a spelling without deciding whether there is a
universal *set*; that question is [#996](https://github.com/asc-community/AngouriMath/issues/996)
and this does not answer it.

Read rather than lexed, deliberately. A literal in a parser rule becomes a global lexer token, and
making `Any` a keyword reserved the name everywhere — `Any + 1` stopped parsing. A test pins that
it stays an ordinary variable.

`Latexize` renders the subscript as `\mathrm{Any}` rather than a `\mathbb`, since there is no set
to render ([#1048](https://github.com/asc-community/AngouriMath/issues/1048)).

### A fold over an empty sequence answers instead of throwing

A fold over a monoid has an identity, so the empty sum is `0` and the empty product is `1` — which
is what makes `xs.Concat(ys).SumAll() == xs.SumAll() + ys.SumAll()` hold for every pair, the empty
one included. These threw instead, and threw the wrong *kind* of exception: `AngouriBugException`
ends its message asking the caller to report a bug against this repository, for a list their own
`Where` happened to filter to nothing.

| | before | now |
|---|---|---|
| `new Entity[0].SumAll()` | `AngouriBugException` | `0` |
| `new Entity[0].MultiplyAll()` | `AngouriBugException` | `1` |
| `Sumf.Sum(new Entity[0])` | `AngouriBugException` | `0` |
| `Mulf.Multiply(new Entity[0])` | `AngouriBugException` | `1` |
| `MathS.Vector()` | `IndexOutOfRangeException` | `InvalidMatrixOperationException` |
| `new Entity[0].ToVector()` | `IndexOutOfRangeException` | `InvalidMatrixOperationException` |

`IndexOutOfRangeException` is not under `AngouriMathBaseException`, so a caller catching the
hierarchy `Docs/Usage/Exceptions.md` documents did not catch it at all.

`Sumf.Sum` and `Mulf.Multiply` are not named in the issue. The defect is *passing an unchecked
caller collection into `MultiHangBinary`*, whose `>= 1` precondition is genuine, and those two are
public and do exactly that. `MultiHangBinary` itself is unchanged
([#1028](https://github.com/asc-community/AngouriMath/issues/1028)).

### `Factorize` uses the polynomial layer where no rule reaches

`Entity.Factorize` was composed entirely out of `RewriteRules`, so it factored what someone had
written a rule for and handed everything else back whole — while square-free decomposition,
Zassenhaus over `Q`, Kronecker's substitution and Hensel lifting all sat in the tree unused by it.

| | before | now |
|---|---|---|
| `"x3 - 1".Factorize()` | `x ^ 3 - 1` | `(x - 1) * (x ^ 2 + x + 1)` |
| `"x4 - 5x2 + 4".Factorize()` | `x ^ 4 - 5 * x ^ 2 + 4` | `(x + 1) * (x + 2) * (x - 2) * (x - 1)` |
| `"x6 - 1".Factorize()` | `x ^ 6 - 1` | `(x + 1) * (x - 1) * (x ^ 2 + x + 1) * (x ^ 2 - x + 1)` |
| `"x7 - 1".Factorize()` | `x ^ 7 - 1` | `(x - 1) * (x ^ 6 + x ^ 5 + x ^ 4 + x ^ 3 + x ^ 2 + x + 1)` |
| `"x2 + 2x + 1".Factorize()` | `x ^ 2 + 2 * x + 1` | `(x + 1) ^ 2` |
| `"a2 - b2".Factorize()` | `(a - b) * (a + b)` | unchanged |
| `"x4 - y4".Factorize()` | `(x - y) * (x + y) * (x ^ 2 + y ^ 2)` | unchanged |
| `"x * y + x".Factorize()` | `x * (1 + y)` | unchanged |
| `"sin(x) + 1".Factorize()` | `sin(x) + 1` | unchanged |

**The layer speaks only where the rules said nothing.** An expression the rules already turned
into a product keeps their answer exactly — the order two factors come out in is arbitrary and
theirs is the one on record, so replacing it would change answers that were never the complaint.
What moves is only what came back whole.

**`Simplify` is unchanged**, and that took a second seam. It offers a factorisation as a
*candidate* and its cost model decides; the metric prefers the expanded form, so a factored
candidate wins only where the two are closest — and those are the places a factored answer is
least wanted (`x ^ 3 / 3 + x ^ 2 / 2` became `(3 + 2 * x) * x ^ 2 / 6`, an antiderivative in a
form nobody writes). `Transformation.RuleBasedFactorizationAtLevel` is what that candidate site
uses now. Offering the layer to that search is
[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 2's pluggable cost model
rather than this ([#1018](https://github.com/asc-community/AngouriMath/issues/1018)).

### A polynomial in two variables is factored by lifting rather than by substituting

`MathS.Polynomials.Factor` in several variables was Kronecker's substitution alone, and its
one-variable image **over-factors**: `x7 - y7` maps to `t^7 (1 - t^49)`, whose factors are
cyclotomic, so a two-factor bivariate becomes a one-variable polynomial with many irreducibles
and the recombination is exponential in a count the substitution inflated itself. Those were
refusals.

An *evaluation* image inflates nothing — `x7 - y7` at `y = 1` is `x7 - 1`, which has the two
factors the answer has — and the factorisation of that image is lifted back one power of `y` at
a time.

| | before | now |
|---|---|---|
| `Factor("x7 - y7", "x")` | `null` | `(x - y) * (x ^ 6 + x ^ 5 * y + x ^ 4 * y ^ 2 + x ^ 3 * y ^ 3 + x ^ 2 * y ^ 4 + x * y ^ 5 + y ^ 6)` |
| `Factor("x6 - y6", "x")` | `null` | `(x + y) * (x - y) * (x ^ 2 + x * y + y ^ 2) * (x ^ 2 - x * y + y ^ 2)` |
| `Factor("x12 - y12", "x")` | `null` | six factors, the full cyclotomic split |
| `Factor("x4 - y10", "x")` | `null` | `(x ^ 2 + y ^ 5) * (x ^ 2 - y ^ 5)` |
| `Factor("x16 + 4 x8 y + 3 y2", "x")` | `null` | `(x ^ 8 + 3 * y) * (x ^ 8 + y)` |
| `Factor("x ^ 3 - (y + z) ^ 3", "x")` | `null` | `null` — three variables, and the lift goes along one |
| `Factor("(x + y) * (x - y)", "x")` | `(x + y) * (x - y)` | unchanged |

**What it will not do.** The leading coefficient in the main variable has to be a constant.
Where it is a polynomial in `y`, the lifted factors' leading coefficients must be known before
the lift to keep them polynomials rather than power series — Wang's leading-coefficient problem
— and that is a second algorithm on top of this one. It declines, and the substitution is still
tried first. Three or more variables decline for the same reason: the lift goes along one
evaluation at a time.

**Nothing is trusted.** Every candidate is checked by exact division of the original, so a bad
evaluation point, a lift that drifted, or a recombination that is not a factor costs a refusal
and cannot cost a wrong answer.

This is [#746](https://github.com/asc-community/AngouriMath/issues/746) tier 1's Hensel lifting
item, in two variables.

### A polynomial in too many variables to substitute can still be answered

`MathS.Polynomials.Factor` in more than one variable works by Kronecker's substitution, whose
one-variable image has degree `Π (d_i + 1) - 1` — a *product*. That leaves the one-variable
factoriser's reach after very few variables, and the answer was a refusal:

| | before | now |
|---|---|---|
| `Factor("x2 + y2 + z2 + w2 + 1", "x")` | `null` | `w ^ 2 + x ^ 2 + y ^ 2 + z ^ 2 + 1` |
| `Factor("x2 + y2 + z2 + 1", "x")` | `x ^ 2 + y ^ 2 + z ^ 2 + 1` | unchanged |
| `Factor("x7 - y7", "x")` | `null` | `null` — it *is* reducible, and this says nothing about it |
| `Factor("(x + y) * (x - y)", "x")` | `(x + y) * (x - y)` | unchanged |
| `Factor("y * (x + 1)", "x")` | `y * (x + 1)` | unchanged |

Where the substitution gives up, the polynomial is now evaluated at an integer point in every
variable but the main one and the one-variable image is factored. **An image that is irreducible
and has kept its degree is a proof that its source is irreducible** — substitution is a ring
homomorphism, so a factorisation would survive it, and degrees in the main variable add, so
neither part can have lost any while the total is preserved. An evaluation image has the degree
of the polynomial in the main variable however many other variables there are, so this reaches
where the substitution cannot.

It answers in one direction only. A reducible image says nothing, so `x7 - y7` is still refused;
a factor free of the main variable is invisible to it, so the content is checked and anything but
a constant declines. Since
[#1059](https://github.com/asc-community/AngouriMath/pull/1059) "it does not factor" is an
answer rather than a refusal, which is what makes this worth having
([#746](https://github.com/asc-community/AngouriMath/issues/746) tier 1).

### `x! = 0` carries the condition under which the factorial exists

A factorial is never zero **where it is defined**, and at a negative integer it is not defined.
`"x! = 0".ToEntity().Simplify()` was `False` for every `x`, so at `x = -1` it answered a question
the original declines: `(-1)! = 0` evaluates to `NaN`.

| | before | now |
|---|---|---|
| `"x! = 0".ToEntity().Simplify()` | `False` | `False provided x in RR and (x >= 0 or not x in ZZ)` |
| the same, at `x = 3` | `False` | `False` |
| the same, at `x = -1` | `False` | `NaN` |
| `RewriteRules.InequalityEquality.ApplyOnce("x! = 0")` | `False` | `False provided x in RR and (x >= 0 or not x in ZZ)` |

The rule read `Factorialf({ DomainCondition: var condition })`, which is a property pattern on the
factorial's **argument** rather than on the factorial. For a bare variable that condition is `True`,
and `Provided` drops a `True`, so the answer went out unconditioned. One character of pattern syntax
between the two, and the wrong one reads as though it were about the factorial.

Where the factorial exists the answer is still `False`, so the condition narrows the rule rather
than withdrawing it. A factorial **over itself** moves with it: `a / a = 1 provided a != 0`
produced `1 provided not x! = 0`, whose condition used to discharge itself because `x! = 0` was
`False`, and now survives. `"x! / x!".ToEntity().Simplify()` was `1` at every point including the
poles; it now agrees with the original at `x = 3`, `0`, `-1` and `-2`, where `1` disagreed at both
negative integers. Found by transcribing the set into `MatchedRules` for
[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 1, as the single disagreement
out of 3,485 generated expressions
([#1081](https://github.com/asc-community/AngouriMath/issues/1081)).

### Four `or`-with-equality rules gave the opposite comparison

`a < b or a = b` is `a <= b`. Written with the comparison the other way round it answers the other
way round: `b < a or a = b` is `a >= b`. Four of the eight arms of `InequalityEqualityRules` that
say this carried their neighbour's answer, so the result was the negation of the input everywhere
off the diagonal.

| | before | now |
|---|---|---|
| `RewriteRules.InequalityEquality.ApplyOnce("(y < x) or (x = y)")` | `x <= y` | `x >= y` |
| `RewriteRules.InequalityEquality.ApplyOnce("(y > x) or (x = y)")` | `x >= y` | `x <= y` |
| `RewriteRules.InequalityEquality.ApplyOnce("(x = y) or (y < x)")` | `x <= y` | `x >= y` |
| `RewriteRules.InequalityEquality.ApplyOnce("(x = y) or (y > x)")` | `x >= y` | `x <= y` |
| `RewriteRules.InequalityEquality.ApplyOnce("(x < y) or (x = y)")` | `x <= y` | `x <= y` — this half was right |
| `RewriteRules.InequalityEquality.ApplyOnce("(x > y) or (x = y)")` | `x >= y` | `x >= y` — and so was this |

`Simplify` moves with it: `"(y < x) or (x = y)".ToEntity().Simplify()` was `x <= y` and is `x >= y`.
At `x = 3, y = 2` the input is True and the old answer is False.

**Only reachable with both operands symbolic**, which is why it survived. With a number on one side,
the `Lessf(var @const, ...)` arm further down the same set rewrites `2 < x` to `x > 2` earlier in the
pass, so the disjunction is only ever looked at with both halves written the same way round and one
of the four *correct* arms matches — `"(2 < x) or (x = 2)".ToEntity().Simplify()` was and is `x >= 2`.

Found by transcribing the set into `MatchedRules` for
[#746](https://github.com/asc-community/AngouriMath/issues/746) tier 1: writing a rule out as data
makes the correspondence between its pattern and its replacement something you have to state, and
four of these did not survive stating it
([#1077](https://github.com/asc-community/AngouriMath/issues/1077)).

### A reciprocal inside a logarithm is no longer moved out unconditionally

`ln(1/b) = -ln(b)` is false on the negative reals, because the principal argument does not negate
with its logarithm. At `b = -0.63`, `ln(1/b)` is `0.462 + πi` and `-ln(b)` is `0.462 − πi`.

Three arms of `PowerRules` applied it for every `b`, and they now carry the guard their neighbours
ten lines below already had — `ln(1/b)` is `ln(1) − ln(b)`, so a reciprocal is the *difference* case
of the logarithm gathering and the same helper answers it. Both ways of earning the rewrite carry
over: the argument is decidably a positive real, or the limit machinery is reading towards a
destination and has established the sign on the way.

| | before | now |
|---|---|---|
| `RewriteRules.Power.ApplyOnce("ln(1 / x)")` | `-ln(x)` | `ln(1 / x)` |
| `RewriteRules.Power.ApplyOnce("log(2, 1 / x)")` | `-log(2, x)` | `log(2, 1 / x)` |
| `RewriteRules.Power.ApplyOnce("log(1 / x, 1 / y)")` | `log(x, y)` | `log(1 / x, 1 / y)` |
| `RewriteRules.Power.ApplyOnce("ln(1 / 2.5)")` | `-ln(5/2)` | `-ln(5/2)` — the argument is positive |

**`Simplify` is unchanged**, and that is why this went unnoticed: `"ln(1 / x)".Simplify()` was
`ln(1 / x)` before and after, because the candidate search never picked that branch. So no answer
from the public simplifier moves. What moves is `RewriteRules.Power` applied on its own, which is
what [#746](https://github.com/asc-community/AngouriMath/issues/746) tier 2 makes a caller able to
do ([#1062](https://github.com/asc-community/AngouriMath/issues/1062)).

### `RewriteRuleGrowth` gained a fourth value, `Unknown`

A rule written as **data** builds its answer in code rather than spelling it out, so there is nothing
to count and no growth to report. The three existing values all make a claim; reporting such a rule
as `Rearranges` — the middle one, and the one that reads as harmless — would be a claim about a
rewrite nobody measured.

**Additive**, so nothing that exists changes value. What it means for a caller is that a `switch`
over `RewriteRuleGrowth` is no longer exhaustive, and code with no default arm will not compile
against the new assembly until it handles the fourth case.

It appears wherever a rule's replacement is code, which today is `RationalizeDenominator` and the
one-way rules of the sets already expressed as data.
### `RewriteRules.Boolean` absorbs in three orientations it used to miss

Absorption is written eight times in `Patterns.BooleanRules`, once for each way the shared operand
can sit inside two commutative pairs — and three of the ways were never written. Expressed as data
the law is **one commutative rule**, which covers all of them.

| | before | now |
|---|---|---|
| `RewriteRules.Boolean.ApplyOnce("a and b or a")` | `a and b or a` | `a` |
| `RewriteRules.Boolean.ApplyOnce("(a or b) and a")` | `(a or b) and a` | `a` |
| `RewriteRules.Boolean.ApplyOnce("a or b and not a")` | `a or b and not a` | `a or b` |

Each is a correct absorption: `(a ∧ b) ∨ a` is `a`, and `a ∨ (b ∧ ¬a)` is `a ∨ b`.

**`Simplify` is unchanged** — it reached all three already, because the canonical order puts the
operands into a fixed arrangement before the rules run, so the orientations the arms missed were
never the ones it was handed. What moves is `RewriteRules.Boolean` applied on its own.

The same file already carried a comment about this class of gap, on the excluded-middle rule:
*"The same law with the operands the other way round. `or` is commutative, so leaving this out made
the answer depend on which side the negation was written."* It was fixed there for one rule and left
for the rest.

### `Factor` factors a polynomial in more than one variable

After the content is taken out, what remains may still have polynomial coefficients — and it can be
factored anyway, by **Kronecker's substitution written in mixed radix**. A factor of a polynomial
has degree at most `d_i` in each variable `v_i`, because a factor divides it. So with radices
`d_i + 1` and place values `s_0 = 1`, `s_(i+1) = s_i * (d_i + 1)`, the map sending a monomial to
`t^(Σ e_i · s_i)` writes each exponent as one digit of a numeral, and is therefore injective on
every monomial that can appear in the polynomial or in any of its factors. The one-variable image
is factored by the existing factoriser, and each subset of its irreducible factors names a
candidate.

| | 2.3.0 | now |
|---|---|---|
| `Factor("x ^ 2 - y ^ 2", "x")` | `null` | `(x + y) * (x - y)` |
| `Factor("x ^ 2 + 2 * x * y + y ^ 2", "x")` | `null` | `(x + y) ^ 2` |
| `Factor("x ^ 3 - y ^ 3", "x")` | `null` | `(x - y) * (x ^ 2 + x * y + y ^ 2)` |
| `Factor("x ^ 4 - y ^ 4", "x")` | `null` | `(x + y) * (x ^ 2 + y ^ 2) * (x - y)` |
| `Factor("x ^ 2 * y ^ 2 - 1", "x")` | `null` | `(x * y + 1) * (x * y - 1)` |
| `Factor("x ^ 2 - y ^ 2 + 2 * x + 1", "x")` | `null` | `(x + y + 1) * (x - y + 1)` |
| `Factor("x ^ 2 - (y + z) ^ 2", "x")` | `null` | `(x + y + z) * (x - y - z)` |
| `Factor("x ^ 2 + 2 * x * y + y ^ 2 - z ^ 2", "x")` | `null` | `(x + y + z) * (x + y - z)` |
| `Factor("(x + y) * (x + z) * (x + w)", "x")` | `null` | `(x + y) * (w + x) * (x + z)` |
| `Factor("x * y + y * z", "x")` | `null` | `y * (x + z)` |
| `Factor("a * x + a * y + a * z", "x")` | `null` | `a * (x + y + z)` |
| `Factor("x ^ 2 * y - y ^ 3", "x")` | `null` | `y * (x + y) * (x - y)` |
| `Factor("x ^ 2 + y ^ 2", "x")` | `null` | `x ^ 2 + y ^ 2` — irreducible over ℚ |
| `Factor("x * y + z", "x")` | `null` | `x * y + z` — irreducible over ℚ |
| `Factor("x ^ 2 - a", "x")` | `null` | `x ^ 2 - a` — irreducible over ℚ |
| `Factor("x + y", "x")` | `null` | `x + y` — irreducible over ℚ |

**"It does not factor" is an answer, and the input is how it is said.** The substitution does not
merely fail to find a factorisation; it *proves* there is none. A splitting of the polynomial into
two parts of positive degree in the main variable maps to a splitting of the one-variable image,
because the substitution is a ring homomorphism on these monomials — and every splitting of the
image is one of the subsets the recombination tries. So where nothing recombines, nothing exists.

Reporting that proof as `null` threw away the content along with it: `x * y + y * z` was refused,
although `y` had already been taken out and `y * (x + z)` is a factorisation. It also disagreed with
the one-variable path, where `Factor("x ^ 2 + 1", "x")` has always been `x ^ 2 + 1`.

The proof has one precondition and it is now checked. A trial division answers `null` both for "does
not divide" and for "ran out of room", and only the first is evidence — so where a division was cut
short by a term or degree budget, the irreducibility claim is withheld and the answer stays a
refusal.

**It cannot answer wrongly.** The substitution is injective on monomials but not on factorisations,
so the image may factor further than the polynomial does and a candidate is a guess. Every one is
tested by exact division before it is kept, and the assembled factors are divided back into the
input, so the failure mode is a refusal.

**What it refuses.** The image has degree `Π (d_i + 1) - 1`, a **product** and not a sum, and the
one-variable factoriser stops at 32 — so the ceiling closes quickly as variables are added. Two
variables reach bidegrees like (2, 10), (3, 7) and (5, 4); three variables of degree 2 fit (27) and
four do not (81). `Factor("x ^ 12 - y ^ 12", "x")` and
`Factor("(x + y + z + w) * (x - y)", "x")` are both `null` for this reason, though both factor
mathematically. The recombination is over subsets, so the image's factor count is capped too.
Lifting that ceiling is Hensel lifting with an evaluation homomorphism, which is a different piece
of work.

`MathS.Polynomials.Factor` has no caller inside the library, so no simplification, solution or
integral changes with it.

### The square-free part is taken where the coefficients are polynomials

`MathS.Polynomials.SquareFreePart` refused every polynomial in more than one variable, for the same
reason `Factor` did: it was written against a representation with rational coefficients.

`p / gcd(p, dp/dx)` is the square-free part whatever ring the coefficients live in — a repeated
factor appears in the derivative one time fewer than in the polynomial, so dividing by the common
part leaves each distinct factor exactly once. The multivariate representation has all three
operations already: `DerivativeIn`, the recursive greatest common divisor that
`MathS.Polynomials.Gcd` is built from, and exact division.

| | 2.3.0 | now |
|---|---|---|
| `SquareFreePart("(x - y) ^ 2 * (x + y)", "x")` | `null` | `x ^ 2 - y ^ 2` |
| `SquareFreePart("(x - y) ^ 3", "x")` | `null` | `x - y` |
| `SquareFreePart("(x + a) ^ 2 * (x + b)", "x")` | `null` | `a * b + a * x + b * x + x ^ 2` |
| `SquareFreePart("x ^ 2 * y ^ 2", "x")` | `null` | `x` |
| `SquareFreePart("y", "x")` | `null` | `null` |

**The content is dropped, as it always was.** `SquareFreePart("4 * x ^ 2", "x")` is `x` rather than
`4 * x`, because the univariate path takes the primitive part first. `x ^ 2 * y ^ 2` is `x` for
exactly that reason, with `y ^ 2` as the content — the existing convention applied to a wider ring,
not a new one.

Reached only where the rational path declined, so nothing that already answered can change.

### `Factor` takes out the content instead of refusing

`MathS.Polynomials.Factor` works over ℚ, so a coefficient that is not a rational number stopped it
before it began and **every polynomial in more than one variable was refused**. Some of them never
needed a bigger ring: `x * y + y` is `y` times something univariate, and only the `y` was in the way.

The content in the named variable — the greatest common divisor of the coefficients, a polynomial in
the other variables — is now taken out first, using the same multivariate machinery
`MathS.Polynomials.Gcd` is already built from, and what remains goes down the ordinary path.

| | 2.3.0 | now |
|---|---|---|
| `Factor("x * y + y", "x")` | `null` | `y * (x + 1)` |
| `Factor("x ^ 2 * y + x * y", "x")` | `null` | `y * x * (x + 1)` |
| `Factor("a * x ^ 2 + a * x", "x")` | `null` | `a * x * (x + 1)` |
| `Factor("x ^ 2 * y ^ 2 - y ^ 2", "x")` | `null` | `y ^ 2 * (x + 1) * (x - 1)` |

**Only a refusal becomes an answer.** Nothing that already factorised changes, because this path
runs only where the old one returned `null`.

Taking the content out does nothing where the content is a constant, so `x ^ 2 - y ^ 2` is not
answered by this change — it needs factorisation over ℚ(y). That is what Kronecker's substitution
does, in the entry above, and the two paths are tried in that order.

The test that pinned the refusal carried a comment saying that handing `x * y + y` back *"would say
that `y * (x + 1)` does not exist, which is a wrong answer and not a graceful failure"*. It now
asserts that answer, checked numerically at twenty random points per case rather than as a string —
`Simplify` does not prove `y * x * (x + 1)` equal to `x ^ 2 * y + x * y`, and the two are equal.

### A narrowed `Codomain` is printed, so it survives being read back

`Codomain` decides evaluation — `sqrt(-1)` is `i`, and the same expression with `Codomain = Real`
is `NaN`, which is the example on `Entity.Codomain` itself. No node printed it, so the two printed
the same string and the annotation was lost the moment an expression was written out: to a file, to
a database column, through `EntityJsonConverter`, or to another process.
[#1022](https://github.com/asc-community/AngouriMath/issues/1022).

The parser already had the syntax. `domain(expr, SET)` maps onto `WithCodomain` and works for every
node, not only a variable, so only the printing half was missing. Measured on a build of 2.3.0 and a
build of this branch:

| expression | 2.3.0 printed | 2.3.0 read that back as | now prints |
|---|---|---|---|
| `"domain(x, ZZ)".ToEntity()` | `x` | a `Variable` with `Codomain = Any` | `domain(x, ZZ)` |
| `"domain(x + 1, RR)".ToEntity()` | `x + 1` | a `Sumf` with `Codomain = Complex` | `domain(x + 1, RR)` |
| `"domain(sqrt(-1), RR)".ToEntity()` | `sqrt(-1)` | a `Powf` that evaluates to `i` | `domain(sqrt(-1), RR)` |
| `"domain([1, 2], RR)".ToEntity()` | `[1, 2]` | a `Matrix` with `Codomain = Any` | `domain([1, 2], RR)` |
| `Sin(Var("x").WithCodomain(Integer)) + Var("y").WithCodomain(Real)` | `sin(x) + y` | both annotations gone | `sin(domain(x, ZZ)) + domain(y, RR)` |

`EntityJsonConverter` serialises what `Stringize` prints, so it changes with it and needed no code:
`JsonSerializer.Serialize("domain(x, ZZ)".ToEntity())` was `"x"` and is `"domain(x, ZZ)"`.

**A sum stops collecting two terms that are not the same term.** `Simplify`'s polynomial
collection keys a monomial by its base's *printed form*, so while the printed form did not
distinguish `x` from `x` narrowed to the integers, it added them up as one:

```
"x - domain(x, ZZ)".ToEntity().Simplify()      2.3.0: 0                    now: x - domain(x, ZZ)
"domain(x, ZZ) + x".ToEntity().Simplify()      2.3.0: 2 * x                now: domain(x, ZZ) + x
```

`0` is the answer only where `x` is an integer, and the annotation is what says it might not be, so
this was a wrong answer rather than a tidier one. Confirmed as the cause by putting the collision
back — keying on the base with its codomain erased brings `0` and `2 * x` straight back.

**The ordinary expression is untouched.** The wrapper is printed only where the codomain is *not*
the one that parsing the bare text would give back, and nothing inside the library narrows a
codomain — `WithCodomain` is called from the parser and from callers, and from nowhere else. So
`"x + 1".ToEntity().Stringize()` is `x + 1` on both versions, and of the 8,084 tests in the suite
the only two that moved are the one written to pin this defect and the recorded public surface.

That default is **not the same for every node**, which is why the rule is a comparison and not a
check against `Complex`: a `Variable` and a `Matrix` default to `Any`, `Absf`, `Modf`, `Minf`,
`Maxf` and `Interval` to `Real`, every boolean node to `Boolean`, each numeric literal to its own
type's domain, `Phif` to `Integer`, the set nodes and `Providedf`, `Piecewise`, `Application` and
`Lambda` to `Any`, and the rest to `Complex`. Each node now declares that default next to its
`Codomain`, and a test asserts that every freshly built node of every node type carries it.

**LaTeX** renders it as a subscripted set, `{\left(x\right)}_{\mathbb{Z}}`. The parentheses are
unconditional because a variable renders its own index as a subscript, so `x_{\mathbb{Z}}` would be
indistinguishable from a variable spelled that way. This is new output for
[CSharpMath.Evaluation](https://github.com/verybadcat/CSharpMath/blob/master/CSharpMath.Evaluation/Evaluation.cs),
which reads our LaTeX back and has no notion of a codomain; it appears only for an expression that
carries a narrowed one ([#822](https://github.com/asc-community/AngouriMath/issues/822)).

**Two annotations still do not survive, and both are the grammar's limit.** `Any` cannot be written
at all — the second argument of `domain(...)` has to be one of the five special sets, and none of
them means "no restriction" — so a node *widened* to `Any` from a narrower default still prints as
though it had not been. And no input string yields a rational literal whose codomain is `Complex`,
because the pass that reads `1/2` as a `Rational` rather than a quotient
([#873](https://github.com/asc-community/AngouriMath/issues/873)) uses `Complex` as its "nobody
annotated this" sentinel. Both are pinned by tests that fail if they start working, so neither can
outlive itself.

**The public surface.** Each node type used to declare its own `public override string Stringize()`
and `Latexize()`; the codomain wrapper is one decision and now lives once on `Entity`, with the
per-node rendering behind an internal member. All 130 overrides are therefore gone from
`PublicApi.txt`. Nothing a caller can write stops compiling — `someSumf.Stringize()` still resolves,
inherited from `Entity` — and nothing already compiled stops running either.

That second half was measured rather than assumed, because it is the kind of claim that reads
plausibly in both directions. A consumer calling `((Entity.Sumf)e).Stringize()` was compiled against
a build of 2.3.0, then run unchanged against a build of this branch: it prints what it printed
before. Reading its metadata says why — C# binds a virtual call to the type that *declares* the
method, so the emitted reference is `AngouriMath.Entity::Stringize()` whatever the static type of
the receiver, and no consumer ever names `Entity+Sumf::Stringize()` at all. The removal is
invisible except to reflection that asks a node type for its own declared members.

Devirtualising `Stringize()` cannot orphan an override outside the library either: `Entity` already
carried five `internal` or `private protected` abstract members (`Priority`, `SortHashName`,
`IntrinsicCondition`, `ToSymPy`, `InvertNode`), so no assembly but this one has ever been able to
derive a node from it.

### `Compile` works in a trimmed or NativeAOT application

The Linq compilation path found the method for each node by name —
`typeof(MathAllMethods).GetMethod(name, ...)` for the mathematical functions,
`expr.Type.GetMethod("IsNaN")` for the NaN test — and left the operators and conversions between
`Complex`, `BigInteger` and the primitives to `Expression.Add` and `Expression.Convert`, which find
them by reflecting over the operand type. A name resolved at run time is invisible to the trimmer,
so the members were removed and the lookups came back empty. All four are now tables of members
named at compile time: `MathAllMethods.Definitions` generated beside the methods it dispatches to,
and `nanChecks`, `operators` and `conversionOperators` in `CompilationProtocol`.

Nothing changes under the JIT — the same `MethodInfo` reaches the same `Expression` node — and the
44 assertions of `Sources/Tests/AotSmokeTest` give byte-identical output under the JIT, a trimmed
publish and a NativeAOT publish. What changed is that the second and third of those now run at all:
before this, six of them threw and the seventh aborted the process.

`AngouriMath` is marked `IsAotCompatible` for `net8.0` and later as a result, which is also the
silent half of the entry above: a trimmer reads that as permission to remove unused code from
inside the assembly, so an app that trims will now get a smaller `AngouriMath` rather than all of
it. That is the point of the mark, and
[`Docs/Contributing/Trimming.md`](Sources/AngouriMath/Docs/Contributing/Trimming.md) says what
keeps it true.

[#363](https://github.com/asc-community/AngouriMath/issues/363),
[#746](https://github.com/asc-community/AngouriMath/issues/746) item 79.
| **Silent** | `"derivative(y, x) + y - x".SolveEquation("y")`, and the same equation written `= 0` | `{ x }`, which is not a root of it | `{ y : derivative(y, x) + y - x = 0 }` |
| **Silent** | `"integral(y, x) + y - x".SolveEquation("y")` | `{ -(C + -x) / (x + 1) }` | `{ y : integral(y, x) + y - x = 0 }` |
| **Silent** | `"limit(y, x, 0) + y - x".SolveEquation("y")` | `{ x / 2 }` | `{ y : limit(y, x, 0) + y - x = 0 }` |
| **Silent** | `"sum(y, k, 1, 3) + y - k".SolveEquation("y")` | `{ k / 4 }` | `{ y : sum(y, k, 1, 3) + y - k = 0 }` |
| **Silent** | `"{ y : derivative(y, x) + y - x = 0 }".ToEntity().Simplify()` | `{ y : y - x = 0 }`, a different set | unchanged |

### An equation whose unknown stands under a derivative is left unsolved

`"derivative(y, x) + y - x".SolveEquation("y")` answered `{ x }`. Substituting that back —
with this library's own `Substitute` — gives `derivative(x, x) + x - x`, which is `1`. The set
named a member that is not a root.

The derivative went to zero because `y` is not `x`, and every calculus operator does the same:
`limit(y, x, 0)` is `y`, `integral(y, x)` is `x * y + C`, `sum(y, k, 1, 3)` is `3 * y`. Each is a
decision about the *name* `y`, and the root the solver then returns says that name stands for an
expression in `x` — so the answer denies the step that produced it. Every one of the operators
was affected, and so was a quadratic in the unknown: `derivative(y, x) + y ^ 2 - x` answered
`{ sqrt(x), -sqrt(x) }`, which leave `x ^ (-1/2) / 2` and `-1/2 * x ^ (-1/2)`.

The equation is not thereby unsatisfiable, so the empty set would replace one false claim with
another. What holds is the condition as written, and that is what comes back. Solving it needs a
differential-equation solver, which
[#746](https://github.com/asc-community/AngouriMath/issues/746) has as item 48 and which this
library does not yet have.

A root that does not mention the name the operator is taken over denies nothing, and is returned
as before: `"derivative(y * x, x) + y - 1".SolveEquation("y")` is `{ 1/2 }`, and
`"derivative(y ^ 2, y) - 2".SolveEquation("y")` is `{ 1 }` — there the unknown *is* the name the
derivative binds, and no independence is claimed of it.

The set builder had the same reading of its own bound name and lost it the same way:
`{ y : derivative(y, x) + y - x = 0 }` simplified to `{ y : y - x = 0 }`, which is `{ x }`. A
binder over `y` makes `y` range over values, and expressions in `x` are among them, so that
simplification settles a condition that was written to stay open. It no longer fires.

[#964](https://github.com/asc-community/AngouriMath/issues/964)

---
| | `Entity.Set.SpecialSet.Create("NN")`, and any domain name this library does not have | `AngouriBugException` | `UnrecognizedDomainException: Unrecognized domain NN` |
| | `Entity.Set.SpecialSet.Create(Domain.Any)`, and any `Domain` that is not one of the five sets | `AngouriBugException` | `NotSufficientlySupportedException: There is no special set for domain Any` |

### An unknown domain is the caller's input, not a library defect

`SpecialSet.Create(string)` and `SpecialSet.Create(Domain)` are both `public`. Given a name or a
`Domain` value they do not know, both threw `AngouriBugException`, whose message ends *"please report
about it to the official repository"* — so a caller who wrote `"NN"` was told their own typo was a
defect in this library and asked to file it.

Both are now the caller's error, and both stay under `AngouriMathBaseException`, so a `catch` for
that is unaffected. Measured on a build of each side:

| input | 2.3.0 | now |
|---|---|---|
| `Create("NN")` | `AngouriBugException: The given domain is not presented in those possible …please report about it to the official repository` | `UnrecognizedDomainException: Unrecognized domain NN` |
| `Create(Domain.Any)` | the same | `NotSufficientlySupportedException: There is no special set for domain Any` |
| `Create((Domain)99)` | the same | `NotSufficientlySupportedException: There is no special set for domain 99` |
| `Create("RR")` | `RR` | `RR` |

`UnrecognizedDomainException` has existed since it was written and nothing threw it;
[`Docs/Usage/Exceptions.md`](Sources/AngouriMath/Docs/Usage/Exceptions.md) is where the difference
between the three types is written down. `Domain.Any` is a documented member of the enum meaning *no
restriction*, which is not a set this library has a node for — `Domains.IsWithinDomain` answers it
before ever reaching `Create`, so nothing inside the library was affected.

If you catch `AngouriBugException` around a call that builds a set from a name you did not choose,
catch `MathSException` — or its parent — instead.

## 2.3.0 — since 2.2.0

### At a glance

| Silent? | What | Was | Is |
|---|---|---|---|
| | `(Entity.Number)someBigInteger` | `FormatException: Illegal character found`, for almost every value | the number |
| **Silent** | `"1/x".Integrate("x")`, and every antiderivative with a logarithm | `ln(abs(x)) + C` | `ln(x) + C` |
| **Silent** | `CostModel.FewestDivisions.Cost(y ^ (1 * (-1)) * x)` | `0.007`, cheaper than `x / y` | `1.007` |
| **Silent** | `Real.NaN > (Real)1`, and the other three operators | `true` | `false` |
| **Silent** | a rewritten expression under `domain(...)` — including via `Substitute` | the constraint was dropped, so it answered | it refuses, as it did before the rewrite |
| **Silent** | `"x!".Differentiate("x")`, and anything containing it | `NaN` | the unevaluated `derivative(x!, x)` |
| **Silent** | `"7/2".ToEntity()` and any quotient of two integer literals | a `Divf` | the `Rational` it denotes |
| **Silent** | `sum(i, i, 1, 10)`, and every binder given `i` as the name it binds | the imaginary unit in the name position, so nothing was bound | `i` is the bound name, and `55` |
| **Silent** | `sum(2i, i, 1, 3)` | `6i` | `12` |
| **Silent** | `derivative(i ^ 2, i)` | `0` | `2 * i_1` |
| **Silent** | `{ i : i > 0 }` | `NaN` | the set it describes |
| **Silent** | `limit(i, i, 0)` | unevaluated | `0` |
| **Silent** | `integral(i, i)` | `-1/2 + C` | `i_1 ^ 2 / 2 + C` |
| | `lambda(i, i + 1)` | `InvalidArgumentParseException` | the lambda |
| **Silent** | `MathS.ToSympyCode` for any set, lambda, piecewise or non-vector matrix | Python that does not run — `NameError`, `SyntaxError`, `TypeError`, or an exception out of the exporter | Python that runs |
| **Silent** | `"{ k : k > 0 }".ToEntity().FreeVariables` — and `Vars`, and `VarsAndConsts` | `{ %1 }`, a name in no expression | `{ }`, `{ k }`, `{ k }` |
| **Silent** | the symbolic determinant of a matrix, substituted where a pivot vanishes — `[[0,1,2],[3,4,5],[6,7,8]]` through `[[a,b,c],[d,e,f],[g,h,i]]` | `NaN` | `0` |
| **Silent** | `((Entity.Matrix)"[[x, 1], [2, y]]".ToEntity()).Determinant.Simplify()` | `x * y - 2 provided not x = 0` | `x * y - 2` |
| **Silent** | `[1, 2] in RR`, and a matrix or a finite set against any of `BB`, `ZZ`, `QQ`, `RR`, `CC` | `True` | `False` |
| **Silent** | `"sin(pi)".ToEntity().Differentiate(MathS.pi)`, and every derivative over `pi` or `e` | `-1`, the chain rule run over a symbol that cannot change | `0` |
| **Silent** | `"x ^ 3".ToEntity().Differentiate(x, 2)`, and every `Differentiate(x, n)` with `n >= 1` | `(0 * x ^ 2 + 2 * x ^ 1 * 1 * 3) * 1 + 0 * 3 * x ^ 2` | `2 * x * 3` |
| **Silent** | `derivative(e ^ 2, e)`, `derivative(pi ^ 2, pi)` | `0` | `2 * e_1`, `2 * pi_1` |
| **Silent** | `{ e : e > 0 }` | `{ e : True }` | the set it describes |
| **Silent** | `limit(e, e, 0).Evaled` | `2.718…` | `0` |
| **Silent** | `integral(e, e, 0, 1).Evaled` | `3.694…` | `1/2` |
| **Silent** | `integral(arccos(0), pi, 0, 1)` | `1/4` | `pi / 2` |
| **Silent** | `derivative(arccos(0) * pi, pi)` | `0` | `pi / 2` |
| **Silent** | `sum(ln(x), e, 1, 2)`, and any binder over `e` around a logarithm | `log(1, x) + log(2, x)` | `2 * ln(x)` |
| **Silent** | `"sum(pi, pi, 1, 3)".ToEntity().Vars` | empty — the index was read as a constant | `pi` |
| | `MathS.pi.GetType()` | `Entity.Variable` | `Entity.Constant`, which derives from it |
| **Silent** | `derivative(x ^ 3, 3)`, and any derivative or integral over a number | `ln(x) * x ^ 3` | the unevaluated `derivative(x ^ 3, 3)` |
| **Silent** | `derivative(x ^ 2, x + 1)`, over a subexpression that does not occur | `0` | `2 * x`, by change of variables |
| **Silent** | `derivative(x * (x + 1), x + 1)`, where the rename left a variable behind | `x` | `1 + 2 * x` |
| **Silent** | `derivative(x ^ 2, x * y)`, over several variables at once | `0` | the unevaluated derivative |
| **Silent** | `derivative(x ^ 2 + y ^ 2, [x, y])` | `0` | `[2 * x, 2 * y]`, the gradient |
| **Silent** | `integral(x, [x, y]T)` | `[[C + x ^ 2, C + x * y]]` | `[[x ^ 2 / 2 + C, x * y + C]]` |
| **Silent** | `derivative(e ^ 2, e)`, over a named constant | `0` | `2 * e` |

### `ToSympyCode` emits Python that runs

`MathS.ToSympyCode` is documented as producing code you can run in SymPy. For every set, every
lambda, every `piecewise` and every non-vector matrix, it did not — and the failure was **silent**
in the sense that matters here: the string came back looking like Python.

The preamble is `import sympy` and nothing else, so every SymPy name in the body has to be
qualified. Six exports were not, and a seventh threw before it got that far:

```python
expr = FiniteSet(1, 2)                        # was: NameError: name 'FiniteSet' is not defined
expr = Interval(0, 1, ...)                    # was: NameError: name 'Interval' is not defined
expr = Union(FiniteSet(1, 2), FiniteSet(3))   # was: NameError: name 'Union' is not defined
expr = x in S.Reals                           # was: NameError: name 'S' is not defined
expr = sympy.Lambda(x, )                      # was: TypeError: missing 1 required positional argument
```

| input | was | now |
|---|---|---|
| `{ 1, 2 }` | `FiniteSet(1, 2)` | `sympy.FiniteSet(1, 2)` |
| `ZZ` | `S.Integers` | `sympy.S.Integers` |
| `{1,2} \/ {3}` | `Union(FiniteSet(1, 2), FiniteSet(3))` | `sympy.Union(sympy.FiniteSet(1, 2), sympy.FiniteSet(3))` |
| `lambda(x, sin(x) + 1)` | `sympy.Lambda(x, )` | `sympy.Lambda(x, sympy.sin(x) + 1)` |
| `{ x : x > 0 }` | `AngouriBugException` out of the exporter | `sympy.ConditionSet(x, x > 0, sympy.S.UniversalSet)` |
| `[sin(a); b]` | `Interval(sin(a), b, ...)` | `sympy.Interval(sympy.sin(a), b, ...)` |
| `piecewise(sin(x) provided x > 0, …)` | `sympy.Piecewise((sin(x), x > 0), …)` | `sympy.Piecewise((sympy.sin(x), x > 0), …)` |
| `[[sin(a), b], [c, d]]` | `sympy.ImmutableMatrix([[sin(a), b], …])` | `sympy.ImmutableMatrix([[sympy.sin(a), b], …])` |
| `x in RR` | `x in S.Reals` | `(sympy.S.Reals).contains(x)` |

Three separate faults, all of them invisible to the tests that were there:

- **Unqualified names.** `import sympy` binds `sympy`, not `FiniteSet` or `S`.
- **Parts interpolated rather than exported.** `Interval`, `Piecewise` and the non-vector `Matrix`
  wrote their children with `{Left}` instead of `{Left.ToSymPy()}`. A bare variable spells the same
  in both languages, so it only shows once the part is a function — `sin(a)`, which Python does not
  have.
- **A set builder threw.** `ConditionalSet.Codomain` is `Domain.Any`, which `SpecialSet.Create` has
  no member for, so the exporter's cast raised `AngouriBugException` — "please report about it to
  the official repository", which is [#985](https://github.com/asc-community/AngouriMath/issues/985).
  SymPy names that set: `S.UniversalSet`, which is the one it prints `ConditionSet` *without* a
  third argument for.

`x in RR` also changes shape rather than just qualification. Python's `in` coerces its result to a
`bool`, and a membership that is not decided is not one: `x in sympy.S.Reals` raises
`TypeError: did not evaluate to a bool: (-oo < x) & (x < oo)`. `.contains` answers with the
condition, and still answers `True` or `False` where it can.

**Why the tests passed.** They asserted substring containment — `Assert.Contains("Piecewise((a, b),
(c, d))")` — which holds whether the parts were exported or interpolated, and holds on a program
that does not run at all. The new cases pin the whole emitted expression, and one of them asserts
every SymPy name carries its qualifier.

Measured with `work/sympycheck`, which executes the generated program rather than reading it: of 45
emitted programs the corpus now covers, **43 run** and 0 return an inexact value. The two that do
not are the set builders, whose preamble still declares the placeholder that
[#989](https://github.com/asc-community/AngouriMath/issues/989) is about — `%1 = sympy.Symbol('%1')`
is a `SyntaxError` whatever the body says. Their `expr` line is correct here, and with #989's fix
in as well, 45 of 45 run. Suite 7385 passed, 0 failed; corpus unchanged at 116/119 with 0 wrong.
[#985](https://github.com/asc-community/AngouriMath/issues/985).

### A set builder's internal placeholder no longer escapes into `Vars` or `FreeVariables`

`ConditionalSet.DirectChildren` is its predicate with the bound name renamed to a fresh one, so that
two builders differing only in that name compare and hash alike — `{ x : x > 0 } = { y : y > 0 }`
is `True`, and stays `True`. Every property that reports names by walking `DirectChildren` picked
that invented name up and returned it.

```csharp
"{ k : k > 0 }".ToEntity().FreeVariables   // was: { %1 }   now: { }
"{ k : k > 0 }".ToEntity().Vars            // was: { %1 }   now: { k }
"{ k : k > a }".ToEntity().FreeVariables   // was: { %1, a }      now: { a }
"{ k : k > a }".ToEntity().Vars            // was: { %1, a }      now: { k, a }
"x + { k : k > a }".ToEntity().Vars        // was: { x, %1, a }   now: { x, k, a }
```

`%1` is neither answer on anybody's definition. It is not the bound name, it is not in the
expression the caller wrote, `Variable.CreateTemp` invents a different one for a different
predicate, and it cannot be typed — the parser has no `%`. It broke `Vars`'s own promise too, which
is the variables that *occur*: `k` occurs and was missing, `%1` does not occur and was there.

A set builder binds the name it declares exactly as a lambda binds its parameter, so all three
properties now answer for `{ k : ... }` what they already answered for `lambda(k, ...)`:

| | `lambda(k, k > a)` | `{ k : k > a }` was | `{ k : k > a }` now |
|---|---|---|---|
| `FreeVariables` | `{ a }` | `{ %1, a }` | `{ a }` |
| `Vars` | `{ k, a }` | `{ %1, a }` | `{ k, a }` |

Only the reporting changed. `DirectChildren` still publishes the renamed predicate, because that is
what makes two alpha-equivalent builders equal, and `Replace` and `Substitute` never used it — both
override and work from `Var` and `Predicate` directly.

This does **not** answer the second half of [#989](https://github.com/asc-community/AngouriMath/issues/989),
which asks whether `sum`, `integral`, `limit` and `derivative` should bind their variable here too.
That is a documented choice rather than a leak, and it is still open.

Measured: suite 7376 passed, 0 failed; corpus unchanged at 116/119 with 0 wrong, and no case's
verdict or answer altered. [#989](https://github.com/asc-community/AngouriMath/issues/989).

### The symbolic determinant is a polynomial, not a quotient by its pivots

`Matrix.Determinant` was computed by Gaussian elimination, which leaves the pivots as literal
divisions. The expression it returned was therefore undefined wherever a pivot vanishes — at points
where the determinant itself is perfectly well defined.

```csharp
var m = (Entity.Matrix)"[[x, 1], [2, y]]".ToEntity();
m.Determinant             // was: x * (y * x + -2) / x           now: x * y + -2
m.Determinant.Simplify()  // was: x * y - 2 provided not x = 0   now: x * y - 2
m.Determinant.Substitute("x", 0).Substitute("y", 5).Evaled   // was: NaN   now: -2
```

`x = 0` is an ordinary point of that matrix — its determinant there is `-2` — and `NaN` asserts the
value **does not exist**. At 3×3 it stops being an edge case, because the denominator is
`a ^ 4 * (a * e - b * d)` and so two conditions have to miss:

```csharp
// [[a, b, c], [d, e, f], [g, h, i]].Determinant, substituted:
// [[0, 1, 2], [3, 4, 5], [6, 7, 8]]     was: NaN   now: 0     (the pivot a is 0)
// [[1, 2, 3], [2, 4, 6], [1, 1, 1]]     was: NaN   now: 0     (a * e = b * d)
// [[1, 2, 3], [4, 5, 6], [7, 8, 10]]    was: -3    now: -3
// [[2, 1, 0], [1, 2, 1], [0, 1, 2]]     was: 4     now: 4
```

Two of those four are ordinary matrices, and the first is the singular example every linear-algebra
course opens with. Both wrong answers were **silent**: the call succeeded and returned `NaN`, which
is exactly the answer a caller checking for singularity was looking for.

The determinant of a matrix over a commutative ring is a polynomial in its entries, so Laplace
expansion — which never divides — needs no condition at all. It is what the property's own
documentation and the comment above it already claimed was in use.

**This is not a performance trade.** Measured on the same machine, property only, both arms built
from source: Laplace returns a *smaller* expression at every size, and the elimination it replaces
was the slower of the two on numeric matrices by a wide margin.

| entries | Gaussian complexity | Laplace complexity | Gaussian, numeric | Laplace, numeric |
|---|---|---|---|---|
| 2×2 | 13 | 9 | | |
| 3×3 | 79 | 37 | | |
| 4×4 | 443 | 163 | | |
| 5×5 | 2461 | 833 | | |
| 6×6 | 13673 | 5021 | 164 ms | 126 ms |
| 7×7 | | 35173 | | |
| 8×8 | | | over 120 s | 358 ms |
| 10×10 | | | over 120 s | 4478 ms |

Laplace expansion is `O(n!)`, and a fully symbolic determinant has `n!` terms however it is
computed, so that is the size of the answer rather than an overhead. The practical ceiling on a
numeric matrix moves from 7×7 to 10×10; 11×11 does not return, where under the elimination 8×8
already did not. A fraction-free elimination (Bareiss) would raise it further and is worth having,
but it is a separate change and this one is not blocked on it.

Measured: the whole suite passes with the fix in, and the corpus is unchanged — 116/119 with 0
wrong, 0 error, 0 timeout, and no case's verdict or answer altered.
[#992](https://github.com/asc-community/AngouriMath/issues/992).

### A matrix is no longer a member of every special set at once

`SpecialSet.TryContains` decided membership by asking `MayContain`, which is deliberately permissive
about anything that is not a constant leaf — the right answer for a codomain guard, which has to let
through what it has not ruled out, and the wrong one for a membership decision.

```csharp
"[1, 2] in BB".ToEntity().Simplify()              // was: True    now: False
"[1, 2] in ZZ".ToEntity().Simplify()              // was: True    now: False
"[1, 2] in RR".ToEntity().Simplify()              // was: True    now: False
"[1, 2] in (ZZ /\\ BB)".ToEntity().Simplify()     // was: True    now: False
"{ 1, 2 } in RR".ToEntity().Simplify()            // was: True    now: False
```

The old answers were mutually contradictory — `ZZ` and `BB` share no members, and the same matrix was
in both — so nothing could have depended on all of them. `MayContain` itself is unchanged, and so is
every answer for a number or a boolean: `3 in RR` is `True`, `i in RR` is `False`, `x in RR` is still
undecided rather than answered.

Measured: the whole suite passes with the fix in, and the corpus is unchanged at 116/119 with 0 wrong.
[#995](https://github.com/asc-community/AngouriMath/issues/995).

### Differentiating over a constant is `0`, not the chain rule over a symbol that cannot change

`Entity.Differentiate(Variable)` takes a `Variable`, and `MathS.pi` and `MathS.e` are ones — so they
could be handed to it, and it differentiated as though they varied.

```csharp
"pi ^ 2".ToEntity().Differentiate(MathS.pi)        // was: 2 * pi                now: 0
"sin(pi)".ToEntity().Differentiate(MathS.pi)       // was: -1                    now: 0
"x * pi".ToEntity().Differentiate(MathS.pi)        // was: x                     now: 0
"sin(x * pi)".ToEntity().Differentiate(MathS.pi)   // was: cos(x * pi) * x       now: 0
"x!".ToEntity().Differentiate(MathS.pi)            // was: derivative(x!, pi)    now: 0
"e ^ 2".ToEntity().Differentiate(MathS.e)          // was: 2 * e                 now: 0
"pi ^ 3".ToEntity().Differentiate(MathS.pi, 2)     // was: an unsimplified 0     now: 0
```

`sin(pi)` is `0`, and its derivative with respect to anything is `0`. `-1` is `cos(pi)` — the chain
rule run over a symbol that cannot change. `x!` is the case where the library cannot take the
derivative at all, and it is `0` here regardless, because the *variable* is what settles it.

An ordinary variable is untouched, including where a constant is present as a coefficient:
`"x ^ 2 * pi".Differentiate(x)` is `2 * x * pi` on both versions. The guard is about what is
differentiated **over**, not about what appears in the expression. `Differentiate(x, 0)` returns the
input and `Differentiate(x, n)` with `n < 0` integrates; neither changes.

**The test is whether the name evaluates to a number, not whether it is spelled like a constant.**
That is what makes this compatible with
[#984](https://github.com/asc-community/AngouriMath/issues/984): a name a *binder* declares can vary
even when it is spelled `pi`, and it evaluates to itself.

**The node form is a different question and belongs to #984.** On this version
`"derivative(x * pi, pi)".ToEntity().InnerSimplified` also goes from `x` to `0`, because the node
asks the same public method. Once #984's fix lands the parser *binds* `pi` there, so it becomes
`2 * pi_1` — a derivative over the variable the binder holds — and that is the intended answer, not
a regression of this one. The two compose: measured together, `sin(pi)` differentiated by `MathS.pi`
is `0` and `derivative(pi ^ 2, pi)` is `2 * pi_1`.

`Integrate` and `Limit` over a constant are **not** changed. They have no value to give — there is
nothing to integrate with respect to a thing that cannot vary — so leaving them unevaluated is the
existing "I could not settle this", not a refusal of something settled. The derivative *is* settled,
which is why it is answered rather than declined.

Measured: suite 7383 passed, 0 failed; corpus unchanged at 116/119 with 0 wrong, no case's verdict or
answer altered; crashcheck 1834 cases, 0 crashed.
[#993](https://github.com/asc-community/AngouriMath/issues/993).

### `Differentiate(x, n)` answers what `Differentiate(x)` n times answers

The two overloads reached different code. `Differentiate(Variable)` goes through the transformation,
which ends at `DifferentiateOnce` and simplifies; `Differentiate(Variable, int)` called
`InnerDifferentiate` straight in its loop, so nothing was ever simplified — and because each pass
differentiated the *unsimplified* result of the last, every `0 *` and `* 1` the chain rule produces
was still there to be differentiated again.

```csharp
var x = MathS.Var("x");
"x ^ 3".ToEntity().Differentiate(x, 1)   // was: 3 * x ^ 2 * 1                                   now: 3 * x ^ 2
"x ^ 3".ToEntity().Differentiate(x, 2)   // was: (0 * x ^ 2 + 2 * x ^ 1 * 1 * 3) * 1 + 0 * 3 * x ^ 2   now: 2 * x * 3
```

At `n = 3` it is no longer just untidy:

```
"x ^ 4".Differentiate(x, 3)
// was: (0 * x ^ 3 + 3 * x ^ 2 * 1 * 0 + ((0 * x ^ 2 + 2 * x ^ 1 * 1 * 3) * 1 + 0 * 3 * x ^ 2) * 4
//       + 0 * 3 * x ^ 2 * 1) * 1 + 0 * (0 * x ^ 3 + 3 * x ^ 2 * 1 * 4) + 0 * 4 * x ^ 3
//       + (0 * x ^ 3 + 3 * x ^ 2 * 1 * 4) * 0
// now: 2 * x * 3 * 4
```

The value was never wrong — `Differentiate(x, 3)` and differentiating three times agree numerically
on both versions — so this is a change of *form*, and it breaks anyone matching on the shape of the
result. It also means the cost grew with the accumulated mess rather than with the derivative.

`n = 0` (returns the input) and `n < 0` (integrates) are unchanged.

**Why the two were not simply merged.** `Derivativef`'s simplification decides whether a derivative
can be taken by asking for it and keeping the node when a `Derivativef` comes back — that test is
what terminates it. Routing it through an overload that simplifies each pass makes it simplify the
very node it is deciding about, arrive back at itself, and recurse: `derivative(x!, x, 2)` overflowed
the stack after 3214 frames. So the raw loop still exists as an internal `InnerDifferentiate(Variable,
int)`, which is what that caller uses, and only the public overload simplifies. There are cases for
both in `work/crashcheck`.

Measured: suite 7378 passed, 0 failed; corpus unchanged at 116/119 with 0 wrong, no case's verdict or
answer altered; crashcheck 1834 cases, 0 crashed.
[#1002](https://github.com/asc-community/AngouriMath/issues/1002).

### A rewritten node keeps its `Codomain`, so a domain constraint no longer disappears

`Entity.Replace` rebuilds every node on the path to a change, and a rebuilt node started from its
type's default codomain rather than the one the original carried. Any rewrite therefore dropped a
`domain(...)` annotation — including `Substitute`, which is built on `Replace`.

```csharp
"domain(sqrt(x), ZZ)".ToEntity().Substitute("x", "4/9".ToEntity()).EvalNumerical()
// was: 2/3     now: NaN        — 2/3 is not an integer, so the constraint refuses it
```

The old behaviour was **silent** and one-sided: constraints were only ever weakened, never
strengthened, so a rewrite could make an undefined expression look defined but never the reverse.
An expression that answered a value where it should have refused now refuses.

Measured: the whole suite passes unchanged with the fix in, so nothing in the library depended on
the annotation being lost. [#955](https://github.com/asc-community/AngouriMath/issues/955).

### Differentiating a factorial declines instead of answering `NaN`

`d/dx x!` answered `NaN`, which asserts the derivative **does not exist**. It does — `x!` is
`Γ(x + 1)`, the library already evaluates `(1/2)!` as `0.886…`, and `d/dx x!` at 3 is an ordinary
`Γ'(4) ≈ 3.966`. It now returns the unevaluated derivative, which is what every other node the
library cannot differentiate already does.

```csharp
"x!".Differentiate("x")           // was: NaN     now: derivative(x!, x)
"x! + x^2".Differentiate("x")     // was: NaN     now: derivative(x!, x) + 2 * x
```

The second line is why it mattered: `NaN` propagates, so a single factorial destroyed an otherwise
fine derivative. Anything testing `== MathS.NaN` to detect "cannot differentiate" should look for an
`Entity.Derivativef` in the result instead.

A limit that reaches a factorial may now be **refused where it previously answered `NaN`** — the same
change of spelling, one level up.

Computing the derivative properly needs the digamma function and remains
[#171](https://github.com/asc-community/AngouriMath/issues/171).
[#958](https://github.com/asc-community/AngouriMath/issues/958).
### A quotient of two integer literals parses as a `Rational`

`"7/2".ToEntity()` was a `Divf(7, 2)`; it is now the `Rational` 7/2. The value was always the same
and the node was not, so a `Rational` did not survive being printed and read back:

```csharp
Entity original = "3.5".ToEntity();   // Rational, 7/2
original == original.Stringize().ToEntity();   // was: false     now: true
```

`Entity` equality is structural, so anything relying on that round trip — printing to text and
parsing back, which is the natural way to serialize — was quietly getting a different tree.
[#873](https://github.com/asc-community/AngouriMath/issues/873).

**A quotient that reduces to an integer is unchanged.** `"4/2".ToEntity()` is still a `Divf`, not
`2`. Parsing is not simplification, and turning it into `2` would discard what the caller wrote;
`4/2` also already round-tripped, being a `Divf` before and after. Only the non-integer case is one
a `Rational` can print as.

What changes for a caller: `"1/2".ToEntity()` now prints `1/2` rather than `1 / 2`, since `Rational`
and `Divf` space differently, and a pattern match on `Divf` no longer sees a parsed integer
quotient. Matching `Rational` catches both spellings, and `InnerSimplified` already produced a
`Rational` here, so code that simplified before matching is unaffected.

**`Complex` has the same defect and is not fixed here.** A constructed `Complex` prints `1 + 2i` and
parses back as a `Sumf`, or a `Minusf` for `3 - 4i`. It is tracked on the same issue.

### `Real`'s comparison operators refuse `NaN` instead of ordering it

`Real`'s `>`, `>=`, `<` and `<=` ordered `NaN` above every number, so `NaN > 1` was `true` and
`1 < NaN` was `true` as well. That is `EDecimal`'s total order showing through, and it is not what
`double` does, where every comparison against `NaN` is false in both directions.

```csharp
Real one = 1;
Real.NaN > one     // was: true     now: false
Real.NaN >= one    // was: true     now: false
one < Real.NaN     // was: true     now: false
Real.NaN <= one    // was: false    now: false
```

The failure it caused is one-sided and unsafe: a guard written as `if (value > threshold)` treated
an undefined value as *exceeding* the threshold. Of the two possible defaults that is the worse one,
and this library's stated position is that answering wrongly is worse than not answering.

**`CompareTo` is unchanged and still orders `NaN` above every number.** That is deliberate, not an
inconsistency left behind: sorting requires a total order — `Array.Sort` may loop or throw without
one — while an operator does not. "Where does this sort" and "is this greater" are different
questions, and only the second has no answer for a value that is not a number. Anything relying on
the old operator behaviour to sort should call `CompareTo`, which never changed.

[#947](https://github.com/asc-community/AngouriMath/issues/947).

### `FewestDivisions` and `FewestRadicals` score by value, not by spelling

Both tested the exponent's **node**: `Powf(_, Real { IsNegative: true })` and
`Powf(_, Rational and not Integer)`. In `y ^ (1 * (-1))` the exponent is a `Mulf`, so neither
fired, and the writing whose division the model could not see scored *cheapest* — under the
criterion whose whole job is to remove divisions.

| expression | `FewestDivisions` was | is |
|---|--:|--:|
| `x / y` | 1.003 | 1.003 |
| `y ^ (-1) * x` | 1.005 | 1.005 |
| `y ^ (1 * (-1)) * x` | **0.007** | **1.007** |

All three are one value written three ways, so one division each is the answer, and the written
form — being the smallest tree — is now correctly the cheapest. `FewestRadicals` had the same shape
of test and so the same blind spot: `x ^ (2 ^ (-1))` is a square root and is now counted as one.

**`Simplify`'s output does not change.** Measured on a build of each, twelve division-heavy inputs
under both models, twenty-four results, byte-identical. `Simplify` scores candidates it has already
evaluated, so it did not reach the gap; what reaches it is a caller scoring expressions it did not
build — extraction on an e-graph, where every writing of a value is a member of one class at once,
which is how this was found. Only `Cost` returns different numbers, and only for an expression with
an unevaluated exponent.

`Default` and `SmallestTree` are deliberately **unchanged**. They count how an expression is
written, and `y ^ (1 * (-1)) * x` really is a bigger tree — for them the node as written is not an
approximation of the question, it is the question.
[#950](https://github.com/asc-community/AngouriMath/issues/950).

### An antiderivative with a logarithm drops the absolute value unless the codomain is real

`abs` is not holomorphic, so `ln(abs(f))` is an antiderivative of `f'/f` **on the real line and
nowhere else** — differentiating it off the line does not return the integrand. The table produced
it unconditionally, including under the default codomain, which is the complex plane. That is
[#946](https://github.com/asc-community/AngouriMath/issues/946), and separating by codomain is the
answer given on the issue.

```csharp
"1/x".Integrate("x")        // was: ln(abs(x)) + C        now: ln(x) + C
"tan(x)".Integrate("x")     // was: -ln(abs(cos(x))) + C  now: -ln(cos(x)) + C
```

The previous answer is still available, and is now a statement about where you are working rather
than the only thing on offer:

```csharp
using var _ = MathS.Settings.Codomain.Set(Domain.Real);
"1/x".Integrate("x")        // ln(abs(x)) + C, as before
```

**Silent**: the call still succeeds and returns a different expression. If you were integrating on
the reals and relying on the default, set the codomain and nothing changes.

Eleven rules in the table introduced the absolute value themselves and all eleven now ask. The rule
for `∫ ln(abs(ax + b)) dx` is deliberately **not** among them: there the absolute value comes from
the integrand the caller wrote, not from the rule, so it is left where it is.

### A `BigInteger` converts to a `Number` instead of throwing

`Entity` and `Entity.Number` both offer an implicit conversion from
`System.Numerics.BigInteger`, and they did not agree. `Entity`'s read the two's-complement bytes;
`Number`'s passed the same bytes to `EInteger.FromString`, whose `byte[]` overload reads them as
**ASCII digits**:

```csharp
Entity        fine  = new BigInteger(123456789);   // 123456789
Entity.Number threw = new BigInteger(123456789);   // FormatException: Illegal character found
```

So the conversion failed for every value whose bytes are not digit characters — which is nearly all
of them, `1` included. The few that worked did so by accident: `12594` is the two bytes `'2'` and
`'1'`, and came out as `21`.

Nothing in the library reached it, which is why no test caught it; it is only on the public surface.
Both conversions now read the bytes, and a test requires the two to agree rather than merely to
work.

Found while documenting the members a `#pragma warning disable CS1591` was covering — the two
conversions sit in different files and had to be read side by side to look wrong.
[#585](https://github.com/asc-community/AngouriMath/issues/585).

### A binder given `i` as the name it binds reads it as that name

`i` is the imaginary unit, and the lexer decides it — `NUMBER: ... | 'i'` — so it never reaches the
rule that makes variables and could not be a bound name anywhere in the language. Every binder
handed one therefore did something other than bind: a sum bound nothing and stayed unevaluated, a
set builder answered `NaN`, a lambda threw. Naming `i` is now read as the declaration it plainly
is, throughout that binder and nowhere else.

```csharp
"sum(i, i, 1, 10)".ToEntity().Simplify()      // was: unevaluated       now: 55
"sum(2i, i, 1, 3)".ToEntity().Simplify()      // was: 6i                now: 12
"integral(i, i)".ToEntity().Simplify()        // was: -1/2 + C          now: i_1 ^ 2 / 2 + C
"limit(i, i, 0)".ToEntity().Simplify()        // was: unevaluated       now: 0
"derivative(i ^ 2, i)".ToEntity().Simplify()  // was: 0                 now: 2 * i_1
"{ i : i > 0 }".ToEntity().Simplify()         // was: NaN               now: { i : i > 0 }
"lambda(i, i + 1)".ToEntity()                 // was: threw             now: the lambda
```

`2i` is one token, so a written coefficient on the bound name arrives as a single number with
nothing in it to rename; under a binder that names `i` it is read as the product it would be with
any other name, which is what turns `6i` into `12` above.

**Only inside the binder that declares it**, which is what makes this a fix and not a new defect:

```csharp
"sum(i * k, k, 1, 3)".ToEntity().Simplify()          // 6i, unchanged
"sum(i, i, 1, 3) + i".ToEntity().Simplify()          // 6 + i, unchanged
"sum(sqrt(-1) * i, i, 1, 10)".ToEntity().Simplify()  // 55i — the sum's index, times the unit
```

The last is what SymPy answers for `Sum(sqrt(-1) * i, (i, 1, 10))`, which resolves the same
collision by naming the constant `I` and refusing to bind it.

`e` and `pi` needed nothing here, being variables that carried a value — and were not fixed by it
either, for the same reason. The entry below does that.

[#976](https://github.com/asc-community/AngouriMath/issues/976).
### A derivative or an integral over something that is not a variable

Differentiating with respect to a *subexpression* is a feature
([#230](https://github.com/asc-community/AngouriMath/issues/230)): the subexpression is given a
name and the derivative is taken over the name, so `derivative((x + 1) ^ 2, x + 1)` is `2 * (x + 1)`.
The rename has two premises that were not checked. The subexpression has to be able to **vary** —
a number in that position was renamed and differentiated over, which answers a question with no
meaning:

```csharp
"derivative(x ^ 3, 3)".ToEntity().Simplify()   // was: ln(x) * x ^ 3       now: unevaluated
"integral(x ^ 2, 2)".ToEntity().Simplify()     // was: x ^ 2 / ln(x) + C   now: unevaluated
```

And the rename has to be **exact**, which means nothing of the subexpression's own variables may be
left behind. Where something is, the leftovers were read as independent of the name they came from:

```csharp
"derivative(x * (x + 1), x + 1)".ToEntity().Simplify()  // was: x   now: 1 + 2 * x
```

`x` is what you get by renaming `x + 1` to `z` and reading the other `x` as a constant. With
`z = x + 1` the expression is `(z - 1) * z`, whose derivative is `2z - 1`, which is `2x + 1`.

**Where the rename is not exact, this is a change of variables — and a change of variables needs no
occurrence at all.** `d f / d g` is `(df/dx) / (dg/dx)`, which is the substitution without having to
invert `g`:

```csharp
"derivative(x ^ 2, x + 1)".ToEntity().Simplify()   // was: 0   now: 2 * x
"derivative(x ^ 2, 2 * x)".ToEntity().Simplify()   // was: 0   now: x
"derivative(x ^ 2, sin(x))".ToEntity().Simplify()  // was: 0   now: 2 * x / cos(x)
"integral(x ^ 2, x + 1)".ToEntity().Simplify()     // was: 0   now: x ^ 3 / 3 + C
```

What still has no answer is a subexpression of **several** variables that the rename cannot take
exactly: `d(x + y)/d(x * y)` is `1/y` through `x` and `1/x` through `y`, so there is nothing to
answer without a direction. Those are the unevaluated node — the library's way of saying it could
not settle the question — rather than `0`.

A definite integral over a subexpression is unchanged: its range is stated in the new variable, and
converting it needs `g` inverted, which is a separate question.

**A vector in that position is now the gradient.** The elementwise broadcast every binder already
has was unreachable, because the rename matched first and answered `0`:

```csharp
"derivative(x ^ 2 + y ^ 2, [x, y])".ToEntity().Simplify()   // was: 0   now: [2 * x, 2 * y]
"integral(x, [x, y]T)".ToEntity().Simplify()
// was: [[C + x ^ 2, C + x * y]]      the first component is not the integral of x
// now: [[x ^ 2 / 2 + C, x * y + C]]
```

This is componentwise and no more than that: `derivative([x * y, x + y], [x, y])` pairs index with
index and is `[y, 1]`, the diagonal of the Jacobian rather than the Jacobian. What shape a full
Jacobian or Hessian should take is a convention this does not choose.

**A named constant can be differentiated over.** `derivative(e ^ 2, e)` was `0` — `e` is a variable
carrying a value, the value arrived in the variable position, and a number there took the rename
path. It is `2 * e`, and `derivative(pi ^ 2, pi)` is `2 * pi`. Two of the four defects recorded in
[#984](https://github.com/asc-community/AngouriMath/issues/984) go with it; the set builder and
`limit(e, e, 0).Evaled` remain.

[#964](https://github.com/asc-community/AngouriMath/issues/964).

### A name a binder declares is a variable, including `pi` and `e`

A mathematical constant used to be a `Variable` whose *name* was looked up in a table, so a `pi` a
binder declared and the constant `pi` were one object and nothing downstream could tell them apart.
It is now `Entity.Constant`, a node — so a binder holds a variable while the rest of the language
holds the constant, and every binder-based operation reads the same thing.

```csharp
"derivative(e ^ 2, e)".ToEntity().Simplify()   // was: 0             now: 2 * e_1
"derivative(pi ^ 2, pi)".ToEntity().Simplify() // was: 0             now: 2 * pi_1
"{ e : e > 0 }".ToEntity().Simplify()          // was: { e : True }  now: { e : e > 0 }
"limit(e, e, 0)".ToEntity().Evaled             // was: 2.718…        now: 0
"integral(e, e, 0, 1)".ToEntity().Evaled       // was: 3.694…        now: 1/2
```

**`pi_1`, not `pi`.** Most binders consume the name they declare — a sum over `pi` answers a number,
a set builder keeps it inside itself — and those print as they were written and read back as
themselves. A derivative and an indefinite integral *return* it, and a variable called `pi` is one
the parser cannot produce, so `2 * pi` would read back as twice the constant. Renaming a bound
variable is free, so it is renamed to a name that can be written. **This applies to `i` as well**,
and changes the two answers the entry above introduced: `derivative(i ^ 2, i)` is `2 * i_1` and
`integral(i, i)` is `i_1 ^ 2 / 2 + C`. Every binder over `pi`, `e` or `i` now answers something that
parses back to itself; an ordinary name is never renamed.

Differentiation compares the node rather than its name, so a constant that simplification
**produces** inside a binder over that name is no longer differentiated as though it were the index:

```csharp
"derivative(arccos(0) * pi, pi)".ToEntity().Simplify()  // was: 0                     now: pi / 2
"derivative(arccos(0) * q, q)".ToEntity().Simplify()    // pi / 2, unchanged — the same answer now
"derivative(ln(x) * e, e)".ToEntity().Simplify()        // was: 0 provided not x = 0  now: ln(x) provided not x = 0
```

A constant that simplification **produces** inside a binder over its name is no longer caught by it,
which was a wrong answer of a different kind — `arccos(0)` is `pi / 2`, and integrating it over a
name spelled `pi` integrated that:

```csharp
"integral(arccos(0), pi, 0, 1)".ToEntity().Simplify()  // was: 1/4   now: pi / 2
"integral(arccos(0), q, 0, 1)".ToEntity().Simplify()   // pi / 2, unchanged — the same answer now
```

`ln` and `exp` are written over Euler's number in their own definitions — `Ln(a)` is `Logf(e, a)`,
and `exp(x)` is `e ^ x` — and that occurrence is the value rather than a mention of the name, so no
binder reaches it:

```csharp
"sum(ln(x), e, 1, 2)".ToEntity().Simplify()   // was: log(1, x) + log(2, x)   now: 2 * ln(x)
"sum(exp(x), e, 1, 2)".ToEntity().Simplify()  // was: 1 + 2 ^ x               now: 2 * e ^ x
"sum(ln(e), e, 1, 1)".ToEntity().Simplify()   // was: NaN, being log(1, 1)    now: 0
```

Substituting **for the constant** still reaches that base, and unchanged — `MathS.Ln("x").Substitute("e", 3)`
is `log(3, x)` before and after. A binder binds *occurrences* and a substitution replaces a *value*,
and the base of a logarithm is Euler's number by value however it got there, so replacing Euler's
number by 3 replaces it there too. `ln(x).VarsAndConsts` has said `e, x` all along.

Where the writer does name `e`, it still binds, which is the same rule read the other way:

```csharp
"sum(log(e, x), e, 1, 2)".ToEntity().Simplify()  // log(1, x) + log(2, x)
"sum(e ^ x, e, 1, 2)".ToEntity().Simplify()      // 1 + 2 ^ x
```

**Two consequences to read for.** A bound name now counts as a variable, so `Vars` reports it:

```csharp
"sum(pi, pi, 1, 3)".ToEntity().Vars   // was: empty      now: pi
"pi * x".ToEntity().Vars              // x, unchanged — a free constant is still not a variable
```

And `MathS.pi` and `MathS.e` are `Entity.Constant` rather than `Entity.Variable`. They are still
*typed* `Variable` and `Constant` derives from it, so no signature changed and nothing that compiles
today stops compiling; but `MathS.Var("pi")` is a constant, and a `pi` a binder declares is no
longer equal to it. Substituting for the constant is unchanged where it was already right — a
binder's own name was out of reach before as well — and no longer reaches into a logarithm:

```csharp
"pi + x".ToEntity().Substitute(MathS.pi, 3)             // 3 + x, unchanged
"sum(pi, pi, 1, 3)".ToEntity().Substitute(MathS.pi, 3)  // unchanged before and after
```

Free `pi` and `e` are otherwise untouched: `sin(pi)` is `0`, `ln(e)` is `1`, `arccos(0)` is `pi / 2`,
and their numeric values are what they were.

[#984](https://github.com/asc-community/AngouriMath/issues/984).

---

## 2.2.0 — since 2.1.0

### At a glance

| Silent? | What | Was | Is |
|---|---|---|---|
| **silent** | `log(x, x)` simplified | `1 provided x > 0` — `NaN` at every negative `x`, and `1` at `x = 1` where the logarithm is `NaN` | `1 provided not x = 0 and not x = 1` |
| **silent** | `DomainCondition` of a logarithm, under the default reading | the real condition, so `log(-3, -3)` was declared undefined while evaluating to `1` | `not b = 0 and not b = 1 and not a = 0` |
| **silent** | `ln(x).DomainCondition` | `x > 0` | `not x = 0` |
| **silent** | `"x ^ n".Differentiate("x").Simplify()` | `x ^ n * n / x provided x > 0` — `NaN` at every negative `x` | `x ^ n * n / x provided not x = 0` |
| **silent** | `ln(a) + ln(b)` and `ln(a) - ln(b)` for symbolic `a`, `b` | `ln(a * b)`, `ln(a / b)` — wrong by `2*pi*i` off the positive reals | left as written |
| **silent** | `lim x->+oo (x^2)^x / e^(2*x*ln(x))` and `lim x->+oo x^x / e^(x*ln(x) - ln(x))` | unevaluated, the deliberate loss 2.1.0 recorded | `1` and `+oo`, the answers from before 2.1.0 |
| **silent** | `"x^5 + 2x^3 - 2x^2 - 4".SolveEquation("x")` | three of the five roots, one of them a float | all five, exact |
| | `"x^4 + x^2 + 1".SolveEquation("x")` | `sqrt((-1 - sqrt(-3)) / 2)` and its three companions | `(-1 - sqrt(-3)) / 2` and its three, with no nested radical |
| | `"x^4 + 3x^2 + 2".SolveEquation("x")` | `{ sqrt(-2), -sqrt(-2), i, -i }` | the same four, in the order the factors are found |
| **silent** | `"1/(x^4 + 3x^2 + 2)".Integrate("x")` | unevaluated | `arctan(x) - sqrt(2) * arctan(sqrt(2) * x / 2) / 2 + C` |
| **silent** | `"1/(x^4 + 4)".Integrate("x")` | unevaluated | the antiderivative over its two quadratic factors |
| **silent** | `"sin(-x) + sin(x)".Simplify()` | `sin(-x) + sin(x)` | `0` |
| **silent** | `"cos(-x)".Simplify()` and `"abs(-x)".Simplify()` | unchanged | `cos(x)`, `abs(x)` |
| **silent** | `"cos(0 ^ y)".InnerSimplified` | `-(-1) provided ...` | `1 provided ...` |
| | `Transformation.Rationalisation` and `RewriteRules.RationaliseDenominator` | spelled `-ise`, alone on a surface that is otherwise `Factorize`, `Normalization` | spelled `-ize`; a build error rather than a wrong answer |

### `InnerSimplified` is idempotent again

An exact trigonometric value reached through a half turn — `cos(x)` read off the table as
`-cos(pi - x)` — was built as a negation over the value it turned to and handed back unfolded. Where
the answer is then *wrapped* rather than rebuilt, nothing normalises it again:

```
"cos(0 ^ y)".InnerSimplified

was  -(-1) provided y / 2 * (1 + 1 / sgn(y) ^ 2) > 0
is       1 provided y / 2 * (1 + 1 / sgn(y) ^ 2) > 0
```

Both are the same value, so nothing was wrong — but applying `InnerSimplified` twice gave a different
tree from applying it once, and a great deal of the library treats what it hands back as settled.
`Simplify` was unaffected, since it normalises again anyway.

Found by `canoncheck`, the canonical-form harness: it was the only idempotence failure in 834
generated expressions, and the count is now zero.
[#930](https://github.com/asc-community/AngouriMath/issues/930).

### The parity identities are applied

`cos(-u) = cos(u)`, `sin(-u) = -sin(u)` and the rest of the family were absent, so an expression that
cancels exactly did not:

```
"sin(-x) + sin(x)".Simplify()

was  sin(-x) + sin(x)
is   0
```

Nothing false was being asserted, so this is a coverage change rather than a wrong answer put right —
but an expression that is identically zero was left standing, and anything testing a residual against
zero saw a non-zero residual where there was none.

The one case that already folded, `cos(-2 * x)`, folded by accident: the multiple-angle expansion
fires for a coefficient of magnitude two or more, and `-x` is a coefficient of `-1`, which it skips.
That is why `cos(-2 * x)` worked and `cos(-x)`, `sin(-2 * x)`, `tan(-2 * x)` and `abs(-2 * x)` did
not. What is matched now is a product with a negative real coefficient, which `-x` and `-2 * x` both
are.

Even: `cos`, `sec`, `abs`. Odd: `sin`, `tan`, `cotan`, `cosec`, `sgn`. Each holds on the whole
complex plane, and the poles of the odd ones sit symmetrically about zero — `tan(-z)` is undefined
exactly where `tan(z)` is — so **no condition is acquired**, which is pinned by its own test. Where a
cancellation is between two reciprocal functions the condition that was already there survives:
`tan(-x) + tan(x)` is `0 provided not cos(x) = 0`, which is right, since it is undefined at the poles
rather than zero there.

**A lone `sin(-x)` still prints as `sin(-x)` rather than `-sin(x)`**, and that is a tie rather than a
failure: both rate exactly 14 under the complexity criteria, and a tie goes to whichever candidate
was generated first. The identity is applied — it is what makes the cancellation above work — but it
does not win a comparison it was never going to win. The tests assert the cancellation for that
reason, rather than pinning the tie-break.

The inverse functions are deliberately untouched: `arcsin` and `arctan` are odd and `arccos` is not,
and this library's `arccotan` has range `(-pi/2, pi/2]` rather than the textbook `(0, pi)`, so each
wants measuring before anything is written down.
[#929](https://github.com/asc-community/AngouriMath/issues/929).

### A rational function is decomposed over the factors of its denominator, not only its roots

The other consumer of the polynomial layer, and the same blindness one level along. A partial
fraction decomposition split `N/D` at a **rational root** of `D`, so a denominator with no rational
root was left whole and its integral came back unevaluated — even where every factor was one the
integrator already reads. `x^4 + 3x^2 + 2` is `(x^2 + 1)(x^2 + 2)`, and the rule for a linear
numerator over a quadratic answers both halves.

```
"1/(x^4 + 3x^2 + 2)".Integrate("x")

was  integral(1 / (x ^ 4 + 3 * x ^ 2 + 2), x)
is   arctan(x) - sqrt(2) * arctan(sqrt(2) * x / 2) / 2 + C
```

The step is a coprime split rather than a full decomposition, which is what the step at a root
already was: the denominator is factored into irreducibles, one irreducible with its multiplicity is
taken against the product of the rest, the extended Euclidean algorithm in `Q[x]` gives `U*A + V*B =
1`, and `N/(A*B)` becomes `N*V/A + N*U/B` with each numerator reduced modulo its own denominator.
Both sides are strictly smaller problems of the same kind, so the integrator recurses into them and
arrives at the full decomposition either way.

**No condition is attached, and that is a statement rather than an omission.** `A` and `B` being
coprime, `A*B` is zero exactly where one of them is, so the two sides are undefined at the same
points. A decomposition that loses a singularity is one that cancels a shared factor, and this does
not cancel anything.

**What is still declined.** A denominator irreducible over `Q` — `x^4 + 1` is, and only factors once
real coefficients are allowed — and, more generally, any denominator one of whose factors is a shape
no integration rule reads: an irreducible of degree three or more, or a quadratic repeated. So
`(x^2 + 1)^2` is declined, and the ladder that would decompose it is deliberately not built, because
every term it produces is over `(x^2 + c)^k` and would come back unevaluated in turn.

*The `x^4 + 1` half of that is no longer true: allowing those real coefficients is exactly what the
entry below does, and `x^2/(x^4 + 1)` is now answered. The rest of the paragraph stands — an
irreducible of degree three or more, and a repeated quadratic, are still declined, and `(x^2 + 1)^2`
still is.*

Deciding that from the factorisation rather than by trying is what keeps the cost of declining to the
one factorisation. Splitting regardless and recursing made `(1 - x^4)/(1 + x^4 + x^8)`, whose
factorisation holds the irreducible quartic `x^4 - x^2 + 1`, take 18s to return the same unevaluated
integral it returns in 203ms — measured, and the reason the guard is there rather than a preference.

[#919](https://github.com/asc-community/AngouriMath/issues/919).

### A biquadratic denominator is decomposed over the reals

The same blindness one level further along, and the last place it reaches. The step above factors
over `Q` and stops where `Q` does, so `x^4 + 1` — irreducible over the rationals — was left whole
and `x^2/(x^4 + 1)` came back unevaluated. Over the reals it is
`(x^2 - sqrt(2)x + 1)(x^2 + sqrt(2)x + 1)`, and both halves are read by the rule for a linear
numerator over a quadratic. Nothing was missing but a factorisation the rational step is right to
refuse.

```
"x^2/(x^4 + 1)".Integrate("x")

was  integral(x ^ 2 / (x ^ 4 + 1), x)
is   -1/2 * sqrt(2) * 1/2 / 2 * ln(x ^ 2 + sqrt(2) * x + 1)
     + 1/2 * arctan((2 * x + sqrt(2)) * 1/2 * sqrt(2)) * 1/2 * sqrt(2)
     + 1/2 * sqrt(2) * 1/2 / 2 * ln(x ^ 2 - sqrt(2) * x + 1)
     + 1/2 * arctan((2 * x + -sqrt(2)) * 1/2 * sqrt(2)) * 1/2 * sqrt(2) + C
```

This is the integral [#233](https://github.com/asc-community/AngouriMath/issues/233) names as
wanting "partial fractioning", and it is the first of that issue's list to need a factorisation
rather than a rule. `1/(x^4 + 1)`, `1/(x^4 - 2)` and `1/(x^4 + 3x^2 + 1)` come with it.

**Biquadratic only, and that is a boundary rather than a first cut.** A general quartic factors into
real quadratics through its resolvent cubic, whose roots carry Cardano's nested radicals; a
biquadratic `x^4 + px^2 + q` is the case where the resolvent is solvable by inspection and the two
factors stay inside one square root. Two shapes come out of it, by the sign of `p^2 - 4q`: negative
gives `(x^2 + ax + b)(x^2 - ax + b)` with `b = sqrt(q)` and `a = sqrt(2b - p)`, and positive gives
the even `(x^2 + u)(x^2 + v)` with `u, v = (p -+ sqrt(p^2 - 4q))/2`. Zero is `(x^2 + p/2)^2`, a
repeated quadratic, declined for the reason the step above declines one. **A quartic with an odd
power in it — `x^4 + x^3 + 1`, `x^4 + x + 1` — is still declined**, and so is everything of degree
five and up that does not factor over `Q`.

**No condition is attached**, on the same argument as the step above: the two factors are distinct,
so their product is zero exactly where the original denominator is, and nothing is cancelled.

It is tried **after** both rational steps, which is what keeps a denominator that factors over `Q`
in exact arithmetic: `x^4 + 3x^2 + 2` is decomposed by the step above and never arrives here to be
given a square root it does not need. Declining stays as cheap as it was —
`(1 - x^4)/(1 + x^4 + x^8)` returns the same unevaluated integral in the same fraction of a second,
because every guard here is rational arithmetic on coefficients already read.

`sqrt(tan(x))`, the one remaining entry on #233's list, is **not** answered by this. It reduces
under `u = sqrt(tan x)` to `2 * integral(u^2/(u^4 + 1), u)`, which is now integrable — but the
substitution that gets there is a separate capability and is not built here. *It is built in the
entry below, and `sqrt(tan(x))` is now answered.*

[#233](https://github.com/asc-community/AngouriMath/issues/233).

### A fractional power, and the tangent, are substitutions

Two substitutions, in one entry because neither reaches `sqrt(tan(x))` without the other and
without the entry above. That integral is the last of the five
[#233](https://github.com/asc-community/AngouriMath/issues/233) lists, and the one it calls "very
painful, requires different solvers" — which it is. Three capabilities in a row:

```
int sqrt(tan(x)) dx
  --- u = tan(x),  dx = du/(1 + u^2)  ------->  int sqrt(u)/(1 + u^2) du
  --- t = sqrt(u), a fractional power  ------->  int 2t^2/(1 + t^4) dt
  --- 1 + t^4 factored over the reals  ------->  logarithms and arctangents
```

Take any one away and it comes back unevaluated.

```
"sqrt(tan(x))".Integrate("x")

was  integral(sqrt(tan(x)), x)
is   a sum of two logarithms and two arctangents in sqrt(tan(x)) — the shape the
     entry above produces, with sqrt(tan(x)) where it had x
```

**The fractional power.** A power substitution rewrites the other powers of the variable into
powers of itself: for `u = x^r` the identity is `x^n = u^(n/r)`, applied wherever `n/r` is a whole
number. A whole `r` reaches only the powers it divides, which is what this did before. An `r` of
`1/2` reaches every one of them — including the bare `x`, which a whole `r` never can — so
`int sqrt(x)/(1 + x^2)` becomes `int 2u^2/(1 + u^4) du`. `1/(1 + sqrt(x))`,
`1/(sqrt(x) * (1 + x))`, `1/(sqrt(x) * (1 + x^2))` and `1/(sqrt(x) + x)` come with it.

The rewrite is made in two passes, powers first and a leftover bare `x` second, because the tree
is rewritten from the leaves up: in one pass the `x` inside `sqrt(x)` is reached before the
`sqrt(x)` node is, and `u = sqrt(x)` turns it into `sqrt(u^2)` rather than `u` — an integrand free
of `x` and no more integrable than it started.

**The tangent.** An integrand that is a function of `tan(x)` and of nothing else becomes a
rational function under `u = tan(x)`, with `dx` as `du/(1 + u^2)`. The test is the rewrite itself:
replace every `tan(x)` and see whether an `x` survives. `tan(x) + x` keeps one and is declined,
which is right — it is answered, but by linearity over the sum.

This is a step of its own rather than a candidate for the general substitution, and the reason is
worth recording. The general one divides the integrand by `du/dx` and asks what is left, which
works while the substitution survives the division. Here it does not: `sqrt(tan(x))` over the
derivative of `sqrt(tan(x))` is `2 tan(x) cos(x)^2`, which is `sin(2x)` and is simplified to it —
a correct answer to a question that has stopped being about the tangent.

**What is still declined**, each by what the rewrite hands on rather than by the rewrite:
`cotan` is its own node rather than a reciprocal of the tangent, so `sqrt(cotan(x))` never starts;
`tan(x)^2` and `tan(x)^3` become improper fractions, and dividing an improper fraction out is not
something the rational integrator does; `1/(1 + tan(x)^2)` becomes `1/(1 + u^2)^2`, a repeated
irreducible quadratic. On the other side, `sqrt(x)/(1 + x^4)` becomes `2u^2/(1 + u^8)`, whose
denominator is neither factorable over the rationals nor a biquadratic.

**The rule for the tangent itself still wins**, being reached first: `int tan(x)` is
`-ln(cos(x)) + C` and not the longer thing this would produce.

**The corpus gate now reads 40 solved of 40.** Its one unsolved problem was `int:hard`, and
`int:hard` is `sqrt(tan(x))` — chosen for that list as the standing example of an integral out of
reach. The gate reached the same verdict independently, by differentiating the answer back rather
than by comparing it with anything.

[#233](https://github.com/asc-community/AngouriMath/issues/233).

### An improper quotient is divided out before it is decomposed

Every step of the rational integrator wants a **proper** fraction — a numerator of lower degree
than the denominator — and each of the three declines an improper one rather than dividing it out.
So `x^2/(x + 1)` had no antiderivative, although it is `x - 1 + 1/(x + 1)` and every piece of that
has been integrable throughout.

```
"x^2/(x + 1)".Integrate("x")

was  integral(x ^ 2 / (x + 1), x)
is   x ^ 2 / 2 + -x + ln(x + 1) + C
```

**The division is not new code.** `TreeAnalyzer.PolynomialLongDivision` has done it all along, for
the simplifier's own `PolynomialLongDivision` rule set; the integrator simply never asked it. What
changed is one call, placed before the three decompositions rather than after them.

New with it, as written: `x^3/(1 + x^2)`, `x^4/(x^2 + 1)`, `(x^5 + 2)/(x^2 + 1)`,
`(x^2 + 3x + 5)/(x + 2)`, and the exact-division cases `(x^3 + 1)/(x + 1)` and `(x^2 - 1)/(x - 1)`,
whose proper part is zero.

New with it **through a substitution**, which is where it matters more, since the substitution
produces the improper fraction rather than the user writing one: `tan(x)^2`, `tan(x)^3` and
`sqrt(x)/(x + 1)` were all declined for this and no other reason, each having been named as such
in the tests that recorded the boundary. `int tan(x)^2` comes back as
`tan(x) - arctan(tan(x)) + C` rather than the textbook `tan(x) - x`, which is the same function on
the principal branch and is what the rewrite has to say without an assumption about which branch
`x` is on.

**A proper fraction is untouched.** The helper answers nothing for one, so the three steps below
see exactly what they saw before: `int 1/(x + 1)` is still `ln(x + 1) + C` and `int x/(x^2 + 1)`
still `ln(x^2 + 1)/2 + C`.

**A symbolic leading coefficient is still declined.** `x^2/(a + b*x)` —
[#180](https://github.com/asc-community/AngouriMath/issues/180)'s item 18 — would have the division
divide by `b`, which is not decidably non-zero, and at `b = 0` the quotient is `x^2/a`, whose
antiderivative is not the limit of the divided form. `x^2/(x + a)` is declined too, for a narrower
reason: the leading coefficient there is `1`, but the helper does not divide a polynomial whose
other coefficients are symbolic.

[#180](https://github.com/asc-community/AngouriMath/issues/180).

### A system with fewer equations than unknowns is answered, not refused

`Solve` raised `WrongNumberOfArgumentsException` for a system with fewer equations than
unknowns — a message that says the caller called it wrongly, for a caller who did nothing wrong.
`2x - 4y = 12` in `x` and `y` is a well-formed question; it simply has infinitely many answers.

```
MathS.Equations("2*x - 4*y - 12").Solve("x", "y")

was  WrongNumberOfArgumentsException: Number of equations must be equal to that of vars
is   [[6 + 2 * t_1, t_1]]
```

which is `x = 6 + 2t, y = t` — the answer the issue asks for in its body.

**The answer type did not change.** A solution has always been a row whose i-th entry is the
i-th unknown's value, and nothing said those entries may not mention a variable. The unknowns a
row reduction leaves free become parameters, named the way the constant of integration is named
in an ODE's answer, and the rest are written in terms of them. The system from the issue thread,
three equations in five unknowns, comes back with two of them:

```
{ p + 2q + 4r + s - u = 1, 2p + 4q + 8r + 3s - 4u = 2, p + 3q + 7r + 3u = -2 }

is   [[7 + 2 * t_1 + 3 * t_2, -3 + (-3) * t_1 + (-2) * t_2, t_1, 2 * t_2, t_2]]
```

**Only the short count is taken this way.** A square system that is rank-deficient still reaches
the eliminator, which has answered it with a free parameter since
[#550](https://github.com/asc-community/AngouriMath/issues/550); those answers are unchanged, as
are every determined and every overdetermined system.

**A contradictory short system is `null`**, the same as a contradictory square one, and that
continues to mean *there are no solutions* rather than *no answer was found*.

**What still raises.** The system has to be linear in the unknowns, checked rather than assumed
— `x^2 + y^2 = 1` and `x*y = 1` fail the check — and the coefficients **on the unknowns** must be
rational, so `a*x + y = 1` raises too. That last is a soundness requirement rather than a
convenience: a row reduction has to decide whether a pivot is zero, the general test available
is structural, and choosing a pivot that is zero without being written as `0` produces a wrong
family rather than no answer. The **constant** term is under no such restriction, being never a
pivot, so `2x - 4y = k` is answered with `k` symbolic.

**Code that caught `WrongNumberOfArgumentsException` around `Solve`** to detect an
underdetermined system now gets a matrix for the linear ones, and the exception only for the
rest.

[#212](https://github.com/asc-community/AngouriMath/issues/212).

### A polynomial equation that factors is solved through its factors

A polynomial of degree four or more may factor over the rationals with no rational root anywhere in
it — `x^4 + 3x^2 + 2` is `(x^2 + 1)(x^2 + 2)` — and nothing in the solver could see that. The
rational-root split that runs first is by construction blind to it, and what followed was the
general machinery for the degree: Ferrari at four, and at five and above no general solution exists
to reach for. A real polynomial layer
([#746](https://github.com/asc-community/AngouriMath/issues/746) item 43) is what was missing, and
the equation solver is its first consumer: where the polynomial factors, each factor is now solved
as a lower-degree equation of its own.

The change is confined to degree four and up, and that is not a threshold chosen for caution. A
quadratic or a cubic that factors at all has a rational root — a cubic splits as a linear factor
times a quadratic, or into three linear factors, and either way there is a linear factor to find —
so the step that runs before this one has already divided it out. Four is the first degree at which
a polynomial can factor with nothing rational to catch. A two-termed `a*x^n + b` is also left alone,
for the reason already written down where the rational-root split declines one: inverting the power
answers it whole and in polar form.

**The one that was wrong rather than merely differently written.** An incomplete solution set is not
a partial answer, it is a false one — it says these are the roots.

```
"x^5 + 2x^3 - 2x^2 - 4".SolveEquation("x")

was  { sqrt(-2), -sqrt(-2), sqrt(1.5874010519681995834417875812505371868610382080078125) }
is   { sqrt(-2), -sqrt(-2), 2 ^ (1/3),
       (-1/2 + i * 1/2 * sqrt(3)) * 2 ^ (1/3), (-1/2 + i * -1/2 * sqrt(3)) * 2 ^ (1/3) }
```

The polynomial is `(x^2 + 2)(x^3 - 2)`. Two of its five roots were missing, and the one real root of
`x^3 - 2` came back as the square root of a float where `2 ^ (1/3)` is exact — an inexact answer to a
question that has an exact one. It also took 4.6 seconds and now takes under a quarter of one.

**The others are the same numbers, written better or written in a different order.** Factoring first
means each root comes out of a quadratic or a cubic rather than out of the resolvent of a quartic,
and the forms are correspondingly plainer:

```
"x^4 + x^2 + 1".SolveEquation("x")
was  { sqrt((-1 - sqrt(-3)) / 2), -sqrt((-1 - sqrt(-3)) / 2), ... }
is   { (-1 - sqrt(-3)) / 2, (-1 + sqrt(-3)) / 2, (1 - sqrt(-3)) / 2, (1 + sqrt(-3)) / 2 }

"x^4 + 3x^3 + 6x^2 + 5x + 3".SolveEquation("x")
was  { -3/4 + (-1/2 + -sqrt(-8)) / 2, ... }
is   { (-1 - sqrt(-3)) / 2, (-1 + sqrt(-3)) / 2, (-2 - sqrt(-8)) / 2, (-2 + sqrt(-8)) / 2 }
```

Both sets are unchanged as sets — the members were checked to be equal, not merely to look it. Where
a solution set is only reordered, as for `x^4 + 3x^2 + 2` and `2x^4 + 6x^2 + 4`, code that indexed
into the result rather than searching it will see different elements at the same positions.

### A sum of logarithms is no longer gathered unless that is exact

`ln(a) + ln(b) = ln(a*b)` is false off the positive reals. At `x = -3` the two sides differ by
`2*pi*i`, the turn of the argument the principal branch discards:

```
"ln(x) + ln(x+1)" at x = -3     1.7918 + 6.2832i
gathered to ln(x*(1+x))         1.7918              — the value that was returned
```

Both rules were applied unconditionally, and this was the last disagreement `boundcheck` reported.
It now reports **none**.

**Numbers still gather.** `ln(2) + ln(3)` is `ln(6)` and `ln(6) - ln(2)` is `ln(3)`, because there
the operands are decidably positive. What no longer happens is the same rewrite on a *symbol*,
which may be anything.

**And the identity is not lost where it was doing real work.** Taking a limit states where the
expression is going, and on a stated approach the sign of each operand is decidable — so the
gathering still happens inside a limit, exactly where it is exact. A sum needs both operands
positive; a difference needs only that their signs *agree*, since `ln` of a negative is
`ln|.| + pi*i` and that cancels in a difference while it would double in a sum. No limit answer is
lost, which was measured rather than assumed: withdrawing the rule without putting it back this way
does not cost coverage, it costs **termination**, because the limit machinery's own expansion
creates the pairs that only this puts back together.

From [#721](https://github.com/asc-community/AngouriMath/issues/721).

### An exponent under a logarithm is read again, where a limit says the base holds its sign

2.1.0 withdrew `log_b(a^c) = c * log_b(a)` from an undecided argument, because it is false off
`ln`'s principal strip, and recorded two limits as the price:

| | 2.1.0 | now |
|---|---|---|
| `lim x->+oo (x^2)^x / e^(2*x*ln(x))` | unevaluated | `1` |
| `lim x->+oo x^x / e^(x*ln(x) - ln(x))` | unevaluated | `+oo` |

Both are answered again, and by the same route the logarithm gathering above takes: a stated
approach. The identity needs `Im(c * ln a)` inside `(-pi, pi]`; a base that holds a positive sign on
the way to the destination makes `ln a` real, and an exponent that is real along the approach leaves
the product real, so there is nothing for the principal branch to discard. Both halves are
answerable while a limit is being read and neither is answerable to a simplifier reading an
expression on its own account — so **outside a limit the rule still declines**, and `ln(e^x)` is
still left as written. That half now has a test of its own, because widening the guard until an
ordinary `Simplify` applies the identity would restore the wrong answer
[#902](https://github.com/asc-community/AngouriMath/issues/902) reported while every limit above
kept passing.

The exponent's realness is decided structurally and conservatively rather than read off a limit: a
positive *limit* does not make a base real on the way to it, since `x + i*sin(x)` tends to `+oo` off
the real line. A power is admitted only with a whole exponent or a decidably positive base, because
`(-2)^(1/2)` is imaginary; a second variable carries no approach and is refused. Anything unlisted
is refused too, which costs coverage and never correctness.

**What this corrects is a recorded prediction, not only a lost answer.** 2.1.0's entry said these two
wanted "an assumption travelling with the expression — `#746`'s tier 1 and the subject of
[#721](https://github.com/asc-community/AngouriMath/issues/721) — and not another pass", on the
strength of three insertion points that were each tried and each failed. That measurement was
sound and the conclusion drawn from it was not: all three were *pre-passes*, which rewrite the
expression before `Simplify` is called and cannot reach the candidate search that rebuilds it. An
ambient scope is not a pass. The rule itself asks whether an approach is being read, so it is
answered wherever the rule is asked — including 117 times inside one l'Hopital descent. No
assumption mechanism arrived, and none was needed.

`boundcheck` stays at **0** disagreements, and the suite finishes in its usual time — the failure
mode to watch for here is a hang rather than a wrong answer, since the limit machinery's own
expansion creates the shapes these rules put back.

### The logarithm's domain follows the reading, as every other node's already did

`Arcsinf`, `Arccosf`, `Arcsecantf` and `Arccosecantf` each state one condition over the reals and
another over the complex plane, and pick between them by the reading. `Logf` stated the real one as
though it were the only one, which is what
[#890](https://github.com/asc-community/AngouriMath/issues/890) reported as a contradiction: the
library gave an expression a value and declared it undefined at the same time.

```
log(-3, -3)                        1          — unchanged
log(-3, -3).DomainCondition        False  ->  True
ln(x).DomainCondition              x > 0  ->  not x = 0
```

Over the complex plane `log_b(a)` is `ln(a) / ln(b)`, which asks only that both logarithms exist
and that the denominator is not zero — no complex number has `0` as its exponential, so `a` and `b`
must be nonzero, and `ln(b) = 0` is `b = 1`. Negative and non-real arguments are admitted:
`log(2, -8)` is `3 + 4.53i` and was declared undefined.

**The real condition is still there and still reachable.** `expr.WithCodomain(Domain.Real)` gives
`x > 0 and not x = 1` for `log(x, x)` exactly as before; what changed is which of the two is the
default, and the default is now the same complex reading the evaluator has always used.

### `log_b(b) -> 1` carries the condition it has rather than one written into the rule

The rewrite asserted `provided x > 0`, and that was wrong in both directions at once — it withdrew
an answer that exists and kept one that does not:

```
"log(x, x)".Simplify()             1 provided x > 0  ->  1 provided not x = 0 and not x = 1
  at x = -3                        NaN  ->  1      (the logarithm is 1)
  at x = 1                         1    ->  NaN    (the logarithm is NaN)
```

`boundcheck` reports one disagreement where it reported two. The survivor,
`ln(x) + ln(x+1) -> ln(x*(1+x))`, needs an assumption travelling with the variable and is
untouched here.

### The derivative of a symbolic power loses a condition it never needed

`d/dx x^n` goes through `ln(x) * 0`, and `anything * 0 = 0` carries the operand's domain condition.
That condition was the logarithm's real one, so the derivative was undefined at every negative `x`:

```
"x ^ n".Differentiate("x").Simplify()   x ^ n * n / x provided x > 0
                                    ->  x ^ n * n / x provided not x = 0
  at n = 3, x = -2.5                 NaN  ->  75/4   (and 3x^2 at -2.5 is 75/4)
```

The condition does not vanish: `ln(0) * 0` is `-oo * 0`, which is `NaN`, so `not x = 0` is kept.

From [#721](https://github.com/asc-community/AngouriMath/issues/721) and
[#890](https://github.com/asc-community/AngouriMath/issues/890), in PR
[#916](https://github.com/asc-community/AngouriMath/pull/916).

### Three members spelled `-ise` are spelled `-ize`

The rest of the surface is `Factorize`, `Latexize`, `Stringize`, `Normalization`, `Factorization`.
Three members added in 2.1.0 were not, so the same operation had two spellings depending on which
one a caller reached for:

| Was | Is |
|---|---|
| `Transformation.Rationalisation` | `Transformation.Rationalization` |
| `RewriteRules.RationaliseDenominator` | `RewriteRules.RationalizeDenominator` |
| `Patterns.RationaliseDenominator` (internal) | `Patterns.RationalizeDenominator` |

**If you recompile, this breaks a build rather than an answer** — the compiler names the missing
member. Two ways it can reach you later than that:

- `AssemblyVersion` is pinned at `2.0.0.0` for the whole of 2.x precisely so that a consumer can
  drop a new DLL in without a binding redirect. Do that without recompiling, and a call to
  `Transformation.Rationalisation` throws `MissingMethodException` when it is *reached*, not when
  the assembly loads. That is the one path on which this behaves like a silent change.
- The rule set's `Name` comes from `nameof`, so a caller matching on the string
  `"RationaliseDenominator"` now sees `"RationalizeDenominator"` and simply stops matching.

Recompiling against 2.2.0 turns both into compile errors, except the string.

Documentation prose keeps British spelling throughout — `Factorize` has always sat beside the word
"factorisation" and still does. The convention is about identifiers.

Recorded here rather than left alone because a tag is public whether or not anyone has taken it. The
two canonical-form members added in this release were renamed before they ever shipped, so they are
not in the table.
In PR [#940](https://github.com/asc-community/AngouriMath/pull/940).

---

## 2.1.0 — since 2.0.0

Everything here landed **after** the 2.0.0 tag, so it is not in the package a reader of that
section already has. Almost all of it is a wrong answer becoming a right one, found by the
boundary and crash harnesses rather than reported; two answers are withdrawn deliberately and say
so, and one word — `NaN` — is reserved.

Performance against 2.0.0 is measured and published as a pair in
[`Docs/WhatsNew/version_performance_control.md`](Sources/AngouriMath/Docs/WhatsNew/version_performance_control.md):
no regression, the largest real move being `SolveMedium` at +4.6%.

### At a glance

| Silent? | What | Was | Is |
|---|---|---|---|
| loud | `NaN` as a variable name | a variable | a keyword, so the NaN value |
| loud | `MathS.ToSympyCode` of any non-integer rational | `SyntaxError` — a parenthesis was never closed | code that runs |
| **silent** | `MathS.ToSympyCode` of `1/2`, `2^(-1)` | ran, and gave the float `0.5` | `1/2`, exact |
| loud | `MathS.ToSympyCode` of `NaN`, `+oo`, `-oo` | `NameError` — the name is never bound | `sympy.nan`, `sympy.oo`, `-sympy.oo` |
| **silent** | `NaN` printed and read back | a variable of that name, which cancels and collects | the NaN value |
| loud | `Compile` of `floor`, `ceil`, `round`, `phi`, `gamma`, `!` | `AngouriBugException`, asking to be reported | `UncompilableNodeException` |
| loud | `Compile` of a boolean or lambda node as `double` | `InvalidOperationException` from Linq | `UncompilableNodeException` |
| **silent** | `abs(sgn(x))` and `sgn(abs(x))` | `1`, wrong at `x = 0` where both are `0` | left as written unless the argument's value can be read |
| **silent** | `ln(e^x)`, `log(2, 2^x)`, `ln(x^2)` | `x`, `x`, `2 * ln(x)` — wrong off the real line | left as written unless the argument is decidable |
| **silent** | two limits over `(x^2)^x` and `x^x` | answered correctly | unevaluated — a deliberate loss |
| **silent** | `DirectChildren` of a conditional set | a name off the predicate's hash, and one in 26^4 threw | `%1`, fresh by construction |
| **silent** | `-(a - b)` inside a power, a function or a matrix | left as written | `b - a`, as at the root |
| **silent** | `Expand` of a matrix | the matrix, unexpanded | expanded entry by entry |
| **silent** | `false and u`, `true or u`, `false implies u` for an undefined `u` | `NaN` | `False`, `True`, `True` — what the truth table settles |
| **silent** | a connective over a number, e.g. `true and 0`, `false and 0` | `NaN`, or `False` by short-circuit | left as written — a number is not a truth value |
| **silent** | `arctan(x) + arccotan(x)` | `pi/2`, wrong for every negative `x` | `pi/2` or `-pi/2` where the sign is known, else left as written |
| **silent** | `log(1, 1)` | `0` | `NaN`, since it is `0/0` |
| **silent** | `log(b, 1)` | `0` for any base | `0 provided not b = 1` |
| **silent** | `log(1/2, 0)` and any base below 1 | `-oo` | `+oo` |
| **silent** | `abs(-sqrt(6))`, `abs(-pi)`, `abs(1 - sqrt(2))` | left as written | `sqrt(6)`, `pi`, `sqrt(2) - 1` |

### `Compile` fails with its own exception rather than someone else's

Two families, both reported as `UncompilableNodeException` — the exception
[`Docs/Usage/Exceptions.md`](Sources/AngouriMath/Docs/Usage/Exceptions.md) documents for a node with
no compiled form. Neither the node set nor the compiled output changes; only what is thrown when
compilation is impossible.

| input, compiled as `<double, double>` | was | is |
|---|---|---|
| `floor(x)`, `ceil(x)`, `round(x)`, `phi(x)`, `gamma(x)`, `x!` | `AngouriBugException`: *An unary node seems to be not added* | `UncompilableNodeException` |
| `not x`, `x and 2`, `x or 2`, `x xor 2`, `x implies 2`, `x -> x + 1` | `InvalidOperationException` from `System.Linq.Expressions` | `UncompilableNodeException` |
| `x provided 2` | `ArgumentException`: *Argument must be boolean* | `UncompilableNodeException` |

`AngouriBugException` means "an internal error occurred, report it", and none of the first row is one
— the converter has no case for the node, which is a gap in coverage. **Four of those nodes are ones
2.0 added**: `floor`, `ceil` and `round` arrived with #809 and the compiler was never taught them, so
a caller compiling a 2.0 feature was asked to file a bug report.

The second family matters for a caller who followed the documentation: `catch (AngouriMathBaseException)`
is what the exception reference tells you to write around a library call, and Linq's exceptions are
not under it, so those escaped the handler entirely.

**What this does not do** is teach the compiler `floor`, `ceil`, `round` and the rest. That is worth
doing and is separate; this is only about the failure being honest rather than either a request for a
bug report or an exception nobody was told to expect. Issue
[#894](https://github.com/asc-community/AngouriMath/issues/894).

Found by `crashcheck`, which runs each case in a child process so that a crash is a result rather
than the end of the run. On 1652 cases it reports 0 crashes and 0 hangs, and these 16 were every
remaining finding.

### A logarithm behaves like the division it is defined as

`log_b(z)` is `ln(z) / ln(b)`, and three answers did not follow from that. Every division by zero in
this library is `NaN` — `0/0`, `2/0` and `-2/0` all are — so a logarithm whose base is `1` divides by
`ln(1) = 0` and must be `NaN` too.

| | was | is |
|---|---|---|
| `log(1, 1)` | `0` | `NaN` — it is `0/0` |
| `log(1, 2)` | `+oo` | `NaN` — dividing by `ln 1 = 0` has no signed answer |
| `log(b, 1)` | `0`, for any base including 1 | `0 provided not b = 1` |
| `log(1/2, 0)`, and any base below 1 | `-oo` | `+oo` |
| `log(2, 0)`, and any base above 1 | `-oo` | `-oo`, unchanged |
| `log(x, 0)` for symbolic `x` | `-oo` | left as written |

The first two came from a shortcut: `Number.Log` uses `EDecimal.LogN` for a positive real base, and
`LogN` answers `0` for `log_1(1)` and `+oo` for `log_1(2)` where falling through to `Ln(x)/Ln(base)`
gives `NaN` for both. A base of `1` is now excluded from the shortcut so the two paths agree.

`log(b, 1)` is `0/ln(b)`, which is `0` for every base but `1`. **A condition is right here**, unlike
the interval cases above: at `b = 1` the expression genuinely is undefined rather than merely
something else, so narrowing the domain is what the mathematics says.

`log(b, 0)` is `-oo/ln(b)`, so the sign of the answer follows the sign of `ln(b)`. It answered `-oo`
for every base, which is wrong below `1`. For a base that cannot be placed on one side of `1` there is
no signed answer to give, so the node is left as written.

Issue [#890](https://github.com/asc-community/AngouriMath/issues/890), which also records a second
half not fixed here: `DomainCondition` of `log(-3, -3)` is `False` while the expression evaluates to
`1`, so the declared domain and the evaluation disagree. That is #721's question and wants a decision.

### `arctan(x) + arccotan(x)` is not always `pi/2`, and `arccotan(cotan(x))` was guarded wrongly

Both follow from one fact about this library's `arccotan`: it is `arctan(1/x)` extended by
`arccotan(0) = pi/2`, so its **range is `(-pi/2, pi/2]`** and not the `(0, pi)` that many textbooks
use. Measured: `arccotan(1)` is `pi/4`, `arccotan(-1)` is `-pi/4`, `arccotan(0)` is `pi/2`.

| | was | is |
|---|---|---|
| `arctan(-3) + arccotan(-3)` | `pi/2` | `-1/2 * pi` — which is the value |
| `arctan(3) + arccotan(3)` | `pi/2` | `pi/2`, unchanged |
| `arctan(x) + arccotan(x)` for symbolic `x` | `pi/2` | left as written |
| `arccotan(cotan(2))` | `2` | `-1.1416...` — which is the value, `2 - pi` |
| `arccotan(cotan(-1/2))` | left as written | `-1/2` |

The sum is `pi/2` for a non-negative real argument and `-pi/2` for a negative one. `pi/2 * sgn(x)`
is the closed form and is wrong at exactly one point — at `x = 0` the sum is `pi/2` while `sgn(0)`
is `0` — so the sign is decided where it can be read and the sum is left alone otherwise. A
`Piecewise` would be total, but `Compile` throws `UncompilableNodeException` on one, so answering
that way would break expressions that compile today.

**The second row is a correction to the release before it.** 2.0.0 guarded
`arccotan(cotan(x))` with `[0, pi]`, on the assumption that `arccotan`'s range was `(0, pi)`. That
admitted `(pi/2, pi)`, where the rewrite is false, and refused `(-pi/2, 0)`, where it is true. The
interval is now `(-pi/2, pi/2]` without zero — zero excluded because `cotan` has no value there, so
the composition has none either and rewriting to `x` would invent one. The other three intervals
from that change check out: `arcsin` is `[-pi/2, pi/2]`, `arccos` is `[0, pi]`, `arctan` is
`(-pi/2, pi/2)`.

`arcsin(x) + arccos(x) -> pi/2` is **unchanged and needs no assumption**, since `arccos(x)` is
`pi/2 - arcsin(x)` by definition over the whole plane.

Three tests moved. `SimplifyTest.Patt8` and `ArctanIdentitiesTest.NeighbouringIdentitiesAreUnaffected`
asserted `pi/2` for a symbolic argument and were pinning the wrong answer; both now use numbers and
cover each sign. `SortSimplifyTest`'s `arctan(x2) + arccot(x*x)` case sorted and collected its whole
sum only because the collapse shortened it, so with the collapse gone the sum stays as written —
the sibling `arcsin`/`arccos` case still collapses and still sorts.

Found by `boundcheck`, a harness that composes every unary function node with every other and
compares against the original at points where an assumption fails rather than at sampled points.
Issue [#887](https://github.com/asc-community/AngouriMath/issues/887).

### An exponent is no longer pulled out of a logarithm over an undecided argument

`log_b(a^c) = c * log_b(a)` holds where `c * ln(a)` stays inside the strip `Im in (-pi, pi]` that `ln`
maps onto. It was applied to any argument at all, and the rewritten form is shorter, so it is what an
ordinary caller got:

| | was | is |
|---|---|---|
| `ln(e^x)` | `x` | left as written |
| `log(2, 2^x)` | `x` | left as written |
| `ln(x^2)` | `2 * ln(x)` | left as written |
| `ln(e^3)`, `log(2, 2^5)` | `3`, `5` | unchanged |
| `ln(e^x)` under `Codomain.Set(Domain.Real)` | `x` | `x`, unchanged |

`ln(e^x) -> x` is wrong wherever `Im x` leaves that strip. At `x = 3*pi*i` the expression is `pi*i`,
because `e^(3*pi*i)` is `-1`, while `x` is `9.4247...i` — the two differ by exactly the full turn the
principal branch discards. `MathS.Settings.Codomain` defaults to `Domain.Complex`, so this was unsound
on the library's own default reading. It is also unsound for a negative real base: `log(2, 64)` is `6`
where `2 * log(2, -8)` is `6 + 9.0647...i`.

The rule now asks for a base that is decidably a positive real, and an exponent that may be taken as
real — because the reading is real analysis, because the node's declared codomain says so, or because
its value is a real. A symbolic exponent under the default complex reading is none of those, so the
expression is left as written: decide, or decline, as with the four inverse-trigonometric rules above.

**Two limits are lost, and that is the cost of this entry rather than an oversight.**

| | was | is |
|---|---|---|
| `lim x->+oo (x^2)^x / e^(2*x*ln(x))` | `1` | unevaluated |
| `lim x->+oo x^x / e^(x*ln(x) - ln(x))` | `+oo` | unevaluated |

Both are right answers becoming no answer, which this file has recorded before for two integrals, and
which the ordering in [AGENTS.md](AGENTS.md) prefers to a wrong answer reachable from `ln(e^x)`. They
are unevaluated rather than `NaN`: the caller is told nothing was settled, not that the limit does not
exist.

They want the identity that was just removed. `d/dx (x^2)^x` carries `ln(x^2)`, and l'Hopital's rule
reached it through `Simplify`. On the way to `+oo` the base genuinely is positive, so the identity is
true there — the limit machinery simply has no way to say so to the simplifier. Supplying it from the
limit side was tried and does not reach: rewriting the expression before `Simplify` is called does pull
the exponent out, and `Simplify`'s own candidate search then writes `(x^2)^x` back into a logarithm and
needs the identity again. It is load-bearing *inside* the search, so what would restore these two is an
assumption travelling with the expression — `#746`'s tier 1 and the subject of
[#721](https://github.com/asc-community/AngouriMath/issues/721) — and not another pass. The two rows
have their own test asserting the unevaluated node, so a future fix flips them back deliberately.

`boundcheck` drops from four disagreements to two; the remaining two are `log(x, x)` and
`ln(x) + ln(x+1)`, both recorded elsewhere as wanting a decision rather than a guard. Issue
[#902](https://github.com/asc-community/AngouriMath/issues/902).

### A logical connective over a number has no answer

A number is not a truth value: `0` is not `False` and `1` is not `True`. The same category error was
reported three different ways, depending on which operator saw it and in which position:

| expression | was | is |
|---|---|---|
| `true and 0`, `false or 0`, `false xor 0` | `NaN` | left as written |
| `false and 0`, `true or 0` | `False`, `True` — by short-circuit | left as written |
| `true xor 0` | `not 0` | left as written |
| `not 0`, `0 xor 0` | left as written | unchanged |
| `true and 1`, `true and 1/2`, `true and i` | `NaN` | left as written |

**`NaN` was the worst of the three.** In this library it means *this does not exist*, and `true and 0`
exists perfectly well — it is not a proposition. Reporting a sort error as nonexistence tells a caller
something false about the mathematics, and it is the answer an `if` on `EvaluableBoolean` does not save
you from. The other two were an answer arrived at by not looking, and a rewrite of one non-boolean into
another.

**Two answers are withdrawn deliberately.** `false and 0` and `true or 0` used to come back `False` and
`True`, because short-circuiting settled them before the number was examined. Those are gone: whether an
operand is admissible cannot depend on whether the operator happened to need it. There is no proposition
here for `False` to be the truth of.

A caller who insists on a boolean is unaffected — `EvalBoolean()` throws `CannotEvalException` for an
unevaluated node exactly as it did for `NaN`, so the type error still surfaces at the layer where
insisting happens. `DomainCondition` stops contradicting evaluation as a side effect: `domain(a xor 0)`
says `True`, and there is no longer a `NaN` for it to disagree with.

**A number is not the same as an undefined truth value, and the two must not be treated alike.** `NaN`
is how this library spells *no truth value* — what an order comparison over the complex plane produces —
and a connective settles what it can with one, which is the entry above. So `(0/0) and False` and
`(i < 0) and False` are both still `False`, and only a genuine number makes a connective decline. A bare
variable is `Domain.Any` and may yet be a truth value, so `false and x` is still `False`.

Issue [#897](https://github.com/asc-community/AngouriMath/issues/897), which asked whether a number is
a truth value here at all. It is not; a truth value that is neither true nor false is what
`MathS.Quantum` is for.

### A logical connective is no longer strict in `NaN`

`Simplify` and evaluation disagreed about three-valued logic. `Simplify` gave the Kleene answer and
evaluation absorbed everything into `NaN`, so the two contradicted each other on the same expression:

```
"True or (True and (x < 0))".Simplify()          ->  True
the same, at x := i, evaluated as written        ->  NaN      (was)
                                                 ->  True     (is)
```

`i < 0` has no truth value — the default codomain is `Domain.Complex` and the complex numbers are not
ordered — so it evaluates to `NaN`. What changed is what a connective does with such an operand.

| expression | was | is |
|---|---|---|
| `(i < 0) and False` | `NaN` | `False` |
| `(i < 0) or True` | `NaN` | `True` |
| `False implies (i < 0)` | `NaN` | `True` |
| `(i < 0) implies True` | `NaN` | `True` |
| `(i < 0) and True` | `NaN` | `NaN`, unchanged |
| `(i < 0) or False` | `NaN` | `NaN`, unchanged |
| `not (i < 0)` | `NaN` | `NaN`, unchanged |
| `(i < 0) xor (i < 0)` | `NaN` | `NaN`, unchanged |
| `(0/0) * 0`, `(0/0) + 1` | `NaN` | `NaN`, unchanged |

The rule is the ordinary one for three-valued logic: an operand with no truth value cannot change an
answer the table settles without it, and where the answer does depend on it the result stays `NaN`.
**Arithmetic is untouched** — `NaN` still absorbs there, which is why this is opted into per node
rather than changed for everything: a rule for a zero factor exists, and `NaN * 0` must not reach it.

The tables were already three-valued. `Andf` reads `(_, Boolean(false))` as `False` and
`(Boolean(true), _)` as its right operand, which is Kleene as written; what overrode them was one line
in the shared `ExpandOnTwoArguments`, `if (left.IsNaN || right.IsNaN) return MathS.NaN;`, running
*before* the table was consulted. The connectives now get first refusal on an undefined operand and
hand back `null` where they cannot settle it, which is what still reaches `NaN`.

**One consequence to know about.** For `x < 0 and x = 0` the evaluator now settles `False` for every
`x`, since `x = 0` is decidably false at `x = i` and `False and u` is `False`. `Simplify` answers
`False provided x in RR`, whose condition
([#876](https://github.com/asc-community/AngouriMath/issues/876)) is over-strong for that row: the
reduction needs one conjunct false, not both operands real. So `Simplify` is now weaker than evaluation
there rather than stronger. It is recorded in a test rather than fixed here, because the rules #876
conditioned want going through one at a time.

Issue [#880](https://github.com/asc-community/AngouriMath/issues/880), which set this out as a fork
between Kleene and strict evaluation and left it open for want of a measurement. The measurement: one
assertion in the suite changed, and it was that issue's own guard clause.

### A negated difference is turned round wherever it sits, and `Expand` descends into a matrix

`-(a - b)` became `b - a` for a whole expression and not for the same expression inside another node,
so what a caller got depended on where the subexpression sat:

| input | was | is |
|---|---|---|
| `-(5 - sqrt(-11))` | `sqrt(-11) - 5` | unchanged |
| `-(5 - sqrt(-11)) + y` | `y - (5 - sqrt(-11))` | `sqrt(-11) - 5 + y` |
| `2 ^ (-(5 - sqrt(-11)))` | left as written | `2 ^ (sqrt(-11) - 5)` |
| `sgn(-(5 - sqrt(-11)))` | left as written | `sgn(sqrt(-11) - 5)` |
| `[[-(5 - sqrt(-11)), 1]]` | left as written | `[[sqrt(-11) - 5, 1]]` |
| `Expand` of `[[(x+1)^2, 1]]` | `[[(x + 1) ^ 2, 1]]` | `[[1 + 2 * x + x ^ 2, 1]]` |

Every one of these was already the right number, written the long way round. The step came from
`Expand`, which the simplifier offers for the **root** expression only and which does not descend into
an exponent, a function's argument or a matrix. Written as a rule instead — a unary minus parses as
`(-1) * x`, so `(-1) * (a - b)` becomes `b - a`, which is five nodes for three — it runs wherever the
shape occurs, since a rule walks the tree.

`Expand` of a matrix is the same story from the other end: it read its argument as a sum, and a matrix
is not one, so a matrix left through the "too complicated, return what came in" exit. It now expands
entry by entry. `Factorize` and `Differentiate` both already descended, being built out of rewrite
rules, so this was `Expand` being the odd one out rather than matrices being held back deliberately.

**Where a caller meets it.** `EquationSystem.Solve` returns a `Matrix`, so a solved system's entries
were left in whatever form the solver built them in:

```
MathS.Equations("x2 + y", "y - x - 3").Solve("x", "y").Simplify()

was:  [[-(-(5 - sqrt(-11)) / 2 + 3), (5 - sqrt(-11)) / 2], [-(-(5 + sqrt(-11)) / 2 + 3), (5 + sqrt(-11)) / 2]]
is:   [[-1/2 + -1/2 * sqrt(-11), 5/2 + -1/2 * sqrt(-11)], [1/2 * sqrt(-11) + -1/2, 1/2 * sqrt(-11) + 5/2]]
```

Issue [#882](https://github.com/asc-community/AngouriMath/issues/882), which
[#497](https://github.com/asc-community/AngouriMath/issues/497) names as the shape of defect to hunt:
the same input simplifying or not depending on its parent.

### A conditional set's bound variable is renamed to a temporary

`DirectChildren` of `{ x : P(x) }` renames the binder, so that the bound `x` is not read as an `x`
that may be free outside the set. The replacement name was four lowercase letters derived from the
predicate's hash code, and it was produced through `MathS.Var`, which **parses** — so a name that the
parser reads as something other than a variable did not come back as one.

| | was | is |
|---|---|---|
| `new ConditionalSet("x", "x > 0").DirectChildren[0]` | `abcd > 0`, a different name in every process | `%1 > 0` |
| one predicate in `26^4` | `CannotParseInstanceException: Cannot parse an instance of Variable from `true`` | `%1 > 0` |

Four lowercase letters can spell `true`. They can also spell a variable the predicate already uses, in
which case the rename captures it and the set means something else — silently, with no exception to
say so. The name now comes from `Variable.CreateTemp`, which reads the predicate's variables rather
than its hash and hands back `%1` upward, skipping what is taken: fresh by construction, and with no
reading as anything but a variable, since the parser does not accept `%`.

**Printed output does not change.** `Stringize` reads the node's own fields, so `{ x : x > 0 }` still
prints with its own binder, and `Solve` answers containing a conditional set are unaffected. Nothing
could have depended on the old name either, since string hash codes are randomised per process and it
was therefore different on every run — which is also why this surfaced as a CI failure that would not
reproduce.

`Variable.CreateRandom`, which had this one caller, is removed. It was `internal`.

Issue [#891](https://github.com/asc-community/AngouriMath/issues/891).

### `abs(sgn(x))` and `sgn(abs(x))` are not `1` at zero

`|sgn(z)|` and `sgn(|z|)` are `1` for every `z` except `0`, where both are `0`, because `sgn(0)` is
`0`. Both rewrites answered `1` for any argument.

| | was | is |
|---|---|---|
| `abs(sgn(x))`, `sgn(abs(x))` | `1` | left as written |
| the same at `x = 0` | `1` | `0` — which is the value |
| `abs(sgn(0))`, `sgn(abs(0))` | `0`, unchanged | |
| `abs(sgn(2))`, `sgn(abs(-3))` | `1`, unchanged | |
| `signum(abs(x/x))`, `abs(signum(x/x))` | `1 provided not x = 0` | left as written |

The rules consulted the argument's `DomainCondition`, which answers a different question from the one
they needed: a bare `x` is defined everywhere and can *be* zero, while `x / x` is defined only away
from zero and is nonzero throughout. Reading the domain conflated the two.

**A condition would not have been the fix**, for the same reason as in the two entries above: the
expression is defined at zero and equal to `0` there, so `1 provided not z = 0` would replace a wrong
value with a wrong domain. There is no closed form for "1 away from zero, 0 at it" other than the
expressions themselves, so the rules decide where the argument's value can be read and decline
otherwise.

**The last row is a coverage loss, not a correction.** `1 provided not x = 0` was right for `x / x`.
Separating it from the `x` case needs "nonzero throughout its domain", and neither test available in
`InnerSimplify` can express it — `Evaled` leaves `x / x` as `x / x`, and `DomainCondition` is what
conflated them; `Simplify` would answer it and cannot be called from there (#403).

Found by `boundcheck` after adding `x = 0` and `x = 1` to its sample points: 366 shapes had been
passing at 23 points chosen for branch cuts and principal intervals, none of which was the place
where a rule's own arithmetic degenerates. Issue
[#892](https://github.com/asc-community/AngouriMath/issues/892).

### `abs` folds where the sign of its argument is known

`|x|` is `x` for a non-negative real `x` and `-x` for a negative one. That is the definition of the
function rather than an identity with a side condition, and it was applied only when the argument
was a *number*. An argument whose value is a known real without its node being a number was left
alone, so a radical or a constant kept its `abs`:

| | was | is |
|---|---|---|
| `abs(-sqrt(6))` | left as written | `sqrt(6)` |
| `abs(-pi)`, `abs(-e)` | left as written | `pi`, `e` |
| `abs(1 - sqrt(2))` | left as written | `sqrt(2) - 1` |
| `abs(-2)` | `2` | `2`, unchanged |
| `abs(sqrt(-4))` | `2` | `2`, unchanged — the magnitude of `2i` |
| `abs(-a)` for symbolic `a` | left as written | left as written |

Where this shows up is in an answer built out of radicals. `(2x^2 - 3 > 0) and (x > 0)` solved to
`(abs(-sqrt(6)) / 2; +oo)` and now solves to `(sqrt(6) / 2; +oo)`; the endpoint was always the same
number, printed in a form that looked like unfinished work. The
[Solvers wiki page](https://github.com/asc-community/AngouriMath/wiki/Solvers) shows the old output
and wants updating with the release.

**Nothing is assumed about a symbol**, and an argument off the real line is declined rather than
guessed at: `sqrt(-4)` evaluates to `2i`, whose absolute value is `2` — neither the argument nor its
negation, so a rule that read "negative, therefore negate" would be wrong there. The sign is read
off the value, and a value that is not a finite real does not answer the question.

Issue [#881](https://github.com/asc-community/AngouriMath/issues/881).

### `MathS.ToSympyCode` emits Python that runs

Its documented purpose is code you can run in SymPy, and for two whole classes of expression it
emitted code that did not run at all.

| expression | was | is |
|---|---|---|
| `1/2`, `1/3 + 1/6` | `sympy.Rational(1, 2` — `SyntaxError: '(' was never closed` | `sympy.Rational(1, 2)` |
| `0/0`, `1/0` | `NaN` — `NameError: name 'NaN' is not defined` | `sympy.nan` |
| `+oo`, `-oo` | `+oo`, `-oo` — `NameError` | `sympy.oo`, `-sympy.oo` |

The first is a missing parenthesis, and it broke **every** expression carrying a non-integer rational,
which is most of what a computer algebra system hands back. The second is a value with no binding: the
generated preamble declares a `sympy.Symbol` for each free *variable*, and a `NaN` or an infinity is
neither a variable nor something SymPy names the same way this library does.

Both were checked by running the emitted programs against SymPy 1.14 rather than by reading them, which
is also how it was established that they now come back **exact** — `1/2` arrives as SymPy's `Half` and
not as the float `0.5`.

Nothing else about the exporter changes. `pi`, `e` and `i` were already emitted as `sympy.pi`,
`sympy.E` and `sympy.I`, and `sqrt` as `sympy.sqrt`.

Two tests now hold the properties that failed, without needing an interpreter in the suite: the
parentheses balance, and every name in the emitted body is either declared in the preamble or reached
through `sympy.`. 17 of their 23 cases fail against the old exporter.

Issue [#909](https://github.com/asc-community/AngouriMath/issues/909).

### `MathS.ToSympyCode` keeps an exact value exact

The generated program ran, and then quietly gave a different number. Python's `/`, and its `**` with a
negative exponent, are float operations on two integers:

| expression | emitted | SymPy read it as | now emitted | and reads as |
|---|---|---|---|---|
| `1/2` | `1 / 2` | `0.500000000000000`, a `Float` | `sympy.Integer(1) / 2` | `1/2`, a `Rational` |
| `2^(-1)` | `2 ** (-1)` | `0.5` | `sympy.Integer(2) ** (-1)` | `1/2` |
| `2^(-3)` | `2 ** (-3)` | `0.125` | `sympy.Integer(2) ** (-3)` | `1/8` |
| `x + 1/2` | `x + 1 / 2` | `x + 0.5` | `x + sympy.Integer(1) / 2` | `x + 1/2` |

Making one operand a SymPy integer hands the arithmetic to SymPy, which keeps it exact. **Only a pair of
integers is rewritten**, and the rest of the emitted code is unchanged: with a symbol anywhere in the
shape SymPy's own operators already take over (`x / 2`, `1 / x`, `x ** (-1)`), and `+`, `-`, `*` and a
non-negative `**` are exact on Python integers, whose precision is unbounded — `2 ** 70` was always
right.

It bit only the **unsimplified** form, which is the one a caller writes: `"1/2".ToEntity()` is a `Divf`
of two integers, because a printed rational parses back as a division
([#873](https://github.com/asc-community/AngouriMath/issues/873)), while a simplified `1/2` is a
`Rational` node and already emitted `sympy.Rational(1, 2)`.

Checked by running the emitted programs against SymPy 1.14, which is the only way this class of defect
shows itself — the code was always valid, so the earlier tests could not have caught it, and the two
added here assert the property instead: no two plain integer literals are combined with `/` or with a
negative `**`.

Issue [#911](https://github.com/asc-community/AngouriMath/issues/911).

### `NaN` is now a keyword, and the printed form of NaN reads back

`Stringize` prints the NaN value as `NaN`, and the grammar had no such token, so reading it back gave a
**variable of that name**. A variable behaves like any symbol — it cancels, collects and compares — and
nothing on the page distinguished the two, since a variable named `NaN` also prints as `NaN`:

| expression | was | is |
|---|---|---|
| `"NaN - NaN".Simplify()` | `0` | `NaN` |
| `"NaN / NaN".Simplify()` | `1 provided not NaN = 0` | `NaN` |
| `"NaN * 0".Evaled` | `0` | `NaN` |
| `"NaN * 2".Evaled`, `"NaN + NaN".Evaled` | left as written | `NaN` |
| `NaN` as a variable name | a variable | a parse of the NaN value |
| `NaNx`, `NaN_1`, `aNaN` | variables | variables, unchanged |

This is the same trade `mod` took above: a word that was an identifier becomes a keyword, so an
expression using it as a variable name now means something else. It is the narrower half of the trade —
only the exact spelling is reserved, because the lexer takes the longest match, so any longer name that
merely contains it is still a variable.

Only `NaN` is affected. Its two siblings already had tokens: `+oo` and `-oo` both print and parse, and
`Latexize` has had `\mathrm{undefined}` for all along — which is the token
[CSharpMath](https://github.com/verybadcat/CSharpMath) decodes back to `MathS.NaN`, so the LaTeX round
trip was closed already and is untouched here.

**The round-trip test now runs in both directions.** Every case in `StringizeRoundTripTest` began from a
*string*, so it could only reach expressions the parser already produces, and a value with no source
form was invisible to all of them however many cases were added. It now also enumerates the library's
named constants by reflection — `MathS` and `Entity.Number.Real` — prints each and reads it back, so a
constant added later is covered without anyone remembering the file exists. Those two cases are what
fail against the old grammar.

Issue [#906](https://github.com/asc-community/AngouriMath/issues/906).

---

## 2.0.0 — since 1.4.0

### At a glance

| Silent? | What | Was | Is |
|---|---|---|---|
| loud | `Minusf.Minuend` / `.Subtrahend` | named for the wrong operand | named for the right one |
| **silent** | `exp(x)`, `log10(x)`, `log2(x)` | products of undeclared variables | the functions |
| loud | `arcsinh(x)` and its five relatives | a product | `UnrecognizedFunctionParseException` |
| loud | `floor(x)` and ten other names the library lacks | a product, silently | `UnrecognizedFunctionParseException` |
| **silent** | `sqrt(x^2)`, `sqrt(-x)` and their kind | `x`, `i*sqrt(x)` — wrong for negative x | left as written |
| loud | `mod` as a variable name | a variable | a keyword, so a parse error |
| **silent** | `Stringize` of powers, lambdas, applications, piecewises | did not parse back | parses back |
| **silent** | `Stringize` of a complex number with a fractional imaginary part | read back as its negation, or as a power | parses back |
| **silent** | numbers below `1e-16` | rounded to `0` | kept |
| **silent** | `Real` `%` | `-7 % 3` was `-1` | `2` |
| **silent** | `Simplify` of a cancelling quotient | the quotient | a `Providedf` |
| **silent** | radicals, everywhere | `sqrt(12)` | `2 * sqrt(3)` |
| **silent** | `Expand` | left like terms uncollected | collects them |
| **silent** | an identity equation | `{ }` or `{ 0 }` | all of `CC` |
| **silent** | a quadratic inequality | wrong for one sign of a symbolic coefficient | a case split on that sign |
| **silent** | numeric root sets | one root per starting point | one root per root |
| **silent** | many limits | `NaN`, or unevaluated | a value |
| **silent** | a vanishing sine behind a constant factor | `NaN` | a value — but `sin(x)*ln(x)*2` loses its `0` |
| **silent** | a limit assembling `oo^0` or `1^oo` | that form's value, `1` | the real answer, or unevaluated |
| **silent** | a factorial under a vanishing exponent | unevaluated | a value, by Stirling |
| **silent** | `ln(x!)` in a limit | unevaluated, and `ln(x!)/ln(x)` was `NaN` | a value |
| **silent** | a boolean expression | factored, not minimised | minimised where that is shorter |
| **silent** | some integrals | not antiderivatives | closed forms |
| **silent** | two integrals | answered correctly | unevaluated — a deliberate loss |
| **silent** | `k/(a x^2 + c)` and `k/sqrt(a x^2 + c)` for symbolic `a` | `NaN` | a piecewise on the sign of the discriminant |
| loud | `Compile` over a missing variable | `KeyNotFoundException` | `UncompilableNodeException` |
| loud | `Expand` of a quotient of factorials | `AngouriBugException` | the expanded polynomial |
| loud | parsing a `provided` in a parenthesised comma list | `NullReferenceException` | `UnhandledParseException` |
| loud | `floor(x)`, `ceil(x)`, `ceiling(x)` | `UnrecognizedFunctionParseException` | the functions |
| loud | `round(x)`, `min(a, b)`, `max(a, b)`, `gcd(a, b)` | `UnrecognizedFunctionParseException` | the functions |
| loud | 28 members deprecated since 1.x | obsolete but present | removed |
| loud | `Latexise`, `ILatexiseable`, `entity_latexise` | the British spelling | `Latexize`, `ILatexizeable`, `entity_latexize` |
| loud | `MathS.Quantum.Factorise` | one letter from the unrelated `Entity.Factorize` | `MathS.Quantum.TensorFactorize` |
| loud | `MathS.Quantum.IsNormalised` | the British spelling | `MathS.Quantum.IsNormalized` |
| loud | the target frameworks | `net7.0;netstandard2.0` | `netstandard2.0;net8.0;net10.0` |
| **silent** | `abs(x) = c` for a negative `c` | a set of non-solutions | the empty set |
| loud | implicit `List<Entity>` to `Entity` | made a `FiniteSet`, and made three `params` overloads uncallable | removed |
| loud | implicit `Entity[]` to `Entity` | made a `FiniteSet`, discarding order and repeats | removed |
| loud | implicit `(Entity, Entity)` to `Entity` | made a **closed** interval, though `(a, b)` reads as the open one | removed |
| **silent** | a `MathS.Settings` scope across an `await`, or inside a task | lost, or somebody else's | follows the call |
| **silent** | a `RewriteRecording` across an `await`, or work started under it | lost, or somebody else's | follows the call |
| loud | a polynomial system with more equations than unknowns | `WrongNumberOfArgumentsException` | solved |
| loud | a known gap, e.g. a cubic inequality | `AngouriBugException`, asking to be reported | `NotSufficientlySupportedException` |
| **silent** | `arcsin(sin(x))` and three siblings | `x`, wrong wherever `x` leaves the principal interval | left as written unless `x` is a real in that interval |

---

## Target frameworks

`net7.0` is replaced by `net8.0` and `net10.0`. `netstandard2.0` is unchanged, so consumers on
.NET Framework and the older runtimes are unaffected.

| | was | is |
|---|---|---|
| `TargetFrameworks` | `net7.0;netstandard2.0` | `netstandard2.0;net8.0;net10.0` |

.NET 7 left support in May 2024, so the old list named one framework nobody should still be
targeting and no supported modern one. A consumer on net8.0 or net10.0 previously resolved the
`netstandard2.0` asset and silently lost the generic-math surface with it; they now get a real
target.

**What breaks.** Anything that pins the framework of its reference:

```xml
<ProjectReference Include="...\AngouriMath.csproj">
  <SetTargetFramework>TargetFramework=net7.0</SetTargetFramework>   <!-- no longer resolves -->
</ProjectReference>
```

fails with `NETSDK1005: Assets file ... doesn't have a target for 'net7.0'`. Change it to `net10.0`
or `net8.0`. This is not hypothetical -- every one of the nine measurement harnesses in this
workspace pinned `net7.0` and had to be repointed.

A consumer that references the NuGet package rather than the project picks its asset by its own
framework and needs no change, unless it targets `net7.0` itself, in which case it now resolves
`netstandard2.0` and loses the generic-math types.

**Generic math is unchanged in reach.** `Core/Entity/GenericMath` needs `INumber<T>` and so is
still compiled only into the net7.0-and-later targets -- now written as a compatibility test
rather than a literal `!= 'net7.0'`, so adding a framework does not silently drop it. Verified on
the built assemblies: present in `net8.0` and `net10.0`, absent from `netstandard2.0`.

---

## Types and members

### The implicit conversions from a collection are removed

`Entity` had two implicit conversions from a collection, both building a `FiniteSet`:

```csharp
public static implicit operator Entity(Entity[] elements);       // removed
public static implicit operator Entity(List<Entity> elements);   // removed
```

The second one made three public members impossible to call with a `List<Entity>`:

```csharp
var equations = new List<Entity> { "x - 1", "y - 2" };
new EquationSystem(equations);        // error CS0121: the call is ambiguous
new FiniteSet(equations);             // error CS0121
MathS.Equations(equations);           // error CS0121
```

Each of those has both an `IEnumerable<Entity>` overload and a `params Entity[]` overload. With
the conversion in place a `List<Entity>` also converts to a single `Entity`, so the `params`
overload becomes applicable in its expanded form, neither candidate is better than the other,
and the call does not compile. The array, `IEnumerable<Entity>`, and variadic forms were always
fine — only a concrete `List<Entity>` broke, which is why it survived to a 2.0 preview.

This was not three separate defects. One conversion made *every* `params Entity[]` overload in
the library uncallable with a list, including any added later.

**This one moves between previews.** The conversion is in `2.0.0-preview.1` and
`2.0.0-preview.2`, both published, and is gone in `2.0.0`. It is the only entry here that
changes between two previews rather than between releases: a reader coming from 1.3.0 or
1.4.0 can ignore the distinction, one already on a preview cannot.

**What breaks.** Assigning a list where an `Entity` is expected:

```csharp
Entity set = new List<Entity> { 1, 2, 3 };   // no longer compiles
```

Write one of these instead:

```csharp
Entity set = new FiniteSet(new List<Entity> { 1, 2, 3 });
Entity set = new List<Entity> { 1, 2, 3 }.ToSet();
```

**The conversion from `Entity[]` is gone as well.** It was kept at first, on the narrow
ground that it never produced the ambiguity — an array binds to the `params` overload in its
normal form by an identity conversion, which wins outright. That was true and beside the
point. An array carries an order and can repeat an element; a set has neither, so the
conversion silently discarded part of what it was handed, and an implicit conversion that
loses information is the wrong shape regardless of which overloads it happens to break.
Set types are built explicitly nearly everywhere for this reason.

```csharp
Entity set = new Entity[] { 1, 2, 3 };          // no longer compiles either
Entity set = new FiniteSet(1, 2, 3);            // say it
Entity set = new Entity[] { 1, 2, 3 }.ToSet();
```

Only one place in the whole repository relied on it, which was the test pinning it.

**And the conversion from a pair, which had the same fault from the other side.** A
two-element tuple became an `Interval`, and since a tuple says nothing about whether its
endpoints are included, the conversion had to supply that:

```csharp
Entity e = ((Entity)1, (Entity)5);
// was: [1; 5] — both endpoints included, and 1 is a member
```

Where the array conversion dropped information that was there, this one produced
information that was not — and chose the reading opposite to the notation, since `(1, 5)`
is the open interval in ordinary mathematical writing and `[1, 5]` the closed one. A caller
writing what looks like an open interval got a closed one, silently.

```csharp
Entity e = MathS.Interval(1, 5);                 // closed, said out loud
Entity e = MathS.Interval(1, false, 5, false);   // open
Entity e = new Interval(1, true, 5, true);
```

Nothing in the library, the tests, the F# wrapper or the utilities used it.

The pairs elsewhere in the API are unaffected, because none of them has to guess: an
integration `Range`, the arguments of `Substitute`, the cases of `MathS.Piecewise` and
`ToProvided` are all ordered pairs whose two halves have distinct, stated roles.

### An inverse trigonometric function no longer cancels its own function off the principal branch

`arcsin` is a left inverse of `sin` only on `[-pi/2, pi/2]`, and the three siblings likewise only on
their own intervals. The rewrite was unconditional, so `Simplify` returned a value that is wrong at
every real point outside the interval:

| | was | is | the value at that point |
|---|---|---|---|
| `arcsin(sin(x))` | `x` | left as written | `pi - x` for `x` in `[pi/2, 3pi/2]`, and so on |
| `arccos(cos(x))` | `x` | left as written | `2*pi - x` for `x` in `[pi, 2pi]` |
| `arctan(tan(x))` | `x` | left as written | `x - pi` for `x` in `(pi/2, 3pi/2)` |
| `arccotan(cotan(x))` | `x` | left as written | `x - pi` for `x` in `(pi, 2pi)` |

Measured on a build of `35f4ae5a` before this change and after it. The four rules are older than
1.4.0 and untouched since, so 1.4.0 answers the same way, but the numbers below are from the two
2.0.0 builds rather than from a 1.4.0 one:

```
"arcsin(sin(x))".Simplify().Substitute("x", 3).EvalNumerical()
  was  3
  is   0.14159...        which is pi - 3, and is what arcsin(sin(3)) equals

"arctan(tan(x))".Simplify().Substitute("x", 2).EvalNumerical()
  was  2
  is   -1.14159...       = 2 - pi

"arccos(cos(x))".Simplify().Substitute("x", 4).EvalNumerical()
  was  4
  is   2.28318...        = 2*pi - 4
```

**Where the argument is a real number inside the interval, the cancellation still happens and stays
exact**: `arcsin(sin(1/2))` is `1/2`, not a decimal. Where it is symbolic, the expression is left as
written, which is what SymPy and Mathematica both answer.

A condition was deliberately *not* attached. `arcsin(sin(x))` is defined for every real `x`, so
`x provided x >= -pi/2 and x <= pi/2` would say the expression is undefined outside the interval
when in fact it merely has another value — trading a wrong value for a wrong domain.

**The other direction is unchanged**, and needs no assumption: `sin(arcsin(z))`, `cos(arccos(z))`,
`tan(arctan(z))` and `cotan(arccotan(z))` are all `z`, because they compose the *right* inverse.

`CircleTest.Test8` asserted `arccotan(cotan(3x)) == 3x` and was pinning the wrong answer; it now
asserts that the expression is left alone. Issue
[#884](https://github.com/asc-community/AngouriMath/issues/884).

### A known gap no longer presents as a bug

`FutureReleaseException` is removed, and the twelve places that threw through it now throw
`NotSufficientlySupportedException`.

It worked by naming the release a feature was planned for:

```csharp
throw FutureReleaseException.Raised("Inequalities are not implemented yet", "1.2.1");
```

and turning *itself* into an `AngouriBugException` once that release had shipped — message
ending *"please report about it to the official repository"*. Every site named 1.2, 1.2.1 or
1.3. All three shipped years ago, so twelve known gaps were inviting bug reports about work
nobody had started:

```csharp
("x3 - x > 0").Solve("x");
// was: AngouriBugException — "Inequalities are not implemented yet was planned for 1.2.1
//      but hasn't been released by 1.2.1 ... please report about it"
// is:  NotSufficientlySupportedException — "Only linear and quadratic polynomial
//      inequalities are supported; this one is of a higher degree"
```

An unbuilt feature is not a bug, and a caller cannot act on being told to report one.

**What breaks.** `FutureReleaseException` is gone from the public surface; catch
`NotSufficientlySupportedException`, or `AngouriMathBaseException` for both. Code catching
`AngouriBugException` around any of these gaps no longer catches them.

The messages changed too, and deliberately: they were notes to a maintainer — *"We should
be able to return sets from invertnode"* — and are now what is not supported, since they
are the only thing a caller sees.

### A settings scope belongs to the call, not to the thread

`MathS.Settings` values were held in `[ThreadStatic]` fields — fourteen of them. A scope
therefore stopped at the first `await`:

```csharp
using var _ = MathS.Settings.MaxExpansionTermCount.Set(1);
Console.WriteLine(MathS.Settings.MaxExpansionTermCount.Value);   // 1
await Task.Delay(20).ConfigureAwait(false);
Console.WriteLine(MathS.Settings.MaxExpansionTermCount.Value);   // was 2000, the default
```

The continuation resumed on a pool thread that had never seen the scope, so the setting
silently read its default. The same mechanism ran the other way too: a thread returned to
the pool still carried whatever scope was left on it, so the *next* caller to borrow that
thread could compute under a precision or codomain it never asked for. Neither shows up as
an error; both change the answer.

The values now live in an `AsyncLocal`, which is what the cancellation token in
`MathS.Multithreading` already used.

| | was | is |
|---|---|---|
| a scope across an `await` | lost | kept |
| a scope inside `Task.Run` started under it | not inherited | **inherited** |
| a scope opened in a task, seen by a sibling | no | no |
| a scope opened in a task, seen after it ends | no | no |
| a thread reused by the pool | could carry a stale scope | cannot |

**What breaks.** The second row is the one to read. Work started inside a scope now runs
under it:

```csharp
using var _ = MathS.Settings.Codomain.Set(Domain.Real);
await Task.Run(() => expr.Solve("x"));   // now solves over R; used to solve over C
```

That is what the code says, and almost always what was meant — but if you parallelised
inside a scope and relied on the child *not* seeing it, it does now. Move the scope inside
the callback to keep the old behaviour.

**Cost.** Measured over 20 000 000 reads and 2 000 000 scopes:

| | was | is |
|---|---|---|
| read `PrecisionErrorZeroRange.Value` | 7.3–8.0 ns | **1.2 ns** |
| read `DowncastingEnabled.Value` | 0.79 ns | 0.96 ns |
| `Set()` + `Dispose()` | 388–395 ns, 32 B | **46 ns**, 112 B |
| a `Simplify` workload | 5.6–5.8 s | 5.5–5.6 s, +1.5 % allocated |

Reads got faster rather than slower: the old getter re-tested a `[ThreadStatic]` field for
null on every access, and that is dearer than the async-local lookup that replaced it.
Opening a scope got much cheaper because it no longer mints a `Guid` to identify itself,
though it allocates more, since assigning an `AsyncLocal` copies the flow's value map. Reads
outnumber scope openings by orders of magnitude in any real workload.
### A rewrite recording belongs to the call, not to the thread

`RewriteRecording` held its ambient scope in a `[ThreadStatic]` field, and documented the
consequence rather than fixing it:

> **A synchronous scope, and it has to be.** Do not `await` inside one.

It no longer has to be. The scope is an `AsyncLocal`, as `MathS.Settings` and the
cancellation token in `MathS.Multithreading` are, so it survives an `await` and work
started under it reports to it wherever it runs.

| | was | is |
|---|---|---|
| a recording across an `await` | lost, and the thread could collect a stranger's rewrites | kept |
| work started under a recording, on another thread | **not** collected | collected |
| two flows each with their own recording | separate | separate |
| a recording opened inside a task, seen after it ends | no | no |

**What breaks.** The second row. A recording no longer stops at the thread boundary:

```csharp
using var recording = RewriteRecording.Start();
var t = new Thread(() => expr.Simplify());
t.Start(); t.Join();
recording.Steps;   // used to be empty of that work; now contains it
```

That is what the code says and what the scope is for, but a caller who fanned out inside a
recording and expected only their own thread's rewrites will now see everything. Open the
recording inside the callback to keep the old behaviour.

**`Steps` is now a snapshot.** It was a live view of the underlying list; because the store
has to tolerate concurrent writers it is a `ConcurrentQueue`, and `Steps` copies out of it.
Reading it after disposing — the documented use — is unchanged. Holding the returned list
across further recording and expecting it to grow no longer works.

**Order across parallel work is not defined.** Steps from one flow keep the order they fired
in; two flows recording into the same recording interleave however they happen to run. The
single-threaded case, which is what `Simplify` is, is unaffected.

Being off is still free: no recording open still costs one ambient read per rule set and
allocates nothing, which `RewriteAllocationTest` continues to hold to.
### An over-determined polynomial system is solved rather than refused

`EquationSystem.Solve` insisted on as many equations as unknowns and threw otherwise:

```csharp
MathS.Equations("x^2 + y^2 - 25", "x + y - 7", "x*y - 12").Solve("x", "y");
// was: WrongNumberOfArgumentsException
// is:  the two solutions, (3, 4) and (4, 3)
```

The count was a consequence of how the old solver worked — it eliminated one variable per
equation — and not of the problem. A Gröbner basis has no use for the equality, and an
extra equation that happens to be a consequence of the others is not an error to report.

An inconsistent system now reports itself as one, which it also could not do before:

```csharp
MathS.Equations("x^2 + y^2 - 25", "x + y - 7", "x*y - 99").Solve("x", "y");
// was: WrongNumberOfArgumentsException
// is:  null — no solutions
```

**What breaks.** Code that catches `WrongNumberOfArgumentsException` to detect a
malformed system will no longer see it for the polynomial case. **Fewer** equations than
unknowns still throws: a free variable means infinitely many solutions, which this does
not enumerate.

The relaxation only applies where the system is a polynomial one over `Q` in at most eight
variables and its solutions are rational. Everything else reaches the previous solver
exactly as before, including the equation-count check.

### `Minusf`'s two operands exchanged names

In `a - b`, `a` is the minuend and `b` is the subtrahend. The record declared them the other way
about, so the property called `Subtrahend` held the operand on the left of the sign:

```csharp
var m = (Minusf)"a - b".ToEntity();
m.Minuend      // was b, is a
m.Subtrahend   // was a, is b
```

The operand order in the constructor and in every compiled form is unchanged — the objects are the
same and only the names moved. Code that reads either property **by name** now gets the other
operand and will not fail to compile.

This is not cosmetic. It is what caused the sign error below: the inverter was written to what the
names say rather than to which operand is which, so `a = 1 / (b - c)` solved to the negation of the
right answer. Code written against the old names was wrong in the same way.

[#632](https://github.com/asc-community/AngouriMath/issues/632), PR
[#664](https://github.com/asc-community/AngouriMath/pull/664).

### `Latexise` is now `Latexize`

The library serialises a node to text two ways, and spelled them in two languages:

| | was | is |
|---|---|---|
| the method | `Latexise()` | `Latexize()` |
| the interface | `ILatexiseable` | `ILatexizeable` |
| the string extension | `"x + 1".Latexise()` | `"x + 1".Latexize()` |
| the C# helper | `MathS.Latex(ILatexiseable)` | `MathS.Latex(ILatexizeable)` |
| the native entry point | `entity_latexise` | `entity_latexize` |
| the C++ method | `Entity::Latexise()` | `Entity::Latexize()` |
| the F# function | `latexise` | `latexize` |

Everything else in the public surface is `-ize` — `Stringize`, `Factorize`, `Serialize`,
`Deserialize` — so `Latexise` and its interface were the only members a caller had to remember the
other spelling for. `Stringize` and `Latexise` do the same kind of thing and sit next to each other
in every file that has either, which is what made the split spelling costly rather than merely
untidy.

**What breaks.** Every call, every override, and every implementation of the interface. There is no
forwarding member: this release is the one that removed 28 members that accumulated as forwarders,
so adding a permanent one here would undo that on the same day. The compiler reports each site, and
the fix is mechanical.

**Downstream.** [`CSharpMath.Evaluation`](https://github.com/verybadcat/CSharpMath) reads LaTeX
produced here back into an `Entity` and calls this method. It is unaffected until it moves to 2.0,
at which point it needs the new name.

[#840](https://github.com/asc-community/AngouriMath/issues/840).

### `MathS.Quantum.Factorise` is now `MathS.Quantum.TensorFactorize`

| | was | is |
|---|---|---|
| the method | `MathS.Quantum.Factorise(Entity)` | `MathS.Quantum.TensorFactorize(Entity)` |

The old name differed by one letter from `Entity.Factorize`, which does something else entirely —
algebraic factoring of an expression, against tensor factorisation of a quantum state. Both take an
`Entity` and return an `Entity`, so nothing but the name distinguished them, and the name barely
did.

Renaming it to `Factorize` would have made the spelling uniform and the API worse: two public
`Factorize` methods doing unrelated things. `MathS.Quantum` already holds the inverse operation as
`TensorExpand`, so `TensorFactorize` states the domain and the direction, pairs with its inverse,
and ends the collision. The spelling is fixed as a side effect rather than as the point.

**What breaks.** Every call to `MathS.Quantum.Factorise`. There is no forwarding member, for the
same reason as above. The compiler reports each site.

`MathS.Quantum.IsNormalised` is renamed to `IsNormalized` in the same pass. It carries no collision,
only the spelling — but it is a public member, so 2.0 is equally the last release that can change
it, and leaving it would have meant shipping one British member after two renames made expressly to
remove them.

With these two, the public surface has one spelling throughout. That is checkable rather than
asserted: `PublicApi.txt` is the recorded surface, and it now contains no `-ise`, `-ised` or
`-isation` member.

[#843](https://github.com/asc-community/AngouriMath/issues/843).

---

## Parsing

Four names that looked like functions were not in the grammar, and none of them failed. Each fell
through to implicit multiplication — the rule that lets `a(b + c)` mean `a * (b + c)` — and came back
as a product with an undeclared variable, silently, propagating into an answer that looked like one.

### `exp(x)` is the exponential

| | |
|---|---|
| was | `exp * x`, a product with an undeclared variable named `exp` |
| is | `e ^ x` |

`exp(x) - 3x = 0` answered `{ 0 }`, which is a root of `exp * x - 3x` and of nothing else. It now
answers the equation's actual roots, the real ones near `0.6191` and `1.5121` among them.

It maps to `MathS.Pow(MathS.e, arg)` rather than to a node of its own, so `exp(x) * exp(y)` comes
out `e ^ (x + y)` with nothing further added. Only the exact name followed by a bracket is the
function: `expr(x)`, `expo(x)` and a bare `exp` are the products they were.

[#730](https://github.com/asc-community/AngouriMath/issues/730), PR
[#731](https://github.com/asc-community/AngouriMath/pull/731).

### `log10(x)` and `log2(x)` are logarithms

| | |
|---|---|
| was | `log ^ 10 * 100` for `log10(100)` — `log10` lexes as the variable `log` followed by `10`, and `x2` means `x^2` by design |
| is | `2` |

Both map to the `log` the library already had, so `log10(x)` and `log(10, x)` parse to one tree.
Without a bracket nothing changed: `log2x` is still `log ^ 2 * x`, and `logx(y)` and `log3(x)` are
still the implicit products they were.

[#733](https://github.com/asc-community/AngouriMath/issues/733), PR
[#734](https://github.com/asc-community/AngouriMath/pull/734).

### `arcsinh` and its five relatives are refused

| | |
|---|---|
| was | `arcsinh * x`, silently |
| is | `UnrecognizedFunctionParseException` |

The inverse hyperbolic functions are *area* functions, not arc functions, so `arcsinh` is not
another name for `arsinh` — it is not a name for anything. The same for `arccosh`, `arctanh`,
`arccotanh`, `arcsech` and `arccosech`. The exception names the spellings that do work:

> there is no function arcsinh: the inverse hyperbolic functions are area functions, not arc
> functions, so the inverse hyperbolic sine is arsinh, asinh or arsh

This one refuses where `exp` was adopted, because `exp` names something the library has and
`arcsinh` names nothing. The twenty-one spellings the grammar already accepted still parse, `arcsin`
and its five relatives are correct and untouched, and a name that merely begins the same way —
`arcs(x)`, `arcsinhh(x)` — is still the product it was.

PR [#687](https://github.com/asc-community/AngouriMath/pull/687).

### Eleven function names the library does not have are refused

| | |
|---|---|
| was | `floor * x`, silently |
| is | `UnrecognizedFunctionParseException` |

`floor`, `ceil`, `ceiling`, `round`, `trunc`, `min`, `max`, `gcd`, `lcm`, `erf` and `conjugate` are
each what some other CAS calls a function, and AngouriMath has none of them. A name the grammar does
not know, followed by a bracket, is the implicit multiplication that lets `a(b + c)` mean
`a * (b + c)` — so each came back as the product of an undeclared variable with its argument, and
nothing said so. What that cost:

```csharp
"floor(x) - 3 = 0".Solve("x")     // was { 3 / floor }, a root of nothing
```

Two things are worth knowing before you read this as a loss. First, it is the **same absence** that
was already reported for two arguments, only reported the same way now: `min(x, y)` did raise, but
with `no viable alternative at input '*('` from the parser generator, so whether a missing function
was invisible or merely cryptic depended on how many arguments the caller happened to pass. Second,
none of these is being removed — none was ever there.

Refusing *every* unknown name is not an option, because that is what `a(b + c)` is. So these are
refused one at a time, as `arcsinh` above is, and everything else is untouched: each name on its own
is still an ordinary variable (`min + 1`, `floor + floor`), and a longer name beginning the same way
is still a product (`minimum(x)`, `rounded(x)`, `maxx(y)`).

`re` and `im` are deliberately **not** refused although they misparse identically. Two letters is
short enough that a caller may reasonably have a variable of that name, and refusing would break
their expression to fix nobody's.

[#733](https://github.com/asc-community/AngouriMath/issues/733), PR
[#750](https://github.com/asc-community/AngouriMath/pull/750).

### `sqrt(x^2)` is no longer `x`, and `sqrt(-x)` is no longer `i * sqrt(x)`

| | |
|---|---|
| was | `x`, and `i * sqrt(x)` |
| is | left as written |

Both were wrong, and by a whole sign rather than a rounding:

```csharp
"sqrt(x ^ 2)".Simplify()      // was x       — at x = -0.63 that is -0.63 where it is 0.63
"(x ^ 2) ^ (3/2)".Simplify()  // was x ^ 3   — at x = -2 that is -8 where it is 8
"sqrt(-x)".Simplify()         // was i*sqrt(x) — at x = -0.63 that is -0.7937, not 0.7937
```

Two rules were being applied without the condition each needs. `(a^b)^c = a^(b*c)` holds for
a positive `a`, and for any `a` when `c` is a whole number — `(a^b)^3` is `a^b` multiplied by
itself three times however `a` is signed. `(a*x)^c = a^c * x^c` holds for a positive `a`, and
again for a whole `c`. Both are now restricted to exactly that, and unchanged where they hold:
`(x^(1/2))^2` is still `x`, `(-x)^2` is still `x^2`, `(2^2)^(1/2)` is still `2`.

`sqrt(x^2)` is `abs(x)`, and the library does not write that for you — it leaves the
expression alone rather than answering something false. Writing `abs` requires knowing the
expression is real, which is what
[#719](https://github.com/asc-community/AngouriMath/issues/719)'s codomain now makes sayable
and is not yet read by the simplifier.

**One thing got worse, and it is worth stating plainly.** The inequality solver was leaning
on `sqrt(4a^2) = 2a`: `(x - a)(x + a) <= 0` answered `[a; -a]`, which is right for `a < 0`
and empty for `a > 0`. It now answers `{ a, -a } \/ (sqrt(a^2); -sqrt(a^2))`, whose interval
is empty for either sign. Neither form is right for both, because the solver has no case
split on the sign of a symbolic coefficient — that is
[#757](https://github.com/asc-community/AngouriMath/issues/757), and it was invisible while an
unsound rewrite was making it look right half the time.

Found by `work/simpsweep`, which generates the expressions it checks; its count of
disagreements over 10463 expressions goes **30 to 0**.

[#752](https://github.com/asc-community/AngouriMath/issues/752), PR
[#758](https://github.com/asc-community/AngouriMath/pull/758).

### `mod` is now a keyword

| | |
|---|---|
| was | `mod + 1` parsed, `mod` being an ordinary variable; `7 mod 3` parsed as `7 * mod ^ 3` |
| is | `mod` is the remainder operator; `mod + 1` is an `UnhandledParseException` |

There was no remainder in the library at all before this. **An expression using `mod` as a variable
name no longer parses** — rename the variable.

`%` is deliberately *not* the parser's spelling, so that it stays free to mean percent, which is
what it means in mathematical writing. `"7 % 3"` is still a parse error. The `%` operator on
`Entity` in C# and F# is a different thing and does exist; see below.

[#402](https://github.com/asc-community/AngouriMath/issues/402),
[#618](https://github.com/asc-community/AngouriMath/issues/618), PR
[#703](https://github.com/asc-community/AngouriMath/pull/703).

### `pow(a, b)` parses

`pow(2, 3)` raised `UnhandledParseException`: `pow` was lexed as the implicit product `p*o*w`, after
which `(2, 3)` was not a valid operand. It is now `8`. Additive.

[#625](https://github.com/asc-community/AngouriMath/issues/625), PR
[#663](https://github.com/asc-community/AngouriMath/pull/663).

---

## Printing

`Stringize`'s contract is that parsing what it prints gives back what was printed. Four kinds of
expression broke it, and broke it silently, since a wrong reading is usually still a valid
expression. **If you compare `Stringize` output against fixed strings, these four changed.**

| expression | was printed | reads back as | is printed |
|---|---|---|---|
| `(2 ^ 3) ^ 2` | `2 ^ 3 ^ 2` | `512`, where the expression is `64` | `(2 ^ 3) ^ 2` |
| `lambda(x, x + 1)` | `x -> x + 1` | `x implies x + 1` | `lambda(x, x + 1)` |
| `apply(f, 2)` | `f 2` | a power | `apply(f, 2)` |
| `piecewise(1 provided x > 0)` | `(1 if x > 0)` | a product, `if` being an undeclared variable | `piecewise(1 provided (x > 0))` |
| `i / 2` evaluated | `1/2i` | `1 / (2i)`, which is its **negation** | `1/2 * i` |
| `2 + 3 * i / 4` evaluated | `2 + (3/4)i` | `2 + (3/4) ^ i`, a different number | `2 + 3/4 * i` |

Powers group to the right, so it is the base that needs bracketing when it is a power of its own —
the mirror of the rule the left-associative operators use. The next three have no operator spelling
in the grammar, so they now print as the function call the parser does have.

The last two are one defect. A complex number whose imaginary part is a fraction was printed with
the fraction pushed up against the `i`, and `1/2i` is read as `1 / (2i)` — the negation of what was
printed. The case with a real part bracketed the coefficient, which looks like a guard and is not:
a bracket followed by a name is read as exponentiation, so `(3/4)i` came back as `(3/4) ^ i`. Both
now multiply explicitly, which needs no bracket since `*` and `/` bind tighter than `+`. Integer
coefficients never had the problem and are unchanged — `i`, `-i` and `2i` print as before.

This one is worth reading twice if you parse printed output, because the misreading is a perfectly
ordinary number: every root of a trigonometric equation prints through this path, and
`cos(x)^2 + sin(x) = 0` returns roots that are exact as entities and were not as text.

`Latexize` is unaffected by this change. Its output is under a weaker obligation, not none:
nothing in this repository parses LaTeX back, but [`CSharpMath.Evaluation`](https://github.com/verybadcat/CSharpMath)
does, so a change to what it emits can break a downstream project with no test here to say so
([#822](https://github.com/asc-community/AngouriMath/issues/822)).

The syntax the parser accepts is now written down in
[`Sources/AngouriMath/Docs/Usage/Syntax.md`](Sources/AngouriMath/Docs/Usage/Syntax.md).

PR [#706](https://github.com/asc-community/AngouriMath/pull/706).

---

## Numbers and precision

### Small numbers are no longer rounded to zero

Downcasting rounded a computed value onto a nearby integer whenever the two were within
`PrecisionErrorZeroRange`, whose default is `1e-16`. Numbers are evaluated to
`DecimalPrecisionContext` digits — 100 by default — so `1e-16` sat nowhere near the noise floor, and
being an *absolute* threshold it swallowed every value whose integer part was zero.

| | was | is |
|---|---|---|
| `"1e-20".EvalNumerical()` | `0` | `1E-20` |
| `"e^(-40)".EvalNumerical()` | `0` | `4.248...E-18` |

The tolerance is now derived from the working precision — half its digits — whenever nobody has set
`PrecisionErrorZeroRange` explicitly. Residuals genuinely left over by exact cancellation still round
away with room to spare: `sin(pi)` is `0`, and such residuals measure around `1e-99`.

**If you depended on tiny results collapsing to zero**, set the setting explicitly and it is honoured
verbatim, as it was before:

```csharp
MathS.Settings.PrecisionErrorZeroRange.As(1e-16m, () => "e^(-40)".EvalNumerical());  // 0
```

That restores the old rounding for *computed* values. A parsed literal such as `1e-20` is exact and
never went through the rounding, so it stays `1e-20` either way.

[#602](https://github.com/asc-community/AngouriMath/issues/602), PR
[#663](https://github.com/asc-community/AngouriMath/pull/663).

### Logarithms of extreme powers

`ln(2^1000) / ln(2^(-1000))` answered `0`; it is `-1`. A `Rational` holds both its exact ratio and a
decimal form, the decimal is rounded into `DecimalPrecisionContext` when the number is built, and
that context runs from `10^(-100)` to `10^1000` — so `2^(-1000)` flushed to zero and `2^10000`
saturated to `+oo`. Both logarithms were computed off the decimal and reported `-oo` and `+oo` for
answers that are perfectly ordinary numbers, after which the quotient of two infinities collapsed.

Where the decimal has lost the ratio this way, `Ln` and `Log` now work from the ratio itself.
Numbers whose decimal form is intact take exactly the path they took before.

[#210](https://github.com/asc-community/AngouriMath/issues/210), PR
[#663](https://github.com/asc-community/AngouriMath/pull/663).

### `MathS.ToBaseN`

| | was | is |
|---|---|---|
| `ToBaseN(13.125m, 5)` | never returned | `23.0303030303…`, bounded by the configured precision |
| `ToBaseN(0.5m, 2)` | `".1"` | `"0.1"` |
| `ToBaseN(0m, 2)` | `""` | `"0"` |

The digit loop ran until the fractional part hit zero, which for a repeating expansion it never
does. It is now bounded by the number of base-N digits that carry the configured decimal precision,
so terminating expansions stay exact and repeating ones stop. A zero integer part rendered as the
empty string; it renders as `0`.

[#584](https://github.com/asc-community/AngouriMath/issues/584), PR
[#663](https://github.com/asc-community/AngouriMath/pull/663).

### Vector norms evaluate numerically

`MathS.FromString("(|[12,15] - [0,0]|)").EvalNumerical()` threw `CannotEvalException` on 1.4.0 where
1.3.0 returned a value — the difference being whether the norm came out a perfect square. The
vector branch of `Absf` always finished with `InnerSimplified`, ignoring the `isExact` flag its
sibling branches respect, so `sqrt(369)` survived as a `Powf` and `EvalNumerical` rejected it. The
exact path still returns a symbolic answer and the numeric path now evaluates it.

[#662](https://github.com/asc-community/AngouriMath/issues/662), PR
[#664](https://github.com/asc-community/AngouriMath/pull/664).

---

## Remainder

The three numeric types answered three different ways, and two of the answers were wrong. Which one
applied depended on the static type at the call site rather than on the values, since `Integer` and
`Rational` are both `Real`.

| | was | is |
|---|---|---|
| `(Integer)(-7) % (Integer)3` | `2` | `2` |
| `(Integer)7 % (Integer)(-3)` | `ArithmeticException: Divisor is negative` | `-2` |
| `(Real)(-7) % (Real)3` | `-1` | `2` |
| `(Rational)(-7/2) % (Integer)(-3)` | `-7/2` — larger in magnitude than the divisor, a remainder under no convention at all | `-1/2` |

All three are now **floored**: the remainder takes the sign of the divisor, and every one of them
computes `a - b*floor(a/b)`. This is the convention under which the residues modulo `n` are the
numbers from `0` to `n - 1`, which is the point of the operation in number theory, and it is what
SymPy 1.14, Mathematica and Maxima all answer — checked on all four sign pairs rather than reasoned
about. C's `%` truncates, but C's `%` is an operation on machine integers.

**`Real` is the one to watch**: `-7 % 3` was `-1` and is `2`, on ordinary input, with no exception
to notice. For `Integer` and `Rational` only the cases that were broken moved.

[#708](https://github.com/asc-community/AngouriMath/issues/708), PR
[#709](https://github.com/asc-community/AngouriMath/pull/709).

---

## Simplification

`Simplify`, `InnerSimplified`, `Expand` and `Factorize` all reach further than they did. Each entry
below is an expression whose printed form changed; **assertions on printed form will need
rewriting**, which is why the house rule is to assert the mathematics instead.

### Radicals are reduced where the power is built

Not in `Simplify` — at construction, so this reaches every path that builds a radical.

```
sqrt(12)           was  sqrt(12)           is  2 * sqrt(3)
sqrt(1/2)          was  sqrt(1/2)          is  sqrt(2) / 2
1 / sqrt(2)        was  1 / sqrt(2)        is  sqrt(2) / 2
1 / sqrt(12)                               is  sqrt(3) / 6
1 / cbrt(2)                                is  4^(1/3) / 2
(2^3 * 5^7)^(1/3)                          is  50 * 5^(1/3)
12^(5/2)                                   is  288 * sqrt(3)
sqrt(369)          was  sqrt(369)          is  3 * sqrt(41)
```

The reduced form was previously generated as a `Simplify` candidate and lost to the shorter one on
node count, so it only ever appeared where two like radicals had to be collected. That is the wrong
judge for this: a reduced radicand is what makes two surds comparable without simplifying their
difference, and it is what SymPy, Mathematica and Maxima all print.

A negative radicand is declined — the principal cube root of `-8` is `1 + i*sqrt(3)`, not `-2`, and
reducing under an even root would settle that branch by accident.

[#281](https://github.com/asc-community/AngouriMath/issues/281),
[#205](https://github.com/asc-community/AngouriMath/issues/205), PR
[#716](https://github.com/asc-community/AngouriMath/pull/716).

### Constant equality is decided by the difference

`Equalsf` compared the two sides' separately evaluated values for exact digit equality, and
evaluating each rounds independently, so two spellings of one number differed in the last ulps.

```
sqrt(i) = (1 + i) / sqrt(2)     was  False    is  True
sin(pi/4) = sqrt(2)/2           was  False    is  True
sqrt(2) * sqrt(3) = sqrt(6)     was  False    is  True
```

It now falls back to testing whether the difference is zero, which reuses the library's existing
notion of zero rather than introducing a new tolerance. The exact-equality fast path is kept, so
infinities compare as before.

[#442](https://github.com/asc-community/AngouriMath/issues/442), PR
[#664](https://github.com/asc-community/AngouriMath/pull/664).

### `Simplify` returns conditions it did not return before

A quotient of polynomials is now put into lowest terms, and the condition that the cancelled factor
is nonzero travels with the answer — where it vanishes the quotient was `0/0` and the reduced form
is something definite. **The returned node is a `Providedf`, not a `Divf`**, so code that pattern-
matches on the node type or reads `Stringize` sees something new.

```
(x + y)^2 / ((x - y)(x + y))
    was  1 + (2y^2 + 2xy) / (x^2 - y^2)   — long-divided, still carrying the factor
    is   (x + y) / (x - y) provided not x + y = 0

(x - y)^2 / (x^2 - y^2)
    was  unchanged
    is   (x - y) / (x + y) provided not x - y = 0

sin(2u) * csc(u)
    was  csc(u) * sin(2u)
    is   2 * cos(u) provided not sin(u) = 0
```

The missing piece was a multivariate GCD (Knuth, TAOCP vol. 2 §4.6.1, algorithms C and E). Nothing
trusts its result: the divisor is divided out of both sides and multiplied back, and the quotient is
used only when both come out equal to what they started as.

[#55](https://github.com/asc-community/AngouriMath/issues/55), PR
[#713](https://github.com/asc-community/AngouriMath/pull/713);
[#557](https://github.com/asc-community/AngouriMath/issues/557), PR
[#693](https://github.com/asc-community/AngouriMath/pull/693).

### `Expand` collects like terms

```
3x + 2x                  was  3 * x + 2 * x            is  5 * x
(x+1)^2 * (x+2-1)^2      was  sixteen terms            is  1 + 4x + 6x^2 + 4x^3 + x^4
((x-1)*(x-2))^3                                        is  8 - 36x + 66x^2 - 63x^3 + 33x^4 - 9x^5 + x^6
```

A term whose coefficients cancel is written out rather than dropped, which looks wasteful and is the
whole of the correctness here: the monomial may carry a domain condition, and
`(4a - 2)/(2x) + (1 - 2a)/x` is zero only where `x` is not.

[#164](https://github.com/asc-community/AngouriMath/issues/164), PR
[#676](https://github.com/asc-community/AngouriMath/pull/676).

### `Factorize` finishes, and stops leaving its workings in the answer

```
x^2 - y^2
    was  (sqrt(x) - sqrt(y)) * (sqrt(x) + sqrt(y)) * (x^1 + y^1)   — correct, but not a factorisation
    is   (x - y) * (x + y)

a*c + a*d + b*c + b*d
    was  a*(c + d) + b*c + b*d
    is   (c + d) * (b + a)

4*x^2 - 4*y^2                                          is  4*(x - y)*(x + y)
```

The difference-of-squares rule halved both exponents without checking they were even, so it fired
again on the linear factors it had just made. Separately, every rule matched two *adjacent* terms of
the sum tree, so a sum of four could never be grouped twice over; sums of three or more now go
through a pass that flattens them.

[#531](https://github.com/asc-community/AngouriMath/issues/531),
[#178](https://github.com/asc-community/AngouriMath/issues/178), PR
[#665](https://github.com/asc-community/AngouriMath/pull/665).

### Polynomials with whole roots factor

`x^2 + 2x + 1` was `1 + x^2 + 2x` and is `(1 + x)^2`. Deliberately narrow on two counts, both
load-bearing: rational roots only — factoring through every root would answer `(x - i)(x + i)` for
`x^2 + 1`, which is not what anyone means by factoring that — and only where the polynomial splits
completely, so `x^2 - 1` stays as it is and the antiderivative of `x^2 + x` reads `x^3/3 + x^2/2`
rather than `x^2 * (x + 3/2) / 3`.

[#177](https://github.com/asc-community/AngouriMath/issues/177), PR
[#669](https://github.com/asc-community/AngouriMath/pull/669).

### The trigonometric tables are read backwards, and the Pythagorean identity in more arrangements

```
arcsin(1/2)                    was  arcsin(1/2)          is  pi / 6
arcsin(sqrt(2) / 2)                                      is  pi / 4
arccos(-1/2)                                             is  2/3 * pi
arctan(1)                      was  arctan(1)            is  pi / 4
arctan(sqrt(3))                                          is  pi / 3
arctan(1/2) + arctan(1/3)                                is  pi / 4

1 - sin(t)^2                   was  unchanged            is  cos(t)^2
1 + tan(t)^2                   was  unchanged            is  sec(t)^2
1 + cotan(t)^2                 was  unchanged            is  csc(t)^2
1 - sin(t)^2 - cos(t)^2        was  unchanged            is  0
sec(t)^2 - tan(t)^2            was  unchanged            is  1 provided not cos(t) = 0
```

Note the last one: the identity divided through by `cos^2` is only `1` where `cos(t)` is not zero,
and the condition comes back with the answer rather than being dropped. That is a `Providedf`, as in
the section above.

The tables were only ever read forwards. `arcsin(0.4999999)` still comes back unanswered: a double
comparison picks the candidate and the exact value confirms it.

The Pythagorean rewrites are one-way on purpose — `1 - sin(:)^2` becomes `cos(:)^2` and not the
reverse, since stating it both ways would undo each rewrite as fast as it fired.

[#569](https://github.com/asc-community/AngouriMath/issues/569),
[#179](https://github.com/asc-community/AngouriMath/issues/179), PR
[#716](https://github.com/asc-community/AngouriMath/pull/716);
[#725](https://github.com/asc-community/AngouriMath/issues/725), PR
[#726](https://github.com/asc-community/AngouriMath/pull/726).

### Interval intersections distribute over unions

`(-1; 1) /\ ((-(sqrt(33) + 3) / 6; (sqrt(33) - 3) / 6) \/ (1; +oo))` came back exactly as written and
is now `(-1; (sqrt(33) - 3) / 6)`. `IntersectIntervalAndInterval` also gave up unless every endpoint
was a bare `Real` node; endpoints are now compared by what they evaluate to, while the bounds of the
answer are taken from the original expressions, so the result stays exact rather than becoming a
hundred decimal places.

[#415](https://github.com/asc-community/AngouriMath/issues/415), PR
[#677](https://github.com/asc-community/AngouriMath/pull/677).

---

## Boolean simplification

### A boolean expression is minimised rather than merely factored

The rewrite rules reached absorption and stopped there, so the factoring worked and had nothing to
finish it — there is no rule taking `b or not b` to `true`:

```
a and b or a and not b                          was  a and (b or not b)   is  a
a or not a                                      was  a or not a           is  true
a and not a                                     was  a and not a          is  false
(a and b) or (a and not b) or (not a and b)      was  not a and b or a and (b or not b)
                                                 is  a or b
(not a and not b and not c) or (not a and not b and c)
                                                 was  a or b or c implies not (a or b) and c
                                                 is  not (a or b)
```

Two-level minimisation by Quine–McCluskey now runs over the truth table, covering excluded middle,
non-contradiction and every larger cover in one procedure rather than as separate rules.

**It is offered to `Simplify` as one more candidate, not as a replacement for its answer.**
Candidates are ranked by node count and the shortest is returned, so this can only change an
expression where the minimal form is shorter than everything else already on offer. That is what
keeps `not (a and b)` as it is — 4 nodes against its sum-of-products form `not a or not b` at 5 — and
it means no expression's answer gets longer.

The last row above is [#769](https://github.com/asc-community/AngouriMath/issues/769), where an
`implies` rewrite won at 12 nodes against the 16-node input. That rewrite was not misbehaving; the
4-node answer was simply never generated for it to lose to.

Bounded at **10 variables**, since the table is `2^n` rows. Beyond that it declines rather than
hangs. A cover of k terms is at least `2k-1` nodes, so where that already exceeds the input the
search is abandoned before it starts — parity over ten variables has 512 minterms, none of which
combine, and finding its 512-term minimal form took 1.5 s to produce a candidate that loses to the
input. With the check, 34 ms.

[#768](https://github.com/asc-community/AngouriMath/issues/768) and
[#769](https://github.com/asc-community/AngouriMath/issues/769), PR
[#770](https://github.com/asc-community/AngouriMath/pull/770).

## Solving

### A quadratic inequality is answered for every sign its coefficients may have

Three things decided a quadratic inequality's answer that the solver was not asking about, each of
them the sign of something it could not evaluate:

```
x^2 + 1 > 0                    was  { }                    is  RR
3*x^2 + 1 > 0                  was  (-oo; c) \/ (c; +oo)   is  RR
a*x^2 - 1 < 0     at a = 3     was  the complement of      is  (-1/sqrt(3); 1/sqrt(3))
                                    the solution set
(x + 1)(x + 2) < a             was  an interval even where it has no real roots
```

A parabola lies above zero *between* its roots when it opens downwards and *outside* them when it
opens upwards, and the test for that read `a is Real { IsNegative: true }` — which a symbol fails,
so every symbolic leading coefficient was read as positive. Where the quadratic has no real roots it
sits wholly on one side, and the empty set was returned whichever side that was. And whether "no
real roots" was noticed at all depended on how far the radical simplified: `sqrt(-4)` is the literal
`2i` and `sqrt(-12)` is a product that is not one, so `x^2 + 1` was recognised and `3*x^2 + 1` was
not. The linear branch had the same defect in miniature — `a*x + b > 0` is `x > -b/a` for a positive
`a` and `x < -b/a` for a negative one.

**What changes for callers.** A *concrete* coefficient is untouched: `(x - 2)(x + 2) <= 0` is still
`[-2; 2]` and `x^2 - 4 < 0` is still `(-2; 2)`. Where a coefficient is symbolic the answer is now a
union of conditional sets rather than a single interval — the case split written in the vocabulary
the library already has, since `Piecewise` is an `Entity` and `Solve` returns a `Set`:

```
{ x : a > 0 and x in ... or a < 0 and x in ... or a = 0 and x in ... }
```

Code that assumed `Solve` on an inequality returns an `Interval` will now sometimes get a
`ConditionalSet` or a `Unionf`. It was returning a wrong interval before.

Measured over 128 specialisations — solve with the symbol, substitute a value of each sign, and
compare membership against the inequality itself at eleven points: **81/128 correct on 1.4.0,
128/128 now**. Four cases skipped as "Piecewise required" since 1.2 pass.

[#757](https://github.com/asc-community/AngouriMath/issues/757) and
[#762](https://github.com/asc-community/AngouriMath/issues/762), PRs
[#761](https://github.com/asc-community/AngouriMath/pull/761) and
[#763](https://github.com/asc-community/AngouriMath/pull/763).

### An identity equation is answered with every value, not with none

An equation that puts no condition on the variable is satisfied by every value of it. The library
said the opposite in two different ways, one of them a wrong answer rather than a missing one:

```
(0 = 0).Solve(x)                       was  { }        is  CC
(x - x = 0).Solve(x)                   was  { 0 }      is  CC
```

The polynomial solver never checked whether a monomial's coefficient had cancelled, so `x - x` was
read as one times `x`, of degree one, with the root `0`.

A system that no longer constrains a variable is now answered with a free parameter rather than with
a single tuple that is *a* solution but not *the* solution set:

```
{ x - y = 0, 2x - 2y = 0 }.Solve(x, y)     was  [[0, 0]]     is  [[t_1, -t_1 / (-1)]]
{ x + y - 1, 2x + 2y - 2 }.Solve(x, y)                       is  [[t_1, -(t_1 + -1)]]
```

Those are `[[t_1, t_1]]` and `[[t_1, 1 - t_1]]` as numbers; the parameter arrives unsimplified.

`null` still means there are no solutions, which is the answer for a system that contradicts itself.
**Code that assumed a `FiniteSet` back from `Solve` now has `CC` or a parametric family to handle.**

[#550](https://github.com/asc-community/AngouriMath/issues/550), PR
[#679](https://github.com/asc-community/AngouriMath/pull/679).

### A sign error inverting a subtraction

`2 = 1/(5 - c)` solved for `c` answered `-9/2` where the answer is `9/2`. See the `Minusf` entry
above for the cause.

[#632](https://github.com/asc-community/AngouriMath/issues/632), PR
[#664](https://github.com/asc-community/AngouriMath/pull/664).

### Roots are checked against the equation they came from

Rewrites such as `log(a) + log(b) = log(a * b)` widen the domain, and the conditions cannot each
survive the chain of substitutions after them. Answers are now checked once, at the top, against the
equation as the caller wrote it. **Some root sets got smaller.**

```
ln(x) + ln(x+1) = 0
    was  { (-1 - sqrt(5)) / 2, (-1 + sqrt(5)) / 2 }
    is   { (-1 + sqrt(5)) / 2 }
```

`(-1 - sqrt(5))/2` is `-1.618…`, where both logarithms are taken off the negative reals and the
left-hand side comes to `2*pi*i`. A root is dropped only on positive evidence — anything that will
not evaluate to a number, a parametric family like `pi + 2*pi*n_1` among them, is kept.

The check also ran only when the caller wrote the equation out as `expr = 0`, so the two entry
points disagreed. Both check now:

```
"2^x + 2^(2x) - 6".SolveEquation(x)
    was  { ln((-3) ^ (1 / ln(2))), 1 }     is  { 1 }
```

A root found numerically is only as accurate as the search that found it, so the residual is read
against the largest of the terms it is the sum of rather than on its own size. Without that, all
four roots of `1/210 - 17x/210 + 101x^2/210 - 247x^3/210 + x^4` were dropped and the quartic came
back empty.

PRs [#664](https://github.com/asc-community/AngouriMath/pull/664) and
[#685](https://github.com/asc-community/AngouriMath/pull/685).

### `x + ln(x) = 0` no longer throws out of `Solve`

It raised `UncompilableNodeException` through the public `Solve`: the Newton solver compiles the
simplified expression, simplification leaves `Providedf` nodes behind, and the compiler has no way
to represent one. The conditions come off before compilation and the roots are verified after. It
answers `0.5671…`.

PR [#664](https://github.com/asc-community/AngouriMath/pull/664).

### Numeric root sets are deduplicated, and the numeric search finds more

The search starts from a grid, and the same root reached from different starting points came back
agreeing to about sixteen significant digits and differing after that, so **each starting point
contributed a root of its own**:

```
x^5 + 3x + 1        was  28 roots     is  5
x^6 + x + 1         was  23 roots     is  6
```

Four of the 28 were `-0.83907243306660750`, `…61`, `…73` and `…84`. Candidates closer together than
the iteration can tell apart are now one root, and the one kept from each group is whichever leaves
the equation closest to zero.

Three separate faults were also making the search miss roots that were inside the region:

- The grid divided each index by the step count with `EDecimal`'s own operator, which carries no
  context and answers `NaN` wherever the quotient does not terminate in base ten. Only step counts
  of the form `2^a * 5^b` divided exactly, so at 3, 6, 7, 9, 12, 21 and most other values the search
  covered a single corner. **Asking for a finer search made the answer worse**; the default divides
  exactly, which is why nothing caught it.
  ([#115](https://github.com/asc-community/AngouriMath/issues/115), PR
  [#695](https://github.com/asc-community/AngouriMath/pull/695))
- The grid is two-dimensional, so a step count of `N` lays real starting points only `(To-From)/N`
  apart — 2 at the default. The real axis is now scanned separately at `StepCount.Re * StepCount.Im`
  points and Newton runs from the brackets a sign change finds, making the spacing that matters for
  a real root `(To-From)/N^2`. `arcsin(x) - x*pi/3` went from `{ 0 }` to `{ -1/2, 0, 1/2 }`.
  ([#115](https://github.com/asc-community/AngouriMath/issues/115), PR
  [#729](https://github.com/asc-community/AngouriMath/pull/729))
- The `FastExpression` compiler conjugated arcsine unconditionally, which is right on the two branch
  cuts and wrong everywhere else, so Newton walked away from roots it started at.
  (PR [#696](https://github.com/asc-community/AngouriMath/pull/696))

**Do not depend on the order roots come back in.** `TestFormula8` asserted which of the two roots of
`x^2 + 1` a `HashSet` handed back first; that was never the solver's to decide, and candidates are
now ordered before being collapsed so that which one is compared against which does not depend on
the order the grid produced them in.

Near-rational roots are also no longer presented as exact ones
([#235](https://github.com/asc-community/AngouriMath/issues/235), PR
[#668](https://github.com/asc-community/AngouriMath/pull/668)).

PR [#686](https://github.com/asc-community/AngouriMath/pull/686).

### Dense linear systems

Elimination gave up where it stalled instead of trying another equation, so a reporter's small
system did not solve. [#608](https://github.com/asc-community/AngouriMath/issues/608), PR
[#667](https://github.com/asc-community/AngouriMath/pull/667).

---

## Limits

More limits are answered, and — this is the part to read — **some that were answered are answered
differently, and some that were unevaluated are now `NaN`**. The three results are three different
claims and they are not interchangeable:

| result | means |
|---|---|
| unevaluated (`Limitf`, the expression back) | "I could not settle this" |
| `NaN` | "**this does not exist**" |
| a value | "this is the value" |

### Wrong values that became right ones

```
lim x->0   (1+x)^(1/x)          was  1        is  e
lim x->0+  (1+x)^(1/x)          was  1        is  e
lim x->+oo (x-5)^x / x^x        was  1 *      is  e^(-5)
lim x->0-  sin(x)/x             was  NaN      is  1
lim x->0+  cos(x)/sin(x)        was  NaN      is  +oo
lim x->0   1/x^2 - 1/sin(x)^2   was  NaN      is  -1/3
```

\* unevaluated on 1.4.0 and `1` once the descent reached it; the wrong answer was reachable from
`Simplify`.

The second remarkable limit required the exponent to have an infinite *two-sided* limit, where at
`x -> 0` the exponent `1/x` goes to `-oo` on the left and `+oo` on the right — no two-sided limit,
even though the magnitude diverges, which is all the rule needs. Separately, the rule ran once at
the top of `ComputeLimit`, so a `1^oo` *created* by a later simplification was judged by rules that
ran before it existed.

`sin(x)/x` from one side is the one worth naming on its own: the library said the most familiar
limit in the subject did not exist. The one-sided path skipped everything the two-sided path has,
both in front of the descent and behind it.

PRs [#664](https://github.com/asc-community/AngouriMath/pull/664),
[#697](https://github.com/asc-community/AngouriMath/pull/697),
[#700](https://github.com/asc-community/AngouriMath/pull/700),
[#728](https://github.com/asc-community/AngouriMath/pull/728),
[#739](https://github.com/asc-community/AngouriMath/pull/739).

### Unevaluated that became `NaN` — a stronger claim, not a weaker one

```
lim x->+oo sin(x)               was  unevaluated    is  NaN
lim x->0   sin(1/x)             was  unevaluated    is  NaN
```

A sine whose argument grows without bound takes every value in its range infinitely often and
settles on none, so the limit genuinely does not exist and `NaN` says so where an unevaluated node
said only that no rule found one. Two tests changed rather than broke, one of which says in its own
summary that the answer must not become `NaN` "since NaN asserts that the limit does not exist
rather than that it was not found" — the principle is right, and a sine's range is what establishes
non-existence.

`lim x->+oo (x + sin(x))/x`, `x * sin(x)` and `x * cos(x)` are still unevaluated.

[#723](https://github.com/asc-community/AngouriMath/issues/723), PR
[#724](https://github.com/asc-community/AngouriMath/pull/724).

### Unevaluated that became a value

A large family, from l'Hôpital's rule at infinity, Gruntz's algorithm, the squeeze theorem,
differences of divergent parts, indeterminate powers and products, and limits of piecewise
expressions. A sample:

```
lim x->+oo sin(x) / x                     unevaluated  ->  0
lim x->0   x * sin(1 / x)                 unevaluated  ->  0
lim x->+oo ln(x) / x                      unevaluated  ->  0
lim x->+oo e^(x + e^(-x)) - e^x           unevaluated  ->  1
lim x->+oo sqrt(x^2 + 3x) - sqrt(x^2+1)   unevaluated  ->  3/2
lim x->+oo x^20 / e^x                     unevaluated  ->  0
lim x->+oo arcsin(x)                      unevaluated  ->  pi/2 - +ooi
lim x->0   csc(x)^2 - 1/x^2               unevaluated  ->  1/3
```

`lim x->+oo sin(a*x)/x` is still unevaluated, correctly — a free variable reads as complex here, and
`sin(i*t)` is `i*sinh(t)`, which grows without bound, so it is not `0` for every `a`.

`lim x->0 x * ln(x)` and `lim x->0 x^x` are still `NaN` deliberately: `x^x` is not real to the left
of `0`, so the `1` from that side is the complex continuation and not a limit to agree with.

PRs [#680](https://github.com/asc-community/AngouriMath/pull/680),
[#682](https://github.com/asc-community/AngouriMath/pull/682),
[#694](https://github.com/asc-community/AngouriMath/pull/694),
[#710](https://github.com/asc-community/AngouriMath/pull/710),
[#714](https://github.com/asc-community/AngouriMath/pull/714),
[#724](https://github.com/asc-community/AngouriMath/pull/724).

### A constant factor no longer decides whether a vanishing sine is seen

The first remarkable limit — a vanishing `sin(u)`, `tan(u)`, `arcsin(u)` or `arctan(u)` rewritten as
the `u` it is equivalent to — was applied at the root of the expression only. A constant factor
written on one side pushes that function a level down and out of reach, so the same product answered
two different things depending on how it was spelled:

```
lim x->+oo sin(1/x) * x * 2       was  2        is  2
lim x->+oo 2 * sin(1/x) * x       was  NaN      is  2
lim x->+oo sin(1/x) * x / 2       was  NaN      is  1/2
lim x->0   sin(x) * 1/x / 2       was  NaN      is  1/2
lim x->+oo 2 * tan(1/x) * x^2     was  NaN      is  +oo
```

The rule is now applied down the product-and-quotient spine. It stops at anything that is not a
product or a quotient, which is what keeps it sound: the equivalence licenses replacing a *factor*
of the expression as a whole, and not a term of a sum, where the difference between the two forms is
the whole answer. `lim x->0 (sin(x)/x - 1)/x^2` is still `-1/6` and `lim x->0 (sin(x) - x)/x^3` is
still `-1/6`, as they must be — a rule that descended into sums would answer both `0`.

**The loss, and it is a value becoming `NaN`.** A vanishing factor against a logarithm was being
shielded from a rewrite the library already applies to the same limit written without the constant:

```
lim x->0   sin(x) * ln(x) * 2     was  0        is  NaN
lim x->0   2 * arctan(x) * ln(x)  was  0        is  NaN
lim x->0   sin(x) * ln(x)         was  NaN      is  NaN     (unchanged, and the reason)
```

The rewrite is sound — `sin(x) * ln(x)` and `x * ln(x)` have the same limit — and what it exposes is
that `lim x->0 x * ln(x)` is `NaN` by design, for the reason given above: `ln(x)` is not real to the
left of `0`. So the previous `0` was the accident, and the two spellings of one limit now agree
instead of contradicting each other. Under `MathS.Settings.Codomain.Set(Domain.Real)` both come back
unevaluated, which is the honest answer where the function has no real left-hand neighbourhood.

Measured over 811 generated products and quotients: 196 `NaN`s became values, 13 values became this
one `NaN` — every one of them of the shape above, and in each case the constant-free spelling was
already `NaN` — and no answer changed into a different answer.

[#749](https://github.com/asc-community/AngouriMath/issues/749), PR
[#759](https://github.com/asc-community/AngouriMath/pull/759).

### An indeterminate power form is no longer read off as its value

A limit was answered, where nothing else had a reading of it, by substituting the destination and
evaluating. That is right wherever the expression is continuous there, and an indeterminate form is
exactly where it is not. Almost all of them already declined, because they evaluate to `NaN` and
every caller reads `NaN` as "no limit" — `0 * oo`, `oo - oo`, `oo / oo`, `0^0`. The two that do not
are `oo^0` and `1^oo`, which this library's arithmetic answers with `1`, so a limit that assembled
either read that `1` off as its answer:

```
lim x->+oo (e^x) ^ (1/ln(x))    was  1     is  +oo
lim x->+oo (x^2) ^ (1/ln(x))    was  1     is  e^2
lim x->+oo (x!)  ^ (1/x)        was  1     is  unevaluated   (it is +oo)
lim x->+oo (x!)  ^ (1/ln(x))    was  1     is  unevaluated   (it is +oo)
```

`(x^2)^(1/ln x)` is `e^(2*ln(x)/ln(x))`, that is `e^2` at every `x`, so the old answer was not merely
imprecise. `(x!)^(1/x)` grows like `x/e` by Stirling — `(100!)^(1/100)` is already `37.99`.

This does **not** change what `(+oo)^0` or `1^oo` evaluate to as expressions. Both are still `1`, the
same convention SymPy and IEEE 754's `pow` use; the change is that a *limit* no longer reads its
answer off one. `0^0` is deliberately untouched for the opposite reason: its `NaN` is how the library
says `lim x->0 x^x` does not exist, which `LimitTest.TestNoLimit` pins, and declining it would turn a
considered "does not exist" into "not settled".

**What it costs.** Two limits that were answered `1` are now unevaluated, because the substitution
that used to land on them is gone and nothing else has a reading:

```
lim x->0   (1/x) ^ x            was  1     is  unevaluated
lim x->0   (1/x) ^ (x^2)        was  1     is  unevaluated
```

A third, `lim x->+oo (x!)^(1/x^2)`, was withdrawn here and is answered `1` again by the entry
below — it was right by luck when this landed, since the same substitution gave `1` for
`(x!)^(1/x)` where the answer is `+oo`, and the two cannot be told apart at the point where the
value is read off. Stirling's expansion answers it by reading instead. The other two are
two-sided limits at `0` whose base has no two-sided limit at all and which are not real to the left
of `0`, so the `1` came from the complex continuation; **their one-sided readings are unchanged** —
`lim x->0+ (1/x)^x` is still `1`.

Measured over 225 generated powers at `0` and `+oo`: 2 wrong values became right ones, 3 wrong values
became unevaluated, 1 `NaN` became unevaluated, and 3 right values became unevaluated.

[#754](https://github.com/asc-community/AngouriMath/issues/754), PR
[#760](https://github.com/asc-community/AngouriMath/pull/760).

### A factorial itself is read where it has no logarithm at all

The two readings below both need the factorial to be under a logarithm already — one supplies the
logarithm, by turning `f^g` into `e^(g * ln f)`, and the other rewrites a logarithm that is written
down. Neither reaches `x! / x^x`, which has none:

```
lim x->+oo x! / x^x           was  unevaluated   is  0
lim x->+oo x^x / x!           was  unevaluated   is  +oo
lim x->+oo x! / e^x           was  unevaluated   is  +oo
lim x->+oo ln(x! / x^x) / x   was  a 20 s+ hang  is  -1
lim x->+oo ln(x! / x^x)       was  unevaluated   is  -oo
```

The logarithm is supplied instead: for a positive expression `lim H` is `e^(lim ln H)`, and `ln H` is
where the expansion applies. The guard is the power of the factorial the expression depends on, which
is exactly the coefficient `ln(f!)` carries in `ln H`, so the dropped `1/(12f)` has to vanish against
it — `(x!)^x` gives `x`, and `x/(12x)` does not, so it is refused.

Not by substituting `e^(Stirling(f))` for the factorial, which is the obvious move and measured much
worse: it puts an `e` to a large exponent into the expression, the machinery evaluates that constant
to a hundred-digit decimal, and everything downstream carries it. `lim x->+oo (x!/e^x)^(1/x)` went
from half a second to over a minute that way, on an expression the library already answered.

The rewrite that reads a *logarithm* is broadened at the same time, from `ln(f!)` to any logarithm
holding a diverging factorial, so `ln(x! / x^x)` is taken apart rather than left as one opaque node.

Over 70 generated factorial expressions, against the release before these three changes: **33
answered → 66 answered, 25 timeouts → 1, and nothing lost**. Values spot-checked numerically at up to
`x = 1e6`.

[#754](https://github.com/asc-community/AngouriMath/issues/754), PR
[#767](https://github.com/asc-community/AngouriMath/pull/767).

### A factorial's logarithm is read wherever it appears

The expansion below arrived inside the rule that turns `f^g` into `e^(g * ln f)`, so it reached a
factorial only under a vanishing exponent — the harder question answered and the easier one left:

```
lim x->+oo ln(x!) / (x * ln(x))   was  unevaluated   is  1
lim x->+oo ln(x!) / x             was  unevaluated   is  +oo
lim x->+oo ln(x!) / x^2           was  unevaluated   is  0
lim x->+oo ln(x!) - x * ln(x)     was  unevaluated   is  -oo
lim x->+oo ln((2*x)!) / (x*ln(x)) was  unevaluated   is  2
lim x->+oo ln(x!) / ln(x)         was  NaN           is  +oo
```

The last is the one that was a **wrong answer** rather than a missing one: `NaN` claims the limit
does not exist, and that quotient is asymptotic to `x` — 9.6e11 at `x = 1e12`.

Applying the expansion wherever a factorial's logarithm appears is **not** unconditionally sound, so
it is guarded. What Stirling drops is `1/(12f)`, and what that costs the answer is the rate at which
the answer moves with the logarithm — so the coefficient is found by putting a variable where the
logarithm is and differentiating, and the rewrite is refused unless `coefficient / f` vanishes.
`x * (ln(x!) - (x*ln(x) - x + ln(2*pi*x)/2))` is `1/12`, built out of the dropped term itself, and an
unguarded rewrite would answer it `0`.

**What it costs.** `lim x->+oo ln((x+1)!) / (x*ln(x))` was unevaluated in about a second and now
takes longer than 20 s to be unevaluated. The rewrite is right and the expression it hands over is
right; the machinery cannot take that limit quickly, which is demonstrable without any of this by
writing the expanded form out by hand. One of 70 in a generated factorial sweep, where 24 already
timed out before this change.

[#765](https://github.com/asc-community/AngouriMath/issues/765), PR
[#766](https://github.com/asc-community/AngouriMath/pull/766).

### A factorial under a vanishing exponent has a limit

A power whose base holds a factorial had none at all. Every route out of `ln(f)` runs through
differentiating `f`, and a factorial's derivative wants the digamma function, which this library does
not have — so the rule that reads `f^g` as `e^(g * ln f)` declined and nothing behind it had a
reading either:

```
lim x->+oo (x! / x^x) ^ (1/x)   was  unevaluated   is  1/e
lim x->+oo (x!) ^ (1/x)         was  unevaluated   is  +oo
lim x->+oo (x!) ^ (1/ln(x))     was  unevaluated   is  +oo
lim x->+oo (x!) ^ (1/x^2)       was  unevaluated   is  1
lim x->+oo (x! / e^x) ^ (1/x)   was  unevaluated   is  +oo
lim x->+oo ((x+1)! / x!) ^ (1/x)  was unevaluated  is  1
```

Stirling's expansion is stated for exactly that logarithm — `ln(f!)` is
`f*ln(f) - f + ln(2*pi*f)/2 + 1/(12f) + O(1/f^3)` — and it is applied to the **exponent** rather than
substituted for the factorial in the base. That is what makes it sound: what is dropped here
*vanishes*, where the asymptotic for `f!` itself has an error that is merely relative and survives
being raised to a power. Vanishing is still not enough on its own, since the dropped term is
multiplied by the exponent it sits under, so `power / f -> 0` is required; `(x!)^x` fails that and is
left alone.

Every one of these values was checked numerically before being claimed — `(x!/x^x)^(1/x)` is
`0.3678794453` at `x = 1e9` against `1/e = 0.3678794412`. Note that **SymPy 1.14.0 answers that one
`0`**, so it is not a usable oracle here.

Nothing without a factorial in it changes: over 225 generated powers, six results differ and all six
are one of these, every one of them an unevaluated node becoming a value. `casbench` 112/117 →
113/117.

[#754](https://github.com/asc-community/AngouriMath/issues/754), PR
[#764](https://github.com/asc-community/AngouriMath/pull/764).

### `lim x->2 signum(x)` no longer kills the process

`Signumf` was the one node whose limit override handed back an unevaluated limit of the very
expression it was asked about. That is a cycle, not a failure to answer: the two-sided path compares
its two one-sided results by evaluating them, evaluating a limit computes it, and computing arrives
back at the same override. Some four thousand frames later the stack ran out — which kills the
process rather than raising anything catchable. It answers `1`.

[#704](https://github.com/asc-community/AngouriMath/issues/704), PR
[#705](https://github.com/asc-community/AngouriMath/pull/705).

---

## Differentiation and integration

### `sgn` has a derivative

`derivative(sgn(x), x)` came back unevaluated, with a note that the delta function would be needed
first. Leaving it unevaluated does not represent the delta either — it only means the derivative of
anything containing `sgn` cannot be evaluated at all, and that reached callers: the antiderivative
of `abs(x)` is `sgn(x) * x^2 / 2`, and differentiating it back stopped there.

`sgn` is flat either side of zero and has no derivative at zero, so the derivative is `0` wherever
it exists, and the condition says where: `0 provided not x = 0`. This is the stance `abs` already
took right below it.

PR [#672](https://github.com/asc-community/AngouriMath/pull/672).

### Some antiderivatives were not antiderivatives

`CreateUnique` handed back a variable name already in use, because it looked for names to avoid by
cutting at the *first* underscore — so for the prefix `u_sub`, which is the one integration uses, it
read an existing `u_sub_1` as the prefix `u` with the index `sub_1`, parsed no number, and returned
`u_sub_1` as though it were free. That only bites when integration substitutes twice, and the result
was a nonsense expression that passed the test for a successful substitution.

```
int x(x^2+1)^3 dx      was the expanded polynomial      is  (x^2 + 1)^4 / 8
int x(x^2+1)^10 dx     was the expanded polynomial      is  (x^2 + 1)^11 / 22
int 3x^2(x^3+2)^2 dx   was not an antiderivative        is  (x^3 + 2)^3 / 3
```

The first two were correct — differentiating either back gives the integrand exactly — and only the
form changed. The third did not: differentiating the old answer gives
`4.5x^8 + 10.5x^5 + 12x^2` against the integrand `3x^8 + 12x^5 + 12x^2`, and the two are already
0.0035 apart at `x = 0.3` and 506 apart at `x = 2.1`.

Those are the values. The antiderivative table does not fold its own arithmetic, so the printed form
is `(x ^ 2 + 1) ^ (3 + 1) / (3 + 1) / 2 + C` and the like — pass it through `Simplify` if you want it
tidy, and do not assert on the string.

The third is the one to check for: `d/dx` of the old answer is `4.5x^8 + 10.5x^5 + 12x^2`, against
the integrand `3x^8 + 12x^5 + 12x^2`.

PR [#670](https://github.com/asc-community/AngouriMath/pull/670).

### An integral that came back holding a `NaN`

Substitution candidates were not required to depend on `x`, and `ln(e)` is picked up from any
integrand carrying a logarithm — its derivative is `0`, the integrand divided by it is `NaN`, and
`NaN` contains no `x`, so it passed the test for a successful substitution and was returned as the
antiderivative.

```
int (sin(x)^2 + cos(x)^2) dx
    was  NaN * (sin(x)^2 + cos(x)^2) + C
    is   an antiderivative, via the power-reduction entry added later
```

The result of the division is now checked for `NaN` directly, which catches every way of arriving at
one.

PRs [#665](https://github.com/asc-community/AngouriMath/pull/665) and
[#675](https://github.com/asc-community/AngouriMath/pull/675).

### A quadratic denominator with a symbolic leading coefficient answered `NaN`

`1/(4 + 9*x^2)` has always come back as an arctangent. `1/(a^2 + b^2*x^2)` came back as `NaN` — the
claim that no antiderivative exists, for one of the most common integrals there is.

```
int 1/(a^2 + b^2*x^2) dx     was  NaN + C
                             is   a piecewise, the arctangent where 4*a^2*b^2 > 0
int 1/sqrt(a*x^2 + c) dx     was  NaN + C
                             is   a piecewise on the sign of a
```

Both patterns build their `a = 0` arm as though the denominator still had an `x` term —
`(k/b) ln|bx + c|` and `2k sqrt(bx + c)/b` — each dividing by a `b` that is zero whenever the
denominator is written without one. The arm is guarded afterwards by a `Providedf` on `a = 0`, so a
numeric leading coefficient makes it decidably unreachable and it is dropped before anything looks
inside; a symbolic one leaves it in place, and a `Providedf` carrying `NaN` takes the whole piecewise
with it. Every test of these shapes used numbers, where the defect cannot be reached.

The arm was also the wrong formula, not merely undefined: where `a = 0` and `b = 0` the integrand is
the constant `k/c` and its antiderivative is `kx/c`, which is what it now says.

Measured over the 1774 fair problems of Rubi's independent test suites: answered 536 -> 543, wrong
7 -> 0. Three problems that were never answered moved from unevaluated to timeout, the `NaN` having
served as an accidental early exit.

PR [#774](https://github.com/asc-community/AngouriMath/pull/774).

### Two integrals that were answered are now unevaluated

This one is a loss, and it is deliberate. Both were correct on 1.4.0 — differentiating either back
gives the integrand exactly — and both are `integral(…)` today:

```
int x(x^2 + x + 1)^2 dx        was answered (2968 ms)    is unevaluated
int x^2(x^2 + 1)^2 dx          was answered (1414 ms)    is unevaluated
```

They reached an answer through integration by parts re-entering itself, which is the same path that
overflowed the stack on `int x*ln(x)` and killed the process. Bounding the recursion closed the
route these two arrived by along with it. No answer is a limit; a crash is not.

**If you were getting an expression for either, you now get an unevaluated node** — check for one
rather than assuming an antiderivative comes back. Both are worth reopening as coverage, and neither
was ever tracked by an issue.

PRs [#665](https://github.com/asc-community/AngouriMath/pull/665) and
[#670](https://github.com/asc-community/AngouriMath/pull/670).

### `int x*ln(x)` no longer overflows the stack

`ComputeIndefiniteIntegral` takes an `integrateByParts` flag so that integration by parts can call
back into it without re-entering itself, and four of the recursive solvers did not hand the flag on
— so by parts was switched back on one level below the call that had switched it off.

`int sin(x)^2 dx` was the same fault without the crash: it had not returned after forty seconds on
1.4.0, and answers instantly now.

PR [#665](https://github.com/asc-community/AngouriMath/pull/665).

### Many integrals that had no antiderivative now have one

Additive; listed because an unevaluated `integral(...)` you were branching on may now be an
expression. The families added are `k / sqrt(ax^2 + bx + c)`, an exponential times a sine or cosine,
the inverse trigonometric functions, any whole power of sine times any whole power of cosine,
`sqrt(ax^2 + bx + c)`, a linear numerator over a linear or quadratic denominator, `1/cos(u)^2` and
`1/sin(u)^2`, `k / (x^2 sqrt(ax^2 + c))`, and rational functions split at a rational root of the
denominator — `1/(x^3 + 1)`, `1/(x^4 - 1)`, `1/((x-1)(x-2)(x-3))` and the rest.

[#233](https://github.com/asc-community/AngouriMath/issues/233), PRs
[#675](https://github.com/asc-community/AngouriMath/pull/675),
[#681](https://github.com/asc-community/AngouriMath/pull/681),
[#690](https://github.com/asc-community/AngouriMath/pull/690),
[#691](https://github.com/asc-community/AngouriMath/pull/691).

---

## Compilation

### A missing variable is named

```csharp
"a * x".ToEntity().Compile("x");
// was  KeyNotFoundException: The given key 'a' was not present in the dictionary.
// is   UncompilableNodeException: a is not among the variables the expression is being
//      compiled over, which are x
```

Both compilers did it, each by reaching into its own dictionary of arguments and letting the lookup
fail, naming neither the variable, nor the compilation, nor anything a caller of `Compile` could act
on. **Change your `catch` clause.** Constants are unaffected: `pi` and `e` are substituted before any
variable is looked up, so `"pi * x"` still compiles over `x` alone.

PR [#688](https://github.com/asc-community/AngouriMath/pull/688).

### `Expand` of a quotient of factorials threw instead of expanding

```csharp
"(x + 1)! / x!".ToEntity().Expand();
// was  AngouriBugException: SmartExpandOver must be only called of non-sum expression
// is   x + 1

"(x + 2)! / x!".ToEntity().Expand();
// was  AngouriBugException
// is   x ^ 2 + 3 * x + 2
```

Expansion cancels a quotient of factorials before it does anything else, and the cancellation can
leave a sum — `(x + 1)! / x!` is `x + 1`. The result was then handed straight back to the routine
that expands a *non*-sum, which asserts that it never receives one, so the assertion fired and an
`AngouriBugException` came out of a public method. Sending the cancelled expression through the
sum-aware entry point instead costs nothing when it is not a sum, since that is where everything
else goes anyway.

**`AngouriBugException` is the library reporting that the library is broken**, so nothing was
depending on this; the entry is here because the same call now returns a value where it used to
throw. `Simplify` was never affected — it answered `1 + x` throughout.

Issue [#817](https://github.com/asc-community/AngouriMath/issues/817).

### The deprecated members are gone

2.0 follows semantic versioning, so everything carrying `[Obsolete]` has been removed — 28 members
in all. Each obsolete message named its replacement, and those replacements are what to move to:

| removed | use instead |
|---|---|
| `MathS.Matrices.Matrix(Entity[,])` | `MathS.Matrix` |
| `MathS.Matrices.Vector(params Entity[])` | `MathS.Vector` |
| `MathS.Matrices.MatrixMultiplication`, `DotProduct` | `operator *` |
| `MathS.Matrices.ScalarProduct(a, b)` | `a.T * b` |
| `MathS.Matrices.Interval(...)` ×2 | `MathS.Interval` |
| `MathS.Compute.Derivative(expr, x)` ×2 | `expr.Differentiate(x)` |
| `MathS.Compute.Integral(expr, x)` ×2 | `expr.Integrate(x)` |
| `MathS.Compute.Limit(expr, x, to)` ×2 | `expr.Limit(x, to)` |
| `MathS.Compute.DefiniteIntegral(...)` ×3 | **`expr.DefiniteIntegral(x, from, to)` — new, see below** |
| `Entity.Derive(x)`, `"...".Derive(x)` | `Differentiate` |
| `Entity.Matrix.Shape` | `RowCount` and `ColumnCount` |
| `Setting.Global`, `Setting.RollBackToDefault` | `Setting.Set` in a `using` |
| `CompilationProtocol`'s six converter delegates | inherit and override the methods |
| `CompilationProtocolBuiltinConstantConverters` | `CompilationProtocol` |

**One of those replacements did not exist.** `MathS.Compute.DefiniteIntegral` was deprecated in
favour of "non-static methods at `Entity`", and there was no `Entity.DefiniteIntegral` — every other
member of that group had a counterpart and this one did not. Removing the group as it stood would
have taken numeric definite integration out of the library with nothing to replace it, so
`Entity.DefiniteIntegral(x, from, to, stepCount = 100)` is added here, in real and complex bound
overloads.

### `round`, `min`, `max` and `gcd` exist, so they no longer raise

```csharp
"round(5/2)".ToEntity().Simplify();   // was UnrecognizedFunctionParseException, is 2
"min(3, 5)".ToEntity().Simplify();    // was UnrecognizedFunctionParseException, is 3
"gcd(1/2, 1/3)".ToEntity().Simplify();// was UnrecognizedFunctionParseException, is 1/6
```

**`round` is half to even**, which is what Python, SymPy, Mathematica and IEEE 754 all mean by
rounding, and what .NET's `Math.Round` does by default: `round(1/2)` is `0`, `round(5/2)` is `2`.
It is deliberately **not** `floor(x + 1/2)`, which disagrees at every tie, and there is a test
pinning that they differ.

`min`, `max` and `gcd` take any number of arguments and fold, so `min(3, 5, 1)` is `1`. `min` and
`max` compare only where the arguments are ordered — an unordered pair is left as the node rather
than guessed at, as SymPy's `Min` does. `gcd` covers integers and rationals, `gcd(1/2, 1/3)` being
`1/6` exactly as SymPy gives; the polynomial case is left unevaluated for now even though this
library computes polynomial gcds elsewhere.

`trunc`, `lcm`, `erf` and `conjugate` are still refused by name.

Issue [#809](https://github.com/asc-community/AngouriMath/issues/809).

### `floor` and `ceil` exist, so they no longer raise

```csharp
"floor(x)".ToEntity();
// was  UnrecognizedFunctionParseException: floor is not a function this library has
// is   floor(x)

"floor(x) - 3 = 0".ToEntity().Solve("x");
// was  UnrecognizedFunctionParseException
// is   { 3 + t_1 provided t_1 in RR and t_1 >= 0 and t_1 < 1 }
```

`ceiling(` is accepted as well, since that is SymPy's spelling; `Stringize` prints the short
`ceil(`. Both round toward the infinities rather than toward zero — `floor(-3/2)` is `-2` — and both
are taken componentwise on a complex argument, which is what SymPy and Mathematica do. Every value
in the test file was measured against SymPy 1.14 rather than reasoned about.

**If you were catching `UnrecognizedFunctionParseException` around these names, that is now dead
code.** Nothing else moves: `round`, `trunc`, `min`, `max`, `gcd` and `lcm` are still refused by
name, and `floor` on its own — without a bracket — is still an ordinary variable.

The derivative is `0 provided not x in ZZ`: flat between the integers, undefined at each of them.
SymPy leaves that derivative unevaluated; this library states the condition instead, which is the
stance `Signumf` and `Absf` already take.

Issue [#809](https://github.com/asc-community/AngouriMath/issues/809).

### A parse failure no longer escapes as a `NullReferenceException`

```csharp
"(1 provided x > 0, 2)".ToEntity();
// was  NullReferenceException
// is   UnhandledParseException: line 1:17 no viable alternative at input '(1providedx>0,'
```

ANTLR reports a syntax error to the listener and then *recovers*, carrying on with a rule context
whose value was never assigned. The grammar's action for `provided` runs anyway and dereferences it,
so the parse came down before the error already recorded against the input was read.

**Change your `catch` clause if you were catching `NullReferenceException` around user input** — but
the point of the fix is that you should not have had to.
[`Docs/Usage/Exceptions.md`](Sources/AngouriMath/Docs/Usage/Exceptions.md) says every parse failure
arrives under `AngouriMathBaseException`, and a caller who wrote the one correct `catch` was not
catching this.

The construct is unaffected: `1 provided x > 0` parses, and so does the piecewise form
`piecewise(1 provided x > 0, 2 provided x < 0, 3)`. What is refused is the comma list with no
`piecewise` in front of it, which has no rule.

Issue [#813](https://github.com/asc-community/AngouriMath/issues/813).

### Compiled arcsine was the conjugate of the arcsine

The `FastExpression` compiler conjugated arcsine unconditionally, which is right on the two branch
cuts — the real arguments outside `[-1, 1]`, where the library does take the lower side and where
the only test of it looked — and wrong everywhere else.

```
arcsin(0.5 + 0.1i) compiled
    was  0.5198083869450859 - 0.11496532217013944i
    is   0.5198083869450859 + 0.11496532217013944i
```

Measured against `Evaled` over a grid of 63 points spanning both cuts, the `FastExpression` compiler
agreed at 15 and the Linq compiler, which does not conjugate, at 59 — neither right, wrong in
complementary places. Both now go through one helper that conjugates on the cut and nowhere else and
agree at all 63. **Nothing changes on the real axis.** Arccosecant is the same, being an arcsine of
a reciprocal.

PR [#696](https://github.com/asc-community/AngouriMath/pull/696).

### A compiled expression may be called from more than one thread

`FastExpression` kept its working stack and its subexpression cache as instance fields, so two
threads calling one compiled expression interleaved their pushes and pops. Measured with sixteen
threads over 400000 calls: 399328 threw "Unused values remain in the stack", 418 threw "Stack
empty", and one returned `-0.483` where the answer is `14884.07`. The damage was permanent — the
count check throws before anything is popped, so a single racing call left its leftovers behind and
every later call failed on them, on one thread or on many.

Both buffers are now per thread. Nothing is slower for it.

[#637](https://github.com/asc-community/AngouriMath/issues/637), PR
[#698](https://github.com/asc-community/AngouriMath/pull/698).

### Expressions whose matrices cancel out compile

And what cannot be compiled is named rather than failing obscurely.
[#425](https://github.com/asc-community/AngouriMath/issues/425),
[#526](https://github.com/asc-community/AngouriMath/issues/526), PR
[#673](https://github.com/asc-community/AngouriMath/pull/673).

---

## Additions

Not behavioural changes, listed so that a reader working through this file has the whole picture.

- **The branches a simplification went down and came back from.**
  `DerivationPath.Abandoned`, beside the `Steps` it kept. The data was recorded already —
  reconstructing the path searched the recorded edges and discarded everything not on the chain —
  so this exposes it rather than collecting anything new. Each abandoned step carries the
  expression it left from, which is the branch's root. Deduplicated against itself and against
  the kept chain: the raw edges are mostly one rewrite recorded once per level of the candidate
  search, and `x^(-1)/(y/z)` produced 425 of them across 13 distinct steps.
  [#273](https://github.com/asc-community/AngouriMath/issues/273).
- **An analytical solver for first-order linear ordinary differential equations.**
  `MathS.SolveOde(equation, function, variable)`, by the integrating factor. The unknown is written
  as an application — `apply(y, x)` — rather than a bare variable, because `derivative(y, x)` is
  `0`: a variable does not depend on `x`. `y' + y = 1` comes back as `1 + C_1 * e ^ (-x)`, and
  `y' + y/x = 1` as `C_1 / x + x / 2`. It returns `null` where the equation is not linear in the
  unknown and its derivative, and where either of the two integrals has no closed form, which is
  why `y' + y = e^(x^2)` is declined. Nothing existed before it; there is no behaviour to compare.
  [#241](https://github.com/asc-community/AngouriMath/issues/241).
- **Integrals wanting a substitution by a power of the variable that occurs nowhere.**
  `int x / (x^4 + 1)` is `arctan(x^2)/2 + C`, and so for `x/(x^4 - 1)`, `x/(x^4 + 4)`,
  `x^2/(x^6 + 1)`, `x/(x^6 + 1)`, `x^3/(x^8 + 1)` and `x^3/(x^12 + 1)`. Each was previously
  returned unevaluated. Two things had to change: `x^2` is now offered as a candidate although
  it is written nowhere in `x / (x^4 + 1)`, and substituting it rewrites `x^4` as `(x^2)^2` so
  that there is something for it to replace. `int x^3 / (x^4 + 1)` worked throughout, its own
  substitution being one that occurs, which is what made the gap hard to see. No integral that
  was answered before is answered differently.
  [#233](https://github.com/asc-community/AngouriMath/issues/233).
- **A modulus node.** `Modf`, `MathS.Mod`, the `%` operator on `Entity` in C# and F#, the `mod`
  keyword in the parser, evaluation, simplification, differentiation, limits, both compilers, LaTeX
  and SymPy export. `a % b` on `Entity` is *not* `int`'s `%` — it is floored, and its documentation
  says so. [#402](https://github.com/asc-community/AngouriMath/issues/402),
  [#618](https://github.com/asc-community/AngouriMath/issues/618), PR
  [#703](https://github.com/asc-community/AngouriMath/pull/703).
- **Taking part of a matrix by range.** `a[1.., ..]`, `a[..2, 1..3]`, `a[^1.., ^1..]`. Both extents
  have to be written out. An empty range is refused; a range reaching past the matrix throws from the
  range itself. [#443](https://github.com/asc-community/AngouriMath/issues/443), PR
  [#683](https://github.com/asc-community/AngouriMath/pull/683).
- **A syntax reference**, at [`Sources/AngouriMath/Docs/Usage/Syntax.md`](Sources/AngouriMath/Docs/Usage/Syntax.md).
  The grammar was previously the only statement of what the language accepts, and it is ANTLR source.
- **MyGet is gone.** Releases still publish to NuGet; the per-push master feed does not exist any
  more, so remove it from your `NuGet.Config` if you had it. PR
  [#701](https://github.com/asc-community/AngouriMath/pull/701).

---

## 1.4.0 — one change that was never written down

This file begins at 2.0.0, so a break introduced *by* 1.4.0 has had no home in it. One is worth
recording, because 1.4.0 is what a `dotnet add package` installs today and the change is silent
until a string is parsed.

### `integral` takes bounds, not an iteration count

| | 1.3.0 | 1.4.0 onwards |
|---|---|---|
| `integral(f, x, 1)` | accepted, read as `integral(f, x)` | `FunctionArgumentCountException` |
| `integral(x, x, 2)` | the second antiderivative, `x ^ 3 / 6` | `FunctionArgumentCountException` |
| `integral(x, x, 0, 1)` | `FunctionArgumentCountException` | the definite integral from `0` to `1` |

The third argument used to be a repetition count and is now the lower bound of a range, so three
arguments name neither form and are refused. `derivative(f, x, n)` is unaffected and still takes an
order, which is why the two functions read differently.

This was deliberate — PR [#657](https://github.com/asc-community/AngouriMath/pull/657), "change
integral node to support range instead of iterations" — and it is covered by a test. It simply went
out without a note. Both samples in this repository called `integral(f, x, 1)` and had been failing
against 1.4.0 since January without anyone seeing it, because they were pinned to 1.3.0; PR
[#846](https://github.com/asc-community/AngouriMath/pull/846) is what surfaced it.

**What to do.** `integral(f, x, 1)` becomes `integral(f, x)` — identical in meaning, since 1.3.0
turned the former into the latter anyway. A count of two or more was not a no-op and is the part
that is actually gone: nest the calls instead, `integral(integral(f, x), x)`, which 1.3.0 accepted
as well. Expect the answer to differ by the constants of integration, which the same PR began
adding — where `integral(x, x, 2)` gave `x ^ 3 / 6` on 1.3.0, the nested form gives
`C_1 + C * x + x ^ 3 / 6` from 1.4.0 onwards.

---

## If one of these hurts

Open an issue at https://github.com/asc-community/AngouriMath/issues saying what you were relying
on and what it was for. A change made because the old answer was wrong will not be reverted to the
wrong answer, but where the old behaviour was *useful* — a tolerance, a printed form, a name — there
is usually a way to have both, and the setting or the overload to add is worth knowing about.
