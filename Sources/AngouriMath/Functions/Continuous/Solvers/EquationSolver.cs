//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Budgets;
using AngouriMath.Core.Exceptions;
using AngouriMath.Extensions;
using AngouriMath.Functions.Algebra.AnalyticalSolving;
using System;

namespace AngouriMath.Functions.Algebra
{
    using static AngouriMath.Entity.Set;
    using static Entity;
    internal static class EquationSolver
    {
        /// <summary>Solves one equation</summary>
        internal static Set Solve(Entity equation, Variable x)
        {
            // An equality `a = b` is `a - b = 0`, as a statement's own solve reads it: passed on as it
            // stood, `MathS.SolveEquation("x + 1 = 3", x)` was `{ x : x + 1 = 3 and 3 = 0 }`, which is
            // empty, where "x + 1 = 3".Solve("x") is `{ 2 }`.
            // https://github.com/asc-community/AngouriMath/issues/1673
            if (equation is Equalsf(var left, var right))
                equation = left - right;
            using var _ = MathS.Settings.PrecisionErrorZeroRange.Set(1e-12m);
            using var __ = MathS.Settings.FloatToRationalIterCount.Set(0);
            using var ___ = MathS.Settings.MaxExpansionTermCount.Set(50);
            var solutions = AnalyticalEquationSolver.Solve(equation, x);

            static Entity simplifier(Entity entity) => entity.InnerSimplified;
            static Entity evaluator(Entity entity) => entity.Evaled;
            var factorizer = equation.Vars.Count == 1 ? (Func<Entity, Entity>)evaluator : simplifier;


            if (solutions is FiniteSet finiteSet)
            {
                var finite = finiteSet.Select(simplifier)
                    .Where(elem => elem.IsFinite && factorizer(equation.Substitute(x, elem)).IsFinite).ToSet();
                // An equation written out as expr = 0 has had its answers checked against it
                // since the extraneous root of ln(x) + ln(x+1) = 0 was fixed, but the same
                // equation written as the bare expr came through here instead and was only
                // checked for being finite. The two disagreed:
                // (2^x + 2^(2x) - 6 = 0).Solve(x) gave { 1 } while
                // "2^x + 2^(2x) - 6".SolveEquation(x) gave { 1, ln((-3) ^ (1 / ln(2))) },
                // and the second of those is not a root of anything.
                return StatementSolver.UnsolvedWhereIndependenceIsDenied(
                    StatementSolver.WithoutSpuriousRoots(finite, equation, x), equation.Equalizes(0), x);
            }
            else
                return solutions;
        }

        /// <summary>
        /// What a whole system solve is allowed to spend before it declines.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Both ceilings, because neither is a bound on its own.</b> A step here is one
        /// candidate solution the elimination explores, and that is what compounds: each
        /// elimination turns the next level's coefficients into nested radicals. Measured, the
        /// systems that answer explore very few — a symbolic 2×2 takes 2, cyclic-4 takes 8,
        /// and the largest that answers at all, five uncoupled quartics with 1024 solutions,
        /// takes 341. Cyclic-5 passes 100 000 without finishing. So 10 000 admits everything
        /// known to work with a factor of thirty to spare and still refuses the ones that run
        /// away.
        /// </para>
        /// <para>
        /// The clock is a backstop, not the bound, because a step can be arbitrarily expensive:
        /// cyclic-4 spends five seconds in eight of them. It is set well above what any
        /// answering case needs — the slowest takes about five seconds unloaded — since a
        /// tight clock makes the same system answer or decline depending on what else the
        /// machine is doing, and a flaky answer is worse than a slow one. That was measured
        /// too: at five seconds the uncoupled case passed alone and failed inside the suite.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/896">#896</a>
        /// </para>
        /// </remarks>
        [ConstantField]
        internal static readonly WorkBudget SystemSolveBudget =
            new() { Steps = 10_000, Time = TimeSpan.FromSeconds(60) };

