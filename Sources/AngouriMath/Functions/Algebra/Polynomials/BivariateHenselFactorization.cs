//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using System.Collections.Generic;

namespace AngouriMath.Functions
{
    /// <summary>
    /// Factorisation in two variables by <b>Hensel lifting along an evaluation
    /// homomorphism</b>: factor the polynomial at a point, then lift that factorisation back
    /// one power of the auxiliary variable at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="KroneckerFactorization"/> reaches the same answers by a different route and
    /// leaves a gap this fills. Its one-variable image has degree <c>Π (d_i + 1) - 1</c>, and —
    /// worse than the size — the image <i>over-factors</i>: <c>x^7 - y^7</c> maps to
    /// <c>t^7 (1 - t^49)</c>, whose factors are cyclotomic, so the recombination is exponential
    /// in a count the substitution itself inflated. An evaluation image does not inflate
    /// anything: <c>x^7 - y^7</c> at <c>y = 1</c> is <c>x^7 - 1</c>, which has the two factors
    /// the answer has.
    /// </para>
    /// <para>
    /// <b>The lift.</b> Write <c>g(x, y) = f(x, y + a)</c> so that the point is the origin.
    /// <c>g(x, 0)</c> factors over the integers as <c>u_1 · … · u_r</c>, and a factorisation
    /// modulo <c>y^k</c> is pushed to <c>y^(k+1)</c> by solving <c>α·v + β·u = e</c> for the
    /// error <c>e</c> — which the Bezout pair of <c>u</c> and <c>v</c> answers immediately,
    /// since a square-free image makes them coprime. Reducing <c>α</c> below the degree of
    /// <c>u</c> and pushing the quotient into <c>β</c> keeps each side at the degree it started
    /// at. Lifted as far as the degree of <c>g</c> in <c>y</c>, a true factor is reached
    /// exactly rather than approximately, because it has no higher power to hide in.
    /// </para>
    /// <para>
    /// <b>What is restricted, and why it is the honest restriction rather than a convenient
    /// one.</b> The leading coefficient in the main variable must be constant. Where it is a
    /// polynomial in <c>y</c>, the lifted factors' leading coefficients have to be *known in
    /// advance* to keep them polynomials rather than power series — Wang's leading-coefficient
    /// problem — and the usual answer is to factor that coefficient and distribute it, which is
    /// a second algorithm on top of this one. Declining is a refusal, and refusing is something
    /// this layer already does.
    /// </para>
    /// <para>
    /// <b>Nothing here is trusted.</b> Every candidate is checked by exact division of the
    /// original, so a mistake anywhere above — a bad point, a lift that drifted, a recombination
    /// that is not a factor — costs a refusal and cannot cost a wrong answer.
    /// </para>
    /// </remarks>
    internal static class BivariateHenselFactorization
    {
        /// <summary>
        /// Evaluation points tried, in order. Small keeps the image's coefficients small, which
        /// is what the one-variable factoriser's own bound is about.
        /// </summary>
        [ConstantField] private static readonly int[] Points = { 0, 1, -1, 2, -2, 3, -3, 4, -4, 5 };

        /// <summary>
        /// Past this the recombination is refused rather than paid for: it is over subsets of
        /// the image's irreducible factors. The evaluation image does not over-factor the way a
        /// substituted one does, so this is reached far less often than
        /// <c>KroneckerFactorization.MaxImageFactors</c> is.
        /// </summary>
        private const int MaxImageFactors = 10;

        /// <summary>
        /// How much degree the monicisation may add before it is refused instead. See the note
        /// where it is used: past this the cost is seconds rather than milliseconds.
        /// </summary>
        private const int MaxMonicGrowth = 12;

