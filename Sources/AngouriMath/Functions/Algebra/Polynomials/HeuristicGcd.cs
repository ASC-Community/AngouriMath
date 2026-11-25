//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using AngouriMath.Core.Multithreading;
using System.Collections.Generic;

namespace AngouriMath.Functions
{
    /// <summary>
    /// The greatest common divisor of two polynomials by <b>evaluation at a large integer</b>,
    /// after Char, Geddes and Gonnet's GCDHEU: one variable is replaced by an integer
    /// <c>ξ</c>, the gcd of what is left is found the same way, and the answer is read back
    /// from its digits in base <c>ξ</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why it is asked at all.</b> <see cref="PolynomialGcd"/> runs the subresultant
    /// remainder sequence, whose intermediates multiply through by leading coefficients that
    /// are themselves polynomials. A product of three factors in three variables and its
    /// derivative already take it past <see cref="MultivariatePolynomial.MaxIntermediateTerms"/>.
    /// This never builds a polynomial larger than its inputs: the size goes into the integers
    /// instead, which is where arbitrary precision is cheap.
    /// </para>
    /// <para>
    /// <b>Why the answer can be believed.</b> With <c>ξ</c> above twice the smaller of the
    /// two inputs' largest coefficients, a candidate read back from the digits that divides
    /// both inputs is their greatest common divisor (Char, Geddes and Gonnet, "GCDHEU:
    /// Heuristic polynomial GCD algorithm based on integer GCD computation", 1989).
    /// Whether it divides is checked by exact division, so a <c>ξ</c> that was unlucky costs
    /// another attempt or a refusal and never a wrong answer.
    /// </para>
    /// </remarks>
    internal static class HeuristicGcd
    {
        /// <summary>Values of <c>ξ</c> tried at each level before giving up.</summary>
        private const int MaxAttempts = 6;

        /// <summary>
        /// The most bits an evaluated coefficient may need, <c>log2 ξ</c> times the degree
        /// being evaluated away. Past this the integers are what costs, and it is refused.
        /// </summary>
        private const int MaxBits = 1 << 16;

        /// <summary>
        /// The greatest common divisor of <paramref name="left"/> and
        /// <paramref name="right"/>, normalised to whole coprime coefficients with a positive
        /// leading one, or <see langword="null"/> where no attempt was settled.
        /// </summary>
        internal static MultivariatePolynomial? Gcd(MultivariatePolynomial left, MultivariatePolynomial right)
        {
            if (left.IsZero)
                return right.Normalized();
            if (right.IsZero)
                return left.Normalized();
            return Whole(left.Normalized(), right.Normalized())?.Normalized();
        }

        /// <summary>The gcd of two polynomials with whole coefficients, content included.</summary>
        private static MultivariatePolynomial? Whole(MultivariatePolynomial left, MultivariatePolynomial right)
        {
            var count = left.VariableCount;
            if (left.IsZero)
                return right;
            if (right.IsZero)
                return left;

            var leftContent = Content(left);
            var rightContent = Content(right);
            var common = MultivariatePolynomial.Constant(count,
                ERational.FromEInteger(leftContent.GreatestCommonDivisor(rightContent)));
            if (left.IsConstant || right.IsConstant)
                return common;

            // A variable the two share; evaluating away the one of lowest degree keeps the
            // integers smallest, since they grow as ξ to that degree.
            var variable = -1;
            var lowest = int.MaxValue;
            for (var candidate = 0; candidate < count; candidate++)
            {
                var degree = System.Math.Max(left.DegreeIn(candidate), right.DegreeIn(candidate));
                if (left.DegreeIn(candidate) > 0 && right.DegreeIn(candidate) > 0 && degree < lowest)
                {
                    lowest = degree;
                    variable = candidate;
                }
            }
            if (variable < 0)
                return common;

            var a = left.ScaleBy(ERational.Create(EInteger.One, leftContent));
            var b = right.ScaleBy(ERational.Create(EInteger.One, rightContent));
            var at = EInteger.FromInt32(2).Multiply(EIntegerMin(Norm(a), Norm(b))).Add(EInteger.FromInt32(29));

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                MultithreadingFunctional.ExitIfCancelled();
                if ((long)at.GetUnsignedBitLengthAsInt64() * lowest > MaxBits)
                    return null;
                if (Whole(Evaluate(a, variable, at), Evaluate(b, variable, at)) is { } image
                    && Interpolate(image, variable, at) is { } candidate
                    && !candidate.IsZero)
                {
                    var primitive = candidate.ScaleBy(ERational.Create(EInteger.One, Content(candidate)));
                    if (a.DivideExact(primitive, MultivariatePolynomial.MaxIntermediateTerms) is not null
                        && b.DivideExact(primitive, MultivariatePolynomial.MaxIntermediateTerms) is not null)
                        return primitive.Multiply(common, MultivariatePolynomial.MaxIntermediateTerms);
                }
                // Grown by about 1 + sqrt(3), as SymPy's implementation does, so that the next
                // value is not a multiple of the last and does not share what made it unlucky.
                at = at.Multiply(EInteger.FromInt32(73794)).Divide(EInteger.FromInt32(27011));
            }
            return null;
        }

