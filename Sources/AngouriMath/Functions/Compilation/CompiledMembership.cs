//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System.Linq;
using NumberStatements = AngouriMath.Numerics.NumberStatements;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Set;

namespace AngouriMath.Core.Compilation
{
    /// <summary>
    /// A membership written, for the compilers, as the statement it makes about its element; the set is fixed
    /// when the expression is compiled, so only the element is computed. In a union, an intersection or a
    /// difference, Kleene's connectives of the memberships in its sides, as evaluation decides them; in an
    /// interval, the element real and between the ends; in a finite set, equal to one of its members; and in a
    /// conditional set, the set's predicate of it. A membership in a set of numbers is left, for each compiler to
    /// compile itself, and one in any other set, or with any other set inside, is left as written, which neither
    /// compiles.
    /// https://github.com/asc-community/AngouriMath/issues/2195
    /// </summary>
    internal static class CompiledMembership
    {
        internal static Entity Rewritten(Entity expression)
            => expression.Nodes.Any(node => node is Inf or Subsetf or Cardf or Quantifier)
                ? expression.Replace(Rewrite)
                : expression;

        // Past this many members a statement over a finite set is left as written: the cardinality and the
        // unique existence grow with the square of the count.
        private const int LargestExpanded = 64;

        // A subset, a cardinality and a quantifier over a finite set are statements about its members, as evaluation
        // decides them, and any other is left as written, which neither compiler compiles.
        // https://github.com/asc-community/AngouriMath/issues/2195
        private static Entity Rewrite(Entity node) => node switch
        {
            Inf(var element, var set) => Statement(element, set) ?? node,
            Subsetf(FiniteSet sub, var super) when sub.Count <= LargestExpanded
                => sub.Select(member => Statement(member, super)).ToList() is var each && each.All(statement => statement is not null)
                    ? each.Aggregate(Boolean.True as Entity, (all, statement) => all & statement!)
                    : node,
            Cardf(FiniteSet set) when set.Count <= LargestExpanded => Cardinality(set.ToList()),
            Forallf(var variable, FiniteSet over, var body) when over.Count <= LargestExpanded
                => over.Aggregate(Boolean.True as Entity, (all, member) => all & body.Substitute(variable, member)),
            Existsf(var variable, FiniteSet over, var body) when over.Count <= LargestExpanded
                => over.Aggregate(Boolean.False as Entity, (any, member) => any | body.Substitute(variable, member)),
            ExistsUniquef(var variable, FiniteSet over, var body) when over.Count <= LargestExpanded
                => ExactlyOne(over.Select(member => body.Substitute(variable, member)).ToList()),
            _ => node
        };

        // The number of distinct values among the members: each counts where it equals none before it, and the
        // count has no value where an equality has none.
        private static Entity Cardinality(System.Collections.Generic.List<Entity> members)
        {
            Entity count = Number.Integer.Zero;
            for (var i = 0; i < members.Count; i++)
            {
                Entity unseen = Boolean.True;
                for (var j = 0; j < i; j++)
                    unseen &= !members[i].Equalizes(members[j]);
                count += new Iversonf(unseen);
            }
            return count;
        }

        // Exactly one of the statements holds, by Kleene's connectives.
        private static Entity ExactlyOne(System.Collections.Generic.List<Entity> statements)
        {
            Entity any = Boolean.False;
            for (var i = 0; i < statements.Count; i++)
            {
                var alone = statements[i];
                for (var j = 0; j < statements.Count; j++)
                    if (j != i)
                        alone &= !statements[j];
                any |= alone;
            }
            return any;
        }

        private static Entity? Statement(Entity element, Entity set) => set switch
        {
            Unionf(var left, var right) => Statement(element, left) is { } inLeft && Statement(element, right) is { } inRight ? inLeft | inRight : null,
            Intersectionf(var left, var right) => Statement(element, left) is { } inLeft && Statement(element, right) is { } inRight ? inLeft & inRight : null,
            SetMinusf(var left, var right) => Statement(element, left) is { } inLeft && Statement(element, right) is { } inRight ? inLeft & !inRight : null,
            FiniteSet finite => finite.Count == 0 ? Boolean.False : finite.Select(member => element.Equalizes(member)).Aggregate((one, another) => one | another),
            // A number that is not real is in no interval, which the comparisons alone would leave with no truth
            // value: i in [0; 1] is False.
            Interval(var from, var fromClosed, var to, var toClosed)
                => new Equalsf(new Imf(element), Number.Integer.Zero)
                   & (fromClosed ? new LessOrEqualf(from, element) : (Entity)new Lessf(from, element))
                   & (toClosed ? new LessOrEqualf(element, to) : (Entity)new Lessf(element, to)),
            ConditionalSet(var variable, var predicate) => predicate.Substitute(variable, element),
            SpecialSet special when NumberStatements.Of(special) is not null => new Inf(element, special),
            _ => null
        };
    }
}
