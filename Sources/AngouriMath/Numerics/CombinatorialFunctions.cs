//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Numerics
{
    /// <summary>
    /// The binomial coefficient, the falling and the rising factorial and the double factorial in
    /// double precision, for the compilers, on the terms the interpreter takes them: a whole order is
    /// a product, and any other goes through the gamma function, with <c>NaN</c> at a pole of the
    /// numerator and <c>0</c> at one of the denominator, where the reciprocal gamma vanishes. At whole
    /// arguments each is computed exactly and rounded once, as evaluation's exact value is.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class CombinatorialFunctions
    {
        [AngouriMath.Core.ConstantField] private static readonly NumericsComplex NaN = new(double.NaN, 0);

        // 2^53: a double holds every whole number below it, so a product of two of them below it is exact.
        private const double ExactBelow = 9007199254740992;

        // Past this many bits a whole number is past the range of a double, whose largest is under 2^1024.
        private const long BeyondDoubles = 1100;

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
                if (IsWhole(n, out var whole))
                    return WholeBinomial(whole, lower);
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
            // n (n - 2) ... down to 2 or 1, and for a negative odd n the reciprocal of (n + 2) (n + 4) ... (-1).
            return whole >= 0
                ? ProductOfWholeFactors(whole, -2, (whole + 1) / 2, reciprocal: false)
                : ProductOfWholeFactors(whole + 2, 2, (-whole - 1) / 2, reciprocal: true);
        }

        public static double DoubleFactorial(double n) => Real(DoubleFactorial(new NumericsComplex(n, 0)));

        /// <summary>
        /// The Catalan number: <c>(2n)!/(n! (n + 1)!)</c> for a whole <c>n &gt;= 0</c>, <c>-1/2</c> at <c>-1</c>, <c>0</c> at
        /// a whole number below it, no value at a negative half-integer, and <c>4^n Γ(n + 1/2)/(√π Γ(n + 2))</c>
        /// elsewhere.
        /// </summary>
        public static NumericsComplex CatalanNumber(NumericsComplex n)
        {
            if (IsWhole(n, out var whole))
            {
                if (whole == -1)
                    return -0.5;
                if (whole < -1)
                    return 0;
                // Exactly, by C_(k + 1) = C_k 2 (2k + 1)/(k + 2), whose division leaves no remainder, and rounded
                // once, since a product of the ratios in doubles rounds at each step and C_5 would be
                // 41.99999999999999. By EFloat, as (double)BigInteger truncates, and C_32 would be
                // 55534064877048190. Past the index the interpreter computes exactly, the gamma ratio.
                if (whole <= 1000)
                {
                    var catalan = EInteger.One;
                    for (var k = 0; k < whole; k++)
                    {
                        catalan = catalan.Multiply(2 * (2 * k + 1)).Divide(k + 2);
                        // The Catalan numbers grow, so once one is past the doubles the rest are.
                        if (catalan.GetUnsignedBitLengthAsInt64() > BeyondDoubles)
                            return double.PositiveInfinity;
                    }
                    return EFloat.FromEInteger(catalan).ToDouble();
                }
            }
            if (IsWhole(n + 0.5, out var half) && half <= 0)
                return NaN;
            var top = SpecialFunctions.Factorial(n - 0.5);
            var bottom = SpecialFunctions.Factorial(n + 1);
            return NumericsComplex.Pow(4, n) * top / (Math.Sqrt(Math.PI) * bottom);
        }

        public static double CatalanNumber(double n) => Real(CatalanNumber(new NumericsComplex(n, 0)));

        // x (x - 1) ... (x - n + 1) falling and x (x + 1) ... (x + n - 1) rising for n > 0, and for
        // n < 0 the reciprocal of (x + 1) ... (x - n) falling and of (x - 1) ... (x + n) rising.
        private static NumericsComplex OfAWholeOrder(NumericsComplex x, long n, bool falling)
        {
            var step = falling == n > 0 ? -1 : 1;
            if (IsWhole(x, out var whole))
                return ProductOfWholeFactors(n > 0 ? whole : whole + step, step, Math.Abs(n), reciprocal: n < 0);
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

        // binomial(n, k) for a whole n and a whole k >= 0, k <= n where n >= 0, exactly and rounded once: by
        // binomial(m, i + 1) = binomial(m, i) (m - i)/(i + 1), whose division leaves no remainder, over the
        // shorter of k and m - k. In doubles while the product before the division is below 2^53, where it
        // and the quotient are exact, and past it in EInteger. For a negative n, binomial(n, k) is
        // (-1)^k binomial(k - n - 1, k).
        private static double WholeBinomial(long n, long k)
        {
            var sign = n < 0 && k % 2 != 0 ? -1 : 1;
            var top = n < 0 ? k - n - 1 : n;
            var steps = Math.Min(k, top - k);
            double value = 1;
            long i = 0;
            for (; i < steps; i++)
            {
                double factor = top - i;
                var product = value * factor;
                if (factor >= ExactBelow || product >= ExactBelow)
                    break;
                value = product / (i + 1);
            }
            if (i == steps)
                return sign * value;
            var exact = EInteger.FromInt64((long)value);
            for (; i < steps; i++)
            {
                exact = exact * EInteger.FromInt64(top - i) / EInteger.FromInt64(i + 1);
                // The binomials grow up to the middle, so once one is past the doubles the rest are.
                if (exact.GetUnsignedBitLengthAsInt64() > BeyondDoubles)
                    return sign * double.PositiveInfinity;
            }
            return sign * EFloat.FromEInteger(exact).ToDouble();
        }

        // The product of the whole factors first, first + step, ..., count of them, or its reciprocal,
        // exactly and rounded once: in doubles while each partial product is below 2^53, where it is exact,
        // and past it in EInteger. 0 where a factor vanishes, and NaN for its reciprocal. The callers' factors
        // are consecutive, or two apart without crossing 0, so otherwise they have one sign and are each at
        // least 1 in size: a partial product past the range of the doubles stays past it.
        private static double ProductOfWholeFactors(long first, long step, long count, bool reciprocal)
        {
            if (count == 0)
                return 1;
            var last = first + step * (count - 1);
            if (Math.Min(first, last) <= 0 && Math.Max(first, last) >= 0 && first % step == 0)
                return reciprocal ? double.NaN : 0;
            double product = 1;
            long i = 0;
            for (; i < count; i++)
            {
                double factor = first + step * i;
                var next = product * factor;
                if (Math.Abs(factor) >= ExactBelow || Math.Abs(next) >= ExactBelow)
                    break;
                product = next;
            }
            if (i == count)
                return reciprocal ? 1 / product : product;
            var exact = EInteger.FromInt64((long)product);
            for (; i < count; i++)
            {
                exact *= EInteger.FromInt64(first + step * i);
                if (exact.GetUnsignedBitLengthAsInt64() > BeyondDoubles)
                {
                    var sign = first < 0 && count % 2 != 0 ? -1 : 1;
                    return reciprocal ? sign * 0.0 : sign * double.PositiveInfinity;
                }
            }
            return reciprocal
                ? ERational.Create(exact.Sign < 0 ? EInteger.FromInt32(-1) : EInteger.One, exact.Abs()).ToDouble()
                : EFloat.FromEInteger(exact).ToDouble();
        }

        private static bool IsInfinite(NumericsComplex z) => double.IsInfinity(z.Real) || double.IsInfinity(z.Imaginary);

        // The real value of a real argument's result, and NaN where it is not real.
        private static double Real(NumericsComplex z) => z.Imaginary == 0 ? z.Real : double.NaN;
    }
}