        /// <summary>
        /// Solves a system of equations for <paramref name="vars"/>, one row per solution, or
        /// <see langword="null"/> where it cannot.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Tried in order: a Gröbner basis, where the system is polynomial over Q; row reduction,
        /// where a linear system has fewer equations than unknowns; once for each factor, where
        /// an equation is a product in more than one unknown; <see cref="NonSquareSystemSolver"/>,
        /// where the count is not square or an unknown is in no equation; and elimination.
        /// </para>
        /// <para>
        /// Elimination solves one equation after another with substitution, e.g. <br/>
        /// let { x - y + a = 0, y + 2a = 0 } be a system of equations for variables { x, y } <br/>
        /// Then we first find y from the first equation, <br/>
        /// y = x + a <br/>
        /// then we substitute it to all others <br/>
        /// x + a + 2a = 0 <br/>
        /// then we find x <br/>
        /// x = -3a <br/>
        /// Then we substitute back <br/>
        /// y = -3a + a = -2a <br/>
        /// and each row it answers is put back into the equations as they were written.
        /// </para>
        /// </remarks>
        internal static Matrix? SolveSystem(IEnumerable<Entity> inputEquations, ReadOnlySpan<Variable> vars)
        {
            var written = inputEquations.ToList();
            var equations = new List<Entity>(written.Select(equation => equation.InnerSimplified));

            // Triangularising first, where the system is polynomial over Q and the answer can
            // be checked exactly. Eliminating in radicals -- which is what InSolveSystem below
            // does -- costs nothing on an uncoupled system and does not finish on a coupled
            // one, so this is tried before the equation count is even insisted on: a Groebner
            // basis has no use for as many equations as unknowns.
            // One ledger for the whole call, drawn on by both paths. Each stage having a
            // budget of its own is what bounded the fast path and left the fall-through
            // unbounded, so the same `Solve` finished or did not depending on which internal
            // path accepted it. https://github.com/asc-community/AngouriMath/issues/896
            var ledger = BudgetLedger.For("SolveSystem", SystemSolveBudget);
            try
            {
                var variables = new Variable[vars.Length];
                vars.CopyTo(variables);
                // The Gröbner path keeps a ledger of its own, deliberately. It uses the same
                // mechanism for two different things -- a genuine resource ceiling, and a
                // structural refusal like "not polynomial" -- and a ledger that has recorded
                // any ceiling refuses every later spend. Sharing it would therefore read
                // "declined in 8 ms because the system is uncoupled" as "the budget is gone",
                // and the 1024-solution case that the eliminator answers cheaply would raise
                // instead. Measured: it does exactly that.
                //
                // What is shared is the clock. This ledger starts when the call does, so the
                // time the Gröbner path spends is already gone from it when the elimination
                // begins -- which is the bound that was missing, without conflating the two
                // meanings of "stopped".
                if (Groebner.GroebnerSystemSolver.TrySolve(equations, variables, out var triangularised))
                    return triangularised;

                // Fewer equations than unknowns is not a mistake by the caller, which is what an
                // exception here used to say it was. A linear system of that shape has infinitely
                // many solutions and one family that describes them all, so it is answered as
                // that family -- the unknowns a row reduction leaves free become parameters.
                // Only the short count is taken here: a square system that is rank-deficient
                // reaches the eliminator, which already answers it.
                // https://github.com/asc-community/AngouriMath/issues/212
                if (equations.Count < vars.Length
                    && Functions.Algebra.AnalyticalSolving.LinearSystemSolver.TrySolve(
                        equations, variables, out var family))
                    return family;

                // A product is zero where any one of its factors is, and the elimination solves
                // `x * y = 0` for `y` as `0`, which is the answer only where `x` is not zero. So a
                // system with a product in more than one of its unknowns is solved once for each
                // factor, and its solutions are theirs together.
                // https://github.com/asc-community/AngouriMath/issues/2128
                if (Branches(equations, variables) is { } branches)
                    return Together(branches, written, vars, variables);

                // Nor is any other count: `x^2 + y^2 = 1` has a family of solutions in a
                // parameter, and `sin(x) = 0` with `x = 0` has one solution. Those are eliminated,
                // and a system whose answer cannot be shown to hold is declined, as `Solve`
                // documents. https://github.com/asc-community/AngouriMath/issues/212
                // An unknown that no equation mentions is free, as one is in a short system, and
                // the eliminator has no equation to take it from.
                if (equations.Count != vars.Length || variables.Any(variable => !equations.Any(equation => equation.ContainsNode(variable))))
                    return NonSquareSystemSolver.Solve(equations, variables, ledger) is { } families
                        ? new MatrixBuilder(families, vars.Length).ToMatrix()
                        : null;
                int initVarCount = vars.Length;

                var res = InSolveSystem(equations, vars, Sumf.Sum(equations), ledger);
                foreach (var tuple in res)
                    if (tuple.Count != initVarCount)
                        throw new AngouriBugException("InSolveSystem incorrect output");
                if (res.Count == 0)
                    return null;
                // Each row is put back into the system as the caller wrote it, as a root of one
                // equation is: `sqrt(y) = x` is solved for `y` as `x^2`, which is no solution
                // where `x` is negative, and `sqrt(y) - x`, `x + 1` was answered [[-1, 1]].
                // https://github.com/asc-community/AngouriMath/issues/2123
                if (RowsThatHold(written, variables, res) is not { Count: > 0 } rows)
                    return null;
                var tb = new MatrixBuilder(rows, initVarCount);
                return tb.ToMatrix();
            }
            finally
            {
                ledger.Report();
            }
        }

