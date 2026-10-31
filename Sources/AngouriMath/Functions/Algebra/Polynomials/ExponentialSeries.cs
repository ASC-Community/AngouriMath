//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions
{
    /// <summary>
    /// A summation to <c>+oo</c> whose summand is a polynomial in the index times a power with
    /// the index in the exponent, over a factorial of the index: <c>sum(x^k / k!, k, 0, +oo)</c>
    /// is <c>e^x</c>, and <c>sum(3^(k+2) * (k^2 + k + 1) / (k + 3)!, k, 0, +oo)</c> is
    /// <c>(8 * e^3 - 41) / 6</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The whole family is <c>e^c</c>. Shift the index so the factorial is <c>j!</c>, and the
    /// summand is <c>q(j) * c^j / j!</c> for a polynomial <c>q</c>; then
    /// <c>sum(j^n * c^j / j!, j, 0, +oo) = e^c * T_n(c)</c>, where <c>T_n</c> is the Touchard
    /// polynomial <c>sum(S(n, m) * c^m, m, 0, n)</c> over the Stirling numbers of the second
    /// kind -- <c>j^n</c> written in falling factorials, each of which sums to <c>c^m * e^c</c>.
    /// A lower bound above the shifted zero subtracts the terms below it, which are finitely
    /// many and exact. The series converges for every <c>c</c>, so no condition is owed.
    /// </para>
    /// <para>
    /// Recognised structurally, on the factors of the simplified summand: exactly one
    /// <c>(k + a)! ^ (-1)</c> with a whole <c>a</c>, at most one <c>c ^ (k + s)</c> with
    /// <c>c</c> free of the index, and a polynomial in the index for the rest; anything else
    /// is left as written. A power whose exponent is not the index plus a constant --
    /// <c>c^(2k)</c> -- is declined rather than rewritten, and so is a base that is zero.
    /// The first of the orientation-week questions of
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1212">#1212</a>.
    /// </para>
    /// </remarks>
    internal static class ExponentialSeries
    {
        /// <summary>
        /// The degree of polynomial this will take; the Stirling table is quadratic in it and
        /// the answer carries <c>c</c> to that power.
        /// </summary>
        private const int MaxDegree = 16;

        /// <summary>
        /// <c>sum(expression, index, from, +oo)</c> in closed form, or <see langword="null"/>
        /// where the summand is not of the shape above, the upper bound is not <c>+oo</c>, or
        /// the lower bound is not a whole number.
        /// </summary>
        internal static Entity? ClosedForm(Entity expression, Entity var, Entity from, Entity to)
        {
            if (var is not Variable index)
                return null;
            if (to.Evaled is not Real { IsFinite: false, IsNaN: false } upper || upper.IsNegative)
                return null;
            if (from.Evaled is not Integer lower || !lower.EInteger.CanFitInInt32())
                return null;
            var start = lower.EInteger.ToInt32Checked();

            Entity? factorialArgument = null;
            Entity? @base = null;
            Entity? exponent = null;
            Entity polynomial = Integer.One;
            Entity constant = Integer.One;
            var summand = WithoutFactorialConditions(expression.InnerSimplified, index, start);
            foreach (var factor in Mulf.LinearChildren(summand))
            {
                if (!factor.ContainsNode(index))
                {
                    constant *= factor;
                    continue;
                }
                switch (factor)
                {
                    case Powf(Factorialf(var argument), var power) when factorialArgument is null && power == Integer.MinusOne:
                        factorialArgument = argument;
                        break;
                    case Powf(var b, var e) when @base is null && !b.ContainsNode(index):
                        @base = b;
                        exponent = e;
                        break;
                    default:
                        polynomial *= factor;
                        break;
                }
            }
            if (factorialArgument is null)
                return null;

            // (k + a)!: the shift that makes it j!, which has to be a whole number. Read off
            // the tree: `k - k` does not simplify to a bare 0 but to 0 with what it assumes.
            if (!TryReadShift(factorialArgument, index, out var a))
                return null;
            // The shifted range starts at start + a, and a factorial of a negative number is
            // not a term of anything.
            if (start + a < 0)
                return null;

            // c ^ (k + s) is c ^ s * c ^ k; the offset may be symbolic, the slope must be one.
            Entity c = Integer.One;
            if (@base is not null)
            {
                if (!TryReadOffset(exponent!, index, out var offset))
                    return null;
                if (@base.Evaled is Complex value && IsZero(value))
                    return null;
                // Not c^0: that would attach `provided not c = 0` for a series whose value at
                // c = 0 is the constant term, which the formula gives without help.
                if (offset != Integer.Zero)
                    constant *= MathS.Pow(@base, offset);
                c = @base;
            }

            // q(j) = p(j - a), as coefficients by power of j; each must be free of the index.
            var shifted = polynomial.Substitute(index, index - Integer.Create(a));
            // A summand with no polynomial part -- 1 / k!, x^k / k! -- is the constant
            // polynomial, which the polynomial reader has nothing to read.
            Dictionary<EInteger, Entity>? monomials;
            if (!shifted.ContainsNode(index))
                monomials = new Dictionary<EInteger, Entity> { [EInteger.Zero] = shifted };
            else if (!TreeAnalyzer.TryGetPolynomial(shifted, index, out monomials))
                return null;
            var degree = 0;
            foreach (var monomial in monomials)
            {
                if (monomial.Key.Sign < 0 || !monomial.Key.CanFitInInt32() || monomial.Value.ContainsNode(index))
                    return null;
                var power = monomial.Key.ToInt32Checked();
                if (power > MaxDegree)
                    return null;
                if (power > degree)
                    degree = power;
            }

            // sum over j >= 0 of q(j) c^j / j! = e^c * sum over n of a_n * T_n(c).
            var stirling = StirlingSecondKind(degree);
            Entity touchardSum = Integer.Zero;
            foreach (var monomial in monomials)
            {
                var n = monomial.Key.ToInt32Checked();
                Entity touchard = Integer.Zero;
                for (var m = 0; m <= n; m++)
                    if (!stirling[n][m].IsZero)
                        // The constant term as a number and not as c^0, for the reason above.
                        touchard += m == 0 ? Integer.Create(stirling[n][m]) : Integer.Create(stirling[n][m]) * MathS.Pow(c, m);
                touchardSum += monomial.Value * touchard;
            }
            Entity whole = MathS.Pow(MathS.e, c) * touchardSum;

            // The terms below the shifted lower bound, finitely many and exact.
            Entity below = Integer.Zero;
            for (var j = 0; j < start + a; j++)
            {
                var term = shifted.Substitute(index, Integer.Create(j)) / MathS.Factorial(Integer.Create(j));
                // c^0 written as nothing, for the reason above.
                below += j == 0 ? term : term * MathS.Pow(c, j);
            }

            // Back from j to k: the summand was p(k) c^k / (k + a)!, and c^k is c^j / c^a.
            var result = constant * (whole - below);
            if (a != 0)
                result *= MathS.Pow(c, -a);
            return result.InnerSimplified;
        }

        /// <summary>
        /// The summand without a condition that says a factorial of the index is not zero: a
        /// division by <c>(k + a)!</c> carries <c>provided not (k + a)! = 0</c> from the moment
        /// it is simplified, and over a range that starts at or above <c>-a</c> the factorial is
        /// that of a whole non-negative number, which is never zero. Any other condition is left
        /// where it is and the summand then declines below.
        /// </summary>
        private static Entity WithoutFactorialConditions(Entity summand, Variable index, int start)
        {
            while (summand is Providedf(var body, var condition) && HoldsOverTheRange(condition, index, start))
                summand = body;
            return summand;
        }

        /// <summary>
        /// Whether a condition holds for every whole <c>index >= start</c>: a factorial of
        /// <c>index + a</c> not being zero, <c>index + a</c> being real, or being at least zero,
        /// each where <c>start + a >= 0</c> -- the clauses a division by a factorial carries.
        /// </summary>
        private static bool HoldsOverTheRange(Entity condition, Variable index, int start)
            => condition switch
            {
                Notf(Equalsf(Factorialf(var argument), var zero))
                    when zero.Evaled is Integer { IsZero: true } && AtLeastZero(argument, index, start)
                    => true,
                Inf(var argument, var set)
                    when set == MathS.Sets.R && AtLeastZero(argument, index, start)
                    => true,
                GreaterOrEqualf(var argument, var zero)
                    when zero.Evaled is Integer { IsZero: true } && AtLeastZero(argument, index, start)
                    => true,
                Andf(var left, var right)
                    => HoldsOverTheRange(left, index, start) && HoldsOverTheRange(right, index, start),
                Orf(var left, var right)
                    => HoldsOverTheRange(left, index, start) || HoldsOverTheRange(right, index, start),
                // A factorial's own condition, index + a in ZZ implies index + a >= 0: true wherever
                // its conclusion is. https://github.com/asc-community/AngouriMath/issues/2256
                Impliesf(_, var conclusion)
                    => HoldsOverTheRange(conclusion, index, start),
                _ => false,
            };

        private static bool AtLeastZero(Entity argument, Variable index, int start)
            => TryReadShift(argument, index, out var a) && start + a >= 0;

        /// <summary>
        /// <paramref name="argument"/> as <c>index + shift</c> for a whole <c>shift</c>: the
        /// index itself, or the index plus or minus a whole number, in either order.
        /// </summary>
        private static bool TryReadShift(Entity argument, Variable index, out int shift)
        {
            shift = 0;
            if (argument == index)
                return true;
            static bool Whole(Entity e, out int value)
            {
                value = 0;
                if (e.Evaled is not Integer whole || !whole.EInteger.CanFitInInt32())
                    return false;
                value = whole.EInteger.ToInt32Checked();
                return true;
            }
            switch (argument)
            {
                case Sumf(var left, var right) when left == index && Whole(right, out shift):
                case Sumf(var left2, var right2) when right2 == index && Whole(left2, out shift):
                    return true;
                case Minusf(var left, var right) when left == index && Whole(right, out var subtracted):
                    shift = -subtracted;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// <paramref name="exponent"/> as <c>index + offset</c> for an <paramref name="offset"/>
        /// free of the index, which may be symbolic: the index itself, or the index plus or
        /// minus something without it. Read off the tree for the same reason as the shift.
        /// </summary>
        private static bool TryReadOffset(Entity exponent, Variable index, out Entity offset)
        {
            offset = Integer.Zero;
            if (exponent == index)
                return true;
            switch (exponent)
            {
                case Sumf(var left, var right) when left == index && !right.ContainsNode(index):
                    offset = right;
                    return true;
                case Sumf(var left, var right) when right == index && !left.ContainsNode(index):
                    offset = left;
                    return true;
                case Minusf(var left, var right) when left == index && !right.ContainsNode(index):
                    offset = -right;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// <c>S(n, m)</c> for <c>n, m</c> up to <paramref name="degree"/>: the number of ways to
        /// partition <c>n</c> things into <c>m</c> non-empty parts, by the recurrence
        /// <c>S(n, m) = m * S(n - 1, m) + S(n - 1, m - 1)</c>.
        /// </summary>
        private static EInteger[][] StirlingSecondKind(int degree)
        {
            var table = new EInteger[degree + 1][];
            for (var n = 0; n <= degree; n++)
            {
                table[n] = new EInteger[n + 1];
                for (var m = 0; m <= n; m++)
                    table[n][m] = EInteger.Zero;
            }
            table[0][0] = EInteger.One;
            for (var n = 1; n <= degree; n++)
                for (var m = 1; m <= n; m++)
                    // S(n - 1, n) is zero and outside the row.
                    table[n][m] = (m < n ? EInteger.FromInt32(m).Multiply(table[n - 1][m]) : EInteger.Zero).Add(table[n - 1][m - 1]);
            return table;
        }
    }
}
