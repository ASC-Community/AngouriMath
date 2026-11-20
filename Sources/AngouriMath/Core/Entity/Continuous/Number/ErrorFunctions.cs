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
            /// The error function, <c>erf(z) = 2/sqrt(pi) int_0^z e^(-t^2) dt</c>, at the working
            /// precision, for every complex <c>z</c>.
            /// </summary>
            /// <remarks>
            /// <para>
            /// With <c>P</c> digits of working precision, up to <c>|z|^2 = P ln 10</c> it is summed as
            /// <c>erf(z) = 2/sqrt(pi) e^(-z^2) sum 2^n z^(2n + 1)/(1 3 5 ... (2n + 1))</c>
            /// (Abramowitz and Stegun 7.1.6). The terms are all positive on the real line, so a
            /// real argument loses nothing to cancellation. Off it they turn, and the sum loses up
            /// to <c>2 Im(z)^2/ln 10</c> digits, which are carried as guard digits.
            /// </para>
            /// <para>
            /// Beyond that it is <c>1 - erfc(z)</c>, with <c>erfc(z)</c> from its asymptotic series
            /// <c>e^(-z^2)/(z sqrt(pi)) sum (-1)^n (2n - 1)!!/(2 z^2)^n</c> cut at its smallest
            /// term. That term is below <c>e^(-|z|^2)</c>, and so below the precision there. An
            /// argument in the left half-plane is folded over by <c>erf(-z) = -erf(z)</c> first,
            /// since the asymptotic series holds for <c>|arg z| &lt; 3 pi/4</c>.
            /// </para>
            /// <para>
            /// The result is rounded to <see cref="MathS.Settings.DecimalPrecisionContext"/>, whose
            /// exponent range bounds what can be represented: <c>erfc(30)</c> is about
            /// <c>2.6e-393</c> and comes out as zero there, and <c>erfi(50)</c>, about
            /// <c>e^2500</c>, as infinity. The working contexts have no such bound.
            /// </para>
            /// https://github.com/asc-community/AngouriMath/issues/1501
            /// </remarks>
            public static Complex Erf(Complex z)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (re, im) = (z.RealPart.EDecimal, z.ImaginaryPart.EDecimal);
                if (re.IsNaN() || im.IsNaN())
                    return Real.NaN;
                if (re.IsInfinity() || im.IsInfinity())
                    return im.IsZero && re.IsInfinity() ? (re.IsNegative ? Integer.MinusOne : Integer.One) : Real.NaN;
                if (re.IsZero && im.IsZero)
                    return Integer.Zero;
                var (erfRe, erfIm) = Erf(re, im, context, extraDigits: 0);
                // Its residual is its argument's: erf(1e-60) is 1.13e-60 and no residual of anything.
                // https://github.com/asc-community/AngouriMath/issues/2074
                var scale = im.IsZero ? re : Magnitude((re, im));
                return Complex.CreateAtScales(erfRe.RoundToPrecision(context), erfIm.RoundToPrecision(context), scale, scale);
            }

            /// <summary>
            /// The complementary error function, <c>erfc(z) = 1 - erf(z)</c>, at the working
            /// precision, for every complex <c>z</c>.
            /// </summary>
            /// <remarks>
            /// Computed on its own in the right half-plane rather than as <c>1 - erf(z)</c> at the
            /// working precision. There <c>erf(z)</c> approaches 1, and the difference would keep
            /// only the digits of <c>erf(z)</c> past its leading nines: <c>erfc(10)</c> is about
            /// <c>2.1e-45</c>. Where <c>|z|^2</c> is past the series' reach it is the asymptotic
            /// series itself. Short of that, <c>erf(z)</c> is carried with <c>Re(z^2)/ln 10</c> more
            /// digits, which is how many of its digits the subtraction cancels.
            /// </remarks>
            public static Complex Erfc(Complex z)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (re, im) = (z.RealPart.EDecimal, z.ImaginaryPart.EDecimal);
                if (re.IsNaN() || im.IsNaN())
                    return Real.NaN;
                if (re.IsInfinity() || im.IsInfinity())
                    return im.IsZero && re.IsInfinity() ? (re.IsNegative ? Integer.Create(2) : Integer.Zero) : Real.NaN;
                if (re.IsZero && im.IsZero)
                    return Integer.One;
                var precision = context.Precision.ToInt32Checked();
                (EDecimal Re, EDecimal Im) erfc;
                // What is left of a cancellation is 0, below the working digits of what cancelled: nothing in the
                // asymptotic series, 1 less erf(z) taken with the digits that loses on the right, 1 on the left.
                // At 1 throughout, erfc(12), 1.36e-64, was 0.
                EDecimal scale;
                // The asymptotic series is erfc itself and cancels nothing, so it does without the
                // digits 1 - erf(z) would lose. Those grow as Re(z^2): 4343 of them at z = 100,
                // where erfc(100) took over 20 s. https://github.com/asc-community/AngouriMath/issues/1606
                // On the imaginary axis it is 1 - erf(z) instead, where erf(i y) is i erfi(y) exactly:
                // the asymptotic series leaves out the 1 there, below its precision beside erfi(y),
                // and answered erfc(20 i) with a real part of 0.
                if (!re.IsNegative && !re.IsZero && SquaredModulus(re, im).CompareTo(Reach(precision)) > 0)
                {
                    erfc = ErfcByTheAsymptoticSeries(re, im, Working(context, GuardDigits(re, im, precision)));
                    scale = EDecimal.Zero;
                }
                else
                {
                    var cancelled = re.IsNegative ? 0 : Cancelled(re, im);
                    scale = EDecimal.Create(EInteger.One, EInteger.FromInt32(-cancelled));
                    var work = Working(context, GuardDigits(re, im, precision) + cancelled);
                    var (erfRe, erfIm) = Erf(re, im, work, extraDigits: 0);
                    erfc = (EDecimal.One.Subtract(erfRe, work), erfIm.Negate());
                }
                return Complex.CreateAtScales(erfc.Re.RoundToPrecision(context), erfc.Im.RoundToPrecision(context), scale, scale);
            }

            /// <summary>
            /// The imaginary error function, <c>erfi(z) = -i erf(i z)</c>, at the working
            /// precision, for every complex <c>z</c>.
            /// </summary>
            /// <remarks>
            /// Computed through <see cref="Erf(Complex)"/> exactly as it is defined, so the two can
            /// never disagree. On the real line <c>erfi(x)</c> is <c>2/sqrt(pi) int_0^x e^(t^2) dt</c>,
            /// and <c>erf(i x)</c> is summed off the real line with the guard digits that costs.
            /// </remarks>
            public static Complex Erfi(Complex z)
            {
                // On the real line, where it is real, it is summed as a real function, so that its
                // imaginary part is zero and not a rounding residue; it grows without bound.
                if (z.ImaginaryPart.EDecimal.IsZero && z.RealPart.EDecimal is var x && !x.IsNaN())
                {
                    if (x.IsInfinity())
                        return x.IsNegative ? Real.NegativeInfinity : Real.PositiveInfinity;
                    var context = MathS.Settings.DecimalPrecisionContext.Value;
                    return Real.CreateAtScale(ErfiOfAReal(x, context).RoundToPrecision(context), x);
                }
                // i z and -i erf(i z) only exchange the parts, which were decided already: made again as residuals,
                // erfi(1e-60 i) was 0.
                var erf = Erf(Complex.CreateAtScales(z.ImaginaryPart.EDecimal.Negate(), z.RealPart.EDecimal, EDecimal.Zero, EDecimal.Zero));
                // -i (a + i b) = b - i a
                return Complex.CreateAtScales(erf.ImaginaryPart.EDecimal, erf.RealPart.EDecimal.Negate(), EDecimal.Zero, EDecimal.Zero);
            }

            /// <summary>
            /// <c>erf(re + i im)</c> in a context of <paramref name="context"/>'s precision and
            /// <paramref name="extraDigits"/> more, before any rounding back.
            /// </summary>
            private static (EDecimal Re, EDecimal Im) Erf(EDecimal re, EDecimal im, EContext context, int extraDigits)
            {
                // erf(-z) = -erf(z), so the asymptotic series is only ever asked in the right half-plane.
                if (re.IsNegative)
                {
                    var (negRe, negIm) = Erf(re.Negate(), im.Negate(), context, extraDigits);
                    return (negRe.Negate(), negIm.Negate());
                }
                // On the imaginary axis erf(i y) = i erfi(y) exactly, and its real part is zero. The
                // asymptotic series of erfc cannot say so there: the 1 it leaves in 1 - erfc(z) is
                // below its precision beside erfi(y), and would be read as a real part.
                if (re.IsZero)
                    return (EDecimal.Zero, ErfiOfAReal(im, context.WithPrecision(context.Precision.ToInt32Checked() + extraDigits)));
                var precision = context.Precision.ToInt32Checked() + extraDigits;
                var work = Working(context, extraDigits + GuardDigits(re, im, precision));
                if (SquaredModulus(re, im).CompareTo(Reach(precision)) <= 0)
                    return ErfByTheSeries(re, im, work);
                var (erfcRe, erfcIm) = ErfcByTheAsymptoticSeries(re, im, work);
                return (EDecimal.One.Subtract(erfcRe, work), erfcIm.Negate());
            }

            /// <summary>
            /// <c>2/sqrt(pi) e^(-z^2) sum 2^n z^(2n + 1)/(1 3 5 ... (2n + 1))</c>, summed until a
            /// term is below the working precision of the sum once the terms have begun to fall.
            /// </summary>
            private static (EDecimal Re, EDecimal Im) ErfByTheSeries(EDecimal re, EDecimal im, EContext work)
            {
                // 2 z^2 = 2 (re^2 - im^2) + 4 re im i
                var twoZSquared = (re.Multiply(re, work).Subtract(im.Multiply(im, work), work).Multiply(2, work),
                                   re.Multiply(im, work).Multiply(4, work));
                var peak = Magnitude(twoZSquared);
                var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                var term = (Re: re, Im: im);
                var sum = term;
                for (var n = 1; ; n++)
                {
                    term = Multiply(term, twoZSquared, work);
                    var divisor = EDecimal.FromInt32(2 * n + 1);
                    term = (term.Re.Divide(divisor, work), term.Im.Divide(divisor, work));
                    sum = (sum.Re.Add(term.Re, work), sum.Im.Add(term.Im, work));
                    if (EDecimal.FromInt32(n).CompareTo(peak) > 0
                        && Magnitude(term).CompareTo(Magnitude(sum).Multiply(epsilon, work)) < 0)
                        break;
                }
                // e^(-z^2) = e^(im^2 - re^2) (cos(2 re im) - i sin(2 re im))
                var scale = im.Multiply(im, work).Subtract(re.Multiply(re, work), work).Exponential(work)
                    .Multiply(TwoOverSqrtPi(work), work);
                var angle = re.Multiply(im, work).Multiply(2, work);
                var factor = (scale.Multiply(angle.Cos(work), work), scale.Multiply(angle.Sin(work), work).Negate());
                return Multiply(sum, factor, work);
            }

            /// <summary>
            /// <c>erfc(z) ~ e^(-z^2)/(z sqrt(pi)) sum (-1)^n (2n - 1)!!/(2 z^2)^n</c> for
            /// <c>Re z &gt;= 0</c>, cut where its terms stop falling or fall below the precision.
            /// </summary>
            private static (EDecimal Re, EDecimal Im) ErfcByTheAsymptoticSeries(EDecimal re, EDecimal im, EContext work)
            {
                // 1/(2 z^2), as the conjugate of 2 z^2 over its squared modulus.
                var twoZSquared = (re.Multiply(re, work).Subtract(im.Multiply(im, work), work).Multiply(2, work),
                                   re.Multiply(im, work).Multiply(4, work));
                var norm = twoZSquared.Item1.Multiply(twoZSquared.Item1, work).Add(twoZSquared.Item2.Multiply(twoZSquared.Item2, work), work);
                var inverse = (twoZSquared.Item1.Divide(norm, work), twoZSquared.Item2.Negate().Divide(norm, work));
                var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                var term = (Re: EDecimal.One, Im: EDecimal.Zero);
                var sum = term;
                var previous = Magnitude(term);
                for (var n = 1; ; n++)
                {
                    var next = Multiply(term, inverse, work);
                    var factor = EDecimal.FromInt32(-(2 * n - 1));
                    next = (next.Re.Multiply(factor, work), next.Im.Multiply(factor, work));
                    var size = Magnitude(next);
                    if (size.CompareTo(previous) >= 0)
                        break;
                    sum = (sum.Re.Add(next.Re, work), sum.Im.Add(next.Im, work));
                    if (size.CompareTo(Magnitude(sum).Multiply(epsilon, work)) < 0)
                        break;
                    (term, previous) = (next, size);
                }
                // e^(-z^2)/(z sqrt(pi)): e^(-z^2) as in the series, over z = re + i im.
                var scale = im.Multiply(im, work).Subtract(re.Multiply(re, work), work).Exponential(work)
                    .Multiply(TwoOverSqrtPi(work), work).Divide(2, work);
                var angle = re.Multiply(im, work).Multiply(2, work);
                var exponential = (scale.Multiply(angle.Cos(work), work), scale.Multiply(angle.Sin(work), work).Negate());
                var modulus = re.Multiply(re, work).Add(im.Multiply(im, work), work);
                var overZ = (re.Divide(modulus, work), im.Negate().Divide(modulus, work));
                return Multiply(Multiply(sum, exponential, work), overZ, work);
            }

            /// <summary>
            /// <c>erfi(y)</c> for a real <c>y</c>: <c>2/sqrt(pi) sum y^(2n + 1)/(n! (2n + 1))</c> up to
            /// the series' reach, every term of which is positive, and past it the asymptotic series
            /// <c>e^(y^2)/(y sqrt(pi)) sum (2n - 1)!!/(2 y^2)^n</c>, positive too, cut at its smallest
            /// term.
            /// </summary>
            private static EDecimal ErfiOfAReal(EDecimal y, EContext context)
            {
                if (y.IsZero)
                    return EDecimal.Zero;
                if (y.IsNegative)
                    return ErfiOfAReal(y.Negate(), context).Negate();
                var work = Working(context, 10);
                var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                var ySquared = y.Multiply(y, work);
                if (SquaredModulus(y, EDecimal.Zero).CompareTo(Reach(context.Precision.ToInt32Checked())) <= 0)
                {
                    // y^(2n + 1)/n!, and the term that divides it by 2n + 1.
                    var power = y;
                    var sum = y;
                    for (var n = 1; ; n++)
                    {
                        power = power.Multiply(ySquared, work).Divide(EDecimal.FromInt32(n), work);
                        var term = power.Divide(EDecimal.FromInt32(2 * n + 1), work);
                        sum = sum.Add(term, work);
                        if (EDecimal.FromInt32(n).CompareTo(ySquared) > 0 && term.CompareTo(sum.Multiply(epsilon, work)) < 0)
                            break;
                    }
                    return sum.Multiply(TwoOverSqrtPi(work), work);
                }
                var twoYSquared = ySquared.Multiply(2, work);
                var current = EDecimal.One;
                var total = EDecimal.One;
                for (var n = 1; ; n++)
                {
                    var next = current.Multiply(EDecimal.FromInt32(2 * n - 1), work).Divide(twoYSquared, work);
                    if (next.CompareTo(current) >= 0)
                        break;
                    total = total.Add(next, work);
                    if (next.CompareTo(total.Multiply(epsilon, work)) < 0)
                        break;
                    current = next;
                }
                return ySquared.Exponential(work).Multiply(total, work).Multiply(TwoOverSqrtPi(work), work).Divide(y.Multiply(2, work), work);
            }

            /// <summary>
            /// How far the series is taken, as <c>|z|^2</c>: past it the asymptotic series' smallest
            /// term, about <c>e^(-|z|^2)</c>, is below <paramref name="precision"/> digits.
            /// </summary>
            private static EDecimal Reach(int precision) => EDecimal.FromDouble((precision + 5) * 2.302585093);

            /// <summary>The digits the series' terms cancel off the real line, about <c>2 Im(z)^2/ln 10</c>, and ten over.</summary>
            private static int GuardDigits(EDecimal re, EDecimal im, int precision)
            {
                var imaginary = Math.Abs(im.ToDouble());
                var digits = Math.Min(2 * imaginary * imaginary, (precision + 5) * 2.302585093 * 2) / 2.302585093;
                return (int)Math.Ceiling(digits) + 10;
            }

            /// <summary>The digits <c>1 - erf(z)</c> cancels in the right half-plane, about <c>Re(z^2)/ln 10</c>.</summary>
            private static int Cancelled(EDecimal re, EDecimal im)
            {
                var (x, y) = (re.ToDouble(), im.ToDouble());
                return (int)Math.Ceiling(Math.Max(0, Math.Min(x * x - y * y, 1e5)) / 2.302585093);
            }

            private static EDecimal SquaredModulus(EDecimal re, EDecimal im)
                => re.Multiply(re, EContext.Binary64).Add(im.Multiply(im, EContext.Binary64), EContext.Binary64);

            /// <summary>
            /// <paramref name="context"/> with <paramref name="guard"/> more digits, rounded up to a
            /// multiple of 16 so that the contexts made, and the constants the library caches per
            /// context, stay few, and with no bound on the exponent.
            /// </summary>
            private static EContext Working(EContext context, int guard)
            {
                var digits = context.Precision.ToInt32Checked() + ((guard + 15) / 16) * 16;
                return workingContexts.GetOrAdd(digits, static digits => EContext.ForPrecision(digits).WithRounding(ERounding.HalfEven).WithUnlimitedExponents());
            }
            [ConcurrentField] private static readonly ConcurrentDictionary<int, EContext> workingContexts = new();

            private static EDecimal TwoOverSqrtPi(EContext work)
                => twoOverSqrtPi.GetOrAdd(work.Precision.ToInt32Checked(), _ => EDecimal.FromInt32(2).Divide(EDecimal.PI(work).Sqrt(work), work));
            [ConcurrentField] private static readonly ConcurrentDictionary<int, EDecimal> twoOverSqrtPi = new();

            private static (EDecimal Re, EDecimal Im) Multiply((EDecimal Re, EDecimal Im) a, (EDecimal Re, EDecimal Im) b, EContext work)
                => (a.Re.Multiply(b.Re, work).Subtract(a.Im.Multiply(b.Im, work), work),
                    a.Re.Multiply(b.Im, work).Add(a.Im.Multiply(b.Re, work), work));

            /// <summary>The larger of the two parts' moduli, within a factor of the modulus and all a stopping test needs.</summary>
            private static EDecimal Magnitude((EDecimal Re, EDecimal Im) value)
                => value.Re.Abs().CompareTo(value.Im.Abs()) >= 0 ? value.Re.Abs() : value.Im.Abs();
        }
    }
}