        /// <summary>The most systems a system is split into by its products.</summary>
        private const int MostBranches = 64;

        /// <summary>
        /// One system for each way of choosing a factor of each product in more than one of
        /// <paramref name="variables"/>, with the product replaced by the factor, or
        /// <see langword="null"/> where there is no such product or the choices are too many.
        /// </summary>
        /// <remarks>
        /// A product whose factors all mention the same unknowns is left alone: solving it for
        /// any of them reads every factor, as <c>x (x - 1)</c> is solved for <c>x</c> as
        /// <c>0</c> and <c>1</c>.
        /// </remarks>
        private static List<List<Entity>>? Branches(List<Entity> equations, Variable[] variables)
        {
            var branches = new List<List<Entity>> { new(equations) };
            var split = false;
            for (var i = 0; i < equations.Count; i++)
            {
                if (equations[i] is not Mulf)
                    continue;
                var factors = Mulf.LinearChildren(equations[i]).Where(factor => variables.Any(factor.ContainsNode)).ToList();
                if (factors.Count < 2)
                    continue;
                var first = variables.Where(factors[0].ContainsNode).ToList();
                if (factors.All(factor => variables.Where(factor.ContainsNode).SequenceEqual(first)))
                    continue;
                if (branches.Count * factors.Count > MostBranches)
                    return null;
                split = true;
                branches = branches.SelectMany(branch => factors.Select(factor =>
                {
                    var chosen = new List<Entity>(branch);
                    chosen[i] = factor;
                    return chosen;
                })).ToList();
            }
            return split ? branches : null;
        }

