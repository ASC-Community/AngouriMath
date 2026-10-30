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
        public static NumericsComplex Product(NumericsComplex a, NumericsComplex b)
            => a.Imaginary == 0 && b.Imaginary == 0 ? new NumericsComplex(a.Real * b.Real, 0) : a * b;

        /// <summary>
        /// The quotient, real where both are real and the divisor is not 0. A division by 0 is left to
        /// <see cref="System.Numerics.Complex"/>, whose <c>NaN</c> is evaluation's: <c>1/0</c> has no value.
        /// </summary>
        public static NumericsComplex Quotient(NumericsComplex a, NumericsComplex b)
            => a.Imaginary == 0 && b.Imaginary == 0 && b.Real != 0 ? new NumericsComplex(a.Real / b.Real, 0) : a / b;

        /// <summary>
        /// The power, real where both are real and it is: a positive base, or a negative one to a whole power.
        /// <see cref="System.Numerics.Complex.Pow(System.Numerics.Complex, System.Numerics.Complex)"/> takes
        /// <c>e^(b ln a)</c>, which at an infinite base brings in <c>∞ · 0</c>, so <c>x^2</c> at <c>+oo</c> was
        /// <c>(NaN, NaN)</c>; and it gave <c>0</c> for <c>0^(-1)</c> and <c>1</c> for <c>0^0</c>, where evaluation has no value.
        /// Anything else is left to it. https://github.com/asc-community/AngouriMath/issues/2225
        /// </summary>
        public static NumericsComplex Power(NumericsComplex a, NumericsComplex b)
        {
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
            if (!double.IsInfinity(y) && Math.Floor(y) == y)
                return new NumericsComplex(Math.Pow(x, y), 0);
            return NumericsComplex.Pow(ComplexBranches.Unsigned(a), ComplexBranches.Unsigned(b));
        }
    }
}
