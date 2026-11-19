//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Extensions;
using AngouriMath.Functions.Algebra;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions.Continuous.Solvers.SetSolver
{
    internal static class AnalyticalSetSolver
    {
        internal static Set Solve(Entity left, Entity right, Variable x)
        {
            switch (left, right)
            {
                case (Providedf(var l, var predicate), _): return EquationSolver.FilterRoots(Solve(l, right, x), predicate, x);
                case (_, Providedf(var r, var predicate)): return EquationSolver.FilterRoots(Solve(left, r, x), predicate, x);
                case (Piecewise p, _): return EquationSolver.SolvePiecewise(p, x, (e, x) => Solve(e, right, x));
                case (_, Piecewise p): return EquationSolver.SolvePiecewise(p, x, (e, x) => Solve(left, e, x));
            }

            left = left.Replace(Patterns.SetOperatorRules);
            right = right.Replace(Patterns.SetOperatorRules);
            if (left.DirectChildren.Count<Entity>(c => c == x) 
                +
                right.DirectChildren.Count<Entity>(c => c == x) != 1)
                return Empty;
            // As in the equation solver, an inversion with no written form leaves it unsolved.
            var roots = left.ContainsNode(x) ? left.Invert(right, x) : right.Invert(left, x);
            return roots is null ? new ConditionalSet(x, left.EqualTo(right)) : roots.ToSet();
        }
    }
}
