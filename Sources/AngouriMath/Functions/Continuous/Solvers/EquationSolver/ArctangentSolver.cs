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
            var arctangents = new List<(EInteger Multiplier, Entity Argument)>();
            Entity constant = Integer.Zero;
            if (!Gather(expr, ERational.One, x, arctangents, ref constant) || arctangents.Count < 2)
                return null;

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
            // Written out by its coefficients, each simplified, since the polynomial solver puts
            // them into its formulas as it reads them: (-3 + sqrt(9 - 4 * -tan(1) * (-1) * 2 * -tan(1)))
            // and so on, where 2 tan(1) is meant.
            if (!TreeAnalyzer.TryGetPolynomial(product, x, out var coefficients))
                return null;
            Entity polynomial = Integer.Zero;
            foreach (var monomial in coefficients)
                polynomial += monomial.Value.Simplify() * MathS.Pow(x, Integer.Create(monomial.Key));
            polynomial = polynomial.InnerSimplified;
            if (AnalyticalEquationSolver.Solve(polynomial, x) is not FiniteSet candidates)
                return null;
            // Each candidate is to be a root of the polynomial, and a polynomial of degree one or
            // more has a complex root, as in LogarithmSolver: a candidate that is not is the
            // polynomial solver failing, and the roots it did not find would be left out.
            if (candidates.Count == 0 && degree > 0 && MathS.Settings.Codomain.Value != Core.Domain.Real)
                return null;
            foreach (var candidate in candidates)
                if (candidate.Vars.Any() || polynomial.Substitute(x, candidate).Evaled is not Complex { IsFinite: true } residual || !IsZero(residual))
                    return null;

            // Over the reals a root is to be real, which the polynomial solver does not decide for a
            // root written with a radical of a negative constant: sqrt(1 - 8 tan(1)^2).
            var overTheReals = MathS.Settings.Codomain.Value == Core.Domain.Real;
            var roots = new List<Entity>();
            foreach (var candidate in candidates)
            {
                if (overTheReals)
                {
                    Entity value;
                    using (MathS.Settings.Codomain.Set(Core.Domain.Complex))
                        value = candidate.Evaled;
                    if (value is not Complex { IsFinite: true } complexValue)
                        return null;
                    if (complexValue is not Real)
                    {
                        if (IsClearOfZero(Complex.Create(Integer.Zero, complexValue.ImaginaryPart)))
                            continue;
                        return null;
                    }
                }
                switch (expr.Substitute(x, candidate).Evaled)
                {
                    case Rational { IsZero: true }:
                    case Complex { IsFinite: true } residual when residual is not Rational && IsZero(residual):
                        roots.Add(candidate);
                        break;
                    // A candidate on another branch is off by a multiple of pi, and one where an
                    // argument is +-i or a denominator is zero has no value: either is plainly no
                    // root. A residual too small to tell from rounding is not evidence, and
                    // dropping the candidate on it could leave a root out, so the equation is not
                    // answered.
                    case Complex { IsFinite: true } residual when !IsClearOfZero(residual):
                        return null;
                    case Complex:
                        break;
                    default:
                        return null;
                }
            }
            return (Set)new FiniteSet(roots).InnerSimplified;
        }

        private static bool IsClearOfZero(Complex residual)
            => residual.Abs().EDecimal.CompareTo(EDecimal.FromString("1E-10")) > 0;

        /// <summary>
        /// Reads <paramref name="term"/>, times <paramref name="sign"/>, as integer multiples of
        /// arctangents of <paramref name="x"/> and a constant; <see langword="false"/> where it is
        /// anything else.
        /// </summary>
        private static bool Gather(Entity term, ERational sign, Variable x,
            List<(EInteger Multiplier, Entity Argument)> arctangents, ref Entity constant)
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
                case Mulf(Integer multiplier, var rest):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, arctangents, ref constant);
                case Mulf(var rest, Integer multiplier):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, arctangents, ref constant);
                case Arctanf(var argument) when sign.Denominator.Equals(EInteger.One) && !sign.IsZero:
                    arctangents.Add((sign.Numerator, argument));
                    return true;
                default:
                    return false;
            }
        }
    }
}
