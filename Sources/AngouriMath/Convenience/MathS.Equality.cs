//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core;
using AngouriMath.Core.Budgets;
using AngouriMath.Core.Transformations;

namespace AngouriMath
{
    partial class MathS
    {
        /// <summary>
        /// Whether <paramref name="left"/> and <paramref name="right"/> are equal: proved by the
        /// rewrite rules over one e-graph, refuted at a point, or neither within
        /// <paramref name="budget"/>.
        /// </summary>
        /// <param name="left">One side.</param>
        /// <param name="right">The other.</param>
        /// <param name="budget">
        /// What the question may spend. <see cref="WorkBudget.Steps"/> counts work -- a point
        /// tried, a rule's match attempt, a node the graph grows by -- so the same question under
        /// the same steps gets the same answer on any machine; a <see cref="WorkBudget.Time"/>
        /// makes it depend on the machine, and <see cref="EqualityAnswer.Budget"/> says when it
        /// did. <see cref="Settings.Budget"/>, where set, caps this: the smaller limit on each axis
        /// applies, so an ambient <see cref="WorkBudget.Unlimited"/> leaves it as passed.
        /// </param>
        /// <param name="weakest">
        /// The weakest soundness a proof may rest on. <see cref="Soundness.Sound"/>, the default,
        /// admits only rules that hold with no side condition -- reordering the terms of a sum or
        /// the factors of a product among them -- and a proof that needs more is
        /// <see cref="EqualityVerdict.Unknown"/>. <see cref="Soundness.SoundUnderAssumptions"/>
        /// admits the conditional rules and the canonical-form pass as well, and names those it
        /// used in <see cref="EqualityAnswer.Assumptions"/>.
        /// </param>
        /// <param name="domain">
        /// <see cref="Domain.Complex"/>, the default, or <see cref="Domain.Real"/>: where the
        /// variables range, and so where a witness may be looked for. A name is a number here, so
        /// <c>a * b</c> and <c>b * a</c> are equal; a question with a matrix or a matrix operation
        /// anywhere in it has no product reordered, since a name in it may stand for a matrix.
        /// </param>
        /// <returns>
        /// <see cref="EqualityVerdict.Equal"/> only with a proof;
        /// <see cref="EqualityVerdict.NotEqual"/> only with a point at which both sides are
        /// defined and enclosures of their values are disjoint;
        /// <see cref="EqualityVerdict.Unknown"/> otherwise -- which is not "unequal".
        /// </returns>
        /// <remarks>
        /// <para>
        /// Not <c>(left - right).Simplify() == 0</c>. A simplification picks one representative
        /// per input by cost, and two equal inputs can settle on different ones; a graph holding
        /// both merges their classes whether or not an extraction would agree. And a
        /// simplification has no third answer: a difference that does not simplify to zero is
        /// read as "unequal" by whoever compares it.
        /// </para>
        /// <para>
        /// The rules are the safe ceiling's, which do not enlarge; zero-equivalence is
        /// undecidable over this language, so <see cref="EqualityVerdict.Unknown"/> is an
        /// answer this gives often and honestly.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1251">#1251</a>
        /// </para>
        /// </remarks>
        public static EqualityAnswer AreEqual(Entity left, Entity right, WorkBudget budget,
            Soundness weakest = Soundness.Sound, Domain domain = Domain.Complex)
            => EqualityOracle.Ask(left, right, budget, weakest, domain);
    }
}
