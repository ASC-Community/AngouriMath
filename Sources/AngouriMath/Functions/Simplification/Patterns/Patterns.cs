//
// Copyright (c) 2019-2022 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Functions
{
    internal static partial class Patterns
    {
        /// <remarks>Internal so the data form of this set calls it rather than repeating it.</remarks>
        internal static Entity SortAndGroup(Entity original, IEnumerable<Entity> children, TreeAnalyzer.SortLevel level, Func<Entity, Entity, Entity> ctor)
        {
            var groups = new Dictionary<string, List<Entity>>();
            foreach (var child in children)
            {
                var hash = child.SortHash(level);
                if (!groups.ContainsKey(hash))
                    groups[hash] = new();
                groups[hash].Add(child);
            }
            var sorted = groups.OrderBy(pair => pair.Key).Select(pair => pair.Value.Aggregate(ctor)).Aggregate(ctor);
            // Hand the original back where the sort changed nothing. Aggregate builds a fresh
            // node whether or not the order moved, and an Entity memoises InnerSimplified, Evaled
            // and its complexity on the instance -- so rebuilding an already-ordered chain threw
            // all of that away and made the next pass re-derive it from cold.
            // https://github.com/asc-community/AngouriMath/issues/975
            return sorted == original ? original : sorted;
        }

        /// <summary>
        /// The factors of a product sorted and grouped, unless one of them is a transpose, which is a matrix: matrices do
        /// not commute, and AᵀA is not AAᵀ. The product is then left in the order it is written, its other factors
        /// included, since a name beside a transpose may stand for a matrix too; a name carries no type to say which
        /// (https://github.com/asc-community/AngouriMath/issues/1237).
        /// https://github.com/asc-community/AngouriMath/issues/2147
        /// </summary>
        internal static Entity SortAndGroupFactors(Entity original, TreeAnalyzer.SortLevel level)
        {
            var factors = Mulf.LinearChildren(original);
            for (var i = 0; i < factors.Count; i++)
                if (factors[i] is Transposef or Powf(Transposef, _))
                    return original;
            return SortAndGroup(original, factors, level, static (a, b) => a * b);
        }

        /// <summary>Actual sorting with <see cref="Entity.SortHash(TreeAnalyzer.SortLevel)"/></summary>
        [AddressableRules]
        internal static Func<Entity, Entity> SortRules(TreeAnalyzer.SortLevel level) => x => x switch
        {
            Sumf or Minusf =>
                SortAndGroup(x, Sumf.LinearChildren(x), level, (a, b) => a + b),
            Mulf or Divf =>
                SortAndGroupFactors(x, level),
            Andf =>
                SortAndGroup(x, Andf.LinearChildren(x), level, (a, b) => a & b),
            Orf =>
                SortAndGroup(x, Orf.LinearChildren(x), level, (a, b) => a | b),
            Unionf =>
                SortAndGroup(x, Unionf.LinearChildren(x), level, (a, b) => a.Unite(b)),
            Intersectionf =>
                SortAndGroup(x, Intersectionf.LinearChildren(x), level, (a, b) => a.Intersect(b)),
            Xorf => 
                SortAndGroup(x, Xorf.LinearChildren(x), level, (a, b) => a ^ b),
            _ => x,
        };
        [AddressableRules]
        // The division is only the quotient where the divisor has a value. `ln(x) / ln(x)` divides
        // out to `1 + 0 / ln(x)`, which is 1 at x = 0 while the quotient it came from is undefined
        // there -- ln(0) is a pole, and the remainder term carries only `ln(x) != 0`. The condition
        // folds away for any divisor that cannot be undefined.
        // https://github.com/asc-community/AngouriMath/issues/1174
        internal static Entity PolynomialLongDivision(Entity x) =>
            x is Divf(var num, var denom)
            && TreeAnalyzer.PolynomialLongDivision(num, denom) is var (divided, remainder)
            ? (divided + remainder).Provided(denom.DomainCondition)
            : x;

        /// <summary>
        /// A quotient of polynomials put into lowest terms:
        /// <c>(x^2 + 2xy + y^2) / (x^2 - y^2)</c> becomes <c>(x + y) / (x - y)</c>.
        /// See <see cref="PolynomialGcd"/> for what is and is not attempted, and
        /// <a href="https://github.com/asc-community/AngouriMath/issues/55">#55</a>.
        /// </summary>
        [AddressableRules]
        internal static Entity PolynomialGcdCancellation(Entity x) =>
            x is Divf(var num, var denom)
            && PolynomialGcd.TryCancel(num, denom, out var cancelled)
            ? cancelled
            : x;
    }
}