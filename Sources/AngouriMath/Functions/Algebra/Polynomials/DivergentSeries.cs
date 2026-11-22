//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions
{
    /// <summary>
    /// A summation to <c>+oo</c> whose terms do not tend to zero, which therefore has no finite
    /// value: <c>sum(2^k, k, 0, +oo)</c> and <c>sum(n^n, n, 1, +oo)</c> are <c>+oo</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <b>nth-term test</b>: if the terms do not tend to zero the series diverges. That alone
    /// says a series has no sum; it does not say what to answer instead. What settles the answer is
    /// the <em>sign</em> of the limit — where the terms tend to a positive <c>L</c>, they are
    /// eventually all above <c>L / 2</c>, so the partial sums pass every bound and the value is
    /// <c>+oo</c>; a negative limit gives <c>-oo</c> the same way. The finitely many terms before
    /// that point are finite and cannot change it.
    /// </para>
    /// <para>
    /// <b>A limit of zero is declined, and that is the whole difficulty of the test.</b> It is
    /// exactly the case the nth-term test says nothing about: <c>sum(1/k)</c> diverges and
    /// <c>sum(1/k^2)</c> converges, and their terms both tend to 0. Answering either from this
    /// reader would be a guess. So would a limit that does not exist — <c>sum((-1)^k)</c> has no
    /// value rather than an infinite one — and that is left as written too, since telling "the
    /// limit does not exist" apart from "the limit was not computed" is not something to infer
    /// from a failed computation.
    /// </para>
    /// <para>
    /// <b>The summand must have no pole in the index, and this is checked before the limit is
    /// asked for.</b> A single undefined term makes the whole sum undefined, not infinite:
    /// <c>sum(k / (k - 5), k, 0, +oo)</c> has terms tending to 1, and answering <c>+oo</c> would
    /// be wrong because the term at <c>k = 5</c> does not exist. Rather than hunt for poles, the
    /// index is allowed to occur only where none can arise — under <c>+</c>, <c>-</c>, <c>*</c>,
    /// and powers of the three shapes <see cref="HasNoPoleInTheIndex"/> lists. Division by
    /// anything containing the index, a factorial of it, a logarithm of it and the rest are
    /// declined outright. That check is also what keeps this cheap: it runs first, so a summand
    /// this cannot speak about never reaches the limit engine.
    /// </para>
    /// <para>
    /// Last in the chain, after every closed form, so that a series which <em>does</em> converge is
    /// summed rather than tested. Asked for on the review of
    /// <a href="https://github.com/asc-community/AngouriMath/pull/1218">#1218</a>, where
    /// <c>integral(floor(x)^floor(x), x, 1, +oo)</c> splits into <c>sum(n^n, n, 1, +oo)</c> and was
    /// left as written for want of this; part of
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1212">#1212</a>.
    /// </para>
    /// </remarks>
    internal static class DivergentSeries
    {
        /// <summary>
        /// <c>+oo</c> or <c>-oo</c> where the terms tend to a non-zero limit of that sign, and
        /// <see langword="null"/> wherever the test does not settle the question.
        /// </summary>
        internal static Entity? ClosedForm(Entity expression, Entity var, Entity from, Entity to)
        {
            if (var is not Variable index)
                return null;
            // To +oo only: a finite range is a finite sum, and the expansion has had it already.
            if (to.Evaled is not Real { IsFinite: false, IsNaN: false, IsNegative: false })
                return null;
            // A concrete whole lower bound, so that the range is a genuine tail of the integers.
            // A symbolic one could be -oo, where there is no first term to be finite.
            if (from.Evaled is not Integer start || !start.EInteger.CanFitInInt32())
                return null;
            // A condition that holds for every index in range says nothing about the sum and is
            // dropped; one that does not is a term that may fail to exist, and the pole check
            // below then declines it as the Providedf it still is.
            var summand = WithoutConditionsThatHold(expression.InnerSimplified, index, start.EInteger.ToInt32Checked());
            if (!HasNoPoleInTheIndex(summand, index, start))
                return null;

            var limit = summand.Limit(index, Real.PositiveInfinity).Evaled;
            return limit switch
            {
                // Tends to +oo or -oo: the terms pass every bound, so the sums do.
                Real { IsFinite: false, IsNaN: false } infinite => infinite,
                // Tends to a non-zero L: eventually every term has L's sign and half its size.
                Real { IsFinite: true } value when !value.IsZero =>
                    value.IsNegative ? Real.NegativeInfinity : Real.PositiveInfinity,
                // A zero limit says nothing, and anything else was not settled.
                _ => null
            };
        }

        /// <summary>
        /// Whether the index can only occur where no term can fail to exist: under <c>+</c>,
        /// <c>-</c> and <c>*</c>, and in a power that is one of three safe shapes — a whole
        /// non-negative exponent (<c>k ^ 3</c>), a base free of the index and not zero
        /// (<c>2 ^ k</c>), or the index itself raised to something over a range that starts at 1
        /// or above (<c>n ^ n</c>). Anything else containing the index is declined.
        /// </summary>
        private static bool HasNoPoleInTheIndex(Entity expression, Variable index, Integer start)
        {
            // Free of the index is free of poles *in the index*, whatever else it is.
            if (!expression.ContainsNode(index))
                return true;
            return expression switch
            {
                Variable => true,
                Sumf(var left, var right) => Both(left, right),
                Minusf(var left, var right) => Both(left, right),
                Mulf(var left, var right) => Both(left, right),
                // k ^ 3: a whole non-negative exponent is a repeated product.
                Powf(var @base, Integer { IsNegative: false }) => Go(@base),
                // 2 ^ k: a base that does not vary and is not zero.
                Powf(var @base, var exponent) when !@base.ContainsNode(index)
                    && @base.Evaled is Complex value && !value.IsZero => Go(exponent),
                // n ^ n: the base is the index, which over a range starting at 1 or above is
                // positive, so the power exists at every term of it.
                Powf(var @base, var exponent) when @base == index && start.EInteger.Sign > 0
                    => Go(exponent),
                _ => false
            };

            bool Go(Entity part) => HasNoPoleInTheIndex(part, index, start);
            bool Both(Entity left, Entity right) => Go(left) && Go(right);
        }

        /// <summary>
        /// The summand with any attached condition removed that is true for every index in the
        /// range, and left alone otherwise.
        /// </summary>
        /// <remarks>
        /// <c>integral(n ^ n, t, 0, 1)</c> is <c>n ^ n provided not n = 0 or n &gt; 0</c>, because
        /// <c>0 ^ 0</c> is the one power that has to say what it assumes — so the summand the step
        /// split hands over carries a condition that is simply true from 1 upwards.
        /// <see cref="ExponentialSeries"/> has a sibling of this specialised to the clauses a
        /// division by a factorial carries; the two vocabularies do not overlap, and a third
        /// reader needing one would be the point at which to merge them rather than now.
        /// </remarks>
        private static Entity WithoutConditionsThatHold(Entity summand, Variable index, int start)
        {
            while (summand is Providedf(var body, var condition) && HoldsOverTheRange(condition, index, start))
                summand = body;
            return summand;
        }

        /// <summary>
        /// Whether a condition is true for every whole <c>index &gt;= start</c>. Only comparisons
        /// of the index with zero are read, since those are what a power attaches; anything else
        /// is answered <see langword="false"/>, which costs a declined summation and never a wrong
        /// sum.
        /// </summary>
        private static bool HoldsOverTheRange(Entity condition, Variable index, int start)
            => condition switch
            {
                Greaterf(var argument, var zero)
                    when IsExactlyZero(zero) && TryReadShift(argument, index, out var a) && start + a > 0 => true,
                GreaterOrEqualf(var argument, var zero)
                    when IsExactlyZero(zero) && TryReadShift(argument, index, out var a) && start + a >= 0 => true,
                // Never zero because it is always above it; the other direction is not read.
                Notf(Equalsf(var argument, var zero))
                    when IsExactlyZero(zero) && TryReadShift(argument, index, out var a) && start + a > 0 => true,
                Andf(var left, var right)
                    => HoldsOverTheRange(left, index, start) && HoldsOverTheRange(right, index, start),
                Orf(var left, var right)
                    => HoldsOverTheRange(left, index, start) || HoldsOverTheRange(right, index, start),
                _ => false,
            };

        private static bool IsExactlyZero(Entity what) => what.Evaled is Integer { IsZero: true };

        /// <summary>
        /// <paramref name="argument"/> as <c>index + shift</c> for a whole <c>shift</c>: the index
        /// itself, or the index plus or minus a whole number, in either order. Read off the tree,
        /// because <c>k - k</c> simplifies to a conditional zero rather than to nothing
        /// (<a href="https://github.com/asc-community/AngouriMath/issues/1174">#1174</a>).
        /// </summary>
        private static bool TryReadShift(Entity argument, Variable index, out int shift)
        {
            shift = 0;
            if (argument == index)
                return true;
            switch (argument)
            {
                case Sumf(var left, Integer right) when left == index && right.EInteger.CanFitInInt32():
                    shift = right.EInteger.ToInt32Checked();
                    return true;
                case Sumf(Integer left, var right) when right == index && left.EInteger.CanFitInInt32():
                    shift = left.EInteger.ToInt32Checked();
                    return true;
                case Minusf(var left, Integer right) when left == index && right.EInteger.CanFitInInt32():
                    shift = -right.EInteger.ToInt32Checked();
                    return true;
                default:
                    return false;
            }
        }
    }
}
