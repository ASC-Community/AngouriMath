//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core.Transformations.Matching;
using AngouriMath.Extensions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// Which of two rules that both fire decides the answer, and why.
    /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2 asks for
    /// "rule priorities and conflict resolution, with confluence and termination checked by tooling
    /// rather than asserted by authors". This is the priorities half;
    /// <see cref="RuleConfluenceTest"/> is the confluence half.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A rule set is first-match-wins, so where two rules fire at one node and disagree,
    /// <b>whichever is tried first decides the answer</b>. Until now that was the position somebody
    /// typed the rule at, and the one place it is written down is a comment on
    /// <see cref="MatchedRules.CollapseMultipleFractions"/>: the set "is order-dependent, since
    /// <c>Mulf(Divf, Divf)</c> has to be tried before <c>Mulf(a, Divf)</c> or the more general rule
    /// would swallow the special one".
    /// </para>
    /// <para>
    /// <see cref="MatchPattern.Subsumes"/> makes that a computed fact, and
    /// <c>MatchedRuleSet.RulesByPriority</c> applies it. What is left over — a conflict where
    /// neither pattern subsumes the other — is a bare choice, and those are recorded below by name
    /// rather than left implicit in a file's layout.
    /// </para>
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class RulePriorityTest
    {
        // `-2` earns its place by not being `-1`. Four rules that take a negative factor out of a
        // product decline a factor of -1, which is the sign rather than a factor to take out
        // (https://github.com/asc-community/AngouriMath/issues/1167) — so with -1 as the only
        // negative leaf they matched nothing here and the subsumption claims about them went
        // unwitnessed. A corpus whose only negative is the one case a rule excludes cannot
        // exercise the rule.
        private static readonly string[] Leaves = { "x", "y", "2", "-1", "-2", "1/2", "0", "1", "3" };

        private static readonly string[] Unary =
        {
            "-({0})", "sqrt({0})", "abs({0})", "ln({0})", "e ^ ({0})",
            "sin({0})", "cos({0})", "tan({0})", "sgn({0})", "1 / ({0})",
            "({0}) ^ 2", "({0}) ^ (-1)", "({0}) ^ (1/2)", "({0})!",
        };

        private static readonly string[] Binary =
        {
            "({0}) + ({1})", "({0}) - ({1})", "({0}) * ({1})", "({0}) / ({1})", "({0}) ^ ({1})",
        };

        private static List<string> Grow(IReadOnlyList<string> below, bool binary)
        {
            var grown = new List<string>();
            foreach (var shape in Unary)
                foreach (var inner in below)
                    grown.Add(string.Format(shape, inner));
            if (binary)
                foreach (var shape in Binary)
                    foreach (var left in below)
                        foreach (var right in below)
                            grown.Add(string.Format(shape, left, right));
            return grown;
        }

        /// <summary>
        /// Generated, deterministic, and <b>three levels deep with binary shapes at every one</b>,
        /// which is the difference that matters here.
        /// </summary>
        /// <remarks>
        /// A third level grown with unary shapes only never builds a quotient of quotients or a
        /// product of quotients — and those are exactly the shapes where a specific rule and the
        /// general rule that would swallow it both fire. On such a corpus <b>none</b> of the
        /// subsumption-ordered pairs below overlaps at all. Growing the third level with binary
        /// shapes too finds six of them, and takes the conflicts this sees from 3 to 45.
        /// <see cref="RuleConfluenceTest"/> was the standing example of the omission and is not
        /// any more: grown the same way, its recorded arm orderings went from 3 to 42.
        /// </remarks>
        private static List<Entity> Expressions()
        {
            var level1 = new List<string>(Leaves);
            var level2 = Grow(level1, binary: true);
            var level3 = Grow(level2.Where((_, i) => i % 11 == 0).ToList(), binary: true);
            var parsed = new List<Entity>();
            foreach (var source in level1.Concat(level2).Concat(level3))
            {
                // Not every generated string parses, and that is the generator's business.
                try { parsed.Add(source.ToEntity()); }
                catch (Exception) { }
            }
            return parsed;
        }

        private static List<Entity> Nodes()
        {
            var nodes = new List<Entity>();
            foreach (var expression in Expressions())
                foreach (var node in expression.Nodes)
                    nodes.Add(node);
            return nodes;
        }

        /// <summary>
        /// Every pair of rules of one set where one pattern is strictly more general than the
        /// other, as <c>Set: specific before general</c> — which is the order they have to be
        /// tried in.
        /// </summary>
        private static SortedSet<string> Subsumptions()
        {
            var found = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var set in MatchedRules.All)
            {
                var rules = set.Rules;
                for (var i = 0; i < rules.Count; i++)
                    for (var j = 0; j < rules.Count; j++)
                    {
                        if (i == j) continue;
                        if (!rules[i].Left.Subsumes(rules[j].Left)) continue;
                        if (rules[j].Left.Subsumes(rules[i].Left)) continue;
                        found.Add($"{set.Name}: {rules[j].Name} before {rules[i].Name}");
                    }
            }
            return found;
        }

        /// <summary>
        /// <b>The claim checked against the behaviour.</b> <see cref="MatchPattern.Subsumes"/>
        /// answers from the shape of two patterns, for every expression there is; this asks whether
        /// it was telling the truth about the expressions there are. Wherever it claims one pattern
        /// is at least as general as another, every node the narrower one matches has to be matched
        /// by the wider one too.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Over every rule rather than within a set, because the relation is about patterns and
        /// nothing about it stops at a set boundary: <b>976</b> ordered pairs claim subsumption,
        /// <b>502</b> of them (501 before the constant antilogarithm rule of #994 subsumed the symbolic one) are put to the test by the corpus containing something the narrower
        /// pattern matches, and none is contradicted across <b>85,153</b> nodes. All three counts
        /// are asserted — a corpus that stopped reaching these shapes would otherwise turn this
        /// into a test that passes by asking nothing, and a witnessed count means nothing without
        /// what it was measured over.
        /// </para>
        /// <para>
        /// <b>Why 501 and not 513.</b> Adding <c>-2</c> to the leaves moved it, and not the way it
        /// looks. Measured four ways: on the old leaves the <c>-1</c> guard of
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1167">#1167</a> cost 24
        /// witnesses (513 to 489), because <c>-1</c> was the only negative leaf and the four
        /// guarded rules then matched nothing at all. With <c>-2</c> present the guard costs
        /// <b>nothing</b> — 501 either way — which is what says the leaf restores exactly what the
        /// guard removed. The remaining 513-to-501 is not coverage at all: <c>level3</c> samples
        /// every eleventh element of <c>level2</c>, so a longer <c>level2</c> lands the sample on
        /// different expressions.
        /// </para>
        /// </remarks>
        [Fact]
        public void SubsumptionIsNeverContradictedByMatching()
        {
            var rules = MatchedRules.All
                .SelectMany(set => set.Rules.Select(rule => (Set: set.Name, Rule: rule)))
                .ToList();
            var claims =
                new List<(string General, string Specific, MatchPattern Wide, MatchPattern Narrow)>();
            foreach (var wider in rules)
                foreach (var narrower in rules)
                {
                    if (ReferenceEquals(wider.Rule, narrower.Rule)) continue;
                    if (wider.Rule.Left.Subsumes(narrower.Rule.Left))
                        claims.Add((
                            $"{wider.Set}/{wider.Rule.Name}",
                            $"{narrower.Set}/{narrower.Rule.Name}",
                            wider.Rule.Left,
                            narrower.Rule.Left));
                }

            var nodes = Nodes();
            var witnessed = 0;
            foreach (var (general, specific, wide, narrow) in claims)
            {
                var put = false;
                foreach (var node in nodes)
                {
                    bool narrowMatches;
                    try { narrowMatches = narrow.Matches(node); }
                    catch (Exception) { continue; }
                    if (!narrowMatches) continue;
                    put = true;
                    bool wideMatches;
                    try { wideMatches = wide.Matches(node); }
                    catch (Exception) { wideMatches = false; }
                    Assert.True(wideMatches,
                        $"'{general}' claims to subsume '{specific}', but {node.Stringize()} "
                        + $"matches {narrow} and not {wide}");
                }
                if (put) witnessed++;
            }

            Assert.Equal(976, claims.Count);
            Assert.Equal(502, witnessed);
            // Asserted so the two figures in the remark above cannot go stale in silence: the
            // whole point of `witnessed` is that it is a coverage number, and it means nothing
            // without knowing what it was measured over.
            Assert.Equal(85153, nodes.Count);
        }

        /// <summary>
        /// <b>The invariant that used to be a comment.</b> Where one rule of a set is strictly more
        /// specific than another it has to be tried first, or it never fires at all and the set
        /// quietly loses a rewrite. This holds the order rules are tried in equal to the order they
        /// are written in, so the file stays readable as well as correct — and it is what fires
        /// when somebody inserts a general rule above a specific one.
        /// </summary>
        [Fact]
        public void TheOrderRulesAreTriedInIsTheOrderTheyAreWrittenIn()
        {
            foreach (var set in MatchedRules.All)
                Assert.Equal(
                    set.Rules.Select(rule => rule.Name),
                    set.RulesByPriority.Select(rule => rule.Name));
        }

        /// <summary>
        /// The orderings specificity has an opinion about, by name.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Twenty-eight of the 5,480 within-set pairs, across eight sets, and none of them mutual.
        /// They are recorded because the list is the thing that changes: a rule added to
        /// <c>Boolean</c> or <c>InequalityEquality</c> whose pattern sits under an existing one
        /// joins this list, and that is worth noticing when it happens rather than the first time
        /// the two overlap.
        /// </para>
        /// <para>
        /// One of the eight sets says anything about this in its own documentation, which is the
        /// argument for computing it rather than writing it down.
        /// </para>
        /// </remarks>
        [Fact]
        public void TheRecordedSubsumptionsAreTheOnesThereAre()
        {
            var recorded = new[]
            {
                "Boolean: a-conjunction-of-negations-is-a-negated-disjunction before a-conjunction-with-a-falsehood-is-false",
                "Boolean: a-conjunction-with-itself-is-itself before a-conjunction-with-a-falsehood-is-false",
                "Boolean: a-disjunction-of-negations-is-a-negated-conjunction before a-disjunction-with-a-truth-is-true",
                "Boolean: a-disjunction-of-negations-is-a-negated-conjunction before a-negation-or-something-is-an-implication",
                "Boolean: a-disjunction-with-itself-is-itself before a-disjunction-with-a-truth-is-true",
                "Boolean: a-negation-or-something-is-an-implication before a-disjunction-with-a-truth-is-true",
                "CollapseMultipleFractions: product-of-two-quotients before product-with-a-quotient-on-the-left",
                "CollapseMultipleFractions: product-of-two-quotients before product-with-a-quotient-on-the-right",
                "CollapseMultipleFractions: quotient-of-two-quotients before quotient-whose-denominator-is-a-quotient",
                "CollapseMultipleFractions: quotient-of-two-quotients before quotient-whose-numerator-is-a-quotient",
                "CollapseTrigonometricFunctions: cosine-over-sine-is-the-cotangent before a-quotient-by-a-sine-is-a-cosecant",
                "CollapseTrigonometricFunctions: sine-over-cosine-is-the-tangent before a-quotient-by-a-cosine-is-a-secant",
                "Common: a-product-of-two-quotients-is-one-quotient before a-quotient-times-a-thing-keeps-the-divisor-outermost",
                "Common: a-product-of-two-quotients-is-one-quotient before a-thing-times-a-quotient-keeps-the-divisor-outermost",
                "ExpandFactorialDivisions: a-quotient-of-shifted-factorials before a-quotient-of-a-plain-factorial-by-a-shifted-one",
                "ExpandFactorialDivisions: a-quotient-of-shifted-factorials before a-quotient-of-a-shifted-factorial-by-a-plain-one",
                "FactorizeFactorialMultiplications: a-shifted-factorial-times-the-next-term before a-plain-factorial-times-the-next-term",
                "FactorizeFactorialMultiplications: a-shifted-factorial-times-the-next-term before a-shifted-factorial-times-a-bare-term",
                "InequalityEquality: a-greater-than-or-equal-as-written-is-at-least before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "InequalityEquality: a-greater-than-or-equal-the-other-way-round-is-at-most before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "InequalityEquality: a-less-than-or-equal-as-written-is-at-most before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "InequalityEquality: a-less-than-or-equal-the-other-way-round-is-at-least before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "InequalityEquality: an-equality-or-a-greater-than-as-written-is-at-least before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "InequalityEquality: an-equality-or-a-greater-than-the-other-way-round-is-at-most before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "InequalityEquality: an-equality-or-a-less-than-as-written-is-at-most before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "InequalityEquality: an-equality-or-a-less-than-the-other-way-round-is-at-least before two-comparisons-of-one-pair-that-leave-no-case-are-true",
                "Power: a-logarithm-of-a-reciprocal-in-a-reciprocal-base-turns-round-twice before a-logarithm-in-a-reciprocal-base-negates",
                "Power: a-logarithm-of-a-reciprocal-in-a-reciprocal-base-turns-round-twice before a-logarithm-of-a-reciprocal-negates",
                // A constant base is a symbolic base that says more, so the arm that discharges
                // the condition goes before the one that attaches it. The numeric arm is guarded
                // by a predicate, which subsumption does not read, so it is ordered by declaration.
                // https://github.com/asc-community/AngouriMath/issues/994
                "Power: a-constant-raised-to-a-logarithm-of-itself-is-the-antilogarithm before a-symbol-raised-to-a-logarithm-of-itself-is-the-antilogarithm-where-defined",
                "Power: a-constant-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm before a-symbol-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm-where-defined",
            };
            Assert.Equal(recorded.OrderBy(name => name, StringComparer.Ordinal), Subsumptions());
        }

        /// <summary>
        /// Every conflict observed on the corpus, split by whether priority decides it.
        /// </summary>
        private static (SortedSet<string> ByPriority, SortedSet<string> ByDeclaration) Conflicts()
        {
            var byPriority = new SortedSet<string>(StringComparer.Ordinal);
            var byDeclaration = new SortedSet<string>(StringComparer.Ordinal);
            var expressions = Expressions();
            foreach (var set in MatchedRules.All)
            {
                var rules = set.RulesByPriority;
                foreach (var expression in expressions)
                    foreach (var node in expression.Nodes)
                    {
                        var firing = new List<int>();
                        for (var i = 0; i < rules.Count; i++)
                        {
                            Entity? applied;
                            try { applied = rules[i].TryApply(node); }
                            catch (Exception) { continue; }
                            // A rule that matches and hands the node back has not fired.
                            if (applied is not null && !applied.Equals(node)) firing.Add(i);
                        }
                        if (firing.Count < 2) continue;

                        // Compared after normalisation, so that two rules writing one answer two
                        // ways are not called a conflict.
                        var settled = new Dictionary<int, Entity>();
                        foreach (var i in firing)
                        {
                            try { settled[i] = rules[i].TryApply(node)!.InnerSimplified; }
                            catch (Exception) { }
                        }
                        foreach (var i in firing)
                            foreach (var j in firing)
                            {
                                if (i >= j) continue;
                                if (!settled.TryGetValue(i, out var left)) continue;
                                if (!settled.TryGetValue(j, out var right)) continue;
                                if (left.Equals(right)) continue;
                                var key = $"{set.Name}: {rules[i].Name} | {rules[j].Name}";
                                var wider = rules[i].Left.Subsumes(rules[j].Left);
                                var narrower = rules[j].Left.Subsumes(rules[i].Left);
                                if (wider ^ narrower) byPriority.Add(key);
                                else byDeclaration.Add(key);
                            }
                    }
            }
            return (byPriority, byDeclaration);
        }

        /// <summary>
        /// The conflicts priority settles: two rules fire, they disagree, and one pattern is
        /// strictly more general than the other — so which of them wins is a consequence of what
        /// the rules are rather than of where they were typed.
        /// </summary>
        /// <remarks>
        /// All six are a general and a special case of one rewrite meeting on a nested quotient,
        /// and four are the pair <see cref="MatchedRules.CollapseMultipleFractions"/> describes in
        /// prose. That the prose was right is the point: what it could not do is stay right on its
        /// own.
        /// </remarks>
        [Fact]
        public void PrioritySettlesTheConflictsItHasAnOpinionAbout()
        {
            var recorded = new[]
            {
                "CollapseMultipleFractions: product-of-two-quotients | product-with-a-quotient-on-the-left",
                "CollapseMultipleFractions: product-of-two-quotients | product-with-a-quotient-on-the-right",
                "CollapseMultipleFractions: quotient-of-two-quotients | quotient-whose-denominator-is-a-quotient",
                "CollapseMultipleFractions: quotient-of-two-quotients | quotient-whose-numerator-is-a-quotient",
                "Common: a-product-of-two-quotients-is-one-quotient | a-quotient-times-a-thing-keeps-the-divisor-outermost",
                "Common: a-product-of-two-quotients-is-one-quotient | a-thing-times-a-quotient-keeps-the-divisor-outermost",
            };
            Assert.Equal(
                recorded.OrderBy(name => name, StringComparer.Ordinal), Conflicts().ByPriority);
        }

        /// <summary>
        /// The conflicts priority does <b>not</b> settle: two rules fire and disagree, and neither
        /// pattern is more general than the other, so the answer is decided by which was written
        /// first and by nothing else.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>These are the ones a reader cannot see.</b> Where one pattern subsumes another the
        /// ordering is at least legible in the patterns; here it is legible nowhere, and this list
        /// is the only place it is written down.
        /// </para>
        /// <para>
        /// Three of them are what <see cref="RuleConfluenceTest"/> records at <c>switch</c> grain,
        /// by index. The other thirty-six come from asking the data rules instead — which have
        /// names, so an ordering can be recorded as the rules it is between rather than as two
        /// numbers that move whenever somebody edits the file.
        /// </para>
        /// <para>
        /// None of the thirty-nine changes what <see cref="Entity.Simplify(int)"/> returns: the
        /// normalisation and the passes after it converge. They are recorded because that is a fact
        /// about the current rules and not a guarantee.
        /// </para>
        /// </remarks>
        [Fact]
        public void OnlyTheRecordedConflictsAreLeftToDeclarationOrder()
        {
            var recorded = new[]
            {
                "CollapseMultipleFractions: product-with-a-quotient-on-the-right | product-with-a-quotient-on-the-left",
                "CollapseMultipleFractions: quotient-whose-numerator-is-a-quotient | quotient-whose-denominator-is-a-quotient",
                "Common: a-common-factor-of-two-added-products-comes-out | a-negated-term-in-a-sum-is-a-subtraction",
                "Common: a-common-factor-of-two-added-products-comes-out | a-term-added-to-itself-doubles",
                "Common: a-common-factor-of-two-subtracted-products-comes-out | a-term-subtracted-from-itself-vanishes",
                "Common: a-factor-shared-by-a-product-and-a-quotient-added-comes-out | a-negated-term-in-a-sum-is-a-subtraction",
                "Common: a-factor-shared-by-a-quotient-and-a-product-added-comes-out | a-negated-term-in-a-sum-is-a-subtraction",
                "Common: a-function-times-a-number-puts-the-number-first | a-reciprocal-rational-factor-is-a-division",
                "Common: a-product-of-two-quotients-is-one-quotient | a-thing-times-itself-is-its-square",
                "Common: a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero | a-shared-factor-cancels-between-two-products",
                "Common: a-quotient-times-a-thing-keeps-the-divisor-outermost | a-reciprocal-rational-factor-is-a-division",
                "Common: a-quotient-times-a-thing-keeps-the-divisor-outermost | a-thing-times-a-quotient-keeps-the-divisor-outermost",
                "Common: a-quotient-times-a-thing-keeps-the-divisor-outermost | a-thing-times-itself-is-its-square",
                "Common: a-term-added-to-itself-doubles | a-negated-term-in-a-sum-is-a-subtraction",
                "Common: a-thing-times-a-quotient-keeps-the-divisor-outermost | a-reciprocal-rational-factor-is-a-division",
                "Common: a-thing-times-a-quotient-keeps-the-divisor-outermost | a-thing-times-itself-is-its-square",
                "Common: a-variable-times-a-number-puts-the-number-first | a-reciprocal-rational-factor-is-a-division",
                "Common: dividing-by-a-quotient-multiplies-by-its-reciprocal | a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero",
                "Common: dividing-by-a-quotient-multiplies-by-its-reciprocal | dividing-twice-divides-by-the-product",
                "Common: dividing-twice-divides-by-the-product | a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero",
                "Common: two-numbers-around-a-factor-collect | a-reciprocal-rational-factor-is-a-division",
                "Common: two-numeric-factors-around-a-variable-collect | a-reciprocal-rational-factor-is-a-division",
                "Common: two-numeric-multiples-of-one-variable-add | a-common-factor-of-two-added-products-comes-out",
                "Common: two-numeric-multiples-of-one-variable-add | a-negated-term-in-a-sum-is-a-subtraction",
                "Common: two-numeric-multiples-of-one-variable-add | a-term-added-to-itself-doubles",
                "Common: two-numeric-multiples-of-one-variable-subtract | a-common-factor-of-two-subtracted-products-comes-out",
                "DivisionPreparing: reciprocal-factor-becomes-a-quotient | numeric-numerator-out-of-a-product",
                "Factorization: a-factor-shared-by-two-added-products-comes-out | a-term-added-to-itself-doubles",
                "Factorization: a-factor-shared-by-two-subtracted-products-comes-out | a-term-subtracted-from-itself-vanishes",
                "Factorization: a-term-added-to-itself-doubles | a-common-factor-is-collected-out-of-a-whole-sum",
                // NumericNeat had two entries here and has none. They were
                // `a-negative-factor-in-a-left-product-comes-out | ...-right-...` and
                // `a-negative-factor-in-a-numerator-comes-out | ...-denominator-...`, and the
                // second of those is the pair that undid each other for ever on `-x / (-y)`. All
                // four decline a factor of -1 now, so they no longer fire on one node together.
                // https://github.com/asc-community/AngouriMath/issues/1167
                "Power: a-numeric-factor-comes-out-of-a-power-of-a-product | a-reciprocal-power-is-a-quotient",
                "Power: a-power-of-a-power-multiplies-the-exponents | a-reciprocal-power-is-a-quotient",
                "Power: a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero | two-powers-of-one-base-divide-by-subtracting-exponents",
                "Power: a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero | two-powers-of-one-exponent-share-a-quotient-of-bases",
                "Power: two-powers-of-one-base-divide-by-subtracting-exponents | two-powers-of-one-exponent-share-a-quotient-of-bases",
                "Power: two-powers-of-one-base-multiply-by-adding-exponents | two-powers-of-one-exponent-share-a-base",
            };
            Assert.Equal(
                recorded.OrderBy(name => name, StringComparer.Ordinal), Conflicts().ByDeclaration);
        }
    }
}
