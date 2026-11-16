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
    /// How broad the saturation runaway is, and where it is not. <see cref="SaturationAblationTest"/>
    /// names the four rules behind one input; this holds the facts that decide what to do about
    /// them, so that a branch already measured dead cannot be re-proposed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two branches were on the table for the inverse pair <c>sin(2x)</c> / <c>2 sin x cos x</c>:
    /// withhold one direction from saturation, or make the two <c>Common</c> coefficient rules
    /// confluent so the graph could absorb the pair. The second assumed those rules were the
    /// problem. <b>They are not.</b> On plain arithmetic — <c>2 * x * y</c>, <c>(1/2) * x</c>,
    /// <c>2 * (x * y) / 3</c> — the widest ceiling saturates in a handful of nodes, with every
    /// trigonometric set present and with all of them removed. The coefficient rules were
    /// load-bearing in the runaway only because they supplied the respellings the trigonometric
    /// pair fed on. (Measured on these three inputs; the family is not confluent in general —
    /// <c>2 * x * 1/2</c> never reports a fixed point — but it stalls at a bounded graph with the
    /// right extraction rather than running away, which <c>ConstantFoldTest</c> pins.)
    /// </para>
    /// <para>
    /// <b>The runaway is a family, and it is bounded to the trigonometric sets.</b> Every sine
    /// input reachable from a product loops at the widest ceiling — <c>sin(2x)</c>,
    /// <c>sin(x) cos(x)</c>, <c>sin(3x)</c>, <c>sin(2x) cos(2x)</c> — and <c>cos(2x)</c> does not.
    /// With the five trigonometric sets removed, the exemplar <c>sin(2x) + cos(2x)</c> saturates
    /// at seven nodes.
    /// </para>
    /// <para>
    /// <b>And the safe ceiling already withholds the expanding direction.</b>
    /// <c>ExpandMultipleAngle</c>'s rules are declared <see cref="RewriteRuleGrowth.Expands"/>,
    /// and <see cref="Saturation.SafeRules"/> is <c>RulesUpTo(Rearranges)</c>, so at that ceiling
    /// only the collecting direction fires and every member of the family saturates. That is the
    /// measured answer to <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
    /// tier 2's "scheduling policy" item: the growth ceiling <i>is</i> the policy. When both
    /// directions of an inverse pair are declared, it keeps the collecting one and withholds the
    /// expanding one, mechanically, for every such pair — and it can only protect pairs whose
    /// directions are declared, which is what declaring growth for a code-built rule is for.
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class RunawayBreadthTest
    {
        private static readonly WorkBudget Quick = new() { Steps = 3_000, Time = TimeSpan.FromSeconds(1) };

        private static readonly string[] TrigonometricSets =
        {
            "Trigonometric", "ExpandMultipleAngle", "ExpandTrigonometric",
            "CollapseTrigonometricFunctions", "NormalTrigonometricForm",
        };

        private static IReadOnlyList<MatchedRule> Rules(RewriteRuleGrowth ceiling, Func<MatchedRuleSet, bool> keep) =>
            MatchedRules.All
                .Where(keep)
                .SelectMany(set => set.Rules)
                .Where(rule => rule.Soundness is Soundness.Sound or Soundness.SoundUnderAssumptions)
                .Where(rule => rule.Growth <= ceiling)
                .ToList();

        private static (bool Saturated, int Nodes, string Extracted) Saturate(string source, IReadOnlyList<MatchedRule> rules)
        {
            var graph = new EGraph();
            var root = graph.AddEntity(source.ToEntity());
            var ledger = BudgetLedger.For(nameof(RunawayBreadthTest), Quick, BudgetRole.Argument);
            var saturated = Saturation.Run(graph, rules, ledger, node => node.Complexity);
            return (saturated, graph.NodeCount, graph.Extract(root, node => node.Complexity)?.Stringize() ?? "(nothing)");
        }

        private static readonly (string Input, string Extracts)[] PlainArithmetic =
        {
            ("2 * x * y", "2 * x * y"),
            ("(1/2) * x", "x / 2"),
            ("2 * (x * y) / 3", "2/3 * x * y"),
        };

        [Fact]
        public void TheCoefficientRulesAreConfluentOnPlainArithmetic()
        {
            var failures = new List<string>();
            foreach (var (input, extracts) in PlainArithmetic)
                foreach (var (label, rules) in new[]
                {
                    ("all sets", Rules(RewriteRuleGrowth.Unknown, _ => true)),
                    ("no trigonometric sets", Rules(RewriteRuleGrowth.Unknown, set => !TrigonometricSets.Contains(set.Name))),
                })
                {
                    var (saturated, nodes, extracted) = Saturate(input, rules);
                    if (!saturated || extracted != extracts)
                        failures.Add($"{input} with {label}: saturated={saturated} nodes={nodes} => {extracted}");
                }

            Assert.True(failures.Count == 0,
                "the coefficient rules no longer settle on plain arithmetic — making them confluent "
                + "stopped being a phantom branch:\n" + string.Join("\n", failures));
        }

        private static readonly string[] Family =
            { "sin(2 * x)", "sin(x) * cos(x)", "sin(3 * x)", "sin(2 * x) * cos(2 * x)" };

        [Fact]
        public void TheWidestCeilingRunsAwayOnTheWholeFamilyAndNotOnCosine()
        {
            var all = Rules(RewriteRuleGrowth.Unknown, _ => true);
            var settled = Family.Where(input => Saturate(input, all).Saturated).ToList();
            Assert.True(settled.Count == 0,
                "these members of the family saturate at the widest ceiling now; delete them from "
                + "the list rather than leave them: " + string.Join(", ", settled));

            var cosine = Saturate("cos(2 * x)", all);
            Assert.True(cosine.Saturated,
                $"cos(2 * x) runs away at the widest ceiling now ({cosine.Nodes} nodes); the family "
                + "has grown a cosine side and the remark above is wrong about the asymmetry");
        }

        [Fact]
        public void TheFamilyIsBoundedToTheTrigonometricSets()
        {
            var noTrig = Rules(RewriteRuleGrowth.Unknown, set => !TrigonometricSets.Contains(set.Name));
            var (saturated, nodes, _) = Saturate("sin(2 * x) + cos(2 * x)", noTrig);
            Assert.True(saturated,
                $"the exemplar runs away ({nodes} nodes) with every trigonometric set removed, so the "
                + "runaway is no longer bounded to them");
        }

        /// <summary>
        /// The claim that makes the ceiling the policy. Asserted, not inferred: at
        /// <see cref="RewriteRuleGrowth.Rearranges"/> the expanding direction is withheld and every
        /// member of the family must settle.
        /// </summary>
        [Fact]
        public void TheSafeCeilingSaturatesTheWholeFamily()
        {
            var safe = Rules(RewriteRuleGrowth.Rearranges, _ => true);
            var runsAway = new List<string>();
            foreach (var input in Family.Append("sin(2 * x) + cos(2 * x)"))
            {
                var (saturated, nodes, extracted) = Saturate(input, safe);
                if (!saturated) runsAway.Add($"{input}: {nodes} nodes => {extracted}");
            }

            Assert.True(runsAway.Count == 0,
                "these run away at the safe ceiling, so the growth ceiling is not withholding the "
                + "expanding direction and is not the policy the remark says it is:\n"
                + string.Join("\n", runsAway));
        }
    }
}
