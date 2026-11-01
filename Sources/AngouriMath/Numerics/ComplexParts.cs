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
    /// The conjugate, the principal argument and the real and the imaginary parts in double
    /// precision, which both compilers call, with the values the interpreter gives.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class ComplexParts
    {
        public static NumericsComplex Conj(NumericsComplex z) => NumericsComplex.Conjugate(z);
        public static double Conj(double x) => x;

        /// <summary>
        /// The principal argument, in <c>(-pi, pi]</c>, with no value at 0, as <c>arg</c> is. A
        /// negative zero is read as zero, so that <c>-1</c> is at <c>pi</c> and not at <c>-pi</c>,
        /// where <see cref="Math.Atan2"/> would put <c>-1 - 0i</c>.
        /// </summary>
        public static NumericsComplex Arg(NumericsComplex z) => Arg(z.Real, z.Imaginary);
        public static double Arg(double x) => Arg(x, 0);
        private static double Arg(double re, double im)
            => re == 0 && im == 0 ? double.NaN : Math.Atan2(im == 0 ? 0.0 : im, re == 0 ? 0.0 : re);

        /// <summary>
        /// The real and the imaginary part, which a number with no value does not have either: a
        /// NaN in one part is the whole number's, as a NaN over the doubles has an imaginary part
        /// of NaN. Taking only the other part gave <c>Im(Im(x / x))</c> the value 0 at 0, where
        /// <c>x / x</c> has none.
        /// </summary>
        public static NumericsComplex Re(NumericsComplex z) => double.IsNaN(z.Imaginary) ? double.NaN : z.Real;
        public static double Re(double x) => x;
        public static NumericsComplex Im(NumericsComplex z) => double.IsNaN(z.Real) ? double.NaN : z.Imaginary;
        public static double Im(double x) => double.IsNaN(x) ? double.NaN : 0;
    }
}
