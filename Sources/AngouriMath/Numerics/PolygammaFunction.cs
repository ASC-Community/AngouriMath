//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using NumericsComplex = System.Numerics.Complex;

namespace AngouriMath.Numerics
{
    /// <summary>
    /// The polygamma functions and the harmonic numbers in double precision, for the compilers, on the
    /// interpreter's terms and by its algorithm: <c>psi^(n)(z)</c> for a whole <c>n &gt;= 0</c>, reflected
    /// left of <c>Re z = 1/2</c> (DLMF 5.15.6), moved right by the recurrence (DLMF 5.15.5) and summed by the
    /// asymptotic series (DLMF 5.11.2, 5.15.8); <c>NaN</c> at its poles <c>0, -1, -2, ...</c> and at an order
    /// that is not whole.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class PolygammaFunction
    {
        [AngouriMath.Core.ConstantField] private static readonly NumericsComplex NaN = new(double.NaN, 0);

        /// <summary>The most terms of the asymptotic series, and so the Bernoulli numbers kept.</summary>
        private const int Terms = 40;

        /// <summary>B_2, B_4, ..., B_(2 Terms), from the tangent numbers exactly and then rounded.</summary>
        [AngouriMath.Core.ConstantField] private static readonly double[] bernoulli = Bernoulli();

        public static NumericsComplex Polygamma(NumericsComplex order, NumericsComplex z)
        {
            if (order.Imaginary != 0 || double.IsNaN(order.Real) || double.IsInfinity(order.Real)
                || Math.Floor(order.Real) != order.Real || order.Real < 0 || order.Real > 170)
                return NaN;
            var n = (int)order.Real;
            if (double.IsNaN(z.Real) || double.IsNaN(z.Imaginary))
                return NaN;
            if (double.IsInfinity(z.Real) || double.IsInfinity(z.Imaginary))
                // Toward +oo on the real line the digamma function grows as ln x and the others vanish.
                return z.Imaginary == 0 && double.IsPositiveInfinity(z.Real) ? (n == 0 ? double.PositiveInfinity : 0) : NaN;
            if (z.Imaginary == 0 && z.Real <= 0 && Math.Floor(z.Real) == z.Real)
                return NaN;
            return Of(n, z);
        }

        public static double Polygamma(double order, double z)
        {
            var value = Polygamma(new NumericsComplex(order, 0), new NumericsComplex(z, 0));
            return value.Imaginary == 0 ? value.Real : double.NaN;
        }

        private static NumericsComplex Of(int n, NumericsComplex z)
        {
            // Reflected only near the axis, where moving right passes its poles, and far left of it, where it
            // would take thousands of steps: off the axis cot(pi z) is near i or -i, where 1 + cot^2 vanishes,
            // and Q_n's terms cancel to all but a few digits.
            if (z.Real < 0.5 && (Math.Abs(z.Imaginary) < 1 || z.Real < -1000))
            {
                // psi^(n)(z) = (-1)^n psi^(n)(1 - z) - pi^(n + 1) Q_n(cot(pi z))
                var reflected = Of(n, 1 - z);
                if (n % 2 == 1)
                    reflected = -reflected;
                var cot = Cot(z);
                return reflected - Math.Pow(Math.PI, n + 1) * Evaluate(CotangentDerivative(n), cot);
            }
            var radius = 16 + n;
            var shift = Math.Max(0, (int)Math.Ceiling(radius - z.Real));
            // sum_(k < shift) 1/(z + k)^(n + 1)
            NumericsComplex sum = 0;
            for (var k = 0; k < shift; k++)
                sum += 1 / NumericsComplex.Pow(z + k, n + 1);
            var tail = Asymptotic(n, z + shift);
            // psi^(n)(z) = psi^(n)(z + shift) - (-1)^n n! sum
            var scaled = SpecialFunctions.Factorial((double)n) * sum;
            return n % 2 == 0 ? tail - scaled : tail + scaled;
        }

        // ln w - 1/(2 w) - sum B_(2k)/(2k w^(2k)) for the digamma function, and
        // (-1)^(n + 1) ((n - 1)!/w^n + n!/(2 w^(n + 1)) + sum B_(2k) (2k + n - 1)!/((2k)! w^(2k + n))) otherwise.
        private static NumericsComplex Asymptotic(int n, NumericsComplex w)
        {
            var inverse = 1 / w;
            var inverseSquared = inverse * inverse;
            NumericsComplex sum, power;
            if (n == 0)
            {
                sum = NumericsComplex.Log(w) - inverse / 2;
                power = inverseSquared;
            }
            else
            {
                var toTheN = NumericsComplex.Pow(inverse, n);
                sum = SpecialFunctions.Factorial((double)(n - 1)) * toTheN + SpecialFunctions.Factorial((double)n) / 2 * toTheN * inverse;
                power = toTheN * inverseSquared;
            }
            for (var k = 1; k <= Terms; k++)
            {
                // B_(2k)/(2k) for the digamma function, B_(2k) (2k + n - 1)!/(2k)! otherwise.
                var coefficient = bernoulli[k - 1];
                if (n == 0)
                    coefficient /= 2 * k;
                else
                    for (var i = 0; i < n - 1; i++)
                        coefficient *= 2 * k + 1 + i;
                var term = coefficient * power;
                sum = n == 0 ? sum - term : sum + term;
                if (NumericsComplex.Abs(term) <= 1e-17 * NumericsComplex.Abs(sum))
                    break;
                power *= inverseSquared;
            }
            return n == 0 || n % 2 == 1 ? sum : -sum;
        }

        /// <summary>
        /// The harmonic number <c>H_n^(m)</c>: the sum <c>1 + 1/2^m + ... + 1/n^m</c> for a whole
        /// <c>n &gt;= 0</c> up to 10000, no value at a negative whole <c>n</c> but for a whole <c>m &lt;= 0</c>,
        /// and elsewhere, for a whole <c>m</c> from 1 to 64,
        /// <c>(-1)^m/(m - 1)! (psi^(m - 1)(1) - psi^(m - 1)(n + 1))</c>.
        /// </summary>
        public static NumericsComplex HarmonicNumber(NumericsComplex n, NumericsComplex m)
        {
            var wholeN = IsWhole(n, out var number);
            var wholeM = IsWhole(m, out var order);
            if (wholeN && number >= 0 && number <= 10000)
            {
                NumericsComplex sum = 0;
                for (var k = 1; k <= number; k++)
                    sum += NumericsComplex.Pow(k, -m);
                return sum;
            }
            if (wholeN && number < 0 && !(wholeM && order <= 0))
                return NaN;
            if (wholeM && order >= 1 && order <= 64)
            {
                var sign = order % 2 == 0 ? 1 : -1;
                var o = (int)order;
                return sign / SpecialFunctions.Factorial((double)(o - 1))
                    * (Polygamma(o - 1, 1) - Polygamma(o - 1, n + 1));
            }
            return NaN;
        }

        public static double HarmonicNumber(double n, double m)
        {
            var value = HarmonicNumber(new NumericsComplex(n, 0), new NumericsComplex(m, 0));
            return value.Imaginary == 0 ? value.Real : double.NaN;
        }

        private static bool IsWhole(NumericsComplex z, out long whole)
        {
            whole = 0;
            if (z.Imaginary != 0 || double.IsNaN(z.Real) || double.IsInfinity(z.Real)
                || Math.Floor(z.Real) != z.Real || Math.Abs(z.Real) > 9e15)
                return false;
            whole = (long)z.Real;
            return true;
        }

        /// <summary>
        /// <c>cot(pi z)</c>, accurate where it vanishes: <c>Q_n</c>'s coefficients and <c>pi^(n + 1)</c> magnify its
        /// error, and at a half-whole <c>z</c> <c>cos(pi z)</c> is a rounding away from 0. The real part is reduced
        /// by its nearest integer, the period, and near a half <c>cot(pi r)</c> is <c>tan(pi (1/2 - r))</c>, whose
        /// argument is then exact. <c>Complex.Tan</c> is taken only of a small argument: of one near <c>pi/2</c> it
        /// takes the imaginary part as <c>sinh(2y)/(cos(2x) + cosh(2y))</c>, 0/0 on the axis.
        /// </summary>
        private static NumericsComplex Cot(NumericsComplex z)
        {
            var x = z.Real - Math.Round(z.Real);
            if (Math.Abs(x) <= 0.25)
            {
                var r = Math.PI * new NumericsComplex(x, z.Imaginary);
                return NumericsComplex.Cos(r) / NumericsComplex.Sin(r);
            }
            var half = x > 0 ? 0.5 : -0.5;
            return NumericsComplex.Tan(Math.PI * new NumericsComplex(half - x, -z.Imaginary));
        }

        private static NumericsComplex Evaluate(double[] coefficients, NumericsComplex x)
        {
            NumericsComplex value = 0;
            for (var i = coefficients.Length - 1; i >= 0; i--)
                value = value * x + coefficients[i];
            return value;
        }

        /// <summary>The coefficients, lowest first, of <c>Q_n</c>, with <c>d^n/dz^n cot(pi z) = pi^n Q_n(cot(pi z))</c>.</summary>
        private static double[] CotangentDerivative(int n)
        {
            var q = new double[] { 0, 1 };
            for (var step = 0; step < n; step++)
            {
                // -(1 + c^2) q'(c)
                var derivative = new double[q.Length - 1];
                for (var i = 1; i < q.Length; i++)
                    derivative[i - 1] = q[i] * i;
                var next = new double[derivative.Length + 2];
                for (var i = 0; i < derivative.Length; i++)
                {
                    next[i] -= derivative[i];
                    next[i + 2] -= derivative[i];
                }
                q = next;
            }
            return q;
        }

        // B_(2k) = (-1)^(k - 1) 2k T_k/(4^k (4^k - 1)), the tangent numbers by Brent and Harvey's recurrence.
        private static double[] Bernoulli()
        {
            var t = new EInteger[Terms + 1];
            t[1] = EInteger.One;
            for (var i = 2; i <= Terms; i++)
                t[i] = t[i - 1].Multiply(i - 1);
            for (var i = 2; i <= Terms; i++)
                for (var j = i; j <= Terms; j++)
                    t[j] = t[j - 1].Multiply(j - i).Add(t[j].Multiply(j - i + 2));
            var values = new double[Terms];
            for (var k = 1; k <= Terms; k++)
            {
                var fourToTheK = EInteger.One.ShiftLeft(2 * k);
                var value = ERational.Create(t[k].Multiply(2 * k), fourToTheK.Multiply(fourToTheK.Subtract(EInteger.One))).ToDouble();
                values[k - 1] = k % 2 == 1 ? value : -value;
            }
            return values;
        }
    }
}
