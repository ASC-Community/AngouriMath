//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Core.Transformations;
using AngouriMath.Core.Transformations.Matching;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    [Trait("Area", "Core")]
    public sealed class MatchedRuleGrowthTest
    {
        [Fact]
        public void ACodeBuiltReplacementCannotSayItsGrowth()
        {
            // RationalizeDenominator's two rules are both RuleReversal.ReplacementIsCode --
            // see Docs/Contributing/InversePairTable.md, which measured this directly.
            foreach (var rule in MatchedRules.RationalizeDenominator.Rules)
                Assert.Equal(RewriteRuleGrowth.Unknown, rule.Growth);
        }

        [Fact]
        public void EveryPatternReplacementRuleHasADeterminedGrowth()
        {
            foreach (var set in MatchedRules.All)
                foreach (var rule in set.Rules)
                    if (rule.Right is not null)
                        Assert.NotEqual(RewriteRuleGrowth.Unknown, rule.Growth);
        }

        [Fact]
        public void ACodeBuiltRuleCanDeclareItsOwnGrowth()
        {
            var declared = new MatchedRule(
                "test-declared-growth",
                MatchPattern.Any("x"),
                (Bindings b) => b["x"],
                Soundness.Sound,
                growth: RewriteRuleGrowth.Collects);
            Assert.Equal(RewriteRuleGrowth.Collects, declared.Growth);
        }

        [Fact]
        public void ACodeBuiltRuleWithNoDeclaredGrowthStaysUnknown()
        {
            var undeclared = new MatchedRule(
                "test-undeclared-growth",
                MatchPattern.Any("x"),
                (Bindings b) => b["x"],
                Soundness.Sound);
            Assert.Equal(RewriteRuleGrowth.Unknown, undeclared.Growth);
        }
    }
}
