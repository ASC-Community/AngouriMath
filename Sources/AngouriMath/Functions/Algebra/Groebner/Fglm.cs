//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using PeterO.Numbers;

namespace AngouriMath.Functions.Algebra.Groebner
{
    /// <summary>
    /// Converts a degree-reverse-lexicographic Gröbner basis of a zero-dimensional ideal
    /// into the lexicographic one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Needed because the two orders are good at opposite things. Degrevlex is what can be
    /// computed; lexicographic is what can be back-substituted, because its basis is
    /// triangular and leaves the last variable a univariate polynomial with rational
    /// coefficients. So the system is solved in one order and answered in the other.
    /// </para>
    /// <para>
    /// The conversion is linear algebra rather than more Buchberger. For a zero-dimensional
    /// ideal the quotient ring is a finite-dimensional vector space spanned by the monomials
    /// no leading term divides, so every monomial reduces to a point in it. Walking
    /// monomials in lexicographic order and asking which is the first to be a combination of
    /// those already seen produces the basis directly: each dependency *is* an element, and
    /// the coefficients of the combination are its terms.
    /// </para>
    /// <para>
    /// The cost is governed by the dimension of that space, which is the number of solutions
    /// counted with multiplicity — a different quantity from anything that bounds Buchberger.
    /// A system can have a basis that computes in milliseconds and a conversion that does
    /// not finish, so <see cref="GroebnerBudget.MaxQuotientDimension"/> is checked before any
    /// of the work below is done rather than discovered partway through it.
    /// </para>
    /// </remarks>
    internal static class Fglm
    {
        /// <summary>
        /// The monomials no leading monomial of <paramref name="basis"/> divides. They span
        /// the quotient ring, and there are finitely many exactly when the ideal is
        /// zero-dimensional — so running past the ceiling is how a system with infinitely
        /// many solutions, or simply too many, announces itself.
        /// </summary>
        internal static List<ulong>? StandardMonomials(
            IReadOnlyList<MultivariatePolynomial> basis, int variableCount, MonomialOrder order, int ceiling)
        {
            var leading = new List<ulong>(basis.Count);
            foreach (var element in basis)
                leading.Add(element.LeadingMonomial(order));

            var standard = new List<ulong>();
            var queue = new SortedSet<ulong> { 0UL };
            var seen = new HashSet<ulong> { 0UL };

            while (queue.Count > 0)
            {
                var monomial = queue.Min;
                queue.Remove(monomial);

                // A monomial some leading term divides is not standard, and neither is any
                // multiple of it, so not enqueueing its multiples prunes rather than skips.
                var divisible = false;
                foreach (var candidate in leading)
                    if (MultivariatePolynomial.MonomialDivides(candidate, monomial))
                    {
                        divisible = true;
                        break;
                    }
                if (divisible)
                    continue;

                standard.Add(monomial);
                if (standard.Count > ceiling)
                    return null;

                for (var variable = 0; variable < variableCount; variable++)
                    if (MultivariatePolynomial.TryTimesMonomials(
                            monomial, MultivariatePolynomial.PackMonomial(variable, 1), variableCount, out var next)
                        && seen.Add(next))
                        queue.Add(next);
            }
            return standard;
        }

