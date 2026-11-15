//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core;
using PeterO.Numbers;

namespace AngouriMath
{
    partial record Entity
    {
        partial record Number
        {
            partial record Real
            {
                /// <summary>
                /// <see cref="FromMachineNumber(EDecimal)"/> of <paramref name="value"/>, whose exact value is the
                /// <see cref="double"/> <paramref name="machine"/> -- a <see cref="double"/>, or a <see cref="float"/>
                /// widened to one, which is exact; <see cref="double.NaN"/> where there is no such double. A value that
                /// is provably no small rational, which is almost every double, is a <see cref="Real"/> without
                /// the downcasting's search; every other value goes through it as before.
                /// </summary>
                /// <remarks>
                /// The search reads the decimal's mantissa, more than half of the bytes and the time a double took to
                /// become a number; the double already has the value exactly, as a ratio of two machine integers.
                /// </remarks>
                internal static Real FromMachineNumber(EDecimal value, double machine)
                    => MathS.Settings.DowncastingEnabled && IsProvablyNoSmallRational(machine)
                        ? new Real(value)
                        : FromMachineNumber(value);

                /// <summary>
                /// Whether <see cref="Create(EDecimal, bool, EDecimal)"/>, from a machine number, provably answers the
                /// <see cref="Real"/> it was given for <paramref name="x"/>: neither its rounding onto an integer nor its
                /// continued-fraction search can return anything else. <see langword="false"/> is no claim at all.
                /// </summary>
                /// <remarks>
                /// <para>
                /// <b>What the downcasting can return.</b> Write <c>t</c> for
                /// <see cref="MathS.Settings.PrecisionErrorZeroRange"/>, <c>B</c> for
                /// <see cref="MathS.Settings.MaxAbsNumeratorOrDenominatorValue"/>, <c>P</c> for the working precision's
                /// digits and <c>u = 10^(1 - P)</c>. Other than the <see cref="Real"/> itself it returns either an integer
                /// within <see cref="MathS.Settings.DowncastingTolerance"/> of <c>x</c> -- excluded below by measuring
                /// <c>x</c>'s distance to the nearest integer, with a factor of two to spare -- or a ratio <c>p/q</c>,
                /// <c>|p|, q &lt;= B</c>, from <see cref="Rational.FindRationalExactly"/>. That runs the continued fraction
                /// of <c>|x|</c>: <c>y_0 = |x|</c>, <c>a_i = floor(y_i)</c>, <c>r_i = y_i - a_i</c> exactly,
                /// <c>y_(i+1) = 1/r_i</c> rounded to the working precision, so that <c>y_(i+1) = (1/r_i)(1 + theta)</c>
                /// with <c>|theta| &lt;= u</c> (to places, an error under 10^(-P) on a quotient over 1; to digits, an ulp);
                /// refuses a level with <c>a_i &gt; B</c>; and stops at the level <c>n</c> where
                /// <c>r_n &lt; a_n t (1 + u)</c>, at most <see cref="MathS.Settings.FloatToRationalIterCount"/> - 1 levels
                /// down, returning <c>R = [a_0; a_1, ..., a_n]</c>.
                /// </para>
                /// <para>
                /// <b>How far such an answer is from x.</b> Let <c>rho_i = [a_i; ..., a_n]</c>, <c>v_i = 1/r_(i-1)</c> and
                /// <c>e_i = |y_i - rho_i|</c>. Then <c>e_n = r_n &lt; a_n t (1 + u)</c>, and
                /// <c>e_(i-1) = |1/v_i - 1/rho_i| &lt;= (e_i + u v_i) / (v_i rho_i)</c>. Every <c>rho_i</c> and every
                /// <c>y_i</c> past the first is at least 1, so <c>e_(i-1) &lt;= e_i (1 + u) + u</c> and every
                /// <c>e_i &lt;= E = (B t (1 + u) + N u)(1 + u)^N</c> for <c>N &gt;= n</c>; hence
                /// <c>v_i &gt;= c rho_i</c> with <c>c = (1 - E)/(1 + u)</c>. Since the product of the <c>rho_i</c> is
                /// <c>q</c>, unrolling gives <c>|x - R| &lt;= K (a_n t (1 + u) + N u q^2) / q^2</c>, <c>K = c^(-N)</c>.
                /// Within the gate below -- <c>P &gt;= 30</c>, <c>B &lt;= 10^9</c>, <c>B t &lt;= 10^(-3)</c>,
                /// <c>N = 100</c> -- <c>E &lt; 1.001e-3</c> and <c>K &lt; 1.12</c>; 1.2 is used.
                /// </para>
                /// <para>
                /// <b>Which ratios those can be.</b> The same bound with <c>a_n &lt;= B</c> and <c>q &lt;= B</c> is under
                /// <c>1/(2 q^2)</c>, so by Legendre's theorem <c>R</c> is a convergent <c>p_k/q_k</c> of <c>|x|</c>
                /// itself. The levels the search took are one of <c>R</c>'s two expansions, so <c>n &lt;= k + 1</c> and
                /// <c>a_n</c> is <c>a_k</c>, 1, or <c>a_(k-1) + 1</c>. And <c>|x| - p_k/q_k</c> is
                /// <c>1/(q_k^2 (x_(k+1) + q_(k-1)/q_k))</c> exactly, <c>x_(k+1)</c> the complete quotient. So the
                /// search can return the convergent only if
                /// <c>x_(k+1) + q_(k-1)/q_k &gt;= 1/(K (A t (1 + u) + N u q_k^2))</c>,
                /// <c>A = min(B, max(a_k, a_(k-1) + 1))</c> -- a partial quotient after it near <c>1/(A t)</c>, a hundred
                /// million at the defaults, where a double that is no small rational has its next quotient near its own
                /// size. A double is <c>m / 2^k</c>, so its continued fraction is Euclid's algorithm on two machine
                /// integers, run here exactly; every convergent within <c>B</c> is tested, with another factor of two
                /// for the rounding of the doubles the test is made in, and one the search can return -- or a
                /// fraction the double equals -- sends the value to the search.
                /// </para>
                /// <para>
                /// <b>The gate.</b> The bound needs the context to round as it is assumed to: digits in decimal, no
                /// traps, a rounding mode, and exponents where every quotient (at most <c>10^max(324, P)</c>) and every
                /// tolerance (at least <c>t</c>) is normal. Any other setting is left to the search.
                /// </para>
                /// </remarks>
                internal static bool IsProvablyNoSmallRational(double x)
                {
                    if (double.IsNaN(x) || double.IsInfinity(x) || x == 0)
                        return false;
                    var gate = MachineNumberGate.Current();
                    if (!gate.Applies)
                        return false;
                    var magnitude = Math.Abs(x);
                    // Past 2^52 every double is an integer.
                    if (magnitude >= 4503599627370496.0)
                        return false;
                    // Exact: a double below 2^52 less its floor.
                    var fractional = magnitude - Math.Floor(magnitude);
                    if (fractional == 0 || Math.Min(fractional, 1 - fractional) <= 2 * gate.IntegerTolerance)
                        return false;
                    var bound = gate.Bound;
                    // The double as m / 2^k, m odd.
                    var bits = BitConverter.DoubleToInt64Bits(magnitude);
                    var biasedExponent = (int)(bits >> 52);
                    var m = (ulong)bits & 0xFFFFFFFFFFFFFUL;
                    if (biasedExponent == 0)
                        biasedExponent = 1;
                    else
                        m |= 1UL << 52;
                    var k = 1075 - biasedExponent;
                    while ((m & 1) == 0)
                    {
                        m >>= 1;
                        k--;
                    }
                    if (k <= 0)
                        return false;

                    // The convergents p_j/q_j, the partial quotients a_j and a_(j-1), and the remainders whose ratio
                    // hi/lo is the complete quotient x_(j+1).
                    ulong p, q, pPrevious, qPrevious, a, aPrevious, hi, lo;
                    if (magnitude >= 1)
                    {
                        var denominator = 1UL << k;
                        a = m >> k;
                        (p, q, pPrevious, qPrevious, aPrevious) = (a, 1, 1, 0, 0);
                        (hi, lo) = (denominator, m & (denominator - 1));
                    }
                    else if (k <= 63)
                    {
                        // a_0 = 0, whose convergent 0/1 the search never returns: it has no level that stops at a
                        // partial quotient of 0, and the integer 0 was excluded above.
                        (p, q, pPrevious, qPrevious, a, aPrevious) = (0, 1, 1, 0, 0, 0);
                        (hi, lo) = (1UL << k, m);
                    }
                    else
                    {
                        // q_1 = a_1 = floor(1/|x|) is past the bound, and so is every later denominator.
                        if (magnitude * (4 * (bound + 1.0)) <= 1)
                            return true;
                        // Else |x| > 2^(-32) and k <= 85: the first step of Euclid's algorithm on (2^k, m) by hand,
                        // 2^k mod m by doubling, and a_1 = (2^k - r)/m, under 2^32, from 1/|x| within 2^(-19).
                        var remainder = (1UL << 63) % m;
                        for (var i = 63; i < k; i++)
                        {
                            remainder <<= 1;
                            if (remainder >= m)
                                remainder -= m;
                        }
                        var first = (ulong)Math.Round(1 / magnitude - (double)remainder / m);
                        if (unchecked(first * m + remainder) != 0)
                            return false;
                        if (first > bound)
                            return true;
                        (p, q, pPrevious, qPrevious, a, aPrevious) = (1, first, 0, 1, first, 0);
                        (hi, lo) = (m, remainder);
                    }

                    while (true)
                    {
                        // Numerators and denominators only grow from here.
                        if (p > bound || q > bound)
                            return true;
                        // |x| is p/q itself.
                        if (lo == 0)
                            return false;
                        if (p != 0)
                        {
                            var lastQuotient = Math.Min(bound, Math.Max((double)a, aPrevious + 1.0));
                            var completeQuotient = (double)hi / lo + (double)qPrevious / q;
                            if (2 * completeQuotient * (gate.ScaledTolerance * lastQuotient + gate.ScaledRounding * ((double)q * q)) >= 1)
                                return false;
                        }
                        var next = hi / lo;
                        // q_(j+1) >= a_(j+1) > B.
                        if (next > bound)
                            return true;
                        (pPrevious, p) = (p, next * p + pPrevious);
                        (qPrevious, q) = (q, next * q + qPrevious);
                        (aPrevious, a) = (a, next);
                        (hi, lo) = (lo, hi % lo);
                    }
                }