        /// <summary>
        /// The factors of <paramref name="poly"/> in <paramref name="main"/> and
        /// <paramref name="other"/>, each of positive degree in <paramref name="main"/>, or
        /// <see langword="null"/> where nothing could be settled. A single factor means it does
        /// not factor, which is an answer.
        /// </summary>
        internal static IReadOnlyList<MultivariatePolynomial>? Factor(
            MultivariatePolynomial poly, int main, int other)
        {
            var degreeInMain = poly.DegreeIn(main);
            var degreeInOther = poly.DegreeIn(other);
            if (degreeInMain < 1 || degreeInOther < 1)
                return null;

            // A leading coefficient that is a polynomial is made into one that is not, and the
            // factorisation of that is mapped back. See MonicIn.
            if (!poly.LeadingCoefficientIn(main).IsConstant)
                return ByMonicisation(poly, main, other, degreeInMain);

            // A factor free of the main variable is not something this finds, and its presence
            // would make the image's factorisation say the wrong thing about degrees.
            if (PolynomialGcd.ContentIn(poly, main, new[] { other }, 0) is not { IsConstant: true })
                return null;

            if (ByYPower(poly, main, other, degreeInOther) is not { } coefficients)
                return null;

            foreach (var point in Points)
            {
                if (Shift(coefficients, point) is not { } shifted)
                    continue;
                if (Attempt(poly, shifted, main, other, point, degreeInMain, degreeInOther) is { } factors)
                    return factors;
            }
            return null;
        }

        /// <summary>
        /// The factorisation of a polynomial whose leading coefficient in
        /// <paramref name="main"/> is not a constant, by making it one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is <b>Wang's leading-coefficient problem</b>, and the answer here is not Wang's.
        /// His is to factor the leading coefficient and distribute its factors among the lifted
        /// ones, which needs a way to tell which goes where. The older answer needs none:
        /// <c>g(z, y) = L^(n-1) · f(z/L, y) = Σ a_i L^(n-1-i) z^i</c> is a polynomial, because
        /// <c>n - 1 - i</c> is never negative below the leading term, and it is <b>monic</b> in
        /// <c>z</c>, because the leading term contributes <c>L · L^(-1)</c>. A monic polynomial
        /// has a constant leading coefficient, which is the case the lift already handles.
        /// </para>
        /// <para>
        /// A factor <c>h(z, y)</c> of the monic form comes back as <c>h(L·x, y)</c> with its
        /// content in <c>y</c> divided out — the substitution multiplies the coefficient of
        /// <c>z^j</c> by <c>L^j</c>, and the content is what that puts in and the original never
        /// had.
        /// </para>
        /// <para>
        /// The cost is that <c>deg_y g</c> grows by up to <c>(n-1)·deg_y L</c>, so the lift is
        /// deeper and the polynomial larger. Nothing is asserted about staying inside
        /// <see cref="MultivariatePolynomial"/>'s bounds: the construction returns
        /// <see langword="null"/> when it leaves them, which is a refusal.
        /// </para>
        /// </remarks>
        private static IReadOnlyList<MultivariatePolynomial>? ByMonicisation(
            MultivariatePolynomial poly, int main, int other, int degreeInMain)
        {
            var leading = poly.LeadingCoefficientIn(main);
            if (leading.IsZero)
                return null;

            // The monic form carries L^(n-1-i), so its degree in the auxiliary variable exceeds
            // the original's by up to (n-1)·deg L, and the lift is that much deeper. Measured on
            // the cases this reaches: a growth of 6 costs 376 ms and a growth of 7 costs 1 ms,
            // while a growth of 14 costs **63 seconds** -- so it is bounded rather than paid
            // for, which is what the rest of this layer does with a cost it cannot predict.
            var growth = (degreeInMain - 1) * leading.DegreeIn(other);
            if (growth > MaxMonicGrowth)
                return null;

            if (MonicIn(poly, main, leading, degreeInMain) is not { } monic)
                return null;
            if (!monic.LeadingCoefficientIn(main).IsConstant)
                return null;

            // The monic form is a different polynomial, so it gets the whole treatment rather
            // than a shortcut into the lift: its own content, its own evaluation point.
            if (Factor(monic, main, other) is not { } factors || factors.Count < 2)
                return null;

            var found = new List<MultivariatePolynomial>();
            var remaining = poly;
            foreach (var factor in factors)
            {
                if (Substitute(factor, main, leading) is not { } mapped)
                    return null;
                if (PolynomialGcd.ContentIn(mapped, main, new[] { other }, 0) is not { } content)
                    return null;
                if (mapped.DivideExact(content) is not { } candidate)
                    return null;
                if (candidate.DegreeIn(main) < 1)
                    continue;
                if (remaining.DivideExact(candidate) is not { } quotient)
                    return null;
                found.Add(candidate.Normalized());
                remaining = quotient;
            }

            if (found.Count < 2)
                return null;
            if (!remaining.IsConstant)
                found.Add(remaining.Normalized());
            return found;
        }

