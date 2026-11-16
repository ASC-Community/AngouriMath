//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;

namespace AngouriMath.Core.Transformations
{
    /// <summary>
    /// A total order on expressions, for choosing <i>the</i> representative of a set of equal
    /// ones rather than a nice one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this is not a <see cref="CostModel"/>.</b> A cost model answers "which of these is
    /// nicer" with a <see cref="double"/>, and two expressions it cannot separate therefore tie —
    /// which <see cref="CostModel"/>'s own remarks say is common enough to design the models
    /// against. A tie is settled by whichever candidate was reached first, and that is an accident
    /// of traversal rather than a form anybody chose. A canonical form cannot be built on an
    /// accident: it needs exactly one least member, and the same one every run. No
    /// <see cref="double"/> can carry a total order on trees, so this is a comparison instead.
    /// </para>
    /// <para>
    /// <b>Why this is not <c>Entity.SortHash</c>.</b> That key orders the operands <i>within</i> a
    /// commutative chain, which is what <c>RewriteRules.CanonicalOrder</c> and its two siblings
    /// sort by. Choosing between whole expressions that are equal is a different question — the
    /// operands are not siblings, they are rival writings of one value — and a key built to answer
    /// the first is not thereby an answer to the second. The two do not compete: a canonical
    /// extraction under this order still wants its operands sorted by that one.
    /// </para>
    /// </remarks>
    internal static class EntityOrder
    {
        /// <summary>
        /// Smallest first, then structural. Total up to <see cref="object.Equals(object)"/> —
        /// two expressions compare equal exactly when they are the same expression, which is what
        /// makes "the least member" well defined.
        /// </summary>
        /// <remarks>
        /// <b>Size first is the half that makes it useful</b> rather than merely well defined: the
        /// representative of a class is its smallest member, so canonicalising never enlarges what
        /// it was given. The structural half is what makes it total, and it is deliberately not
        /// the printed form — <c>(x + y) + a</c> and <c>x + (y + a)</c> print identically while
        /// being different trees, so ordering on text would call two distinct expressions one.
        /// </remarks>
        internal static IComparer<Entity> Canonical { get; } = new CanonicalOrdering();

        private sealed class CanonicalOrdering : IComparer<Entity>
        {
            public int Compare(Entity? left, Entity? right)
            {
                if (ReferenceEquals(left, right)) return 0;
                if (left is null) return -1;
                if (right is null) return 1;

                var bySize = left.Complexity.CompareTo(right.Complexity);
                if (bySize != 0) return bySize;

                var leftChildren = left.DirectChildren;
                var rightChildren = right.DirectChildren;

                // A leaf against a node of the same size cannot happen -- a node is bigger than
                // any leaf -- but arity still separates two nodes the size test tied.
                var byArity = leftChildren.Count.CompareTo(rightChildren.Count);
                if (byArity != 0) return byArity;

                // A leaf is what it prints as; anything else is its node type. Comparing a leaf
                // by text is safe where comparing a node by text is not: a leaf has no structure
                // for the text to lose.
                var byName = leftChildren.Count == 0
                    ? string.CompareOrdinal(left.StringizeAscii(), right.StringizeAscii())
                    : string.CompareOrdinal(left.GetType().Name, right.GetType().Name);
                if (byName != 0) return byName;

                for (var i = 0; i < leftChildren.Count; i++)
                {
                    var byChild = Compare(leftChildren[i], rightChildren[i]);
                    if (byChild != 0) return byChild;
                }

                // Per-node data that is not a child, and the only such data the library has: two
                // expressions identical in every other way but restricted to different domains are
                // different expressions, and one of them has to come first.
                return string.CompareOrdinal(left.Codomain.ToString(), right.Codomain.ToString());
            }
        }
    }
}
