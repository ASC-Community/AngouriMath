//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// The branches the compilers take where <see cref="System.Numerics.Complex"/> takes a
    /// different one from the rest of the library. Everything here is answering the same
    /// question: what does <see cref="Entity.Evaled"/> give?
    /// </summary>
    internal static class ComplexBranches
    {
        /// <summary>
        /// The arcsine the library evaluates to.
        /// </summary>
        /// <remarks>
        /// <see cref="System.Numerics.Complex.Asin(System.Numerics.Complex)"/> agrees with
        /// <see cref="Entity.Evaled"/> everywhere except on the two branch cuts, the real
        /// arguments outside [-1, 1], where it takes the upper side and the library takes the
        /// lower one: the library reads arcsin(3) as pi/2 - 1.7627i, and
        /// <see cref="System.Numerics.Complex"/> as pi/2 + 1.7627i.
        /// <para/>
        /// Conjugating on the cut is the whole of the difference. Measured over a grid of 63
        /// points spanning both cuts, both sides of each and the interval between them, this
        /// agrees with <see cref="Entity.Evaled"/> at every one.
        /// <para/>
        /// The FE compiler used to conjugate <em>unconditionally</em>, which is right on the
        /// cut -- the only place anything tested it -- and wrong everywhere else. It made
        /// compiled arcsine the conjugate of the arcsine, so <c>sin(arcsin(z))</c> came back
        /// as the conjugate of z and <c>arcsin(z) + arccos(z)</c> was not pi/2 off the real
        /// axis. Newton's method felt it as a divergence away from a root it was started on:
        /// see https://github.com/asc-community/AngouriMath/issues/115.
        /// </remarks>
        public static NumericsComplex Arcsin(NumericsComplex a)
            => (a = Unsigned(a)).Imaginary == 0 && Math.Abs(a.Real) > 1
                ? NumericsComplex.Conjugate(NumericsComplex.Asin(a))
                : NumericsComplex.Asin(a);

        /// <summary>
        /// <paramref name="z"/> with each zero part read as <c>+0</c>. The library has no signed zero, so a real
        /// number is on the upper side of every branch cut it lies on; <see cref="System.Numerics.Complex"/>
        /// computes <c>-0</c> as a part of many real values, <c>cos(2)</c> and <c>conj(-2)</c> among them, and reads
        /// it as the lower side, which made <c>ln(cos(x))</c> at 2 the conjugate of the logarithm. Adding <c>+0</c>
        /// turns <c>-0</c> into <c>+0</c> and changes no other number. Taken at the entry of every function with a cut.
        /// https://github.com/asc-community/AngouriMath/issues/2236
        /// </summary>
        public static NumericsComplex Unsigned(NumericsComplex z) => new(z.Real + 0.0, z.Imaginary + 0.0);

        /// <summary>
        /// The logarithm of <paramref name="value"/> to <paramref name="base"/>, <c>ln(value) / ln(base)</c> for any
        /// base, where FastExpression's <c>Complex.Log(value, base.Real)</c> dropped a complex base's imaginary part.
        /// https://github.com/asc-community/AngouriMath/issues/2239
        /// </summary>
        public static NumericsComplex Log(NumericsComplex @base, NumericsComplex value)
            => RealProducts.Quotient(NumericsComplex.Log(Unsigned(value)), NumericsComplex.Log(Unsigned(@base)));

        public static NumericsComplex Arccos(NumericsComplex a) => NumericsComplex.Acos(Unsigned(a));

        public static NumericsComplex Arctan(NumericsComplex a) => NumericsComplex.Atan(Unsigned(a));

        public static NumericsComplex Arccotan(NumericsComplex a) => NumericsComplex.Atan(Unsigned(1 / a));

        public static NumericsComplex Arcsecant(NumericsComplex a) => NumericsComplex.Acos(Unsigned(1 / a));

        /// <summary>
        /// The arccosecant the library evaluates to, which is <see cref="Arcsin"/> of the
        /// reciprocal and so lands on the same cuts -- for arccosecant, at the real arguments
        /// strictly inside [-1, 1].
        /// </summary>
        public static NumericsComplex Arccosecant(NumericsComplex a)
            => Arcsin(1 / a);
    }
}
