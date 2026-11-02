//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// The operations that are defined on the reals and not on the complex numbers, for the
    /// compiler that carries every value as a <see cref="System.Numerics.Complex"/> whether it
    /// is real or not.
    /// </summary>
    internal static class RealOnly
    {
        /// <summary>
        /// The remainder of one number by another, or NaN where either of them is not real.
        /// </summary>
        /// <remarks>
        /// There is no one remainder of a complex number by another: which multiple of the
        /// divisor to subtract is a choice, and the two usual ones -- rounding the quotient to
        /// the nearest Gaussian integer, or truncating it -- disagree. The interpreter declines
        /// the question and leaves <c>a % b</c> unevaluated; the compiled path has no way to
        /// return an unevaluated node, so it answers NaN, which says the same thing.
        /// <para/>
        /// For real arguments this is the runtime's own <c>%</c>, and so takes the sign of the
        /// dividend, which is what <see cref="Entity.Evaled"/> does as well.
        /// </remarks>
        public static NumericsComplex Mod(NumericsComplex a, NumericsComplex b)
        {
            if (a.Imaginary is not 0 || b.Imaginary is not 0)
                return new NumericsComplex(double.NaN, double.NaN);
            var truncated = a.Real % b.Real;
            // The runtime's own % truncates, where the node is the floored remainder that takes
            // the sign of the divisor. They differ by exactly one divisor, and only where the
            // remainder and the divisor disagree in sign.
            // The + 0.0 is not idle: the runtime's % gives -0.0 for -6 % 3, and while that
            // compares equal to zero it prints as -0 and is not what the interpreter answers.
            return new NumericsComplex(
                (truncated is 0 || truncated < 0 == b.Real < 0
                    ? truncated
                    : truncated + b.Real) + 0.0,
                0);
        }

        /// <summary>
        /// The floored remainder of two reals, which takes the sign of the divisor, as
        /// <see cref="Mod(NumericsComplex, NumericsComplex)"/> takes it. Written as
        /// <c>((a % b) + b) % b</c> it is rounded a second time where the first remainder already
        /// had the divisor's sign: <c>2 mod e</c> came out 1.9999999999999996, and
        /// <c>floor(2 mod e)</c> 1, where both evaluate to 2.
        /// </summary>
        public static double Mod(double a, double b)
        {
            var truncated = a % b;
            return (truncated is 0 || truncated < 0 == b < 0 ? truncated : truncated + b) + 0.0;
        }

        /// <summary>The floored remainder of two reals in single precision, as <see cref="Mod(double, double)"/>.</summary>
        public static float Mod(float a, float b)
        {
            var truncated = a % b;
            return (truncated is 0 || truncated < 0 == b < 0 ? truncated : truncated + b) + 0.0f;
        }

        /// <summary>
        /// The larger of two numbers, or NaN where they are not both real: the complex numbers are
        /// not ordered, and the interpreter leaves <c>max(2, 1 + i)</c> unevaluated and gives
        /// <c>max(i, i)</c> no value. Two equal complex arguments had themselves for a maximum
        /// here after the interpreter stopped saying so, and compiled <c>max(x, x)</c> was
        /// <c>x</c> off the real line. https://github.com/asc-community/AngouriMath/issues/2256
        /// </summary>
        public static NumericsComplex Max(NumericsComplex a, NumericsComplex b)
            => Ordered(a, b, larger: true);

        /// <summary>The smaller of two numbers, on the terms of <see cref="Max"/>.</summary>
        public static NumericsComplex Min(NumericsComplex a, NumericsComplex b)
            => Ordered(a, b, larger: false);

        private static NumericsComplex Ordered(NumericsComplex a, NumericsComplex b, bool larger)
            => a.Imaginary is not 0 || b.Imaginary is not 0 || double.IsNaN(a.Real) || double.IsNaN(b.Real)
                ? new NumericsComplex(double.NaN, double.NaN)
             : a == b ? a
             : new NumericsComplex(larger == a.Real >= b.Real ? a.Real : b.Real, 0);
    }
}
