//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// The product and the quotient of two numbers carried as <see cref="System.Numerics.Complex"/>, real where both
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
    }
}
