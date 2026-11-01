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
                    if (!(mpChild is Powf(var @base, var exponent) && exponent.InnerSimplified is Number num and not Integer)) // (x + 3) ^ 3
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
            for (int i = 1; i < fractioned.Count; i++)
            {
                if (BasesAreEqual(fractioned[i].fracs, fractioned[0].fracs))
                {
                    var were = fractioned[0];
                    fractioned[0] = (were.multiplier + fractioned[i].multiplier, were.fracs);
                }
                else
                    return false;
            }

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
    }
}