                /// <summary>
                /// The settings <see cref="IsProvablyNoSmallRational"/> reads, as doubles, with whether its bound holds
                /// under them; kept for the setting values it was made from, which are compared by reference and
                /// replaced whole, so that a reader never sees half of one.
                /// </summary>
                private sealed class MachineNumberGate
                {
                    private readonly EContext context;
                    private readonly EDecimal tolerance;
                    private readonly EInteger maxValue;
                    private readonly int iterCount;
                    private readonly bool toleranceOverridden;

                    internal readonly bool Applies;
                    /// <summary><c>B</c>.</summary>
                    internal readonly ulong Bound;
                    /// <summary><see cref="MathS.Settings.DowncastingToleranceAsDouble"/>.</summary>
                    internal readonly double IntegerTolerance;
                    /// <summary><c>K t (1 + u)</c>, rounded up.</summary>
                    internal readonly double ScaledTolerance;
                    /// <summary><c>K N u</c>.</summary>
                    internal readonly double ScaledRounding;

                    [ConcurrentField] private static MachineNumberGate? last;

                    private MachineNumberGate(EContext context, EDecimal tolerance, EInteger maxValue, int iterCount, bool toleranceOverridden)
                    {
                        (this.context, this.tolerance, this.maxValue, this.iterCount, this.toleranceOverridden)
                            = (context, tolerance, maxValue, iterCount, toleranceOverridden);
                        IntegerTolerance = MathS.Settings.DowncastingToleranceAsDouble;
                        const double k = 1.2;
                        const int levels = 100;
                        if (iterCount > levels
                            || context.IsSimplified || context.IsPrecisionInBits || context.Traps != 0
                            || context.Rounding == ERounding.None
                            || !context.HasMaxPrecision || !context.Precision.CanFitInInt32()
                            || !tolerance.IsFinite || tolerance.Sign <= 0
                            || maxValue.Sign <= 0 || maxValue.CompareTo(EInteger.FromInt32(1_000_000_000)) > 0
                            || !(IntegerTolerance > 0))
                            return;
                        var digits = context.Precision.ToInt32Unchecked();
                        var t = tolerance.ToDouble();
                        Bound = (ulong)maxValue.ToInt64Unchecked();
                        if (digits < 30 || !(t > 0) || double.IsInfinity(t) || Bound * t > 1e-3)
                            return;
                        if (context.HasExponentRange
                            && (!context.AdjustExponent
                                || context.EMin.CompareTo(EInteger.FromInt32((int)Math.Floor(Math.Log10(t)) - 1)) > 0
                                || context.EMax.CompareTo(EInteger.FromInt32(Math.Max(324, digits) + 1)) < 0))
                            return;
                        var u = Math.Pow(10, 1 - digits);
                        ScaledTolerance = k * t * (1 + u) * (1 + 1e-12);
                        ScaledRounding = k * levels * u;
                        Applies = true;
                    }

                    /// <summary>The gate for the settings in force.</summary>
                    internal static MachineNumberGate Current()
                    {
                        var context = MathS.Settings.DecimalPrecisionContext.Value;
                        var tolerance = MathS.Settings.PrecisionErrorZeroRange.Value;
                        var maxValue = MathS.Settings.MaxAbsNumeratorOrDenominatorValue.Value;
                        var iterCount = MathS.Settings.FloatToRationalIterCount.Value;
                        var toleranceOverridden = MathS.Settings.PrecisionErrorZeroRange.IsOverriden;
                        var gate = System.Threading.Volatile.Read(ref last);
                        if (gate is not null && ReferenceEquals(gate.context, context) && ReferenceEquals(gate.tolerance, tolerance)
                            && ReferenceEquals(gate.maxValue, maxValue) && gate.iterCount == iterCount
                            && gate.toleranceOverridden == toleranceOverridden)
                            return gate;
                        gate = new MachineNumberGate(context, tolerance, maxValue, iterCount, toleranceOverridden);
                        System.Threading.Volatile.Write(ref last, gate);
                        return gate;
                    }
                }
            }
        }
    }
}