        /// <summary><paramref name="poly"/> with <paramref name="variable"/> replaced by <paramref name="at"/>.</summary>
        private static MultivariatePolynomial Evaluate(MultivariatePolynomial poly, int variable, EInteger at)
        {
            var result = MultivariatePolynomial.Zero(poly.VariableCount);
            foreach (var pair in poly.CoefficientsIn(variable))
                result = result.Add(pair.Value.ScaleBy(ERational.FromEInteger(at.Pow(pair.Key))));
            return result;
        }

        /// <summary>
        /// The polynomial whose coefficients are the digits of <paramref name="image"/>'s in
        /// base <paramref name="at"/>, each digit taken in <c>(-ξ/2, ξ/2]</c>, the
        /// <c>k</c>th digit becoming the coefficient of <paramref name="variable"/> to the
        /// <c>k</c>th.
        /// </summary>
        private static MultivariatePolynomial? Interpolate(MultivariatePolynomial image, int variable, EInteger at)
        {
            var count = image.VariableCount;
            var half = at.Divide(EInteger.FromInt32(2));
            var result = MultivariatePolynomial.Zero(count);
            var rest = image;
            for (var power = 0; !rest.IsZero; power++)
            {
                if (power > MultivariatePolynomial.MaxDegree)
                    return null;
                var digit = MultivariatePolynomial.Zero(count);
                foreach (var monomial in rest.Monomials)
                {
                    var value = rest.CoefficientOf(monomial);
                    if (!value.Denominator.Equals(EInteger.One))
                        return null;
                    var remainder = value.Numerator.Mod(at);
                    if (remainder.CompareTo(half) > 0)
                        remainder = remainder.Subtract(at);
                    if (!remainder.IsZero)
                        digit = digit.Add(MultivariatePolynomial.Term(count, monomial, ERational.FromEInteger(remainder)));
                }
                if (!digit.IsZero)
                {
                    if (digit.ShiftedBy(variable, power) is not { } shifted)
                        return null;
                    result = result.Add(shifted);
                }
                rest = rest.Subtract(digit).ScaleBy(ERational.Create(EInteger.One, at));
            }
            return result;
        }

        /// <summary>The gcd of the coefficients, which are whole.</summary>
        private static EInteger Content(MultivariatePolynomial poly)
        {
            var result = EInteger.Zero;
            foreach (var monomial in poly.Monomials)
                result = result.GreatestCommonDivisor(poly.CoefficientOf(monomial).Numerator);
            return result.IsZero ? EInteger.One : result;
        }

        /// <summary>The largest coefficient in absolute value.</summary>
        private static EInteger Norm(MultivariatePolynomial poly)
        {
            var result = EInteger.Zero;
            foreach (var monomial in poly.Monomials)
            {
                var value = poly.CoefficientOf(monomial).Numerator.Abs();
                if (value.CompareTo(result) > 0)
                    result = value;
            }
            return result;
        }

        private static EInteger EIntegerMin(EInteger left, EInteger right)
            => left.CompareTo(right) <= 0 ? left : right;
    }
}
