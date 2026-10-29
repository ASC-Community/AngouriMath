//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Numerics
{
    /// <summary>
    /// The binomial coefficient, the falling and the rising factorial and the double factorial in
    /// double precision, for the compilers, on the terms the interpreter takes them: a whole order is
    /// a product, and any other goes through the gamma function, with <c>NaN</c> at a pole of the
    /// numerator and <c>0</c> at one of the denominator, where the reciprocal gamma vanishes.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class CombinatorialFunctions
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

        /// <summary>
        /// <c>binomial(n, k)</c>: for a whole <c>k</c>, <c>n (n - 1) ... (n - k + 1)/k!</c>, <c>0</c> below
        /// <c>k = 0</c> and above a whole <c>n &gt;= 0</c>; for any other <c>k</c>,
        /// <c>Γ(n + 1)/(Γ(k + 1) Γ(n - k + 1))</c>, <c>NaN</c> at a negative whole <c>n</c> and <c>0</c>
        /// where <c>n - k</c> is a negative whole number.
        /// </summary>
        public static NumericsComplex Binomial(NumericsComplex n, NumericsComplex k)
        {
            if (IsWhole(k, out var lower))
            {
                if (lower < 0 || IsWhole(n, out var upper) && upper >= 0 && lower > upper)
                    return 0;
                NumericsComplex product = 1;
                for (long i = 0; i < lower && product != 0 && !IsInfinite(product); i++)
                    product *= (n - i) / (i + 1);
                return product;
            }
            if (IsWhole(n, out var negative) && negative < 0)
                return NaN;
            if (IsWhole(n - k, out var difference) && difference < 0)
                return 0;
            return SpecialFunctions.Factorial(n) / (SpecialFunctions.Factorial(k) * SpecialFunctions.Factorial(n - k));
        }

        public static double Binomial(double n, double k) => Real(Binomial(new NumericsComplex(n, 0), new NumericsComplex(k, 0)));

        /// <summary>
        /// The falling factorial <c>x (x - 1) ... (x - n + 1)</c>: for a whole <c>n</c> the product, the
        /// reciprocal of <c>(x + 1) ... (x - n)</c> for a negative one and <c>NaN</c> where a factor of
        /// that vanishes; for any other <c>n</c>, <c>Γ(x + 1)/Γ(x - n + 1)</c>, <c>NaN</c> at a negative
        /// whole <c>x</c> and <c>0</c> where <c>x - n</c> is a negative whole number.
        /// </summary>
        public static NumericsComplex FallingFactorial(NumericsComplex x, NumericsComplex n)
        {
            if (IsWhole(n, out var order))
                return OfAWholeOrder(x, order, falling: true);
            if (IsWhole(x, out var negative) && negative < 0)
                return NaN;
            if (IsWhole(x - n, out var difference) && difference < 0)
                return 0;
            return SpecialFunctions.Factorial(x) / SpecialFunctions.Factorial(x - n);
        }

        public static double FallingFactorial(double x, double n) => Real(FallingFactorial(new NumericsComplex(x, 0), new NumericsComplex(n, 0)));

        /// <summary>
        /// The rising factorial <c>x (x + 1) ... (x + n - 1)</c>: for a whole <c>n</c> the product, the
        /// reciprocal of <c>(x - 1) ... (x + n)</c> for a negative one and <c>NaN</c> where a factor of
        /// that vanishes; for any other <c>n</c>, <c>Γ(x + n)/Γ(x)</c>, <c>NaN</c> where <c>x + n</c> is a
        /// whole number not above 0 and <c>0</c> where <c>x</c> is one.
        /// </summary>
        public static NumericsComplex RisingFactorial(NumericsComplex x, NumericsComplex n)
        {
            if (IsWhole(n, out var order))
                return OfAWholeOrder(x, order, falling: false);
            if (IsWhole(x + n, out var top) && top <= 0)
                return NaN;
            if (IsWhole(x, out var bottom) && bottom <= 0)
                return 0;
            return SpecialFunctions.Factorial(x + n - 1) / SpecialFunctions.Factorial(x - 1);
        }

        public static double RisingFactorial(double x, double n) => Real(RisingFactorial(new NumericsComplex(x, 0), new NumericsComplex(n, 0)));

        /// <summary>
        /// The double factorial: <c>n (n - 2) ...</c> down to 1 or 2 for a whole <c>n &gt;= 0</c>,
        /// <c>(n + 2)!!/(n + 2)</c> for a negative odd one, and no value for a negative even one or for
        /// any number that is not whole.
        /// </summary>
        public static NumericsComplex DoubleFactorial(NumericsComplex n)
        {
            if (!IsWhole(n, out var whole) || whole < 0 && whole % 2 == 0)
                return NaN;
            NumericsComplex product = 1;
            if (whole >= 0)
            {
                for (var k = whole; k > 1 && !IsInfinite(product); k -= 2)
                    product *= k;
                return product;
            }
            for (var j = whole + 2; j <= -1; j += 2)
                product *= j;
            return 1 / product;
        }

        public static double DoubleFactorial(double n) => Real(DoubleFactorial(new NumericsComplex(n, 0)));

        // x (x - 1) ... (x - n + 1) falling and x (x + 1) ... (x + n - 1) rising for n > 0, and for
        // n < 0 the reciprocal of (x + 1) ... (x - n) falling and of (x - 1) ... (x + n) rising.
        private static NumericsComplex OfAWholeOrder(NumericsComplex x, long n, bool falling)
        {
            var step = falling == n > 0 ? -1 : 1;
            NumericsComplex product = 1;
            for (long i = 0; i < Math.Abs(n) && product != 0 && !IsInfinite(product); i++)
            {
                var factor = x + (n > 0 ? step * i : step * (i + 1));
                if (n < 0 && factor == 0)
                    return NaN;
                product *= factor;
            }
            return n > 0 ? product : 1 / product;
        }

        private static bool IsInfinite(NumericsComplex z) => double.IsInfinity(z.Real) || double.IsInfinity(z.Imaginary);

        // The real value of a real argument's result, and NaN where it is not real.
        private static double Real(NumericsComplex z) => z.Imaginary == 0 ? z.Real : double.NaN;
    }
}
