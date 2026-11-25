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
    /// Factorisation of a polynomial in any number of variables, by reducing it to one variable
    /// and putting the answer back.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Kronecker's substitution, in mixed radix.</b> A factor of a polynomial has degree at
    /// most <c>d_i</c> in each variable <c>v_i</c>, because a factor divides it. So with radices
    /// <c>d_i + 1</c> and place values <c>s_0 = 1</c>, <c>s_(i+1) = s_i * (d_i + 1)</c>, the map
    /// <c>v_0^e_0 · … · v_(k-1)^e_(k-1) -> t^(Σ e_i · s_i)</c> writes each exponent as one digit
    /// of a numeral and is therefore injective on every monomial that can appear in the
    /// polynomial or in any of its factors. A factorisation of the one-variable image can be read
    /// back digit by digit, and each subset of its irreducible factors names a candidate.
    /// </para>
    /// <para>
    /// <b>A candidate is a guess and is checked by division.</b> The image can factor further
    /// than the polynomial does — the substitution is injective on monomials, not on
    /// factorisations — so a subset whose product reads back as a polynomial need not divide the
    /// original. Every one is tested with exact division before it is kept, which is why this
    /// cannot answer wrongly: the worst it does is fail to find a factorisation that exists and
    /// say so.
    /// </para>
    /// <para>
    /// <b>What it will not do.</b> The image has degree <c>Π (d_i + 1) - 1</c>, a *product* and
    /// not a sum, and the one-variable factoriser stops at
    /// <see cref="IntegerPolynomial.MaxDegree"/> — so the ceiling closes quickly as variables are
    /// added. Two variables reach bidegrees like (2, 10), (3, 7) and (5, 4); three variables of
    /// degree 2 fit (27 ≤ 32) and four do not (81); and a quadratic in eight variables is far
    /// past it. The recombination is over subsets, so the number of irreducible factors of the
    /// image is capped as well. Both limits are refusals, never wrong answers.
    /// </para>
    /// <para>
    /// <b>Raising the degree ceiling is not what would lift them, and that was measured rather
    /// than assumed.</b> <see cref="IntegerPolynomial.MaxDegree"/> is 32 while the machinery
    /// underneath affords 64 (<see cref="PrimeFieldFactorization.MaxDegree"/>), which looks like
    /// a free doubling of the exponent budget — a whole variable's worth of reach, since the
    /// budget is a product. Doubled, <b>nothing new factors</b>: <c>x^7 - y^7</c>,
    /// <c>x^6 - y^6</c>, <c>x^3 - (y + z)^3</c>, <c>(x^8 + y)(x^8 + 3y)</c> and
    /// <c>(x^2 + y^5)(x^2 - y^5)</c> all still refuse, at images of degree 34 to 56 that are
    /// inside the raised ceiling.
    /// </para>
    /// <para>
    /// The reason is the method rather than the bound: <b>the substitution's images over-factor
    /// badly</b>. A two-factor bivariate maps to a one-variable polynomial that splits into many
    /// irreducibles — <c>x^7 - y^7</c> becomes <c>t^7 (1 - t^49)</c>, whose factors are
    /// cyclotomic — so the recombination is exponential in a factor count the substitution itself
    /// inflates. That is exactly the cost <see cref="IntegerPolynomial.MaxDegree"/> is set to
    /// bound, and moving it only moves where the refusal comes from. Lifting these limits means
    /// not inflating the factor count in the first place, which is <b>Hensel lifting with an
    /// evaluation homomorphism</b> — a different algorithm, not a larger number
    /// (<a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> item 43).
    /// </para>
    /// </remarks>
    internal static class KroneckerFactorization
    {
        /// <summary>
        /// Beyond this the subset search is refused rather than paid for: the recombination is
        /// over every subset of the image's irreducible factors, and a polynomial this reducible
        /// is not what the substitution is for.
        /// </summary>
        private const int MaxImageFactors = 12;

        /// <summary>
        /// The factors of <paramref name="poly"/> in <paramref name="main"/> and whatever other
        /// variables it has, each to the first power and each of positive degree in
        /// <paramref name="main"/>, or <see langword="null"/> where nothing could be settled. A
        /// single factor means the polynomial did not factor, which is an answer.
        /// </summary>
        internal static IReadOnlyList<MultivariatePolynomial>? Factor(
            MultivariatePolynomial poly, int main)
        {
            if (poly.IsZero || poly.DegreeIn(main) < 1)
                return null;
            if (BySubstitution(poly, main) is { } factors)
                return factors;
            // The substitution's image over-factors -- `x^7 - y^7` maps to `t^7 (1 - t^49)`,
            // whose factors are cyclotomic -- so its recombination is exponential in a count it
            // inflated itself. Lifting an *evaluation* image inflates nothing, and reaches the
            // shapes the remark above names as refused.
            if (OnlyOther(poly, main) is { } other)
            {
                if (BivariateHenselFactorization.Factor(poly, main, other) is { } lifted)
                    return lifted;
            }
            else if (ByLiftingTheOthersAsOne(poly, main) is { } collapsed)
                return collapsed;
            // The substitution gave up -- on the image's degree, on how far it split, or on the
            // arithmetic. An *evaluation* image has the degree of the polynomial in the main
            // variable however many other variables there are, so it is still there to be asked,
            // and what it can settle is that there is nothing to find. Asked second because it
            // answers only that: the substitution is the one that can actually factor.
            return EvaluationHomomorphism.CertifiesIrreducible(poly, main)
                ? new[] { poly }
                : null;
        }

        /// <summary>
        /// The one variable other than <paramref name="main"/> that <paramref name="poly"/>
        /// actually uses, or <see langword="null"/> where it uses none or several — the lift
        /// works along one evaluation at a time.
        /// </summary>
        private static int? OnlyOther(MultivariatePolynomial poly, int main)
        {
            int? only = null;
            for (var variable = 0; variable < poly.VariableCount; variable++)
            {
                if (variable == main || poly.DegreeIn(variable) == 0)
                    continue;
                if (only is not null)
                    return null;
                only = variable;
            }
            return only;
        }

        /// <summary>
        /// The most the image's degree in the variable standing for all the others may be:
        /// what one exponent of <see cref="MultivariatePolynomial"/> holds. The lift goes that
        /// deep, and measured at the top of it the cost is milliseconds — <c>x^10 - (y+z)^10</c>,
        /// an image of degree 120, factors in 60 ms and <c>x^8 - (y z)^8</c>, at 80, in 170 ms —
        /// so the representation is the bound and nothing smaller is needed.
        /// </summary>
        private const int MaxCollapsedDegree = MultivariatePolynomial.MaxDegree;

        /// <summary>
        /// The factorisation of a polynomial in three or more variables, by Kronecker's
        /// substitution among the variables other than <paramref name="main"/> and the lift in
        /// two variables on what that leaves — or <see langword="null"/> where it could not be
        /// settled. A single factor means the polynomial does not factor, which is an answer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The substitution over-factors when it takes in the main variable, which is why the
        /// whole of it is refused past a small degree. Taken over the others only, it leaves the
        /// main variable alone: <c>y -> t</c>, <c>z -> t^(d_y + 1)</c>, and so on in mixed radix,
        /// is a ring homomorphism into polynomials in the main variable and <c>t</c>, and it is
        /// injective on every monomial a factor can have, since a factor's degree in each
        /// variable is at most the polynomial's. The image is then a polynomial in two
        /// variables, which <see cref="BivariateHenselFactorization"/> factors by lifting an
        /// evaluation image — and an evaluation image does not over-factor the way a substituted
        /// one does.
        /// </para>
        /// <para>
        /// A factorisation of the polynomial maps to one of the image, each factor keeping its
        /// degree in the main variable; so an image that does not factor is a proof that the
        /// polynomial does not either, its content in the other variables having been taken
        /// out by the caller and checked again by the lift. The converse does not hold — the
        /// image may split further — so the image's factors are recombined, read back digit by
        /// digit, and each candidate is kept only if it divides the original exactly.
        /// </para>
        /// </remarks>
        private static IReadOnlyList<MultivariatePolynomial>? ByLiftingTheOthersAsOne(
            MultivariatePolynomial poly, int main)
        {
            var others = new List<int>();
            for (var variable = 0; variable < poly.VariableCount; variable++)
                if (variable != main && poly.DegreeIn(variable) > 0)
                    others.Add(variable);
            if (others.Count < 2)
                return null;

            var degrees = new int[others.Count];
            var places = new int[others.Count];
            long size = 1;
            for (var i = 0; i < others.Count; i++)
            {
                degrees[i] = poly.DegreeIn(others[i]);
                places[i] = (int)size;
                size *= degrees[i] + 1;
                if (size - 1 > MaxCollapsedDegree)
                    return null;
            }

            // The first of the others stands for all of them, so the image lives in the same
            // number of variables and the lift needs nothing new.
            var collapsed = others[0];
            var image = MultivariatePolynomial.Zero(poly.VariableCount);
            foreach (var term in poly.Terms)
            {
                var power = 0;
                for (var i = 0; i < others.Count; i++)
                    power += MultivariatePolynomial.PowerOf(term.Key, others[i]) * places[i];
                if (Monomial(poly.VariableCount, term.Value,
                        (main, MultivariatePolynomial.PowerOf(term.Key, main)), (collapsed, power))
                    is not { } monomial)
                    return null;
                image = image.Add(monomial);
            }

            if (BivariateHenselFactorization.Factor(image, main, collapsed) is not { } lifted)
                return null;
            if (lifted.Count < 2)
                return new[] { poly };
            if (lifted.Count > MaxImageFactors)
                return null;

            var found = new List<MultivariatePolynomial>();
            var remaining = poly;
            var used = 0;
            var count = lifted.Count;
            var masks = new List<int>();
            for (var mask = 1; mask < (1 << count) - 1; mask++)
                masks.Add(mask);
            masks.Sort((left, right) => PopCount(left).CompareTo(PopCount(right)));

            foreach (var mask in masks)
            {
                if ((mask & used) != 0)
                    continue;
                var product = MultivariatePolynomial.One(poly.VariableCount);
                for (var i = 0; i < count && product is not null; i++)
                    if ((mask & (1 << i)) != 0)
                        product = product.Multiply(lifted[i], MultivariatePolynomial.MaxIntermediateTerms);
                if (product is null)
                    return null;
                if (ReadBack(product, main, collapsed, others, degrees, places) is not { } candidate)
                    continue;
                if (candidate.DegreeIn(main) < 1)
                    continue;
                if (remaining.DivideExact(candidate) is not { } quotient)
                    continue;
                found.Add(candidate.Normalized());
                remaining = quotient;
                used |= mask;
            }

            // Nothing divided: the image split where the polynomial may not have, and no
            // grouping of its factors read back to one that divides. That settles nothing.
            if (found.Count == 0)
                return null;
            if (!remaining.IsConstant)
                found.Add(remaining.Normalized());
            return found.Count < 2 ? new[] { poly } : found;
        }

        /// <summary>
        /// A polynomial in the main variable and the collapsed one, with each power of the
        /// collapsed variable read back as the monomial in the others whose mixed-radix numeral
        /// it is — or <see langword="null"/> where a power is past what the polynomial's own
        /// degrees allow, so that it is not the image of anything that could divide it.
        /// </summary>
        private static MultivariatePolynomial? ReadBack(
            MultivariatePolynomial image, int main, int collapsed,
            IReadOnlyList<int> others, IReadOnlyList<int> degrees, IReadOnlyList<int> places)
        {
            var result = MultivariatePolynomial.Zero(image.VariableCount);
            var digits = new (int Variable, int Power)[others.Count + 1];
            foreach (var term in image.Terms)
            {
                var power = MultivariatePolynomial.PowerOf(term.Key, collapsed);
                digits[0] = (main, MultivariatePolynomial.PowerOf(term.Key, main));
                for (var i = others.Count - 1; i >= 0; i--)
                {
                    var digit = power / places[i];
                    if (digit > degrees[i])
                        return null;
                    digits[i + 1] = (others[i], digit);
                    power -= digit * places[i];
                }
                if (Monomial(image.VariableCount, term.Value, digits) is not { } monomial)
                    return null;
                result = result.Add(monomial);
            }
            return result;
        }

        private static MultivariatePolynomial? Monomial(
            int variableCount, ERational coefficient, params (int Variable, int Power)[] powers)
        {
            var result = MultivariatePolynomial.Constant(variableCount, coefficient);
            foreach (var (variable, power) in powers)
                if (result.ShiftedBy(variable, power) is not { } shifted)
                    return null;
                else
                    result = shifted;
            return result;
        }

        private static int PopCount(int value)
        {
            var count = 0;
            for (; value != 0; value >>= 1)
                count += value & 1;
            return count;
        }

        /// <summary>
        /// The factorisation by Kronecker's substitution, or <see langword="null"/> where it
        /// could not be reached.
        /// </summary>
        private static IReadOnlyList<MultivariatePolynomial>? BySubstitution(
            MultivariatePolynomial poly, int main)
        {

            // The main variable is placed first so that its exponent is the lowest digit; the
            // rest follow in index order, and a variable the polynomial does not use is left out
            // rather than given a radix of one.
            var order = new List<int> { main };
            for (var variable = 0; variable < poly.VariableCount; variable++)
                if (variable != main && poly.DegreeIn(variable) > 0)
                    order.Add(variable);
            if (order.Count < 2)
                return null;

            var radices = new int[order.Count];
            var places = new int[order.Count];
            long size = 1;
            for (var i = 0; i < order.Count; i++)
            {
                radices[i] = poly.DegreeIn(order[i]) + 1;
                places[i] = (int)size;
                size *= radices[i];
                if (size > IntegerPolynomial.MaxDegree + 1)
                    return null;
            }
            var imageDegree = (int)size - 1;

            if (ToImage(poly, order, places, radices, imageDegree) is not { } image)
                return null;
            if (PolynomialFactorization.FactorPrimitive(image.PrimitivePart()) is not { } parts)
                return null;

            // The multiplicities are flattened: a square in the image may or may not be a square
            // in the original, and the recombination settles that by division rather than by
            // carrying the exponent across the substitution.
            var irreducibles = new List<IntegerPolynomial>();
            foreach (var part in parts)
                for (var i = 0; i < part.Multiplicity; i++)
                {
                    if (irreducibles.Count == MaxImageFactors)
                        return null;
                    irreducibles.Add(part.Factor);
                }
            if (irreducibles.Count < 2)
                return new[] { poly };

            return Recombine(poly, irreducibles, main, order, places, radices);
        }

        /// <summary>
        /// The one-variable image, with the denominators cleared — the constant they came to is
        /// not wanted, since a rational multiple of a factor is the same factor.
        /// </summary>
        private static IntegerPolynomial? ToImage(
            MultivariatePolynomial poly, IReadOnlyList<int> order,
            IReadOnlyList<int> places, IReadOnlyList<int> radices, int imageDegree)
        {
            var coefficients = new ERational[imageDegree + 1];
            for (var i = 0; i < coefficients.Length; i++)
                coefficients[i] = ERational.Zero;

            if (!Peel(poly, 0, 0))
                return null;

            var denominator = EInteger.One;
            foreach (var coefficient in coefficients)
                denominator = Lcm(denominator, coefficient.Denominator);
            var whole = new EInteger[coefficients.Length];
            for (var i = 0; i < whole.Length; i++)
                whole[i] = coefficients[i].Numerator
                    .Multiply(denominator.Divide(coefficients[i].Denominator));
            return IntegerPolynomial.Create(whole);

            // One variable at a time, adding that exponent's digit to the place already
            // accumulated. What is left when every variable has been peeled has to be a constant.
            bool Peel(MultivariatePolynomial rest, int depth, int at)
            {
                if (depth == order.Count)
                {
                    if (!rest.IsConstant)
                        return false;
                    coefficients[at] = coefficients[at].Add(rest.CoefficientOf(0));
                    return true;
                }
                foreach (var pair in rest.CoefficientsIn(order[depth]))
                {
                    if (pair.Key >= radices[depth])
                        return false;
                    if (!Peel(pair.Value, depth + 1, at + places[depth] * pair.Key))
                        return false;
                }
                return true;
            }
        }

        private static EInteger Lcm(EInteger left, EInteger right)
            => left.Divide(left.GreatestCommonDivisor(right)).Multiply(right);

        /// <summary>
        /// Reading each exponent back as one digit of the numeral — or <see langword="null"/>
        /// where a power is past what a packed monomial holds.
        /// </summary>
        private static MultivariatePolynomial? FromImage(
            IntegerPolynomial image, int variableCount, IReadOnlyList<int> order,
            IReadOnlyList<int> places, IReadOnlyList<int> radices)
        {
            var result = MultivariatePolynomial.Zero(variableCount);
            for (var power = 0; power <= image.Degree; power++)
            {
                if (image[power].IsZero)
                    continue;
                var monomial = MultivariatePolynomial.One(variableCount);
                for (var i = 0; i < order.Count; i++)
                {
                    var digit = power / places[i] % radices[i];
                    if (digit > MultivariatePolynomial.MaxDegree)
                        return null;
                    if (MultivariatePolynomial.Monomial(variableCount, order[i]).Power(digit)
                            is not { } part
                        || monomial.Multiply(part) is not { } multiplied)
                        return null;
                    monomial = multiplied;
                }
                result = result.Add(monomial.ScaleBy(ERational.Create(image[power], EInteger.One)));
            }
            return result.IsZero ? null : result;
        }

        /// <summary>
        /// Every subset of the image's irreducible factors, smallest first, kept where its
        /// product divides what is left of the polynomial.
        /// </summary>
        /// <remarks>
        /// Smallest first so that what is taken out is irreducible: a subset that divides and
        /// whose proper subsets do not is a factor with nothing inside it. The loop restarts
        /// after each success because the remaining polynomial has changed.
        /// </remarks>
        private static IReadOnlyList<MultivariatePolynomial>? Recombine(
            MultivariatePolynomial poly, List<IntegerPolynomial> irreducibles, int main,
            IReadOnlyList<int> order, IReadOnlyList<int> places, IReadOnlyList<int> radices)
        {
            var found = new List<MultivariatePolynomial>();
            var remaining = poly;
            var available = new List<IntegerPolynomial>(irreducibles);
            var everyDivisionSettled = true;

            var progress = true;
            while (progress && available.Count > 0)
            {
                progress = false;
                for (var size = 1; size <= available.Count / 2 && !progress; size++)
                    foreach (var subset in Subsets(available.Count, size))
                    {
                        var product = IntegerPolynomial.One;
                        foreach (var index in subset)
                            if (product.Multiply(available[index]) is { } multiplied)
                                product = multiplied;
                            else
                                return null;
                        if (FromImage(product, poly.VariableCount, order, places, radices)
                                is not { } candidate)
                            return null;
                        // A candidate of degree zero in the main variable would be content, and
                        // the content is taken out before this runs -- so skipping it discards
                        // nothing a factorisation could have used.
                        if (candidate.DegreeIn(main) < 1)
                            continue;
                        if (remaining.DivideExact(candidate, out var settled) is not { } quotient)
                        {
                            // "Does not divide" and "ran out of room" are the same null, and
                            // only the first of them is evidence.
                            everyDivisionSettled &= settled;
                            continue;
                        }
                        found.Add(candidate.Normalized());
                        remaining = quotient;
                        for (var i = subset.Count - 1; i >= 0; i--)
                            available.RemoveAt(subset[i]);
                        progress = true;
                        break;
                    }
            }

            // Nothing recombined. Every subset of the image's irreducible factors was tried, so
            // a factorisation of the polynomial into two parts of positive degree in the main
            // variable would have been found: the substitution is a ring homomorphism on these
            // monomials, so such a factorisation maps to a splitting of the image, and every
            // splitting of the image is one of the subsets. The polynomial is irreducible, and
            // saying so is an answer -- but only where every division was settled, since a
            // division that ran out of room is not a "does not divide".
            if (found.Count == 0)
                return everyDivisionSettled ? new[] { poly } : null;
            if (!remaining.IsConstant)
                found.Add(remaining.Normalized());
            // Nothing is returned that does not multiply back to what was asked about.
            var check = MultivariatePolynomial.One(poly.VariableCount);
            foreach (var factor in found)
                if (check.Multiply(factor) is { } multiplied)
                    check = multiplied;
                else
                    return null;
            return DividesBackExactly(check, poly) ? found : null;
        }

        /// <summary>
        /// Whether the factors multiply back to the polynomial up to a rational constant, which
        /// is as far as a factorisation is ever fixed.
        /// </summary>
        private static bool DividesBackExactly(MultivariatePolynomial product, MultivariatePolynomial poly)
            => !product.IsZero
               && poly.DivideExact(product) is { } quotient
               && quotient.IsConstant;

        /// <summary>The index subsets of a given size, in a fixed order.</summary>
        private static IEnumerable<List<int>> Subsets(int count, int size)
        {
            var chosen = new List<int>(size);
            return Walk(0);

            IEnumerable<List<int>> Walk(int from)
            {
                if (chosen.Count == size)
                {
                    yield return new List<int>(chosen);
                    yield break;
                }
                for (var index = from; index < count; index++)
                {
                    chosen.Add(index);
                    foreach (var subset in Walk(index + 1))
                        yield return subset;
                    chosen.RemoveAt(chosen.Count - 1);
                }
            }
        }
    }
}
