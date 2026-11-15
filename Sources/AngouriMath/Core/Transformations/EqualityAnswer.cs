//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core.Budgets;

namespace AngouriMath.Core.Transformations
{
    /// <summary>What <see cref="MathS.AreEqual"/> could establish about two expressions.</summary>
    public enum EqualityVerdict
    {
        /// <summary>
        /// Neither shown: no proof within the budget and no point at which the two differ. Says
        /// nothing about whether they are equal.
        /// </summary>
        Unknown,

        /// <summary>Proved equal by rewrite rules; see <see cref="EqualityAnswer.Justification"/>.</summary>
        Equal,

        /// <summary>
        /// Shown to differ at <see cref="EqualityAnswer.Witness"/>, where both are defined and
        /// enclosures of their values are disjoint. Never a failure to prove equality.
        /// </summary>
        NotEqual,
    }

    /// <summary>
    /// The answer of <see cref="MathS.AreEqual"/>: a verdict, what it rests on, and what it cost.
    /// </summary>
    /// <remarks>
    /// <b>What <see cref="EqualityVerdict.Equal"/> claims.</b> At every assignment of the free
    /// variables in the domain asked at which both sides have a value, the values agree, provided
    /// that each rewrite the proof used holds as its <see cref="Soundness"/> declares. A
    /// <see cref="Soundness.Sound"/> justification rests on nothing else. A
    /// <see cref="Soundness.SoundUnderAssumptions"/> one rests in addition on the side
    /// conditions of the steps named in <see cref="Assumptions"/>, which are not data the library
    /// can check: a caller who needs an unconditional answer asks for
    /// <see cref="Soundness.Sound"/> and gets <see cref="EqualityVerdict.Unknown"/> instead.
    /// </remarks>
    public sealed record EqualityAnswer
    {
        internal EqualityAnswer(EqualityVerdict verdict, Soundness? justification,
            IReadOnlyList<string> assumptions, IReadOnlyDictionary<Entity.Variable, Entity>? witness,
            BudgetOutcome budget)
        {
            Verdict = verdict;
            Justification = justification;
            Assumptions = assumptions;
            Witness = witness;
            Budget = budget;
        }

        /// <summary>Equal, not equal, or not shown either way.</summary>
        public EqualityVerdict Verdict { get; }

        /// <summary>
        /// For <see cref="EqualityVerdict.Equal"/>, the weakest soundness among the steps the
        /// proof may rest on. <see langword="null"/> for the other verdicts.
        /// </summary>
        public Soundness? Justification { get; }

        /// <summary>
        /// The conditional steps — rules declaring <see cref="Soundness.SoundUnderAssumptions"/>,
        /// and the canonical-form pass — that joined two classes while the proof was sought: a
        /// superset of those it needs. Empty for a <see cref="Soundness.Sound"/> proof.
        /// </summary>
        public IReadOnlyList<string> Assumptions { get; }

        /// <summary>
        /// For <see cref="EqualityVerdict.NotEqual"/>, the point at which the two sides differ:
        /// a value for each free variable. Empty where neither side has one.
        /// </summary>
        public IReadOnlyDictionary<Entity.Variable, Entity>? Witness { get; }

        /// <summary>What the question spent, and which ceiling, if any, stopped it.</summary>
        public BudgetOutcome Budget { get; }

        /// <inheritdoc/>
        public override string ToString()
            => Verdict switch
            {
                EqualityVerdict.Equal when Assumptions.Count == 0 => $"Equal ({Justification})",
                EqualityVerdict.Equal => $"Equal ({Justification}: {string.Join(", ", Assumptions)})",
                EqualityVerdict.NotEqual => "NotEqual at "
                    + (Witness!.Count == 0 ? "the constants" : string.Join(", ", Witness.Select(p => $"{p.Key} = {p.Value}"))),
                _ => $"Unknown ({Budget})",
            };
    }
}
