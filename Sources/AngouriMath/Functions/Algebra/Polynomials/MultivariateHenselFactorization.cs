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
    /// Factorisation in three or more variables by <b>Hensel lifting one variable at a
    /// time</b>, after Wang: factor at a point, lift the factorisation to two variables with
    /// <see cref="BivariateHenselFactorization"/>, then bring back each remaining variable in
    /// turn.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>What this adds to the collapse.</b> <c>KroneckerFactorization</c> also reaches three
    /// or more variables, by substituting all the others into one in mixed radix and lifting
    /// the two-variable image. That image's degree is a <i>product</i> of the degrees, and past
    /// what one exponent holds it is refused. Lifting one variable at a time costs a
    /// <i>sum</i>: each variable is lifted to its own degree and no further, so
    /// <c>x ^ 7 - (y + z ^ 3) ^ 7</c>, whose collapsed image has degree 175, is reached here at
    /// degrees 7 and 21.
    /// </para>
    /// <para>
    /// <b>The lift.</b> The other variables are moved so that the point is the origin:
    /// <c>g = f(x, y_1 + a_1, …, y_k + a_k)</c>. The slice <c>g(x, y_1, 0, …, 0)</c> is
    /// factored by the two-variable lift, which also does the recombination, so its factors
    /// are true ones. Then for each further variable <c>y_j</c> the factors are pushed from
    /// agreeing with <c>g</c> at <c>y_j = 0</c> to agreeing in every power of <c>y_j</c>: the
    /// coefficient of <c>y_j ^ m</c> in the error is written as <c>Σ σ_i · Π_(l≠i) F_l</c>,
    /// with each <c>σ_i</c> of lower degree in <c>x</c> than <c>F_i</c>, and <c>σ_i y_j ^ m</c>
    /// is added to <c>F_i</c>. That equation is in the variables already lifted, and it is
    /// solved the same way one variable down, until at the bottom it is in <c>x</c> alone and
    /// the Bezout pairs of the image's factors answer it.
    /// </para>
    /// <para>
    /// <b>Why a single image factor is a proof.</b> The leading coefficient in <c>x</c> is a
    /// constant and the content in the other variables has been taken out. A factorisation
    /// <c>g = A · B</c> with both of positive degree in <c>x</c> then survives setting any
    /// variable to zero, each side keeping its degree in <c>x</c>; so a slice that does not
    /// factor proves that <c>g</c> does not, and <c>g</c> is the input moved by a ring
    /// automorphism.
    /// </para>
    /// <para>
    /// <b>A leading coefficient that is a polynomial</b> — Wang's leading-coefficient
    /// problem — is answered the way the two-variable lift answers it, by asking about the
    /// monic <c>L ^ (n - 1) · f(z / L, …)</c> instead; see <see cref="ByMonicisation"/>.
    /// </para>
    /// <para>
    /// <b>What is refused.</b> A monic form that adds more than
    /// <see cref="MaxMonicGrowth"/> to the degrees; a polynomial with a repeated factor, whose
    /// images are never square-free; and a slice that splits further than the polynomial
    /// does, for every point tried. The lift cannot follow a factor of the slice that is not
    /// the image of one of the polynomial, so it finds that the product it built is not the
    /// polynomial and the point is given up. Every factor is checked by exact division before
    /// it is returned.
    /// </para>
    /// </remarks>
    internal static class MultivariateHenselFactorization
    {
        /// <summary>
        /// The coordinates a point is built from. Zero first: a polynomial moved by zero is the
        /// polynomial, so the lift works on something as sparse as the input.
        /// </summary>
        [ConstantField] private static readonly int[] Values = { 0, 1, -1, 2, -2, 3, -3, 4, -4, 5, -5 };

        /// <summary>Points examined before giving up on finding a usable one.</summary>
        private const int MaxPoints = 16;

        /// <summary>
        /// Usable points compared before lifting. The one whose image has the fewest factors is
        /// lifted first, since a factor the polynomial does not have is what makes a lift fail.
        /// </summary>
        private const int Candidates = 3;

        /// <summary>The two-variable lift recombines over subsets, and refuses past this.</summary>
        private const int MaxImageFactors = 10;

        /// <summary>
        /// How much degree the monicisation may add, over all the other variables together.
        /// Measured: growths of 24 to 45 answer in 66 to 344 ms, and at 60 and past it the
        /// representation's own bounds refuse within a second.
        /// </summary>
        private const int MaxMonicGrowth = 60;

        /// <summary>
        /// The factors of <paramref name="poly"/>, each of positive degree in
        /// <paramref name="main"/>, or <see langword="null"/> where nothing could be settled. A
        /// single factor means it does not factor, which is an answer.
        /// </summary>
        internal static IReadOnlyList<MultivariatePolynomial>? Factor(MultivariatePolynomial poly, int main)
        {
            if (poly.DegreeIn(main) < 1)
                return null;
            var others = new List<int>();
            for (var variable = 0; variable < poly.VariableCount; variable++)
                if (variable != main && poly.DegreeIn(variable) > 0)
                    others.Add(variable);
            if (others.Count < 2)
                return null;
            if (PolynomialGcd.ContentIn(poly, main, others, 0) is not { IsConstant: true })
                return null;
            if (!poly.LeadingCoefficientIn(main).IsConstant)
                return ByMonicisation(poly, main, others);

            // The two-variable stage does the recombination, so it gets the variable with the
            // most to recombine over.
            others.Sort((left, right) => poly.DegreeIn(right).CompareTo(poly.DegreeIn(left)));

            var bounds = new int[poly.VariableCount];
            for (var variable = 0; variable < bounds.Length; variable++)
                bounds[variable] = poly.DegreeIn(variable);

            var usable = new List<(int[] Point, int Count)>();
            var attempt = 0;
            for (; attempt < MaxPoints && usable.Count < Candidates; attempt++)
            {
                var point = PointFor(attempt, others.Count);
                if (ImageFactorCount(poly, main, others, point) is not { } count)
                    continue;
                if (count == 1)
                    return new[] { poly };
                usable.Add((point, count));
            }
            // Stable, so that among equals the earlier and sparser point goes first.
            var order = new List<int>();
            for (var i = 0; i < usable.Count; i++)
                order.Add(i);
            order.Sort((left, right) => usable[left].Count != usable[right].Count
                ? usable[left].Count.CompareTo(usable[right].Count)
                : left.CompareTo(right));
            foreach (var index in order)
                if (Attempt(poly, main, others, usable[index].Point, bounds) is { } factors)
                    return factors;

            // Each of those split further than the polynomial does -- `x ^ 3 + z w ^ 3` at
            // `z = 1` is `x ^ 3 + w ^ 3`, which has a factor the polynomial lacks -- so the
            // remaining points are lifted as they are found.
            for (; attempt < MaxPoints; attempt++)
            {
                var point = PointFor(attempt, others.Count);
                if (ImageFactorCount(poly, main, others, point) is not { } count)
                    continue;
                if (count == 1)
                    return new[] { poly };
                if (Attempt(poly, main, others, point, bounds) is { } factors)
                    return factors;
            }
            return null;
        }

        /// <summary>
        /// The factorisation of a polynomial whose leading coefficient <c>L</c> in
        /// <paramref name="main"/> is not a constant, through the monic
        /// <c>L ^ (n - 1) · f(z / L, …)</c>, as <c>BivariateHenselFactorization</c> does for two
        /// variables.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A factorisation <c>f = A · B</c> with <c>lc(A) · lc(B) = L</c> becomes one of the
        /// monic form: <c>L ^ a · A(z / L)</c> is a polynomial each of whose terms carries
        /// <c>lc(A)</c>, and dividing that out leaves a monic factor of degree <c>a</c>; the same
        /// for <c>B</c>, and the two multiply to the monic form. So a monic form that does not
        /// factor proves that <paramref name="poly"/> does not either, the content having been
        /// taken out.
        /// </para>
        /// <para>
        /// The monic form's degree in each other variable exceeds the input's by up to
        /// <c>(n - 1) · deg L</c> there, and every lift goes that much deeper; past
        /// <see cref="MaxMonicGrowth"/> in all of them together it is refused.
        /// </para>
        /// </remarks>
        private static IReadOnlyList<MultivariatePolynomial>? ByMonicisation(
            MultivariatePolynomial poly, int main, List<int> others)
        {
            var leading = poly.LeadingCoefficientIn(main);
            var degree = poly.DegreeIn(main);
            var growth = 0;
            foreach (var variable in others)
                growth += (degree - 1) * leading.DegreeIn(variable);
            if (growth > MaxMonicGrowth)
                return null;
            if (BivariateHenselFactorization.MonicIn(poly, main, leading, degree) is not { } monic
                || !monic.LeadingCoefficientIn(main).IsConstant)
                return null;
            if (Factor(monic, main) is not { } factors)
                return null;
            if (factors.Count < 2)
                return new[] { poly };

            var found = new List<MultivariatePolynomial>();
            var remaining = poly;
            foreach (var factor in factors)
            {
                if (BivariateHenselFactorization.Substitute(factor, main, leading) is not { } mapped
                    || PolynomialGcd.ContentIn(mapped, main, others, 0) is not { } content
                    || mapped.DivideExact(content, MultivariatePolynomial.MaxIntermediateTerms) is not { } candidate)
                    return null;
                if (candidate.DegreeIn(main) < 1)
                    continue;
                if (remaining.DivideExact(candidate, MultivariatePolynomial.MaxIntermediateTerms) is not { } quotient)
                    return null;
                found.Add(candidate.Normalized());
                remaining = quotient;
            }
            if (!remaining.IsConstant)
                return null;
            return found.Count < 2 ? null : found;
        }

        /// <summary>
        /// The <paramref name="attempt"/>th point: the origin first, then a fixed
        /// pseudo-random walk over <see cref="Values"/>, so that the answer does not depend on
        /// anything but the input.
        /// </summary>
        private static int[] PointFor(int attempt, int count)
        {
            var point = new int[count];
            if (attempt == 0)
                return point;
            for (var i = 0; i < count; i++)
            {
                // A hash of the attempt and the coordinate, mixed by Wellons' lowbias32
                // integer hash, so that no coordinate is tied to another.
                var state = (uint)attempt * 0x9E3779B9u ^ (uint)(i + 1) * 0x85EBCA6Bu;
                state ^= state >> 16;
                state *= 0x7FEB352Du;
                state ^= state >> 15;
                state *= 0x846CA68Bu;
                state ^= state >> 16;
                point[i] = Values[(int)(state % (uint)Values.Length)];
            }
            return point;
        }

        /// <summary>
        /// How many irreducible factors the image at <paramref name="point"/> has, or
        /// <see langword="null"/> where the point is not usable: the image has lost degree,
        /// has a repeated factor, or splits further than the recombination is allowed to go.
        /// </summary>
        private static int? ImageFactorCount(
            MultivariatePolynomial poly, int main, IReadOnlyList<int> others, int[] point)
        {
            if (Image(poly, main, others, point) is not { } image)
                return null;
            if (image.Degree != poly.DegreeIn(main))
                return null;
            if (!RationalPolynomial.TryBezout(image, Derivative(image), out _, out _))
                return null;
            if (ToInteger(image) is not { } whole)
                return null;
            if (PolynomialFactorization.FactorPrimitive(whole.PrimitivePart()) is not { } parts)
                return null;
            var count = 0;
            foreach (var part in parts)
            {
                if (part.Multiplicity != 1)
                    return null;
                count++;
            }
            return count == 0 || count > MaxImageFactors ? null : count;
        }

        /// <summary>
        /// One point: move it to the origin, factor the two-variable slice, lift the rest,
        /// move back and check by division.
        /// </summary>
        private static IReadOnlyList<MultivariatePolynomial>? Attempt(
            MultivariatePolynomial poly, int main, List<int> others, int[] point, int[] bounds)
        {
            var moved = poly;
            for (var i = 0; i < others.Count; i++)
                if (point[i] != 0)
                {
                    if (Translate(moved, others[i], ERational.FromInt32(point[i])) is not { } next)
                        return null;
                    moved = next;
                }

            var first = others[0];
            var slice = moved;
            for (var i = 1; i < others.Count; i++)
                slice = AtZero(slice, others[i]);
            if (BivariateHenselFactorization.Factor(slice, main, first) is not { } pieces)
                return null;
            // The slice keeps the degree in x and a constant leading coefficient, so it not
            // factoring is the proof the remarks describe.
            if (pieces.Count < 2)
                return new[] { poly };

            // The two-variable lift returns its factors normalised; the lift below needs a
            // product that is the slice exactly, so the constant goes on the first.
            var factors = new List<MultivariatePolynomial>(pieces);
            if (Product(factors, null) is not { } product
                || slice.DivideExact(product, MultivariatePolynomial.MaxIntermediateTerms) is not { IsConstant: true } constant
                || constant.IsZero)
                return null;
            factors[0] = factors[0].ScaleBy(constant.CoefficientOf(0));

            if (UnivariateDiophantine.Create(factors, main, first) is not { } solver)
                return null;

            var lifted = new List<int> { first };
            for (var j = 1; j < others.Count; j++)
            {
                var target = moved;
                for (var i = j + 1; i < others.Count; i++)
                    target = AtZero(target, others[i]);
                if (Lift(target, factors, others[j], lifted, bounds, solver) is not { } next)
                    return null;
                factors = next;
                lifted.Add(others[j]);
            }

            var found = new List<MultivariatePolynomial>();
            var remaining = poly;
            foreach (var factor in factors)
            {
                var back = factor;
                for (var i = 0; i < others.Count; i++)
                    if (point[i] != 0)
                    {
                        if (Translate(back, others[i], ERational.FromInt32(-point[i])) is not { } next)
                            return null;
                        back = next;
                    }
                var candidate = back.Normalized();
                if (candidate.DegreeIn(main) < 1)
                    return null;
                if (remaining.DivideExact(candidate, MultivariatePolynomial.MaxIntermediateTerms) is not { } quotient)
                    return null;
                found.Add(candidate);
                remaining = quotient;
            }
            return remaining.IsConstant ? found : null;
        }

        /// <summary>
        /// <paramref name="factors"/>, whose product is <paramref name="target"/> at
        /// <c><paramref name="variable"/> = 0</c>, pushed to a product that is
        /// <paramref name="target"/> exactly; or <see langword="null"/> where no such
        /// factorisation follows from them.
        /// </summary>
        private static List<MultivariatePolynomial>? Lift(
            MultivariatePolynomial target, List<MultivariatePolynomial> factors, int variable,
            List<int> lifted, int[] bounds, UnivariateDiophantine solver)
        {
            var current = new List<MultivariatePolynomial>(factors);
            if (Product(current, bounds) is not { } product)
                return null;
            var error = target.Subtract(product);
            for (var power = 1; power <= bounds[variable] && !error.IsZero; power++)
            {
                var coefficients = error.CoefficientsIn(variable);
                foreach (var key in coefficients.Keys)
                    // Every lower power has been cancelled; one that has not means the factors
                    // were not what the lift assumed.
                    if (key < power)
                        return null;
                if (!coefficients.TryGetValue(power, out var coefficient))
                    continue;
                if (Diophantine(factors, coefficient, lifted, lifted.Count, bounds, solver) is not { } corrections)
                    return null;
                for (var i = 0; i < current.Count; i++)
                {
                    if (corrections[i].ShiftedBy(variable, power) is not { } shifted)
                        return null;
                    current[i] = current[i].Add(shifted);
                }
                if (Product(current, bounds) is not { } next)
                    return null;
                error = target.Subtract(next);
            }
            // The truncated product agreeing is not yet the product agreeing: a factor the
            // polynomial does not have lifts to a series that does not end.
            if (Product(current, null) is not { } exact || !target.Subtract(exact).IsZero)
                return null;
            return current;
        }

        /// <summary>
        /// <c>σ_i</c> with <c>Σ σ_i · Π_(l≠i) a_l = c</c> and each <c>σ_i</c> of lower degree in
        /// the main variable than <c>a_i</c>, in the first <paramref name="count"/> of the
        /// lifted variables, reduced modulo their degree bounds.
        /// </summary>
        /// <remarks>
        /// The last variable is set to zero and the equation solved one variable down; the
        /// error that leaves is a series in that variable, cancelled one power at a time by
        /// the same solution one variable down — so every level ends at the Bezout pairs of
        /// the image's factors.
        /// </remarks>
        private static List<MultivariatePolynomial>? Diophantine(
            List<MultivariatePolynomial> factors, MultivariatePolynomial target,
            List<int> lifted, int count, int[] bounds, UnivariateDiophantine solver)
        {
            if (count == 0)
                return solver.Solve(target);

            var variable = lifted[count - 1];
            var below = new List<MultivariatePolynomial>(factors.Count);
            foreach (var factor in factors)
                below.Add(AtZero(factor, variable));
            if (Diophantine(below, AtZero(target, variable), lifted, count - 1, bounds, solver) is not { } solution)
                return null;

            if (Cofactors(factors, bounds) is not { } cofactors)
                return null;
            if (Combine(solution, cofactors, bounds) is not { } combined)
                return null;
            var error = target.Subtract(combined).Truncated(bounds);
            for (var power = 1; power <= bounds[variable] && !error.IsZero; power++)
            {
                if (!error.CoefficientsIn(variable).TryGetValue(power, out var coefficient))
                    continue;
                if (Diophantine(below, coefficient, lifted, count - 1, bounds, solver) is not { } step)
                    return null;
                for (var i = 0; i < step.Count; i++)
                {
                    if (step[i].ShiftedBy(variable, power) is not { } shifted)
                        return null;
                    step[i] = shifted;
                    solution[i] = solution[i].Add(shifted);
                }
                if (Combine(step, cofactors, bounds) is not { } correction)
                    return null;
                error = error.Subtract(correction).Truncated(bounds);
            }
            return solution;
        }

        /// <summary><c>Σ σ_i · b_i</c>, reduced modulo the degree bounds.</summary>
        private static MultivariatePolynomial? Combine(
            List<MultivariatePolynomial> solution, List<MultivariatePolynomial> cofactors, int[] bounds)
        {
            var total = MultivariatePolynomial.Zero(cofactors[0].VariableCount);
            for (var i = 0; i < solution.Count; i++)
            {
                if (solution[i].Multiply(cofactors[i], MultivariatePolynomial.MaxIntermediateTerms) is not { } term)
                    return null;
                total = total.Add(term.Truncated(bounds));
            }
            return total;
        }

        /// <summary>Each <c>Π_(l≠i) a_l</c>, reduced modulo the degree bounds.</summary>
        private static List<MultivariatePolynomial>? Cofactors(List<MultivariatePolynomial> factors, int[] bounds)
        {
            var result = new List<MultivariatePolynomial>(factors.Count);
            for (var i = 0; i < factors.Count; i++)
            {
                var others = new List<MultivariatePolynomial>(factors.Count - 1);
                for (var l = 0; l < factors.Count; l++)
                    if (l != i)
                        others.Add(factors[l]);
                if (Product(others, bounds) is not { } product)
                    return null;
                result.Add(product);
            }
            return result;
        }

        /// <summary>
        /// The product, reduced modulo the degree bounds after every step where they are given.
        /// Reducing modulo <c>(v ^ (d_v + 1))</c> is a ring homomorphism, so the reduced
        /// product is the product reduced.
        /// </summary>
        private static MultivariatePolynomial? Product(List<MultivariatePolynomial> factors, int[]? bounds)
        {
            var result = MultivariatePolynomial.One(factors.Count > 0 ? factors[0].VariableCount : 1);
            foreach (var factor in factors)
            {
                if (result.Multiply(factor, MultivariatePolynomial.MaxIntermediateTerms) is not { } next)
                    return null;
                result = bounds is null ? next : next.Truncated(bounds);
            }
            return result;
        }

        /// <summary>
        /// The equation at the bottom, in the main variable alone: <c>σ_i = c · s_i mod a_i</c>,
        /// where <c>s_i · Π_(l≠i) a_l ≡ 1</c> modulo <c>a_i</c>.
        /// </summary>
        /// <remarks>
        /// <c>Σ σ_i b_i - c</c> vanishes modulo every <c>a_i</c>, so modulo their product, since
        /// they are coprime; and its degree is below the product's when that of <c>c</c> is,
        /// which a constant leading coefficient guarantees of every error the lift produces.
        /// </remarks>
        private sealed class UnivariateDiophantine
        {
            private readonly RationalPolynomial[] moduli;
            private readonly RationalPolynomial[] inverses;
            private readonly int main;
            private readonly int variableCount;

            private UnivariateDiophantine(RationalPolynomial[] moduli, RationalPolynomial[] inverses, int main, int variableCount)
            {
                this.moduli = moduli;
                this.inverses = inverses;
                this.main = main;
                this.variableCount = variableCount;
            }

            internal static UnivariateDiophantine? Create(List<MultivariatePolynomial> factors, int main, int first)
            {
                var moduli = new RationalPolynomial[factors.Count];
                for (var i = 0; i < moduli.Length; i++)
                    if (ToUnivariate(AtZero(factors[i], first), main) is { } modulus)
                        moduli[i] = modulus;
                    else
                        return null;
                var inverses = new RationalPolynomial[moduli.Length];
                for (var i = 0; i < moduli.Length; i++)
                {
                    var cofactor = RationalPolynomial.One;
                    for (var l = 0; l < moduli.Length; l++)
                        if (l != i)
                            cofactor = cofactor.Multiply(moduli[l]);
                    if (!RationalPolynomial.TryBezout(cofactor, moduli[i], out var inverse, out _))
                        return null;
                    inverses[i] = inverse;
                }
                return new UnivariateDiophantine(moduli, inverses, main, factors[0].VariableCount);
            }

            internal List<MultivariatePolynomial>? Solve(MultivariatePolynomial target)
            {
                if (ToUnivariate(target, main) is not { } c)
                    return null;
                var result = new List<MultivariatePolynomial>(moduli.Length);
                for (var i = 0; i < moduli.Length; i++)
                {
                    if (!c.Multiply(inverses[i]).TryDivide(moduli[i], out _, out var remainder))
                        return null;
                    if (FromUnivariate(remainder, main, variableCount) is not { } back)
                        return null;
                    result.Add(back);
                }
                return result;
            }
        }

        /// <summary><paramref name="poly"/> with <paramref name="variable"/> set to zero.</summary>
        private static MultivariatePolynomial AtZero(MultivariatePolynomial poly, int variable)
            => poly.CoefficientsIn(variable).TryGetValue(0, out var constant)
                ? constant : MultivariatePolynomial.Zero(poly.VariableCount);

        /// <summary>
        /// <paramref name="poly"/> with <paramref name="variable"/> replaced by
        /// <c><paramref name="variable"/> + <paramref name="by"/></c>, by Horner's rule.
        /// </summary>
        private static MultivariatePolynomial? Translate(MultivariatePolynomial poly, int variable, ERational by)
        {
            var coefficients = poly.CoefficientsIn(variable);
            var degree = poly.DegreeIn(variable);
            var step = MultivariatePolynomial.Monomial(poly.VariableCount, variable)
                .Add(MultivariatePolynomial.Constant(poly.VariableCount, by));
            var result = MultivariatePolynomial.Zero(poly.VariableCount);
            for (var power = degree; power >= 0; power--)
            {
                if (result.Multiply(step, MultivariatePolynomial.MaxIntermediateTerms) is not { } next)
                    return null;
                result = coefficients.TryGetValue(power, out var coefficient) ? next.Add(coefficient) : next;
            }
            return result;
        }

        /// <summary>
        /// The image at <paramref name="point"/>: every variable but <paramref name="main"/>
        /// replaced by its coordinate.
        /// </summary>
        private static RationalPolynomial? Image(
            MultivariatePolynomial poly, int main, IReadOnlyList<int> others, int[] point)
        {
            var coefficients = new ERational[poly.DegreeIn(main) + 1];
            for (var i = 0; i < coefficients.Length; i++)
                coefficients[i] = ERational.Zero;
            foreach (var pair in poly.CoefficientsIn(main))
            {
                if (Evaluate(pair.Value, others, point, 0) is not { } value)
                    return null;
                coefficients[pair.Key] = value;
            }
            return RationalPolynomial.Create(coefficients);
        }

        private static ERational? Evaluate(
            MultivariatePolynomial poly, IReadOnlyList<int> others, int[] point, int depth)
        {
            if (depth == others.Count)
                return poly.IsZero ? ERational.Zero
                    : poly.IsConstant ? poly.CoefficientOf(0) : (ERational?)null;
            var total = ERational.Zero;
            var at = EInteger.FromInt32(point[depth]);
            foreach (var pair in poly.CoefficientsIn(others[depth]))
            {
                if (Evaluate(pair.Value, others, point, depth + 1) is not { } inner)
                    return null;
                total = total.Add(inner.Multiply(ERational.Create(at.Pow(pair.Key), EInteger.One))).InLowestTerms();
            }
            return total;
        }

        /// <summary>A polynomial in <paramref name="main"/> alone, as a one-variable polynomial.</summary>
        private static RationalPolynomial? ToUnivariate(MultivariatePolynomial poly, int main)
        {
            var coefficients = new ERational[poly.DegreeIn(main) + 1];
            for (var i = 0; i < coefficients.Length; i++)
                coefficients[i] = ERational.Zero;
            foreach (var pair in poly.CoefficientsIn(main))
            {
                if (pair.Value.IsZero)
                    continue;
                if (!pair.Value.IsConstant)
                    return null;
                coefficients[pair.Key] = pair.Value.CoefficientOf(0);
            }
            return RationalPolynomial.Create(coefficients);
        }

        private static MultivariatePolynomial? FromUnivariate(RationalPolynomial poly, int main, int variableCount)
        {
            var result = MultivariatePolynomial.Zero(variableCount);
            for (var i = 0; i <= poly.Degree; i++)
            {
                if (poly[i].IsZero)
                    continue;
                if (MultivariatePolynomial.Constant(variableCount, poly[i]).ShiftedBy(main, i) is not { } term)
                    return null;
                result = result.Add(term);
            }
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

        private static IntegerPolynomial ToInteger(RationalPolynomial poly)
        {
            var denominator = EInteger.One;
            for (var i = 0; i <= poly.Degree; i++)
                denominator = denominator.Divide(denominator.GreatestCommonDivisor(poly[i].Denominator))
                    .Multiply(poly[i].Denominator);
            var whole = new EInteger[poly.Degree + 1];
            for (var i = 0; i < whole.Length; i++)
                whole[i] = poly[i].Numerator.Multiply(denominator.Divide(poly[i].Denominator));
            return IntegerPolynomial.Create(whole);
        }
    }
}