        /// <summary>
        /// <c>Σ a_i · L^(n-1-i) · x^i</c> — the same polynomial with a leading coefficient of
        /// one, in a variable that stands for <c>x·L</c>.
        /// </summary>
        internal static MultivariatePolynomial? MonicIn(
            MultivariatePolynomial poly, int main, MultivariatePolynomial leading, int degreeInMain)
        {
            var result = MultivariatePolynomial.Zero(poly.VariableCount);
            foreach (var pair in poly.CoefficientsIn(main))
            {
                var power = degreeInMain - 1 - pair.Key;
                var term = pair.Value;
                if (power > 0)
                {
                    if (leading.Power(power) is not { } weight)
                        return null;
                    if (term.Multiply(weight) is not { } scaled)
                        return null;
                    term = scaled;
                }
                else if (power < 0)
                {
                    // Only the leading term, where L^(n-1-n) cancels the L the term carries.
                    if (term.DivideExact(leading) is not { } divided)
                        return null;
                    term = divided;
                }
                if (pair.Key > 0)
                {
                    if (term.ShiftedBy(main, pair.Key) is not { } shifted)
                        return null;
                    term = shifted;
                }
                result = result.Add(term);
            }
            return result;
        }

        /// <summary>
        /// <paramref name="poly"/> with <paramref name="main"/> replaced by
        /// <c><paramref name="main"/> · <paramref name="by"/></c> — so the coefficient of
        /// <c>x^j</c> is multiplied by <c>by^j</c>.
        /// </summary>
        internal static MultivariatePolynomial? Substitute(
            MultivariatePolynomial poly, int main, MultivariatePolynomial by)
        {
            var result = MultivariatePolynomial.Zero(poly.VariableCount);
            foreach (var pair in poly.CoefficientsIn(main))
            {
                var term = pair.Value;
                if (pair.Key > 0)
                {
                    if (by.Power(pair.Key) is not { } weight)
                        return null;
                    if (term.Multiply(weight) is not { } scaled)
                        return null;
                    if (scaled.ShiftedBy(main, pair.Key) is not { } shifted)
                        return null;
                    term = shifted;
                }
                result = result.Add(term);
            }
            return result;
        }

        /// <summary>
        /// One evaluation point: factor at it, lift, and recombine. <see langword="null"/> where
        /// this point does not settle it, which is not a statement about the polynomial.
        /// </summary>
        private static IReadOnlyList<MultivariatePolynomial>? Attempt(
            MultivariatePolynomial poly, RationalPolynomial[] shifted, int main, int other,
            int point, int degreeInMain, int degreeInOther)
        {
            var image = shifted[0];
            // The leading coefficient is a non-zero constant, so the degree cannot have fallen;
            // this says so rather than assuming it.
            if (image.Degree != degreeInMain)
                return null;
            // Square-free, so that the image's factors are pairwise coprime and the Bezout pair
            // the lift needs exists. Coprimality with the derivative is exactly that, and
            // TryBezout reports it as it computes the pair.
            if (!RationalPolynomial.TryBezout(image, Derivative(image), out _, out _))
                return null;

            if (ToInteger(image) is not { } whole)
                return null;
            if (PolynomialFactorization.FactorPrimitive(whole.PrimitivePart()) is not { } parts)
                return null;

            var irreducibles = new List<RationalPolynomial>();
            foreach (var part in parts)
            {
                // A square-free image has no repeated factor; anything else means the point was
                // a bad one rather than that the polynomial has one.
                if (part.Multiplicity != 1)
                    return null;
                if (irreducibles.Count == MaxImageFactors)
                    return null;
                irreducibles.Add(RationalPolynomial.FromInteger(part.Factor));
            }
            if (irreducibles.Count == 0)
                return null;
            if (irreducibles.Count == 1)
                return new[] { poly };

            // The image is the product of its primitive factors times its content, and the lift
            // needs a product that is the image exactly -- so the constant goes on the first.
            var product = irreducibles[0];
            for (var i = 1; i < irreducibles.Count; i++)
                product = product.Multiply(irreducibles[i]);
            if (!image.TryDivide(product, out var constant, out var remainder)
                || !remainder.IsZero || !constant.IsConstant)
                return null;
            irreducibles[0] = irreducibles[0].Multiply(constant);

            var lifted = Lift(shifted, irreducibles, degreeInOther + 1);
            if (lifted is null)
                return null;
            return Recombine(poly, lifted, main, other, point, degreeInOther + 1);
        }

