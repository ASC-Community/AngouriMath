//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using static AngouriMath.Entity;

namespace AngouriMath.Functions
{
    internal static partial class Patterns
    {
        /// <summary>
        /// The terms that survive a quotient of factorials, or <paramref name="whole"/> where none
        /// do — the two offsets are not a fixed distance apart, or they are far enough apart that
        /// writing the product out is not a simplification.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A static method rather than the local function it was, so that the <c>switch</c> below
        /// is the whole of the method and its arms can be read as data. See
        /// <see cref="Core.Transformations.RewriteRuleSet.Rules"/> and
        /// <a href="https://github.com/asc-community/AngouriMath/issues/825">#825</a>.
        /// </para>
        /// <para>Internal so that the data form of this set calls it rather than repeating it.</para>
        /// </remarks>
        internal static Entity CancelFactorials(Entity whole, Entity x, Entity x2, Number num, Number den)
        {
            static Entity Add(Entity a, Number b) =>
                b is Integer(0) ? a : a + b;
            if (x == x2
                && num - den is Integer { EInteger: var diff }
                && !diff.IsZero && diff.Abs() < 20) // We don't want to expand (x+100)!/x!
                if (diff > 0) // e.g. (x+3)!/x! = (x+1)(x+2)(x+3)
                {
                    var expr = Add(x, den + 1);
                    for (var i = 2; i <= diff; i++)
                        expr *= Add(x, den + i);
                    return expr;
                }
                else // e.g. x!/(x+3)! = 1/(x+1)/(x+2)/(x+3)
                {
                    diff = -diff;
                    var expr = 1 / Add(x, num + 1);
                    for (var i = 2; i <= diff; i++)
                        expr /= Add(x, num + i);
                    return expr;
                }
            return whole;
        }

        /// <summary>(x + a)! / (x + b)! -> (x+b+1)*(x+b+2)*...*(x+a)</summary>
        [AddressableRules]
        internal static Entity ExpandFactorialDivisions(Entity expr)
            => expr switch
            {
                Divf(Factorialf(Sumf(var any1, Number const1)), Factorialf(Sumf(var any1a, Number const2)))
                    => CancelFactorials(expr, any1, any1a, const1, const2),
                Divf(Factorialf(Sumf(var any1, Number const1)), Factorialf(Sumf(Number const2, var any1a)))
                    => CancelFactorials(expr, any1, any1a, const1, const2),
                Divf(Factorialf(Sumf(Number const1, var any1)), Factorialf(Sumf(var any1a, Number const2)))
                    => CancelFactorials(expr, any1, any1a, const1, const2),
                Divf(Factorialf(Sumf(Number const1, var any1)), Factorialf(Sumf(Number const2, var any1a)))
                    => CancelFactorials(expr, any1, any1a, const1, const2),
                Divf(Factorialf(var any1), Factorialf(Sumf(var any1a, Number const2)))
                    => CancelFactorials(expr, any1, any1a, 0, const2),
                Divf(Factorialf(var any1), Factorialf(Sumf(Number const2, var any1a)))
                    => CancelFactorials(expr, any1, any1a, 0, const2),
                Divf(Factorialf(Sumf(var any1, Number const1)), Factorialf(var any1a))
                    => CancelFactorials(expr, any1, any1a, const1, 0),
                Divf(Factorialf(Sumf(Number const1, var any1)), Factorialf(var any1a))
                    => CancelFactorials(expr, any1, any1a, const1, 0),
                _ => expr
            };

