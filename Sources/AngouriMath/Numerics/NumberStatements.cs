//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Functions;
using PeterO.Numbers;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Numerics
{
    /// <summary>
    /// Membership in the sets of numbers and divisibility, for the compilers, on the terms evaluation decides
    /// them: no truth value at <c>NaN</c>, an infinity in <c>RR</c> and <c>CC</c> as evaluation has it and in
    /// none of the sets of whole numbers, and primality left open past the machine word, where the interpreter
    /// leaves it. Divisibility is a statement about whole numbers, and has no truth value at any other.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class NumberStatements
    {
        /// <summary>The sets of numbers membership in which compiles, as FastExpression's instruction names them.</summary>
        internal enum NumberSet { Complexes, Reals, Integers, PositiveIntegers, NonNegativeIntegers, Primes }

        /// <summary>
        /// The set of numbers <paramref name="set"/> is, or <see langword="null"/> for one membership in which does
        /// not compile: <c>QQ</c>, of which every finite double is a member while <c>pi in QQ</c> is
        /// <c>False</c>, and <c>BB</c>, whose members are not numbers.
        /// </summary>
        internal static NumberSet? Of(Entity.Set.SpecialSet set) => set switch
        {
            Entity.Set.SpecialSet.Complexes => NumberSet.Complexes,
            Entity.Set.SpecialSet.Reals => NumberSet.Reals,
            Entity.Set.SpecialSet.Integers => NumberSet.Integers,
            Entity.Set.SpecialSet.PositiveIntegers => NumberSet.PositiveIntegers,
            Entity.Set.SpecialSet.NonNegativeIntegers => NumberSet.NonNegativeIntegers,
            Entity.Set.SpecialSet.Primes => NumberSet.Primes,
            _ => null
        };

        // 2^63: a whole double below it is a long, and the trial division the interpreter decides primality by
        // takes it.
        private const double PastTheWord = 9223372036854775808.0;

        /// <summary>Whether <paramref name="z"/> is in the set of numbers <paramref name="set"/>, a <see cref="NumberSet"/>.</summary>
        public static bool? In(NumericsComplex z, int set)
        {
            if (double.IsNaN(z.Real) || double.IsNaN(z.Imaginary))
                return null;
            // +oo and -oo are reals as evaluation holds them, which #1019 item 15 takes up for v3:
            // https://github.com/asc-community/AngouriMath/issues/2220
            switch ((NumberSet)set)
            {
                case NumberSet.Complexes:
                    return true;
                case NumberSet.Reals:
                    return z.Imaginary == 0;
            }
            if (z.Imaginary != 0 || double.IsInfinity(z.Real) || Math.Floor(z.Real) != z.Real)
                return false;
            return (NumberSet)set switch
            {
                NumberSet.Integers => true,
                NumberSet.PositiveIntegers => z.Real > 0,
                NumberSet.NonNegativeIntegers => z.Real >= 0,
                _ => z.Real < 2 ? false : z.Real >= PastTheWord ? null : Primes.IsPrime(EInteger.FromInt64((long)z.Real))
            };
        }

        public static bool? In(double x, int set) => In(new NumericsComplex(x, 0), set);

        /// <summary>
        /// Whether <paramref name="divisor"/> divides <paramref name="dividend"/>: for whole numbers, whether the
        /// remainder is 0, and 0 dividing only 0; for any other, no truth value.
        /// </summary>
        public static bool? Divides(NumericsComplex divisor, NumericsComplex dividend)
            => IsWhole(divisor) && IsWhole(dividend)
                ? divisor.Real == 0 ? dividend.Real == 0 : dividend.Real % divisor.Real == 0
                : null;

        public static bool? Divides(double divisor, double dividend) => Divides(new NumericsComplex(divisor, 0), new NumericsComplex(dividend, 0));

        // A double's remainder by another is exact, so a whole one is divided exactly however large.
        private static bool IsWhole(NumericsComplex z)
            => z.Imaginary == 0 && !double.IsNaN(z.Real) && !double.IsInfinity(z.Real) && Math.Floor(z.Real) == z.Real;
    }
}