        /// <summary>
        /// The solutions of each of <paramref name="branches"/> together, each kept only where
        /// it satisfies the system as written: a factor can be zero where another is not
        /// defined, as <c>x ln(y)</c> is at <c>x = 0</c> and <c>y = 0</c>.
        /// </summary>
        /// <remarks>
        /// A branch answered by <see langword="null"/> is taken to have no solutions, which is
        /// what <see cref="EquationSystem.Solve"/> says of it: <c>x e^y = 0</c> has none where
        /// <c>e^y</c> is zero.
        /// </remarks>
        private static Matrix? Together(List<List<Entity>> branches, List<Entity> written, ReadOnlySpan<Variable> vars, Variable[] variables)
        {
            var rows = new List<List<Entity>>();
            foreach (var branch in branches)
            {
                if (SolveSystem(branch, vars) is not { } solved)
                    continue;
                for (var row = 0; row < solved.RowCount; row++)
                {
                    var values = Enumerable.Range(0, solved.ColumnCount).Select(column => solved[row, column]).ToList();
                    if (!rows.Any(other => other.SequenceEqual(values)))
                        rows.Add(values);
                }
            }
            if (rows.Count == 0 || RowsThatHold(written, variables, rows) is not { Count: > 0 } kept)
                return null;
            return new MatrixBuilder(kept, vars.Length).ToMatrix();
        }

        /// <summary>
        /// The rows of <paramref name="rows"/> that satisfy each of <paramref name="equations"/>,
        /// or <see langword="null"/> where a row is shown not to for some value of the symbols
        /// left in it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A row is dropped on the evidence a root of one equation is dropped on,
        /// <see cref="StatementSolver.IsEvidenceAgainst"/>: an equation that, with the row put
        /// in, has no symbol left and is a number away from zero, or is no finite number at all, as
        /// <c>x ln(y)</c> is at <c>(0, 0)</c>. Such a row is no solution for any value of anything,
        /// so dropping it loses none.
        /// </para>
        /// <para>
        /// A row with symbols left in it is tried at one value of them, and one that fails there
        /// declines the system instead of being dropped. It may hold for other values, as
        /// <c>sqrt(t^2) = t</c> does for every positive <c>t</c>, and the rows left would read as
        /// all of the solutions. A symbol of the system's own is tried at a positive value that
        /// is not whole, and a parameter that a row brings with it, a <c>t_1</c> or an
        /// <c>n_1</c>, at a whole one, since an <c>n_1</c> stands for those only.
        /// </para>
        /// <para>
        /// Anything else is kept, as a root of one equation is: a residual that cannot be
        /// evaluated, or one too small next to its terms to be told from rounding.
        /// </para>
        /// </remarks>
        private static List<List<Entity>>? RowsThatHold(List<Entity> equations, Variable[] variables, List<List<Entity>> rows)
        {
            var own = new HashSet<Variable>(equations.SelectMany(equation => equation.Vars));
            var replacements = new Dictionary<Variable, Entity>();
            var kept = new List<List<Entity>>(rows.Count);
            foreach (var row in rows)
            {
                replacements.Clear();
                for (var index = 0; index < variables.Length; index++)
                    replacements[variables[index]] = row[index];
                var residuals = equations.Select(equation => equation.Substitute(replacements)).ToList();
                if (residuals.Any(residual => !residual.Vars.Any()
                        && (residual.Evaled is Complex { IsFinite: false } || StatementSolver.IsEvidenceAgainst(residual))))
                    continue;
                if (residuals.Any(residual => residual.Vars.Any() && StatementSolver.IsEvidenceAgainst(AtASamplePoint(residual, own))))
                    return null;
                kept.Add(row);
            }
            return WithoutDecimalCopies(kept);
        }