        /// <summary>
        /// The lexicographic basis, or <see langword="null"/> where the ideal is not
        /// zero-dimensional or the budget ran out.
        /// </summary>
        internal static List<MultivariatePolynomial>? ToLexicographic(
            IReadOnlyList<MultivariatePolynomial> degreeReverseLexicographic,
            int variableCount, GroebnerBudget budget)
        {
            const MonomialOrder computed = MonomialOrder.DegreeReverseLexicographic;

            var standard = StandardMonomials(
                degreeReverseLexicographic, variableCount, computed, GroebnerBudget.MaxQuotientDimension);
            if (standard is null)
            {
                _ = budget.Allow(false, "quotient dimension");
                return null;
            }

            var position = new Dictionary<ulong, int>(standard.Count);
            for (var i = 0; i < standard.Count; i++)
                position[standard[i]] = i;

            // An echelon form over the quotient ring. Each row remembers both what it is as
            // a vector and which combination of staircase monomials produced it, so a
            // dependency can be read off directly as the terms of a new basis element.
            var rowVectors = new List<ERational[]>();
            var rowCombinations = new List<ERational[]>();
            var rowPivots = new List<int>();
            var staircase = new List<ulong>();

            var lexicographic = new List<MultivariatePolynomial>();
            var lexicographicLeading = new List<ulong>();

            // Lexicographic order is integer order under this packing, so a sorted set of the
            // packed monomials walks them smallest first, which is what FGLM wants.
            var queue = new SortedSet<ulong> { 0UL };
            var seen = new HashSet<ulong> { 0UL };

            while (queue.Count > 0)
            {
                if (!budget.Spend())
                    return null;

                var monomial = queue.Min;
                queue.Remove(monomial);

                var covered = false;
                foreach (var leading in lexicographicLeading)
                    if (MultivariatePolynomial.MonomialDivides(leading, monomial))
                    {
                        covered = true;
                        break;
                    }
                if (covered)
                    continue;

                var reduced = Buchberger.FullyReduce(
                    MultivariatePolynomial.Term(variableCount, monomial, ERational.One),
                    degreeReverseLexicographic, computed, budget);
                if (reduced is null)
                    return null;

                var vector = new ERational[standard.Count];
                for (var i = 0; i < vector.Length; i++)
                    vector[i] = ERational.Zero;
                foreach (var term in reduced.Monomials)
                {
                    if (!position.TryGetValue(term, out var at))
                    {
                        // Only reachable if the input was not a Gröbner basis under this
                        // order, which would make everything below meaningless.
                        _ = budget.Allow(false, "not a Gröbner basis");
                        return null;
                    }
                    vector[at] = reduced.CoefficientOf(term);
                }

                var combination = new ERational[Math.Max(staircase.Count, 1)];
                for (var i = 0; i < combination.Length; i++)
                    combination[i] = ERational.Zero;

                for (var row = 0; row < rowVectors.Count; row++)
                {
                    var pivot = rowPivots[row];
                    if (vector[pivot].IsZero)
                        continue;
                    var factor = vector[pivot].Divide(rowVectors[row][pivot]);
                    for (var i = 0; i < vector.Length; i++)
                        vector[i] = vector[i].Subtract(factor.Multiply(rowVectors[row][i])).InLowestTerms();
                    for (var i = 0; i < rowCombinations[row].Length && i < combination.Length; i++)
                        combination[i] = combination[i].Add(factor.Multiply(rowCombinations[row][i])).InLowestTerms();
                }

                var pivotAt = -1;
                for (var i = 0; i < vector.Length; i++)
                    if (!vector[i].IsZero)
                    {
                        pivotAt = i;
                        break;
                    }

                if (pivotAt < 0)
                {
                    // This monomial is a combination of ones already standing, so the
                    // difference lies in the ideal and is a lexicographic basis element.
                    var element = MultivariatePolynomial.Term(variableCount, monomial, ERational.One);
                    for (var i = 0; i < staircase.Count; i++)
                        if (!combination[i].IsZero)
                            element = element.Subtract(
                                MultivariatePolynomial.Term(variableCount, staircase[i], combination[i]));
                    if (!budget.CheckPolynomial(element))
                        return null;
                    lexicographic.Add(element);
                    lexicographicLeading.Add(monomial);
                    continue;
                }

                // The row stored is this monomial's normal form less the rows that reduced it,
                // so the combination it stands for is the monomial less theirs. Recording the
                // monomial alone gave every later element that leans on this row the wrong
                // coefficients: x * y, x + y^2 - 1 came back as x + y^2, whose root (0, 0)
                // is no solution, and the system fell back to an eliminator that lost two of
                // its three solutions.
                staircase.Add(monomial);
                var unit = new ERational[staircase.Count];
                for (var i = 0; i < unit.Length; i++)
                    unit[i] = i < staircase.Count - 1 && i < combination.Length ? combination[i].Negate() : ERational.Zero;
                unit[staircase.Count - 1] = ERational.One;

                for (var row = 0; row < rowCombinations.Count; row++)
                {
                    var widened = new ERational[staircase.Count];
                    for (var i = 0; i < widened.Length; i++)
                        widened[i] = ERational.Zero;
                    Array.Copy(rowCombinations[row], widened, rowCombinations[row].Length);
                    rowCombinations[row] = widened;
                }

                rowVectors.Add(vector);
                rowCombinations.Add(unit);
                rowPivots.Add(pivotAt);

                if (!budget.Allow(staircase.Count <= standard.Count, "quotient dimension"))
                    return null;

                for (var variable = 0; variable < variableCount; variable++)
                    if (MultivariatePolynomial.TryTimesMonomials(
                            monomial, MultivariatePolynomial.PackMonomial(variable, 1), variableCount, out var next)
                        && seen.Add(next))
                        queue.Add(next);
            }

            return lexicographic;
        }
    }
}
