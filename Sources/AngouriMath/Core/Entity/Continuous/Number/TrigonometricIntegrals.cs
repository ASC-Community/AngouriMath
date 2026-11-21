//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
            /// <summary>
            /// The sine integral, <c>Si(z) = sum_(k >= 0) (-1)^k z^(2k + 1)/((2k + 1) (2k + 1)!)</c>, at the
            /// working precision, for every complex <c>z</c>: entire, odd, real on the real line and
            /// imaginary on the imaginary axis.
            /// </summary>
            /// <remarks>
            /// <para>
            /// With <c>P</c> digits of working precision, up to <c>|z| = (P + 5) ln 10</c> the four
            /// integrals are summed as their series, which cancel <c>(|z| - |Im z|)/ln 10</c> digits for
            /// <c>Si</c> and <c>Ci</c>, and <c>(|z| - |Re z|)/ln 10</c> for <c>Shi</c> and <c>Chi</c>,
            /// carried as guard digits. Beyond that they are the exponential integral's, whose
            /// asymptotic series answers there: <c>Shi(z) = (Ei(z) - Ei(-z))/2 - i pi/2 sgn(Im z)</c>,
            /// <c>Chi(z) = (Ei(z) + Ei(-z))/2</c> with <c>i pi/2 sgn(Im z)</c>, or <c>i pi</c> on the
            /// negative real axis, <c>Si(z) = -i Shi(i z)</c> and <c>Ci(z) = Chi(i z) + ln z - ln(i z)</c>,
            /// each branch term read off <c>(ln z - ln(1/z))/2</c> in <see cref="Ei(Complex)"/>.
            /// </para>
            /// <para>
            /// On the axes where a value is real or imaginary, or has a known imaginary part, it is
            /// given so, and not with a rounding residue: <c>Ci(-x)</c> is <c>Ci(x) + i pi</c> exactly.
            /// </para>
            /// https://github.com/asc-community/AngouriMath/issues/1501
            /// </remarks>
            public static Complex Si(Complex z) => TrigonometricIntegral(z, hyperbolic: false, odd: true);

            /// <summary>
            /// The cosine integral, <c>Ci(z) = gamma + ln z + sum_(k >= 1) (-1)^k z^(2k)/(2k (2k)!)</c>, with the
            /// principal logarithm, at the working precision, for every complex <c>z</c>; <c>Ci(0)</c>
            /// is <c>-oo</c>. See <see cref="Si(Complex)"/>.
            /// </summary>
            public static Complex Ci(Complex z) => TrigonometricIntegral(z, hyperbolic: false, odd: false);

            /// <summary>
            /// The hyperbolic sine integral, <c>Shi(z) = sum_(k >= 0) z^(2k + 1)/((2k + 1) (2k + 1)!)</c>, at the
            /// working precision, for every complex <c>z</c>. See <see cref="Si(Complex)"/>.
            /// </summary>
            public static Complex Shi(Complex z) => TrigonometricIntegral(z, hyperbolic: true, odd: true);

            /// <summary>
            /// The hyperbolic cosine integral, <c>Chi(z) = gamma + ln z + sum_(k >= 1) z^(2k)/(2k (2k)!)</c>,
            /// with the principal logarithm, at the working precision, for every complex <c>z</c>;
            /// <c>Chi(0)</c> is <c>-oo</c>. See <see cref="Si(Complex)"/>.
            /// </summary>
            public static Complex Chi(Complex z) => TrigonometricIntegral(z, hyperbolic: true, odd: false);

            private static Complex TrigonometricIntegral(Complex z, bool hyperbolic, bool odd)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (re, im) = (z.RealPart.EDecimal, z.ImaginaryPart.EDecimal);
                if (re.IsNaN() || im.IsNaN())
                    return Real.NaN;
                if (re.IsInfinity() || im.IsInfinity())
                {
                    if (!im.IsZero || !re.IsInfinity())
                        return Real.NaN;
                    var sign = re.IsNegative ? -1 : 1;
                    return (hyperbolic, odd) switch
                    {
                        (false, true) => Real.Create(EDecimal.PI(context).Divide(2 * sign, context)),
                        (false, false) => sign > 0 ? Integer.Zero : Complex.Create(EDecimal.Zero, EDecimal.PI(context)),
                        (true, true) => sign > 0 ? Real.PositiveInfinity : Real.NegativeInfinity,
                        (true, false) => sign > 0 ? Real.PositiveInfinity : Real.NaN,
                    };
                }
                if (re.IsZero && im.IsZero)
                    return odd ? Integer.Zero : Real.NegativeInfinity;
                var (valueRe, valueIm, reScale, imScale) = TrigonometricIntegral(re, im, hyperbolic, odd, context);
                // On the axes, what is known exactly is given so: Si and Shi are real on the real line
                // and imaginary on the imaginary axis; Ci and Chi are real on the positive real axis, i pi
                // more on the negative, and i pi/2 sgn(y) more than a real value on the imaginary axis.
                var pi = EDecimal.PI(context);
                if (im.IsZero)
                    valueIm = odd || !re.IsNegative ? EDecimal.Zero : pi;
                else if (re.IsZero)
                {
                    if (odd)
                        valueRe = EDecimal.Zero;
                    else
                        valueIm = im.IsNegative ? pi.Divide(-2, context) : pi.Divide(2, context);
                }
                // A part is a residual only against what was added to make it, not against 1: Si(1e-60) is 1e-60
                // (https://github.com/asc-community/AngouriMath/issues/2074), and the imaginary parts of
                // Si(1 + 10^(-60) i) and Chi(1 + 10^(-60) i) are about 10^(-60).
                // https://github.com/asc-community/AngouriMath/issues/1944
                return Complex.CreateAtScales(valueRe.RoundToPrecision(context), valueIm.RoundToPrecision(context), reScale, imScale);
            }

            /// <summary>
            /// One of the four at <c>re + i im</c>, before any rounding back, and for each part the scale it is a
            /// residual against at <paramref name="context"/>'s precision.
            /// </summary>
            private static (EDecimal Re, EDecimal Im, EDecimal ReScale, EDecimal ImScale) TrigonometricIntegral(EDecimal re, EDecimal im, bool hyperbolic, bool odd, EContext context)
            {
                var precision = context.Precision.ToInt32Checked();
                var modulus = Math.Sqrt(Math.Pow(re.ToDouble(), 2) + Math.Pow(im.ToDouble(), 2));
                if (modulus > (precision + 5) * Math.Log(10))
                    return ByTheExponentialIntegral(re, im, hyperbolic, odd, context);
                // The digits the terms cancel away from the axis the function grows along, and ten over.
                var growth = hyperbolic ? Math.Abs(re.ToDouble()) : Math.Abs(im.ToDouble());
                var work = Working(context, (int)Math.Ceiling((modulus - growth) / Math.Log(10)) + 10);
                var ((sumRe, sumIm), (scaleRe, scaleIm)) = TheSeries(re, im, alternating: !hyperbolic, odd, modulus, work);
                // The series is taken with the digits it cancels and more, so a part is a residual only against the
                // terms added to make it less those digits.
                var beyond = precision - work.Precision.ToInt32Checked();
                if (odd)
                    return (sumRe, sumIm, scaleRe.ScaleByPowerOfTen(beyond), scaleIm.ScaleByPowerOfTen(beyond));
                // gamma + ln z, with the principal logarithm.
                var logarithmRe = re.Multiply(re, work).Add(im.Multiply(im, work), work).Sqrt(work).Log(work);
                var logarithmIm = im.Arctan2(re, work);
                var gamma = EulerGamma(work);
                return (sumRe.Add(logarithmRe, work).Add(gamma, work), sumIm.Add(logarithmIm, work),
                    scaleRe.Add(logarithmRe.Abs(), work).Add(gamma, work).ScaleByPowerOfTen(beyond),
                    scaleIm.Add(logarithmIm.Abs(), work).ScaleByPowerOfTen(beyond));
            }

            /// <summary>
            /// <c>sum s^k z^(2k + 1)/((2k + 1) (2k + 1)!)</c> from <c>k = 0</c>, or
            /// <c>sum s^k z^(2k)/(2k (2k)!)</c> from <c>k = 1</c>, with <c>s = -1</c> where
            /// <paramref name="alternating"/>, summed until a term is below the working precision of
            /// the sum once the terms have begun to fall, and for each part the sum of the sizes of the terms.
            /// </summary>
            private static ((EDecimal Re, EDecimal Im) Sum, (EDecimal Re, EDecimal Im) Scale) TheSeries(EDecimal re, EDecimal im, bool alternating, bool odd, double modulus, EContext work)
            {
                var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                // z^n/n!, and the sum of the terms of the parity asked.
                var power = (Re: EDecimal.One, Im: EDecimal.Zero);
                var sum = (Re: EDecimal.Zero, Im: EDecimal.Zero);
                var scale = (Re: EDecimal.Zero, Im: EDecimal.Zero);
                for (var n = 1; ; n++)
                {
                    var divisor = EDecimal.FromInt32(n);
                    power = Multiply(power, (re, im), work);
                    power = (power.Re.Divide(divisor, work), power.Im.Divide(divisor, work));
                    if ((n % 2 == 1) != odd)
                        continue;
                    var negative = alternating && (n / 2) % 2 == 1;
                    var term = (Re: power.Re.Divide(divisor, work), Im: power.Im.Divide(divisor, work));
                    sum = negative
                        ? (sum.Re.Subtract(term.Re, work), sum.Im.Subtract(term.Im, work))
                        : (sum.Re.Add(term.Re, work), sum.Im.Add(term.Im, work));
                    scale = (scale.Re.Add(term.Re.Abs(), work), scale.Im.Add(term.Im.Abs(), work));
                    if (n > modulus && Magnitude(term).CompareTo(Magnitude(sum).Add(EDecimal.One, work).Multiply(epsilon, work)) < 0)
                        break;
                }
                return (sum, scale);
            }

            /// <summary>
            /// The four past the series' reach, from the exponential integral: see <see cref="Si(Complex)"/>.
            /// </summary>
            private static (EDecimal Re, EDecimal Im, EDecimal ReScale, EDecimal ImScale) ByTheExponentialIntegral(EDecimal re, EDecimal im, bool hyperbolic, bool odd, EContext context)
            {
                if (!hyperbolic)
                {
                    // Si(z) = -i Shi(i z), and Ci(z) = Chi(i z) + ln z - ln(i z): i z = -im + i re.
                    var (valueRe, valueIm, reScale, imScale) = ByTheExponentialIntegral(im.Negate(), re, hyperbolic: true, odd, context);
                    if (odd)
                        // -i (a + i b) = b - i a
                        return (valueIm, valueRe.Negate(), imScale, reScale);
                    // ln z - ln(i z) is -i pi/2 where arg z <= pi/2, and 3 i pi/2 past it.
                    var pi = EDecimal.PI(Working(context, 10));
                    var pastAQuarterTurn = re.IsNegative && !im.IsNegative;
                    var branchTerm = pastAQuarterTurn ? pi.Multiply(3).Divide(2) : pi.Divide(-2);
                    return (valueRe, valueIm.Add(branchTerm), reScale, imScale.Add(branchTerm.Abs()));
                }
                var work = Working(context, 10);
                var (plusRe, plusIm, plusReScale, plusImScale) = Ei(re, im, context, extraDigits: 10);
                var (minusRe, minusIm, minusReScale, minusImScale) = Ei(re.Negate(), im.Negate(), context, extraDigits: 10);
                var half = EDecimal.FromString("0.5");
                var (sumRe, sumIm) = odd
                    ? (plusRe.Subtract(minusRe, work).Multiply(half, work), plusIm.Subtract(minusIm, work).Multiply(half, work))
                    : (plusRe.Add(minusRe, work).Multiply(half, work), plusIm.Add(minusIm, work).Multiply(half, work));
                // Half of Ei(z) and Ei(-z) each, so a residual against half the scales of both.
                var sumReScale = plusReScale.Add(minusReScale, work).Multiply(half, work);
                var sumImScale = plusImScale.Add(minusImScale, work).Multiply(half, work);
                var halfPi = EDecimal.PI(work).Multiply(half, work);
                // The branch terms of (ln z - ln(1/z))/2 in Ei(z) and Ei(-z).
                if (!im.IsZero)
                {
                    var branch = im.IsNegative ? halfPi.Negate() : halfPi;
                    sumIm = odd ? sumIm.Subtract(branch, work) : sumIm.Add(branch, work);
                    sumImScale = sumImScale.Add(halfPi, work);
                }
                else if (!odd && re.IsNegative)
                {
                    sumIm = sumIm.Add(EDecimal.PI(work), work);
                    sumImScale = sumImScale.Add(EDecimal.PI(work), work);
                }
                return (sumRe, sumIm, sumReScale, sumImScale);
            }
        }
    }
}
