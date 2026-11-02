//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Multithreading;
using PeterO.Numbers;

namespace AngouriMath.Functions.Algebra.AnalyticalSolving
{
    using static Entity;
    using static Entity.Number;
    internal static class FractionedPolynoms
    {
        internal static bool TrySolve(Entity expr, Variable x, out Set dst)
        {
            dst = Set.Empty;
            var children = TreeAnalyzer.GatherLinearChildrenOverSumAndExpand(
                expr, entity => entity.ContainsNode(x)
            );

            if (children is null)
                return false;

            Entity normalPolynom = 0;
            var fractioned = new List<(Entity multiplier, List<(Entity main, Integer pow)> fracs)>();

            // Use PowerRules to replace sqrt(f(x))^2 => f(x)
            foreach (var child in children.Select(c => c.InnerSimplified.Replace(Patterns.PowerRules)))
            {
                (Entity multiplier, List<(Entity main, Integer pow)> fracs) potentialFraction;
                potentialFraction.multiplier = 1;
                potentialFraction.fracs = new List<(Entity main, Integer pow)>();
                foreach (var mpChild in Mulf.LinearChildren(child))
                {
                    // The exponent is folded first: the power rules write x * sqrt(x) as x ^ (1/2 + 1),
                    // whose exponent is a sum, and read as it stands that was no fractional power, so
                    // sqrt(x) - 1/x - 1 - i = 0, cleared of its denominator, went to Newton's method and
                    // was answered without its root -1.
                    // And only a power of something in x is a radical to raise. sqrt(2) is a coefficient:
                    // taken for one, -2 sqrt(2) + |x| + (2 sqrt(2) - 2) x + x^2 = 0 was squared into
                    // (|x| + ...)^2 = 8, whose substitution gave back the equation it started from, and
                    // the solver recursed until the stack ran out.
                    if (!(mpChild is Powf(var @base, var exponent) && @base.ContainsNode(x) && exponent.InnerSimplified is Number num and not Integer)) // (x + 3) ^ 3
                    {
                        potentialFraction.multiplier *= mpChild;
                        continue;
                    }
                    if (num is not Rational fracNum)
                        return false; // (x + 1)^0.2348728
                    // A power past one is a whole power times a proper fractional one, x^(3/2) being
                    // x * sqrt(x), so it shares its radical with sqrt(x): 3 sqrt(x) - x^(3/2) is the one
                    // term (3 - x) sqrt(x), where read as sqrt(x) and sqrt(x^3) it was two radicals that
                    // cannot be raised together, and the equation went to Newton's method without its
                    // root 0. Raising to the power keeps every root whichever root is taken, since
                    // (x^(p/q))^q is x^p, and the roots it adds are not the equation's.
                    var den = fracNum.ERational.Denominator;
                    var whole = fracNum.ERational.Numerator.Divide(den);
                    var rest = fracNum.ERational.Numerator.Subtract(whole.Multiply(den));
                    if (rest.Sign < 0)
                    {
                        whole = whole.Subtract(EInteger.One);
                        rest = rest.Add(den);
                    }
                    if (!whole.IsZero)
                        potentialFraction.multiplier *= MathS.Pow(@base, Integer.Create(whole));
                    var newChild = MathS.Pow(@base, Integer.Create(rest)).InnerSimplified;
                    potentialFraction.fracs.Add((newChild, den));

                    MultithreadingFunctional.ExitIfCancelled();
                }

                if (potentialFraction.fracs.Count > 0)
                    fractioned.Add(potentialFraction);
                else
                    normalPolynom += child;
            }

            if (fractioned.Count == 0)
                return false; // means that one can either be solved polynomially or unsolvable at all

            // starting from i = 1 check if all are equal to [0]
            static bool BasesAreEqual(List<(Entity main, Integer pow)> f1,
                List<(Entity main, Integer pow)> f2)
            {
                if (f1.Count != f2.Count)
                    return false;
                for (int i = 0; i < f1.Count; i++)
                    if (f1[i].main != f2[i].main || f1[i].pow != f2[i].pow)
                        return false;
                return true;
            }
            // Terms under one radical are gathered into one term.
            var groups = new List<(Entity multiplier, List<(Entity main, Integer pow)> fracs)>();
            foreach (var term in fractioned)
            {
                var same = groups.FindIndex(group => BasesAreEqual(group.fracs, term.fracs));
                if (same < 0)
                    groups.Add(term);
                else
                    groups[same] = (groups[same].multiplier + term.multiplier, groups[same].fracs);
            }
            // Only with no other name in the equation: squaring adds roots, and each is put back
            // into the equation to be dropped where it fails, which with a parameter left in cannot
            // be decided. sqrt(x + 1) + sqrt(x + 2) + a + x = 0 kept all four roots of its square,
            // and at a = 2 none of them is a root.
            if (groups.Count == 2 && expr.Vars.Count == 1
                && TwoSquareRootsSquaredAway(groups[0], groups[1], normalPolynom) is { } squared)
            {
                dst = (Set)AnalyticalEquationSolver.Solve(squared, x).InnerSimplified;
                return true;
            }
            if (groups.Count != 1)
                return false;
            fractioned = groups;

            var (multiplier, fracs) = fractioned[0];

            var lcm = fracs.Select(c => c.pow.EInteger).Aggregate((aggregate, current) => aggregate.Lcm(current));
            var intLcm = Integer.Create(lcm);

            //                        "-" to compensate sum: x + sqrt(x + 1) = 0 => x = -sqrt(x+1)
            var mp = MathS.Pow(-multiplier, intLcm).InnerSimplified;
            foreach (var (main, pow) in fracs)
                mp *= MathS.Pow(main, Integer.Create(lcm.Divide(pow.EInteger)));

            MultithreadingFunctional.ExitIfCancelled();

            var finalExpr = MathS.Pow(normalPolynom, intLcm) - mp;

            dst = (Set)AnalyticalEquationSolver.Solve(finalExpr, x).InnerSimplified;
            return true;
        }

        /// <summary>
        /// <c>m_1 sqrt(A) + m_2 sqrt(B) + P = 0</c>, two different square roots and nothing else
        /// fractional, squared until neither is left: <c>m_1^2 A = (P + m_2 sqrt(B))^2</c>, so
        /// <c>2 P m_2 sqrt(B) = m_1^2 A - P^2 - m_2^2 B</c>, and squared again
        /// <c>4 P^2 m_2^2 B = (m_1^2 A - P^2 - m_2^2 B)^2</c>. Each squaring keeps every root of
        /// the equation it squares, so this has all of them, and the roots it adds are not the
        /// equation's; the caller puts each back in. Without it two different radicals were not
        /// raised at all, and sqrt(-4 - 4x) + sqrt(2 + x) = -3 - 4x went to Newton's method, which
        /// answered its root -1.195 and not its root -1, where the first radical's argument is 0.
        /// Null where either is not a lone square root.
        /// </summary>
        private static Entity? TwoSquareRootsSquaredAway(
            (Entity multiplier, List<(Entity main, Integer pow)> fracs) first,
            (Entity multiplier, List<(Entity main, Integer pow)> fracs) second,
            Entity rest)
        {
            if (first.fracs is not [(var a, Integer(2))] || second.fracs is not [(var b, Integer(2))])
                return null;
            var m1 = first.multiplier;
            var m2 = second.multiplier;
            var once = m1 * m1 * a - rest * rest - m2 * m2 * b;
            return (4 * rest * rest * m2 * m2 * b - once * once).InnerSimplified;
        }
    }
}