        // https://en.wikipedia.org/wiki/Reflection_formula
        // (z-1)! (-z)! -> Γ(z) Γ(1 - z) = π/sin(π z), z ∉ ℤ // actually, when z ∈ ℤ, both sides include division by zero, so we can still replace
        // Replace z with -z => z! (-z-1)! = π/sin(-π z)
        // TODO: Modify the complexity criteria to rank non-elementary functions more complex than elementary functions
        //       so that this formula can be used to simplify
        // TODO: Other than the reflection formula,
        // (z-1)! (z-1/2)! -> Γ(z) Γ(z + 1/2) = 2^(1 - 2 z) sqrt(π) Γ(2 z) -> 2^(1 - 2 z) sqrt(π) (2 z - 1)!
        // is also another possible simplification
        /// <summary>(x-1)! x -> x!, x! (x+1) -> (x+1)!, etc. <!--as well as z! (-z-1)! -> -π/sin(π z)--></summary>
        [AddressableRules]
        internal static Entity FactorizeFactorialMultiplications(Entity expr)
            => expr switch
            {
                Mulf(Factorialf(Sumf(var any1, Number const1)), Sumf(var any1a, Number const2)) =>
                    GatherFactorial(expr, any1, any1a, const1, const2),
                Mulf(Factorialf(Sumf(Number const1, var any1)), Sumf(var any1a, Number const2)) =>
                    GatherFactorial(expr, any1, any1a, const1, const2),
                Mulf(Factorialf(Sumf(var any1, Number const1)), Sumf(Number const2, var any1a)) =>
                    GatherFactorial(expr, any1, any1a, const1, const2),
                Mulf(Factorialf(Sumf(Number const1, var any1)), Sumf(Number const2, var any1a)) =>
                    GatherFactorial(expr, any1, any1a, const1, const2),
                Mulf(Factorialf(var any1), Sumf(var any1a, Number const2)) =>
                    GatherFactorial(expr, any1, any1a, 0, const2),
                Mulf(Factorialf(var any1), Sumf(Number const2, var any1a)) =>
                    GatherFactorial(expr, any1, any1a, 0, const2),
                Mulf(Factorialf(Sumf(var any1, Number const1)), var any1a) =>
                    GatherFactorial(expr, any1, any1a, const1, 0),
                Mulf(Factorialf(Sumf(Number const1, var any1)), var any1a) =>
                    GatherFactorial(expr, any1, any1a, const1, 0),

                // The binomial coefficient's three identities, in the direction that collects.
                // https://github.com/asc-community/AngouriMath/issues/1409
                Sumf(Binomialf(var top1, var k), Binomialf(var top2, var j)) when top1 == top2 => PascalsRule(expr, top1, k, j),
                Mulf(var n, Binomialf(var top, var j)) => ChairpersonsRule(expr, n, top, j),
                Mulf(Binomialf(var top, var j), var n) => ChairpersonsRule(expr, n, top, j),
                Binomialf(var top, var d) => SymmetricBinomial(expr, top, d),
                _ => expr
            };

        /// <summary>
        /// <c>binomial(a, k) + binomial(a, k - 1)</c> is <c>binomial(a + 1, k)</c> -- Pascal's rule,
        /// an identity of the falling factorial for every <c>a</c> and whole <c>k</c>, and of the
        /// gamma function elsewhere -- read in either order of the two, with the lower index that
        /// is one less found by simplifying the difference; <paramref name="whole"/> where neither is.
        /// </summary>
        internal static Entity PascalsRule(Entity whole, Entity top, Entity k, Entity j)
        {
            if (PartialFractions.Bare((k - j).Simplify()) == Integer.One)
                return new Binomialf((top + 1).InnerSimplified, k);
            if (PartialFractions.Bare((j - k).Simplify()) == Integer.One)
                return new Binomialf((top + 1).InnerSimplified, j);
            return whole;
        }

        /// <summary>
        /// <c>n * binomial(n - 1, k - 1)</c> is <c>k * binomial(n, k)</c> -- the chairperson identity,
        /// choosing the chair first or the committee first -- where the multiplier is one more
        /// than the upper index; <paramref name="whole"/> otherwise.
        /// </summary>
        internal static Entity ChairpersonsRule(Entity whole, Entity n, Entity top, Entity j)
        {
            if (n.ContainsNode(top) || top.ContainsNode(n) ? PartialFractions.Bare((n - top).Simplify()) != Integer.One : (n - top).InnerSimplified != Integer.One)
                return whole;
            var k = (j + 1).InnerSimplified;
            return k * new Binomialf(n, k);
        }

        /// <summary>
        /// <c>binomial(n, n - k)</c> is <c>binomial(n, k)</c>, taken where the complement is the
        /// smaller expression: the symmetry of the coefficient, an identity of the gamma
        /// function; <paramref name="whole"/> where the lower index is already the smaller.
        /// </summary>
        internal static Entity SymmetricBinomial(Entity whole, Entity top, Entity d)
        {
            if (!d.ContainsNode(top) || top is Number)
                return whole;
            var complement = PartialFractions.Bare((top - d).Simplify());
            return complement.Complexity < d.Complexity ? new Binomialf(top, complement) : whole;
        }

        /// <summary>
        /// The factorial one term further on, where the term multiplying it is the next one — and
        /// <paramref name="whole"/> where it is not. A static method for the same reason
        /// <see cref="CancelFactorials"/> is one.
        /// </summary>
        /// <remarks>Internal so that the data form of this set calls it rather than repeating it.</remarks>
        internal static Entity GatherFactorial(Entity whole, Entity x, Entity x2, Number factConst, Number @const) =>
            x == x2 && factConst + 1 == @const ? new Factorialf(x + @const) : whole;
    }
}
