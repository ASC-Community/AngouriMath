//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;

namespace AngouriMath.Functions
{
    using static Entity;
    using static Entity.Number;

    /// <summary>
    /// Writes a rational raised to a rational power as a whole part times a radical that has
    /// nothing left to give up, so that sqrt(12) is 2 * sqrt(3) and sqrt(1/2) is sqrt(2) / 2.
    /// https://github.com/asc-community/AngouriMath/issues/281
    /// </summary>
    internal static class RootExtraction
    {
        // Trial division is the whole method, and these two bounds are what keep it from costing
        // more than the tidier answer is worth: a power is simplified over and over, and most of
        // them have nothing to give up, so the ordinary case has to be a handful of divisions.
        //
        // They are also chosen so that the extraction is *complete* rather than best-effort. Once
        // every factor below the divisor bound has been taken out, what is left is either one, a
        // prime, or a product of two primes above the bound -- three primes above 10^4 would
        // already exceed 10^12. A product of two distinct primes has nothing to extract for any
        // root, and the one remaining case, a square of a single prime, is what the perfect-power
        // check at the end is for. So nothing within the bound is missed.
        [ConstantField] private static readonly EInteger maxRadicand = EInteger.FromInt64(1_000_000_000_000L);
        [ConstantField] private static readonly EInteger maxDivisor = EInteger.FromInt32(10_000);

        /// <summary>
        /// <see cref="maxRadicand"/> as a bit count, rounded up, so that a radicand can be
        /// rejected before it is raised to the exponent's numerator.
        /// </summary>
        private const int MaxRadicandBits = 40;

        /// <summary>
        /// A root of an order beyond this is not one anybody wrote down, and the radicand is
        /// raised to the numerator before anything else happens, so both have to stay small.
        /// </summary>
        private const int MaxExponentPart = 64;

        /// <summary>
        /// The most bits a whole part taken out of a power may have, about 10^300.
        /// https://github.com/asc-community/AngouriMath/issues/2609
        /// </summary>
        private const int MaxWholePartBits = 1000;

        /// <summary>
        /// The reciprocal of <paramref name="radicand"/> ^ <paramref name="exponent"/>, written
        /// without a radical below the line, or null when the exponent is not a root at all.
        /// </summary>
        internal static Entity? PullOutOfDenominator(Rational radicand, Rational exponent)
            => PullOutOfRadical(radicand,
                Rational.Create(exponent.Numerator.EInteger.Negate(), exponent.Denominator.EInteger));

