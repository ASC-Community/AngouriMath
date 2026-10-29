//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Concurrent;
using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
            /// <summary>
            /// The exponential integral, <c>Ei(z) = gamma + (ln z - ln(1/z))/2 + sum_(k >= 1) z^k/(k k!)</c>,
            /// at the working precision, for every complex <c>z</c>; <c>Ei(0)</c> is <c>-oo</c>.
            /// </summary>
            /// <remarks>
            /// <para>
            /// <c>(ln z - ln(1/z))/2</c> is <c>ln z</c> off the negative real axis and <c>ln |z|</c>
            /// on it, so <c>Ei</c> is real on the whole real line but 0, as SymPy's, mpmath's and
            /// Mathematica's is: on the positive axis it is the principal value of
            /// <c>int_-oo^x e^t/t dt</c>, on the negative axis <c>-E1(-x)</c>, and just above or below
            /// the negative axis it is <c>i pi</c> more or less.
            /// </para>
            /// <para>
            /// With <c>P</c> digits of working precision, up to <c>|z| = (P + 5) ln 10</c> it is summed
            /// as written. The terms peak near <c>k = |z|</c> at about <c>e^|z|/|z|</c>, while
            /// <c>Ei(z)</c> is about <c>e^Re(z)/|z|</c>, so the sum cancels <c>(|z| - Re z)/ln 10</c>
            /// digits, which are carried as guard digits: none on the positive real axis, and twice
            /// <c>|x|/ln 10</c> on the negative one.
            /// </para>
            /// <para>
            /// Beyond that it is <c>e^z/z sum_k k!/z^k</c> (Abramowitz and Stegun 5.1.51), cut at its
            /// smallest term, which is about <c>e^(-|z|)</c> of the sum and so below the precision
            /// there, and off the real axis the logarithm's branch adds <c>i pi sgn(Im z)</c>.
            /// </para>
            /// <para>
            /// The result is rounded to <see cref="MathS.Settings.DecimalPrecisionContext"/>, whose
            /// exponent range bounds what can be represented: <c>Ei(-500)</c>, about <c>1e-220</c>,
            /// can come out as zero there.
            /// </para>
            /// https://github.com/asc-community/AngouriMath/issues/1501
            /// </remarks>
            public static Complex Ei(Complex z)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (re, im) = (z.RealPart.EDecimal, z.ImaginaryPart.EDecimal);
                if (re.IsNaN() || im.IsNaN())
                    return Real.NaN;
                if (re.IsInfinity() || im.IsInfinity())
                    return im.IsZero && re.IsInfinity() ? (re.IsNegative ? Integer.Zero : Real.PositiveInfinity) : Real.NaN;
                if (re.IsZero && im.IsZero)
                    return Real.NegativeInfinity;
                var (eiRe, eiIm) = Ei(re, im, context, extraDigits: 0);
                return Complex.Create(eiRe.RoundToPrecision(context), eiIm.RoundToPrecision(context));
            }

            /// <summary>
            /// The logarithmic integral, <c>li(z) = Ei(ln z)</c>, with the principal logarithm, at the
            /// working precision, for every complex <c>z</c>: <c>li(0) = 0</c> and <c>li(1) = -oo</c>.
            /// </summary>
            /// <remarks>
            /// On <c>x > 0</c> it is the principal value of <c>int_0^x dt/ln t</c>, real, and on
            /// <c>0 &lt; x &lt; 1</c>, where <c>ln x</c> is negative, <c>Ei</c> of a negative number.
            /// The logarithm is taken with ten more digits, so that <c>Ei</c> is asked the argument to
            /// the precision it answers in.
            /// https://github.com/asc-community/AngouriMath/issues/1501
            /// </remarks>
            public static Complex Li(Complex z)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (re, im) = (z.RealPart.EDecimal, z.ImaginaryPart.EDecimal);
                if (re.IsNaN() || im.IsNaN())
                    return Real.NaN;
                if (re.IsInfinity() || im.IsInfinity())
                    return im.IsZero && re.IsPositiveInfinity() ? Real.PositiveInfinity : Real.NaN;
                if (re.IsZero && im.IsZero)
                    return Integer.Zero;
                if (im.IsZero && re.CompareTo(EDecimal.One) == 0)
                    return Real.NegativeInfinity;
                var work = Working(context, 10);
                var modulus = re.Multiply(re, work).Add(im.Multiply(im, work), work).Sqrt(work);
                var (lnRe, lnIm) = (modulus.Log(work), im.Arctan2(re, work));
                var (liRe, liIm) = Ei(lnRe, lnIm, context, extraDigits: 10);
                return Complex.Create(liRe.RoundToPrecision(context), liIm.RoundToPrecision(context));
            }

            /// <summary>
            /// <c>Ei(re + i im)</c> in a context of <paramref name="context"/>'s precision and
            /// <paramref name="extraDigits"/> more, before any rounding back.
            /// </summary>
            private static (EDecimal Re, EDecimal Im) Ei(EDecimal re, EDecimal im, EContext context, int extraDigits)
            {
                var precision = context.Precision.ToInt32Checked() + extraDigits;
                var modulus = Math.Sqrt(Math.Pow(re.ToDouble(), 2) + Math.Pow(im.ToDouble(), 2));
                if (modulus > (precision + 5) * Math.Log(10))
                    return EiByTheAsymptoticSeries(re, im, Working(context, extraDigits + 10));
                // The digits the terms cancel off the positive axis, and ten over.
                var cancelled = (int)Math.Ceiling((modulus - re.ToDouble()) / Math.Log(10));
                return EiByTheSeries(re, im, Working(context, extraDigits + cancelled + 10));
            }

            /// <summary>
            /// <c>gamma + (ln z - ln(1/z))/2 + sum_(k >= 1) z^k/(k k!)</c>, summed until a term is below
            /// the working precision of the sum once the terms have begun to fall.
            /// </summary>
            private static (EDecimal Re, EDecimal Im) EiByTheSeries(EDecimal re, EDecimal im, EContext work)
            {
                var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                var peak = EDecimal.FromDouble(Math.Sqrt(Math.Pow(re.ToDouble(), 2) + Math.Pow(im.ToDouble(), 2)));
                // z^k/k!, and the sum of those over k.
                var power = (Re: EDecimal.One, Im: EDecimal.Zero);
                var sum = (Re: EDecimal.Zero, Im: EDecimal.Zero);
                for (var k = 1; ; k++)
                {
                    power = Multiply(power, (re, im), work);
                    var divisor = EDecimal.FromInt32(k);
                    power = (power.Re.Divide(divisor, work), power.Im.Divide(divisor, work));
                    var term = (Re: power.Re.Divide(divisor, work), Im: power.Im.Divide(divisor, work));
                    sum = (sum.Re.Add(term.Re, work), sum.Im.Add(term.Im, work));
                    if (divisor.CompareTo(peak) > 0
                        && Magnitude(term).CompareTo(Magnitude(sum).Add(EDecimal.One, work).Multiply(epsilon, work)) < 0)
                        break;
                }
                // (ln z - ln(1/z))/2: ln |z| on the negative axis, where both logarithms add i pi.
                var modulus = re.Multiply(re, work).Add(im.Multiply(im, work), work).Sqrt(work);
                var logarithm = (Re: modulus.Log(work), Im: im.IsZero && re.IsNegative ? EDecimal.Zero : im.Arctan2(re, work));
                return (sum.Re.Add(logarithm.Re, work).Add(EulerGamma(work), work), sum.Im.Add(logarithm.Im, work));
            }

            /// <summary>
            /// <c>e^z/z sum_k k!/z^k + i pi sgn(Im z)</c>, cut at its smallest term.
            /// </summary>
            private static (EDecimal Re, EDecimal Im) EiByTheAsymptoticSeries(EDecimal re, EDecimal im, EContext work)
            {
                var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                // 1/z = (re - i im)/|z|^2
                var squaredModulus = re.Multiply(re, work).Add(im.Multiply(im, work), work);
                var reciprocal = (Re: re.Divide(squaredModulus, work), Im: im.Negate().Divide(squaredModulus, work));
                var term = (Re: EDecimal.One, Im: EDecimal.Zero);
                var sum = term;
                var previous = Magnitude(term);
                for (var k = 1; ; k++)
                {
                    var next = Multiply(term, reciprocal, work);
                    next = (next.Re.Multiply(k, work), next.Im.Multiply(k, work));
                    var size = Magnitude(next);
                    // Past the smallest term the series diverges; below the precision it is done.
                    if (size.CompareTo(previous) >= 0 || size.CompareTo(epsilon) < 0)
                        break;
                    sum = (sum.Re.Add(next.Re, work), sum.Im.Add(next.Im, work));
                    (term, previous) = (next, size);
                }
                // e^z/z = e^re (cos im + i sin im) (re - i im)/|z|^2
                var scale = re.Exponential(work);
                var exponential = (scale.Multiply(im.Cos(work), work), scale.Multiply(im.Sin(work), work));
                var (valueRe, valueIm) = Multiply(Multiply(exponential, reciprocal, work), sum, work);
                if (!im.IsZero)
                    valueIm = valueIm.Add(im.IsNegative ? EDecimal.PI(work).Negate() : EDecimal.PI(work), work);
                return (valueRe, valueIm);
            }

            /// <summary>
            /// Euler's constant, <c>gamma = lim (1 + 1/2 + ... + 1/n - ln n)</c>, to the precision of
            /// <paramref name="work"/>, by Brent and McMillan's algorithm B1: with
            /// <c>B_k = (n^k/k!)^2</c> and <c>A_k = B_k (H_k - ln n)</c>, <c>gamma</c> is
            /// <c>sum A_k/sum B_k</c> to within <c>pi e^(-4n)</c>, so <c>n</c> is a quarter of the
            /// digits' <c>ln 10</c>. The terms stay positive past <c>k = n</c>, where they are largest,
            /// and ten guard digits cover what the first ones cancel.
            /// </summary>
            internal static EDecimal EulerGamma(EContext work)
            {
                var digits = work.Precision.ToInt32Checked();
                return eulerGamma.GetOrAdd(digits, static digits =>
                {
                    var inner = EContext.ForPrecision(digits + 10 + (int)Math.Ceiling(Math.Log10(digits + 10)))
                        .WithRounding(ERounding.HalfEven).WithUnlimitedExponents();
                    var n = (int)Math.Ceiling(digits * Math.Log(10) / 4) + 1;
                    var nSquared = EDecimal.FromInt32(n).Multiply(EDecimal.FromInt32(n));
                    var a = EDecimal.FromInt32(n).Log(inner).Negate();
                    var b = EDecimal.One;
                    var (u, v) = (a, b);
                    var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-inner.Precision.ToInt32Checked()));
                    for (var k = 1; ; k++)
                    {
                        var kk = EDecimal.FromInt32(k);
                        b = b.Multiply(nSquared, inner).Divide(kk.Multiply(kk), inner);
                        a = a.Multiply(nSquared, inner).Divide(kk, inner).Add(b, inner).Divide(kk, inner);
                        u = u.Add(a, inner);
                        v = v.Add(b, inner);
                        if (k > n && b.CompareTo(v.Multiply(epsilon, inner)) < 0 && a.Abs().CompareTo(u.Abs().Multiply(epsilon, inner)) < 0)
                            break;
                    }
                    return u.Divide(v, inner).RoundToPrecision(EContext.ForPrecision(digits).WithRounding(ERounding.HalfEven).WithUnlimitedExponents());
                });
            }
            [ConcurrentField] private static readonly ConcurrentDictionary<int, EDecimal> eulerGamma = new();
        }
    }
}