        /// <summary>
        /// <paramref name="rows"/> without a row that is a copy of another: one equal to an
        /// earlier row, and one with a number in it that is not exact, each of whose entries is
        /// within a part in a million of an exact row's.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two branches of an elimination can reach one solution, and both were answered: the
        /// circles <c>(x - 3/2)^2 + (y - sqrt(3)/2)^2 = 1</c> and <c>(x - 1)^2 + y^2 = 1</c> met at
        /// <c>(1/2, sqrt(3)/2)</c> twice. Where one branch went through Newton's method the copy
        /// is a decimal: <c>(x - 1/2)^2 + (y - sqrt(3)/2)^2 = 1</c> with the same second circle
        /// met at <c>(3/2, sqrt(3)/2)</c> and again where <c>x</c> is <c>1.4999999999999997</c>.
        /// </para>
        /// <para>
        /// The tolerance is a part in a million, not the double's own precision: a root taken
        /// of the decimal multiplies its error, and that copy's <c>y</c> is out by about
        /// <c>2e-8</c>. A decimal row that no exact row agrees with is kept: it is the answer the
        /// search found, as a root of one equation is.
        /// https://github.com/asc-community/AngouriMath/issues/2138
        /// </para>
        /// </remarks>
        private static List<List<Entity>> WithoutDecimalCopies(List<List<Entity>> rows)
        {
            static bool HasADecimal(List<Entity> row)
                => row.Any(entry => entry.Nodes.Any(node => node is Number { IsExact: false }));
            static Complex? ValueOf(Entity entry)
                => entry.Vars.Any() ? null : entry.Evaled as Complex;
            static bool Agree(List<Entity> row, List<Entity> exact)
            {
                for (var index = 0; index < row.Count; index++)
                {
                    if (ValueOf(row[index]) is not { } value || ValueOf(exact[index]) is not { } other)
                        return false;
                    var difference = ((Complex)(value - other)).Abs().EDecimal;
                    var scale = PeterO.Numbers.EDecimal.Max(PeterO.Numbers.EDecimal.One, other.Abs().EDecimal);
                    if (difference.CompareTo(scale.Multiply(PeterO.Numbers.EDecimal.FromString("1e-6"))) > 0)
                        return false;
                }
                return true;
            }
            var distinct = new List<List<Entity>>(rows.Count);
            foreach (var row in rows)
                if (!distinct.Any(earlier => earlier.SequenceEqual(row)))
                    distinct.Add(row);
            var exactRows = distinct.Where(row => !HasADecimal(row)).ToList();
            if (exactRows.Count == distinct.Count)
                return distinct;
            return distinct.Where(row => !HasADecimal(row) || !exactRows.Any(exact => Agree(row, exact))).ToList();
        }

        /// <summary>
        /// <paramref name="residual"/> with each symbol of <paramref name="own"/> at a positive
        /// value that is not whole, and every other symbol at 2.
        /// </summary>
        private static Entity AtASamplePoint(Entity residual, HashSet<Variable> own)
        {
            var values = new[] { (137, 100), (61, 100), (223, 100), (191, 100), (83, 100), (113, 100) };
            var index = 0;
            foreach (var symbol in residual.Vars.ToList())
            {
                Entity value = Integer.Create(2);
                if (own.Contains(symbol))
                {
                    var (numerator, denominator) = values[index++ % values.Length];
                    value = Rational.Create(numerator, denominator);
                }
                residual = residual.Substitute(symbol, value);
            }
            return residual;
        }

