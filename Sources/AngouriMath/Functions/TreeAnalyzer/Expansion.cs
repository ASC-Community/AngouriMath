//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using AngouriMath.Core.Exceptions;
using AngouriMath.Core.Multithreading;
using PeterO.Numbers;

namespace AngouriMath.Functions
{
    using static Entity;
    using static Entity.Number;
    internal static partial class TreeAnalyzer
    {
        /// <summary>
        /// Finds out how many terms we get after expansion via binomial coefficients, e. g
        /// (a + b) ^ 3 -> 2, 3 -> 4
        /// </summary>
        internal static EInteger EstimateTermCount(EInteger numberOfTerms, EInteger power) =>
            (power + numberOfTerms - 1).Combinations(power);

        /// <summary>
        /// Returns a list of linear children over sum
        /// where at most one term doesn't contain x, e. g.
        /// x2 + x + a + b + x
        /// =>
        /// [x2, x, a + b, x]
        /// </summary>
        internal static List<Entity>? GatherLinearChildrenOverSumAndExpand(Entity expr, Func<Entity, bool> conditionForUniqueTerms)
        {
            if (expr is not (Sumf or Minusf))
                return SmartExpandOver(expr, conditionForUniqueTerms);
            var res = new List<Entity>();
            Entity? freeTerm = null;
            foreach (var child in Sumf.LinearChildren(expr))
                if (conditionForUniqueTerms(child))
                {
                    var expanded = SmartExpandOver(child, conditionForUniqueTerms);
                    if (expanded is null)
                        return null;
                    res.AddRange(expanded);
                }
                else
                    freeTerm = freeTerm is { } ? freeTerm + child : child;
            if (freeTerm is { })
                res.Add(freeTerm);
            return res;
        }

