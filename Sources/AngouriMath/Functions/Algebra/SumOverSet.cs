//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using PeterO.Numbers;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions
{
    /// <summary>
    /// The value of a sum over a set, <c>sum(f(x), x in S)</c>: the terms written out and added
    /// where the members of <c>S</c> are known and there are finitely many of them, and the sum
    /// left as written otherwise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A sum over a set counts each member once, which is what makes it well defined without an
    /// order: a set has no first member, and addition does not care. So the members have to be
    /// <i>known to be distinct</i> before they are added, and a listed set is read only where its
    /// elements are numbers. <c>sum(x, x in { a, b })</c> is <c>a + b</c> when <c>a</c> and
    /// <c>b</c> differ and <c>a</c> when they are equal, and nothing here can tell which, so it is
    /// left as written.
    /// </para>
    /// <para>
    /// The sum over the roots of a polynomial is the case the node exists for:
    /// <c>sum(f(w), w in { w : p(w) = 0 })</c>. Where every root is rational, a root of a
    /// quadratic or a root of a binomial, the solver writes them and they are added; <i>every</i>
    /// is checked, by counting the distinct roots it gave against the degree of the square-free
    /// part of <c>p</c>, which is how many distinct roots <c>p</c> has. The roots of an
    /// irreducible cubic or quartic are not written out in radicals, which read no more simply
    /// than the sum. A root set the solver answers only in part is left as written, since a sum
    /// that misses a term is a wrong answer. Evaluated to a number, the roots the solver cannot
    /// write are found to the working precision, all of them or none, by
    /// <see cref="Algebra.NumericalSolving.DurandKerner"/>. And a sum over a set that is not finite -- an
    /// interval, the integers -- is a series, which is not this node's to answer.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/1285">#1285</a>
    /// </para>
    /// </remarks>
    internal static class SumOverSet
    {
        /// <summary>
        /// The sum, or <see langword="null"/> where the members of the set are not known or not
        /// finitely many.
        /// </summary>
        internal static Entity? Value(Entity expression, Entity var, Entity over, bool isExact)
        {
            if (var is not Variable x || over.InnerSimplified is not Set set)
                return null;
            // A numeric value needs the roots as numbers, and those do not need the solver: asking
            // it first would be a solve per evaluation, and a check evaluates at many points.
            var members = isExact ? Members(set) : NumericMembers(set) ?? Members(set);
            if (members is null || members.Count > Summationf.MaxExpandedTerms)
                return null;
            Entity? sum = null;
            foreach (var member in members)
            {
                var term = expression.Substitute(x, member);
                sum = sum is null ? term : sum + term;
            }
            sum ??= Integer.Zero;
            return isExact ? sum.InnerSimplified : sum.Evaled;
        }

        /// <summary>
        /// The members of <paramref name="set"/>, each once, or <see langword="null"/> where they
        /// are not known to be distinct or there are not finitely many.
        /// </summary>
        private static IReadOnlyList<Entity>? Members(Set set)
            => set switch
            {
                FiniteSet finite => Distinct(finite.Elements),
                // { w in S : p(w) = 0 }, which the set builder holds as w in S and p(w) = 0: the
                // roots that are members of S.
                ConditionalSet { Var: Variable w } builder when builder.DeclaredMembership is (Set universe, Equalsf(var left, var right))
                    => Roots(left - right, w) is { } roots ? Within(roots, universe) : null,
                ConditionalSet { Var: Variable w, Predicate: Equalsf(var left, var right) } => Roots(left - right, w),
                _ => null,
            };

        /// <summary>
        /// The members of <paramref name="members"/> that are in <paramref name="universe"/>, or
        /// <see langword="null"/> where that is not decided for one of them.
        /// </summary>
        private static IReadOnlyList<Entity>? Within(IReadOnlyList<Entity> members, Set universe)
        {
            var inside = new List<Entity>();
            foreach (var member in members)
            {
                if (!universe.TryContains(member, out var contains))
                    return null;
                if (contains)
                    inside.Add(member);
            }
            return inside;
        }

        /// <summary>
        /// The elements, where each is a number and no two are equal; <see langword="null"/>
        /// otherwise. Two numbers written differently can be the same number, so they are
        /// compared by value, and a pair that agrees to the working precision is taken for one
        /// that may be equal and the set is not read.
        /// </summary>
        private static IReadOnlyList<Entity>? Distinct(IEnumerable<Entity> elements)
        {
            var members = new List<Entity>();
            var values = new List<Complex>();
            foreach (var element in elements)
            {
                if (element.Evaled is not Complex value || !value.IsFinite)
                    return null;
                foreach (var seen in values)
                    if (seen == value)
                        return null;
                members.Add(element);
                values.Add(value);
            }
            return members;
        }

        /// <summary>
        /// The roots of the polynomial <paramref name="polynomial"/> in <paramref name="w"/>, each
        /// once, where the solver writes all of them; <see langword="null"/> otherwise.
        /// </summary>
        /// <remarks>
        /// Only roots that read as simply as the sum does are written out: rational ones, the
        /// roots of a quadratic, and the roots of a binomial. The roots of an irreducible cubic or
        /// quartic in radicals are no simpler than the sum over them, and for a real polynomial
        /// with three real roots Cardano's formula writes them with complex radicals, so that sum
        /// is kept as it is; its value is still found numerically. The solver is not asked for
        /// anything past that, which also spares a search every time a simplification meets the
        /// sum.
        /// </remarks>
        private static IReadOnlyList<Entity>? Roots(Entity polynomial, Variable w)
        {
            if (SquareFreeParts(polynomial, w) is not { } parts)
                return null;
            var expected = 0;
            foreach (var part in parts)
                expected += part.Factor.Degree;
            if (expected > 2 && !(PolynomialFactorization.FactorComplete(polynomial, w) is { } factored
                    && factored.Parts.All(static part => part.Factor.Degree <= 2 || IsBinomial(part.Factor))))
                return null;
            if (polynomial.SolveEquation(w) is not FiniteSet solutions
                || Distinct(solutions.Elements.Select(static root => root.InnerSimplified)) is not { } roots)
                return null;
            return roots.Count == expected ? roots : null;
        }

        /// <summary>
        /// The members of a set of roots to the working precision, where the solver cannot write
        /// them: each square-free part of the polynomial by <see cref="Algebra.NumericalSolving.DurandKerner"/>,
        /// all its roots or none. The parts share no root, so the roots are distinct.
        /// </summary>
        private static IReadOnlyList<Entity>? NumericMembers(Set set)
            => set switch
            {
                ConditionalSet { Var: Variable w } builder when builder.DeclaredMembership is (Set universe, Equalsf(var left, var right))
                    => NumericRoots(left - right, w) is { } roots ? Within(roots, universe) : null,
                ConditionalSet { Var: Variable w, Predicate: Equalsf(var left, var right) } => NumericRoots(left - right, w),
                _ => null,
            };

        /// <summary>
        /// The roots of <paramref name="polynomial"/> in <paramref name="w"/> to the working
        /// precision, each once, all of them or <see langword="null"/>.
        /// </summary>
        internal static IReadOnlyList<Entity>? NumericRoots(Entity polynomial, Variable w)
        {
            if (SquareFreeParts(polynomial, w) is not { } parts)
                return null;
            var context = MathS.Settings.DecimalPrecisionContext.Value;
            var roots = new List<Entity>();
            foreach (var part in parts)
            {
                if (part.Factor.Degree < 1)
                    continue;
                if (Algebra.NumericalSolving.DurandKerner.Roots(part.Factor, context) is not { } found)
                    return null;
                roots.AddRange(found);
            }
            return roots;
        }

        /// <summary>
        /// The square-free decomposition of <paramref name="polynomial"/> in <paramref name="w"/>,
        /// where it is a polynomial with rational coefficients of degree at least one; its parts'
        /// degrees add up to the number of distinct roots. <see langword="null"/> otherwise.
        /// </summary>
        /// <remarks>
        /// Read as <see cref="MultivariatePolynomial"/> reads a polynomial, off the tree, so the
        /// coefficients are the rationals written rather than values evaluated: with downcasting
        /// off an evaluated <c>3</c> is a decimal and not the rational <c>3</c>, and a reading
        /// through evaluation would refuse the polynomial -- in the setting numerical checks
        /// run in.
        /// </remarks>
        internal static IReadOnlyList<SquareFreeDecomposition.SquareFreePart>? SquareFreeParts(Entity polynomial, Variable w)
        {
            if (MultivariatePolynomial.TryParse(polynomial, new Dictionary<Variable, int> { [w] = 0 }) is not { } parsed)
                return null;
            var degree = parsed.DegreeIn(0);
            if (degree < 1 || degree > IntegerPolynomial.MaxDegree)
                return null;
            var rational = new ERational[degree + 1];
            for (var i = 0; i < rational.Length; i++)
                rational[i] = ERational.Zero;
            foreach (var term in parsed.Terms)
                rational[MultivariatePolynomial.PowerOf(term.Key, 0)] = term.Value;
            var denominator = EInteger.One;
            foreach (var coefficient in rational)
                denominator = denominator.Divide(denominator.Gcd(coefficient.Denominator)).Multiply(coefficient.Denominator);
            var whole = new EInteger[rational.Length];
            for (var i = 0; i < whole.Length; i++)
                whole[i] = rational[i].Numerator.Multiply(denominator.Divide(rational[i].Denominator));
            var poly = IntegerPolynomial.Create(whole);
            return poly.Degree < 1 ? null : SquareFreeDecomposition.Decompose(poly.PrimitivePart());
        }

        private static bool IsBinomial(IntegerPolynomial poly)
        {
            var terms = 0;
            for (var power = 0; power <= poly.Degree; power++)
                if (!poly[power].IsZero)
                    terms++;
            return terms == 2;
        }
    }
}
