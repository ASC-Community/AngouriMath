//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Core.Budgets;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using AngouriMath.Functions;
using AngouriMath.Functions.Algebra;
using Xunit;

namespace AngouriMath.Tests.Core.Budgets
{
    /// <summary>
    /// What a set <see cref="MathS.Settings.Budget"/> does to each kind of budget: it replaces
    /// an allowance, and it can only tighten a lid or a budget passed as an argument.
    /// https://github.com/asc-community/AngouriMath/issues/2454
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class BudgetRoleTest
    {
        private static readonly WorkBudget Lid
            = new() { Steps = 2_000, Time = TimeSpan.FromMilliseconds(50) };

        [Fact]
        public void UnsetLeavesEveryRoleOnItsOwn()
        {
            Assert.False(MathS.Settings.Budget.IsOverriden);
            foreach (var role in new[] { BudgetRole.Lid, BudgetRole.Allowance, BudgetRole.Argument })
                Assert.Equal(Lid, BudgetLedger.Limits(Lid, role));
        }

        [Fact]
        public void UnlimitedDoesNotLiftALid()
        {
            using var _ = MathS.Settings.Budget.Set(WorkBudget.Unlimited);
            Assert.Equal(Lid, BudgetLedger.Limits(Lid, BudgetRole.Lid));
        }

        [Fact]
        public void ALidTakesTheSmallerLimitOnEachAxis()
        {
            using (MathS.Settings.Budget.Set(new WorkBudget { Steps = 1 }))
                Assert.Equal(Lid with { Steps = 1 }, BudgetLedger.Limits(Lid, BudgetRole.Lid));
            using (MathS.Settings.Budget.Set(new WorkBudget { Steps = 1_000_000 }))
                Assert.Equal(Lid, BudgetLedger.Limits(Lid, BudgetRole.Lid));
            using (MathS.Settings.Budget.Set(new WorkBudget { Time = TimeSpan.FromMilliseconds(1) }))
                Assert.Equal(Lid with { Time = TimeSpan.FromMilliseconds(1) }, BudgetLedger.Limits(Lid, BudgetRole.Lid));
        }

        /// <summary>
        /// What the solver's refusal tells a caller to do -- "raise MathS.Settings.Budget to allow
        /// more" -- has to work, so an allowance is replaced, not capped.
        /// </summary>
        [Fact]
        public void ARaisedBudgetRaisesAnAllowance()
        {
            var raised = new WorkBudget { Steps = 100_000 };
            using var _ = MathS.Settings.Budget.Set(raised);
            Assert.Equal(raised, BudgetLedger.Limits(EquationSolver.SystemSolveBudget, BudgetRole.Allowance));
        }

        [Fact]
        public void AnArgumentIsCappedByTheSetBudget()
        {
            var argument = new WorkBudget { Steps = 1_000, Time = TimeSpan.FromSeconds(1) };
            using (MathS.Settings.Budget.Set(WorkBudget.Unlimited))
                Assert.Equal(argument, BudgetLedger.Limits(argument, BudgetRole.Argument));
            using (MathS.Settings.Budget.Set(new WorkBudget { Steps = 5 }))
                Assert.Equal(argument with { Steps = 5 }, BudgetLedger.Limits(argument, BudgetRole.Argument));
        }

        /// <summary>
        /// The perfect-square proof under an unlimited setting keeps the lid its caller gave it.
        /// It used to run to saturation; ten steps do not reach the proof it would find.
        /// </summary>
        [Fact]
        public void TheProofKeepsItsLidUnderUnlimited()
        {
            using var _ = MathS.Settings.Budget.Set(WorkBudget.Unlimited);
            using var recording = BudgetRecording.Start();
            var proved = Saturation.ProvesEqual("2 * sqrt(2) * sqrt(3)".ToEntity(), "2 * sqrt(6)".ToEntity(),
                Saturation.SafeRules, new WorkBudget { Steps = 10, Time = TimeSpan.FromSeconds(30) });
            Assert.False(proved);
            Assert.Equal("steps", Assert.Single(recording.Outcomes).Reason);
        }

        /// <summary>
        /// A tight setting tightens the proof, and the collapse still answers: an unproved cross
        /// term falls back to the symbolic test, so the proof stopping early costs time, not the
        /// answer.
        /// </summary>
        [Fact]
        public void ATightSettingTightensTheProofAndTheCollapseStillAnswers()
        {
            using var _ = MathS.Settings.Budget.Set(new WorkBudget { Steps = 1 });
            using var recording = BudgetRecording.Start();
            Assert.Equal("(sqrt(3) + sqrt(2)) ^ 2".ToEntity(),
                Patterns.CollapseToPerfectSquare("2 + 2 * sqrt(2) * sqrt(3) + 3".ToEntity()));
            var proofs = recording.Outcomes.Where(outcome => outcome.Where == nameof(Saturation.ProvesEqual)).ToList();
            Assert.NotEmpty(proofs);
            Assert.All(proofs, proof => Assert.True(proof.Steps <= 2));
        }

        /// <summary>
        /// <c>AreEqual</c> with a budget of its own inside an unlimited scope spends that budget.
        /// It used to run unlimited and prove the equality.
        /// </summary>
        [Fact]
        public void AnExplicitBudgetHoldsInsideAnUnlimitedScope()
        {
            using var _ = MathS.Settings.Budget.Set(WorkBudget.Unlimited);
            var answer = MathS.AreEqual("2 * sqrt(2) * sqrt(3)", "2 * sqrt(6)", new WorkBudget { Steps = 1 });
            Assert.Equal(EqualityVerdict.Unknown, answer.Verdict);
            Assert.Equal("steps", answer.Budget.Reason);
        }

        /// <summary>The rows of BREAKING-CHANGES.md, which were run unlimited on 2.7.0.</summary>
        [Theory]
        [InlineData("x * 2 * 3 + x", "x * 2 * 3 + x", "6 * x + x")]
        [InlineData("a * b + b * a", "a * b + b * a", "a * b + a * b")]
        public void TheGraphSpendsItsArgumentInsideAnUnlimitedScope(string input, string saturated, string canonical)
        {
            var budget = new WorkBudget { Steps = 5, Time = TimeSpan.FromSeconds(30) };
            using var _ = MathS.Settings.Budget.Set(WorkBudget.Unlimited);
            Assert.Equal(saturated.ToEntity(),
                Transformation.EqualitySaturation(budget, AngouriMath.Core.CostModel.Default).ApplyOrKeep(input.ToEntity()));
            Assert.Equal(canonical.ToEntity(),
                Transformation.CanonicalizationOverGraph(budget).ApplyOrKeep(input.ToEntity()));
        }

        [Fact]
        public void ATighterScopeCapsAnExplicitBudget()
        {
            using var _ = MathS.Settings.Budget.Set(new WorkBudget { Steps = 1 });
            var answer = MathS.AreEqual("2 * sqrt(2) * sqrt(3)", "2 * sqrt(6)", new WorkBudget { Steps = 1_000_000 });
            Assert.Equal(EqualityVerdict.Unknown, answer.Verdict);
            Assert.Equal("steps", answer.Budget.Reason);
        }
    }
}