        /// <summary>
        /// Rewrites <paramref name="radicand"/> ^ <paramref name="exponent"/> as an exact factor
        /// times a radical, or returns null when there is nothing to pull out.
        /// </summary>
        internal static Entity? PullOutOfRadical(Rational radicand, Rational exponent)
        {
            // A negative radicand is declined for correctness and not out of caution: the
            // principal cube root of -8 is 1 + i * sqrt(3), not -2, so pulling the sign out
            // would quietly pick the real branch over the one the rest of the library uses.
            if (radicand.Numerator.EInteger.Sign <= 0)
                return null;
            if (!exponent.Numerator.EInteger.CanFitInInt32() || !exponent.Denominator.EInteger.CanFitInInt32())
                return null;
            var power = exponent.Numerator.EInteger.ToInt32Checked();
            var rootIndex = exponent.Denominator.EInteger.ToInt32Checked();
            // A whole exponent is somebody else's case.
            if (rootIndex < 2 || power is 0 || rootIndex > MaxExponentPart)
                return null;

            // An exponent past one takes its whole part out first: b^(p/q) is b^(p div q) times
            // b^((p mod q)/q), and only the second is a radical. Raising the radicand to all of p
            // before splitting it gave up past 2^40, so 2^(19/2) was 512 sqrt(2) and 2^(21/2)
            // was left as written, and 32768 * 2^(-29/2) did not cancel to sqrt(2).
            // https://github.com/asc-community/AngouriMath/issues/2609
            if (System.Math.Abs(power) > rootIndex)
            {
                var @base = power < 0
                    ? Rational.Create(radicand.Denominator.EInteger, radicand.Numerator.EInteger)
                    : radicand;
                var wholePower = System.Math.Abs(power) / rootIndex;
                // Kept far below the decimal context's exponent range. A product of an integer past
                // about 10^1000 and a radical is folded to +oo, so 2^4000 sqrt(2) simplifies to +oo,
                // and extracting a whole part that large would turn a power written correctly into
                // that wrong answer.
                if (Number.IsTooLargeToBuild(@base, EInteger.FromInt32(wholePower))
                    || TooLargeRaised(@base.Numerator.EInteger, wholePower, MaxWholePartBits)
                    || TooLargeRaised(@base.Denominator.EInteger, wholePower, MaxWholePartBits))
                    return null;
                var wholeFactor = Rational.Create(
                    @base.Numerator.EInteger.Pow(wholePower), @base.Denominator.EInteger.Pow(wholePower));
                var restExponent = Rational.Create(EInteger.FromInt32(System.Math.Abs(power) % rootIndex), EInteger.FromInt32(rootIndex));
                return wholeFactor * (PullOutOfRadical(@base, restExponent) ?? new Powf(@base, restExponent));
            }

            // A negative exponent is the same question asked of the reciprocal: 2^(-1/2) is
            // (1/2)^(1/2). Turning it round rather than declining it is what keeps 1/sqrt(2)
            // and sqrt(1/2) from printing differently, which they did.
            var (above, below) = power < 0
                ? (radicand.Denominator.EInteger, radicand.Numerator.EInteger)
                : (radicand.Numerator.EInteger, radicand.Denominator.EInteger);
            power = System.Math.Abs(power);

            // Checked before the power is taken rather than after. A thousand-digit rational
            // raised to the 64th is a sixty-thousand-digit integer, and building one only to
            // find it over the bound and throw it away is the whole cost of the call.
            if (TooLargeRaised(above, power) || TooLargeRaised(below, power))
                return null;

            var numerator = above.Pow(power);
            var denominator = below.Pow(power);
            if (numerator.CompareTo(maxRadicand) > 0 || denominator.CompareTo(maxRadicand) > 0)
                return null;

            Split(numerator, rootIndex, out var wholeAbove, out var underAbove);
            Split(denominator, rootIndex, out var wholeBelow, out var underBelow);

            // A radical left in the denominator is worse than the form we started from, so it is
            // moved upstairs the usual way: 1 / b^(1/n) is b^((n-1)/n) / b. What arrives up there
            // may itself have a whole part -- 1/8 under a cube root leaves 8^2, which is 4 cubed
            // -- so it goes back through the same split rather than being multiplied on as is.
            if (!underBelow.Equals(EInteger.One))
            {
                var raised = underBelow.Pow(rootIndex - 1);
                if (raised.CompareTo(maxRadicand) > 0)
                    return null;
                var moved = underAbove.Multiply(raised);
                if (moved.CompareTo(maxRadicand) > 0)
                    return null;
                Split(moved, rootIndex, out var wholeMoved, out underAbove);
                wholeAbove = wholeAbove.Multiply(wholeMoved);
                wholeBelow = wholeBelow.Multiply(underBelow);
                underBelow = EInteger.One;
            }

            if (wholeAbove.Equals(EInteger.One) && wholeBelow.Equals(EInteger.One)
                && underAbove.Equals(numerator) && underBelow.Equals(denominator))
                return null;

            var whole = Rational.Create(wholeAbove, wholeBelow);
            if (underAbove.Equals(EInteger.One))
                return whole;
            return whole * new Powf(Integer.Create(underAbove), Rational.Create(EInteger.One, EInteger.FromInt32(rootIndex)));
        }

        /// <summary>
        /// Whether <paramref name="value"/> raised to <paramref name="power"/> would exceed the
        /// radicand bound, decided from the bit length so that the power is never taken.
        /// </summary>
        private static bool TooLargeRaised(EInteger value, int power, int maxBits = MaxRadicandBits)
            => value.GetUnsignedBitLengthAsEInteger()
                    .Multiply(power)
                    .CompareTo(EInteger.FromInt32(maxBits)) > 0;

        /// <summary>
        /// Splits <paramref name="value"/> into the part that comes out from under a root of
        /// order <paramref name="rootIndex"/> and the part that stays under it.
        /// </summary>
        private static void Split(EInteger value, int rootIndex, out EInteger whole, out EInteger under)
        {
            whole = EInteger.One;
            under = EInteger.One;
            var remaining = value;
            for (var divisor = EInteger.FromInt32(2);
                 divisor.CompareTo(maxDivisor) <= 0 && divisor.Multiply(divisor).CompareTo(remaining) <= 0;
                 divisor = divisor.Add(EInteger.One))
            {
                var multiplicity = 0;
                while (remaining.Remainder(divisor).IsZero)
                {
                    remaining = remaining.Divide(divisor);
                    multiplicity++;
                }
                if (multiplicity is 0)
                    continue;
                whole = whole.Multiply(divisor.Pow(multiplicity / rootIndex));
                under = under.Multiply(divisor.Pow(multiplicity % rootIndex));
            }
            if (remaining.Equals(EInteger.One))
                return;
            // What survives trial division is prime or a product of two primes, so the only thing
            // it can still hide is being an exact power itself.
            var root = remaining.Root(rootIndex);
            if (root.Pow(rootIndex).Equals(remaining))
                whole = whole.Multiply(root);
            else
                under = under.Multiply(remaining);
        }
    }
}
