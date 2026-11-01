//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core;
using AngouriMath.Core.Budgets;
using PeterO.Numbers;
using static AngouriMath.Entity;

namespace AngouriMath.Functions.Algebra.Groebner
{
    /// <summary>
    /// Solves a system of polynomial equations over <c>Q</c> by triangularising it, for the
    /// systems where that can be done and answered exactly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The solver this sits in front of eliminates one variable at a time by calling
    /// <see cref="Entity.SolveEquation(Variable)"/>, which applies the closed-form radical
    /// formulas. With numeric coefficients those are cheap; with symbolic ones they are not,
    /// and since each elimination turns the next one's coefficients into nested radicals the
    /// size compounds. Four coupled variables did not finish in three hundred seconds, while
    /// four uncoupled ones with 256 solutions took seventeen milliseconds — the cost was
    /// never the size of the system, it was eliminating in radicals.
    /// </para>
    /// <para>
    /// A Gröbner basis eliminates without them: the lexicographic basis is triangular and
    /// leaves the last variable a univariate polynomial with rational coefficients, which
    /// the existing polynomial solver already handles. The basis is computed under
    /// degree-reverse-lexicographic, which is the order that can actually be computed, and
    /// converted by <see cref="Fglm"/>.
    /// </para>
    /// <para>
    /// <b>It answers only where it can check its own answer.</b> Every candidate goes back
    /// into the original equations and is kept only if they reduce to exactly zero. That
    /// covers rational and radical solutions — <c>x^2 - 2, y - x</c> comes back as
    /// <c>(sqrt(2), sqrt(2))</c> and <c>(-sqrt(2), -sqrt(2))</c> — because one structural
    /// pass is enough to prove a radical identity. Where a root is a decimal the check cannot
    /// be made at all, and rather than accept a tuple on a tolerance, which is how a root
    /// that is merely close becomes a reported solution, the whole system went back to the
    /// existing solver, which answers it in decimals too and can leave out what this would have
    /// found exactly: x^2 + y^2 = 2, x^3 - y = -2 lost (-1, 1). So a candidate in decimals is
    /// checked to the precision it has instead, a negligible share of the largest term, which a
    /// tuple that is not a solution misses by whole quantities.
    /// </para>
    /// </remarks>
    internal static class GroebnerSystemSolver
    {
        /// <summary>
        /// Answers <see langword="true"/> where the system was solved, in which case
        /// <paramref name="solutions"/> holds them, or is <see langword="null"/> where there
        /// are none. Answers <see langword="false"/> where the caller should carry on with
        /// whatever it would have done.
        /// </summary>
        /// <remarks>
        /// <see langword="false"/> is one word for several different situations — the system
        /// is not polynomial, the basis ran out of budget, a root could not be verified — and
        /// a caller who has to guess which cannot debug the call
        /// (<a href="https://github.com/asc-community/AngouriMath/issues/896">#896</a>). Each
        /// of them is named on the way out, and a <see cref="BudgetRecording"/> open around
        /// the call collects them; with none open this costs one ambient read for the whole
        /// solve.
        /// </remarks>
        internal static bool TrySolve(
            IReadOnlyList<Entity> equations, IReadOnlyList<Variable> variables, out Matrix? solutions)
        {
            var budget = new GroebnerBudget();
            try
            {
                return Attempt(equations, variables, budget, out solutions);
            }
            finally
            {
                budget.Report();
            }
        }

