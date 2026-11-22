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

namespace AngouriMath.Functions
{
    /// <summary>
    /// A summation whose summand is a polynomial in the index times a power with the index in
    /// the exponent: <c>sum(k 2^k, k, 1, n)</c> is <c>(n - 1) 2^(n + 1) + 2</c>, and
    /// <c>sum((-1)^(k - 1) k^2, k, 1, n)</c> is <c>(-1)^(n - 1) n (n + 1)/2</c>, which is
    /// §5.3.4 Try 6 of Sullivan and Mackey's <i>An Introduction to Proofs</i>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The summand is read as <c>p(k) C b^(m k + s)</c>, which is <c>C b^s p(k) r^k</c> with the
    /// ratio <c>r = b^m</c>, the way <see cref="GeometricSeries"/> reads its summand, with a
    /// polynomial <c>p</c> of degree at least one beside the power. For a ratio other than 1
    /// there is a polynomial <c>q</c> of the same degree with <c>q(k + 1) r - q(k) = p(k)</c>,
    /// so that <c>q(k) r^k</c> is a discrete antiderivative of the summand, and the sum from
    /// <c>a</c> to <c>b</c> is <c>q(b + 1) r^(b + 1) - q(a) r^a</c>. The coefficients of
    /// <c>q</c> are found highest first: the coefficient of <c>k^j</c> in <c>q(k + 1) r - q(k)</c>
    /// is <c>(r - 1) q_j + r sum_{i > j} binomial(i, j) q_i</c>, one division by <c>r - 1</c>
    /// each, which is the whole of the linear algebra -- Gosper's algorithm specialised to a
    /// summand whose ratio of consecutive terms is a rational function with a constant
    /// denominator.
    /// </para>
    /// <para>
    /// At <c>r = 1</c> the summand is the polynomial, which <see cref="PolynomialSummation"/>
    /// answers; a symbolic ratio carries <c>provided not r = 1</c>, since the formula divides
    /// by <c>r - 1</c>. To <c>+oo</c> the series converges exactly when <c>|r| &lt; 1</c>, to
    /// <c>-q(a) r^a</c>, since <c>q(k) r^k</c> then goes to zero. Part of
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1409">#1409</a>.
    /// </para>
    /// </remarks>
    internal static class PolynomialGeometricSeries
    {
        /// <summary>
        /// <c>sum(expression, index, from, to)</c> in closed form, or <see langword="null"/>
        /// where the summand is not a polynomial in the index times a power with a whole
        /// multiple of the index in its exponent, or a bound is a number that is neither whole
        /// nor <c>+oo</c>.
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

            // `k/2^k` as `k 2^(-k)`: the power below the bar is the power with its exponent
            // negated, and the factors are then read as they are read above it.
            var summand = expression.InnerSimplified;
            if (summand is Divf(var over, Powf(var underBase, var underExponent)) && !underBase.ContainsNode(index) && underExponent.ContainsNode(index))
                summand = over * MathS.Pow(underBase, (-underExponent).InnerSimplified);
            Entity? @base = null;
            Entity? exponent = null;
            Entity constant = Integer.One;
            Entity? polynomial = null;
            foreach (var factor in Mulf.LinearChildren(summand))
            {
                if (!factor.ContainsNode(index))
                    constant *= factor;
                else if (factor is Powf(var b, var e) && @base is null && !b.ContainsNode(index) && e.ContainsNode(index))
                {
                    @base = b;
                    exponent = e;
                }
                else
                    polynomial = polynomial is null ? factor : polynomial * factor;
            }
            if (@base is null || polynomial is null || !GeometricSeries.TryReadLinear(exponent!, index, out var slope, out var offset))
                return null;
            if (!TreeAnalyzer.TryGetPolynomial(polynomial, index, out var monomials) || monomials.Count == 0
                || monomials.Keys.Any(power => power.Sign < 0 || !power.CanFitInInt32()))
                return null;
            var degree = monomials.Keys.Max()!.ToInt32Checked();
            if (degree < 1 || degree > 12)
                return null;
            if (@base.Evaled is Complex value && value.IsZero)
                return null;

            if (offset != Integer.Zero)
                constant *= MathS.Pow(@base, offset);
            var ratio = slope == 1 ? @base : MathS.Pow(@base, Integer.Create(slope));
            var ratioIsANumber = ratio.Evaled is Complex;
            if (ratioIsANumber && ratio.Evaled == Integer.One)
                return null;

            // The coefficients of q, highest first: (r - 1) q_j + r sum_{i > j} C(i, j) q_i = p_j.
            var p = new Entity[degree + 1];
            for (var j = 0; j <= degree; j++)
                p[j] = monomials.TryGetValue(EInteger.FromInt32(j), out var coefficient) ? coefficient : Integer.Zero;
            var q = new Entity[degree + 1];
            var rMinusOne = ratio - Integer.One;
            for (var j = degree; j >= 0; j--)
            {
                Entity above = Integer.Zero;
                for (var i = j + 1; i <= degree; i++)
                    above += Integer.Create(Binomial(i, j)) * q[i];
                q[j] = ((p[j] - ratio * above) / rMinusOne).InnerSimplified;
            }
            Entity Q(Entity at)
            {
                Entity sum = Integer.Zero;
                for (var j = 0; j <= degree; j++)
                    if (q[j] != Integer.Zero)
                        sum += j == 0 ? q[j] : q[j] * MathS.Pow(at, j);
                return sum;
            }

            if (toInfinity)
            {
                Entity limit = -constant * Q(from) * PowerAt(ratio, from);
                if (ratioIsANumber)
                    return ((Complex)ratio.Evaled).Abs() < 1 ? limit.InnerSimplified : null;
                return new Providedf(limit, new Lessf(MathS.Abs(ratio), Integer.One)).InnerSimplified;
            }

            var after = (to + Integer.One).InnerSimplified;
            var closed = PolynomialSummation.Simplest((constant * (Q(after) * PowerAt(ratio, after) - Q(from) * PowerAt(ratio, from))).InnerSimplified);
            var nonEmpty = PolynomialSummation.NonEmpty(from, to);
            if (ratioIsANumber)
                return MathS.Piecewise(new[]
                {
                    new Providedf(closed, nonEmpty),
                    new Providedf(Integer.Zero, Entity.Boolean.True),
                }).InnerSimplified;
            // At r = 1 the summand is the polynomial, and that sum is the polynomial reader's:
            // its closed form is the first case of what it answers, over the same range.
            Entity atOne = new Summationf(constant * polynomial, index, from, to);
            if (PolynomialSummation.ClosedForm(constant * polynomial, index, from, to) is Piecewise piecewise
                && piecewise.Cases.FirstOrDefault() is Providedf(var polynomialSum, _))
                atOne = polynomialSum;
            return MathS.Piecewise(new[]
            {
                new Providedf(atOne, new Andf(new Equalsf(ratio, Integer.One), nonEmpty)),
                new Providedf(closed, nonEmpty),
                new Providedf(Integer.Zero, Entity.Boolean.True),
            }).InnerSimplified;
        }

        /// <summary>
        /// <c>ratio ^ bound</c>, as the number 1 where the bound is 0, for the reason
        /// <see cref="GeometricSeries"/> gives.
        /// </summary>
        private static Entity PowerAt(Entity ratio, Entity bound)
            => bound.Evaled == Integer.Zero ? Integer.One : MathS.Pow(ratio, bound);

        private static EInteger Binomial(int n, int k)
        {
            var result = EInteger.One;
            for (var i = 1; i <= k; i++)
                result = result.Multiply(n - k + i).Divide(i);
            return result;
        }
    }
}
