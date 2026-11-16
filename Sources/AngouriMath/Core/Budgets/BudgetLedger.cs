//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Diagnostics;

namespace AngouriMath.Core.Budgets
{
    /// <summary>
    /// The accounting side of a <see cref="WorkBudget"/>: what has been spent so far, and
    /// what stopped the computation if anything has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Mutable, and single-flow, on purpose.</b> The limits are an immutable value so
    /// that they can be shared; the drawing-down is one object mutated in program order.
    /// The alternative — threading a remaining count through as an immutable value — has to
    /// <i>split</i> the count at every branch of a search, and there is no right split: an
    /// even one starves the branch that needed the work, giving it all to the first starves
    /// the rest, and a branch that fails then needs a protocol for handing back what it did
    /// not spend. A single ledger drawn down in program order needs none of that, is exactly
    /// reproducible as long as the algorithm's own order is defined — which
    /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>'s third
    /// design principle requires anyway — and bounds the search as a whole rather than each
    /// branch of it, which is the difference between a bound and a suggestion.
    /// </para>
    /// <para>
    /// <b>A subgoal inherits the ledger rather than getting one.</b> Passing the same object
    /// down is what makes a call bounded end to end. Giving each stage a budget of its own
    /// bounds each stage and leaves the whole unbounded, which is how
    /// <a href="https://github.com/asc-community/AngouriMath/issues/896">#896</a> arose:
    /// the same call is bounded or not depending on which internal path accepted it, and
    /// that is worse to debug than either extreme.
    /// </para>
    /// <para>
    /// <b>Honouring is cooperative, so no thread is involved.</b> The algorithm asks —
    /// <see cref="Spend"/> before doing a unit of work, <see cref="Require"/> before letting
    /// a structure grow — and declines when told to. Nothing is interrupted, nothing is
    /// aborted, and there is no second thread to make the answer depend on scheduling. The
    /// cost is that an algorithm which does not ask cannot be bounded, which is the honest
    /// trade: a bound that is enforced from outside is a thread abort, and a thread abort in
    /// the middle of a rewrite leaves no answer worth having.
    /// </para>
    /// <para>
    /// <b>The first ceiling to fire is the one reported.</b> Once one has, every subsequent
    /// question answers "no" without re-testing, so the reason cannot be overwritten by
    /// whatever the algorithm happened to ask next on its way out.
    /// </para>
    /// </remarks>
    internal sealed class BudgetLedger
    {
        [ConstantField]
        private static readonly double ticksPerTimestamp
            = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private readonly string where;
        private readonly WorkBudget limits;
        private readonly long started = Stopwatch.GetTimestamp();
        private long spent;
        private string? ceiling;
        private bool timeBound;

        private BudgetLedger(string where, WorkBudget limits)
            => (this.where, this.limits) = (where, limits);

        /// <summary>
        /// A ledger for one computation. <paramref name="ownDefault"/> is what the algorithm
        /// bounds itself by when nobody has set <see cref="MathS.Settings.Budget"/>;
        /// <paramref name="role"/> says what a set one does to it.
        /// https://github.com/asc-community/AngouriMath/issues/2454
        /// </summary>
        internal static BudgetLedger For(string where, WorkBudget ownDefault, BudgetRole role)
            => new(where, Limits(ownDefault, role));

        /// <summary>The limits a computation of <paramref name="role"/> runs under.</summary>
        internal static WorkBudget Limits(WorkBudget ownDefault, BudgetRole role)
        {
            var setting = MathS.Settings.Budget;
            if (!setting.IsOverriden)
                return ownDefault;
            var set = setting.Value;
            // A lid is tightened on both axes, not on the clock alone, although a step means
            // something different to each algorithm. The incomparability is a reason not to
            // *raise* one algorithm's ceiling by a number chosen for another, and a lid is never
            // raised. Taking only the clock would ignore a caller who set steps -- the
            // reproducible axis -- at exactly the sites where they asked for less. And it is
            // cheap: the one lid, the perfect-square proof, completes in 15 to 1,203 steps over
            // the unit suite (median 212), and a proof stopped short is "not proved", which falls
            // back to the symbolic test -- so a budget below that costs time, and no answer of
            // Factorize or Simplify was measured to move under one.
            return role switch
            {
                BudgetRole.Allowance => set,
                BudgetRole.Lid or BudgetRole.Argument => Tighter(ownDefault, set),
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
            };
        }

        /// <summary>
        /// The smaller limit on each axis: what both <paramref name="a"/> and
        /// <paramref name="b"/> allow. A missing <see cref="WorkBudget.Time"/> is no ceiling,
        /// so it yields to any clock.
        /// </summary>
        private static WorkBudget Tighter(WorkBudget a, WorkBudget b)
            => new()
            {
                Steps = Math.Min(a.Steps, b.Steps),
                Time = a.Time is { } x && b.Time is { } y ? (x < y ? x : y) : a.Time ?? b.Time,
            };

        /// <summary>
        /// Charges <paramref name="units"/> of work and answers whether the computation may
        /// carry on. Call it before the work, not after, so that the ceiling bounds what is
        /// done rather than what has been done.
        /// </summary>
        internal bool Spend(long units = 1)
        {
            if (ceiling is not null)
                return false;
            spent += units;
            if (spent > limits.Steps)
            {
                ceiling = "steps";
                return false;
            }
            if (limits.Time is { } limit && Elapsed > limit)
            {
                ceiling = "time";
                timeBound = true;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Answers whether <paramref name="reason"/>'s condition <paramref name="within"/>
        /// holds, and where it does not, records it as why this computation stopped.
        /// </summary>
        internal bool Require(bool within, string reason)
        {
            if (ceiling is not null)
                return false;
            if (within)
                return true;
            ceiling = reason;
            return false;
        }

        /// <summary>Whether something has already stopped this computation.</summary>
        internal bool Exhausted => ceiling is not null;

        private TimeSpan Elapsed
            => new((long)((Stopwatch.GetTimestamp() - started) * ticksPerTimestamp));

        /// <summary>What this ledger has to say for itself.</summary>
        internal BudgetOutcome Outcome() => new(where, ceiling, spent, Elapsed, !timeBound);

        /// <summary>
        /// Hands the outcome to whoever is recording, if anyone is. Costs one ambient read
        /// per bounded computation — not per step — when nobody is.
        /// </summary>
        internal void Report() => BudgetRecording.Report(this);
    }
}
