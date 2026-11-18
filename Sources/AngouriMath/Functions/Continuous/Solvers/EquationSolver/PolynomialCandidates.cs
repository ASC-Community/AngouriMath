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
    /// The roots of an equation in inverse functions, from a polynomial equation that has every one
    /// of them and possibly more.
    /// </summary>
    internal static class PolynomialCandidates
    {
        /// <summary>
        /// The positive rational that the <paramref name="multipliers"/> of a sum of functions are
        /// whole and coprime multiples of: the greatest common divisor of their numerators over the
        /// least common multiple of their denominators. Dividing the sum by it changes no root, and
        /// leaves the multipliers the equation means rather than the number it was written times:
        /// <c>(ln(a) + ln(b)) 10^12 = 0</c> read each logarithm as raised to the 10^12th power, and
        /// overflowed an <see cref="int"/> working out the degree.
        /// </summary>
        internal static ERational CommonFactor(IEnumerable<ERational> multipliers)
        {
            var numerator = EInteger.Zero;
            var denominator = EInteger.One;
            foreach (var multiplier in multipliers)
            {
                numerator = numerator.Gcd(multiplier.Numerator.Abs());
                denominator = denominator.Multiply(multiplier.Denominator).Divide(denominator.Gcd(multiplier.Denominator));
            }
            return numerator.IsZero ? ERational.One : ERational.Create(numerator, denominator);
        }

        /// <summary>
        /// <paramref name="multiplier"/> over <paramref name="factor"/>, a <see cref="CommonFactor"/>
        /// it is a whole multiple of. In whole numbers: an <see cref="ERational"/> quotient is not
        /// reduced, and (1/2) / (1/2) is 2/2, whose numerator is 2.
        /// </summary>
        internal static EInteger WholeMultiple(ERational multiplier, ERational factor)
            => multiplier.Numerator.Multiply(factor.Denominator).Divide(multiplier.Denominator.Multiply(factor.Numerator));

        /// <summary>
        /// The roots of <paramref name="product"/> = 0, a polynomial in <paramref name="x"/> of degree
        /// at most <paramref name="degree"/> with every root of <paramref name="expr"/> = 0 among its
        /// own, that are roots of <paramref name="expr"/> = 0; or <see langword="null"/> where that
        /// cannot be decided.
        /// </summary>
        internal static Set? RootsAmong(Entity expr, Variable x, Entity product, int degree)
        {
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
                    // A candidate on another branch leaves a residual clear of zero, and one where a
                    // function has no value leaves none: either is plainly no root. A residual too
                    // small to tell from rounding is not evidence, and dropping the candidate on it
                    // could leave a root out, so the equation is not answered.
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
    }
}
