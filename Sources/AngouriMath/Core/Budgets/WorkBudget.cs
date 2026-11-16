//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;

namespace AngouriMath.Core.Budgets
{
    /// <summary>
    /// What a computation is allowed to spend before it declines. A value, not a counter:
    /// two budgets with the same ceilings are the same budget, and one can be reused,
    /// shared and cached freely.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two axes, and they are not the same kind of thing.</b> <see cref="Steps"/> counts
    /// work the algorithm does and so gives the same answer on every machine;
    /// <see cref="Time"/> reads a clock and so does not. Both are here because they catch
    /// different runaways — a computation can be spending a colossal number of cheap steps
    /// or a handful of ruinous ones — but only the first can appear in an answer that is
    /// supposed to be reproducible, which is why
    /// <see cref="BudgetOutcome.IsDeterministic"/> exists and why exhaustion says which
    /// ceiling fired rather than only that one did
    /// (<a href="https://github.com/asc-community/AngouriMath/issues/373">#373</a>).
    /// </para>
    /// <para>
    /// <b>A step is one unit of whatever the algorithm charges for.</b> It is deliberately
    /// not defined further: an algorithm charges where it would otherwise be able to loop,
    /// and comparing step counts between two algorithms means nothing. What a step does
    /// guarantee is that the count is a function of the input, so the same input exhausts at
    /// the same point twice.
    /// </para>
    /// <para>
    /// <b>What is not counted, and is not pretended to be.</b> Nodes allocated and memory
    /// held are both named as budget axes by
    /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> and neither
    /// is counted anywhere in the library, so neither is a property here. A ceiling nothing
    /// enforces reads as a promise, and the caller finds out it was not one by waiting.
    /// Algorithm-specific ceilings — how many S-polynomial pairs, how wide a coefficient may
    /// get — stay with the algorithm that knows what they mean; they are reported through
    /// the same <see cref="BudgetOutcome"/> by name.
    /// </para>
    /// </remarks>
    public sealed record WorkBudget
    {
        /// <summary>
        /// How many units of work may be charged before the computation declines.
        /// <see cref="long.MaxValue"/>, the default, is no ceiling.
        /// </summary>
        public long Steps { get; init; } = long.MaxValue;

        /// <summary>
        /// A wall-clock backstop, or <see langword="null"/> for none. An answer decided by
        /// this depends on the machine and on what else is running on it, and says so
        /// through <see cref="BudgetOutcome.IsDeterministic"/>.
        /// </summary>
        public TimeSpan? Time { get; init; }

        /// <summary>
        /// No ceiling on either axis. This is what <see cref="MathS.Settings.Budget"/>
        /// reads as its default — but see that property: leaving it alone means each
        /// algorithm keeps its own default, and setting it to this lifts only the working
        /// allowances, never a lid.
        /// </summary>
        public static WorkBudget Unlimited { get; } = new();
    }
}
