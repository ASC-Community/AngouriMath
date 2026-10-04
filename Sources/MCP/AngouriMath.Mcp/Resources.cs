//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Text.Json.Nodes;

namespace AngouriMath.Mcp;

/// <summary>
/// Three documents the model can read. They cost nothing in tool-list context, and the
/// reliability one tells the model when to trust a result and when to expect a decline.
/// </summary>
/// <remarks>
/// ResourcesTest checks the syntax document against the grammar, so a function the grammar
/// gains or loses fails the build until the list here says so.
/// </remarks>
internal static class Resources
{
    public static JsonArray List() =>
    [
        new JsonObject
        {
            ["uri"] = "angourimath://syntax",
            ["name"] = "AngouriMath expression syntax",
            ["description"] = "How to write expressions for this server, including the two " +
                              "parsing traps that silently change meaning. Read before " +
                              "composing a non-trivial expression.",
            ["mimeType"] = "text/markdown",
        },
        new JsonObject
        {
            ["uri"] = "angourimath://curiosities",
            ["name"] = "Verified mathematical curiosities",
            ["description"] = "Famous results and coincidences, each with the exact " +
                              "expression to reproduce it on this server. Doubles as a " +
                              "self-test corpus.",
            ["mimeType"] = "text/markdown",
        },
        new JsonObject
        {
            ["uri"] = "angourimath://reliability",
            ["name"] = "Measured reliability by problem class",
            ["description"] = "Which operations are trustworthy and which are known to " +
                              "decline or mislead, as the library's own harnesses measure them.",
            ["mimeType"] = "text/markdown",
        },
    ];

    public static string? Read(string uri) => uri switch
    {
        "angourimath://syntax" => Syntax,
        "angourimath://curiosities" => Curiosities,
        "angourimath://reliability" => Reliability,
        _ => null,
    };

    private const string Syntax = """
        # AngouriMath expression syntax

        Plain text. `x^2 + 3*x - 1`, `sin(x)/x`, `sqrt(2)`, `e^x`, `+oo` / `-oo` for infinity.

        ## Two traps that change the meaning of your expression without any error

        **1. A number directly after an identifier is an EXPONENT, not a factor.**

        | You write | It means |
        |---|---|
        | `x2`        | x²             |
        | `2x`        | 2·x            |
        | `2(g + e)3` | 2·(g + e)³     |

        So a variable named `x2`, `v1` or `a0` is silently squared / raised. Avoid variable
        names ending in a digit; write `x_2` or `xa` instead. Always read the `parsed` field
        that every response returns.

        **2. An unknown function name becomes a multiplication.**

        `im(z)` is not a function here — it is the product `im * z`. The parse succeeds and
        produces a different expression. Some names are refused outright, but not every
        unknown name can be: refusing them all would refuse `a(b + c)`.

        Functions:

        - trigonometric: `sin cos tan cotan sec cosec`, also spelled `cot csc`, and their
          inverses `arcsin arccos arctan arccotan arcsec arccosec`, also spelled
          `asin acos atan acot acotan arccot asec acsc acosec arccsc`
        - hyperbolic: `sinh cosh tanh cotanh sech cosech`, also spelled
          `sh ch th cth coth sch csch`, and their inverses as *area* functions:
          `arsinh arcosh artanh arcotanh arsech arcosech`, also spelled
          `asinh acosh atanh acotanh acoth asech acosech acsch arsh arch arth arcth arcoth
          arsch arcsch` — **not** `arcsinh`, which is refused
        - `ln log log2 log10 exp pow sqrt cbrt sqr abs signum sign sgn`
        - `floor ceil ceiling round min max gcd lcm`
        - `gamma factorial binomial phi prime valuation`
        - special functions: `erf erfc erfi Ei li Si Ci Shi Chi`
        - sets: `card powerset union intersection complement image preimage domain aleph
          iverson`
        - `derivative integral limit limitleft limitright sum product argmax argmin piecewise
          apply lambda`

        ## Spellings worth knowing (verified against the grammar, not guessed)

        | You might write | Reality |
        |---|---|
        | `arcsinh(x)` | **refused.** The inverse hyperbolics are area functions: write `arsinh`, `asinh` or `arsh` |
        | `trunc`, `conjugate` | refused by name — the library has neither |
        | `im(z)`, `re(z)` | **silently become `im * z`.** There is no real- or imaginary-part function |
        | `elementin(x, A)`, `setsubtraction(A, B)`, `impl(a, b)` | **silently become products.** Use the infix `x in A`, `A \ B`, `a -> b` |
        | `union(A, B)`, `intersection(A, B)` | refused: both are indexed, `union(A_i, i in I)`. For two sets write `A \/ B` and `A /\ B` |
        | `7 mod 3` | `mod` is FLOORED: `-7 mod 3` is `2`, not `-1` |
        | `7 % 3` | a parse error. `%` stays free to mean percent |
        | `mod` as a variable name | a parse error: `mod` is a keyword. Rename it |
        | `log(2, 8)` | `3` — base first, then argument |

        The "silently become" cases are the dangerous ones: they parse, and they mean
        something else. Anything else followed by `(` gets a warning.

        ## Statements

        `=` is equality (`x^2 = 4`), and `>`, `<`, `>=`, `<=` are comparisons. Combine with
        `and`, `or`, `not`, `xor`, `implies`. `am_solve` accepts these, so you can solve
        under constraints: `['x^2 = 4', 'x > 0']` yields `2`.

        ## LaTeX

        LaTeX is OUTPUT only. This server cannot parse LaTeX input — convert
        `\frac{a}{b}` to `a/b` yourself before calling.
        """;