        /// <summary>
        /// The image's factors lifted to agree with <paramref name="g"/> modulo
        /// <c>y^<paramref name="depth"/></c>, each a truncated series in <c>y</c> whose
        /// coefficients are polynomials in the main variable.
        /// </summary>
        /// <remarks>
        /// Several factors at once, by splitting off one at a time: lift the pair
        /// <c>(u_1, u_2 · … · u_r)</c>, then lift the second half against the rest. The pair
        /// step is where the work is.
        /// </remarks>
        private static List<RationalPolynomial[]>? Lift(
            RationalPolynomial[] g, IReadOnlyList<RationalPolynomial> factors, int depth)
        {
            var result = new List<RationalPolynomial[]>();
            var rest = g;
            for (var i = 0; i < factors.Count - 1; i++)
            {
                var others = factors[i + 1];
                for (var j = i + 2; j < factors.Count; j++)
                    others = others.Multiply(factors[j]);
                if (LiftPair(rest, factors[i], others, depth) is not { } pair)
                    return null;
                result.Add(pair.Left);
                rest = pair.Right;
            }
            result.Add(Truncate(rest, depth));
            return result;
        }

        /// <summary>
        /// <c>g = A·B</c> modulo <c>y^<paramref name="depth"/></c>, from
        /// <c>g(x, 0) = u·v</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Writing the next pair as <c>A + α y^k</c> and <c>B + β y^k</c> and asking that the
        /// product agree one power further leaves <c>α·v + β·u = e</c>, where <c>e</c> is the
        /// coefficient of <c>y^k</c> in the error so far. The Bezout pair <c>s·u + t·v = 1</c>
        /// answers it at once — <c>α = e·t</c>, <c>β = e·s</c> — and reducing <c>α</c> below the
        /// degree of <c>u</c>, pushing the quotient into <c>β</c>, is what keeps each side at
        /// the degree it started at. It is the same step as the p-adic lift in
        /// <c>PolynomialFactorization</c>, over <c>(y)</c> rather than over a prime.
        /// </para>
        /// <para>
        /// The Bezout pair is computed once: neither side changes modulo <c>y</c> as it is
        /// lifted, which is the whole reason the same pair keeps answering.
        /// </para>
        /// </remarks>
        private static (RationalPolynomial[] Left, RationalPolynomial[] Right)? LiftPair(
            RationalPolynomial[] g, RationalPolynomial u, RationalPolynomial v, int depth)
        {
            if (!RationalPolynomial.TryBezout(u, v, out var s, out var t))
                return null;

            var a = new RationalPolynomial[depth];
            var b = new RationalPolynomial[depth];
            for (var i = 0; i < depth; i++)
                a[i] = b[i] = RationalPolynomial.Zero;
            a[0] = u;
            b[0] = v;

            for (var k = 1; k < depth; k++)
            {
                // The coefficient of y^k in g - A·B, with A and B known below y^k.
                var error = k < g.Length ? g[k] : RationalPolynomial.Zero;
                for (var i = 0; i <= k; i++)
                    error = error.Subtract(a[i].Multiply(b[k - i]));
                if (error.IsZero)
                    continue;

                var alpha = error.Multiply(t);
                var beta = error.Multiply(s);
                if (!alpha.TryDivide(u, out var quotient, out var reduced))
                    return null;
                alpha = reduced;
                beta = beta.Add(quotient.Multiply(v));
                // Either side growing past the degree it started at means the lift has left the
                // factorisation it was following, and going on would only bury that.
                if (alpha.Degree >= u.Degree || beta.Degree >= v.Degree + 1)
                    return null;
                a[k] = alpha;
                b[k] = beta;
            }

            return (a, b);
        }

