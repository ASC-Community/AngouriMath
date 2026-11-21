//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
            /// <summary>
            /// The Hurwitz zeta function <c>zeta(s, a)</c> at the working precision, <c>NaN</c> at its pole <c>s = 1</c>
            /// and where <c>a</c> is <c>0, -1, -2, ...</c>, or <see langword="null"/> where it is not computed: an
            /// infinite argument, or an <c>s</c> so far out that the sum would take too long.
            /// </summary>
            /// <remarks>
            /// <para>
            /// By the Euler–Maclaurin formula (DLMF 25.11.5 summed to <c>N</c> and 2.10.1 for the rest),
            /// <c>zeta(s, a) = sum_(k &lt; N) (a + k)^(-s) + w^(1 - s)/(s - 1) + w^(-s)/2
            /// + sum_(j >= 1) B_(2j)/(2j)! s (s + 1) ... (s + 2j - 2) w^(-s - 2j + 1)</c> with <c>w = a + N</c>, which is
            /// the analytic continuation for every <c>s</c> but 1 and every <c>a</c> but the poles, powers being
            /// principal. <c>N</c> is taken with <c>w</c> at least <c>0.4 W + |s|/2</c> for <c>W</c> working digits: the
            /// terms of the last sum fall until <c>2j</c> is about <c>2 pi w - |s|</c>, the smallest about
            /// <c>e^(-(2 pi w - |s|))</c> of the first, which is below <c>10^(-W)</c>.
            /// </para>
            /// <para>
            /// Where the terms are much larger than the value, as they are for <c>Re s</c> below 0 or near a zero, the
            /// digits lost to cancellation are counted and the sum is taken again with as many more. The Riemann zeta
            /// function left of 0 is instead reflected (DLMF 25.4.1),
            /// <c>zeta(s) = 2^s pi^(s - 1) sin(pi s/2) Gamma(1 - s) zeta(1 - s)</c>, which loses nothing.
            /// </para>
            /// https://github.com/asc-community/AngouriMath/issues/2182
            /// </remarks>
            internal static Complex? HurwitzZeta(Complex s, Complex a)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (sr, si) = (s.RealPart.EDecimal, s.ImaginaryPart.EDecimal);
                var (ar, ai) = (a.RealPart.EDecimal, a.ImaginaryPart.EDecimal);
                if (sr.IsNaN() || si.IsNaN() || ar.IsNaN() || ai.IsNaN())
                    return Real.NaN;
                if (!sr.IsFinite || !si.IsFinite || !ar.IsFinite || !ai.IsFinite)
                    return null;
                if (si.IsZero && sr.CompareTo(EDecimal.One) == 0)
                    return Real.NaN;
                if (ai.IsZero && ar.Sign <= 0 && ar.IsInteger())
                    return Real.NaN;
                var digits = context.Precision.ToInt32Checked();
                var riemann = ai.IsZero && ar.CompareTo(EDecimal.One) == 0;
                ((EDecimal Re, EDecimal Im) Value, EDecimal Scale)? value;
                if (riemann && sr.Sign < 0)
                    value = ReflectedRiemannZeta((sr, si), context, digits);
                else
                    value = ZetaBySummation((sr, si), (ar, ai), context, digits);
                if (value is not var ((re, im), scale))
                    return null;
                // A part is a residual only against what the value is known to, not against 1: zeta(-2 + 10^(-60) i)
                // is about -3 * 10^(-62) i. https://github.com/asc-community/AngouriMath/issues/1944
                return Complex.CreateAtScales(re.RoundToPrecision(context), im.RoundToPrecision(context), scale, scale);
            }

            /// <summary>
            /// <c>zeta(s, a)</c> by the Euler–Maclaurin formula, taken again with more digits while the cancellation
            /// in it leaves fewer than <paramref name="digits"/>, and the scale a part of it is a residual against: its
            /// largest term, less the digits it was taken with beyond <paramref name="digits"/>.
            /// </summary>
            private static ((EDecimal Re, EDecimal Im) Value, EDecimal Scale)? ZetaBySummation((EDecimal Re, EDecimal Im) s, (EDecimal Re, EDecimal Im) a,
                EContext context, int digits)
            {
                var modulus = Math.Sqrt(s.Re.ToDouble() * s.Re.ToDouble() + s.Im.ToDouble() * s.Im.ToDouble());
                // The terms of the last sum fall until 2j is about 2 pi w - |s|, the smallest being about e^(-(2 pi w - |s|))
                // of the first, so w = 0.4 W + |s|/2 for W working digits leaves it below 10^(-W).
                int Terms(int workingDigits) => (int)Math.Max(0, Math.Ceiling(0.4 * workingDigits + modulus / 2 + 5 - a.Re.ToDouble()));
                if (Terms(digits) > 20000)
                    return null;
                // The largest term is about (a + N)^(1 - Re s), where Re s < 1, and that many digits cancel.
                var expectedLoss = s.Re.ToDouble() < 1 ? (1 - s.Re.ToDouble()) * Math.Log10(Terms(digits) + Math.Abs(a.Re.ToDouble()) + 1) : 0;
                if (expectedLoss > 2000)
                    return null;
                var guard = 20 + (int)(2 * Math.Log10(2 + modulus)) + (int)expectedLoss;
                (EDecimal Re, EDecimal Im) value = (EDecimal.Zero, EDecimal.Zero);
                var scale = EDecimal.Zero;
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    var work = Working(context, guard);
                    var (sum, lost, largest) = EulerMaclaurinZeta(s, a, Terms(work.Precision.ToInt32Checked()), work);
                    value = sum;
                    scale = largest.ScaleByPowerOfTen(digits - work.Precision.ToInt32Checked());
                    if (lost <= guard - 10 || guard > 4 * digits + 2000)
                        break;
                    guard = lost + 20;
                }
                return (value, scale);
            }

            private static ((EDecimal Re, EDecimal Im) Value, int Lost, EDecimal Largest) EulerMaclaurinZeta(
                (EDecimal Re, EDecimal Im) s, (EDecimal Re, EDecimal Im) a, int terms, EContext work)
            {
                (EDecimal Re, EDecimal Im) minusS = (s.Re.Negate(), s.Im.Negate());
                // A whole s, as zeta(3) is evaluated, raises to its power by multiplying, without a logarithm.
                int? whole = s.Im.IsZero && s.Re.IsInteger() && s.Re.Abs().CompareTo(EDecimal.FromInt32(1000)) <= 0 ? s.Re.ToInt32Checked() : null;
                (EDecimal Re, EDecimal Im) ToTheMinusS((EDecimal Re, EDecimal Im) x)
                    => whole is not { } n ? ComplexPower(x, minusS, work)
                        : n >= 0 ? Reciprocal(Power(x, n, work), work)
                        : Power(x, -n, work);
                var largest = EDecimal.Zero;
                var sum = (Re: EDecimal.Zero, Im: EDecimal.Zero);
                for (var k = 0; k < terms; k++)
                {
                    var term = ToTheMinusS((a.Re.Add(EDecimal.FromInt32(k), work), a.Im));
                    Note(term);
                    sum = (sum.Re.Add(term.Re, work), sum.Im.Add(term.Im, work));
                }
                var w = (Re: a.Re.Add(EDecimal.FromInt32(terms), work), Im: a.Im);
                var wToMinusS = ToTheMinusS(w);
                // w^(1 - s)/(s - 1) + w^(-s)/2
                var integral = Divide(Multiply(w, wToMinusS, work), (s.Re.Subtract(EDecimal.One, work), s.Im), work);
                (EDecimal Re, EDecimal Im) half = (wToMinusS.Re.Divide(2, work), wToMinusS.Im.Divide(2, work));
                Note(integral);
                Note(half);
                sum = (sum.Re.Add(integral.Re, work).Add(half.Re, work), sum.Im.Add(integral.Im, work).Add(half.Im, work));

                // B_(2j)/(2j)! (s)_(2j - 1) w^(-s - 2j + 1), the rising product over powers of w carried along.
                var inverseW = Reciprocal(w, work);
                var inverseWSquared = Multiply(inverseW, inverseW, work);
                var factor = Multiply(Multiply(s, wToMinusS, work), inverseW, work);
                var tolerance = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                var coefficients = BernoulliOverFactorial(work);
                var previous = (EDecimal?)null;
                for (var j = 1; j <= 4 * work.Precision.ToInt32Checked() + 16; j++)
                {
                    if (factor.Re.IsZero && factor.Im.IsZero)
                        break;
                    var coefficient = coefficients(j);
                    var term = (Re: factor.Re.Multiply(coefficient, work), Im: factor.Im.Multiply(coefficient, work));
                    var size = Magnitude(term);
                    sum = (sum.Re.Add(term.Re, work), sum.Im.Add(term.Im, work));
                    if (size.CompareTo(tolerance.Multiply(largest, work)) <= 0 || previous is { } before && size.CompareTo(before) > 0)
                        break;
                    previous = size;
                    // (s + 2j - 1)(s + 2j)/w^2
                    var next = Multiply((s.Re.Add(EDecimal.FromInt32(2 * j - 1), work), s.Im), (s.Re.Add(EDecimal.FromInt32(2 * j), work), s.Im), work);
                    factor = Multiply(factor, Multiply(next, inverseWSquared, work), work);
                }
                var result = Magnitude(sum);
                var lost = largest.IsZero ? 0
                    : result.IsZero ? int.MaxValue / 2
                    : Math.Max(0, DecimalOrder(largest) - DecimalOrder(result));
                return (sum, lost, largest);

                void Note((EDecimal Re, EDecimal Im) part)
                {
                    var size = Magnitude(part);
                    if (size.CompareTo(largest) > 0)
                        largest = size;
                }
            }

            /// <summary><c>B_(2j)/(2j)!</c> at <paramref name="work"/>'s digits, remembered for each precision.</summary>
            private static Func<int, EDecimal> BernoulliOverFactorial(EContext work)
            {
                var known = bernoulliOverFactorial.GetOrAdd(work.Precision.ToInt32Checked(), static _ => new List<EDecimal>());
                return j =>
                {
                    lock (known)
                    {
                        while (known.Count < j)
                        {
                            var k = known.Count + 1;
                            var bernoulli = Bernoulli(k);
                            known.Add(EDecimal.FromEInteger(bernoulli.Numerator)
                                .Divide(EDecimal.FromEInteger(bernoulli.Denominator.Multiply(Factorial(2 * k))), work));
                        }
                        return known[j - 1];
                    }
                };
            }
            [ConcurrentField] private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, List<EDecimal>> bernoulliOverFactorial = new();

            /// <summary>
            /// <c>zeta(s) = 2^s pi^(s - 1) sin(pi s/2) Gamma(1 - s) zeta(1 - s)</c>, for <c>Re s &lt; 0</c>, a product
            /// that cancels nothing, and so a residual only against its own size less the digits it was taken with beyond
            /// <paramref name="digits"/>.
            /// </summary>
            private static ((EDecimal Re, EDecimal Im) Value, EDecimal Scale)? ReflectedRiemannZeta((EDecimal Re, EDecimal Im) s, EContext context, int digits)
            {
                var guard = 20 + (int)(2 * Math.Log10(2 + Math.Abs(s.Re.ToDouble()) + Math.Abs(s.Im.ToDouble())));
                var work = Working(context, guard);
                var reflected = (Re: EDecimal.One.Subtract(s.Re, work), Im: s.Im.Negate());
                if (ZetaBySummation(reflected, (EDecimal.One, EDecimal.Zero), work, work.Precision.ToInt32Checked()) is not var (zeta, _))
                    return null;
                var pi = InternalAMExtensions.ConstantCache.Lookup(work).Pi;
                var twoToTheS = ComplexExp(Multiply(s, (EDecimal.FromInt32(2).Log(work), EDecimal.Zero), work), work);
                var piToTheSLessOne = ComplexExp(Multiply((s.Re.Subtract(EDecimal.One, work), s.Im), (pi.Log(work), EDecimal.Zero), work), work);
                // sin(x + i y) = sin x cosh y + i cos x sinh y, with x = pi Re s/2 taken exactly at the whole multiples.
                var (cos, sin) = CosAndSinOfPiTimes(s.Re.Divide(2, work), work);
                (EDecimal Re, EDecimal Im) sine;
                if (s.Im.IsZero)
                    sine = (sin, EDecimal.Zero);
                else
                {
                    // sinh with its own digits: e^y - e^(-y) leaves only half of them at y = 10^(-60).
                    var (sinh, cosh) = SinhAndCosh(pi.Multiply(s.Im, work).Divide(2, work), work);
                    sine = (sin.Multiply(cosh, work), cos.Multiply(sinh, work));
                }
                var gamma = GammaOf(reflected, work);
                var value = Multiply(Multiply(Multiply(twoToTheS, piToTheSLessOne, work), Multiply(sine, gamma, work), work), zeta, work);
                return (value, Magnitude(value).ScaleByPowerOfTen(digits - work.Precision.ToInt32Checked()));
            }

            /// <summary>
            /// <c>zeta(s, a)</c> exactly at a whole <c>s</c> and a rational <c>a</c>, or <see langword="null"/>: <c>NaN</c>
            /// at the pole <c>s = 1</c> and where <c>a</c> is <c>0, -1, -2, ...</c>; the rational <c>-B_(n + 1)(a)/(n + 1)</c>
            /// at <c>s = -n</c> (DLMF 25.11.14); and at <c>s >= 2</c> with a whole or half-whole <c>a</c>, <c>zeta(s)</c>, or
            /// <c>(2^s - 1) zeta(s)</c> at <c>a = 1/2</c> (DLMF 25.11.11), less the terms that move <c>a</c> there
            /// (25.11.3), where <c>zeta(2m) = (-1)^(m + 1) B_(2m) (2 pi)^(2m)/(2 (2m)!)</c> (25.6.2) and <c>zeta</c> at
            /// an odd number is left as the Riemann zeta function.
            /// </summary>
            internal static Entity? ExactZeta(EInteger order, ERational a)
            {
                if (a.Denominator.Equals(EInteger.One) && a.Sign <= 0 || order.Equals(EInteger.One))
                    return Real.NaN;
                if (order.Sign <= 0)
                {
                    if (order.CompareTo(-300) < 0)
                        return null;
                    var n = order.Negate().ToInt32Checked();
                    return Rational.Create(BernoulliPolynomial(n + 1, a).Divide(ERational.FromInt32(n + 1)).Negate());
                }
                if (order.CompareTo(600) > 0)
                    return null;
                var s = order.ToInt32Checked();
                var half = ERational.Create(EInteger.One, EInteger.FromInt32(2));
                var start = a.Denominator.Equals(EInteger.One) ? ERational.One
                    : a.Denominator.Equals(EInteger.FromInt32(2)) ? half : null;
                if (start is null)
                    return null;
                if (a.Equals(ERational.One) && s % 2 == 1)
                    return null;
                var steps = a.Subtract(start).ToEInteger();
                if (steps.Abs().CompareTo(1000) > 0)
                    return null;
                // zeta(s, a) = zeta(s, start) - sum_(start <= x < a) x^(-s), or + sum_(a <= x < start) x^(-s)
                var rest = ERational.Zero;
                var count = steps.Abs().ToInt32Checked();
                for (var k = 0; k < count; k++)
                {
                    var x = steps.Sign > 0 ? start.Add(ERational.FromInt32(k)) : a.Add(ERational.FromInt32(k));
                    // x^(-s), whole s, so of a negative x as well
                    var power = ERational.Create(x.Denominator.Pow(s), x.Numerator.Abs().Pow(s));
                    if (x.Numerator.Sign < 0 && s % 2 == 1)
                        power = power.Negate();
                    rest = steps.Sign > 0 ? rest.Subtract(power) : rest.Add(power);
                }
                var scale = start.Equals(half) ? ERational.FromEInteger(EInteger.One.ShiftLeft(s).Subtract(EInteger.One)) : ERational.One;
                Entity riemann;
                if (s % 2 == 0)
                {
                    var m = s / 2;
                    // zeta(2m)/pi^(2m) = (-1)^(m + 1) B_(2m) 2^(2m)/(2 (2m)!)
                    var coefficient = Bernoulli(m).Multiply(ERational.FromEInteger(EInteger.One.ShiftLeft(s)))
                        .Divide(ERational.FromEInteger(Factorial(s).Multiply(2)));
                    if (m % 2 == 0)
                        coefficient = coefficient.Negate();
                    riemann = Rational.Create(coefficient.Multiply(scale)) * MathS.Pow(MathS.pi, Integer.Create(s));
                }
                else
                    riemann = scale.Equals(ERational.One) ? new Zetaf(Integer.Create(s), Integer.One) : Rational.Create(scale) * new Zetaf(Integer.Create(s), Integer.One);
                return rest.IsZero ? riemann.InnerSimplified
                    : rest.Sign < 0 ? (riemann - Rational.Create(rest.Negate())).InnerSimplified
                    : (riemann + Rational.Create(rest)).InnerSimplified;
            }

            /// <summary>The Bernoulli polynomial <c>B_m(x) = sum_k binomial(m, k) B_k x^(m - k)</c>, with <c>B_1 = -1/2</c>.</summary>
            private static ERational BernoulliPolynomial(int m, ERational x)
            {
                var value = ERational.Zero;
                var binomial = EInteger.One;
                for (var k = 0; k <= m; k++)
                {
                    if (k > 0)
                        binomial = binomial.Multiply(m - k + 1).Divide(k);
                    var b = k == 0 ? ERational.One
                        : k == 1 ? ERational.Create(EInteger.FromInt32(-1), EInteger.FromInt32(2))
                        : k % 2 == 1 ? ERational.Zero
                        : Bernoulli(k / 2);
                    if (b.IsZero)
                        continue;
                    var power = ERational.Create(x.Numerator.Pow(m - k), x.Denominator.Pow(m - k));
                    value = value.Add(b.Multiply(ERational.FromEInteger(binomial)).Multiply(power));
                }
                return value;
            }
        }
    }
}
