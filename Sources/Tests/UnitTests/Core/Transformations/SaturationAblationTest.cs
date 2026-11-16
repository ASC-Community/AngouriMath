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
using AngouriMath.Core.Transformations.Matching;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// What makes saturation at the widest ceiling run away, found by taking rules out one at a
    /// time rather than by reasoning about tiers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The blow-up is not the unjudged rules as a class. It is one inverse pair, split across
    /// two sets.</b> <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
    /// tier 2's open item was phrased as "a scheduling policy for <c>Expands</c>/<c>Unknown</c>
    /// rules", on the observation that <c>sin(2x) + cos(2x)</c> reaches 254 e-nodes without
    /// saturating under the <c>Unknown</c> ceiling. Ablating every set in turn: removing any one
    /// of <c>Common</c>, <c>ExpandMultipleAngle</c> or <c>Trigonometric</c> makes it saturate at
    /// about twenty nodes, and removing <c>CollapseMultipleFractions</c> makes it <i>worse</i>
    /// (618 — that set was acting as a brake). Ablating the rules of those three sets in turn
    /// names four:
    /// </para>
    /// <list type="bullet">
    /// <item><c>ExpandMultipleAngle / sine-of-a-whole-multiple-of-an-angle</c> — <c>sin(2x)</c> to
    /// <c>2 sin x cos x</c>;</item>
    /// <item><c>Trigonometric / a-sine-times-a-cosine-of-one-angle-is-half-the-doubled-sine</c> —
    /// <c>sin x cos x</c> to <c>sin(2x) / 2</c>, <b>the exact inverse</b>;</item>
    /// <item><c>Common / a-numeric-factor-floats-out-of-a-product-of-functions</c> and
    /// <c>Common / a-reciprocal-rational-factor-is-a-division</c> — which keep respelling the
    /// <c>2 ·</c> and <c>/ 2</c> between the two, so the two forms never land in one e-class and
    /// every pass makes a fresh spelling instead of merging.</item>
    /// </list>
    /// <para>
    /// An e-graph is supposed to absorb an inverse pair — both directions land in one class and
    /// it stops. This pair does not, because its two forms differ by a coefficient the
    /// <c>Common</c> rules rearrange non-confluently. That is a precise thing, and it is the
    /// first cross-set inverse pair anything here has <i>measured</i>:
    /// <c>Docs/Contributing/InversePairTable.md</c> says single-set observation "never meets" a
    /// pair split across two sets, and saturation meets it because it runs every set at once.
    /// </para>
    /// <para>
    /// Pinned in both directions. The runaway is a known non-saturation, and each of the four
    /// rules is asserted to be load-bearing for it — so fixing the pair deletes entries here
    /// rather than leaving names that mean nothing, and a fifth rule joining the cycle fails.
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class SaturationAblationTest
    {
        private const string Runaway = "sin(2 * x) + cos(2 * x)";

        /// <summary>
        /// Saturating at twenty nodes takes a few hundred steps; the runaway is past 250 nodes by
        /// thirty thousand and keeps going. Three thousand steps separates the two cleanly, and
        /// the step ceiling rather than the wall is what bounds a non-culprit ablation — there are
        /// about a hundred of those, and at a three-second wall each they cost the suite five
        /// minutes. The second is a backstop, not the bound.
        /// </summary>
        private static readonly WorkBudget Quick = new() { Steps = 3_000, Time = TimeSpan.FromSeconds(1) };

        private static readonly (string Set, string Rule)[] LoadBearing =
        {
            ("Common", "a-numeric-factor-floats-out-of-a-product-of-functions"),
            ("Common", "a-reciprocal-rational-factor-is-a-division"),
            ("ExpandMultipleAngle", "sine-of-a-whole-multiple-of-an-angle"),
            ("Trigonometric", "a-sine-times-a-cosine-of-one-angle-is-half-the-doubled-sine"),
        };

        private static IReadOnlyList<MatchedRule> Widest(Func<MatchedRuleSet, MatchedRule, bool> keep) =>
            MatchedRules.All
                .SelectMany(set => set.Rules.Select(rule => (set, rule)))
                .Where(pair => keep(pair.set, pair.rule))
                .Select(pair => pair.rule)
                .Where(rule => rule.Soundness is Soundness.Sound or Soundness.SoundUnderAssumptions)
                .ToList();

        private static (bool Saturated, int Nodes) Saturate(string source, IReadOnlyList<MatchedRule> rules)
        {
            var graph = new EGraph();
            graph.AddEntity(source.ToEntity());
            var ledger = BudgetLedger.For(nameof(SaturationAblationTest), Quick, BudgetRole.Argument);
            var saturated = Saturation.Run(graph, rules, ledger, node => node.Complexity);
            return (saturated, graph.NodeCount);
        }

        [Fact]
        public void TheWidestCeilingDoesNotSaturateOnTheKnownRunaway()
        {
            var (saturated, nodes) = Saturate(Runaway, Widest((_, _) => true));
            Assert.False(saturated,
                $"{Runaway} saturates now, at {nodes} nodes. The inverse pair above has been "
                + "resolved; delete this and the load-bearing list rather than leave them.");
        }

        [Fact]
        public void EachOfTheFourRulesIsLoadBearingForTheRunaway()
        {
            var stillRunsAway = new List<string>();
            foreach (var (setName, ruleName) in LoadBearing)
            {
                var rules = Widest((set, rule) => !(set.Name == setName && rule.Name == ruleName));
                Assert.True(rules.Count == Widest((_, _) => true).Count - 1,
                    $"{setName}/{ruleName} is not a rule in the registry any more; the entry is stale");
                var (saturated, nodes) = Saturate(Runaway, rules);
                if (!saturated)
                    stillRunsAway.Add($"{setName}/{ruleName} removed: still {nodes} nodes and unsaturated");
            }

            Assert.True(stillRunsAway.Count == 0,
                "these entries are no longer load-bearing for the runaway and should be deleted:\n"
                + string.Join("\n", stillRunsAway));
        }

        /// <summary>
        /// And no rule outside those four is load-bearing on its own — the pair is the whole of
        /// it. Ablating every other rule of the three implicated sets must leave the runaway
        /// intact, so a fifth rule joining the cycle is a change this notices.
        /// </summary>
        [Fact]
        public void NoOtherRuleOfTheImplicatedSetsIsLoadBearing()
        {
            var implicated = new[] { "Common", "ExpandMultipleAngle", "Trigonometric" };
            var newlyLoadBearing = new List<string>();
            foreach (var set in MatchedRules.All.Where(s => implicated.Contains(s.Name)))
                foreach (var rule in set.Rules)
                {
                    if (LoadBearing.Contains((set.Name, rule.Name))) continue;
                    var rules = Widest((s, r) => !ReferenceEquals(r, rule));
                    var (saturated, nodes) = Saturate(Runaway, rules);
                    if (saturated)
                        newlyLoadBearing.Add($"{set.Name}/{rule.Name}: removing it saturates at {nodes}");
                }

            Assert.True(newlyLoadBearing.Count == 0,
                "these rules are now load-bearing for the runaway and belong in the list above:\n"
                + string.Join("\n", newlyLoadBearing));
        }
    }
}
