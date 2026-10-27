//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core.Transformations;
using AngouriMath.Core.Transformations.Matching;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// Whether a step renders as a sentence.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2's last
    /// piece of required infrastructure is "transformation metadata rich enough that v5.0 can
    /// render a step as a sentence" — and the only way to check that a description is rich enough
    /// to render from is to render from it.
    /// </summary>
    /// <remarks>
    /// Every word of a sentence comes off a rule: the clause is <see cref="RewriteRule.Name"/> with
    /// its hyphens replaced, and the identity in brackets is <see cref="RewriteRule.Description"/>.
    /// So these tests are about the rules as much as about the rendering — a rule that cannot be
    /// said is a rule that is missing something, and this is where that shows.
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class StepAsASentenceTest
    {
        private static readonly string[] Corpus =
        {
            "tan(x) * cos(x)", "x ^ (-1) / (y / z)", "a / (b / c)", "sin(x) / cos(x)",
            "(x + 1) ^ 2 - 1", "1/2 * x + 1/2 * x", "sin(x) ^ 2 + cos(x) ^ 2", "x / x",
            "(a + b) ^ 2", "ln(e ^ x)", "x! / (x - 1)!", "A /\\ (B \\/ C)",
        };

        private static IEnumerable<DerivationPath> Paths()
        {
            foreach (var source in Corpus)
                if (DerivationPath.OfSimplifying(source.ToEntity()) is { } path)
                    yield return path;
        }

        /// <summary>
        /// <b>A rule written as data is named in words; a rule read off a <c>switch</c> is named by
        /// its own pattern.</b> The two are told apart exactly rather than guessed at.
        /// </summary>
        /// <remarks>
        /// This is what stops the rendering quoting a matcher at the reader. The first version of
        /// it rendered every name as a clause and produced <i>"Divf(Sinf(var any1), Cosf(var
        /// any1a)) when any1 == any1a, so sin(x) / cos(x) becomes tan(x)"</i>.
        /// </remarks>
        [Fact]
        public void EveryRuleWrittenAsDataIsNamedInProse()
        {
            var names = MatchedRules.All.SelectMany(set => set.Rules).Select(rule => rule.Name)
                .Distinct(StringComparer.Ordinal).ToList();
            Assert.Equal(312, names.Count);
            Assert.All(names, name => Assert.True(Explanation.IsProse(name),
                $"'{name}' is a rule name that does not read as a phrase in English"));

            // And no set is described by `RuleRegistryGenerator` any more. A set whose rules
            // carry no tier of their own was one still described by the generator, and every name
            // it gave was a rendered pattern rather than prose; this used to assert that such
            // sets existed and that their names did not pass for English. The last three --
            // the CanonicalOrder family -- were repointed at `MatchedRules.Sort`, so the
            // population is empty, and that is asserted rather than the assertion being deleted:
            // a set falling back to the generator would be a regression this should catch.
            var generated = RewriteRules.All
                .Where(set => set.Rules.Count > 0 && set.Rules.All(rule => rule.Soundness is null))
                .Select(set => set.Name)
                .ToList();
            Assert.True(generated.Count == 0,
                "these sets are described by the generator again, and their rule names are "
                + "rendered patterns rather than prose: " + string.Join(", ", generated));
        }

        /// <summary>
        /// Every step of every derivation says something, and none of them says nothing.
        /// </summary>
        /// <remarks>
        /// The <c>X becomes X</c> check is the one that earns its place. Two different trees can
        /// print identically — <c>1 + (x + y)</c> and <c>(1 + x) + y</c> — so a normalisation that
        /// regroups a chain has a real before and after and nothing to show for it, and it happened
        /// on two of the first six derivations this was tried on.
        /// <see cref="Explanation.Transition"/> says so instead.
        /// </remarks>
        [Fact]
        public void EveryStepRendersAsASentenceThatSaysSomething()
        {
            var rendered = 0;
            foreach (var path in Paths())
                foreach (var step in path.Steps)
                {
                    var sentence = step.Explain();
                    rendered++;
                    Assert.NotEmpty(sentence);
                    Assert.EndsWith(".", sentence, StringComparison.Ordinal);
                    Assert.True(char.IsUpper(sentence[0]) || char.IsLetterOrDigit(sentence[0])
                                || sentence[0] is '(' or '-' or '{',
                        $"a sentence should not open with '{sentence[0]}': {sentence}");

                    var written = step.Before.Stringize();
                    if (written == step.After.Stringize())
                        Assert.Contains("prints the same way", sentence, StringComparison.Ordinal);
                    else
                        Assert.DoesNotContain($"{written} becomes {written}.", sentence,
                            StringComparison.Ordinal);
                }
            // A corpus that stopped reaching the simplifier would otherwise make this a test that
            // passes by asking nothing.
            Assert.True(rendered > 20, $"only {rendered} steps were rendered");
        }

        /// <summary>
        /// The whole derivation reads as prose, and the paragraph that closes it is counted from
        /// the steps rather than written out.
        /// </summary>
        [Fact]
        public void ADerivationExplainsItselfAndItsClosingNoteIsCounted()
        {
            foreach (var path in Paths())
            {
                var prose = path.Explain();
                Assert.StartsWith(path.Input.Stringize(), prose, StringComparison.Ordinal);
                Assert.Contains(path.Result.Stringize(), prose, StringComparison.Ordinal);
                for (var i = 0; i < path.Steps.Count; i++)
                    Assert.Contains($"{i + 1}. ", prose, StringComparison.Ordinal);

                var tiered = path.Steps.Count(step => step.Soundness is not null);
                if (tiered > 0 && tiered < path.Steps.Count)
                    Assert.Contains($"of the {path.Steps.Count} steps", prose, StringComparison.Ordinal);
                if (path.ExpressionsExplored > path.Steps.Count)
                    Assert.Contains($"reached {path.ExpressionsExplored} expressions", prose,
                        StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// <b>The one worked example, because a feature like this is judged by reading it.</b>
        /// </summary>
        /// <remarks>
        /// <c>tan(x) * cos(x)</c> is the case worth pinning: four steps, three of them naming an
        /// identity, and an answer that carries the condition the last rule attached rather than
        /// asserting an equality that is false at a pole. If this stops reading as an explanation,
        /// the feature has stopped working whatever else still passes.
        /// </remarks>
        [Fact]
        public void TheWorkedExample()
        {
            var path = DerivationPath.OfSimplifying("tan(x) * cos(x)".ToEntity());
            Assert.NotNull(path);
            var prose = path!.Explain();

            Assert.Contains("tan(x) * cos(x) becomes sin(x) provided not cos(x) = 0", prose,
                StringComparison.Ordinal);
            Assert.Contains("Tangent is sine over cosine (tan(a) = sin(a) / cos(a)), so", prose,
                StringComparison.Ordinal);
            Assert.Contains("A quotient of symbolic parts is grouped pairwise", prose,
                StringComparison.Ordinal);
            Assert.Contains("hold under assumptions rather than universally", prose,
                StringComparison.Ordinal);
        }

        /// <summary>
        /// A rewrite reports <b>its own</b> tier where it has one, not its set's.
        /// </summary>
        /// <remarks>
        /// A set's tier is the minimum over its rules, so reading it as every rewrite's tier
        /// understates most of them: all thirty sets declare
        /// <see cref="Soundness.SoundUnderAssumptions"/> while most rules written as data are
        /// <see cref="Soundness.Sound"/> — <c>RuleAuthoringGuideTest</c> holds the live counts, so
        /// that no number here can go stale. This is the step grain
        /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 5
        /// recorded as missing, and this test is it not being missing any more.
        /// </remarks>
        [Fact]
        public void ARewriteReportsItsOwnTierWhereItHasOne()
        {
            using var recording = RewriteRecording.Start();
            RewriteRules.SetOperator.ApplyOnce(@"A /\ A".ToEntity());
            recording.Dispose();

            var step = Assert.Single(recording.Steps);
            Assert.NotNull(step.Rule);
            Assert.Equal(Soundness.Sound, step.Rule!.Soundness);
            Assert.Equal(Soundness.Sound, step.Soundness);
            // While the set it came from still reports the weakest of all its rules.
            Assert.Equal(Soundness.SoundUnderAssumptions, step.RuleSet.Soundness);
        }
    }
}
