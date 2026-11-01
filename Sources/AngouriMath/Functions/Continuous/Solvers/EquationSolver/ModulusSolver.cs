//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// Equations in moduli: where one is real for every complex <c>x</c>, and how one in moduli of
    /// linear functions is solved over the reals.
    /// </summary>
    /// <remarks>
    /// Over the complex numbers <c>|x - 2| = |x - 3|</c> holds on the whole line
    /// <c>Re x = 5/2</c>: both sides are real whatever <c>x</c> is, so the equation is one real
    /// condition on the two real unknowns <c>x</c> is made of, and what solves it is a curve. A
    /// numerical search finds points of it and would answer them as the set. Over the reals the
    /// same equation is <c>x = 5/2</c>, found by cases: between two neighbouring kinks every
    /// modulus has one sign, and the equation there has no moduli.
    /// https://github.com/asc-community/AngouriMath/issues/1573
    /// </remarks>
    internal static class ModulusSolver
    {
        /// <summary>
        /// Whether <paramref name="expr"/> is real for every complex <paramref name="x"/>, read off
        /// its structure: <paramref name="x"/> only ever inside a modulus, and around the moduli
        /// arithmetic with real numbers. A numerical probe could not tell this from a function
        /// that is real at the points it tried.
        /// </summary>
        internal static bool IsRealForEveryComplex(Entity expr, Variable x)
            => expr.ContainsNode(x) && RealWhereverItIs(expr, x);

        /// <summary>
        /// Whether <paramref name="x"/> is both inside a function that is not holomorphic -- a
        /// modulus, a signum, a conjugate, a real or imaginary part, an argument -- and outside every
        /// such function somewhere in <paramref name="expr"/>.
        /// </summary>
        /// <remarks>
        /// Newton's method assumes a holomorphic function, and over the complex numbers
        /// <c>|u| = +-u</c> only where <c>u</c> is real, which a root need not make it:
        /// <c>x^2 + 1 = |x|</c> has roots at <c>+-0.618 i</c>, where <c>|x|</c> is <c>0.618</c>. So
        /// neither the search nor a split by the sign finds every root, and the search's near-copies
        /// of a root came back as roots: <c>x^2/|1 - x^2| = x</c> had <c>1.618 + 1.7e-9 i</c> beside
        /// <c>1.618</c>. https://github.com/asc-community/AngouriMath/issues/2139
        /// </remarks>
        internal static bool IsInsideAndOutsideAFunctionThatIsNotHolomorphic(Entity expr, Variable x)
            => expr.Nodes.Any(node => IsNotHolomorphic(node) && node.ContainsNode(x)) && IsOutsideThem(expr, x);

        private static bool IsNotHolomorphic(Entity node)
            => node is Absf or Signumf or Conjugatef or Ref or Imf or Argf;

        private static bool IsOutsideThem(Entity expr, Variable x)
            => expr == x || !IsNotHolomorphic(expr) && expr.DirectChildren.Any(child => IsOutsideThem(child, x));

        private static bool RealWhereverItIs(Entity expr, Variable x)
            => expr switch
            {
                _ when !expr.ContainsNode(x) => expr.Evaled is Real || expr.Evaled is Complex { ImaginaryPart.IsZero: true },
                Absf => true,
                Sumf(var augend, var addend) => RealWhereverItIs(augend, x) && RealWhereverItIs(addend, x),
                Minusf(var minuend, var subtrahend) => RealWhereverItIs(minuend, x) && RealWhereverItIs(subtrahend, x),
                Mulf(var multiplier, var multiplicand) => RealWhereverItIs(multiplier, x) && RealWhereverItIs(multiplicand, x),
                Divf(var dividend, var divisor) => RealWhereverItIs(dividend, x) && RealWhereverItIs(divisor, x),
                Powf(var @base, Integer) => RealWhereverItIs(@base, x),
                _ => false,
            };

        /// <summary>
        /// The real <paramref name="x"/> at which <paramref name="expr"/> is zero, where every
        /// modulus in it with <paramref name="x"/> inside is of a polynomial with real
        /// coefficients whose real roots are found; <see langword="null"/> otherwise, or where a
        /// piece is not solved.
        /// </summary>
        /// <remarks>
        /// A polynomial keeps one sign between two neighbouring real roots, so its real roots are
        /// where a modulus of it can change from the argument to its negation, as a linear one's
        /// single root is. <c>x^2/|1 - x^2| = x</c> has kinks at -1 and 1, and on each piece an
        /// equation with no modulus, solved exactly; it was answered by a numerical search, in
        /// decimals and with two non-real numbers that solve nothing.
        /// https://github.com/asc-community/AngouriMath/issues/2139
        /// </remarks>
        internal static Set? SolveOverTheReals(Entity expr, Variable x)
        {
            var moduli = new List<Entity>();
            var kinks = new List<(Entity Exact, Real Value)>();
            foreach (var node in expr.Nodes)
            {
                if (node is not Absf(var argument) || !argument.ContainsNode(x))
                    continue;
                if (RealRootsOf(argument, x) is not { } roots)
                    return null;
                moduli.Add(argument);
                kinks.AddRange(roots);
            }
            if (moduli.Count == 0)
                return null;
            var ends = kinks.GroupBy(kink => kink.Value).Select(same => same.First())
                .OrderBy(kink => kink.Value.EDecimal).Select(kink => kink.Exact).ToList();

            // The pieces between neighbouring kinks, closed where they end at one, and on each the
            // sign every modulus takes, read at a point inside it.
            Set answer = Set.Empty;
            for (var i = 0; i <= ends.Count; i++)
            {
                Entity left = i == 0 ? Real.NegativeInfinity : ends[i - 1];
                Entity right = i == ends.Count ? Real.PositiveInfinity : ends[i];
                Entity inside = ends.Count == 0 ? Integer.Zero
                    : i == 0 ? ends[0] - 1 : i == ends.Count ? ends[^1] + 1 : (ends[i - 1] + ends[i]) / 2;
                var signs = new Dictionary<Entity, bool>();
                foreach (var argument in moduli)
                    if (!signs.ContainsKey(argument))
                    {
                        if (argument.Substitute(x, inside).Evaled is not Real value)
                            return null;
                        signs[argument] = !value.IsNegative;
                    }
                var withoutModuli = expr.Replace(node => node is Absf(var argument) && signs.TryGetValue(argument, out var nonNegative)
                    ? (nonNegative ? argument : -argument)
                    : node).InnerSimplified;
                Set piece = new Interval(left, i > 0, right, i < ends.Count);
                if (!withoutModuli.ContainsNode(x))
                {
                    if (withoutModuli.Evaled is Complex { IsZero: true })
                        answer = answer.Unite(piece);
                    continue;
                }
                var solved = AnalyticalEquationSolver.Solve(withoutModuli, x);
                // An equation that holds everywhere on the piece has the whole piece for solutions:
                // |x - 1| + |x - 2| = 1 is every x from 1 to 2, the solver answering such an
                // identity with every number.
                if (solved == MathS.Sets.C)
                {
                    answer = answer.Unite(piece);
                    continue;
                }
                if (solved is not FiniteSet roots)
                    return null;
                foreach (var root in roots)
                    if (root.Evaled is Real value && Within(value, left, right))
                        answer = answer.Unite(new FiniteSet(root));
            }
            return (Set)answer.InnerSimplified;
        }

        /// <summary>
        /// The real roots of <paramref name="argument"/>, exact and with their values, where it is a
        /// polynomial in <paramref name="x"/> with real numeric coefficients whose roots the solver
        /// finds; <see langword="null"/> otherwise.
        /// </summary>
        private static List<(Entity Exact, Real Value)>? RealRootsOf(Entity argument, Variable x)
        {
            if (TreeAnalyzer.TryGetPolyLinear(argument, x, out var slope, out var intercept))
                return !intercept.ContainsNode(x) && slope.Evaled is Real { IsZero: false }
                    && (-intercept / slope).InnerSimplified is Real kink
                    ? new() { (kink, kink) }
                    : null;
            if (!TreeAnalyzer.TryGetPolynomial(argument, x, out var monomials)
                || monomials.Count == 0
                || monomials.Keys.Any(power => power.Sign < 0)
                || monomials.Values.Any(coefficient => coefficient.ContainsNode(x) || coefficient.Evaled is not Real))
                return null;
            if (AnalyticalEquationSolver.Solve(argument, x) is not FiniteSet roots)
                return null;
            var real = new List<(Entity, Real)>();
            foreach (var root in roots)
                switch (root.Evaled)
                {
                    case Real value:
                        real.Add((root, value));
                        break;
                    case Complex { ImaginaryPart.IsZero: true } value:
                        real.Add((root, value.RealPart));
                        break;
                }
            return real;
        }

        private static bool Within(Real value, Entity left, Entity right)
            => left.Evaled is Real low && right.Evaled is Real high
                && low.EDecimal.CompareTo(value.EDecimal) <= 0 && value.EDecimal.CompareTo(high.EDecimal) <= 0;
    }
}
