//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    /// <summary>
    /// A summation whose summand is a power with the index in the exponent, times something free
    /// of the index: <c>sum(x^k, k, 0, n)</c> is <c>(1 - x^(n + 1)) / (1 - x)</c>, and
    /// <c>sum(2^(-k), k, 0, +oo)</c> is <c>2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The summand is read as <c>C * b^(m k + s)</c>, which is <c>C b^s * r^k</c> with the ratio
    /// <c>r = b^m</c> for a whole non-zero <c>m</c>. Between two bounds the sum is
    /// <c>C b^s (r^a - r^(b + 1)) / (1 - r)</c>, which holds for every pair of integers with
    /// <c>b >= a - 1</c> and every ratio but 1 -- at <c>r = 1</c> the sum is the number of terms
    /// times the constant, and below <c>b = a - 1</c> the range is empty, which this library
    /// answers with <c>0</c>. Where the ratio is a number the branch that applies is known; where it
    /// is symbolic, both are offered as a piecewise, the way
    /// <see cref="PolynomialSummation"/> offers the empty range.
    /// </para>
    /// <para>
    /// To <c>+oo</c> the series converges exactly when <c>|r| &lt; 1</c>, to <c>C b^s r^a / (1 - r)</c>.
    /// A numeric ratio outside that is left as written -- the sum is infinite or has no value,
    /// and which of the two is not this reader's to say -- and a symbolic ratio carries the
    /// condition, since outside it the sum has no value the formula could be standing for.
    /// </para>
    /// <para>
    /// Recognised structurally, on the factors of the simplified summand: exactly one power whose
    /// base is free of the index and whose exponent is the index times a whole number plus
    /// something free of the index; every other factor free of the index. A polynomial factor in
    /// the index -- <c>k x^k</c> -- is not this family and is declined, and so is a base that is
    /// zero or a ratio that is a number and 1. Part of question I.2 of
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1212">#1212</a>, where the
    /// step integral of <c>2^(-floor(x))</c> leaves this sum.
    /// </para>
    /// </remarks>
    internal static class GeometricSeries
    {
        /// <summary>
        /// <c>sum(expression, index, from, to)</c> in closed form, or <see langword="null"/>
        /// where the summand is not of the shape above, or a bound is a number that is neither
        /// whole nor <c>+oo</c>.
        /// </summary>
        internal static Entity? ClosedForm(Entity expression, Entity var, Entity from, Entity to)
        {
            if (var is not Variable index)
                return null;
            if (!PolynomialSummation.IsWholeOrSymbolic(from))
                return null;
            var toInfinity = to.Evaled is Real { IsFinite: false, IsNaN: false, IsNegative: false };
            if (!toInfinity && !PolynomialSummation.IsWholeOrSymbolic(to))
                return null;

            Entity? @base = null;
            Entity? exponent = null;
            Entity constant = Integer.One;
            foreach (var factor in Mulf.LinearChildren(expression.InnerSimplified))
            {
                if (!factor.ContainsNode(index))
                {
                    constant *= factor;
                    continue;
                }
                if (factor is Powf(var b, var e) && @base is null && !b.ContainsNode(index))
                {
                    @base = b;
                    exponent = e;
                }
                else
                    return null;
            }
            if (@base is null || !TryReadLinear(exponent!, index, out var slope, out var offset))
                return null;
            if (@base.Evaled is Complex value && value.IsZero)
                return null;

            // C b^(m k + s) = (C b^s) r^k with r = b^m. Not b^0: that would attach
            // `provided not b = 0`, which a base already known not to be zero does not need.
            if (offset != Integer.Zero)
                constant *= MathS.Pow(@base, offset);
            var ratio = slope == 1 ? @base : MathS.Pow(@base, Integer.Create(slope));
            var ratioIsANumber = ratio.Evaled is Complex;
            if (ratioIsANumber && ratio.Evaled == Integer.One)
                return null;

            if (toInfinity)
            {
                Entity limit = constant * PowerAt(ratio, from) / (Integer.One - ratio);
                if (ratioIsANumber)
                    return ((Complex)ratio.Evaled).Abs() < 1 ? limit.InnerSimplified : null;
                return new Providedf(limit, new Lessf(MathS.Abs(ratio), Integer.One)).InnerSimplified;
            }

            // The terms from a to b, as the difference of two tails of the same series.
            Entity closed = constant * (PowerAt(ratio, from) - PowerAt(ratio, to + Integer.One)) / (Integer.One - ratio);
            var nonEmpty = PolynomialSummation.NonEmpty(from, to);
            if (ratioIsANumber)
                return MathS.Piecewise(new[]
                {
                    new Providedf(closed, nonEmpty),
                    new Providedf(Integer.Zero, Entity.Boolean.True),
                }).InnerSimplified;
            Entity constantTerms = constant * (to - from + Integer.One);
            return MathS.Piecewise(new[]
            {
                new Providedf(constantTerms, new Andf(new Equalsf(ratio, Integer.One), nonEmpty)),
                new Providedf(closed, nonEmpty),
                new Providedf(Integer.Zero, Entity.Boolean.True),
            }).InnerSimplified;
        }

        /// <summary>
        /// <c>ratio ^ bound</c>, as the number 1 where the bound is 0: <c>r^0</c> would attach
        /// <c>provided not r = 0</c> to a sum whose value at <c>r = 0</c> is its first term,
        /// which the formula gives without help.
        /// </summary>
        private static Entity PowerAt(Entity ratio, Entity bound)
            => bound.Evaled == Integer.Zero ? Integer.One : MathS.Pow(ratio, bound);

        /// <summary>
        /// <paramref name="exponent"/> as <c>slope * index + offset</c> for a whole non-zero
        /// <paramref name="slope"/> and an <paramref name="offset"/> free of the index, which may
        /// be symbolic. Read off the tree: <c>k - k</c> does not simplify to a bare 0 but to 0
        /// with what it assumes, so the exponent is not rewritten to find out.
        /// </summary>
        internal static bool TryReadLinear(Entity exponent, Variable index, out int slope, out Entity offset)
        {
            offset = Integer.Zero;
            switch (exponent)
            {
                case Sumf(var left, var right) when !right.ContainsNode(index) && TryReadSlope(left, index, out slope):
                    offset = right;
                    return true;
                case Sumf(var left, var right) when !left.ContainsNode(index) && TryReadSlope(right, index, out slope):
                    offset = left;
                    return true;
                case Minusf(var left, var right) when !right.ContainsNode(index) && TryReadSlope(left, index, out slope):
                    offset = -right;
                    return true;
                case Minusf(var left, var right) when !left.ContainsNode(index) && TryReadSlope(right, index, out slope):
                    offset = left;
                    slope = -slope;
                    return true;
                default:
                    return TryReadSlope(exponent, index, out slope);
            }
        }

        /// <summary>
        /// <paramref name="term"/> as <c>slope * index</c> for a whole non-zero
        /// <paramref name="slope"/>: the index itself, or the index times a whole literal.
        /// </summary>
        private static bool TryReadSlope(Entity term, Variable index, out int slope)
        {
            slope = 1;
            if (term == index)
                return true;
            var literal = term switch
            {
                Mulf(Integer m, var k) when k == index => m,
                Mulf(var k, Integer m) when k == index => m,
                _ => null,
            };
            if (literal is null || literal.EInteger.IsZero || !literal.EInteger.CanFitInInt32())
                return false;
            slope = literal.EInteger.ToInt32Checked();
            return true;
        }
    }
}
