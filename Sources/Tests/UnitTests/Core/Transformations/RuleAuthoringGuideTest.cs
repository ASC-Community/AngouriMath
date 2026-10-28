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
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// Every count stated in <c>Docs/Contributing/WritingARule.md</c>, measured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A document that states a number goes stale silently, and nothing about reading it says
    /// so.</b> A guide is worse than most: it is read by somebody who does not yet know the code,
    /// and is therefore in no position to notice that "33 of the 322 rules have a pattern on both
    /// sides" stopped being true two releases ago. The only defence is to make the number fail a
    /// build, which is what this is.
    /// </para>
    /// <para>
    /// The assertion is one-directional on purpose: it says the library still has this many, not
    /// that it should. A count that moves is a prompt to update one line of a document, and the
    /// failure message says which line.
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class RuleAuthoringGuideTest
    {
        private static void Stated(int stated, int measured, string claim)
            => Assert.True(stated == measured,
                $"WritingARule.md says {claim} is {stated}; it is {measured}. "
                + "Update the document, not this test.");

        [Fact]
        public void WhereARuleGoes()
        {
            Stated(33, MatchedRules.All.Count, "the number of rule sets written as data");
            Stated(341, MatchedRules.All.Sum(set => set.Rules.Count), "the number of rules written as data");
            Stated(30, RewriteRules.All.Count, "the number of registered sets");
            // Two families register under one name and run a data set under another --
            // CommonDenominator over MatchedRules.CommonDenominator(level), CanonicalOrder over
            // MatchedRules.Sort(level) -- so a name match alone undercounts them. This said 27
            // while the truth was 30 for exactly as long as the second family was unlisted here.
            Stated(30, RewriteRules.All.Count(set =>
                    MatchedRules.All.Any(data => data.Name == set.Name)
                    || set.Name.StartsWith("CommonDenominator", StringComparison.Ordinal)
                    || set.Name.StartsWith("CanonicalOrder", StringComparison.Ordinal)),
                "how many registered sets run the matcher");
            Stated(30, RewriteRules.All.Count(set =>
                    set.Rules.Count > 0 && set.Rules.All(rule => rule.Soundness is not null)),
                "how many registered sets describe what they run");
        }

        [Fact]
        public void TheNameIsASentence()
        {
            var names = MatchedRules.All.SelectMany(set => set.Rules)
                .Select(rule => rule.Name).Distinct(StringComparer.Ordinal).ToList();
            Stated(312, names.Count, "the number of distinct rule names");

            var words = names.Select(name => name.Split('-').Length).ToList();
            Stated(4, words.Min(), "the shortest rule name in words");
            Stated(19, words.Max(), "the longest rule name in words");
        }

        [Fact]
        public void TheIdentityIsNotTheName()
            => Stated(338,
                MatchedRules.All.SelectMany(set => set.Rules).Count(rule => rule.Description is not null),
                "how many rules carry an identity");

        [Fact]
        public void ThePatternLanguage()
            => Stated(67, MatchPattern.BuildableNodeTypes.Count, "the number of buildable node types");

        [Fact]
        public void ReplacementAPatternOrCode()
        {
            var rules = MatchedRules.All.SelectMany(set => set.Rules).ToList();
            Stated(42, rules.Count(rule => rule.Right is not null),
                "how many rules have a pattern on both sides");
            Stated(36, rules.Count(rule => rule.Reversed is not null),
                "how many two-sided rules have a direction");
            Stated(126, rules.Count(rule => rule.Growth is RewriteRuleGrowth.Unknown),
                "how many rules sit at Unknown growth");

            // The rest of the census, because `Saturation.RulesUpTo` argues from it in prose and
            // the figures it quoted had gone stale by a factor of four -- "26 collect, 17
            // rearrange, 9 expand and 270 unjudged", measured before the growth declarations
            // went in. The argument survived; the arithmetic did not. A number a remark reasons
            // from belongs somewhere that fails when it moves.
            Stated(130, rules.Count(rule => rule.Growth is RewriteRuleGrowth.Collects),
                "how many rules are declared Collects");
            Stated(49, rules.Count(rule => rule.Growth is RewriteRuleGrowth.Rearranges),
                "how many rules are declared Rearranges");
            Stated(36, rules.Count(rule => rule.Growth is RewriteRuleGrowth.Expands),
                "how many rules are declared Expands");

            // And the ones the document names. By their own names rather than by what the prose
            // calls them: the first is `squared-sine-and-cosine-of-one-argument-sum-to-one` and
            // the prose calls it the Pythagorean identity, which is right and is not what to
            // match on. All of them forget something the backwards direction would have to invent
            // -- the angle, the name the set builder bound, and which constant was raised.
            var oneWay = rules
                .Where(rule => rule.Right is not null && rule.Reversed is null)
                .Select(rule => rule.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            Assert.Equal(
                new[]
                {
                    "a-conditional-set-whose-condition-is-its-own-membership-is-that-set",
                    "a-constant-raised-to-a-logarithm-of-itself-is-the-antilogarithm",
                    "a-constant-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm",
                    "squared-sine-and-cosine-of-one-argument-sum-to-one",
                    "squared-sine-and-cosine-over-one-divisor-sum-to-its-reciprocal",
                    "squared-sine-and-cosine-with-one-coefficient-sum-to-it",
                },
                oneWay);
            Assert.All(
                rules.Where(rule => oneWay.Contains(rule.Name) && rule.Right is not null),
                rule => Assert.Equal(RuleReversal.ReplacementDropsHoles, rule.Reversal));
        }

        [Fact]
        public void SoundnessIsPerRule()
        {
            var rules = MatchedRules.All.SelectMany(set => set.Rules).ToList();
            Stated(190, rules.Count(rule => rule.Soundness is Soundness.Sound),
                "how many rules are Sound");
            Stated(151, rules.Count(rule => rule.Soundness is Soundness.SoundUnderAssumptions),
                "how many rules are SoundUnderAssumptions");
            Assert.All(RewriteRules.All,
                set => Assert.Equal(Soundness.SoundUnderAssumptions, set.Soundness));
        }

        /// <summary>
        /// The two ordering counts. Both are held by <see cref="RulePriorityTest"/> as lists — which
        /// is the stronger check, since a list says <i>which</i> — and repeated here as counts only
        /// because the document states them as counts.
        /// </summary>
        [Fact]
        public void OrderAndWhenItIsYoursToChoose()
        {
            var subsuming = 0;
            foreach (var set in MatchedRules.All)
            {
                var rules = set.Rules;
                for (var i = 0; i < rules.Count; i++)
                    for (var j = 0; j < rules.Count; j++)
                    {
                        if (i == j) continue;
                        if (rules[i].Left.Subsumes(rules[j].Left)
                            && !rules[j].Left.Subsumes(rules[i].Left))
                            subsuming++;
                    }
            }
            Stated(30, subsuming, "how many rule pairs are ordered by subsumption");
        }

        /// <summary>
        /// The tests the document promises will check a new rule all exist, by name.
        /// </summary>
        /// <remarks>
        /// A guide that sends a contributor to a test which has been renamed is worse than one that
        /// names none: they will conclude the check does not exist rather than that the document is
        /// old. Reflection over the test assembly is what keeps that honest.
        /// </remarks>
        [Fact]
        public void TheTestsItPromisesExist()
        {
            var promised = new[]
            {
                "MatchedRulesAllTest", "MatchedRulesAgreeWithTheSwitchTest", "RulePriorityTest",
                "RuleSetTerminationTest", "RuleConfluenceTest", "ReversibleRuleTest",
                "MatchedRuleGrowthTest", "StepAsASentenceTest", "RuleMetadataTest",
                "BuildableNodeTypesTest",
            };
            var present = typeof(RuleAuthoringGuideTest).Assembly.GetTypes()
                .Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
            Assert.All(promised, name => Assert.True(present.Contains(name),
                $"WritingARule.md sends a contributor to {name}, which does not exist"));
        }

        /// <summary>
        /// The worked example at the end of the document is a rule that really is in the library,
        /// and really does what the document says it does.
        /// </summary>
        [Fact]
        public void TheWorkedExampleIsReal()
        {
            var rule = MatchedRules.All.SelectMany(set => set.Rules).First(r =>
                r.Name == "a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero");
            Assert.Equal(Soundness.SoundUnderAssumptions, rule.Soundness);
            Assert.NotNull(rule.TryApply("x / x".ToEntity()));
            Assert.Null(rule.TryApply("x / y".ToEntity()));
        }
    }
}