        private static RationalPolynomial[] Truncate(RationalPolynomial[] series, int depth)
        {
            var result = new RationalPolynomial[depth];
            for (var i = 0; i < depth; i++)
                result[i] = i < series.Length ? series[i] : RationalPolynomial.Zero;
            return result;
        }

        /// <summary>
        /// Every subset of the lifted factors, tried as a divisor of the original — which is
        /// what makes a mistake anywhere above cost a refusal rather than a wrong answer.
        /// </summary>
        /// <remarks>
        /// A subset that corresponds to a true factor multiplies to it exactly, because the lift
        /// went as far as the degree in <c>y</c> and a factor has no higher power to hide in.
        /// Subsets are walked smallest first so that the factorisation comes out in pieces
        /// rather than as the polynomial and a constant.
        /// </remarks>
        private static IReadOnlyList<MultivariatePolynomial>? Recombine(
            MultivariatePolynomial poly, List<RationalPolynomial[]> lifted,
            int main, int other, int point, int depth)
        {
            var found = new List<MultivariatePolynomial>();
            var remaining = poly;
            var used = 0;
            var count = lifted.Count;

            // Every subset as a bit pattern, smallest first, so that the factorisation comes out
            // in pieces rather than as the polynomial itself and a constant. Half of them is
            // enough: what is left over after a subset divides out is the complement.
            var masks = new List<int>();
            for (var mask = 1; mask < (1 << count) - 1; mask++)
                masks.Add(mask);
            masks.Sort((left, right) => PopCount(left).CompareTo(PopCount(right)));

            foreach (var mask in masks)
            {
                if ((mask & used) != 0)
                    continue;
                if (PopCount(mask) > count - PopCount(used) - 1)
                    continue;

                var product = new RationalPolynomial[depth];
                for (var i = 0; i < depth; i++)
                    product[i] = i == 0 ? RationalPolynomial.One : RationalPolynomial.Zero;
                for (var i = 0; i < count; i++)
                    if ((mask & (1 << i)) != 0)
                        product = MultiplySeries(product, lifted[i], depth);

                if (ToPolynomial(product, main, other, point, poly.VariableCount) is not { } candidate)
                    continue;
                if (candidate.IsConstant || candidate.DegreeIn(main) < 1)
                    continue;
                if (remaining.DivideExact(candidate) is not { } quotient)
                    continue;

                found.Add(candidate.Normalized());
                remaining = quotient;
                used |= mask;
            }

            if (found.Count == 0)
                return null;
            if (!remaining.IsConstant)
                found.Add(remaining.Normalized());
            return found.Count < 2 ? new[] { poly } : found;
        }

        private static int PopCount(int value)
        {
            var count = 0;
            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }
            return count;
        }

        private static RationalPolynomial[] MultiplySeries(
            RationalPolynomial[] left, RationalPolynomial[] right, int depth)
        {
            var result = new RationalPolynomial[depth];
            for (var i = 0; i < depth; i++)
                result[i] = RationalPolynomial.Zero;
            for (var i = 0; i < depth; i++)
            {
                if (left[i].IsZero)
                    continue;
                for (var j = 0; i + j < depth; j++)
                    if (!right[j].IsZero)
                        result[i + j] = result[i + j].Add(left[i].Multiply(right[j]));
            }
            return result;
        }

        /// <summary>
        /// A lifted series back to a polynomial in the two variables, with the evaluation point
        /// undone — the lift worked in <c>y + point</c>, so the answer is in <c>y - point</c>.
        /// </summary>
        private static MultivariatePolynomial? ToPolynomial(
            RationalPolynomial[] series, int main, int other, int point, int variableCount)
        {
            if (Shift(series, -point) is not { } unshifted)
                return null;
            var result = MultivariatePolynomial.Zero(variableCount);
            for (var k = 0; k < unshifted.Length; k++)
            {
                if (unshifted[k].IsZero)
                    continue;
                for (var i = 0; i <= unshifted[k].Degree; i++)
                {
                    var coefficient = unshifted[k][i];
                    if (coefficient.IsZero)
                        continue;
                    var term = MultivariatePolynomial.Constant(variableCount, coefficient);
                    if (i > 0)
                    {
                        if (term.ShiftedBy(main, i) is not { } withMain)
                            return null;
                        term = withMain;
                    }
                    if (k > 0)
                    {
                        if (term.ShiftedBy(other, k) is not { } withOther)
                            return null;
                        term = withOther;
                    }
                    result = result.Add(term);
                }
            }
            return result;
        }

