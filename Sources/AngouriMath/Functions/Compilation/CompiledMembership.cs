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
            => expression.Nodes.Any(node => node is Inf)
                ? expression.Replace(node => node is Inf(var element, var set) && Statement(element, set) is { } statement ? statement : node)
                : expression;

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