        /// <summary>
        /// Split out so that <see cref="TrySolve"/> can report what this spent on every way
        /// out of it, including the ones that decline before any work is done.
        /// </summary>
        static bool Attempt(
            IReadOnlyList<Entity> equations, IReadOnlyList<Variable> variables,
            GroebnerBudget budget, out Matrix? solutions)
        {
            solutions = null;
            if (equations.Count == 0 || variables.Count == 0)
                return budget.Allow(false, "nothing to solve");
            if (!budget.Allow(variables.Count <= MultivariatePolynomial.MaxVariables, "variable count"))
                return false;

            var index = new Dictionary<Variable, int>(variables.Count);
            for (var i = 0; i < variables.Count; i++)
            {
                // A repeated variable would make the column layout of the answer a lie.
                if (index.ContainsKey(variables[i]))
                    return budget.Allow(false, "repeated variable");
                index[variables[i]] = i;
            }

            var polynomials = new List<MultivariatePolynomial>(equations.Count);
            foreach (var equation in equations)
            {
                // Refuses anything that is not a polynomial over Q in these variables, which
                // is the guard everything below relies on.
                if (MultivariatePolynomial.TryParse(equation, index) is not { } polynomial)
                    return budget.Allow(false, "not polynomial");
                if (!polynomial.IsZero)
                    polynomials.Add(polynomial);
            }
            if (polynomials.Count == 0)
                return budget.Allow(false, "no equations");

            var basis = Buchberger.Compute(polynomials, MonomialOrder.DegreeReverseLexicographic, budget);
            if (basis is null)
                return false;

            // The textbook signal for an inconsistent system: the ideal is everything, so a
            // nonzero constant is in it. Nothing satisfies the equations.
            foreach (var element in basis)
                if (element.IsConstant && !element.IsZero)
                {
                    solutions = null;
                    return true;
                }

            var lexicographic = Fglm.ToLexicographic(basis, variables.Count, budget);
            if (lexicographic is null)
                return false;

            var triangular = new List<Entity>(lexicographic.Count);
            foreach (var element in lexicographic)
                triangular.Add(element.ToEntity(variables));

            var found = new List<Entity[]>();
            var assignment = new Entity[variables.Count];
            if (!BackSubstitute(triangular, variables, variables.Count - 1, assignment, found, budget))
                return false;

            foreach (var candidate in found)
                if (!Satisfies(equations, variables, candidate, budget))
                    return false;

            if (found.Count == 0)
            {
                solutions = null;
                return true;
            }

            var builder = new MatrixBuilder(variables.Count);
            foreach (var candidate in found)
                // An entry already in decimals is the number it evaluates to, rather than the
                // polynomial in decimals that back-substitution wrote it as.
                builder.Add(candidate.Select(entry => entry.Nodes.Any(IsADecimal) && entry.Evaled is Number.Complex value ? value : entry).ToArray());
            solutions = builder.ToMatrix();
            return true;
        }

