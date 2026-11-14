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
    /// The functions of a factorisation, the p-adic valuation, the n-th prime and the prime-counting
    /// function, for the compilers, by the routines the interpreter uses: exact at a whole argument, and
    /// <c>NaN</c> at any other, where none of them has a value, and where the interpreter leaves one as
    /// written, a number too large to factorise or a prime too far to build towards. The gcd and the lcm
    /// likewise, though evaluation has them at a rational too.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class NumberTheoreticFunctions
    {
        [AngouriMath.Core.ConstantField] private static readonly NumericsComplex NaN = new(double.NaN, 0);

        /// <summary>Whether <paramref name="z"/> is a whole number a <see cref="long"/> holds exactly.</summary>
        private static bool IsWhole(NumericsComplex z, out long whole)
        {
            whole = 0;
            if (z.Imaginary != 0 || double.IsNaN(z.Real) || double.IsInfinity(z.Real)
                || Math.Floor(z.Real) != z.Real || Math.Abs(z.Real) > 9e15)
                return false;
            whole = (long)z.Real;
            return true;
        }

        // The factorisation of a positive whole number, or null where there is none or it is not found.
        private static System.Collections.Generic.List<(EInteger Prime, int Power)>? Factors(NumericsComplex n)
            => IsWhole(n, out var whole) && whole > 0 ? Primes.Factorization(EInteger.FromInt64(whole)) : null;

        /// <summary>The Möbius function: 0 where a square divides <c>n</c>, and <c>(-1)^k</c> for <c>k</c> distinct primes.</summary>
        public static NumericsComplex Mobius(NumericsComplex n)
            => Factors(n) is { } factors ? (factors.Exists(factor => factor.Power > 1) ? 0 : factors.Count % 2 == 0 ? 1 : -1) : NaN;

        /// <summary>The number of distinct primes dividing <c>n</c>.</summary>
        public static NumericsComplex PrimeNu(NumericsComplex n) => Factors(n) is { } factors ? factors.Count : NaN;

        /// <summary>The number of primes dividing <c>n</c>, with multiplicity.</summary>
        public static NumericsComplex PrimeOmega(NumericsComplex n)
        {
            if (Factors(n) is not { } factors)
                return NaN;
            var count = 0;
            foreach (var factor in factors)
                count += factor.Power;
            return count;
        }

        /// <summary>The sum of the <c>k</c>-th powers of the divisors of <c>n</c>, by its product over the prime powers.</summary>
        public static NumericsComplex DivisorSigma(NumericsComplex k, NumericsComplex n)
        {
            if (Factors(n) is not { } factors || double.IsNaN(k.Real) || double.IsNaN(k.Imaginary))
                return NaN;
            NumericsComplex product = 1;
            foreach (var (prime, power) in factors)
            {
                NumericsComplex sum = 0;
                var p = (double)prime.ToInt64Checked();
                for (var j = 0; j <= power; j++)
                    sum += NumericsComplex.Pow(Math.Pow(p, j), k);
                product *= sum;
            }
            return product;
        }

        /// <summary>The p-adic valuation of a whole <c>n</c> at a prime <c>p</c>: <c>+oo</c> at 0, and no value for a <c>p</c> not prime.</summary>
        public static NumericsComplex Valuation(NumericsComplex n, NumericsComplex p)
        {
            if (!IsWhole(n, out var whole) || !IsWhole(p, out var prime) || Primes.IsPrime(EInteger.FromInt64(prime)) is not true)
                return NaN;
            if (whole == 0)
                return double.PositiveInfinity;
            var count = 0;
            for (var rest = Math.Abs(whole); rest % prime == 0; rest /= prime)
                count++;
            return count;
        }

        /// <summary>The n-th prime, for a whole <c>n</c> from 1 to a million, as far as the table is built.</summary>
        public static NumericsComplex Prime(NumericsComplex n)
            => IsWhole(n, out var index) && index >= 1 && index <= 1000000 ? Primes.GetPrime((int)index - 1).EInteger.ToInt64Checked() : NaN;

        /// <summary>The number of primes up to a real <c>x</c>, as far as the sieve counts.</summary>
        public static NumericsComplex PrimePi(NumericsComplex x)
        {
            if (x.Imaginary != 0 || double.IsNaN(x.Real) || double.IsInfinity(x.Real) || Math.Abs(x.Real) > 9e15)
                return NaN;
            return Primes.CountUpTo(EInteger.FromInt64((long)Math.Floor(x.Real))) is { } count ? count : NaN;
        }

        /// <summary>
        /// A whole number held exactly, of any size a <see cref="double"/> has: every finite double past
        /// <c>2^53</c> is whole, and is the integer it reads as.
        /// </summary>
        private static EInteger? Whole(NumericsComplex z)
            => z.Imaginary != 0 || double.IsNaN(z.Real) || double.IsInfinity(z.Real) || Math.Floor(z.Real) != z.Real
                ? null
                : EFloat.FromDouble(z.Real).ToEInteger();

        // gcd and lcm compile at whole arguments only. Evaluation takes them over the rationals, and every finite double
        // is a rational, so a compiled gcd(0.1, 1) would be exact for the double nearest 0.1 and mean nothing for 0.1.
        // Both are non-negative, as evaluation has them: gcd(-4, 6) = 2, gcd(0, 5) = gcd(0, -5) = 5, gcd(0, 0) = 0,
        // lcm(-4, 6) = lcm(-4, -6) = 12, lcm(0, 3) = lcm(0, 0) = 0. Rounded once by EFloat, as (double)BigInteger
        // truncates. https://github.com/asc-community/AngouriMath/issues/2195

        /// <summary>The greatest common divisor of two whole numbers, and <c>NaN</c> where either is not whole.</summary>
        public static NumericsComplex Gcd(NumericsComplex a, NumericsComplex b)
            => Whole(a) is { } left && Whole(b) is { } right
                ? EFloat.FromEInteger(left.GreatestCommonDivisor(right)).ToDouble()
                : NaN;

        /// <summary>The least common multiple of two whole numbers, <c>0</c> where either is, and <c>NaN</c> where either is not whole.</summary>
        public static NumericsComplex Lcm(NumericsComplex a, NumericsComplex b)
            => Whole(a) is { } left && Whole(b) is { } right
                ? left.IsZero || right.IsZero
                    ? 0
                    : EFloat.FromEInteger((left * right).Abs() / left.GreatestCommonDivisor(right)).ToDouble()
                : NaN;

        public static double Mobius(double n) => Real(Mobius(new NumericsComplex(n, 0)));
        public static double PrimeNu(double n) => Real(PrimeNu(new NumericsComplex(n, 0)));
        public static double PrimeOmega(double n) => Real(PrimeOmega(new NumericsComplex(n, 0)));
        public static double DivisorSigma(double k, double n) => Real(DivisorSigma(new NumericsComplex(k, 0), new NumericsComplex(n, 0)));
        public static double Valuation(double n, double p) => Real(Valuation(new NumericsComplex(n, 0), new NumericsComplex(p, 0)));
        public static double Prime(double n) => Real(Prime(new NumericsComplex(n, 0)));
        public static double PrimePi(double x) => Real(PrimePi(new NumericsComplex(x, 0)));
        public static double Gcd(double a, double b) => Real(Gcd(new NumericsComplex(a, 0), new NumericsComplex(b, 0)));
        public static double Lcm(double a, double b) => Real(Lcm(new NumericsComplex(a, 0), new NumericsComplex(b, 0)));

        private static double Real(NumericsComplex z) => z.Imaginary == 0 ? z.Real : double.NaN;
    }
}
