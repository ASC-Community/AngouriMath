//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Functions.Algebra.NumericalSolving
{
    /// <summary>
    /// Every root of a square-free polynomial with whole coefficients, to the working precision,
    /// by the Durand–Kerner iteration: each approximation moves by <c>p(z_i) / prod_{j != i}
    /// (z_i - z_j)</c>, which is Newton's step for the root it is nearest once the others are
    /// close to theirs, so the iteration converges to all the roots at once and quadratically at
    /// the end.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All the roots or none: a sum over the roots of a polynomial that is missing one is a wrong
    /// answer, so where the iteration does not settle, or settles on two approximations too close
    /// to tell apart, there is no answer. A square-free polynomial has no repeated root, so two
    /// approximations that close mean the iteration has not separated them.
    /// </para>
    /// <para>
    /// The coefficients are real, so the non-real roots come in conjugate pairs, and a root whose
    /// imaginary part is smaller than half the separation that was checked is real: were it not,
    /// it and its conjugate would be closer together than that. It is written as a real number
    /// then, rather than as one with an imaginary part of <c>1e-110</c> that no root has.
    /// </para>
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1285">#1285</a>
    /// </remarks>
    internal static class DurandKerner
    {
        /// <summary>Digits carried beyond the working precision, and dropped at the end.</summary>
        private const int GuardDigits = 10;

        /// <summary>How many sweeps over all the roots before the iteration is taken not to settle.</summary>
        private const int MaxSweeps = 1000;

        /// <summary>
        /// The roots of <paramref name="squareFree"/>, which has degree at least one and no repeated
        /// root, at the precision of <paramref name="context"/>; <see langword="null"/> where they
        /// are not settled.
        /// </summary>
        internal static IReadOnlyList<Complex>? Roots(IntegerPolynomial squareFree, EContext context)
        {
            var degree = squareFree.Degree;
            if (degree < 1)
                return null;
            var coefficients = new EDecimal[degree + 1];
            for (var i = 0; i <= degree; i++)
                coefficients[i] = EDecimal.FromEInteger(squareFree[i]);
            // Distinct to half the working precision, which a square-free polynomial's roots are
            // unless the iteration has not separated two of them.
            return Roots(coefficients, context, context.Precision.ToInt32Checked() / 2);
        }

        /// <summary>
        /// The roots of <paramref name="polynomial"/> in <paramref name="w"/> at the precision of
        /// <paramref name="context"/>, where it is a polynomial whose coefficients are real
        /// constants and not all of them rational, <c>x^5 - sqrt(2) x + 1</c> or <c>x^5 + pi</c>;
        /// <see langword="null"/> otherwise, and where they are not settled.
        /// </summary>
        /// <remarks>
        /// The coefficients are known to the working precision and not exactly, so a repeated root
        /// cannot be told from a cluster: perturbed by that error, a root of multiplicity <c>m</c>
        /// splits into <c>m</c> roots about its <c>m</c>-th root apart, and <c>m</c> is at most
        /// the degree <c>n</c>. Approximations closer than <c>10^(-digits / (2 n))</c> are taken
        /// not to be separated then, where a polynomial known to be square-free is held to half
        /// the digits. And a coefficient smaller than the largest by more than half the digits may
        /// be a zero that was not simplified to one, which as the leading coefficient would put
        /// roots beyond any bound, so it answers nothing then.
        /// </remarks>
        internal static IReadOnlyList<Complex>? RootsOf(Entity polynomial, Entity.Variable w, EContext context)
        {
            if (!TreeAnalyzer.TryGetPolynomial(polynomial, w, out var monomials))
                return null;
            var values = new Dictionary<int, EDecimal>();
            var rational = true;
            foreach (var monomial in monomials)
            {
                if (monomial.Key.Sign < 0 || monomial.Key.CompareTo(IntegerPolynomial.MaxDegree) > 0
                    || monomial.Value.Vars.Count > 0
                    || monomial.Value.EvalNumerical() is not Real { EDecimal: { IsFinite: true } value })
                    return null;
                rational &= monomial.Value.InnerSimplified is Rational;
                if (!value.IsZero)
                    values[monomial.Key.ToInt32Checked()] = value;
            }
            if (rational || values.Count == 0)
                return null;
            var degree = values.Keys.Max();
            if (degree < 1)
                return null;
            var digits = context.Precision.ToInt32Checked();
            var largest = values.Values.Aggregate(EDecimal.Zero, static (max, c) => EDecimal.Max(max, c.Abs()));
            var smallest = largest.Multiply(EDecimal.Create(EInteger.One, EInteger.FromInt32(-(digits / 2))), context);
            if (values.Values.Any(c => c.Abs().CompareTo(smallest) < 0))
                return null;
            var coefficients = new EDecimal[degree + 1];
            for (var i = 0; i <= degree; i++)
                coefficients[i] = values.TryGetValue(i, out var c) ? c : EDecimal.Zero;
            return Roots(coefficients, context, digits / (2 * degree));
        }

        /// <summary>
        /// The roots of the polynomial with real <paramref name="coefficients"/>, lowest power first,
        /// at the precision of <paramref name="context"/>, each approximation at least
        /// <c>10^-separationDigits</c> of the size of the roots from the others;
        /// <see langword="null"/> where they are not settled or not that far apart.
        /// </summary>
        /// <remarks>
        /// Solved for <c>w = z / 10^e</c>, with <c>10^e</c> the size of the roots, so that the
        /// iteration below meets roots of about unit size whatever theirs is. Its starting circle,
        /// the tolerance it settles to and the separation it holds the roots to are in absolute
        /// terms, and a polynomial whose roots were all within <c>10^-12</c> of zero was started
        /// <c>10^12</c> times as far out as they are and did not settle; with roots within
        /// <c>10^-30</c>, the products the step divides by passed below the context's exponent
        /// range and every root came out NaN. The system solved for the parts of x in
        /// <c>(1 + i) + 2x - 3x^2 + x|3 - x| = 0</c> with x written as <c>10^12 x</c> kept one root
        /// of its two. The scaling is by a power of ten, which is exact.
        /// </remarks>
        private static IReadOnlyList<Complex>? Roots(EDecimal[] coefficients, EContext context, int separationDigits)
        {
            var exponent = ExponentOfTheRoots(coefficients);
            if (exponent == 0)
                return RootsOfUnitSize(coefficients, context, separationDigits);
            // p(10^e w) has the coefficients c_k 10^(e k).
            var scaled = new EDecimal[coefficients.Length];
            for (var k = 0; k < scaled.Length; k++)
                scaled[k] = coefficients[k].ScaleByPowerOfTen(EInteger.FromInt64(exponent * k));
            if (RootsOfUnitSize(scaled, context, separationDigits) is not { } roots)
                return null;
            var power = EInteger.FromInt64(exponent);
            var back = new List<Complex>(roots.Count);
            foreach (var root in roots)
                back.Add(AsARoot(
                    root.RealPart.EDecimal.ScaleByPowerOfTen(power).RoundToPrecision(context),
                    root.ImaginaryPart.EDecimal.ScaleByPowerOfTen(power).RoundToPrecision(context)));
            return back;
        }

        /// <summary>A root with these parts, neither of them read as a residual of cancellation.</summary>
        /// <remarks>
        /// A root is a value the iteration has settled to the working precision of its own size,
        /// not what is left of a difference. Made at the unit scale, a number smaller than half the
        /// working digits is taken for such a residual and rounded onto 0: the root 3.3e-91 of
        /// <c>x^5 - 3e-60 x + 1e-150</c>, beside four of size 1.3e-15, came out 0, where the
        /// polynomial is 1e-150, and 0 was answered as a root. An imaginary part too small to be one
        /// is already 0.
        /// </remarks>
        private static Complex AsARoot(EDecimal real, EDecimal imaginary)
            => Complex.CreateAtScales(real, imaginary, EDecimal.Zero, EDecimal.Zero);

        /// <summary>
        /// The power of ten nearest below Fujiwara's bound on the roots of the polynomial with
        /// <paramref name="coefficients"/>, <c>2 max_k |a_(n-k) / a_n|^(1/k)</c> with <c>a_0</c>
        /// halved, read off the coefficients' decimal logarithms so that no quotient of them is
        /// formed; 0 where the polynomial has only its leading term.
        /// </summary>
        private static long ExponentOfTheRoots(EDecimal[] coefficients)
        {
            var degree = coefficients.Length - 1;
            var lead = Log10OfModulus(coefficients[degree]);
            var largest = double.NegativeInfinity;
            for (var k = 1; k <= degree; k++)
            {
                var coefficient = coefficients[degree - k];
                if (coefficient.IsZero)
                    continue;
                var log = Log10OfModulus(coefficient) - lead - (k == degree ? Math.Log10(2) : 0);
                largest = Math.Max(largest, log / k);
            }
            return double.IsNegativeInfinity(largest) ? 0 : (long)Math.Floor(largest + Math.Log10(2));
        }

        /// <summary>The decimal logarithm of <c>|value|</c>, to about a double's precision, for any exponent.</summary>
        private static double Log10OfModulus(EDecimal value)
        {
            var rounded = value.Abs().RoundToPrecision(EContext.ForPrecision(17));
            return Math.Log10(EDecimal.FromEInteger(rounded.Mantissa).ToDouble()) + EDecimal.FromEInteger(rounded.Exponent).ToDouble();
        }

        /// <summary><see cref="Roots(EDecimal[], EContext, int)"/>, for roots of about unit size.</summary>
        private static IReadOnlyList<Complex>? RootsOfUnitSize(EDecimal[] coefficients, EContext context, int separationDigits)
        {
            var degree = coefficients.Length - 1;
            var digits = context.Precision.ToInt32Checked();
            var work = context.WithPrecision(digits + GuardDigits);
            var lead = coefficients[degree];
            var monic = new EDecimal[degree];
            for (var i = 0; i < degree; i++)
                monic[i] = coefficients[i].Divide(lead, work);

            // Cauchy's bound: every root is within 1 + max |a_i / a_n| of zero. The approximations
            // start spread round that circle, turned off the axes so that no two start as a
            // conjugate pair or on a line of symmetry the iteration cannot leave.
            var radius = EDecimal.One;
            foreach (var coefficient in monic)
                radius = EDecimal.Max(radius, EDecimal.One.Add(coefficient.Abs(), work));
            var r = radius.ToDouble();
            var re = new EDecimal[degree];
            var im = new EDecimal[degree];
            for (var i = 0; i < degree; i++)
            {
                var angle = 2 * Math.PI * i / degree + 0.4;
                re[i] = EDecimal.FromDouble(r * Math.Cos(angle));
                im[i] = EDecimal.FromDouble(r * Math.Sin(angle));
            }

            var tolerance = EDecimal.Create(EInteger.One, EInteger.FromInt32(-(digits + 2))).Multiply(radius, work);
            var settled = false;
            for (var sweep = 0; sweep < MaxSweeps && !settled; sweep++)
            {
                var largest = EDecimal.Zero;
                for (var i = 0; i < degree; i++)
                {
                    // p(z_i) by Horner's scheme, on the monic polynomial.
                    var (pRe, pIm) = (EDecimal.One, EDecimal.Zero);
                    for (var k = degree - 1; k >= 0; k--)
                    {
                        (pRe, pIm) = Times(pRe, pIm, re[i], im[i], work);
                        pRe = pRe.Add(monic[k], work);
                    }
                    var (qRe, qIm) = (EDecimal.One, EDecimal.Zero);
                    for (var j = 0; j < degree; j++)
                        if (j != i)
                            (qRe, qIm) = Times(qRe, qIm, re[i].Subtract(re[j], work), im[i].Subtract(im[j], work), work);
                    if (qRe.IsZero && qIm.IsZero)
                        return null;
                    var (stepRe, stepIm) = Over(pRe, pIm, qRe, qIm, work);
                    re[i] = re[i].Subtract(stepRe, work);
                    im[i] = im[i].Subtract(stepIm, work);
                    largest = EDecimal.Max(largest, stepRe.Abs().Add(stepIm.Abs(), work));
                }
                // A step that is not a number has not settled, and NaN compares as anything.
                if (!largest.IsFinite)
                    return null;
                settled = largest.CompareTo(tolerance) <= 0;
            }
            if (!settled)
                return null;

            var separation = EDecimal.Create(EInteger.One, EInteger.FromInt32(-separationDigits));
            for (var i = 0; i < degree; i++)
                for (var j = i + 1; j < degree; j++)
                    if (re[i].Subtract(re[j], work).Abs().Add(im[i].Subtract(im[j], work).Abs(), work).CompareTo(separation) <= 0)
                        return null;

            var half = separation.Divide(EDecimal.FromInt32(2), work);
            var roots = new List<Complex>(degree);
            for (var i = 0; i < degree; i++)
            {
                var imaginary = im[i].Abs().CompareTo(half) < 0 ? EDecimal.Zero : im[i].RoundToPrecision(context);
                roots.Add(AsARoot(re[i].RoundToPrecision(context), imaginary));
            }
            return roots;
        }

        private static (EDecimal Re, EDecimal Im) Times(EDecimal aRe, EDecimal aIm, EDecimal bRe, EDecimal bIm, EContext work)
            => (aRe.Multiply(bRe, work).Subtract(aIm.Multiply(bIm, work), work),
                aRe.Multiply(bIm, work).Add(aIm.Multiply(bRe, work), work));

        private static (EDecimal Re, EDecimal Im) Over(EDecimal aRe, EDecimal aIm, EDecimal bRe, EDecimal bIm, EContext work)
        {
            var norm = bRe.Multiply(bRe, work).Add(bIm.Multiply(bIm, work), work);
            return (aRe.Multiply(bRe, work).Add(aIm.Multiply(bIm, work), work).Divide(norm, work),
                aIm.Multiply(bRe, work).Subtract(aRe.Multiply(bIm, work), work).Divide(norm, work));
        }
    }
}
