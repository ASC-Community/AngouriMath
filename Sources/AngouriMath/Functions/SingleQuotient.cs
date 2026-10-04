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
    /// Writes an expression as one quotient: a numerator and a denominator with no division
    /// anywhere inside either.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is writing an expression as a single fraction, and it is deliberately
    /// <b>not</b> part of <see cref="Entity.Simplify(int)"/>. Putting a sum over a common
    /// denominator makes some expressions worse — <c>1/x + 1/y</c> is easier to read than
    /// <c>(x + y)/(x*y)</c> — which is why every system that has it keeps it as an operation you
    /// ask for. <c>Simplify</c> here combines nothing: <c>a + b/c</c> comes back as <c>a + b/c</c>.
    /// https://github.com/asc-community/AngouriMath/issues/1239
    /// </para>
    /// <para>
    /// <b>What it is for.</b> Everything that takes a rational function apart —
    /// <see cref="PartialFractions"/> and the integrator's rational path — wants its input as a
    /// single <see cref="Divf"/> of two polynomials, and declines anything else. So a rewrite that
    /// produces a correct but nested answer is thrown away one step short of being usable. The
    /// half-angle substitution is the case that exposed it: <c>1/(1 - sin(x))</c> rewrites to
    /// <c>2/((t^2 + 1)(1 + (-2)t/(t^2 + 1)))</c>, which is <c>2/(t^2 - 2t + 1)</c> after one
    /// distribution and is integrated at once, and was declined for want of it.
    /// </para>
    /// <para>
    /// <b>The public entry</b> is <see cref="Entity.AsSingleFraction"/>, through
    /// <see cref="AngouriMath.Core.Transformations.Transformation.AsSingleFraction"/>, which
    /// gathers through <see cref="OverLeastCommonDenominator"/>, reads a rational number as the
    /// fraction it is written as, and tidies each half.
    /// </para>
    /// <para>
    /// <b>No cancellation.</b> The two halves are returned as built, with no common factor taken
    /// out: <c>x/x</c> comes back as <c>(x, x)</c> and not as <c>(1, 1)</c>. Cancelling needs a
    /// gcd, which needs to know what the expression is a polynomial <em>in</em>, and this runs
    /// before anything has decided that. The callers here divide out afterwards anyway, and a
    /// caller that wants the tidy form asks the simplifier for it.
    /// </para>
    /// <para>
    /// <b>It always terminates and never grows without bound</b>, because it recurses only into
    /// the operands of the node it is given and each recursion is on a strictly smaller tree. What
    /// it can do is make the tree bigger — combining a sum of <c>n</c> quotients multiplies the
    /// denominators — so <see cref="Of(Entity)"/> is a transformation to ask for rather than one to apply
    /// on the way past.
    /// </para>
    /// </remarks>
    internal static class SingleQuotient
    {
        /// <summary>
        /// The expression as <c>Numerator / Denominator</c>. The denominator is <c>1</c> for an
        /// expression that had no division in it, which is the signal that nothing was combined.
        /// </summary>
        internal static (Entity Numerator, Entity Denominator) Of(Entity expr)
            => Of(expr, carried: null, leastCommon: false);

        /// <summary>
        /// The expression as one fraction, written the way it is by hand: a sum goes over the
        /// least common multiple of its terms' denominators as they stand, each factor to the
        /// highest power any of them has it and the whole numbers by their least common multiple,
        /// so <c>1/x + 1/x^2</c> is <c>(x + 1, x^2)</c> and not <c>(x^2 + x, x^3)</c>. Nothing is
        /// factorised to find it: <c>x^2 - 1</c> and <c>x + 1</c> share no factor here. Each
        /// denominator that turning a quotient over moves into the numerator is added to
        /// <paramref name="carried"/>, since the expression is undefined where one of those is
        /// zero and the fraction need not be: <c>1/(1/x)</c> is <c>(x, 1)</c>, which has a value
        /// at zero.
        /// </summary>
        /// <remarks>
        /// <see cref="Of(Entity)"/> keeps the product of the denominators, for the callers above,
        /// which divide out afterwards.
        /// </remarks>
        internal static (Entity Numerator, Entity Denominator) OverLeastCommonDenominator(Entity expr, List<Entity> carried)
            => Of(expr, carried, leastCommon: true);

        private static (Entity Numerator, Entity Denominator) Of(Entity expr, List<Entity>? carried, bool leastCommon)
        {
            switch (expr)
            {
                case Divf(var dividend, var divisor):
                {
                    var (an, ad) = Of(dividend, carried, leastCommon);
                    var (bn, bd) = Of(divisor, carried, leastCommon);
                    // (an/ad) / (bn/bd) = (an * bd) / (ad * bn)
                    Carry(bd, carried);
                    return (Times(an, bd), Times(ad, bn));
                }

                case Mulf(var left, var right):
                {
                    var (an, ad) = Of(left, carried, leastCommon);
                    var (bn, bd) = Of(right, carried, leastCommon);
                    return (Times(an, bn), Times(ad, bd));
                }

                case Sumf(var augend, var addend):
                {
                    var (an, ad) = Of(augend, carried, leastCommon);
                    var (bn, bd) = Of(addend, carried, leastCommon);
                    var (common, forA, forB) = Over(ad, bd, leastCommon);
                    return (Plus(Times(an, forA), Times(bn, forB)), common);
                }

                case Minusf(var minuend, var subtrahend):
                {
                    var (an, ad) = Of(minuend, carried, leastCommon);
                    var (bn, bd) = Of(subtrahend, carried, leastCommon);
                    var (common, forA, forB) = Over(ad, bd, leastCommon);
                    return (Minus(Times(an, forA), Times(bn, forB)), common);
                }

                // A whole power distributes over the quotient, and a negative one turns it over.
                // Anything else — a symbolic or fractional exponent — is left whole, since
                // (a/b)^(1/2) is not sqrt(a)/sqrt(b) on the branch cut.
                case Powf(var @base, Integer power):
                {
                    var (bn, bd) = Of(@base, carried, leastCommon);
                    if (bd == Integer.One && power.EInteger.Sign >= 0)
                        return (expr, Integer.One);
                    if (power.EInteger.Sign < 0)
                        Carry(bd, carried);
                    // The first power is the base itself: `A^(-1)` is `1/A`, not `1^1/A^1`,
                    // which nothing reading the factors of the denominator took for `A`.
                    if (power.EInteger.Equals(EInteger.FromInt32(-1)))
                        return (bd, bn);
                    return power.EInteger.Sign >= 0
                        ? (MathS.Pow(bn, power), MathS.Pow(bd, power))
                        : (MathS.Pow(bd, -power), MathS.Pow(bn, -power));
                }

                default:
                    return (expr, Integer.One);
            }
        }

        private static void Carry(Entity denominator, List<Entity>? carried)
        {
            if (carried is not null && denominator != Integer.One)
                carried.Add(denominator);
        }

        /// <summary>
        /// A common denominator of <paramref name="a"/> and <paramref name="b"/>, with what each
        /// is multiplied by to reach it: their product, or with <paramref name="leastCommon"/>
        /// their least common multiple as written.
        /// </summary>
        private static (Entity Common, Entity ForA, Entity ForB) Over(Entity a, Entity b, bool leastCommon)
        {
            if (!leastCommon)
                return (Times(a, b), b, a);
            if (a == Integer.One)
                return (b, b, Integer.One);
            if (b == Integer.One || a == b)
                return (a, Integer.One, b == Integer.One ? a : Integer.One);
            var (aWhole, aFactors) = Factors(a);
            var (bWhole, bFactors) = Factors(b);
            if (aWhole.IsZero || bWhole.IsZero)
                return (Times(a, b), b, a);
            var whole = aWhole.Abs().Divide(aWhole.Abs().Gcd(bWhole.Abs())).Multiply(bWhole.Abs());
            var common = new List<(Entity Base, EInteger Power)>(aFactors);
            foreach (var (@base, power) in bFactors)
            {
                var at = common.FindIndex(factor => factor.Base == @base);
                if (at < 0)
                    common.Add((@base, power));
                else if (power.CompareTo(common[at].Power) > 0)
                    common[at] = (@base, power);
            }
            return (Product(whole, common, null),
                Product(whole.Divide(aWhole), common, aFactors),
                Product(whole.Divide(bWhole), common, bFactors));
        }

        /// <summary>
        /// A denominator as a whole number times its other factors, each with the whole power it
        /// is raised to: a factor written twice is one factor with the powers added, and a power
        /// of a whole number is part of the whole number.
        /// </summary>
        private static (EInteger Whole, List<(Entity Base, EInteger Power)> Factors) Factors(Entity denominator)
        {
            var whole = EInteger.One;
            var factors = new List<(Entity Base, EInteger Power)>();
            foreach (var factor in Mulf.LinearChildren(denominator))
            {
                switch (factor)
                {
                    case Integer integer:
                        whole = whole.Multiply(integer.EInteger);
                        continue;
                    case Powf(Integer number, Integer power) when power.EInteger.Sign > 0
                        && power.EInteger.CompareTo(EInteger.FromInt32(MaxFoldedPower)) <= 0:
                        whole = whole.Multiply(number.EInteger.Pow(power.EInteger));
                        continue;
                }
                var (@base, exponent) = factor is Powf(var raised, Integer raisedTo) && raisedTo.EInteger.Sign > 0
                    ? (raised, raisedTo.EInteger)
                    : (factor, EInteger.One);
                var at = factors.FindIndex(known => known.Base == @base);
                if (at < 0)
                    factors.Add((@base, exponent));
                else
                    factors[at] = (@base, factors[at].Power.Add(exponent));
            }
            return (whole, factors);
        }

        /// <summary>
        /// A power of a whole number is multiplied out into the whole number only up to this
        /// exponent; past it the power stays a factor like any other, which is still correct.
        /// </summary>
        private const int MaxFoldedPower = 64;

        /// <summary>
        /// <paramref name="whole"/> times each factor of <paramref name="common"/> to the power
        /// that <paramref name="own"/> is short of it, or to its whole power where
        /// <paramref name="own"/> is <see langword="null"/>.
        /// </summary>
        private static Entity Product(EInteger whole, List<(Entity Base, EInteger Power)> common,
            List<(Entity Base, EInteger Power)>? own)
        {
            Entity product = Integer.Create(whole);
            foreach (var (@base, power) in common)
            {
                var ownPower = own?.Find(factor => factor.Base == @base).Power ?? EInteger.Zero;
                var missing = power.Subtract(ownPower);
                if (missing.Sign > 0)
                    product = Times(product, missing.Equals(EInteger.One) ? @base : MathS.Pow(@base, Integer.Create(missing)));
            }
            return product;
        }

        /// <summary>
        /// <paramref name="expr"/> rewritten as a single quotient, or unchanged where it had no
        /// division in it to combine.
        /// </summary>
        internal static Entity Combine(Entity expr)
        {
            var (numerator, denominator) = Of(expr);
            return denominator == Integer.One ? numerator : numerator / denominator;
        }

        // Multiplying and adding through `1` and `0` rather than building the nodes keeps the
        // result readable, and keeps the common case -- an expression with one division in it --
        // from coming back wrapped in a chain of `* 1`.
        private static Entity Times(Entity a, Entity b) =>
            a == Integer.One ? b : b == Integer.One ? a : a * b;

        private static Entity Plus(Entity a, Entity b) =>
            a == Integer.Create(0) ? b : b == Integer.Create(0) ? a : a + b;

        private static Entity Minus(Entity a, Entity b) =>
            b == Integer.Create(0) ? a : a - b;
    }
}
