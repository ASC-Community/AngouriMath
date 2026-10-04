//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;

namespace CasBench
{
    public enum Op { Integrate, Limit, Solve, Simplify, Derive }

    /// <summary>
    /// One benchmark problem. <see cref="Expected"/> is only used for limits and
    /// simplifications; integrals are checked by differentiating the answer and
    /// equations by substituting the roots back, so no canonical form is assumed.
    /// </summary>
    public sealed record Problem(
        string Category,
        Op Op,
        string Input,
        string Variable = "x",
        string Approach = null,
        string Expected = null,
        string Source = null,
        bool NoElementaryForm = false);

    /// <summary>
    /// Problems are drawn from the standard technique taxonomy that mainstream CAS
    /// test suites are organised around: SymPy's series/integrals/solvers test files,
    /// the Rubi rule-based integration suite, and the Gruntz thesis limit examples.
    /// Each entry records which technique it is meant to exercise, so a failure names
    /// the missing solver rather than just the missing answer.
    /// </summary>
    public static class Corpus
    {
        public static readonly List<Problem> All = new()
        {
            // ---------------- INTEGRALS ----------------
            // Table / standard forms
            new("int:table", Op.Integrate, "x ^ 3"),
            new("int:table", Op.Integrate, "1 / x"),
            new("int:table", Op.Integrate, "e ^ x"),
            new("int:table", Op.Integrate, "sin(x)"),
            new("int:table", Op.Integrate, "cos(x)"),
            new("int:table", Op.Integrate, "1 / cos(x) ^ 2"),
            new("int:table", Op.Integrate, "sinh(x)"),
            new("int:table", Op.Integrate, "2 ^ x"),
            new("int:table", Op.Integrate, "x ^ (1/2)"),
            new("int:table", Op.Integrate, "1 / x ^ 2"),

            // Linearity
            new("int:linearity", Op.Integrate, "3 * x ^ 2 + 2 * x + 1"),
            new("int:linearity", Op.Integrate, "sin(x) + cos(x)"),
            new("int:linearity", Op.Integrate, "5 * e ^ x - 2 / x"),

            // Linear inner argument f(ax+b)
            new("int:linear-arg", Op.Integrate, "sin(3 * x + 1)"),
            new("int:linear-arg", Op.Integrate, "e ^ (2 * x)"),
            new("int:linear-arg", Op.Integrate, "1 / (2 * x + 5)"),
            new("int:linear-arg", Op.Integrate, "(3 * x + 4) ^ 7"),

            // u-substitution
            new("int:u-sub", Op.Integrate, "cos(x ^ 2) * x", Source: "issue #233"),
            new("int:u-sub", Op.Integrate, "x * e ^ (x ^ 2)"),
            new("int:u-sub", Op.Integrate, "2 * x / (x ^ 2 + 1)"),
            new("int:u-sub", Op.Integrate, "sin(x) * cos(x)"),
            new("int:u-sub", Op.Integrate, "ln(x) / x"),
            new("int:u-sub", Op.Integrate, "x / sqrt(x ^ 2 + 1)"),
            new("int:u-sub", Op.Integrate, "tan(x)"),

            // Integration by parts
            new("int:by-parts", Op.Integrate, "x * e ^ x"),
            new("int:by-parts", Op.Integrate, "x * sin(x)"),
            new("int:by-parts", Op.Integrate, "ln(x)"),
            new("int:by-parts", Op.Integrate, "x ^ 2 * e ^ x"),
            new("int:by-parts", Op.Integrate, "arctan(x)"),
            new("int:by-parts", Op.Integrate, "arcsin(x)"),
            // Cyclic by parts: needs solving an integral equation
            new("int:by-parts-cyclic", Op.Integrate, "sin(x) * e ^ x", Source: "issue #233"),
            new("int:by-parts-cyclic", Op.Integrate, "cos(x) * e ^ x"),

            // Standard arctan / arcsin forms
            new("int:arctan-form", Op.Integrate, "1 / (1 + x ^ 2)"),
            new("int:arctan-form", Op.Integrate, "1 / (a ^ 2 + x ^ 2)", Source: "issue #233"),
            new("int:arcsin-form", Op.Integrate, "1 / sqrt(1 - x ^ 2)"),
            new("int:arcsin-form", Op.Integrate, "1 / sqrt(4 - x ^ 2)"),

            // Partial fractions
            new("int:partial-fractions", Op.Integrate, "1 / (x ^ 2 - 1)"),
            new("int:partial-fractions", Op.Integrate, "1 / (x * (x + 1))"),
            new("int:partial-fractions", Op.Integrate, "(x + 3) / (x ^ 2 + 3 * x + 2)"),
            new("int:partial-fractions", Op.Integrate, "x ^ 2 / (x ^ 4 + 1)", Source: "issue #233"),
            new("int:partial-fractions", Op.Integrate, "1 / (x ^ 3 + 1)"),
            // Repeated rational root in the denominator. The corpus had none, so splitting
            // off only one copy of a root went unnoticed: 1/(x^4 + x^2) has zero twice and
            // no other rational root, and had no antiderivative at all.
            new("int:partial-fractions", Op.Integrate, "1 / (x ^ 4 + x ^ 2)"),
            new("int:partial-fractions", Op.Integrate, "1 / ((x - 1) ^ 2 * (x + 2))"),
            new("int:partial-fractions", Op.Integrate, "1 / ((x + 1) ^ 3 * (x - 2))"),
            new("int:partial-fractions", Op.Integrate, "x / ((x - 1) ^ 2 * (x ^ 2 + 1))"),

            // Rational with quadratic denominator
            new("int:rational-quadratic", Op.Integrate, "1 / (x ^ 2 + 2 * x + 5)"),
            new("int:rational-quadratic", Op.Integrate, "x / (x ^ 2 + 2 * x + 5)"),

            // Trigonometric powers / identities
            new("int:trig-powers", Op.Integrate, "sin(x) ^ 2"),
            new("int:trig-powers", Op.Integrate, "cos(x) ^ 2"),
            new("int:trig-powers", Op.Integrate, "sin(x) ^ 3"),
            new("int:trig-powers", Op.Integrate, "sin(x) ^ 2 * cos(x) ^ 2"),

            // Trigonometric substitution
            new("int:trig-sub", Op.Integrate, "sqrt(1 - x ^ 2)"),
            new("int:trig-sub", Op.Integrate, "1 / (x ^ 2 * sqrt(x ^ 2 - 1))"),
            new("int:trig-sub", Op.Integrate, "sqrt(x ^ 2 + 1)"),

            // Hard / Risch-territory
            new("int:hard", Op.Integrate, "sqrt(tan(x))", Source: "issue #233"),
            new("int:hard", Op.Integrate, "ln(x) ^ 2"),
            new("int:hard", Op.Integrate, "x * ln(x)"),
            new("int:hard", Op.Integrate, "e ^ x / x", NoElementaryForm: true),        // Ei(x)
            new("int:hard", Op.Integrate, "e ^ (x ^ 2)", NoElementaryForm: true),      // erfi(x)

            // ---------------- LIMITS ----------------
            new("lim:basic", Op.Limit, "1 / x", Approach: "+oo", Expected: "0"),
            new("lim:basic", Op.Limit, "x ^ 2", Approach: "+oo", Expected: "+oo"),
            new("lim:basic", Op.Limit, "2 * x + 1", Approach: "3", Expected: "7"),

            new("lim:0/0", Op.Limit, "sin(x) / x", Approach: "0", Expected: "1", Source: "issue #231"),
            new("lim:0/0", Op.Limit, "(e ^ x - 1) / x", Approach: "0", Expected: "1"),
            new("lim:0/0", Op.Limit, "(1 - cos(x)) / x ^ 2", Approach: "0", Expected: "1/2"),
            new("lim:0/0", Op.Limit, "tan(x) / x", Approach: "0", Expected: "1"),
            new("lim:0/0", Op.Limit, "arcsin(x) / x", Approach: "0", Expected: "1", Source: "issue #333"),
            new("lim:0/0", Op.Limit, "ln(1 + x) / x", Approach: "0", Expected: "1"),

            new("lim:oo/oo", Op.Limit, "(x ^ 16) / (1 + x ^ 16)", Approach: "+oo", Expected: "1"),
            new("lim:oo/oo", Op.Limit, "(3 * x ^ 2 + 1) / (2 * x ^ 2 - 5)", Approach: "+oo", Expected: "3/2"),
            new("lim:oo/oo", Op.Limit, "ln(x) / x", Approach: "+oo", Expected: "0"),
            new("lim:oo/oo", Op.Limit, "x / e ^ x", Approach: "+oo", Expected: "0"),

            new("lim:1^oo", Op.Limit, "(1 + 1/x) ^ x", Approach: "+oo", Expected: "e", Source: "issue #231"),
            new("lim:1^oo", Op.Limit, "(1 + 2/x) ^ x", Approach: "+oo", Expected: "e ^ 2"),
            new("lim:1^oo", Op.Limit, "(1 + x) ^ (1/x)", Approach: "0", Expected: "e"),

            new("lim:oo-oo", Op.Limit, "e ^ x - x", Approach: "+oo", Expected: "+oo", Source: "issue #231"),
            new("lim:oo-oo", Op.Limit, "1/x + ln(x)", Approach: "+oo", Expected: "+oo", Source: "issue #209"),
            new("lim:oo-oo", Op.Limit, "sqrt(x ^ 2 + x) - x", Approach: "+oo", Expected: "1/2"),

            new("lim:gruntz", Op.Limit, "e ^ (x + e ^ (-x)) - e ^ x", Approach: "+oo", Expected: "1", Source: "Gruntz"),
            new("lim:gruntz", Op.Limit, "x / ln(x) ^ 2", Approach: "+oo", Expected: "+oo", Source: "Gruntz"),

            new("lim:factorial", Op.Limit, "((x!) / x ^ x) ^ (1/x)", Approach: "+oo", Expected: "1 / e", Source: "issue #596"),

            new("lim:one-sided", Op.Limit, "1 / x", Approach: "0", Expected: null),

            // ---------------- EQUATIONS ----------------
            new("eq:linear", Op.Solve, "2 * x + 3 = 7"),
            new("eq:quadratic", Op.Solve, "x ^ 2 - 5 * x + 6 = 0"),
            new("eq:quadratic", Op.Solve, "x ^ 2 + 1 = 0"),
            new("eq:cubic", Op.Solve, "x ^ 3 - 6 * x ^ 2 + 11 * x - 6 = 0"),
            new("eq:quartic", Op.Solve, "x ^ 4 - 5 * x ^ 2 + 4 = 0"),
            new("eq:rational", Op.Solve, "1 / x + 1 / (x + 1) = 1"),
            new("eq:radical", Op.Solve, "sqrt(x + 1) = 3"),
            new("eq:radical", Op.Solve, "sqrt(x) + sqrt(x + 1) = 5"),

            new("eq:trig", Op.Solve, "sin(x) = 0"),
            new("eq:trig", Op.Solve, "cos(x) = 1/2"),
            new("eq:trig", Op.Solve, "sin(x) ^ 2 - 1 = 0"),
            new("eq:trig", Op.Solve, "cos(x) ^ 2 + sin(x) = 1", Source: "issue #270"),

            new("eq:exponential", Op.Solve, "2 ^ x = 8", Source: "issue #214"),
            new("eq:exponential", Op.Solve, "e ^ x = 5"),
            new("eq:exponential", Op.Solve, "2 ^ x + 2 ^ (-x) = 5/2", Source: "issue #214"),
            new("eq:exponential", Op.Solve, "e ^ (2 * x) - 3 * e ^ x + 2 = 0", Source: "issue #214"),

            new("eq:logarithmic", Op.Solve, "ln(x) = 2", Source: "issue #246"),
            new("eq:logarithmic", Op.Solve, "log(10, x) = 3", Source: "issue #246"),
            new("eq:logarithmic", Op.Solve, "ln(x) + ln(x + 1) = 0", Source: "issue #246"),
            new("eq:logarithmic", Op.Solve, "ln(x) ^ 2 - 3 * ln(x) + 2 = 0", Source: "issue #246"),

            new("eq:abs", Op.Solve, "(|x|) = 3"),
            new("eq:polynomial-high", Op.Solve, "x ^ 5 - 1 = 0"),
            new("eq:transcendental", Op.Solve, "x * e ^ x = 1"),      // needs Lambert W
            new("eq:transcendental", Op.Solve, "x + ln(x) = 0"),      // needs Lambert W

            // ---------------- SIMPLIFICATION ----------------
            new("simp:polynomial", Op.Simplify, "(x + 1) ^ 2 - x ^ 2 - 2 * x - 1", Expected: "0"),
            new("simp:rational", Op.Simplify, "(x ^ 2 - 1) / (x - 1)", Expected: "x + 1"),
            new("simp:rational", Op.Simplify, "(x ^ 2 + 2 * x * y + y ^ 2) / (x ^ 2 - y ^ 2)", Expected: "(x + y) / (x - y)", Source: "issue #55"),
            new("simp:factoring", Op.Simplify, "a * c + a * d + b * c + b * d", Expected: "(a + b) * (c + d)", Source: "issue #531"),
            new("simp:surds", Op.Simplify, "sqrt(8)", Expected: "2 * sqrt(2)", Source: "issue #205"),
            new("simp:surds", Op.Simplify, "sqrt(12) + sqrt(27)", Expected: "5 * sqrt(3)", Source: "issue #205"),
            new("simp:surds", Op.Simplify, "sqrt(2) * sqrt(3)", Expected: "sqrt(6)"),
            new("simp:trig", Op.Simplify, "sin(x) ^ 2 + cos(x) ^ 2", Expected: "1"),
            new("simp:trig", Op.Simplify, "(sin(2 * x) * csc(x)) ^ 2 / 4 - cos(2 * x) - sin(x) ^ 2", Expected: "0", Source: "issue #557"),
            new("simp:trig", Op.Simplify, "sin(2 * x) - 2 * sin(x) * cos(x)", Expected: "0"),
            new("simp:log", Op.Simplify, "ln(x) + ln(y) - ln(x * y)", Expected: "0"),
            new("simp:log", Op.Simplify, "ln(2 ^ 1000) / ln(2 ^ (-1000))", Expected: "-1", Source: "issue #210"),
            new("simp:inverse-trig", Op.Simplify, "arcsin(x) + arccos(x)", Expected: "pi / 2", Source: "issue #179"),
            new("simp:inverse-trig", Op.Simplify, "arctan(1/2) + arctan(1/3)", Expected: "pi / 4", Source: "issue #179"),
            new("simp:collapse", Op.Simplify, "x ^ 2 + 2 * x + 1", Expected: "(x + 1) ^ 2", Source: "issue #177"),
        };
    }
}
