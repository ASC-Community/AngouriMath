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
            if (!BackSubstitute(triangular, triangular, variables, variables.Count - 1, assignment, found, budget))
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
        /// way this can enumerate. <paramref name="originals"/> are the equations of the
        /// triangular system that <paramref name="equations"/> were narrowed from, in the same
        /// order, before any root was put into them.
        /// </summary>
        static bool BackSubstitute(
            IReadOnlyList<Entity> equations, IReadOnlyList<Entity> originals, IReadOnlyList<Variable> variables,
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
                var narrowedFrom = new List<Entity>(equations.Count);
                var consistent = true;
                for (var e = 0; e < equations.Count; e++)
                {
                    var equation = equations[e];
                    var substituted = Canonical(equation.Substitute(variable, root).InnerSimplified, remaining, still);
                    if (substituted.Vars.Any())
                    {
                        narrowed.Add(substituted);
                        narrowedFrom.Add(originals[e]);
                    }
                    else if (substituted is Number.Rational { IsZero: false })
                    {
                        // Unless what is left is only the part the decimals did not flush to 0, where
                        // the root is not shown to extend to nothing, and the equations narrowed by
                        // the flushed decimals are not to be trusted either.
                        if (VanishesAtTheExactRoot(equation, variable, root))
                            return budget.Allow(false, "a root past the decimal range");
                        // And an equation with a radical in it, from this root or one assigned before, is
                        // put to a number by evaluating, where a large term cancels to what its digits
                        // leave, which downcasting reads as a rational: the root
                        // (-4 10^24 - sqrt(28 10^48)) / 6 of its own quartic, whose terms are near 1e96,
                        // left 13/10000, and the one real root of 1 - 2x + x|-2 - 3x| = 0 with x written
                        // as 10^-24 x was taken for no root, the answer { }. Only an interval leaving 0 out
                        // shows such a root extends to nothing; an equation that is a polynomial over Q is
                        // decided exactly. Asked of the equation before any root was put into it, with
                        // every root assigned so far: one narrowed by a radical root keeps what its
                        // evaluation left as a rational, and 3x^2 - 2x - 2|2x - 3| = 0 with x written as
                        // 10^-30 x lost the root (sqrt(19) - 1) 10^30 / 3 to a constant 2 10^21 left
                        // where its terms are near 1e120, read exactly once the imaginary part 0 was put in.
                        var exactly = originals[e];
                        for (var assigned = at; assigned < variables.Count; assigned++)
                            exactly = exactly.Substitute(variables[assigned], assignment[assigned]);
                        if (!root.Nodes.Any(IsADecimal)
                            && MultivariatePolynomial.TryParse(exactly, new Dictionary<Variable, int>()) is null
                            && !AnalyticalSolving.StatementSolver.IsShownNotZero(exactly))
                            continue;
                        consistent = false;
                    }
                }
                // A root that leaves an equation a nonzero number extends to no solution.
                if (!consistent)
                    continue;
                if (!BackSubstitute(narrowed, narrowedFrom, variables, at - 1, assignment, found, budget))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Whether <paramref name="root"/>, a number in decimals, makes <paramref name="equation"/>
        /// in <paramref name="variable"/> alone negligible next to the largest of its terms when
        /// the decimals are read as the exact rationals they are.
        /// </summary>
        /// <remarks>
        /// A decimal below about <c>10^-200</c> is flushed to 0 by the decimal context, where a
        /// rational is kept exactly. So a root near <c>10^-30</c> put into an eighth-degree
        /// polynomial left the polynomial's rational constant term alone, every power of the root
        /// gone, and each of the eight roots was read as extending to no solution: the system
        /// solved for the parts of x in <c>(1 + i) + 2x - 3x^2 + x|3 - x| = 0</c> with x written as
        /// <c>10^30 x</c> was answered <c>{ }</c>. Exactly, the root leaves a residual of the
        /// size its hundred digits allow.
        /// </remarks>
        static bool VanishesAtTheExactRoot(Entity equation, Variable variable, Entity root)
        {
            if (!root.Nodes.Any(IsADecimal) || root.Evaled is not Number.Complex value
                || !value.RealPart.EDecimal.IsFinite || !value.ImaginaryPart.EDecimal.IsFinite
                || MultivariatePolynomial.TryParse(equation, new Dictionary<Variable, int> { [variable] = 0 }) is not { } polynomial)
                return false;
            // In integers: the root as (a + b i) 10^shift, and every term c z^k times one positive
            // number -- the coefficients' common denominator, and the power of ten that makes the
            // root's powers whole -- which scales the residual and the largest term alike. In
            // rationals the denominators were multiplied together at every addition, so their
            // digits doubled with each power of the root: the parts of x in an equation with a
            // sign in it, with x written as 10^-12 x, spent longer than twenty seconds here.
            var (re, im) = (value.RealPart.EDecimal, value.ImaginaryPart.EDecimal);
            var shift = EInteger.Min(re.Exponent, im.Exponent);
            if (!re.Exponent.Subtract(shift).CanFitInInt32() || !im.Exponent.Subtract(shift).CanFitInInt32())
                return false;
            var ten = EInteger.FromInt32(10);
            var (a, b) = (re.Mantissa.Multiply(ten.Pow(re.Exponent.Subtract(shift).ToInt32Unchecked())),
                          im.Mantissa.Multiply(ten.Pow(im.Exponent.Subtract(shift).ToInt32Unchecked())));
            var degree = 0;
            var common = EInteger.One;
            foreach (var term in polynomial.Terms)
            {
                degree = System.Math.Max(degree, MultivariatePolynomial.PowerOf(term.Key, 0));
                var denominator = term.Value.Denominator;
                common = common.Divide(common.Gcd(denominator)).Multiply(denominator);
            }
            // The power of ten each term carries: 10^(shift k) times 10^(-shift degree) where the
            // shift is negative, so that the least of them is 10^0.
            var lowest = shift.Sign < 0 ? shift.Multiply(degree) : EInteger.Zero;
            if (!shift.Abs().Multiply(degree).CanFitInInt32())
                return false;
            var powers = new List<(EInteger Re, EInteger Im)> { (EInteger.One, EInteger.Zero) };
            var (sumRe, sumIm) = (EInteger.Zero, EInteger.Zero);
            var largest = EInteger.Zero;
            foreach (var term in polynomial.Terms)
            {
                var power = MultivariatePolynomial.PowerOf(term.Key, 0);
                while (powers.Count <= power)
                {
                    var (pRe, pIm) = powers[powers.Count - 1];
                    powers.Add((pRe.Multiply(a).Subtract(pIm.Multiply(b)), pRe.Multiply(b).Add(pIm.Multiply(a))));
                }
                var coefficient = term.Value.Numerator.Multiply(common.Divide(term.Value.Denominator))
                    .Multiply(ten.Pow(shift.Multiply(power).Subtract(lowest).ToInt32Unchecked()));
                var (tRe, tIm) = (powers[power].Re.Multiply(coefficient), powers[power].Im.Multiply(coefficient));
                (sumRe, sumIm) = (sumRe.Add(tRe), sumIm.Add(tIm));
                var size = tRe.Abs().Add(tIm.Abs());
                if (size.CompareTo(largest) > 0)
                    largest = size;
            }
            return ERational.FromEInteger(sumRe.Abs().Add(sumIm.Abs()))
                .CompareTo(ERational.FromEInteger(largest).Multiply(ERational.FromEDecimal(NegligibleShare))) <= 0;
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
