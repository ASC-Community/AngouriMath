//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Budgets;
using AngouriMath.Core.Exceptions;
using System;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    /// <summary>
    /// A system with more or fewer equations than unknowns that neither the triangularisation nor
    /// <see cref="LinearSystemSolver"/> takes, such as <c>x^2 + y^2 = 1</c> for <c>x</c> and
    /// <c>y</c>, or <c>sin(x) = 0</c> and <c>x = 0</c> for <c>x</c>. Each unknown is eliminated in
    /// turn, an unknown that nothing is left to constrain becomes a parameter, and every family
    /// found is substituted back into every equation.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/212">#212</a>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An answer is given only where it is shown to hold.</b> The elimination trusts each solve
    /// it makes, and a solve can answer for a case its equation does not cover: <c>sqrt(y) = x</c>
    /// is solved for <c>y</c> as <c>x^2</c>, which is no solution where <c>x</c> is negative. So
    /// each family is kept only where every equation, with the family substituted, simplifies to
    /// <c>0</c>. <c>sqrt(t^2) - t</c> does not, and that system is declined.
    /// </para>
    /// <para>
    /// <b>A decline declines the whole system.</b> An equation left with no unknown in it is zero,
    /// a number away from zero, or neither as far as can be told, like <c>(a + b)/2 - c</c>. A
    /// branch where one is away from zero has no solutions and is dropped. A branch where one
    /// cannot be told gives no answer for the system, rather than leaving the other branches to
    /// read as all of its solutions.
    /// </para>
    /// <para>
    /// A square system does not come here. It is the eliminator's in
    /// <see cref="EquationSolver.InSolveSystem"/>, whose answers this would change.
    /// </para>
    /// </remarks>
    internal static class NonSquareSystemSolver
    {
        /// <summary>
        /// The solutions of <paramref name="equations"/> in <paramref name="variables"/>, a list of
        /// values in their order for each family, or <see langword="null"/> where none were found.
        /// </summary>
        internal static List<List<Entity>>? Solve(List<Entity> equations, Variable[] variables, BudgetLedger ledger)
        {
            // One parameter per unknown that is left free, the same in every branch. Each is
            // minted against the equations and the parameters already minted, since CreateUnique
            // compares whole names in use and would otherwise hand back the same name twice.
            var parameters = new Variable?[variables.Length];
            Entity nameSource = equations.Count > 0 ? Sumf.Sum(equations) : Integer.Zero;
            Variable ParameterFor(int index)
            {
                if (parameters[index] is { } minted)
                    return minted;
                var parameter = Variable.CreateUnique(nameSource, "t");
                nameSource += parameter;
                return parameters[index] = parameter;
            }

            if (Eliminate(equations, variables, variables.Length, ParameterFor, ledger) is not { Count: > 0 } families)
                return null;
            var own = new HashSet<Variable>(equations.SelectMany(equation => equation.Vars));
            foreach (var family in families)
                foreach (var equation in equations)
                {
                    var substituted = equation;
                    for (var index = 0; index < variables.Length; index++)
                        substituted = substituted.Substitute(variables[index], family[index]);
                    // An n_1 a family brings from a periodic root stands for every whole number.
                    var whole = substituted.Vars.Where(name => !own.Contains(name) && name.Name.StartsWith("n_")).Distinct().ToList();
                    if (!Vanishes(substituted, whole))
                        return null;
                }
            return families;
        }

        /// <summary>
        /// The solutions in the first <paramref name="count"/> of <paramref name="variables"/>, each
        /// a list of their values in order. Empty where there are none, and
        /// <see langword="null"/> where that could not be told.
        /// </summary>
        private static List<List<Entity>>? Eliminate(List<Entity> equations, Variable[] variables, int count,
            Func<int, Variable> parameterFor, BudgetLedger ledger)
        {
            // Charged as InSolveSystem charges it: one unit for each candidate explored.
            if (!ledger.Spend())
                throw new NotSufficientlySupportedException(
                    "this system of equations is not solvable within the budget. Raise "
                    + "MathS.Settings.Budget to allow more, or pass a cancellation token with "
                    + "MathS.Multithreading.SetLocalCancellationToken");

            // No unknown is left, so each equation holds as it stands or does not.
            if (count == 0)
            {
                var told = true;
                foreach (var equation in equations)
                    switch (Holds(equation))
                    {
                        case false:
                            return new();
                        case null:
                            told = false;
                            break;
                    }
                return told ? new() { new() } : null;
            }

            // No equation is left, so every unknown left is free.
            if (equations.Count == 0)
            {
                var free = new List<Entity>();
                for (var index = 0; index < count; index++)
                    free.Add(parameterFor(index));
                return new() { free };
            }

            var variable = variables[count - 1];
            // The equation with the fewest occurrences of the unknown first, which is solved by
            // inverting it and has the plainest roots: of `cos(x) = sin(x)` and `x = pi/4`, the
            // first is solved as ln(sqrt(i))/i and the second as pi/4.
            var candidates = Enumerable.Range(0, equations.Count)
                .Where(i => equations[i].ContainsNode(variable))
                .OrderBy(i => equations[i].Nodes.Count(node => node == variable))
                .ThenBy(i => equations[i].Complexity)
                .ToList();
            foreach (var i in candidates)
            {
                // An empty set is passed over as InSolveSystem passes it over: substituting an
                // earlier unknown can leave an occurrence that cancels, and then the solve has
                // nothing to say about this one.
                if (equations[i].SolveEquation(variable).InnerSimplified is not FiniteSet roots || roots.Count == 0)
                    continue;
                var rest = new List<Entity>(equations);
                rest.RemoveAt(i);
                if (EachRoot(roots, rest, variables, count, parameterFor, ledger) is { } solutions)
                    return solutions;
                // This equation's roots left a question that could not be told. Another
                // equation's may not: `sin(x) = 0` gives x = 2 pi n_1, where `x = 0` gives 0.
            }

            if (candidates.Count > 0)
                return null;
            // Nothing constrains this unknown, so it is a parameter in every solution of the rest.
            var others = Eliminate(equations, variables, count - 1, parameterFor, ledger);
            others?.ForEach(family => family.Add(parameterFor(count - 1)));
            return others;
        }

        /// <summary>
        /// The solutions where the last of the unknowns is each of <paramref name="roots"/> in
        /// turn, or <see langword="null"/> where that could not be told for one of them.
        /// </summary>
        private static List<List<Entity>>? EachRoot(FiniteSet roots, List<Entity> rest, Variable[] variables, int count,
            Func<int, Variable> parameterFor, BudgetLedger ledger)
        {
            var variable = variables[count - 1];
            var solutions = new List<List<Entity>>();
            foreach (var root in roots)
            {
                var substituted = rest.Select(equation => equation.Substitute(variable, root)).ToList();
                if (Eliminate(substituted, variables, count - 1, parameterFor, ledger) is not { } families)
                    return null;
                foreach (var family in families)
                {
                    var value = root;
                    for (var index = 0; index < count - 1; index++)
                        value = value.Substitute(variables[index], family[index]);
                    family.Add(value.InnerSimplified);
                    solutions.Add(family);
                }
            }
            return solutions;
        }

        /// <summary>
        /// Whether <paramref name="equation"/>, with no unknown left in it, holds:
        /// <see langword="true"/> where it simplifies to zero, <see langword="false"/> where it is a
        /// number away from zero, and <see langword="null"/> where neither can be told.
        /// </summary>
        private static bool? Holds(Entity equation)
        {
            if (Vanishes(equation, System.Array.Empty<Variable>()))
                return true;
            if (equation.Vars.Any() || equation.Evaled is not Complex value || value.IsNaN)
                return null;
            // An exact number that is not zero is not zero. A decimal is taken as away from zero
            // only well past the digits its evaluation may have lost.
            if (value is Rational)
                return false;
            return ((Real)value.Abs()).EDecimal.ToDouble() > 1e-9 ? false : null;
        }

        /// <summary>
        /// Whether <paramref name="expression"/> is zero for every value of its symbols: zero as it
        /// is evaluated, or simplified to zero. A simplification to <c>0 provided C</c> counts only
        /// where <c>C</c> holds with the symbols pinned, since <c>t - c (t/c)</c> simplifies to
        /// <c>0 provided not c = 0</c> for a <c>c</c> that is zero without being written
        /// <c>0</c>, such as <c>(a + 1)^2 - a^2 - 2a - 1</c>.
        /// </summary>
        /// <remarks>
        /// A symbol in <paramref name="whole"/> stands for every whole number, so it is pinned at
        /// one, and the expression is zero where it is zero for each: <c>sin(pi/6 + 2 pi n_1) - 1/2</c>
        /// is decided as <c>forall n_1 in ZZ : sin(pi/6 + 2 pi n_1) - 1/2 = 0</c>, which takes the
        /// whole periods out. https://github.com/asc-community/AngouriMath/issues/212
        /// </remarks>
        private static bool Vanishes(Entity expression, IReadOnlyList<Variable> whole)
        {
            if (expression.InnerSimplified.Evaled is Complex { IsZero: true })
                return true;
            // Away from zero at a point is not zero everywhere, which settles most without
            // simplifying.
            if (Pinned(expression, whole).Evaled is Complex { IsNaN: false } atAPoint
                && ((Real)atAPoint.Abs()).EDecimal.ToDouble() > 1e-9)
                return false;
            if (whole.Count > 0)
            {
                Entity statement = expression.EqualTo(Integer.Zero);
                foreach (var name in whole)
                    statement = MathS.ForAll(name, MathS.Sets.Z, statement);
                if (statement.Simplify() == Entity.Boolean.True)
                    return true;
            }
            return expression.Simplify() switch
            {
                Providedf(var simplified, var condition) =>
                    simplified.Evaled is Complex { IsZero: true } && Pinned(condition, whole).Evaled is Entity.Boolean { Value: true },
                var simplified => simplified.Evaled is Complex { IsZero: true },
            };
        }

        /// <summary>
        /// <paramref name="expression"/> with each of its symbols at a fixed value, a whole one for
        /// a symbol in <paramref name="whole"/>.
        /// </summary>
        private static Entity Pinned(Entity expression, IReadOnlyList<Variable> whole)
        {
            var values = new[] { 1.37, 0.61, 2.23, 1.91, 0.83, 1.13 };
            var wholeValues = new[] { 2, -1, 3 };
            var (index, wholeIndex) = (0, 0);
            foreach (var symbol in expression.Vars.ToList())
                expression = whole.Contains(symbol)
                    ? expression.Substitute(symbol, wholeValues[wholeIndex++ % wholeValues.Length])
                    : expression.Substitute(symbol, values[index++ % values.Length]);
            return expression;
        }
    }
}