        /// <summary>
        /// Walks the triangular system from the last variable back. Answers
        /// <see langword="false"/> where the shape it needs is not there — a variable with no
        /// equation of its own means the system does not have finitely many solutions in the
        /// way this can enumerate.
        /// </summary>
        static bool BackSubstitute(
            IReadOnlyList<Entity> equations, IReadOnlyList<Variable> variables,
            int at, Entity[] assignment, List<Entity[]> found, GroebnerBudget budget)
        {
            if (at < 0)
            {
                found.Add((Entity[])assignment.Clone());
                return true;
            }

            var variable = variables[at];
            Entity? univariate = null;
            foreach (var equation in equations)
            {
                var free = equation.Vars.ToList();
                if (free.Count == 1 && free[0] == variable)
                {
                    univariate = equation;
                    break;
                }
            }
            if (univariate is null)
                return budget.Allow(false, "no univariate equation");

            if (univariate.SolveEquation(variable).InnerSimplified is not Set.FiniteSet roots)
                return budget.Allow(false, "unenumerable roots");

            var remaining = new Dictionary<Variable, int>(at);
            for (var i = 0; i < at; i++)
                remaining[variables[i]] = i;
            var still = variables.Take(at).ToList();
            foreach (var root in roots)
            {
                assignment[at] = root;
                var narrowed = new List<Entity>(equations.Count);
                var consistent = true;
                foreach (var equation in equations)
                {
                    var substituted = Canonical(equation.Substitute(variable, root).InnerSimplified, remaining, still);
                    if (substituted.Vars.Any())
                        narrowed.Add(substituted);
                    else if (substituted is Number.Rational { IsZero: false })
                        consistent = false;
                }
                // A root that leaves an equation a nonzero number extends to no solution.
                if (!consistent)
                    continue;
                if (!BackSubstitute(narrowed, variables, at - 1, assignment, found, budget))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <paramref name="substituted"/> as a polynomial over <c>Q</c> in the variables still to
        /// be assigned, where it is one, so that a term the root has made worth nothing is gone.
        /// </summary>
        /// <remarks>
        /// A basis that is not in shape position has elements like <c>x y + 3 x - 5 y - 15</c>,
        /// whose leading coefficient vanishes at a root: at <c>y = -3</c> it is <c>0</c>, and
        /// says nothing about <c>x</c>. One structural pass leaves it <c>x * (-3) + 3 * x</c>,
        /// which still mentions <c>x</c>, so it was taken as the equation to solve <c>x</c>
        /// from, solved to every number, and the system was declined. Where a root is a
        /// radical the coefficients are not rational, and it is left as it is.
        /// </remarks>
        static Entity Canonical(Entity substituted, IReadOnlyDictionary<Variable, int> remaining, IReadOnlyList<Variable> still)
            => MultivariatePolynomial.TryParse(substituted, remaining) is { } polynomial
                ? polynomial.ToEntity(still)
                : substituted;

        /// <summary>
        /// Substitutes a candidate into the original equations and insists they come out
        /// exactly zero.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Needed because a triangular basis can hand back a tuple that satisfies the
        /// triangle without satisfying the system it came from, where the ideal is not in
        /// shape position. So candidates are checked rather than trusted.
        /// </para>
        /// <para>
        /// <see cref="Entity.InnerSimplified"/> and deliberately not
        /// <see cref="Entity.Simplify(int)"/>. The full simplifier searches — it generates
        /// candidate forms and picks between them — so how long it takes to decide a nested
        /// radical is not bounded by anything, and an early version of this spent longer
        /// failing to prove a degree-nine root satisfied its system than the old solver takes
        /// to solve the whole thing. `InnerSimplified` is one structural pass, which is
        /// cheap enough to be safe here and still settles what is needed:
        /// <c>sqrt(2)^2 - 2</c>, <c>(3^(1/3))^3 - 3</c> and a Cardano cube root all reduce to
        /// zero in single-digit milliseconds.
        /// </para>
        /// <para>
        /// It only ever answers zero, never disproves it, so a candidate it cannot settle is
        /// declined and the system falls back, which costs coverage. The zero is decided to
        /// about fifty digits, though, not exactly. With downcasting on, the default, a
        /// residual that evaluates below <c>DowncastingTolerance</c>, half the hundred digits
        /// of <see cref="MathS.Settings.DecimalPrecisionContext"/>, times the scale that
        /// cancelled is taken as <c>0</c>. So a candidate that is close enough passes as
        /// well: <c>8y^3 + 4y^2 - 4y - 1</c> at the Cardano root for <c>cos(2pi/7)</c>, with
        /// one <c>7</c> made <c>7 + 10^(-50)</c>, comes out <c>0</c>, where its value is
        /// <c>2.48e-52 - 3.26e-52 i</c>.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/2143">#2143</a>
        /// </para>
        /// </remarks>
        static bool Satisfies(
            IReadOnlyList<Entity> equations, IReadOnlyList<Variable> variables,
            Entity[] candidate, GroebnerBudget budget)
        {
            var substitutions = new Dictionary<Variable, Entity>(variables.Count);
            for (var i = 0; i < variables.Count; i++)
                substitutions[variables[i]] = candidate[i];

            // A candidate with a decimal in it is a root of a factor of a univariate polynomial whose
            // roots have no closed form, found numerically, and no structural pass makes its residual
            // exactly zero. Such a candidate is checked to the precision it has: each equation's
            // residual is to be a negligible part of the largest term the equation adds up to. A
            // tuple that does not satisfy the system, as a basis out of shape position can give, is
            // off by whole quantities and fails this as it fails the exact test. Declining every
            // decimal sent the system to elimination, which answered x^2 + y^2 = 2, x^3 - y = -2 in
            // decimals and without its rational solution (-1, 1).
            bool? inDecimals = null;
            foreach (var equation in equations)
            {
                if (!budget.Spend())
                    return false;
                var substituted = equation.Substitute(substitutions);
                if (substituted.InnerSimplified is Number.Integer { IsZero: true })
                    continue;
                inDecimals ??= candidate.Any(entry => entry.Nodes.Any(IsADecimal));
                if (inDecimals is false || !IsNegligible(substituted))
                    return budget.Allow(false, "unverifiable root");
            }
            return true;
        }

        private static bool IsADecimal(Entity node)
            => node is Number.Complex number && (number.RealPart is not Number.Rational || number.ImaginaryPart is not Number.Rational);

        /// <summary>How small a residual is next to the largest term, to be what rounding leaves of a zero.</summary>
        [ConstantField] private static readonly EDecimal NegligibleShare = EDecimal.FromString("1E-10");

        /// <summary>
        /// Whether <paramref name="substituted"/>, an equation with numbers for its unknowns,
        /// evaluates to a residual negligible next to the largest of the terms it adds up to.
        /// </summary>
        private static bool IsNegligible(Entity substituted)
        {
            if (substituted.Evaled is not Number.Complex { IsFinite: true } residual)
                return false;
            var largest = EDecimal.One;
            foreach (var term in Sumf.LinearChildren(substituted))
                if (term.Evaled is Number.Complex { IsFinite: true } value)
                    largest = EDecimal.Max(largest, value.Abs().EDecimal);
            return residual.Abs().EDecimal.CompareTo(largest.Multiply(NegligibleShare)) <= 0;
        }
    }
}