    private const string Curiosities = """
        # Curiosities, each verified on this server

        Every entry below was checked here, not copied from memory. The expression given is
        exactly what to run — so this doubles as a self-test corpus. If one of these stops
        reproducing, something regressed.

        ## Exact identities

        | Result | Run |
        |---|---|
        | Euler's identity, exactly 0 — not 1e-16 | `am_evaluate  e^(i*pi) + 1` |
        | Machin's formula: how π was computed to 100 digits by hand in 1706 | `am_verify_equal  4*arctan(1/5) - arctan(1/239)  vs  pi/4` |
        | Two arctans make a right angle's eighth | `am_verify_equal  arctan(1/2) + arctan(1/3)  vs  pi/4` |
        | Golden ratio is a pentagon in disguise | `am_verify_equal  (1+sqrt(5))/2  vs  2*cos(pi/5)` |
        | φ from its defining property | `am_solve  ['x^2 = x + 1', 'x > 0']  for x` |
        | Ramanujan's taxicab: 1729 two ways | `am_verify_equal  1^3 + 12^3  vs  9^3 + 10^3` |

        ## A machine-checked proof that 22/7 > π

        The integrand is positive on (0,1), so the integral is positive — which proves the
        inequality:

        1. `am_integrate  x^4*(1-x)^4/(1+x^2)  dx`  (verifies by differentiating back)
        2. `am_evaluate  1/7 - 4/6 + 1 - 4/3 + 4 - 4*arctan(1)`  → **exactly `22/7 - pi`**

        ## Near misses — where floating point would lie to you

        | Result | Run |
        |---|---|
        | Homer Simpson's Fermat counterexample. Agrees to **ten** significant figures — exactly a pocket calculator's width, which is the joke | `am_evaluate  3987^12 + 4365^12 - 4472^12` → 1211886809373872630985912112862690, not 0 |
        | 42 as a sum of three cubes (Booker & Sutherland, 2019). Each term is ~5e50, so a double returns pure noise | `am_evaluate  (-80538738812075974)^3 + 80435758145817515^3 + 12602123297335631^3` → 42 |
        | 355/113, the best simple approximation to π (Milü, 5th century) | `am_evaluate  355/113 - pi  digits 20` → 2.67e-7 |
        | Ramanujan's quartic approximation to π | `am_evaluate  (2143/22)^(1/4) - pi  digits 20` → -1.01e-9 |
        | One term of Ramanujan's 1/π series already gives 8 digits | `am_evaluate  9801/(2*sqrt(2)*1103) - pi  digits 20` → 7.6e-8 |
        | e^π − π is almost exactly 20, for no known reason | `am_evaluate  e^pi - pi  digits 20` → 19.999099979189475768 |

        ## 42

        | Result | Run |
        |---|---|
        | Adams' "six by nine" is true in base 13 | `am_represent base 54 to_base 13` → 42 |
        | 42 = 101010, a perfect alternating bit pattern | `am_represent base 42 to_base 2` |
        | The 5th Catalan number — counts the ways to parenthesise six factors | `am_evaluate  10! / (6! * 5!)` |
        | 6¹ + 6², and also the sum of the first six even numbers | `am_evaluate  6^1 + 6^2` |
        | The rainbow really is at 42°: minimise deviation through a raindrop | `am_solve ['(4/3)^2 - 1 = 3*c^2','c > 0'] for c` → `sqrt(21)/9`, giving 42.03° |

        ## Ramanujan's constant, which this build gets right

        `e^(pi*sqrt(163))` is `262537412640768743.99999999999925007...`, sitting 7.5e-13
        below an integer for a reason from class field theory. Ask for enough digits and the
        near-miss reproduces.

        ## Where the answer is honest but not what you asked

        - `sqrt(x^2)` is left as written rather than reduced to `abs(x)`. It is not reduced
          to `x` either, which would be wrong for every negative.

        ## Beyond this server

        No infinite series: `sum` adds up finitely many terms, and an infinite sum is left
        unevaluated, so Basel (π²/6) and the Leibniz series cannot be evaluated here. Worth
        knowing anyway — truncating Leibniz at
        500,000 terms gives
        `3.14159065358979324046264338326950` against π's
        `3.14159265358979323846264338327950`: nearly every digit correct, with isolated
        wrong ones. Those errors are not noise — they are the Euler numbers.

        """;

