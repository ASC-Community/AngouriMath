//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using AngouriMath.Functions;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
            /// <summary>
            /// The branch <paramref name="branch"/> of the Lambert W function at <paramref name="z"/>,
            /// the solution <c>w</c> of <c>w e^w = z</c> on that branch, at the working precision; or
            /// <see langword="null"/> where it is not computed.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The branches are those of Corless, Gonnet, Hare, Jeffrey and Knuth, "On the Lambert W
            /// function" (1996), as Mathematica's <c>ProductLog</c> and mpmath's <c>lambertw</c> have
            /// them: the principal branch <c>W_0</c> is real on <c>[-1/e, +oo)</c>, <c>W_(-1)</c> is real
            /// on <c>[-1/e, 0)</c>, and a value on a branch cut is the one approached from above.
            /// </para>
            /// <para>
            /// The branch is found in double precision, where it is cheap, by the kernel the compilers use,
            /// <see cref="AngouriMath.Numerics.LambertWFunction"/>: from the series in <c>p = sqrt(2 (e z + 1))</c>
            /// about the branch point <c>-1/e</c>, from <c>ln(1 + z)</c> elsewhere on the principal branch short of
            /// <c>|z| = 3</c>, and from <c>ln z + 2 pi i k - ln(ln z + 2 pi i k)</c> otherwise, each then taken to a
            /// root by Halley's iteration. That root is refined by the same iteration
            /// at the working precision, which triples the digits with each step and does not leave the
            /// branch. Near the branch point, where double precision cannot resolve <c>z + 1/e</c>, the
            /// series is evaluated at the working precision instead, and refined from there.
            /// </para>
            /// https://github.com/asc-community/AngouriMath/issues/2182
            /// </remarks>
            internal static Complex? LambertW(EInteger branch, Complex z)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (re, im) = (z.RealPart.EDecimal, z.ImaginaryPart.EDecimal);
                if (re.IsNaN() || im.IsNaN())
                    return Real.NaN;
                if (!re.IsFinite || !im.IsFinite || !branch.CanFitInInt32())
                    return null;
                var k = branch.ToInt32Checked();
                if (re.IsZero && im.IsZero)
                    return k == 0 ? Integer.Zero : Real.NegativeInfinity;
                var work = Working(context, 16);
                var precision = work.Precision.ToInt32Checked();

                // Real where the branch is real: W_0 on [-1/e, +oo) and W_(-1) on [-1/e, 0).
                var e = EDecimal.One.Exponential(work);
                var nearTheBranchPoint = e.Multiply(re, work).Add(EDecimal.One, work);
                var isReal = im.IsZero && (k == 0 && nearTheBranchPoint.Sign >= 0 || k == -1 && nearTheBranchPoint.Sign >= 0 && re.Sign < 0);

                (EDecimal Re, EDecimal Im) w;
                var distance = Magnitude((nearTheBranchPoint, e.Multiply(im, work)));
                // The two branches that meet at -1/e: W_0 from either side, W_(-1) from above and on the
                // axis, W_1 from below.
                var meetsTheBranchPoint = k == 0 || k == -1 && im.Sign >= 0 || k == 1 && im.Sign < 0;
                if (meetsTheBranchPoint && distance.CompareTo(EDecimal.FromString("0.25")) < 0)
                {
                    // p = sqrt(2 (e z + 1)), principal, and w = -1 + p - p^2/3 + 11 p^3/72 on W_0, -p on
                    // the other branch. At the working precision, since e z + 1 cancels.
                    (EDecimal Re, EDecimal Im) twice = (nearTheBranchPoint.Multiply(EDecimal.FromInt32(2), work), e.Multiply(im, work).Multiply(EDecimal.FromInt32(2), work));
                    var p = Sqrt(twice, work);
                    if (k != 0)
                        p = (p.Re.Negate(), p.Im.Negate());
                    var p2 = Multiply(p, p, work);
                    var p3 = Multiply(p2, p, work);
                    w = (EDecimal.One.Negate().Add(p.Re, work).Subtract(p2.Re.Divide(EDecimal.FromInt32(3), work), work).Add(p3.Re.Multiply(EDecimal.FromInt32(11), work).Divide(EDecimal.FromInt32(72), work), work),
                         p.Im.Subtract(p2.Im.Divide(EDecimal.FromInt32(3), work), work).Add(p3.Im.Multiply(EDecimal.FromInt32(11), work).Divide(EDecimal.FromInt32(72), work), work));
                }
                else
                {
                    var start = AngouriMath.Numerics.LambertWFunction.LambertW(new NumericsComplex(k, 0), new NumericsComplex(re.ToDouble(), im.ToDouble()));
                    if (double.IsNaN(start.Real) || double.IsNaN(start.Imaginary) || double.IsInfinity(start.Real) || double.IsInfinity(start.Imaginary))
                        return null;
                    w = (EDecimal.FromDouble(start.Real), EDecimal.FromDouble(start.Imaginary));
                }
                if (isReal)
                    w = (w.Re, EDecimal.Zero);

                var tolerance = EDecimal.Create(EInteger.One, EInteger.FromInt32(-precision + 4));
                for (var step = 0; step < 64; step++)
                {
                    // Halley: w - f / (e^w (w + 1) - (w + 2) f / (2 w + 2)), with f = w e^w - z.
                    var ew = Exp(w, work);
                    var wew = Multiply(w, ew, work);
                    (EDecimal Re, EDecimal Im) f = (wew.Re.Subtract(re, work), wew.Im.Subtract(im, work));
                    (EDecimal Re, EDecimal Im) wPlusOne = (w.Re.Add(EDecimal.One, work), w.Im);
                    if (wPlusOne.Re.IsZero && wPlusOne.Im.IsZero)
                        break;
                    var correction = Divide(Multiply((w.Re.Add(EDecimal.FromInt32(2), work), w.Im), f, work),
                        (wPlusOne.Re.Multiply(EDecimal.FromInt32(2), work), wPlusOne.Im.Multiply(EDecimal.FromInt32(2), work)), work);
                    var slope = Multiply(ew, wPlusOne, work);
                    var change = Divide(f, (slope.Re.Subtract(correction.Re, work), slope.Im.Subtract(correction.Im, work)), work);
                    w = (w.Re.Subtract(change.Re, work), isReal ? EDecimal.Zero : w.Im.Subtract(change.Im, work));
                    if (Magnitude(change).CompareTo(tolerance.Multiply(EDecimal.Max(Magnitude(w), EDecimal.One), work)) <= 0)
                        break;
                }
                return Complex.Create(w.Re.RoundToPrecision(context), w.Im.RoundToPrecision(context));
            }

            private static (EDecimal Re, EDecimal Im) Exp((EDecimal Re, EDecimal Im) w, EContext work)
            {
                var modulus = w.Re.Exponential(work);
                return (modulus.Multiply(w.Im.Cos(work), work), modulus.Multiply(w.Im.Sin(work), work));
            }

            private static (EDecimal Re, EDecimal Im) Divide((EDecimal Re, EDecimal Im) a, (EDecimal Re, EDecimal Im) b, EContext work)
            {
                var norm = b.Re.Multiply(b.Re, work).Add(b.Im.Multiply(b.Im, work), work);
                return (a.Re.Multiply(b.Re, work).Add(a.Im.Multiply(b.Im, work), work).Divide(norm, work),
                        a.Im.Multiply(b.Re, work).Subtract(a.Re.Multiply(b.Im, work), work).Divide(norm, work));
            }

            /// <summary>The principal square root, with its cut on the negative axis, approached from above.</summary>
            private static (EDecimal Re, EDecimal Im) Sqrt((EDecimal Re, EDecimal Im) z, EContext work)
            {
                var modulus = z.Re.Multiply(z.Re, work).Add(z.Im.Multiply(z.Im, work), work).SqrtByIntegerRoot(work);
                var re = modulus.Add(z.Re, work).Divide(EDecimal.FromInt32(2), work).SqrtByIntegerRoot(work);
                var im = modulus.Subtract(z.Re, work).Divide(EDecimal.FromInt32(2), work).SqrtByIntegerRoot(work);
                return (re, z.Im.Sign < 0 ? im.Negate() : im);
            }
        }
    }
}