        /// <summary>Solves system of equations</summary>
        /// <param name="equations"><see cref="List{T}"/> of <see cref="Entity"/></param>
        /// <param name="vars">
        /// <see cref="List{T}"/> of <see cref="Variable"/>s,
        /// where each of them must be mentioned in at least one entity from equations
        /// </param>
        /// <param name="nameSource">
        /// The system as a whole, so that a parameter standing for a free variable is given
        /// a name that none of the equations already uses
        /// </param>
        /// <param name="ledger">
        /// The caller's budget, drawn on once per candidate solution explored. Optional
        /// because the recursion passes it along and a caller may have none; where it is
        /// absent this is unbounded, which is what it always was.
        /// </param>
        internal static List<List<Entity>> InSolveSystem(
            List<Entity> equations, ReadOnlySpan<Variable> vars, Entity nameSource,
            BudgetLedger? ledger = null)
        {
            // Charged before the branch rather than after it, so the ceiling bounds what is
            // done rather than what has been done. One unit per candidate solution explored is
            // the right grain: this eliminates one variable per level, and each elimination
            // turns the next level's coefficients into nested radicals, so the count of
            // branches explored is what compounds. https://github.com/asc-community/AngouriMath/issues/896
            if (ledger is not null && !ledger.Spend())
                throw new NotSufficientlySupportedException(
                    "this system of equations is not solvable within the budget: the "
                    + "triangularising path declined it and eliminating in radicals has not "
                    + "finished. Raise MathS.Settings.Budget to allow more, or pass a "
                    + "cancellation token with MathS.Multithreading.SetLocalCancellationToken");
            var var = vars[^1];
            if (equations.Count == 1)
            {
                var solutions = equations[0].InnerSimplified.SolveEquation(var).InnerSimplified;
                if (solutions is FiniteSet els)
                    return els.Select(sol => new List<Entity> { sol }).ToList();
                // Nothing is left to constrain `var`, so every value of it extends to a solution
                // of the system rather than none of them doing so. Reporting no solutions here is
                // what made a solvable system come back as null from SolveSystem --
                // https://github.com/asc-community/AngouriMath/issues/550. A free parameter says
                // what the answer is in the same way the trigonometric solvers say it with n_1.
                if (solutions == MathS.Sets.C)
                    return new() { new List<Entity> { Variable.CreateUnique(nameSource, "t") } };
                return new();
            }
            var result = new List<List<Entity>>();
            var replacements = new Dictionary<Variable, Entity>();
            var remainingVars = vars.Slice(0, vars.Length - 1);
            // Which equation to eliminate `var` from is decided by whether it occurs in
            // one, and occurring is a syntactic question. Substituting an earlier variable
            // routinely leaves an occurrence that cancels: eliminating x_4 from a dense
            // 4x4 turns the next equation into x_1 + 2*x_2 + x_3 - (x_1 + x_2 + x_3 - 4) - 5,
            // where x_3 is written twice and worth nothing. Solving that for x_3 has no
            // answer, and committing to the first candidate meant the whole system was
            // then declared unsolvable -- https://github.com/asc-community/AngouriMath/issues/608.
            // So a candidate that yields nothing is passed over for the next one rather than ending the search.
            for (int i = 0; i < equations.Count; i++)
                if (equations[i].ContainsNode(var))
                {
                    if (equations[i].SolveEquation(var).InnerSimplified is not FiniteSet sols
                        || sols.Count == 0)
                        continue;

                    var rest = new List<Entity>(equations);
                    rest.RemoveAt(i);

                    foreach (var sol in sols)
                        foreach (var j in InSolveSystem(rest.Select(eq => eq.Substitute(var, sol)).ToList(), remainingVars, nameSource, ledger))
                        {
                            replacements.Clear();
                            for (int varid = 0; varid < remainingVars.Length; varid++)
                                replacements.Add(remainingVars[varid], j[varid]);
                            j.Add(sol.Substitute(replacements).InnerSimplified);
                            result.Add(j);
                        }

                    if (result.Count > 0)
                        return result;
                }
            return result;
        }
        internal static Set SolvePiecewise(Piecewise piecewise, Variable x, Func<Entity, Variable, Set> solve)
        {
            Entity cond = true;
            var res = new List<Set>();
            foreach (var c in piecewise.Cases)
            {
                res.Add(solve(c.Expression, x).Filter(c.Predicate & cond, x));
                cond &= !c.Predicate;
            }
            return res.Unite();
        }

        /// <summary>
        /// <see cref="SolvePiecewise"/> for a solver that may decline a case, which declines the
        /// whole: <see langword="null"/> where it declines any case.
        /// </summary>
        internal static Set? SolvePiecewiseOrNull(Piecewise piecewise, Variable x, Func<Entity, Variable, Set?> solve)
        {
            Entity cond = true;
            var res = new List<Set>();
            foreach (var c in piecewise.Cases)
            {
                if (solve(c.Expression, x) is not { } solved)
                    return null;
                res.Add(solved.Filter(c.Predicate & cond, x));
                cond &= !c.Predicate;
            }
            return res.Unite();
        }
    }
}