    private const string Reliability = """
        # Measured reliability

        ## What this server runs against

        The library it ships with: `amcli mcp` is built and released with the library, so an
        answer here is the library's answer at this version.

        What follows is a snapshot. If something contradicts it, trust the tool's own `status`
        and `verified` fields over this document, and say so.

        The library measures itself on every change. Its CasBench harness runs a corpus of
        problems with known answers, drawn from SymPy's test suite, the Rubi integration suite
        and Gruntz's thesis, and fails on a wrong answer. Answers are not trusted on their
        face: integrals are checked by differentiating back, equation roots by substituting
        them in.

        ## Trustworthy

        - **Equations**: linear, quadratic, cubic, quartic, higher polynomial, rational,
          radical, trigonometric, exponential, logarithmic, absolute-value and transcendental.
          Every root is checked by substituting it back.
        - **Derivatives**: fast and dependable. The PropCheck harness compares them with a
          difference quotient.
        - **Integrals**: table forms, linearity, f(ax+b), u-substitution, by parts (including
          cyclic), arctan and arcsin forms, partial fractions, trig powers, trig substitution.
        - **Limits**: including Gruntz-class, 0/0, ∞/∞, 1^∞, ∞−∞, and factorial asymptotics:
          `lim x→∞ (x!/x^x)^(1/x)` is `1/e`, by Stirling.

        ## Integrals without an elementary antiderivative

        `∫ e^x/x`, `∫ e^(x^2)`, `∫ sin(x)/x` and `∫ 1/ln(x)` have none, and are answered with
        the special functions that name them: `Ei(x)`, `sqrt(pi)/2 * erfi(x)`, `Si(x)` and
        `li(x)`. They are verified by differentiating back like any other answer.

        ## Expect a decline (this is correct behaviour, not a bug)

        - `∫ x^x` and `∫ sqrt(1 + x^3)`: no closed form in the functions the library has;
          the second is elliptic. Do not retry or reword.

        A decline from `am_integrate` with `from` and `to` still carries a
        `numeric_definite_value` for that one interval, rounded to the digits it is good for
        — roughly four. It is quadrature, not an antiderivative, and it is not verified by
        differentiating back.

        ## Where results mislead

        - **`am_verify_equal` decides on POSITIVE real points**, because correct
          antiderivatives are full of `ln(x)` and `abs(x)`. So `sqrt(x^2)` against `x` comes
          back `equal: true` — true on the positives, false at every negative. It is not
          silent about it: a second pass across the whole real line adds a note saying the
          two agree only for positive inputs. **Read the note before repeating the verdict.**
        - **Simplify is not canonical**: `sqrt(12)+sqrt(27)` comes back as `sqrt(3)*5` while
          `5*sqrt(3)` is left alone. Two different-looking outputs can be equal — use
          `am_verify_equal` rather than comparing strings.
        - **A `status` of `unchanged` means no progress, not "already simplest".**
        - **Output tidiness** varies. Results are correct but not always in the form a human
          would write. Pass `alternatives: true` to `am_simplify` and pick a nicer form.
        - **Nonlinear systems** can come back with no solution when one exists.

        ## How to talk about a result

        State the status you were given. If a tool returns `declined`, say the library has no
        rule for it rather than presenting the unevaluated form as an answer. If it returns
        `unchanged`, say it made no progress. If `verified` is false or a `conflict` is
        reported, do not use the result. Say which tool produced a number rather than
        implying you worked it out.

        ## Soundness note

        Unlike some CAS libraries, this one tracks domain conditions: `(x^2-1)/(x-1)`
        simplifies to `x + 1 provided not x - 1 = 0`, rather than an unconditional `x + 1`
        that is wrong at x = 1. The `provided` clause is a feature — preserve it.
        """;
}
