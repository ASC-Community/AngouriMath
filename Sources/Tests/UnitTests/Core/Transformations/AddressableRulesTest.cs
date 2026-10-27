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
using AngouriMath.Extensions;
using AngouriMath.Functions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// The generated rules have to be the <c>switch</c> they were generated from, arm for arm.
    /// https://github.com/asc-community/AngouriMath/issues/825
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the check that makes the generator worth trusting, and it is why the design is a
    /// generator rather than a hand transcription: the arms are copied as syntax, so a rule
    /// <i>cannot</i> say something its arm does not — and this test says so out loud, over
    /// generated input, at every node rather than only at the root. Most rules match a shape that
    /// only ever occurs as a subexpression, so a root-only comparison would exercise a handful of
    /// them and report green.
    /// </para>
    /// <para>
    /// The comparison is per node and three-way: which rule the set says fired, what that rule
    /// produces, and what the <c>switch</c> produces. Agreeing on the answer while disagreeing on
    /// which rule gave it would make every derivation built on this wrong in a way no output
    /// comparison notices.
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class AddressableRulesTest
    {
        /// <summary>Each addressable set, next to the method its arms were generated from.</summary>
        private static IEnumerable<(RewriteRuleSet Set, Func<Entity, Entity> Switch)> Addressable()
        {
            yield return (RewriteRules.Common, Patterns.CommonRules);
            yield return (RewriteRules.DivisionPreparing, Patterns.DivisionPreparingRules);
            yield return (RewriteRules.NumericNeat, Patterns.NumericNeatRules);
            yield return (RewriteRules.InvertNegativeMultipliers, Patterns.InvertNegativeMultipliers);
            yield return (RewriteRules.Power, Patterns.PowerRules);
            yield return (RewriteRules.Expansion, Patterns.ExpandRules);
            yield return (RewriteRules.Factorization, Patterns.FactorizeRules);
            yield return (RewriteRules.Trigonometric, Patterns.TrigonometricRules);
            yield return (RewriteRules.NormalTrigonometricForm, Patterns.NormalTrigonometricForm);
            yield return (RewriteRules.CollapseTrigonometricFunctions, Patterns.CollapseTrigonometricFunctions);
            yield return (RewriteRules.ExpandTrigonometric, Patterns.ExpandTrigonometricRules);
            yield return (RewriteRules.ExpandMultipleAngle, Patterns.ExpandMultipleAngleRules);
            yield return (RewriteRules.Boolean, Patterns.BooleanRules);
            yield return (RewriteRules.InequalityEquality, Patterns.InequalityEqualityRules);
            yield return (RewriteRules.SetOperator, Patterns.SetOperatorRules);
            yield return (RewriteRules.PhiFunction, Patterns.PhiFunctionRules);
            // The factory shape: one switch, three sets, differing only in the sort level it is
            // closed over. Each is replayed against the switch built at its own level, so a rule
            // generated at one level and applied at another would show up here.
            yield return (RewriteRules.CanonicalOrder, Patterns.SortRules(TreeAnalyzer.SortLevel.HIGH_LEVEL));
            yield return (RewriteRules.CanonicalOrderCountingConstants, Patterns.SortRules(TreeAnalyzer.SortLevel.MIDDLE_LEVEL));
            yield return (RewriteRules.CanonicalOrderExact, Patterns.SortRules(TreeAnalyzer.SortLevel.LOW_LEVEL));
            // Sets that are a single rule: the method is the rule, so it is its own switch here.
            yield return (RewriteRules.InvertNegativePowers, Patterns.InvertNegativePowers);
            yield return (RewriteRules.PolynomialLongDivision, Patterns.PolynomialLongDivision);
            yield return (RewriteRules.PolynomialGcdCancellation, Patterns.PolynomialGcdCancellation);
            // A switch that takes a second parameter: one switch, three sets, differing only in
            // the sort level it is closed over.
            yield return (RewriteRules.CommonDenominator,
                expr => Patterns.FractionCommonDenominatorRules(expr, TreeAnalyzer.SortLevel.HIGH_LEVEL));
            yield return (RewriteRules.CommonDenominatorCountingConstants,
                expr => Patterns.FractionCommonDenominatorRules(expr, TreeAnalyzer.SortLevel.MIDDLE_LEVEL));
            yield return (RewriteRules.CommonDenominatorExact,
                expr => Patterns.FractionCommonDenominatorRules(expr, TreeAnalyzer.SortLevel.LOW_LEVEL));
            // A shape that was already readable and simply not marked.
            yield return (RewriteRules.CollapseMultipleFractions, Patterns.CollapseMultipleFractions);
            // Held back until #974 made it affordable: replaying it against its switch over this
            // corpus took 5m10s, as long as the rest of the suite. It is 5.7s now.
            yield return (RewriteRules.PerfectSquare, Patterns.PerfectSquareRules);
        }

        public static IEnumerable<object[]> AddressableSets()
            => Addressable().Select(pair => new object[] { pair.Set.Name });

        private static Func<Entity, Entity> SwitchFor(string setName)
            => Addressable().First(pair => pair.Set.Name == setName).Switch;

        private static RewriteRuleSet SetFor(string setName)
            => Addressable().First(pair => pair.Set.Name == setName).Set;

        private static readonly string[] Leaves =
            { "x", "y", "2", "-1", "1/2", "1", "0", "pi", "x > 0", "true", "false", "{ 1, 2 }" };

        private static readonly string[] Unary =
        {
            "-({0})", "1 / ({0})", "({0}) ^ 2", "sqrt({0})", "sin({0})", "cos({0})", "tan({0})",
            "abs({0})", "ln({0})", "not ({0})", "({0})!", "sgn({0})", "phi({0})",
        };

        private static readonly string[] Binary =
        {
            "({0}) + ({1})", "({0}) - ({1})", "({0}) * ({1})", "({0}) / ({1})", "({0}) ^ ({1})",
            "({0}) and ({1})", "({0}) or ({1})", "({0}) = ({1})", "({0}) >= ({1})",
            "({0}) unionwith ({1})", "log({0}, {1})",
        };

        /// <summary>
        /// Shapes a small grammar does not reach, one per rule set that the grammar alone leaves
        /// untouched. Generated input finds what nobody wrote down; a rule that only fires on
        /// <c>sin(x)*cos(x)</c> is found by writing <c>sin(x)*cos(x)</c> down. Neither subsumes
        /// the other, which is why both are here.
        /// </summary>
        private static readonly string[] Seeds =
        {
            // Trigonometric
            "sin(x) * cos(x)", "cos(x) * sin(x)", "arcsin(x) + arccos(x)", "arccos(x) + arcsin(x)",
            "arctan(x) + arccotan(x)", "arctan(1/2) + arctan(1/3)", "sin(x) / cos(x)",
            "sin(x) ^ 2 + cos(x) ^ 2", "cos(x) ^ 2 + sin(x) ^ 2", "tan(x) * cotan(x)",
            // ExpandRules, which is what RewriteRules.Expansion is
            "sin(x + y)", "sin(x - y)", "sin(2 + y)",
            // ExpandTrigonometric
            "1/2 * sin(2 * x)", "cos(2 * x)", "cos(2 * y)",
            // ExpandMultipleAngle
            "sin(2 * x)", "cos(3 * x)", "sin(-4 * y)",
            // SetOperator
            "{ 1, 2 } intersect { 1, 2 }", "{ 1, 2 } unionwith { 1, 2 }",
            "{ 1, 2 } setsubtract { 1, 2 }", "{ 1, 2 } intersect ({ 3 } unionwith { 4 })",
            "x in { 1 }", "x in [0; 1]", "[-oo; +oo]",
            // PhiFunction
            "phi(2 ^ x)", "phi(3 ^ y)",
            // NormalTrigonometricForm and CollapseTrigonometricFunctions
            "tan(x)", "cotan(x)", "sec(x)", "csc(x)", "1 / sin(x)", "cos(x) / sin(x)",
            // Factorization
            "x ^ 2 - y ^ 2", "x ^ 4 - y ^ 2", "x * y + x * 2",
            // PerfectSquare, which collapses u + 2*sqrt(u)*sqrt(v) + v. It needs a surd, and the
            // grammar above makes no fractional exponent — so without these the set never fires
            // and the replay would compare nothing while reporting green.
            "1 + sqrt(2 * x) + x / 2", "x + 2 * sqrt(x) * sqrt(y) + y", "2 + 2 * sqrt(2) + 1",
            // Power
            "log(x, y) + log(x, 2)", "(x ^ 2) ^ 3", "sqrt(x) * sqrt(x)", "2 ^ x * 2 ^ y",
        };

        /// <summary>
        /// Every expression a small grammar makes, and every node of each. A written list would
        /// only prove the cases somebody pictured, and what a rule set does at a node nobody
        /// pictured is the whole subject.
        /// </summary>
        private static List<Entity> Corpus()
        {
            var level1 = new List<string>(Leaves);
            var level2 = new List<string>();
            foreach (var shape in Unary)
                foreach (var inner in level1)
                    level2.Add(string.Format(shape, inner));
            foreach (var shape in Binary)
                foreach (var left in level1)
                    foreach (var right in level1)
                        level2.Add(string.Format(shape, left, right));
            var level3 = new List<string>();
            foreach (var shape in Binary)
                foreach (var left in level2.Where((_, i) => i % 29 == 0))
                    foreach (var right in level2.Where((_, i) => i % 31 == 0))
                        level3.Add(string.Format(shape, left, right));

            var parsed = new List<Entity>();
            foreach (var source in level1.Concat(level2).Concat(level3).Concat(Seeds))
            {
                try { parsed.Add(source.ToEntity()); }
                catch { /* the grammar makes some strings the parser declines; not its subject */ }
            }
            return parsed;
        }

        private static readonly Lazy<List<Entity>> nodes = new(() =>
        {
            var seen = new HashSet<Entity>();
            var all = new List<Entity>();
            foreach (var expr in Corpus())
                foreach (var node in expr.Nodes)
                    if (seen.Add(node))
                        all.Add(node);
            return all;
        });

        private static List<Entity> Nodes() => nodes.Value;

        [Theory]
        [MemberData(nameof(AddressableSets))]
        public void EveryArmIsTheArmItWasGeneratedFrom(string setName)
        {
            var set = SetFor(setName);
            var bySwitch = SwitchFor(setName);
            var corpus = Nodes();
            Assert.True(corpus.Count > 2000, $"the corpus is only {corpus.Count} nodes");

            var disagreements = new List<string>();
            var fired = 0;
            foreach (var node in corpus)
            {
                Entity expected;
                try { expected = bySwitch(node); }
                catch { continue; /* an arm that throws is not a claim about the registry */ }

                RewriteRule? rule;
                try { rule = set.RuleFiringAt(node); }
                catch (Exception thrown)
                {
                    disagreements.Add($"{node.Stringize()}: no rule could be asked ({thrown.GetType().Name})");
                    continue;
                }

                if (expected.Equals(node))
                {
                    // The switch fell through to its discard arm, so no arm matched — and no rule
                    // may claim to have.
                    if (rule is not null && !rule.TryApply(node).Equals(node))
                        disagreements.Add($"{node.Stringize()}: the switch did nothing, "
                            + $"but '{rule.Name}' claims {rule.TryApply(node).Stringize()}");
                    continue;
                }

                fired++;
                if (rule is null)
                {
                    disagreements.Add($"{node.Stringize()}: the switch gave {expected.Stringize()}, "
                        + "no rule claims it");
                    continue;
                }
                var actual = rule.TryApply(node);
                if (!expected.Equals(actual))
                    disagreements.Add($"{node.Stringize()}: the switch gave {expected.Stringize()}, "
                        + $"'{rule.Name}' gave {actual?.Stringize() ?? "null"}");
            }

            Assert.True(disagreements.Count == 0,
                $"{setName}: {disagreements.Count} of {corpus.Count} nodes disagree\n"
                + string.Join("\n", disagreements.Take(20)));
            Assert.True(fired > 0, $"{setName} never fired on the corpus, so nothing was compared");
        }

        /// <summary>
        /// A rule whose pattern and guard repeat an earlier rule's can never fire: first match
        /// wins, so the earlier one always takes it.
        /// </summary>
        /// <remarks>
        /// The generator names a rule after its pattern, so a repeat is exactly a name it has had
        /// to suffix. That makes an unreachable arm a thing the registry states rather than a
        /// thing somebody has to notice while reading a hundred of them.
        /// </remarks>
        [Fact]
        public void NoArmIsShadowedByAnIdenticalOneAboveIt()
        {
            var shadowed = RewriteRules.All
                .SelectMany(set => set.Rules.Select(rule => (set, rule)))
                .Where(pair => pair.rule.Name.Contains(" #"))
                .Select(pair => $"{pair.set.Name}: line {pair.rule.SourceLine}, {pair.rule.Name}")
                .ToList();
            Assert.True(shadowed.Count == 0,
                $"{shadowed.Count} unreachable arms:\n" + string.Join("\n", shadowed));
        }

        /// <summary>
        /// How much of the registry is addressable at rule grain, as a number that moves rather
        /// than a claim that it is "mostly" done.
        /// </summary>
        /// <remarks>
        /// A set that is not addressable is one whose rewrites are not a <c>switch</c> over the
        /// expression. Each is named here rather than counted, so that making one addressable is a
        /// change to this list rather than a silent improvement nobody records — and so that a
        /// claim about <i>why</i> a set is on it can be checked against the set.
        /// </remarks>
        [Fact]
        public void TheRegistryIsAddressableAsFarAsItSaysItIs()
        {
            var withRules = RewriteRules.All.Where(set => set.Rules.Count > 0).ToList();
            var without = RewriteRules.All.Where(set => set.Rules.Count == 0)
                .Select(set => set.Name).OrderBy(name => name, StringComparer.Ordinal).ToList();

            // None left. RationalizeDenominator was the last, and it was the only one whose
            // reason was real: a rewrite written as a procedure -- branches, locals, a conjugate
            // computed and oriented -- with no arms for the generator to read. It did not become
            // a `switch`; its rules are read from its data form instead, through
            // MatchedRuleSet.AsAddressable, which is the other half of #825. The two factorial
            // sets were on this list before it, and were not that shape at all: each was a switch
            // that a statement body and a local function had put out of reach.
            Assert.Empty(without);

            Assert.Equal(30, withRules.Count);

            // 314, and it was 407 before the registry started reporting the rules it runs rather
            // than the `switch` it no longer runs. The ninety-four that went are not rules lost, they
            // are arms the data form writes once. Boolean's thirty-six are twenty, because a
            // commutative pattern finds a shared operand wherever it sits, so eight arms of
            // distributivity are two rules and absorption's four-arms-each is one rule twice;
            // Factorization's twenty-two are eleven for the same reason; Trigonometric's forty-three
            // are thirty-three; NumericNeat's sixteen are eleven, six of them being three rules
            // written once per side a negative factor can sit on; and the two factorial sets are
            // eight arms each written as three. Every other repointed set is one arm for one rule.
            Assert.Equal(334, withRules.Sum(set => set.Rules.Count));
        }

        /// <summary>
        /// The two factorial sets, whose arms match a shape and then decide — and whose deciding
        /// is what makes "which rule fired" a question with a wrong answer available.
        /// </summary>
        [Fact]
        public void AnArmThatMatchesAndDeclinesIsNotTheRuleThatFired()
        {
            var set = RewriteRules.ExpandFactorialDivisions;
            var declined = "(x + 100)! / x!".ToEntity();

            // An arm matches -- the shape is a quotient of factorials over the same variable --
            // and hands back what it was given, because a hundred terms written out is not a
            // simplification. So the set as a whole changes nothing.
            var matched = set.Rules.Where(rule => rule.TryApply(declined) is not null).ToList();
            Assert.NotEmpty(matched);
            Assert.All(matched, rule => Assert.Equal(declined, rule.TryApply(declined)));
            Assert.Equal(declined, set.ApplyOnce(declined));
            // So no rule fired here, and saying one did would be reporting a rewrite that did not
            // happen.
            Assert.Null(set.RuleFiringAt(declined));

            // and the same set, on an input where an arm does fire, names it
            var fires = "(x + 2)! / x!".ToEntity();
            var rule = Assert.IsType<RewriteRule>(set.RuleFiringAt(fires));
            Assert.Equal(set.ApplyOnce(fires), rule.TryApply(fires));
            Assert.NotEqual(fires, set.ApplyOnce(fires));
        }

        [Fact]
        public void EveryRuleSaysWhereItIsWrittenAndWhatItMatches()
        {
            foreach (var set in RewriteRules.All)
                foreach (var rule in set.Rules)
                {
                    Assert.False(string.IsNullOrWhiteSpace(rule.Name), $"{set.Name} has a nameless rule");
                    Assert.False(string.IsNullOrWhiteSpace(rule.PatternSource), $"{set.Name}/{rule.Name} has no pattern");
                    Assert.False(string.IsNullOrWhiteSpace(rule.ReplacementSource), $"{set.Name}/{rule.Name} builds nothing");
                    Assert.True(rule.SourceLine > 0, $"{set.Name}/{rule.Name} has no line");
                    Assert.All(rule.NodeTypes, type => Assert.True(typeof(Entity).IsAssignableFrom(type),
                        $"{set.Name}/{rule.Name} is filed under {type}, which is not a node"));
                }
        }

        /// <summary>
        /// A rule's node type has to be the type it actually fires on, since the point of
        /// recording it is that a scheduler may use it to skip the rule.
        /// </summary>
        [Fact]
        public void ARuleOnlyFiresOnTheNodeTypeItIsFiledUnder()
        {
            var wrong = new List<string>();
            foreach (var set in RewriteRules.All)
                foreach (var rule in set.Rules)
                    foreach (var node in Nodes().Where((_, i) => i % 3 == 0))
                    {
                        if (rule.NodeTypes.Count == 0
                            || rule.NodeTypes.Any(type => type.IsInstanceOfType(node))) continue;
                        Entity? applied;
                        try { applied = rule.TryApply(node); }
                        catch { continue; }
                        if (applied is not null)
                            wrong.Add($"{set.Name}/{rule.Name} is filed under {string.Join("/", rule.NodeTypes.Select(type => type.Name))} "
                                + $"but fired on {node.Stringize()}");
                    }
            Assert.True(wrong.Count == 0, string.Join("\n", wrong.Take(10)));
        }

        /// <summary>
        /// The growth field has to agree with what the rule sets are for: expansion opens
        /// expressions out and factorisation gathers them up.
        /// </summary>
        [Fact]
        public void GrowthSaysWhichWayTheSetMoves()
        {
            Assert.All(RewriteRules.Expansion.Rules,
                rule => Assert.Equal(RewriteRuleGrowth.Expands, rule.Growth));
            Assert.Contains(RewriteRules.Factorization.Rules, rule => rule.Growth == RewriteRuleGrowth.Collects);
            Assert.Contains(RewriteRules.ExpandMultipleAngle.Rules, rule => rule.Growth == RewriteRuleGrowth.Expands);
        }

        [Fact]
        public void ARecordedStepNamesTheRuleAndNotOnlyTheSet()
        {
            using var recording = RewriteRecording.Start();
            RewriteRules.Common.ApplyOnce("a / (b / c)".ToEntity());
            recording.Dispose();

            var step = Assert.Single(recording.Steps);
            Assert.Equal(RewriteRules.Common, step.RuleSet);
            Assert.NotNull(step.Rule);
            Assert.Equal("dividing-by-a-quotient-multiplies-by-its-reciprocal", step.Rule.Name);
            Assert.Equal("a / (b / c) = a * c / b", step.Rule.Description);
            // The matcher's spelling of the pattern, not the `switch`'s. This read
            // `Divf(var any1, Divf(var any2, var any3))` until `Common` was described by the rules
            // it runs; both are source text for a reader, and neither is something to match
            // against -- `TryApply` below is how you ask whether the rule fires.
            Assert.Equal("Divf(var a, Divf(var b, var c))", step.Rule.PatternSource);
            Assert.Equal(step.After, step.Rule.TryApply(step.Before));
        }

        /// <summary>
        /// The set that used to have no addressable rules now names the one that fired.
        /// </summary>
        /// <remarks>
        /// This test asserted the opposite until <c>RationalizeDenominator</c> became data: that a
        /// set with no arms still records its step, with <c>step.Rule</c> null. It carried a note
        /// saying the premise was stated rather than assumed, <i>"if this set ever becomes
        /// addressable the test would otherwise keep passing while testing nothing at all"</i> —
        /// which is exactly what happened, and the note is why it failed rather than went quiet.
        /// </remarks>
        [Fact]
        public void TheLastSetWithoutArmsNowNamesTheRuleThatFired()
        {
            Assert.NotEmpty(RewriteRules.RationalizeDenominator.Rules);

            using var recording = RewriteRecording.Start();
            RewriteRules.RationalizeDenominator.ApplyOnce("1 / (3 - sqrt(5))".ToEntity());
            recording.Dispose();

            var step = Assert.Single(recording.Steps);
            Assert.Equal(RewriteRules.RationalizeDenominator, step.RuleSet);
            Assert.NotNull(step.Rule);
            Assert.Equal("a-two-term-denominator-is-multiplied-by-its-conjugate", step.Rule!.Name);
            // Rendered from the pattern rather than copied from source text, which is what a rule
            // written as data has instead of a `switch` arm's syntax.
            Assert.Equal("Divf(var num, var den)", step.Rule.PatternSource);
            Assert.Equal(RewriteRuleGrowth.Unknown, step.Rule.Growth);
        }
    }
}
