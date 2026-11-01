//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// The product, the quotient and the power of two numbers carried as <see cref="System.Numerics.Complex"/>, real where both
    /// are real. <see cref="System.Numerics.Complex"/> forms the product's imaginary part as <c>a.Re b.Im + a.Im b.Re</c>,
    /// which is <c>∞ · 0</c>, <c>NaN</c>, where a real factor is infinite, so a compiled <c>-x</c> at <c>+oo</c> was
    /// <c>(-∞, NaN)</c>, a value with no imaginary part at all, where evaluation gives <c>-oo</c>.
    /// https://github.com/asc-community/AngouriMath/issues/2222
    /// </summary>
    internal static class RealProducts
    {
        /// <summary>
        /// The product as evaluation takes it: a part that is 0 times a part that is infinite is 0, so <c>i</c> times
        /// <c>+oo</c> is <c>+oo i</c>, and a number that is 0 times an infinite one is <c>NaN</c>, as <c>0 * oo</c> is.
        /// <see cref="System.Numerics.Complex"/> took <c>0 · ∞</c> as <c>NaN</c> in every part, so <c>i * x</c> at <c>+oo</c>
        /// was <c>(NaN, ∞)</c>. https://github.com/asc-community/AngouriMath/issues/2247
        /// </summary>
        public static NumericsComplex Product(NumericsComplex a, NumericsComplex b)
        {
            if (a.Imaginary == 0 && b.Imaginary == 0)
                return new NumericsComplex(a.Real * b.Real, 0);
            if (a == NumericsComplex.Zero && IsInfinite(b) || b == NumericsComplex.Zero && IsInfinite(a))
                return new NumericsComplex(double.NaN, 0);
            var (ac, bd) = (PartProduct(a.Real, b.Real), PartProduct(a.Imaginary, b.Imaginary));
            var (ad, bc) = (PartProduct(a.Real, b.Imaginary), PartProduct(a.Imaginary, b.Real));
            return new NumericsComplex(ac - bd, ad + bc);

            static double PartProduct(double x, double y) => x == 0 && double.IsInfinity(y) || y == 0 && double.IsInfinity(x) ? 0 : x * y;
            static bool IsInfinite(NumericsComplex z) => double.IsInfinity(z.Real) || double.IsInfinity(z.Imaginary);
        }

        /// <summary>
        /// The quotient, real where both are real and the divisor is not 0, and taken part by part where only the
        /// divisor is real and not 0, as evaluation takes it: <see cref="System.Numerics.Complex"/> multiplied each part by
        /// the divisor's imaginary 0, so <c>ln(-oo)</c>, <c>+oo + pi i</c>, over <c>1</c> was <c>(∞, NaN)</c>. A division by 0
        /// is left to <see cref="System.Numerics.Complex"/>, whose <c>NaN</c> is evaluation's: <c>1/0</c> has no value.
        /// https://github.com/asc-community/AngouriMath/issues/2247
        /// </summary>
        public static NumericsComplex Quotient(NumericsComplex a, NumericsComplex b)
            => b.Imaginary == 0 && b.Real != 0
                ? new NumericsComplex(a.Real / b.Real, a.Imaginary == 0 ? 0 : a.Imaginary / b.Real)
                : a / b;

        /// <summary>
        /// The power, real where both are real and it is: a positive base, or a negative one to a whole power.
        /// <see cref="System.Numerics.Complex.Pow(System.Numerics.Complex, System.Numerics.Complex)"/> takes
        /// <c>e^(b ln a)</c>, which at an infinite base brings in <c>∞ · 0</c>, so <c>x^2</c> at <c>+oo</c> was
        /// <c>(NaN, NaN)</c>; and it gave <c>0</c> for <c>0^(-1)</c> and <c>1</c> for <c>0^0</c>, where evaluation has no value.
        /// Anything else is left to it. https://github.com/asc-community/AngouriMath/issues/2225
        /// </summary>
        public static NumericsComplex Power(NumericsComplex a, NumericsComplex b)
        {
            if (a.Imaginary != 0 && b.Imaginary == 0 && IsWhole(b.Real))
                return WholePower(a, b.Real);
            if (a.Imaginary != 0 || b.Imaginary != 0)
                return NumericsComplex.Pow(ComplexBranches.Unsigned(a), ComplexBranches.Unsigned(b));
            var (x, y) = (a.Real, b.Real);
            if (double.IsNaN(x) || double.IsNaN(y))
                return new NumericsComplex(double.NaN, 0);
            // 0^y is 0 for a positive y and has no value otherwise, 0^0 included, as evaluation has it.
            if (x == 0)
                return y > 0 ? NumericsComplex.Zero : new NumericsComplex(double.NaN, 0);
            if (x > 0)
                return new NumericsComplex(y == 0.5 ? Math.Sqrt(x) : Math.Pow(x, y), 0);
            if (IsWhole(y))
                return new NumericsComplex(Math.Pow(x, y), 0);
            return NumericsComplex.Pow(ComplexBranches.Unsigned(a), ComplexBranches.Unsigned(b));
        }

        private static bool IsWhole(double y) => !double.IsInfinity(y) && Math.Floor(y) == y;

        /// <summary>
        /// A power to a rational exponent whose denominator is odd and below ten, as evaluation takes it:
        /// of a negative real, the real root to the numerator, <c>(-8)^(1/3) = -2</c> and <c>(-8)^(2/3) = 4</c>,
        /// as <c>Number.Pow</c>'s search for a good root finds it; of any other number, <see cref="Power"/>.
        /// The principal power gave <c>1 + 1.73i</c> and <c>-2 + 3.46i</c>, and <c>Math.Pow</c> NaN over
        /// <c>double</c>, where evaluation has a real value. Whether the exponent is such a rational is only
        /// known where it is written, so the compilers choose this where they compile the power.
        /// </summary>
        public static NumericsComplex RealRootPower(NumericsComplex a, NumericsComplex b, bool oddNumerator)
            => a.Imaginary == 0 && a.Real < 0 && b.Imaginary == 0 && !double.IsNaN(b.Real)
                ? new NumericsComplex((oddNumerator ? -1 : 1) * Math.Pow(-a.Real, b.Real), 0)
                : Power(a, b);

        /// <summary><see cref="RealRootPower(NumericsComplex, NumericsComplex, bool)"/> over <see cref="double"/>.</summary>
        public static double RealRootPower(double a, double b, bool oddNumerator)
            => a < 0 && !double.IsNaN(b)
                ? (oddNumerator ? -1 : 1) * Math.Pow(-a, b)
                : IntoLinq.MathAllMethods.Pow(a, b);

        /// <summary>
        /// Whether a power to <paramref name="exponent"/> takes the real root of a negative real, in evaluation:
        /// a rational that is not whole, with an odd denominator below ten.
        /// </summary>
        internal static bool TakesTheRealRootOfANegative(PeterO.Numbers.ERational exponent)
            => !exponent.Denominator.Equals(PeterO.Numbers.EInteger.One) && !exponent.Denominator.IsEven
                && exponent.Denominator.CompareTo(PeterO.Numbers.EInteger.FromInt32(10)) < 0;

        /// <summary>
        /// A whole power of a number that is not real, as a product of its factors, which is how evaluation takes it.
        /// <see cref="System.Numerics.Complex.Pow(System.Numerics.Complex, System.Numerics.Complex)"/> takes <c>e^(n ln z)</c>,
        /// whose rounded angle leaves a part that should be 0 at about <c>1e-16</c>: <c>i^2</c> was <c>(-1, 1.2e-16)</c>, which is
        /// not real, so <c>(i pi)^2 &gt; 0</c> had no truth value where it is false. Past <c>2^62</c> the exponent is
        /// left to it, where a product would take more than 124 multiplications.
        /// https://github.com/asc-community/AngouriMath/issues/2247
        /// </summary>
        private static NumericsComplex WholePower(NumericsComplex z, double n)
        {
            if (Math.Abs(n) > 4.611686018427388E+18)
                return NumericsComplex.Pow(ComplexBranches.Unsigned(z), n);
            var power = NumericsComplex.One;
            for (var (factor, remaining) = (z, (long)Math.Abs(n)); remaining > 0; remaining >>= 1)
            {
                if ((remaining & 1) == 1)
                    power = Product(power, factor);
                if (remaining > 1)
                    factor = Product(factor, factor);
            }
            return n < 0 ? Quotient(NumericsComplex.One, power) : power;
        }
    }
}
