//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Linq;
using AngouriMath.Core.Transformations;
using AngouriMath.Core.Transformations.Matching;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// What a rule says about itself, and whether the registry is able to repeat it.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2 asks for
    /// rules that are data carrying "identity, name, direction, applicability conditions,
    /// justification tier, provenance, cost effect", and for metadata "rich enough that v5.0 can
    /// render a step as a sentence".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The registry used to run one thing and describe another.</b> When this was written,
    /// 27 of the 30 sets executed <see cref="MatchedRuleSet.ApplyHere"/> while 29 took their
    /// <see cref="RewriteRuleSet.Rules"/> from <c>RuleRegistryGenerator</c> reading a
    /// <c>switch</c> they no longer ran — <a href="https://github.com/asc-community/AngouriMath/issues/825">#825</a>'s
    /// open half. Both numbers are now 30 and 0: every registration passes its
    /// <see cref="MatchedRuleSet"/> to the constructor that takes one, which runs it and
    /// describes it from the same object. The last three to move were the <c>CanonicalOrder</c>
    /// family, and the counts below moved with them.
    /// </para>
    /// <para>
    /// This file is about the three things that had to be true of
    /// <see cref="MatchedRuleSet.AsAddressable"/> before it could be what the registry reads:
    /// growth that is the rule's own answer rather than a proxy for it, a justification tier per
    /// rule, and somewhere for the identity to live.
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class RuleMetadataTest
    {
        /// <summary>
        /// <b>Growth is the rule's exact answer, not a guess from how long the rendering is.</b>
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>AsAddressable</c> used to compare the lengths of the two rendered pattern strings,
        /// which is what <c>RuleRegistryGenerator</c> has to do — it reads C# source text and has
        /// no tree to count nodes on. Here there is a tree, and the proxy disagreed with it
        /// <b>23 times in 323, in both directions</b>:
        /// </para>
        /// <list type="bullet">
        /// <item>thirteen <c>Boolean</c> rules that <i>declare</i> <c>Collects</c> on a code-built
        /// replacement read as <c>Unknown</c>, because with no string to measure the proxy threw
        /// the author's declaration away;</item>
        /// <item>two <c>CollapseTrigonometricFunctions</c> rules read as <c>Expands</c> because
        /// <c>Cosecantf</c> is a longer word than <c>Sinf</c> — the same number of nodes;</item>
        /// <item>two <c>DivisionPreparing</c> rules read as <c>Collects</c> for that reason
        /// reversed.</item>
        /// </list>
        /// </remarks>
        [Fact]
        public void AddressableGrowthIsTheRulesOwnAnswer()
        {
            foreach (var set in MatchedRules.All)
            {
                var addressable = set.AsAddressable();
                Assert.Equal(set.Rules.Count, addressable.Count);
                for (var i = 0; i < set.Rules.Count; i++)
                    Assert.Equal(set.Rules[i].Growth, addressable[i].Growth);
            }
        }

        /// <summary>
        /// <b>A set's tier is the minimum over its rules, so reading it as every rule's tier
        /// understates most of them.</b>
        /// </summary>
        /// <remarks>
        /// All thirty registry sets declare <see cref="Soundness.SoundUnderAssumptions"/>, and one
        /// conditional rule is enough to make that true of a set of a hundred. Asked per rule
        /// instead, <b>181 of the 323 rules written as data are <see cref="Soundness.Sound"/></b> —
        /// they hold for every complex argument, with nothing assumed. The counts are asserted
        /// rather than the ratio, because the interesting failure is a rule quietly changing tier.
        /// </remarks>
        [Fact]
        public void SoundnessIsFinerPerRuleThanPerSet()
        {
            var rules = MatchedRules.All.SelectMany(set => set.Rules).ToList();
            Assert.Equal(340, rules.Count);
            Assert.Equal(189,rules.Count(rule => rule.Soundness is Soundness.Sound));
            Assert.Equal(151, rules.Count(rule => rule.Soundness is Soundness.SoundUnderAssumptions));

            // And every set still reports the weakest of them, which is what makes the set grain
            // uninformative rather than wrong.
            Assert.All(RewriteRules.All,
                set => Assert.Equal(Soundness.SoundUnderAssumptions, set.Soundness));
        }

        /// <summary>
        /// A rule's own tier reaches the registry, and a <c>switch</c> arm's absence of one is
        /// reported as absent rather than as its set's.
        /// </summary>
        /// <remarks>
        /// Twenty-one sets are described from their data form and carry a tier per rule. Of the
        /// nine left, six are still described by <c>RuleRegistryGenerator</c> reading a <c>switch</c>
        /// they no longer run, and three — the <c>CanonicalOrder</c> family — still run theirs, so
        /// describing it is honest. Named rather than counted, so that repointing a set is a change
        /// to this list rather than a silent one.
        /// </remarks>
        [Fact]
        public void ARuleWrittenAsDataCarriesItsTierIntoTheRegistry()
        {
            var fromData = new[]
            {
                nameof(RewriteRules.Boolean),
                nameof(RewriteRules.CanonicalOrder),
                nameof(RewriteRules.CanonicalOrderCountingConstants),
                nameof(RewriteRules.CanonicalOrderExact),
                nameof(RewriteRules.CollapseMultipleFractions),
                nameof(RewriteRules.Common),
                nameof(RewriteRules.CollapseTrigonometricFunctions),
                nameof(RewriteRules.CommonDenominator),
                nameof(RewriteRules.CommonDenominatorCountingConstants),
                nameof(RewriteRules.CommonDenominatorExact),
                nameof(RewriteRules.DivisionPreparing),
                nameof(RewriteRules.ExpandFactorialDivisions),
                nameof(RewriteRules.ExpandMultipleAngle),
                nameof(RewriteRules.ExpandTrigonometric),
                nameof(RewriteRules.Expansion),
                nameof(RewriteRules.Factorization),
                nameof(RewriteRules.FactorizeFactorialMultiplications),
                nameof(RewriteRules.InequalityEquality),
                nameof(RewriteRules.InvertNegativeMultipliers),
                nameof(RewriteRules.InvertNegativePowers),
                nameof(RewriteRules.NormalTrigonometricForm),
                nameof(RewriteRules.NumericNeat),
                nameof(RewriteRules.PerfectSquare),
                nameof(RewriteRules.PhiFunction),
                nameof(RewriteRules.PolynomialGcdCancellation),
                nameof(RewriteRules.PolynomialLongDivision),
                nameof(RewriteRules.Power),
                nameof(RewriteRules.RationalizeDenominator),
                nameof(RewriteRules.SetOperator),
                nameof(RewriteRules.Trigonometric),
            };

            var described = RewriteRules.All
                .Where(set => set.Rules.Count > 0 && set.Rules.All(rule => rule.Soundness is not null))
                .Select(set => set.Name)
                .OrderBy(name => name, StringComparer.Ordinal);
            Assert.Equal(fromData.OrderBy(name => name, StringComparer.Ordinal), described);

            // And the rest report no tier of their own rather than their set's.
            var generated = RewriteRules.All.Where(set => !fromData.Contains(set.Name));
            Assert.All(generated, set => Assert.All(set.Rules, rule => Assert.Null(rule.Soundness)));
        }

        /// <summary>
        /// <b>Repointing a set at its data form gains descriptions rather than trading them.</b>
        /// </summary>
        /// <remarks>
        /// A set is repointed only once every rule of it carries an identity, so that the registry
        /// gains descriptions rather than trading them. The first thirteen were the ones with no
        /// described arm at all; the six after them are one arm to one rule, so their existing
        /// descriptions carry across and the rules that had none gain one. Of the seven still on
        /// the <c>switch</c>, repointing <c>Common</c> today would lose 33 descriptions and
        /// <c>Power</c> 22, which is what porting those identities is for.
        /// </remarks>
        [Fact]
        public void EveryRuleOfARepointedSetCarriesItsIdentity()
        {
            var repointed = RewriteRules.All
                .Where(set => set.Rules.Count > 0 && set.Rules.All(rule => rule.Soundness is not null))
                .ToList();
            Assert.Equal(334, repointed.Sum(set => set.Rules.Count));
            Assert.All(repointed, set => Assert.All(set.Rules, rule => Assert.NotNull(rule.Description)));
        }

        /// <summary>
        /// <b>The identity has somewhere to live now, which is what blocked the registry from
        /// describing the rules it runs.</b>
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>RuleRegistryGenerator</c> reads the comment above a <c>switch</c> arm into
        /// <see cref="RewriteRule.Description"/>, and <b>95 of the registry's 407 arms</b> carry one
        /// — <c>a / (b / c) = a * c / b</c> and its like, which is the sentence a step would be
        /// rendered as. A rule written as data has its identity in a comment too, and a comment is
        /// not readable at run time, so converting a set to data used to <i>lose</i> the
        /// description. Now it does not have to.
        /// </para>
        /// <para>
        /// Demonstrated end to end on the one set the registry already describes from its data
        /// form: two rules that reported no identity at all now report one, through the public
        /// surface.
        /// </para>
        /// </remarks>
        [Fact]
        public void ADescriptionSurvivesTheJourneyIntoTheRegistry()
        {
            var described = RewriteRules.RationalizeDenominator.Rules;
            Assert.Equal(2, described.Count);
            Assert.Equal("k * (value / d) = (k * value) / d", described[0].Description);
            Assert.Equal("num / (a + b) = num * (a - b) / (a^2 - b^2)", described[1].Description);
        }

        /// <summary>
        /// A rule read backwards says so, rather than repeating the forward identity as though it
        /// were the same claim.
        /// </summary>
        [Fact]
        public void AReversedRuleDoesNotClaimTheForwardIdentity()
        {
            var described = MatchedRules.All
                .SelectMany(set => set.Rules)
                .Where(rule => rule.Description is not null && rule.Reversed is not null)
                .ToList();
            Assert.All(described, rule =>
                Assert.StartsWith("read backwards: ", rule.Reversed!.Description!));

            var undescribed = MatchedRules.All
                .SelectMany(set => set.Rules)
                .Where(rule => rule.Description is null && rule.Reversed is not null);
            Assert.All(undescribed, rule => Assert.Null(rule.Reversed!.Description));
        }
    }
}
