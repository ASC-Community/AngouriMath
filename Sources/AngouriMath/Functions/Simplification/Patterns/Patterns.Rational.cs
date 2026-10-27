//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using AngouriMath.Extensions;
using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    partial class Patterns
    {
        /// <remarks>Internal so the data form of this set calls it rather than repeating it.</remarks>
        internal static Entity SumOfFractions(Entity expr,
            Entity leftNum, Entity leftDen, Entity rightNum, Entity rightDen)
        {
            // a/d + b/d = (a + b)/d. Cross-multiplying instead builds d*d and leaves the
            // simplifier to cancel it back down, and since this rule runs inside Simplify's
            // own pass -- calling Simplify again on both halves -- each fraction added to
            // the sum squares the denominator before anything cancels. Three fractions over
            // x + y + z never finished, which is https://github.com/asc-community/AngouriMath/issues/403.
            if (leftDen == rightDen)
                return (leftNum + rightNum).InnerSimplified / leftDen;

            var twoInt = ((leftNum + leftDen).Vars, (rightNum + rightDen).Vars).IntersectSequences().Any();
            if (twoInt)
                return (leftNum * rightDen + rightNum * leftDen).Simplify() / (rightDen * leftDen).Simplify();
            else
                return expr;
        }

        // a * b * c / (b * c * d)
        // =>
        // a * (b / b) * (c / c) * 1/d
        private static IEnumerable<Entity> PairwiseGrouping(Entity num, Entity den, TreeAnalyzer.SortLevel level)
        {
            var numFactors = Mulf.LinearChildren(num);
            var denFactors = Mulf.LinearChildren(den);
            var factors = new Dictionary<string, Entity>();
            foreach (var numFactor in numFactors)
            {
                var sorted = numFactor.SortHash(level);
                if (!factors.ContainsKey(sorted))
                    factors[sorted] = 1;
                factors[sorted] = (factors[sorted] * numFactor).InnerSimplified;
            }
            foreach (var denFactor in denFactors)
            {
                var sorted = denFactor.SortHash(level);
                if (!factors.ContainsKey(sorted))
                    factors[sorted] = 1;
                factors[sorted] = (factors[sorted] / denFactor).InnerSimplified;
            }
            return factors.Values;
        }

        [AddressableRules]
        internal static Entity FractionCommonDenominatorRules(Entity expr, TreeAnalyzer.SortLevel level)
            => expr switch
            {
                Sumf(Divf(var leftNum, var leftDen), Divf(var rightNum, var rightDen)) 
                    => SumOfFractions(expr, leftNum, leftDen, rightNum, rightDen),
                Minusf(Divf(var leftNum, var leftDen), Divf(var rightNum, var rightDen))
                    => SumOfFractions(expr, leftNum, leftDen, -rightNum, rightDen),
                Divf(var num, var den) when num.Vars.Any() && den.Vars.Any()
                    => PairwiseGroupedQuotient(expr, num, den, level),
                _ => expr
            };

        /// <summary>
        /// The quotient regrouped pairwise, its factors put through the power rules and
        /// multiplied back. A method of its own so that the data form of this set calls the same
        /// code rather than a copy of it.
        /// </summary>
        internal static Entity PairwiseGroupedQuotient(
            Entity whole, Entity num, Entity den, TreeAnalyzer.SortLevel level)
            => PairwiseGrouping(num, den, level).Select(PowerRules).MultiplyAll()
                .InnerSimplified.Replace(CollapseMultipleFractions);

        /// <summary>n^(p/q) for a q of 2 or more -- a root that is not a whole power.</summary>
        private static bool IsSurd(Entity node)
            => node is Powf(_, Rational and not Integer);

        /// <summary>
        /// <c>p * value / q</c> for a rational <c>p/q</c>, written so that the rational is
        /// split across the quotient rather than left as a factor: a unit-numerator rational
        /// carries its own weight in the complexity criteria, so <c>(1/2) * value</c> would
        /// rate worse than <c>value / 2</c> and hand back the comparison this is trying to win.
        /// </summary>
        private static Entity ScaleBy(ERational ratio, Entity value)
        {
            var scaled = ratio.Numerator.Equals(EInteger.One)
                ? value
                : (Integer.Create(ratio.Numerator) * value).InnerSimplified;
            return ratio.Denominator.Equals(EInteger.One)
                ? scaled
                : scaled / Integer.Create(ratio.Denominator);
        }

        /// <summary>
        /// The two rules of this set, in order. Split into one method each so that the data form
        /// in <c>MatchedRules</c> calls the same code rather than a copy of it — this set is an
        /// ordinary method with branches and locals, which the rule registry generator declines,
        /// so its arms had no other way of becoming addressable.
        /// </summary>
        internal static Entity RationalizeDenominator(Entity expr)
            => GatherNumericCoefficientOverASurd(expr) is var gathered && !ReferenceEquals(gathered, expr)
                ? gathered
                : MultiplyByTheConjugate(expr);

        /// <summary>
        /// <c>k * (value / d) -> (k * value) / d</c>, reduced, where the numerator carries a surd
        /// this rule moved up out of a denominator.
        /// </summary>
        /// <remarks>
        /// Without it a numeric coefficient never meets the divisor: <c>k / (p + sqrt(q))</c> is
        /// split into <c>k * (1 / (p + sqrt(q)))</c> before this rule runs, so the quotient it
        /// rewrites has a numerator of 1 and the <c>k</c> stays outside. <c>2 / (3 - sqrt(5))</c>
        /// came out as <c>2 * (3 + sqrt(5)) / 4</c>, which is longer than what it replaced —
        /// while <c>1 / (3 - sqrt(5))</c>, with no coefficient to strand, answered correctly all
        /// along.
        /// </remarks>
        internal static Entity GatherNumericCoefficientOverASurd(Entity expr)
        {
            if (expr is Mulf(Rational coefficient, Divf(var inner, Rational { IsZero: false } innerDivisor))
                && inner.Nodes.Any(IsSurd))
                return ScaleBy(coefficient.ERational.Divide(innerDivisor.ERational), inner);
            return expr;
        }

        /// <summary>
        /// <c>num / (a + b)</c> becomes <c>num * (a - b) / (a^2 - b^2)</c> where that clears a
        /// surd out of the denominator, and <paramref name="expr"/> where it does not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>(5 - sqrt(3)) / (5 + sqrt(3))</c> is <c>14/11 - 5/11 * sqrt(3)</c>.
        /// </para>
        /// <para>
        /// The library already prefers a denominator without a surd -- <c>1/sqrt(2)</c> has
        /// always come back as <c>sqrt(2)/2</c> -- so this extends an existing preference to
        /// the binomial case. https://github.com/asc-community/AngouriMath/issues/205
        /// </para>
        /// <para>
        /// Three conditions, and each is load-bearing. The denominator must be constant,
        /// because multiplying numerator and denominator by <c>a - b</c> is only valid where
        /// that is non-zero, and for a symbolic denominator it is not decidable -- rewriting
        /// there would either lose a value or attach a condition to every such quotient.
        /// <c>a^2 - b^2</c> must fold to a rational, which is what says the root was
        /// actually cleared: a conjugate does nothing for a cube root, where
        /// <c>1 - 2^(2/3)</c> is no better than <c>1 + 2^(1/3)</c>. And it must be non-zero,
        /// which rules out <c>a = b</c> and, since it is <c>(a-b)(a+b)</c>, guarantees the
        /// original denominator was non-zero too.
        /// </para>
        /// <para>
        /// Terminates because the rewritten denominator is a rational and this rule requires
        /// a surd in the denominator to fire at all.
        /// </para>
        /// </remarks>
        internal static Entity MultiplyByTheConjugate(Entity expr)
        {
            if (expr is not Divf(var num, var den))
                return expr;
            var (a, b) = den switch
            {
                Sumf(var left, var right) => (left, right),
                Minusf(var left, var right) => (left, -right),
                _ => (null, null)
            };
            if (a is null || b is null)
                return expr;
            if (den.Vars.Any() || !den.Nodes.Any(IsSurd))
                return expr;

            var product = (a * a - b * b).InnerSimplified;
            if (product is not Rational { IsZero: false } rational)
                return expr;
            // Orient the conjugate so the rational denominator comes out positive. Taking
            // it the other way round leaves a double negative -- 1/(5 + sqrt(3)) became
            // (sqrt(3) - 5) / (-22) -- which nothing downstream folds back, so the candidate
            // stayed longer than the form it replaced and lost on the metric it was supposed
            // to win. Negating both halves is the same number.
            var negative = rational.IsNegative;
            var conjugate = negative ? b - a : a - b;
            var divisor = negative ? (Rational)(-rational).InnerSimplified : rational;

            // A rational numerator is cancelled against the divisor rather than left to meet
            // it later, which it does not: InnerSimplify leaves `2 * (3 + sqrt(5)) * 1/4`.
            if (num is Rational numerator)
                return ScaleBy(numerator.ERational.Divide(divisor.ERational), conjugate);
            return ((num * conjugate) / divisor).InnerSimplified;
        }

        [AddressableRules]
        internal static Entity CollapseMultipleFractions(Entity expr)
            => expr switch
            {
                Powf(Divf(var a, var b), Integer { IsPositive: true } c) => a.Pow(c) / b.Pow(c),
                Powf(Mulf(var a, var b), Integer { IsPositive: true } c) => a.Pow(c) * b.Pow(c),

                Mulf(Divf(var a, var b), Divf(var c, var d)) => (a * c) / (b * d),
                Mulf(var a, Divf(var b, var c)) => (a * b) / c,
                Mulf(Divf(var a, var b), var c) => (a * c) / b,

                Divf(Divf(var a, var b), Divf(var c, var d)) => (a * d) / (b * c),
                Divf(Divf(var a, var b), var c) => a / (b * c),
                Divf(var a, Divf(var b, var c)) => (a * c) / b,

                _ => expr
            };
    }
}
