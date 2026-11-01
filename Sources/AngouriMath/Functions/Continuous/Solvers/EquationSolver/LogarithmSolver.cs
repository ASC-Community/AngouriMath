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
    /// An equation linear in logarithms of polynomials to one base,
    /// <c>k_1 log_b(P_1) + ... + k_n log_b(P_n) = c</c> with rational <c>k_i</c> and at least two
    /// logarithms, solved by exponentiating.
    /// </summary>
    /// <remarks>
    /// Every root makes <c>b</c> to either side the same, so it is a root of
    /// <c>P_1^(k_1 L) ... P_n^(k_n L) = b^(c L)</c>, for <c>L</c> the common denominator of the
    /// <c>k_i</c>: a polynomial equation, with finitely many roots, which holds at every root of the
    /// original. The converse does not hold. Over the complex numbers <c>log(u) + log(v)</c> is
    /// <c>log(u v)</c> only up to a multiple of <c>2 pi i</c>, and a factor can be zero, so each
    /// candidate is put back into the equation as written, under the principal logarithm, and kept
    /// only where it holds. <c>ln(x) + ln(x + 1) = 0</c> is <c>x (x + 1) = 1</c>, whose roots are
    /// <c>(-1 +- sqrt(5)) / 2</c>; at the negative one both logarithms carry <c>i pi</c> and their sum
    /// is <c>2 pi i</c>, so the answer is <c>{ (sqrt(5) - 1) / 2 }</c>. Newton's method answered it
    /// in decimals, and <c>ln(1 + 3x^2) - ln(4x) = 1</c> with one of its two roots.
    /// </remarks>
    internal static class LogarithmSolver
    {
        /// <summary>The highest degree the exponentiated equation may have.</summary>
        private const int HighestDegree = 8;

        internal static Set? Solve(Entity expr, Variable x)
        {
            if (expr.Vars.Any(variable => variable != x))
                return null;
            var logarithms = new List<(ERational Multiplier, Entity Argument)>();
            Entity constant = Integer.Zero;
            Entity? logBase = null;
            if (!Gather(expr, ERational.One, x, logarithms, ref constant, ref logBase) || logarithms.Count < 2 || logBase is null)
                return null;

            var common = EInteger.One;
            foreach (var (multiplier, _) in logarithms)
                common = common.Multiply(multiplier.Denominator).Divide(common.Gcd(multiplier.Denominator));
            Entity numerator = Integer.One, denominator = Integer.One;
            var degree = 0;
            foreach (var (multiplier, argument) in logarithms)
            {
                // In whole numbers: an ERational product is not reduced, and (1/2) 2 is 2/2.
                var power = multiplier.Numerator.Multiply(common.Divide(multiplier.Denominator));
                if (!TreeAnalyzer.TryGetPolynomial(argument, x, out var monomials)
                    || monomials.Keys.Any(exponent => exponent.Sign < 0)
                    || monomials.Values.Any(coefficient => coefficient.ContainsNode(x)))
                    return null;
                degree += power.Abs().ToInt32Checked() * monomials.Keys.Max()!.ToInt32Checked();
                if (degree > HighestDegree)
                    return null;
                if (power.Sign > 0)
                    numerator *= MathS.Pow(argument, Integer.Create(power));
                else
                    denominator *= MathS.Pow(argument, Integer.Create(power.Negate()));
            }
            // The sum of the logarithms is -constant, so the product of the powers is b to -constant L.
            var target = MathS.Pow(logBase, -constant * Integer.Create(common)).InnerSimplified;
            var polynomial = numerator - target * denominator;
            if (AnalyticalEquationSolver.Solve(polynomial, x) is not FiniteSet candidates)
                return null;
            // Each candidate is to be a root of the polynomial: one that is not is the polynomial
            // solver failing, and the roots it did not find would be left out, the answer claiming
            // there are none. 3x^3 + 9x^2 + 9x + 1 = 0 came back as -1 three times, which is no
            // root, while a shifted cube's C was 0. And a polynomial of degree one or more has a
            // complex root, so no candidates at all is the same failure.
            if (candidates.Count == 0 && degree > 0 && MathS.Settings.Codomain.Value != Core.Domain.Real)
                return null;
            foreach (var candidate in candidates)
                if (candidate.Vars.Any() || polynomial.Substitute(x, candidate).Evaled is not Complex { IsFinite: true } residual || !IsZero(residual))
                    return null;

            // Over the reals a logarithm has a value only of a positive number, which the evaluation
            // below, over the complex numbers, does not ask.
            var overTheReals = MathS.Settings.Codomain.Value == Core.Domain.Real;
            var roots = new List<Entity>();
            foreach (var candidate in candidates)
            {
                if (candidate.Vars.Any())
                    return null;
                if (overTheReals)
                {
                    var positive = true;
                    foreach (var (_, argument) in logarithms)
                        switch (argument.Substitute(x, candidate).Evaled)
                        {
                            case Real { IsPositive: true }:
                                break;
                            case Complex value when value is Real || IsClearOfZero(Complex.Create(Integer.Zero, value.ImaginaryPart)):
                                positive = false;
                                break;
                            default:
                                return null;
                        }
                    if (!positive)
                        continue;
                }
                switch (expr.Substitute(x, candidate).Evaled)
                {
                    case Rational { IsZero: true }:
                    case Complex { IsFinite: true } residual when residual is not Rational && IsZero(residual):
                        roots.Add(candidate);
                        break;
                    // A candidate on another branch is off by a multiple of 2 pi i, and one where a
                    // factor is zero has no value: either is plainly no root. A residual too small
                    // to tell from rounding is not evidence, and dropping the candidate on it could
                    // leave a root out, so the equation is not answered.
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
        /// Reads <paramref name="term"/>, times <paramref name="sign"/>, as rational multiples of
        /// logarithms of <paramref name="x"/> to one constant base, and a constant; <see langword="false"/>
        /// where it is anything else.
        /// </summary>
        private static bool Gather(Entity term, ERational sign, Variable x,
            List<(ERational Multiplier, Entity Argument)> logarithms, ref Entity constant, ref Entity? logBase)
        {
            switch (term)
            {
                case var _ when !term.ContainsNode(x):
                    constant += Rational.Create(sign) * term;
                    return true;
                case Sumf(var augend, var addend):
                    return Gather(augend, sign, x, logarithms, ref constant, ref logBase)
                        && Gather(addend, sign, x, logarithms, ref constant, ref logBase);
                case Minusf(var minuend, var subtrahend):
                    return Gather(minuend, sign, x, logarithms, ref constant, ref logBase)
                        && Gather(subtrahend, sign.Negate(), x, logarithms, ref constant, ref logBase);
                case Mulf(Rational multiplier, var rest):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, logarithms, ref constant, ref logBase);
                case Mulf(var rest, Rational multiplier):
                    return Gather(rest, sign.Multiply(multiplier.ERational), x, logarithms, ref constant, ref logBase);
                case Divf(var rest, Rational { IsZero: false } divisor):
                    return Gather(rest, sign.Divide(divisor.ERational), x, logarithms, ref constant, ref logBase);
                case Logf(var @base, var argument) when !@base.ContainsNode(x):
                    if (logBase is null)
                        logBase = @base;
                    else if (logBase != @base)
                        return false;
                    logarithms.Add((sign, argument));
                    return true;
                default:
                    return false;
            }
        }
    }
}
