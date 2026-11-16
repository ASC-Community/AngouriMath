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
using AngouriMath.Tests.Corpus;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// The e-graph folds an arithmetic operator over two rational leaves into that number's class
    /// on insertion, beside the neutral-element fold — and what the safe ceiling does on the
    /// library's own corpus once it does.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Found by running the safe ceiling over something larger than sixteen expressions</b>,
    /// which is what <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
    /// tier 2 asked for before the graph is wired into anything. Over the growth corpus's 3,630
    /// generated shapes, 54 did not saturate within 20,000 steps — and every one of the 54 was
    /// constant-only arithmetic: <c>2 - 0 + 0 * 2</c>, <c>1/2 * 2 * sin(y)</c>,
    /// <c>2 + -2 + (-2) / (-1)</c>. With nothing folding a number, the rearranging rules respell
    /// it for ever — <c>2 * 1/2</c>, <c>2 / 2</c>, <c>1/2 * 2</c> are three e-nodes — and on
    /// pure constants the safe ceiling did not terminate. Folding them on insertion takes the 54
    /// to 7, and the run from more than ten minutes to 24 seconds.
    /// </para>
    /// <para>
    /// <b>What is left is bounded by the budget and by nothing else.</b> The seven are a
    /// rational coefficient beside a variable — <c>2 * x * 1/2</c>, <c>x / x * -x</c>,
    /// <c>x ^ 2 / x</c> — whose spellings <c>1/2 * x</c>, <c>x / 2</c>, <c>x * 2 ^ (-1)</c> the
    /// <c>Rearranges</c> rules keep exchanging. Measured once extraction stopped being the
    /// cost (<a href="https://github.com/asc-community/AngouriMath/issues/1199">#1199</a>):
    /// <c>x ^ 2 / x</c>, <c>x / x * -x</c> and <c>x / x * x * 1/2</c> are genuine runaways,
    /// ten to twelve thousand e-nodes in thirty seconds; <c>2 * x * 1/2</c> and its relatives
    /// plateau at some 560 e-nodes and stop only when 200,000 steps run out. An earlier
    /// version of this remark called them stalls at 135–583 e-nodes, which was the slow
    /// extraction and not the graph. Every one extracts the right answer regardless, and the
    /// claim in <c>RunawayBreadthTest</c> that the coefficient rules are confluent on plain
    /// arithmetic is true of the three inputs it pins and not of the family.
    /// </para>
    /// <para>
    /// <b>On the corpus the ceiling is cheap and finds a little more.</b> Every one of the 40
    /// problems saturates, none above 65 e-nodes; before the fold it moved 6 of the 15
    /// <c>Simplify</c> problems and matched <c>Simplify</c>'s answer on 3; now 7 and 5 —
    /// <c>sqrt(2) * sqrt(3)</c> reaches <c>sqrt(6)</c> and <c>(x ^ 2) ^ 3 - x ^ 6</c> reaches
    /// <c>0</c>. The eight it still leaves need what the safe set has none of: an expanding rule
    /// (<c>sin(-x)</c> to <c>-sin(x)</c> is declared <c>Expands</c>) or a rule that attaches a
    /// condition (every <c>Unknown</c> of the cancelling family).
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class ConstantFoldTest
    {
        private static readonly WorkBudget Quick = new() { Steps = 3_000, Time = TimeSpan.FromSeconds(1) };
        private static readonly WorkBudget Corpus = new() { Steps = 20_000, Time = TimeSpan.FromSeconds(2) };

        private static (bool Saturated, int Nodes, Entity? Extracted) Saturate(string source, WorkBudget budget)
        {
            var graph = new EGraph();
            var root = graph.AddEntity(source.ToEntity());
            var ledger = BudgetLedger.For(nameof(ConstantFoldTest), budget, BudgetRole.Argument);
            var saturated = Saturation.Run(graph, Saturation.RulesUpTo(RewriteRuleGrowth.Rearranges), ledger, n => n.Complexity);
            return (saturated, graph.NodeCount, graph.Extract(root, n => n.Complexity));
        }

        [Theory]
        [InlineData("1 + 1", "2")]
        [InlineData("3 / 3", "1")]
        [InlineData("2 * 3", "6")]
        [InlineData("2 ^ 10", "1024")]
        [InlineData("1/2 + 1/2", "1")]
        [InlineData("2 ^ (-1)", "1/2")]
        [InlineData("-1 * 2", "-2")]
        [InlineData("x + (1 + 1)", "x + 2")]
        public void AnArithmeticNodeOverRationalLeavesIsItsValueOnInsertion(string source, string value)
        {
            var graph = new EGraph();
            var added = graph.AddEntity(source.ToEntity());
            var expected = graph.AddEntity(value.ToEntity());
            Assert.Equal(graph.Find(expected), graph.Find(added));
        }

        [Theory]
        [InlineData("1 / 0")]
        [InlineData("2 ^ (1/2)")]
        [InlineData("2 ^ 100000")]
        [InlineData("2 ^ (-100000)")]
        [InlineData("0 ^ (-1)")]
        public void WhatIsNotARationalOrNotCheapStaysAsWritten(string source)
        {
            var graph = new EGraph();
            var root = graph.AddEntity(source.ToEntity());
            Assert.Equal(3, graph.NodeCount);
            Assert.Equal(source.ToEntity(), graph.Extract(root, n => n.Complexity));
        }

        /// <summary>
        /// A node carrying a codomain of its own is not the number: folding <c>domain(1 + 1, ZZ)</c>
        /// into <c>2</c>'s class would be the conflation <c>ENodeIdentityTest</c> exists to prevent.
        /// </summary>
        [Fact]
        public void ACodomainBearingNodeDoesNotFold()
        {
            var graph = new EGraph();
            var annotated = graph.AddEntity("domain(1 + 1, ZZ)".ToEntity());
            var two = graph.AddEntity("2".ToEntity());
            Assert.NotEqual(graph.Find(two), graph.Find(annotated));
        }

        [Theory]
        [InlineData("2 - 0 + 0 * 2")]
        [InlineData("2 + -2 + (-2) / (-1)")]
        [InlineData("1/2 * 2 * sin(y)")]
        [InlineData("1/2 * 2 * (x + -1/2)")]
        [InlineData("(2 - 0) * (-2) / (-1)")]
        [InlineData("-1/2 + 2 + (-2) / (-1)")]
        public void ConstantOnlyArithmeticSaturatesAtTheSafeCeiling(string source)
        {
            var (saturated, nodes, extracted) = Saturate(source, Quick);
            Assert.True(saturated, $"{source} runs away again at the safe ceiling: {nodes} e-nodes and no fixed point");
            Assert.NotNull(extracted);
            var expected = source.ToEntity().Simplify();
            Assert.True((extracted! - expected).Simplify().Evaled == 0 || extracted == expected,
                $"{source} extracts {extracted}, which is not {expected}");
        }

        /// <summary>
        /// Pinned in both directions under a three-thousand-step budget: an entry that starts
        /// saturating is to be deleted, and one whose graph passes a thousand e-nodes within
        /// those steps has changed character. The extraction must be right either way.
        /// </summary>
        [Fact]
        public void ACoefficientBesideAVariableIsBoundedByTheBudgetAndExtractsRight()
        {
            var stalls = new[] { "2 * x * 1/2", "x * 1/2 * 2", "x ^ 2 / x", "x / x * -x" };
            var answers = new[] { "x", "x", "x", "-x" };
            var report = new List<string>();
            for (var i = 0; i < stalls.Length; i++)
            {
                var (saturated, nodes, extracted) = Saturate(stalls[i], Quick);
                if (saturated) report.Add($"{stalls[i]} saturates now, at {nodes} e-nodes; delete it from the list");
                else if (nodes > 1_000) report.Add($"{stalls[i]} is a runaway now: {nodes} e-nodes");
                else if (extracted is null || extracted != answers[i].ToEntity())
                    report.Add($"{stalls[i]} extracts {extracted?.Stringize() ?? "(null)"} rather than {answers[i]}");
            }
            Assert.True(report.Count == 0, string.Join("\n", report));
        }

        /// <summary>
        /// What the safe ceiling does on the corpus gate's forty problems: saturates on every one,
        /// stays small, and on the <c>Simplify</c> problems moves seven and matches
        /// <c>Simplify</c>'s own answer on five. Each figure is asserted exactly, so that a
        /// declaration that widens or narrows the ceiling shows up here as the count it changed.
        /// </summary>
        [Fact]
        public void TheSafeCeilingOnTheCorpus()
        {
            var unsaturated = new List<string>();
            var largest = 0;
            var moved = new List<string>();
            var matched = new List<string>();
            foreach (var problem in AngouriMath.Tests.Corpus.Corpus.All)
            {
                var input = problem.Input.ToEntity();
                var (saturated, nodes, extracted) = Saturate(problem.Input, Corpus);
                if (!saturated) unsaturated.Add($"{problem.Name}: {nodes}");
                largest = Math.Max(largest, nodes);
                if (problem.Op != Op.Simplify || extracted is null) continue;
                if (extracted != input)
                {
                    moved.Add(problem.Name);
                    if (extracted == input.Simplify()) matched.Add(problem.Name);
                }
            }

            Assert.True(unsaturated.Count == 0, "no fixed point on: " + string.Join(", ", unsaturated));
            Assert.True(largest <= 65, $"the largest corpus graph is {largest} e-nodes; it was 65");
            Assert.True(moved.Count == 7, $"the safe ceiling moves {moved.Count} of the Simplify problems; it was 7: {string.Join(", ", moved)}");
            Assert.True(matched.Count == 5, $"it matches Simplify on {matched.Count}; it was 5: {string.Join(", ", matched)}");
        }

        /// <summary>
        /// The one the pinned five-input measurement used to leave at <c>x ^ 2 - 1 ^ 2</c>, "since
        /// no safe rule folds a numeric power": the fold is not a rule, and it happens on insertion.
        /// </summary>
        [Fact]
        public void TheDifferenceOfSquaresFoldsItsPower()
        {
            var (_, _, extracted) = Saturate("(x + 1) * (x - 1)", Quick);
            Assert.Equal("x ^ 2 - 1".ToEntity(), extracted);
        }
    }
}
