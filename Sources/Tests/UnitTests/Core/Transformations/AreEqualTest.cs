//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core;
using AngouriMath.Core.Budgets;
using AngouriMath.Core.Transformations;
using AngouriMath.Extensions;
using AngouriMath.Numerics;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// <see cref="MathS.AreEqual"/>: the e-graph asked whether two expressions are equal.
    /// https://github.com/asc-community/AngouriMath/issues/1251
    /// </summary>
    [Trait("Area", "Core")]
    public sealed class AreEqualTest
    {
        /// <summary>Steps only: an answer must not depend on the machine running the test.</summary>
        private static readonly WorkBudget Budget = new() { Steps = 20_000 };

        private static EqualityAnswer Ask(string left, string right,
            Soundness weakest = Soundness.Sound, Domain domain = Domain.Complex)
            => MathS.AreEqual(left.ToEntity(), right.ToEntity(), Budget, weakest, domain);

        [Theory]
        [InlineData("x + 0", "x")]
        [InlineData("x * 1", "x")]
        [InlineData("x - x", "0")]
        [InlineData("2 * x + 3 * x", "5 * x")]
        [InlineData("(x + y) * (x - y)", "x ^ 2 - y ^ 2")]
        [InlineData("sin(x) ^ 2 + cos(x) ^ 2", "1")]
        [InlineData("a * b + a * c", "a * (b + c)")]
        [InlineData("2 ^ (1/2)", "sqrt(2)")]
        public void AnIdentityTheSoundRulesReachIsEqualUnconditionally(string left, string right)
        {
            var answer = Ask(left, right);
            Assert.Equal(EqualityVerdict.Equal, answer.Verdict);
            Assert.Equal(Soundness.Sound, answer.Justification);
            Assert.Empty(answer.Assumptions);
            Assert.Null(answer.Witness);
        }

        /// <summary>
        /// An identity whose proof needs a step that declares
        /// <see cref="Soundness.SoundUnderAssumptions"/> is <see cref="EqualityVerdict.Unknown"/>
        /// to a caller who asked for an unconditional proof, and <see cref="EqualityVerdict.Equal"/>
        /// naming the steps to a caller who admits them. Commutativity is here because the graph
        /// does not sort operands and the pass that does declares the weaker soundness.
        /// </summary>
        [Theory]
        [InlineData("x + y", "y + x", "canonicalization")]
        [InlineData("x * y", "y * x", "canonicalization")]
        [InlineData("tan(x)", "sin(x) / cos(x)", "sine-over-cosine-is-the-tangent")]
        [InlineData("exp(x + y)", "exp(x) * exp(y)", "two-powers-of-one-base-multiply-by-adding-exponents")]
        [InlineData("x ^ 2 * x ^ 3", "x ^ 5", "two-powers-of-one-base-multiply-by-adding-exponents")]
        [InlineData("2 * sqrt(2) * sqrt(3)", "2 * sqrt(6)", "a-numeric-factor-floats-out-of-a-product-of-functions")]
        public void AConditionalProofIsEqualOnlyWhereItsConditionIsAdmitted(string left, string right, string step)
        {
            Assert.Equal(EqualityVerdict.Unknown, Ask(left, right).Verdict);

            var admitted = Ask(left, right, Soundness.SoundUnderAssumptions);
            Assert.Equal(EqualityVerdict.Equal, admitted.Verdict);
            Assert.Equal(Soundness.SoundUnderAssumptions, admitted.Justification);
            Assert.Contains(admitted.Assumptions, name => name.StartsWith(step, StringComparison.Ordinal));
        }

        /// <summary>
        /// Admitting conditional rules does not weaken a proof the sound rules make alone: the
        /// justification is the weakest the answer needs, not the weakest that was admitted.
        /// </summary>
        [Fact]
        public void ASoundProofStaysSoundWhenConditionalRulesAreAdmitted()
        {
            var answer = Ask("(x + y) * (x - y)", "x ^ 2 - y ^ 2", Soundness.SoundUnderAssumptions);
            Assert.Equal(EqualityVerdict.Equal, answer.Verdict);
            Assert.Equal(Soundness.Sound, answer.Justification);
            Assert.Empty(answer.Assumptions);
        }

        [Theory]
        [InlineData("x + 1", "x")]
        [InlineData("x", "y")]
        [InlineData("sqrt(x ^ 2)", "x")]
        [InlineData("(x ^ 2) ^ (1/2)", "x")]
        [InlineData("arcsin(sin(x))", "x")]
        [InlineData("pi", "3.14159")]
        // False off the real line, and each a rule some system ships unguarded.
        [InlineData("sqrt(x ^ 2)", "abs(x)")]
        [InlineData("ln(x * y)", "ln(x) + ln(y)")]
        [InlineData("sqrt(x) * sqrt(y)", "sqrt(x * y)")]
        [InlineData("ln(e ^ x)", "x")]
        public void ANonIdentityIsNotEqualWithAWitnessThatChecks(string left, string right)
        {
            foreach (var weakest in new[] { Soundness.Sound, Soundness.SoundUnderAssumptions })
            {
                var answer = Ask(left, right, weakest);
                Assert.Equal(EqualityVerdict.NotEqual, answer.Verdict);
                Assert.NotNull(answer.Witness);
                AssertDiffersAt(left.ToEntity(), right.ToEntity(), answer.Witness!);
            }
        }

        /// <summary>
        /// The domain asked decides where a witness may be: these hold over the reals and fail
        /// off them, so over the reals the answer is <see cref="EqualityVerdict.Unknown"/> --
        /// not proved, and not refuted with a point the caller did not ask about.
        /// </summary>
        [Theory]
        [InlineData("sqrt(x ^ 2)", "abs(x)")]
        [InlineData("ln(e ^ x)", "x")]
        public void AWitnessIsLookedForOnlyWhereTheVariablesRange(string left, string right)
        {
            Assert.Equal(EqualityVerdict.NotEqual, Ask(left, right).Verdict);
            Assert.Equal(EqualityVerdict.Unknown, Ask(left, right, domain: Domain.Real).Verdict);
        }

        /// <summary>
        /// Identities the safe rules do not reach. Each is true wherever both sides are defined
        /// (and <c>e ^ ln(x)</c> everywhere they are), so <see cref="EqualityVerdict.NotEqual"/>
        /// would be wrong; <see cref="EqualityVerdict.Unknown"/> is the honest answer, and one of
        /// these turning <see cref="EqualityVerdict.Equal"/> is an improvement to record.
        /// </summary>
        [Theory]
        [InlineData("(x + 1) ^ 2", "x ^ 2 + 2 * x + 1")]
        [InlineData("1 / x + 1 / y", "(x + y) / (x * y)")]
        [InlineData("sin(2 * x)", "2 * sin(x) * cos(x)")]
        [InlineData("e ^ ln(x)", "x")]
        [InlineData("x / x", "1")]
        public void AnIdentityBeyondTheRulesIsUnknown(string left, string right)
        {
            foreach (var weakest in new[] { Soundness.Sound, Soundness.SoundUnderAssumptions })
                foreach (var domain in new[] { Domain.Complex, Domain.Real })
                {
                    var answer = Ask(left, right, weakest, domain);
                    Assert.Equal(EqualityVerdict.Unknown, answer.Verdict);
                    Assert.Null(answer.Justification);
                    Assert.Null(answer.Witness);
                }
        }

        /// <summary>
        /// Under no steps nothing is tried, and the answer says the ceiling stopped it rather
        /// than reading as a negative.
        /// </summary>
        [Fact]
        public void UnderNoBudgetTheAnswerIsUnknownAndSaysWhy()
        {
            var answer = MathS.AreEqual("x + y".ToEntity(), "y + x".ToEntity(), new WorkBudget { Steps = 0 },
                Soundness.SoundUnderAssumptions);
            Assert.Equal(EqualityVerdict.Unknown, answer.Verdict);
            Assert.False(answer.Budget.Completed);
            Assert.True(answer.Budget.IsDeterministic);
        }

        /// <summary>The budget is work, so the same question costs the same steps every time.</summary>
        [Theory]
        [InlineData("(x + y) * (x - y)", "x ^ 2 - y ^ 2")]
        [InlineData("(x + 1) ^ 2", "x ^ 2 + 2 * x + 1")]
        public void TheSameQuestionCostsTheSameSteps(string left, string right)
        {
            var first = Ask(left, right, Soundness.SoundUnderAssumptions);
            var second = Ask(left, right, Soundness.SoundUnderAssumptions);
            Assert.Equal(first.Budget.Steps, second.Budget.Steps);
            Assert.Equal(first.Verdict, second.Verdict);

            // And a ceiling below what it spent stops it at the ceiling, not after it.
            var starved = MathS.AreEqual(left.ToEntity(), right.ToEntity(),
                new WorkBudget { Steps = first.Budget.Steps / 2 }, Soundness.SoundUnderAssumptions);
            Assert.False(starved.Budget.Completed);
            Assert.True(starved.Budget.Steps <= first.Budget.Steps / 2 + 1);
        }

        [Fact]
        public void AHeuristicCannotJustifyAnEquality()
            => Assert.Throws<ArgumentOutOfRangeException>(
                () => MathS.AreEqual("x".ToEntity(), "x".ToEntity(), Budget, Soundness.Heuristic));

        /// <summary>
        /// None of the answers above is wrong, checked against evaluation at points the oracle
        /// does not sample: positive, negative, zero and off the real line. An
        /// <see cref="EqualityVerdict.Equal"/> must agree wherever both sides are defined; a
        /// <see cref="EqualityVerdict.NotEqual"/> is checked at its own witness by
        /// <see cref="AssertDiffersAt"/>.
        /// </summary>
        [Theory]
        [InlineData("x + 0", "x")]
        [InlineData("x - x", "0")]
        [InlineData("2 * x + 3 * x", "5 * x")]
        [InlineData("(x + y) * (x - y)", "x ^ 2 - y ^ 2")]
        [InlineData("sin(x) ^ 2 + cos(x) ^ 2", "1")]
        [InlineData("a * b + a * c", "a * (b + c)")]
        [InlineData("x + y", "y + x")]
        [InlineData("tan(x)", "sin(x) / cos(x)")]
        [InlineData("exp(x + y)", "exp(x) * exp(y)")]
        [InlineData("x ^ 2 * x ^ 3", "x ^ 5")]
        public void AnEqualAnswerAgreesWithEvaluation(string left, string right)
        {
            var l = left.ToEntity();
            var r = right.ToEntity();
            Assert.Equal(EqualityVerdict.Equal, Ask(left, right, Soundness.SoundUnderAssumptions).Verdict);

            var values = new Entity[]
            {
                Rational.Create(5, 7), Rational.Create(-9, 4), 0, 3,
                Complex.Create(Rational.Create(3, 2), Rational.Create(2, 5)),
                Complex.Create(Rational.Create(-1, 3), Rational.Create(-7, 4)),
                Complex.Create(Rational.Create(1, 9), Rational.Create(11, 2)),
            };
            var variables = l.Vars.Concat(r.Vars).Distinct().ToList();
            var compared = 0;
            for (var i = 0; i < values.Length; i++)
            {
                var point = new Dictionary<Variable, Entity>();
                for (var j = 0; j < variables.Count; j++)
                    point[variables[j]] = values[(i + 3 * j) % values.Length];
                if (IntervalEvaluation.Of(l.Substitute(point)) is not { } lv
                    || IntervalEvaluation.Of(r.Substitute(point)) is not { } rv)
                    continue;
                compared++;
                Assert.NotEqual(false, IntervalEvaluation.Agree(lv, rv, 1e-12));
            }
            Assert.True(compared >= 4, $"only {compared} points had a value on both sides");
        }

        /// <summary>
        /// A witness checked by a route the oracle does not take: the library's own numeric
        /// evaluation of each side, which must be finite and differ by more than rounding.
        /// </summary>
        private static void AssertDiffersAt(Entity left, Entity right, IReadOnlyDictionary<Variable, Entity> witness)
        {
            var l = left.Substitute(witness).EvalNumerical();
            var r = right.Substitute(witness).EvalNumerical();
            Assert.True(l.IsFinite && r.IsFinite, $"{left} = {l}, {right} = {r} at the witness");
            var (ln, rn) = (l.ToNumerics(), r.ToNumerics());
            var difference = System.Numerics.Complex.Abs(ln - rn);
            Assert.True(difference > 1e-9 * Math.Max(1, Math.Max(ln.Magnitude, rn.Magnitude)),
                $"{left} = {l} and {right} = {r} do not differ");
        }
    }
}