        /// <summary>
        /// CombinateSums(3, 5) ->
        /// { 0, 0, 5 }, { 0, 1, 4 }, { 0, 2, 3 }, { 0, 3, 2 }, { 0, 4, 1 },
        /// { 0, 5, 0 }, { 1, 0, 4 }, { 1, 1, 3 }, { 1, 2, 2 }, { 1, 3, 1 },
        /// { 1, 4, 0 }, { 2, 0, 3 }, { 2, 1, 2 }, { 2, 2, 1 }, { 2, 3, 0 },
        /// { 3, 0, 2 }, { 3, 1, 1 }, { 3, 2, 0 }, { 4, 0, 1 }, { 4, 1, 0 }, { 5, 0, 0 }
        /// </summary>
        internal static IEnumerable<List<EInteger>> CombinateSums(EInteger itemCount, EInteger targetSum)
        {
            // Combinations(2, 6, 3) ->
            // { 4, 3, 2 }, { 5, 3, 2 }, { 6, 3, 2 }, { 5, 4, 2 }, { 6, 4, 2 },
            // { 6, 5, 2 }, { 5, 4, 3 }, { 6, 4, 3 }, { 6, 5, 3 }, { 6, 5, 4 }
            static IEnumerable<List<EInteger>> Combinations(EInteger min, EInteger max, EInteger cellCount)
            {
                for (EInteger i = min; i <= max; i++)
                    if (!cellCount.Equals(EInteger.One))
                        foreach (var l in Combinations(i + 1, max, cellCount - 1))
                        {
                            l.Add(i);
                            yield return l;
                        }
                    else
                        yield return new List<EInteger> { i };
            }
            foreach (var comb in Combinations(1, targetSum + itemCount - 1, itemCount - 1))
            {
                var newComb = new List<EInteger> { 0 };
                comb.Reverse();
                newComb.AddRange(comb);
                newComb.Add(targetSum + itemCount);

                var item = new List<EInteger>();
                for (int i = 0; i < itemCount; i++)
                    item.Add(newComb[i + 1] - newComb[i] - 1);
                yield return item;
            }
        }
        /// <summary><paramref name="expr"/> is NEITHER <see cref="Sumf"/> NOR <see cref="Minusf"/></summary>
        /// <remarks>
        /// Checks for cancellation on every call, as the other steps that can run long do: an
        /// expansion of a product of sums is bounded by
        /// <see cref="MathS.Settings.MaxExpansionTermCount"/> only once its terms are counted, and
        /// the products before that can take seconds a computation under a budget does not have.
        /// https://github.com/asc-community/AngouriMath/issues/718
        /// </remarks>
        internal static List<Entity>? SmartExpandOver(Entity expr, Func<Entity, bool> conditionForUniqueTerms)
        {
            MultithreadingFunctional.ExitIfCancelled();
            var newChildren = new List<Entity>();
            switch (expr)
            {
                case Sumf or Minusf:
                    throw new AngouriBugException("SmartExpandOver must be only called of non-sum expression");
                case Divf:
                    expr = expr.Replace(Patterns.ExpandFactorialDivisions);
                    // Cancelling a quotient of factorials can leave a sum -- (x + 1)! / x!
                    // is x + 1 -- and this method is documented not to take one. Going back
                    // in through the sum-aware entry point costs nothing when the result is
                    // not a sum, since that is where it sends anything else anyway.
                    // https://github.com/asc-community/AngouriMath/issues/817
                    if (!(expr is Divf(var dividend, var divisor)))
                        return GatherLinearChildrenOverSumAndExpand(expr, conditionForUniqueTerms);
                    var numChildren = GatherLinearChildrenOverSumAndExpand(dividend, conditionForUniqueTerms);
                    if (numChildren is null || numChildren.Count > MathS.Settings.MaxExpansionTermCount)
                        return null;
                    return numChildren.Select(c => c / divisor).ToList();
                case Mulf(var multiplier, var multiplicand):
                    var oneChildren = GatherLinearChildrenOverSumAndExpand(multiplier, conditionForUniqueTerms);
                    var twoChildren = GatherLinearChildrenOverSumAndExpand(multiplicand, conditionForUniqueTerms);
                    if (oneChildren is null || twoChildren is null)
                        return null;
                    if (oneChildren.Count * twoChildren.Count > MathS.Settings.MaxExpansionTermCount)
                        return null;
                    foreach (var one in oneChildren)
                        foreach (var two in twoChildren)
                            newChildren.Add(one * two);
                    return newChildren;
                case Powf(var @base, Integer { EInteger: var power }) when power >= 1:
                    var linBaseChildren = GatherLinearChildrenOverSumAndExpand(@base, conditionForUniqueTerms);
                    if (linBaseChildren is null)
                        return null;
                    if (linBaseChildren.Count == 1)
                        return linBaseChildren[0] switch
                        {
                            // (a * b)^2 = a^2 * b^2
                            Mulf(var multiplier, var multiplicand) =>
                                new List<Entity> { new Powf(multiplier, power) * new Powf(multiplicand, power) },
                            // (a / b)^2 = a^2 / b^2
                            Divf(var baseDividend, var baseDivisor) =>
                                new List<Entity> { new Powf(baseDividend, power) / new Powf(baseDivisor, power) },
                            _ => new List<Entity> { expr }
                        };
                    if (EstimateTermCount(linBaseChildren.Count, power) >
                        EInteger.FromInt64(MathS.Settings.MaxExpansionTermCount))
                        return null;
                    foreach (var powerListForTerm in CombinateSums(linBaseChildren.Count, power))
                    {
                        EInteger biCoef = 1;
                        EInteger sumPow = power;
                        foreach (var pow in powerListForTerm)
                        {
                            biCoef *= sumPow.Combinations(pow);
                            sumPow -= pow;
                        }
                        Entity term = Integer.Create(biCoef);
                        for (int i = 0; i < powerListForTerm.Count; i++)
                            if (powerListForTerm[i].Equals(1))
                                term *= linBaseChildren[i];
                            else if (powerListForTerm[i] > 1)
                                term *= MathS.Pow(linBaseChildren[i], Integer.Create(powerListForTerm[i]));
                        newChildren.AddRange(SmartExpandOver(term, conditionForUniqueTerms) ?? throw new AngouriBugException("Null unexpected"));
                    }
                    return newChildren;
            }

            return new List<Entity> { expr };
        }
    }
}
