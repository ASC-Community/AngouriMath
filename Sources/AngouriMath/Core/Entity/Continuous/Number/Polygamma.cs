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
            /// The polygamma function <c>psi^(n)(z)</c>, the <c>(n + 1)</c>-th derivative of <c>ln Gamma(z)</c>, at the
            /// working precision, for a whole <c>n >= 0</c> and every complex <c>z</c> but its poles <c>0, -1, -2, ...</c>,
            /// where it is <c>NaN</c>. <c>psi^(0)</c> is the digamma function, <c>Gamma'(z)/Gamma(z)</c>.
            /// </summary>
            /// <remarks>
            /// <para>
            /// Left of <c>Re z = 1/2</c> it is reflected (DLMF 5.15.6),
            /// <c>psi^(n)(z) = (-1)^n psi^(n)(1 - z) - pi^(n + 1) Q_n(cot(pi z))</c>, where <c>Q_n</c> is the polynomial
            /// with <c>d^n/dz^n cot(pi z) = pi^n Q_n(cot(pi z))</c>: <c>Q_0(c) = c</c> and
            /// <c>Q_(k + 1)(c) = -(1 + c^2) Q_k'(c)</c>.
            /// </para>
            /// <para>
            /// Right of it, the recurrence <c>psi^(n)(z + 1) = psi^(n)(z) + (-1)^n n!/z^(n + 1)</c> (DLMF 5.15.5) moves
            /// the argument to <c>Re w >= R</c>, with <c>R</c> the working digits and <c>n</c>, where the asymptotic
            /// series (DLMF 5.11.2 and 5.15.8) is summed until a term is below the working precision:
            /// <c>ln w - 1/(2 w) - sum B_(2k)/(2k w^(2k))</c> for the digamma function, and
            /// <c>(-1)^(n + 1) ((n - 1)!/w^n + n!/(2 w^(n + 1)) + sum B_(2k) (2k + n - 1)!/((2k)! w^(2k + n)))</c> for
            /// <c>n >= 1</c>. Its smallest term is about <c>e^(-2 pi R)</c> of the sum, so it converges well before
            /// it diverges.
            /// </para>
            /// <para>
            /// The Bernoulli numbers are exact, from the tangent numbers by Brent and Harvey's recurrence,
            /// <c>B_(2k) = (-1)^(k - 1) 2k T_k/(4^k (4^k - 1))</c>, and cached.
            /// </para>
            /// https://github.com/asc-community/AngouriMath/issues/171
            /// </remarks>
            public static Complex Polygamma(EInteger order, Complex z)
            {
                var context = MathS.Settings.DecimalPrecisionContext.Value;
                var (re, im) = (z.RealPart.EDecimal, z.ImaginaryPart.EDecimal);
                if (re.IsNaN() || im.IsNaN() || order.Sign < 0 || !order.CanFitInInt32())
                    return Real.NaN;
                var n = order.ToInt32Checked();
                if (re.IsInfinity() || im.IsInfinity())
                    // Toward +oo on the real line the digamma function grows as ln x and the others vanish.
                    return im.IsZero && re.IsPositiveInfinity() ? (n == 0 ? Real.PositiveInfinity : Integer.Zero) : Real.NaN;
                if (im.IsZero && re.Sign <= 0 && re.IsInteger())
                    return Real.NaN;
                var work = Working(context, 10 + n.ToString().Length + context.Precision.ToString().Length);
                var (valueRe, valueIm) = Polygamma(n, re, im, work);
                return Complex.Create(valueRe.RoundToPrecision(context), valueIm.RoundToPrecision(context));
            }

            /// <summary>
            /// <c>psi^(n)(q)</c> exactly, for <c>n = 0</c> or an odd order <c>n</c> and a whole or half-whole
            /// <c>q</c> other than a pole, or <see langword="null"/>: a rational times <c>pi^(n + 1)</c>, plus a
            /// rational, or for the digamma function a rational less Euler's constant and, at a half-whole
            /// <c>q</c>, <c>2 ln 2</c>.
            /// </summary>
            /// <remarks>
            /// <c>psi(1) = -gamma</c> and <c>psi(1/2) = -gamma - 2 ln 2</c> (DLMF 5.4.12, 5.4.13), and
            /// <c>psi^(n)(1) = n! zeta(n + 1)</c> and <c>psi^(n)(1/2) = (2^(n + 1) - 1) n! zeta(n + 1)</c> for an odd
            /// <c>n</c> (DLMF 5.15.2, 5.15.3), where <c>zeta(2m) = (-1)^(m + 1) B_(2m) (2 pi)^(2m)/(2 (2m)!)</c> is a
            /// rational times <c>pi^(2m)</c>; the recurrence <c>psi^(n)(z + 1) = psi^(n)(z) + (-1)^n n!/z^(n + 1)</c>
            /// moves the argument to either. An even order from 2 wants <c>zeta</c> at an odd number, which has no
            /// closed form here. https://github.com/asc-community/AngouriMath/issues/171
            /// </remarks>
            internal static Entity? ExactPolygamma(EInteger order, ERational argument)
                => ExactPolygammaParts(order, argument) is var (n, coefficient, rest)
                    ? Exact(withGamma: n == 0, multipleFirst: n != 0, n, coefficient, rest)
                    : null;

            /// <summary>
            /// The harmonic number <c>H_n^(m) = (-1)^m/(m - 1)! (psi^(m - 1)(1) - psi^(m - 1)(n + 1))</c> exactly, where
            /// <see cref="ExactPolygamma"/> has both values: <c>m = 1</c> or an even <c>m</c>, at a half-whole <c>n</c>.
            /// Euler's constant, which both carry for <c>m = 1</c>, is left out rather than cancelled.
            /// </summary>
            internal static Entity? ExactHarmonicNumber(int m, ERational n)
            {
                if (m < 1 || ExactPolygammaParts(m - 1, ERational.One) is not var (order, coefficientAtOne, restAtOne)
                    || ExactPolygammaParts(m - 1, n.Add(ERational.One)) is not var (_, coefficient, rest))
                    return null;
                var scale = ERational.FromEInteger(m % 2 == 0 ? EInteger.One : EInteger.FromInt32(-1))
                    .Divide(ERational.FromEInteger(Factorial(m - 1)));
                return Exact(withGamma: false, multipleFirst: false, order,
                    coefficientAtOne.Subtract(coefficient).Multiply(scale), restAtOne.Subtract(rest).Multiply(scale));
            }

            /// <summary>
            /// <c>psi^(n)(q)</c> but for Euler's constant, as a rational times <c>pi^(n + 1)</c> for an odd
            /// <c>n</c> or times <c>ln 2</c> for <c>n = 0</c>, and a rational.
            /// </summary>
            private static (int Order, ERational Coefficient, ERational Remainder)? ExactPolygammaParts(EInteger order, ERational argument)
            {
                if (order.Sign < 0 || order.Sign > 0 && order.IsEven || order.CompareTo(100) > 0)
                    return null;
                var half = ERational.Create(EInteger.One, EInteger.FromInt32(2));
                var start = argument.Denominator.Equals(EInteger.One) ? ERational.One
                    : argument.Denominator.Equals(EInteger.FromInt32(2)) ? half : null;
                if (start is null || argument.Denominator.Equals(EInteger.One) && argument.Sign <= 0)
                    return null;
                var steps = argument.Subtract(start).ToEInteger();
                if (steps.Abs().CompareTo(1000) > 0)
                    return null;
                var n = order.ToInt32Checked();
                var factorial = Factorial(n);
                // The recurrence, up from the start or down to the argument; (-1)^n n!/z^(n + 1) a step.
                var rest = ERational.Zero;
                var power = n + 1;
                var step = ERational.FromEInteger(n % 2 == 0 ? factorial : factorial.Negate());
                if (steps.Sign > 0)
                    for (var j = EInteger.Zero; j.CompareTo(steps) < 0; j = j.Add(EInteger.One))
                        rest = rest.Add(step.Divide(Pow(start.Add(ERational.FromEInteger(j)), power)));
                else
                    for (var j = EInteger.One; j.CompareTo(steps.Negate()) <= 0; j = j.Add(EInteger.One))
                        rest = rest.Subtract(step.Divide(Pow(start.Subtract(ERational.FromEInteger(j)), power)));
                if (n == 0)
                    // psi(1) = -gamma, and psi(1/2) = -gamma - 2 ln 2.
                    return (n, start.Equals(half) ? ERational.FromInt32(-2) : ERational.Zero, rest);
                var m = (n + 1) / 2;
                // zeta(2m) / pi^(2m) = (-1)^(m + 1) B_(2m) 2^(2m) / (2 (2m)!)
                var zetaOverPiPower = Bernoulli(m).Multiply(ERational.FromEInteger(EInteger.One.ShiftLeft(2 * m)))
                    .Divide(ERational.FromEInteger(Factorial(2 * m).Multiply(2)));
                if (m % 2 == 0)
                    zetaOverPiPower = zetaOverPiPower.Negate();
                var piCoefficient = zetaOverPiPower.Multiply(ERational.FromEInteger(factorial));
                if (start.Equals(half))
                    piCoefficient = piCoefficient.Multiply(ERational.FromEInteger(EInteger.One.ShiftLeft(n + 1).Subtract(EInteger.One)));
                return (n, piCoefficient, rest);

                static ERational Pow(ERational x, int exponent)
                    => ERational.Create(x.Numerator.Pow(exponent), x.Denominator.Pow(exponent));
            }

            // c ln 2 for n = 0 and c pi^(n + 1) otherwise, and r, less Euler's constant where it is asked for, without
            // the terms that are zero and with a negative one subtracted. An odd order leads with the multiple of
            // pi^(n + 1), as it always has; the digamma function and a difference lead with the rational.
            private static Entity Exact(bool withGamma, bool multipleFirst, int n, ERational coefficient, ERational rest)
            {
                Entity? sum = null;
                var unit = n == 0 ? MathS.Ln(Integer.Create(2)) : MathS.Pow(MathS.pi, Integer.Create(n + 1));
                if (multipleFirst)
                {
                    Add(coefficient, unit);
                    Add(rest, null);
                }
                else
                {
                    Add(rest, null);
                    Add(coefficient, unit);
                }
                if (withGamma)
                    sum = sum is null ? -MathS.eulergamma : sum - MathS.eulergamma;
                return (sum ?? Integer.Zero).InnerSimplified;

                void Add(ERational c, Entity? of)
                {
                    if (c.IsZero)
                        return;
                    var term = of is null ? Rational.Create(c.Abs()) : Rational.Create(c.Abs()) * of;
                    sum = sum is null ? (c.Sign < 0 ? -term : term) : c.Sign < 0 ? sum - term : sum + term;
                }
            }

            private static (EDecimal Re, EDecimal Im) Polygamma(int n, EDecimal re, EDecimal im, EContext work)
            {
                if (re.CompareTo(EDecimal.FromString("0.5")) < 0)
                {
                    // psi^(n)(z) = (-1)^n psi^(n)(1 - z) - pi^(n + 1) Q_n(cot(pi z))
                    var reflected = Polygamma(n, EDecimal.One.Subtract(re, work), im.Negate(), work);
                    if (n % 2 == 1)
                        reflected = (reflected.Re.Negate(), reflected.Im.Negate());
                    var pi = EDecimal.PI(work);
                    var cot = Cot(pi.Multiply(re, work), pi.Multiply(im, work), work);
                    var q = Evaluate(CotangentDerivative(n), cot, work);
                    var piPower = pi.Pow(n + 1, work);
                    return (reflected.Re.Subtract(piPower.Multiply(q.Re, work), work), reflected.Im.Subtract(piPower.Multiply(q.Im, work), work));
                }
                var radius = work.Precision.ToInt32Checked() + n;
                var shift = Math.Max(0, (int)Math.Ceiling(radius - re.ToDouble()));
                // sum_(k < shift) 1/(z + k)^(n + 1)
                var sum = (Re: EDecimal.Zero, Im: EDecimal.Zero);
                for (var k = 0; k < shift; k++)
                {
                    var term = Reciprocal(Power((re.Add(EDecimal.FromInt32(k), work), im), n + 1, work), work);
                    sum = (sum.Re.Add(term.Re, work), sum.Im.Add(term.Im, work));
                }
                var w = (Re: re.Add(EDecimal.FromInt32(shift), work), Im: im);
                var tail = AsymptoticPolygamma(n, w, work);
                // psi^(n)(z) = psi^(n)(z + shift) - (-1)^n n! sum
                var factorial = EDecimal.FromEInteger(Factorial(n));
                var scaled = (Re: sum.Re.Multiply(factorial, work), Im: sum.Im.Multiply(factorial, work));
                return n % 2 == 0
                    ? (tail.Re.Subtract(scaled.Re, work), tail.Im.Subtract(scaled.Im, work))
                    : (tail.Re.Add(scaled.Re, work), tail.Im.Add(scaled.Im, work));
            }

            /// <summary>The asymptotic series of <c>psi^(n)(w)</c>, for <c>Re w</c> at least the working digits.</summary>
            private static (EDecimal Re, EDecimal Im) AsymptoticPolygamma(int n, (EDecimal Re, EDecimal Im) w, EContext work)
            {
                var epsilon = EDecimal.Create(EInteger.One, EInteger.FromInt32(-work.Precision.ToInt32Checked()));
                var inverse = Reciprocal(w, work);
                var inverseSquared = Multiply(inverse, inverse, work);
                (EDecimal Re, EDecimal Im) sum;
                // The power of 1/w the first Bernoulli term carries: w^-(2 + n).
                (EDecimal Re, EDecimal Im) power;
                if (n == 0)
                {
                    // ln w - 1/(2 w)
                    var modulus = w.Re.Multiply(w.Re, work).Add(w.Im.Multiply(w.Im, work), work).Sqrt(work);
                    sum = (modulus.Log(work).Subtract(inverse.Re.Divide(2, work), work), w.Im.Arctan2(w.Re, work).Subtract(inverse.Im.Divide(2, work), work));
                    power = inverseSquared;
                }
                else
                {
                    // (n - 1)!/w^n + n!/(2 w^(n + 1))
                    var toTheN = Power(inverse, n, work);
                    var toTheNPlusOne = Multiply(toTheN, inverse, work);
                    var (previous, current) = (EDecimal.FromEInteger(Factorial(n - 1)), EDecimal.FromEInteger(Factorial(n)).Divide(2, work));
                    sum = (toTheN.Re.Multiply(previous, work).Add(toTheNPlusOne.Re.Multiply(current, work), work),
                           toTheN.Im.Multiply(previous, work).Add(toTheNPlusOne.Im.Multiply(current, work), work));
                    power = Multiply(toTheN, inverseSquared, work);
                }
                for (var k = 1; k <= 4 * work.Precision.ToInt32Checked() + 4 * n + 16; k++)
                {
                    // B_(2k)/(2k) for the digamma function, B_(2k) (2k + n - 1)!/(2k)! otherwise.
                    var bernoulli = Bernoulli(k);
                    var coefficient = n == 0
                        ? EDecimal.FromEInteger(bernoulli.Numerator).Divide(EDecimal.FromEInteger(bernoulli.Denominator.Multiply(2 * k)), work)
                        : EDecimal.FromEInteger(bernoulli.Numerator.Multiply(RisingProduct(2 * k + 1, n - 1))).Divide(EDecimal.FromEInteger(bernoulli.Denominator), work);
                    var term = (Re: power.Re.Multiply(coefficient, work), Im: power.Im.Multiply(coefficient, work));
                    sum = n == 0 ? (sum.Re.Subtract(term.Re, work), sum.Im.Subtract(term.Im, work)) : (sum.Re.Add(term.Re, work), sum.Im.Add(term.Im, work));
                    if (Magnitude(term).CompareTo(Magnitude(sum).Multiply(epsilon, work)) < 0)
                        break;
                    power = Multiply(power, inverseSquared, work);
                }
                // (-1)^(n + 1) in front of the series for n >= 1
                return n == 0 || n % 2 == 1 ? sum : (sum.Re.Negate(), sum.Im.Negate());
            }

            /// <summary><c>(from)(from + 1)...(from + count - 1)</c>, and one for a count of zero.</summary>
            private static EInteger RisingProduct(int from, int count)
            {
                var product = EInteger.One;
                for (var i = 0; i < count; i++)
                    product = product.Multiply(from + i);
                return product;
            }

            private static EInteger Factorial(int n)
            {
                var product = EInteger.One;
                for (var i = 2; i <= n; i++)
                    product = product.Multiply(i);
                return product;
            }

            /// <summary><c>B_(2k)</c>, exactly, from the tangent numbers.</summary>
            private static ERational Bernoulli(int k)
            {
                var tangent = Tangent(k);
                var fourToTheK = EInteger.One.ShiftLeft(2 * k);
                var value = ERational.Create(tangent.Multiply(2 * k), fourToTheK.Multiply(fourToTheK.Subtract(EInteger.One)));
                return k % 2 == 1 ? value : value.Negate();
            }

            /// <summary>
            /// The tangent number <c>T_k</c>, the <c>(2k - 1)</c>-th derivative of <c>tan</c> at 0: 1, 2, 16, 272, ...
            /// Brent and Harvey's in-place recurrence for <c>T_1 ... T_m</c>, recomputed at twice the length whenever a
            /// larger one is asked for.
            /// </summary>
            private static EInteger Tangent(int k)
            {
                lock (tangentNumbers)
                {
                    if (k > tangentNumbers.Count)
                    {
                        var m = Math.Max(k, 2 * tangentNumbers.Count);
                        var t = new EInteger[m + 1];
                        t[1] = EInteger.One;
                        for (var i = 2; i <= m; i++)
                            t[i] = t[i - 1].Multiply(i - 1);
                        for (var i = 2; i <= m; i++)
                            for (var j = i; j <= m; j++)
                                t[j] = t[j - 1].Multiply(j - i).Add(t[j].Multiply(j - i + 2));
                        tangentNumbers.Clear();
                        for (var i = 1; i <= m; i++)
                            tangentNumbers.Add(t[i]);
                    }
                    return tangentNumbers[k - 1];
                }
            }
            [ConcurrentField] private static readonly List<EInteger> tangentNumbers = new();

            /// <summary>The coefficients, lowest first, of <c>Q_n</c>, with <c>d^n/dz^n cot(pi z) = pi^n Q_n(cot(pi z))</c>.</summary>
            private static EInteger[] CotangentDerivative(int n)
            {
                var q = new[] { EInteger.Zero, EInteger.One };
                for (var step = 0; step < n; step++)
                {
                    // -(1 + c^2) q'(c)
                    var derivative = new EInteger[q.Length - 1];
                    for (var i = 1; i < q.Length; i++)
                        derivative[i - 1] = q[i].Multiply(i);
                    var next = new EInteger[derivative.Length + 2];
                    for (var i = 0; i < next.Length; i++)
                        next[i] = EInteger.Zero;
                    for (var i = 0; i < derivative.Length; i++)
                    {
                        next[i] = next[i].Subtract(derivative[i]);
                        next[i + 2] = next[i + 2].Subtract(derivative[i]);
                    }
                    q = next;
                }
                return q;
            }

            private static (EDecimal Re, EDecimal Im) Evaluate(EInteger[] coefficients, (EDecimal Re, EDecimal Im) x, EContext work)
            {
                var value = (Re: EDecimal.Zero, Im: EDecimal.Zero);
                for (var i = coefficients.Length - 1; i >= 0; i--)
                {
                    value = Multiply(value, x, work);
                    value = (value.Re.Add(EDecimal.FromEInteger(coefficients[i]), work), value.Im);
                }
                return value;
            }

            /// <summary><c>cot(x + i y) = (sin 2x - i sinh 2y)/(cosh 2y - cos 2x)</c>.</summary>
            private static (EDecimal Re, EDecimal Im) Cot(EDecimal x, EDecimal y, EContext work)
            {
                var (twoX, twoY) = (x.Multiply(2, work), y.Multiply(2, work));
                if (twoY.IsZero)
                    return (twoX.Sin(work).Divide(EDecimal.One.Subtract(twoX.Cos(work), work), work), EDecimal.Zero);
                var exponential = twoY.Exponential(work);
                var inverseExponential = EDecimal.One.Divide(exponential, work);
                var sinh = exponential.Subtract(inverseExponential, work).Divide(2, work);
                var cosh = exponential.Add(inverseExponential, work).Divide(2, work);
                var denominator = cosh.Subtract(twoX.Cos(work), work);
                return (twoX.Sin(work).Divide(denominator, work), sinh.Negate().Divide(denominator, work));
            }

            private static (EDecimal Re, EDecimal Im) Reciprocal((EDecimal Re, EDecimal Im) a, EContext work)
            {
                var squared = a.Re.Multiply(a.Re, work).Add(a.Im.Multiply(a.Im, work), work);
                return (a.Re.Divide(squared, work), a.Im.Negate().Divide(squared, work));
            }

            private static (EDecimal Re, EDecimal Im) Power((EDecimal Re, EDecimal Im) a, int exponent, EContext work)
            {
                var result = (Re: EDecimal.One, Im: EDecimal.Zero);
                for (var square = a; exponent > 0; exponent >>= 1)
                {
                    if ((exponent & 1) == 1)
                        result = Multiply(result, square, work);
                    if (exponent > 1)
                        square = Multiply(square, square, work);
                }
                return result;
            }
        }
    }
}