        /// <summary>
        /// <paramref name="poly"/> as coefficients of powers of <paramref name="other"/>, each a
        /// polynomial in <paramref name="main"/> alone.
        /// </summary>
        private static RationalPolynomial[]? ByYPower(
            MultivariatePolynomial poly, int main, int other, int degreeInOther)
        {
            var result = new RationalPolynomial[degreeInOther + 1];
            for (var i = 0; i < result.Length; i++)
                result[i] = RationalPolynomial.Zero;
            foreach (var pair in poly.CoefficientsIn(other))
            {
                if (pair.Key >= result.Length)
                    return null;
                if (ToUnivariate(pair.Value, main) is not { } coefficient)
                    return null;
                result[pair.Key] = coefficient;
            }
            return result;
        }

        /// <summary>A polynomial in one variable only, as a one-variable polynomial.</summary>
        private static RationalPolynomial? ToUnivariate(MultivariatePolynomial poly, int main)
        {
            var degree = poly.DegreeIn(main);
            var coefficients = new ERational[degree + 1];
            for (var i = 0; i < coefficients.Length; i++)
                coefficients[i] = ERational.Zero;
            foreach (var pair in poly.CoefficientsIn(main))
            {
                if (pair.Key >= coefficients.Length)
                    return null;
                if (pair.Value.IsZero)
                    continue;
                if (!pair.Value.IsConstant)
                    return null;   // a variable other than the two this works in
                coefficients[pair.Key] = pair.Value.CoefficientOf(0);
            }
            return RationalPolynomial.Create(coefficients);
        }

        /// <summary>
        /// The same polynomial written in <c>y + point</c> — so that the evaluation point
        /// becomes the origin and the ideal to lift along is <c>(y)</c>.
        /// </summary>
        private static RationalPolynomial[]? Shift(RationalPolynomial[] coefficients, int point)
        {
            if (point == 0)
                return (RationalPolynomial[])coefficients.Clone();
            var result = new RationalPolynomial[coefficients.Length];
            for (var i = 0; i < result.Length; i++)
                result[i] = RationalPolynomial.Zero;
            var at = EInteger.FromInt32(point);
            for (var j = 0; j < coefficients.Length; j++)
            {
                if (coefficients[j].IsZero)
                    continue;
                // (y + a)^j expanded, so the coefficient of y^j moves to every y^k below it
                // weighted by C(j, k) a^(j-k).
                for (var k = 0; k <= j; k++)
                {
                    var weight = ERational.Create(Binomial(j, k).Multiply(at.Pow(j - k)), EInteger.One);
                    result[k] = result[k].Add(coefficients[j].ScaleBy(weight));
                }
            }
            return result;
        }

        private static EInteger Binomial(int n, int k)
        {
            var result = EInteger.One;
            for (var i = 0; i < k; i++)
                result = result.Multiply(EInteger.FromInt32(n - i)).Divide(EInteger.FromInt32(i + 1));
            return result;
        }

        private static RationalPolynomial Derivative(RationalPolynomial poly)
        {
            if (poly.Degree < 1)
                return RationalPolynomial.Zero;
            var coefficients = new ERational[poly.Degree];
            for (var i = 1; i <= poly.Degree; i++)
                coefficients[i - 1] = poly[i].Multiply(ERational.FromInt32(i));
            return RationalPolynomial.Create(coefficients);
        }

        private static IntegerPolynomial? ToInteger(RationalPolynomial poly)
        {
            var denominator = EInteger.One;
            for (var i = 0; i <= poly.Degree; i++)
                denominator = Lcm(denominator, poly[i].Denominator);
            var whole = new EInteger[poly.Degree + 1];
            for (var i = 0; i < whole.Length; i++)
                whole[i] = poly[i].Numerator.Multiply(denominator.Divide(poly[i].Denominator));
            return IntegerPolynomial.Create(whole);
        }

        private static EInteger Lcm(EInteger left, EInteger right)
            => left.IsZero || right.IsZero ? EInteger.One
                : left.Divide(left.GreatestCommonDivisor(right)).Multiply(right);
    }
}
