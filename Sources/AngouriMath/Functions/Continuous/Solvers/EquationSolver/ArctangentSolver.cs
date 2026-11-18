//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// An equation linear in arctangents of polynomials or quotients of polynomials,
    /// <c>m_1 arctan(a_1) + ... + m_n arctan(a_n) = c</c> with integer <c>m_j</c> and at least two
    /// arctangents, solved by taking the tangent of both sides.
    /// </summary>
    /// <remarks>
    /// Write <c>A</c> for the sum of the arctangents. Since <c>cos(arctan a) = 1 / sqrt(1 + a^2)</c>
    /// is never zero, <c>e^(i A)</c> divided by the product of the cosines is the product of the
    /// <c>1 + i a_j</c>, each to its <c>m_j</c>; as polynomials, <c>P + i Q</c>. Then
    /// <c>Q cos c - P sin c</c> is <c>sin(A - c)</c> divided by that product, which holds for complex
    /// <c>a_j</c> as well, so the polynomial equation <c>Q cos c = P sin c</c> has every root of the
    /// original and no root is lost. It also has the roots of <c>A = c + k pi</c> for every other
    /// integer <c>k</c>, so each candidate is put back into the equation as written, under the
    /// principal arctangent, and kept only where it holds. <c>arctan(x) + arctan(2x) = pi/4</c> is
    /// <c>3x = 1 - 2x^2</c>, whose roots are <c>(-3 +- sqrt(17)) / 4</c>; at the negative one the
    /// sum is <c>-3 pi / 4</c>, so the answer is <c>{ (sqrt(17) - 3) / 4 }</c>. Newton's method
    /// answered these in decimals, and <c>arctan(x) - arctan(2x) = 1</c>, whose roots are complex,
    /// not at all.
    /// </remarks>
    internal static class ArctangentSolver
    {
        /// <summary>The highest degree the polynomial equation may have.</summary>
        private const int HighestDegree = 8;

        internal static Set? Solve(Entity expr, Variable x)
        {
            if (expr.Vars.Any(variable => variable != x))
                return null;
            var gathered = new List<(ERational Multiplier, Entity Argument)>();
            Entity constant = Integer.Zero;
            if (!Gather(expr, ERational.One, x, gathered, ref constant) || gathered.Count < 2)
                return null;
            // Whole and coprime: (arctan(x) + arctan(2x) + 1) 10^12 is arctan(x) + arctan(2x) + 1,
            // and the half of a sum of arctangents is the sum.
            var common = PolynomialCandidates.CommonFactor(gathered.Select(arctangent => arctangent.Multiplier));
            var arctangents = gathered.Select(arctangent => (Multiplier: PolynomialCandidates.WholeMultiple(arctangent.Multiplier, common), arctangent.Argument)).ToList();
            constant = (constant / Rational.Create(common)).InnerSimplified;

            // P + i Q, multiplied out one factor at a time: (P + i Q)(d + i s n) for the argument
            // n / d and the sign s of its multiplier, which arctan being odd allows.
            Entity real = Integer.One, imaginary = Integer.Zero;
            var degree = 0;
            foreach (var (multiplier, argument) in arctangents)
            {
                var (numerator, denominator) = argument is Divf(var n, var d) ? (n, d) : (argument, (Entity)Integer.One);
                var largest = 0;
                foreach (var part in new[] { numerator, denominator })
                {
                    if (!TreeAnalyzer.TryGetPolynomial(part, x, out var monomials)
                        || monomials.Keys.Any(exponent => exponent.Sign < 0)
                        || monomials.Values.Any(coefficient => coefficient.ContainsNode(x)))
                        return null;
                    largest = System.Math.Max(largest, monomials.Count == 0 ? 0 : monomials.Keys.Max()!.ToInt32Checked());
                }
                if (multiplier.Abs().CompareTo(HighestDegree) > 0)
                    return null;
                degree += multiplier.Abs().ToInt32Checked() * largest;
                if (degree > HighestDegree)
                    return null;
                Entity signed = multiplier.Sign > 0 ? numerator : -numerator;
                for (var times = EInteger.Zero; times.CompareTo(multiplier.Abs()) < 0; times = times.Add(1))
                    (real, imaginary) = (real * denominator - imaginary * signed, real * signed + imaginary * denominator);
            }

            // The sum of the arctangents is c = -constant. Where cos c is zero the equation is P = 0;
            // elsewhere it is divided by cos c, so that the roots carry tan c rather than both.
            var target = (-constant).InnerSimplified;
            var product = MathS.Cos(target).Simplify() is Integer { IsZero: true }
                ? real
                : imaginary - MathS.Tan(target) * real;
            // Checked against the equation divided as its multipliers were, so that the residual has
            // the size it has at scale one, which is what the bound on it is for.
            return PolynomialCandidates.RootsAmong(expr / Rational.Create(common), x, product, degree);
        }

        /// <summary>
        /// Reads <paramref name="term"/>, times <paramref name="sign"/>, as rational multiples of
        /// arctangents of <paramref name="x"/> and a constant; <see langword="false"/> where it is
        /// anything else.
        /// </summary>
        private static bool Gather(Entity term, ERational sign, Variable x,
            List<(ERational Multiplier, Entity Argument)> arctangents, ref Entity constant)
        {
            switch (term)
            {
                case var _ when !term.ContainsNode(x):
                    constant += Rational.Create(sign) * term;
                    return true;
                case Sumf(var augend, var addend):
                    return Gather(augend, sign, x, arctangents, ref constant)
                        && Gather(addend, sign, x, arctangents, ref constant);
                case Minusf(var minuend, var subtrahend):
                    return Gather(minuend, sign, x, arctangents, ref constant)
                        && Gather(subtrahend, sign.Negate(), x, arctangents, ref constant);
                case Mulf(Rational multiplier, var rest):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, arctangents, ref constant);
                case Mulf(var rest, Rational multiplier):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, arctangents, ref constant);
                case Divf(var rest, Rational { IsZero: false } divisor):
                    return Gather(rest, sign.Divide(divisor.ERational), x, arctangents, ref constant);
                case Arctanf(var argument) when !sign.IsZero:
                    arctangents.Add((sign, argument));
                    return true;
                default:
                    return false;
            }
        }
    }
}
