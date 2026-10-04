//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath;
using AngouriMath.Core.Transformations;
using AngouriMath.Core.Transformations.Matching;
using AngouriMath.Extensions;
using AngouriMath.Functions;
using Xunit;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// A rule set written as **data** has to do exactly what the <c>switch</c> that already
    /// expresses it does. https://github.com/asc-community/AngouriMath/issues/746 v1.0 asks for
    /// pattern matching as a data structure; this is what makes replacing a `switch` with one a
    /// mechanical step rather than a leap of faith.
    /// </summary>
    /// <remarks>
    /// The comparison is differential and generative: both forms are run over every expression
    /// a small grammar produces, and they must agree on all of them. A hand-written list of
    /// cases would only prove the cases someone thought of, and the interesting disagreements
    /// in a matcher are the shapes nobody pictured — a literal that is a `Rational` rather than
    /// an `Integer`, a node whose child count differs from the pattern's, a name bound twice.
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class MatchedRulesAgreeWithTheSwitchTest
    {
        // -2 and -1/2 are here for the sets that key on a negative literal. With -1 as the only
        // negative leaf a rule about negative powers fired on 7 of 1399 expressions, which is
        // agreement between two things that barely ran.
        private static readonly string[] Leaves = { "x", "y", "2", "-1", "-2", "1/2", "-1/2", "1", "0" };

        private static readonly string[] Unary =
        {
            "-({0})", "1 / ({0})", "({0}) ^ 2", "({0}) ^ (-2)", "sqrt({0})", "sin({0})", "abs({0})",
        };

        private static readonly string[] Binary =
        {
            "({0}) + ({1})", "({0}) - ({1})", "({0}) * ({1})", "({0}) / ({1})", "({0}) ^ ({1})",
        };

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
                foreach (var left in level2.Where((_, i) => i % 17 == 0))
                    foreach (var right in level2.Where((_, i) => i % 23 == 0))
                        level3.Add(string.Format(shape, left, right));

            var parsed = new List<Entity>();
            foreach (var source in level1.Concat(level2).Concat(level3))
            {
                try { parsed.Add(source.ToEntity()); }
                catch { /* the generator makes some strings the parser declines; not its subject */ }
            }
            return parsed;
        }

        private static void AssertAgrees(
            string what, System.Func<Entity, Entity> bySwitch, MatchedRuleSet byData, int leastFirings,
            string[]? extra = null, string[]? firesWhereTheSwitchDoesNot = null)
        {
            var corpus = Corpus();
            Assert.True(corpus.Count > 500, $"the corpus is only {corpus.Count} expressions");
            // A shape the grammar does not generate is still worth comparing on, where a set keys
            // on a function the corpus has no leaf for. It is added to the generated corpus and
            // never in place of it: a hand-written list only proves the cases someone thought of.
            if (extra is not null)
                foreach (var source in extra)
                    corpus.Add(source.ToEntity());

            var disagreements = new List<string>();
            var extraFirings = new List<string>();
            var fired = 0;
            foreach (var expr in corpus)
            {
                var expected = bySwitch(expr);
                var actual = byData.ApplyHere(expr);
                if (!expected.Equals(expr)) fired++;
                if (expected.Equals(actual))
                    continue;
                // A rule expressed commutatively can fire where the `switch` misses an
                // orientation it never wrote out. That is a change rather than a disagreement,
                // and it is only allowed where the caller names the shape it happens on -- so a
                // silent divergence is still a failure and a known one is a list to read.
                if (expected.Equals(expr) && firesWhereTheSwitchDoesNot is not null
                    && firesWhereTheSwitchDoesNot.Contains(expr.Stringize()))
                {
                    extraFirings.Add(expr.Stringize());
                    continue;
                }
                disagreements.Add($"{expr.Stringize()}: switch gave {expected.Stringize()}, "
                    + $"data gave {actual.Stringize()}");
            }

            // The set has to actually fire, or agreement is the agreement of two things that
            // both did nothing.
            Assert.True(fired >= leastFirings,
                $"{what}: the rules only fired on {fired} of {corpus.Count} expressions");
            Assert.True(disagreements.Count == 0,
                $"{what}: {disagreements.Count} of {corpus.Count} disagreed:\n"
                + string.Join("\n", disagreements.Take(10)));

            // The other direction, so a named shape cannot outlive the gap it describes: if the
            // `switch` learns the orientation, this list is wrong and says so.
            if (firesWhereTheSwitchDoesNot is not null)
                Assert.Equal(
                    firesWhereTheSwitchDoesNot.OrderBy(one => one, StringComparer.Ordinal).ToArray(),
                    extraFirings.Distinct().OrderBy(one => one, StringComparer.Ordinal).ToArray());
        }

        [Fact]
        public void DivisionPreparingAsDataMatchesTheSwitch()
            => AssertAgrees("DivisionPreparing", Patterns.DivisionPreparingRules,
                MatchedRules.DivisionPreparing, leastFirings: 20);

        /// <summary>
        /// The second set, and the one that says whether the shape generalises. It is harder in
        /// three ways: eight rules rather than three, an order that is load-bearing — the
        /// quotient-times-quotient rule has to be tried before the general product rule or the
        /// general one swallows it — and a predicate on a hole,
        /// <c>Integer { IsPositive: true }</c>.
        /// </summary>
        [Fact]
        public void CollapseMultipleFractionsAsDataMatchesTheSwitch()
            => AssertAgrees("CollapseMultipleFractions", Patterns.CollapseMultipleFractions,
                MatchedRules.CollapseMultipleFractions, leastFirings: 50);

        /// <summary>
        /// The first set here whose replacement is <b>code</b>: <c>-1 * n</c> is arithmetic on
        /// the bound integer, so the rule builds <c>a ^ 3</c> where a pattern would build
        /// <c>a ^ (-1 * -3)</c>. Agreement over generated expressions is what says the arithmetic
        /// was done the same way, and there is no other way to check it.
        /// </summary>
        [Fact]
        public void InvertNegativePowersAsDataMatchesTheSwitch()
            => AssertAgrees("InvertNegativePowers", Patterns.InvertNegativePowers,
                MatchedRules.InvertNegativePowers, leastFirings: 20);

        /// <summary>
        /// Two rules, one with a code replacement and one whose sides are both patterns, so this
        /// is the first set where <see cref="MatchedRule.Reversal"/> differs between the rules of
        /// one set rather than between sets.
        /// </summary>
        [Fact]
        public void InvertNegativeMultipliersAsDataMatchesTheSwitch()
            => AssertAgrees("InvertNegativeMultipliers", Patterns.InvertNegativeMultipliers,
                MatchedRules.InvertNegativeMultipliers, leastFirings: 40);

        /// <summary>
        /// Four rules whose sides are all patterns, so this set reverses whole. Nothing runs the
        /// reverse direction today, which is why agreement forward is what is asserted.
        /// </summary>
        /// <remarks>
        /// The generated corpus has no <c>tan</c>, <c>cotan</c>, <c>sec</c> or <c>cosec</c> leaf
        /// and so fired this set <b>zero</b> times — agreement between two things neither of
        /// which ran. The shapes are supplied on top of the corpus rather than instead of it.
        /// </remarks>
        [Fact]
        public void NormalTrigonometricFormAsDataMatchesTheSwitch()
            => AssertAgrees("NormalTrigonometricForm", Patterns.NormalTrigonometricForm,
                MatchedRules.NormalTrigonometricForm, leastFirings: 12, extra: new[]
                {
                    "tan(x)", "cotan(x)", "sec(x)", "cosec(x)",
                    "tan(x + y)", "cotan(2 * x)", "sec(sqrt(x))", "cosec(-x)",
                    "tan(1/2)", "cotan(0)", "sec(2)", "cosec(-1)",
                    "tan(x) + cotan(x)", "sec(x) * cosec(x)", "1 / tan(x)", "sin(tan(x))",
                });

        /// <summary>
        /// The reverse of <see cref="MatchedRules.NormalTrigonometricForm"/>, and the set where
        /// order is most obviously load-bearing: the two named quotients have to be tried before
        /// the general reciprocal rules, or <c>sin(x) / cos(x)</c> becomes <c>sin(x) * sec(x)</c>.
        /// </summary>
        [Fact]
        public void CollapseTrigonometricFunctionsAsDataMatchesTheSwitch()
            => AssertAgrees("CollapseTrigonometricFunctions", Patterns.CollapseTrigonometricFunctions,
                MatchedRules.CollapseTrigonometricFunctions, leastFirings: 25);

        /// <summary>
        /// The angle-sum identities, both of whose sides are patterns. The corpus has no sum
        /// inside a sine, so the shapes are given.
        /// </summary>
        [Fact]
        public void ExpansionAsDataMatchesTheSwitch()
            => AssertAgrees("Expansion", Patterns.ExpandRules,
                MatchedRules.Expansion, leastFirings: 5, extra: new[]
                {
                    "sin(x + y)", "sin(x - y)", "sin(2 + x)", "sin(x - 1/2)",
                    "sin(x + y) * cos(x - y)", "sin(sin(x) + cos(y))",
                });

        /// <summary>
        /// The doubled-angle identities, keyed on literals — <c>1/2</c> and <c>2</c> — which the
        /// corpus does build but never in this arrangement.
        /// </summary>
        [Fact]
        public void ExpandTrigonometricAsDataMatchesTheSwitch()
            => AssertAgrees("ExpandTrigonometric", Patterns.ExpandTrigonometricRules,
                MatchedRules.ExpandTrigonometric, leastFirings: 5, extra: new[]
                {
                    "1/2 * sin(2 * x)", "cos(2 * x)", "cos(2 * y)", "1/2 * sin(2 * y)",
                    "cos(2 * (x + y))", "1/2 * sin(2 * sin(x))", "cos(3 * x)", "1/3 * sin(2 * x)",
                });

        /// <summary>
        /// A predicate on a hole asked through the <c>switch</c>'s own helper, so the two cannot
        /// disagree about where the multiplier stops being worth expanding.
        /// </summary>
        [Fact]
        public void ExpandMultipleAngleAsDataMatchesTheSwitch()
            => AssertAgrees("ExpandMultipleAngle", Patterns.ExpandMultipleAngleRules,
                MatchedRules.ExpandMultipleAngle, leastFirings: 6, extra: new[]
                {
                    "sin(2 * x)", "cos(2 * x)", "sin(3 * x)", "cos(5 * x)",
                    "sin(8 * x)", "cos(9 * x)", "sin(1 * x)", "sin(-3 * x)",
                    "cos(x * 2)", "sin(2 * (x + y))",
                });

        /// <summary>
        /// The two sets whose rule has no side condition because the work that decides whether it
        /// applies <i>is</i> the rewrite. Agreement here is what says that handing the expression
        /// back unchanged reads the same as a <c>switch</c> arm falling through.
        /// </summary>
        [Fact]
        public void PolynomialLongDivisionAsDataMatchesTheSwitch()
            => AssertAgrees("PolynomialLongDivision", Patterns.PolynomialLongDivision,
                MatchedRules.PolynomialLongDivision, leastFirings: 20);

        [Fact]
        public void PolynomialGcdCancellationAsDataMatchesTheSwitch()
            => AssertAgrees("PolynomialGcdCancellation", Patterns.PolynomialGcdCancellation,
                MatchedRules.PolynomialGcdCancellation, leastFirings: 10);

        /// <summary>
        /// <b>Eight arms against three rules</b>, and this is the test that says the collapse is
        /// sound. Four of the eight are one rule written for every way a sum can be spelled, which
        /// <c>Commutative</c> says once — but a commutative pattern may bind the other way round
        /// where both children fit both holes, and only agreement over generated input settles
        /// whether that ever changes the answer.
        /// </summary>
        [Fact]
        public void ExpandFactorialDivisionsAsDataMatchesTheSwitch()
            => AssertAgrees("ExpandFactorialDivisions", Patterns.ExpandFactorialDivisions,
                MatchedRules.ExpandFactorialDivisions, leastFirings: 4, extra: new[]
                {
                    "(x + 3)! / x!", "x! / (x + 3)!", "(3 + x)! / x!", "x! / (3 + x)!",
                    "(x + 3)! / (x + 1)!", "(3 + x)! / (1 + x)!", "(x + 3)! / (1 + x)!",
                    "(x + 100)! / x!", "(2 + 3)! / (2 + 1)!", "(x + 1/2)! / x!",
                    "(y + 2)! / (x + 1)!", "x! / y!", "(x + 2)! / (x + 2)!",
                });

        /// <summary>
        /// The same eight-into-three collapse on the multiplicative side.
        /// </summary>
        [Fact]
        public void FactorizeFactorialMultiplicationsAsDataMatchesTheSwitch()
            => AssertAgrees("FactorizeFactorialMultiplications", Patterns.FactorizeFactorialMultiplications,
                MatchedRules.FactorizeFactorialMultiplications, leastFirings: 4, extra: new[]
                {
                    "(x - 1)! * x", "x! * (x + 1)", "x! * (1 + x)", "(1 + x)! * (x + 2)",
                    "(x + 1)! * (x + 2)", "(x + 1)! * (2 + x)", "(x + 2)! * (x + 1)",
                    "(2 + 3)! * (2 + 4)", "x! * y", "(x + 1)! * y", "x! * (x + 2)",
                });

        /// <summary>
        /// <b>The alternation case.</b> The <c>switch</c> arm is <c>x is Sumf or Minusf</c>, which
        /// <c>Node&lt;T&gt;</c> cannot say — and which the work-list in <c>Sources/Tests/Harnesses/RuleCheck</c>
        /// recorded as needing an addition to the matcher. A typed hole with a predicate says it,
        /// and agreement over the corpus is what turns that from an argument into a fact.
        /// </summary>
        [Fact]
        public void PerfectSquareAsDataMatchesTheSwitch()
            => AssertAgrees("PerfectSquare", Patterns.PerfectSquareRules,
                MatchedRules.PerfectSquare, leastFirings: 1, extra: new[]
                {
                    "1 + sqrt(2 * x) + x / 2", "x + 2 * sqrt(x) * sqrt(y) + y",
                    "x - 2 * sqrt(x) * sqrt(y) + y", "4 + 4 * x + x ^ 2",
                    "x + y", "x - y", "sin(x) + cos(x)",
                });

        /// <summary>
        /// The set that had no addressable rules at all, because it is an ordinary method with
        /// branches and locals rather than a <c>switch</c>. Its two rules now come from the data
        /// form, and this is what says the data form does what the method does.
        /// </summary>
        [Fact]
        public void RationalizeDenominatorAsDataMatchesTheSwitch()
            => AssertAgrees("RationalizeDenominator", Patterns.RationalizeDenominator,
                MatchedRules.RationalizeDenominator, leastFirings: 8, extra: new[]
                {
                    "1 / (3 - sqrt(5))", "2 / (3 - sqrt(5))", "1 / (5 + sqrt(3))",
                    "(5 - sqrt(3)) / (5 + sqrt(3))", "1 / (1 + sqrt(2))", "3 / (2 + sqrt(7))",
                    "1 / (x + sqrt(2))", "1 / (2 + 3)", "sqrt(2) / (1 + sqrt(2))",
                    "1/2 * (sqrt(2) / 3)", "1/2 * (x / 3)",
                });

        /// <summary>
        /// <b>Sixteen arms against eleven rules</b>, and the set where order matters most: the
        /// both-negative rules have to be tried before the commutative one-sided ones, which
        /// match a both-negative sum too.
        /// </summary>
        [Fact]
        public void NumericNeatAsDataMatchesTheSwitch()
            => AssertAgrees("NumericNeat", Patterns.NumericNeatRules,
                MatchedRules.NumericNeat, leastFirings: 200);

        /// <summary>
        /// <b>Thirty-six arms against sixteen rules</b>, and the set #248 is for. Distributivity
        /// is written eight times in the <c>switch</c> and absorption another eight, because a C#
        /// pattern cannot say "either way round" at two levels at once.
        /// </summary>
        /// <remarks>
        /// Boolean shapes are supplied on top of the corpus: the generated grammar builds
        /// arithmetic, so <c>and</c>, <c>or</c>, <c>not</c>, <c>xor</c> and <c>implies</c> reach
        /// this set only through what is named here.
        /// </remarks>
        [Fact]
        public void BooleanAsDataMatchesTheSwitch()
            => AssertAgrees("Boolean", Patterns.BooleanRules,
                MatchedRules.Boolean, leastFirings: 20, extra: new[]
                {
                    "not a and not b", "not a or not b", "not a or a", "a or not a",
                    "not a or b", "a and a", "a or a", "a implies a", "a xor a", "not not a",
                    "a or true", "true or a", "a and false", "false and a", "false implies a",
                    "(a and b) or (a and c)", "(b and a) or (a and c)", "(a and b) or (c and a)",
                    "(b and a) or (c and a)",
                    "(a or b) and (a or c)", "(b or a) and (a or c)", "(a or b) and (c or a)",
                    "(b or a) and (c or a)",
                    "a or (a and b)", "a and (a or b)", "(a and b) or a", "(a or b) and a",
                    "a or (not a and b)", "a and (not a or b)", "a or (b and not a)",
                    "(not a and b) or a", "(a and not b) or (b and not a)",
                    "(not b and a) or (b and not a)", "(a and not b) or (not a and b)",
                    "not a implies not b", "a implies b", "a and b", "a or b", "not a",
                },
                // Three shapes where the data form fires and the `switch` does not, because the
                // `switch` wrote some orientations of absorption and not others. Each is a
                // correct absorption -- (a and b) or a is a, and a or (b and not a) is a or b --
                // so this is the commutative form being complete where the arms were not, and it
                // is named here rather than waved through.
                firesWhereTheSwitchDoesNot: new[]
                {
                    "a and b or a", "(a or b) and a", "a or b and not a",
                });

        /// <summary>
        /// <b>Twenty-two arms against eleven rules.</b> Taking a common factor out is written
        /// four times for a sum and four for a difference, and a commutative pattern says each
        /// once — but a difference is not commutative, so only its operands' products are matched
        /// either way round, which is a distinction a <c>switch</c> cannot make visible.
        /// </summary>
        [Fact]
        public void FactorizationAsDataMatchesTheSwitch()
            => AssertAgrees("Factorization", Patterns.FactorizeRules,
                MatchedRules.Factorization, leastFirings: 100, extra: new[]
                {
                    "x ^ 4 - y ^ 6", "x ^ 2 - 9", "x ^ 2 - 2",
                    "a * b + a * c", "b * a + a * c", "a * b + c * a", "b * a + c * a",
                    "a + a * b", "a + b * a", "a * b + a", "b * a + a", "a + a",
                    "a * b - a * c", "b * a - a * c", "a * b - c * a", "b * a - c * a",
                    "a - a * b", "a - b * a", "a * b - a", "b * a - a", "a - a",
                    "x ^ 3 * y ^ 3", "sqrt(x) * sqrt(y)", "4 + 4 * sqrt(x) + x",
                });

        /// <summary>
        /// <b>Forty-three arms against thirty-three rules</b>, and the set with the most
        /// conditions: the interval guards of #884 and #887 are per rule here rather than per
        /// set, which is what the soundness field wanted all along.
        /// </summary>
        [Fact]
        public void TrigonometricAsDataMatchesTheSwitch()
            => AssertAgrees("Trigonometric", Patterns.TrigonometricRules,
                MatchedRules.Trigonometric, leastFirings: 20, extra: new[]
                {
                    "sin(x) * cos(x)", "cos(x) * sin(x)", "arcsin(x) + arccos(x)",
                    "arccos(x) + arcsin(x)", "arctan(1) + arccotan(1)", "arctan(-1) + arccotan(-1)",
                    "arctan(x) + arccotan(x)", "arctan(1/2) + arctan(1/3)", "arctan(2) + arctan(3)",
                    "arctan(sqrt(3))", "arctan(1 / sqrt(3))",
                    "sin(2 * x) * cosec(x)", "cosec(x) * sin(2 * x)",
                    "tan(x) * cotan(x)", "cotan(x) * tan(x)",
                    "arcsin(sin(1))", "arcsin(sin(3))", "arccos(cos(1))", "arccos(cos(4))",
                    "arctan(tan(1))", "arccotan(cotan(1))", "arccotan(cotan(0))",
                    "sin(arcsin(x))", "cos(arccos(x))", "tan(arctan(x))", "cotan(arccotan(x))",
                    "sin(x) ^ 2 + cos(x) ^ 2", "cos(x) ^ 2 + sin(x) ^ 2",
                    "1 - sin(x) ^ 2", "1 - cos(x) ^ 2",
                    "1 + tan(x) ^ 2", "tan(x) ^ 2 + 1", "1 + cotan(x) ^ 2", "cotan(x) ^ 2 + 1",
                    "sec(x) ^ 2 - tan(x) ^ 2", "cosec(x) ^ 2 - cotan(x) ^ 2",
                    "sin(x) ^ 2 - cos(x) ^ 2", "cos(x) ^ 2 - sin(x) ^ 2",
                    "y / sec(x)", "y / cosec(x)",
                    "sec(x) * cos(x)", "cos(x) * sec(x)", "cosec(x) * sin(x)", "sin(x) * cosec(x)",
                    "arcsin(2 / x)", "arccos(2 / x)", "arccosec(2 / x)", "arcsec(2 / x)",
                    "arcsin(x / 2)",
                });

        /// <summary>
        /// The set that was expected to need something the matcher does not have. It did not —
        /// a node pattern is not limited to two children, and a name bound twice inside a
        /// <c>ConditionalSet</c> still matches the same thing twice despite the capture-avoiding
        /// rename its traversal performs. This test is what says so.
        /// </summary>
        [Fact]
        public void SetOperatorAsDataMatchesTheSwitch()
            => AssertAgrees("SetOperator", Patterns.SetOperatorRules,
                MatchedRules.SetOperator, leastFirings: 8, extra: new[]
                {
                    @"A /\ A", @"A \/ A", @"A \ A",
                    @"{ 1, 2 } /\ ({ 2, 3 } \/ { 3, 4 })", @"({ 2, 3 } \/ { 3, 4 }) /\ { 1, 2 }",
                    "{ x : x in [0; 1] }", "{ x : x in { 1, 2 } }", "{ x : x > 0 }",
                    "1 in { 5 }", "x in { 5 }", "x in { 5, 6 }",
                    "x in [0; 1]", "x in (0; 1)", "x in (-oo; +oo)",
                    "{ true, false }", "{ true }", "(-oo; +oo)", "(0; +oo)",
                });

        /// <summary>
        /// <b>The first sets parameterised by something other than the expression</b>, and the
        /// pair that unblocks six registry entries between them: three canonical orders and three
        /// common-denominator variants, one per sort level.
        /// </summary>
        /// <remarks>
        /// Checked at every level, because the level is what the rules close over and a set built
        /// for one is not evidence about another.
        /// </remarks>
        // The level is an internal enum, so the theory carries its ordinal: a public test method
        // cannot take it as a parameter.
        private static TreeAnalyzer.SortLevel Level(int ordinal) => (TreeAnalyzer.SortLevel)ordinal;

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void SortAsDataMatchesTheSwitch(int ordinal)
        {
            var level = Level(ordinal);
            AssertAgrees($"Sort.{level}", Patterns.SortRules(level),
                MatchedRules.Sort(level), leastFirings: 100);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void CommonDenominatorAsDataMatchesTheSwitch(int ordinal)
        {
            var level = Level(ordinal);
            AssertAgrees($"CommonDenominator.{level}",
                expr => Patterns.FractionCommonDenominatorRules(expr, level),
                MatchedRules.CommonDenominator(level), leastFirings: 50);
        }

        /// <summary>
        /// <b>Sixty-five arms, and the set where transcription found a wrong answer.</b> Four of
        /// the eight `or`-with-equality arms carried their neighbour's comparison, so
        /// `(y &lt; x) or (x = y)` simplified to `x &lt;= y` — its own negation off the diagonal.
        /// That was fixed first (#1077), so this agrees with a `switch` that is right; had it been
        /// transcribed faithfully instead, this test would have passed and recorded the defect.
        /// </summary>
        /// <remarks>
        /// The corpus needs comparisons, which the arithmetic grammar does not generate, so almost
        /// all of the firings come from `extra` here. The `leastFirings` threshold is what keeps
        /// that honest.
        /// </remarks>
        [Fact]
        public void InequalityEqualityAsDataMatchesTheSwitch()
            => AssertAgrees("InequalityEquality", Patterns.InequalityEqualityRules,
                MatchedRules.InequalityEquality, leastFirings: 60, extra: new[]
                {
                    "(x < y) or (x = y)", "(y < x) or (x = y)", "(x > y) or (x = y)",
                    "(y > x) or (x = y)", "(x = y) or (x < y)", "(x = y) or (y < x)",
                    "(x = y) or (x > y)", "(x = y) or (y > x)",
                    "not (x > y)", "not (x < y)", "not (x >= y)", "not (x <= y)",
                    "not (x > y and y = z)", "not (x > y or y = z)",
                    "not (x > y and y < z and z = x)", "not (x > y or y < z or z = x)",
                    "(x > y and y > z) implies (x > z)", "(x < y and y < z) implies (x < z)",
                    "0 = x", "0 > x", "0 < x", "0 >= x", "0 <= x",
                    "2 = x", "2 > x", "2 < x", "2 >= x", "2 <= x",
                    "x < y and x > y", "x < y and x = y", "x >= y and x < y",
                    "x < y or x >= y", "x <= y or x > y", "x <= y or x >= y",
                    "x ^ 2 = 0", "x ^ (1/2) = 0", "1 / x = 0",
                    "2 * x = 0", "2 * x > 0", "2 * x >= 0", "2 * x < 0", "2 * x <= 0",
                    "x * 2 = 0", "x * 2 > 0", "x * 2 >= 0", "x * 2 < 0", "x * 2 <= 0",
                    "-2 * x = 0", "-2 * x > 0", "-2 * x >= 0", "-2 * x < 0", "-2 * x <= 0",
                    "x * (-2) = 0", "x * (-2) > 0", "x * (-2) < 0",
                    "x / 2 = 0", "x / 2 > 0", "x / 2 >= 0", "x / 2 < 0", "x / 2 <= 0",
                    "x / (-2) = 0", "x / (-2) > 0", "x / (-2) < 0",
                    "x! = 0", "x > x", "x < x", "x >= x", "x <= x",
                });

        /// <summary>
        /// <b>The largest set, and the one carrying the most branch-cut conditions.</b> Five
        /// separate issues put a guard on one rule here; the data form asks the same helpers in
        /// <c>Patterns.Power.cs</c> rather than restating any of them, which is the only way two
        /// copies of a branch cut cannot come to disagree.
        /// </summary>
        [Fact]
        public void PowerAsDataMatchesTheSwitch()
            => AssertAgrees("Power", Patterns.PowerRules,
                MatchedRules.Power, leastFirings: 100, extra: new[]
                {
                    "x ^ (2 / log(3, x))", "2 ^ log(2, x)", "3 ^ log(3, x + 1)",
                    "x ^ 2 * x ^ 3", "x ^ 2 / x ^ 3", "(x ^ 2) ^ 3", "sqrt(x ^ 2)",
                    "x ^ y * z ^ y", "x ^ y / z ^ y", "2 ^ y * 3 ^ y", "2 ^ y / 3 ^ y",
                    "x ^ y / z ^ (2 * y)", "x ^ (2 * y) / z ^ y",
                    "x / x ^ 2", "x ^ 2 / x", "x ^ (-1)", "y ^ (-1)",
                    "x ^ 2 * (x * y)", "(x * y) * x ^ 2", "x ^ 2 * (y * x)", "(y * x) * x ^ 2",
                    "(2 * x) ^ 3", "(2 * x) ^ (1/2)", "(-2 * x) ^ (1/2)",
                    "(2 / x) ^ 3 * x", "(2 / x) ^ 3 * x ^ 4",
                    "x / y / y", "x / y ^ 2 / y", "x / y / y ^ 2", "x / y ^ 3 / y ^ 2",
                    "x * y ^ 2", "log(2, x ^ 3)", "log(2, 3 ^ x)", "log(x, x)", "log(2, 2)",
                    "log(1 / x, 1 / y)", "log(x, 1 / y)", "log(1 / x, y)",
                    "log(2, 3) + log(2, 5)", "log(2, 3) - log(2, 5)",
                    "log(2, x) + log(2, y)", "log(2, x) - log(2, y)",
                    "8 ^ (1/2)", "54 ^ (1/3)", "12 ^ (1/2)", "7 ^ (1/2)", "8 ^ (3/2)",
                });

        /// <summary>
        /// <b>The last set, and the largest: a hundred arms.</b> Most of them are one shape
        /// written out in every orientation its operands can take, which is what a `switch` has
        /// to do and a commutative pattern does not — so the hundred are sixty-two rules. Every
        /// orientation collapsed here is one the `switch` writes out, and this is what says so:
        /// `firesWhereTheSwitchDoesNot` is empty, so a pattern that reached one shape more than
        /// its arms did would fail rather than pass quietly.
        /// </summary>
        [Fact]
        public void CommonAsDataMatchesTheSwitch()
            => AssertAgrees("Common", Patterns.CommonRules,
                MatchedRules.Common, leastFirings: 400, extra: new[]
                {
                    "2 * sin(x) * cos(x)", "(2 * sin(x)) * 3", "3 * (2 * sin(x))",
                    "(2 * sin(x)) * (3 * cos(x))", "(sin(x) + y) + cos(x)", "cos(x) + (sin(x) + y)",
                    "sin(x) * 2", "2 + sin(x)", "x * 2", "2 + x",
                    "2 * x + 3 * x", "2 * x - 3 * x", "(2 * x) * 3", "3 * (2 * x)",
                    "(x + 2) + 3", "3 + (x + 2)",
                    "abs(x) * abs(y)", "abs(x) / abs(y)",
                    "sgn(x) * (y * x) / abs(x)", "sgn(x) * abs(x)", "abs(x) * sgn(x)",
                    "x / 2", "(1/2) * x", "x * (1/2)", "(-1/2) * x", "x * (-1/2)",
                    "cos(-x)", "cos(-2 * x)", "sec(-x)", "abs(-x)", "sin(-x)", "sin(-2 * x)",
                    "tan(-x)", "cotan(-x)", "cosec(-x)", "sgn(-x)",
                    "(x - y) * (x + y)", "(x + y) * (x - y)",
                    "x * y / y", "y * x / y", "x * y / (y * z)", "x * y / (z * y)",
                    "(x - y) / (y - x)", "(x + y) / (y + x)",
                    "2 / (3 * x)", "2 / (x * 3)", "2 * (3 * x)",
                    "x + (y + x)", "x + (x + y)", "(y + x) + x", "(x + y) + x",
                    "(x + y) - x", "(y + x) - x", "x - (y + x)", "x - (x + y)",
                    "x + (y - x)", "x + (x - y)", "(y - x) + x", "(x - y) + x",
                    "x - (y - x)", "x - (x - y)", "(y - x) - x", "(x - y) - x",
                    "x / y + x * z", "x / y + z * x", "z * x + x / y", "x * z + x / y",
                    "x + x / y", "x / y + x", "2 + 2 / y",
                });

        /// <summary>
        /// A predicate on a hole that is a mathematical property rather than a sign or a type.
        /// The corpus reaches it rarely, so the shapes are given here as well — a set that fires
        /// four times over a generated corpus is worth checking against cases chosen for it.
        /// </summary>
        [Fact]
        public void PhiFunctionAsDataMatchesTheSwitch()
            => AssertAgrees("PhiFunction", Patterns.PhiFunctionRules,
                MatchedRules.PhiFunction, leastFirings: 1, extra: new[]
                {
                    "phi(2 ^ 5)", "phi(3 ^ 2)", "phi(7 ^ 1)", "phi(13 ^ x)",
                    "phi(4 ^ 3)", "phi(6 ^ 2)", "phi(9 ^ 2)", "phi(x ^ 2)",
                });

        /// <summary>
        /// A predicate on a hole refuses what fails it, which is the C# property pattern
        /// <c>Integer { IsPositive: true }</c> as data.
        /// </summary>
        [Theory]
        [InlineData("(x / y) ^ 2", true)]
        [InlineData("(x / y) ^ (-2)", false)]
        [InlineData("(x / y) ^ 0", false)]
        [InlineData("(x / y) ^ z", false)]
        public void APredicateOnAHoleIsChecked(string expression, bool shouldFire)
        {
            var expr = expression.ToEntity();
            var rewritten = MatchedRules.CollapseMultipleFractions.ApplyHere(expr);
            Assert.Equal(shouldFire, !rewritten.Equals(expr));
        }

        /// <summary>
        /// Order is part of the data — and <b>where one pattern subsumes another it is no longer
        /// part of the writing</b>. Reversing this set used to make the general rule swallow the
        /// special one; it does not any more, because the specific rule is put first by what the
        /// two patterns are rather than by which was typed above the other.
        /// </summary>
        /// <remarks>
        /// This test asserted the opposite until <c>MatchedRuleSet.RulesByPriority</c> existed, and
        /// its own comment gave the reason to change it: a <c>switch</c> gets its ordering "by
        /// accident of being written top to bottom", and an accident is what an ordered list of
        /// values does not have to inherit. <c>RulePriorityTest</c> is where the mechanism and its
        /// limits are.
        /// </remarks>
        [Fact]
        public void ASubsumedRuleIsTriedFirstHoweverTheSetIsWritten()
        {
            var expr = "(a / b) * (c / d)".ToEntity();
            var asWritten = MatchedRules.CollapseMultipleFractions.FirstMatching(expr);
            Assert.Equal("product-of-two-quotients", asWritten!.Name);

            var reversed = new MatchedRuleSet("reversed",
                MatchedRules.CollapseMultipleFractions.Rules.Reverse().ToArray());
            Assert.Equal("product-of-two-quotients", reversed.FirstMatching(expr)!.Name);
        }

        /// <summary>
        /// And where neither pattern subsumes the other, order is still the whole of the answer.
        /// </summary>
        /// <remarks>
        /// The two rules here both match a product of two quotients, and neither is more general
        /// than the other — one takes the quotient on the left, the other the quotient on the
        /// right — so nothing but their order decides which fires. Asked as a set of two so that
        /// the rule which subsumes them both is out of the way; in the real set it wins, which is
        /// the previous test.
        /// </remarks>
        [Fact]
        public void WhereNeitherRuleSubsumesTheOtherTheOrderStillDecides()
        {
            var expr = "(a / b) * (c / d)".ToEntity();
            var left = Named("product-with-a-quotient-on-the-left");
            var right = Named("product-with-a-quotient-on-the-right");

            Assert.False(left.Left.Subsumes(right.Left));
            Assert.False(right.Left.Subsumes(left.Left));

            Assert.Equal(left.Name, new MatchedRuleSet("left first", left, right)
                .FirstMatching(expr)!.Name);
            Assert.Equal(right.Name, new MatchedRuleSet("right first", right, left)
                .FirstMatching(expr)!.Name);
        }

        private static MatchedRule Named(string name)
            => MatchedRules.CollapseMultipleFractions.Rules.Single(rule => rule.Name == name);

        /// <summary>
        /// A rule-level guard over <b>two</b> bindings at once, which no predicate on a single
        /// hole can express: <c>(a^b)^c = a^(b*c)</c> holds for a positive base whatever the
        /// exponents, and for any base when the outer exponent is whole.
        /// </summary>
        /// <remarks>
        /// Compared against the <c>switch</c> only where the comparison is meaningful.
        /// <c>PowerRules</c> is a large set and an earlier arm may fire on the same expression,
        /// so a case counts only where the switch either did nothing or produced exactly the
        /// power-of-a-power answer; anything else means a different arm matched and says
        /// nothing about this rule. <b>That the rule cannot be isolated from its `switch` any
        /// other way is itself the argument for rules being data.</b>
        /// </remarks>
        [Fact]
        public void AGuardOverTwoBindingsMatchesTheSwitch()
        {
            var disagreements = new List<string>();
            var compared = 0;
            var fired = 0;
            foreach (var expr in Corpus())
            {
                if (expr is not Entity.Powf(Entity.Powf(var a, var b), var c)) continue;
                var expected = Patterns.PowerRules(expr);
                var mine = MatchedRules.PowerOfPower.ApplyHere(expr);
                var theAnswer = (Entity)new Entity.Powf(a, b * c);

                if (!expected.Equals(expr) && !expected.Equals(theAnswer)) continue;
                compared++;
                if (!expected.Equals(expr)) fired++;
                if (!expected.Equals(mine))
                    disagreements.Add($"{expr.Stringize()}: switch gave {expected.Stringize()}, "
                        + $"data gave {mine.Stringize()}");
            }
            Assert.True(compared > 20, $"only {compared} comparable cases");
            Assert.True(fired > 0, "the switch never applied this rule, so agreement proves nothing");
            Assert.True(disagreements.Count == 0,
                $"{disagreements.Count} of {compared} disagreed:\n" + string.Join("\n", disagreements.Take(10)));
        }

        /// <summary>
        /// The guard is the whole point: #752 is what happens when this rule is applied without
        /// one. <c>sqrt(x^2)</c> must not become <c>x</c>, since at -0.63 that is -0.63 where
        /// the expression is 0.63.
        /// </summary>
        [Theory]
        [InlineData("(x ^ 2) ^ 3", true)]      // whole outer exponent, any base
        [InlineData("(x ^ 2) ^ (-1)", true)]   // still whole
        [InlineData("(2 ^ x) ^ (1/2)", true)]  // base is a positive real
        [InlineData("(x ^ 2) ^ (1/2)", false)] // neither, and this one is #752
        [InlineData("(x ^ 2) ^ (3/2)", false)]
        [InlineData("(x ^ y) ^ z", false)]
        public void TheGuardDecidesWhetherItFires(string expression, bool shouldFire)
        {
            var expr = expression.ToEntity();
            Assert.Equal(shouldFire, !MatchedRules.PowerOfPower.ApplyHere(expr).Equals(expr));
        }

        /// <summary>
        /// And where it does fire the value survives, checked at a negative point — which is
        /// where the unguarded version went wrong.
        /// </summary>
        [Theory]
        [InlineData("(x ^ 2) ^ 3", -0.63)]
        [InlineData("(x ^ 2) ^ (-1)", -1.7)]
        [InlineData("(x ^ 3) ^ 2", -2.4)]
        public void WhereItFiresTheValueSurvives(string expression, double at)
        {
            var expr = expression.ToEntity();
            var rewritten = MatchedRules.PowerOfPower.ApplyHere(expr);
            Assert.NotEqual(expr, rewritten);
            var before = expr.Substitute("x", at).EvalNumerical().RealPart.EDecimal.ToDouble();
            var after = rewritten.Substitute("x", at).EvalNumerical().RealPart.EDecimal.ToDouble();
            Assert.Equal(before, after, 8);
        }

        /// <summary>
        /// Every <c>p*q + r*s</c> over a handful of operands. The general corpus above only
        /// happens to contain five expressions of that shape, which is too few to conclude
        /// anything from, and the shape is the whole subject here — so it is generated rather
        /// than hoped for. Four operands give 256 sums, most sharing a factor and some sharing
        /// two, which is the case the tie-break is about.
        /// </summary>
        private static List<Entity> ProductSums()
        {
            var operands = new[] { "x", "y", "z", "2" };
            var made = new List<Entity>();
            foreach (var p in operands)
                foreach (var q in operands)
                    foreach (var r in operands)
                        foreach (var s in operands)
                        {
                            try { made.Add($"{p} * {q} + {r} * {s}".ToEntity()); }
                            catch { /* every one of these parses; the guard is for the generator */ }
                        }
            return made;
        }

        /// <summary>
        /// The four hand-written arms of `{1}*{2} + {1}*{3}`, as an oracle. `CommonRules`
        /// writes the identity out once per ordering because a C# pattern cannot say "either
        /// way round"; this reproduces them, in their order, so the one commutative rule can be
        /// held against them.
        /// </summary>
        private static Entity? TheFourArms(Entity expr)
        {
            if (expr is not Entity.Sumf(Entity.Mulf(var l1, var l2), Entity.Mulf(var r1, var r2)))
                return null;
            if (l1.Equals(r1)) return l1 * (l2 + r2);
            if (l2.Equals(r1)) return l2 * (l1 + r2);
            if (l1.Equals(r2)) return l1 * (l2 + r1);
            if (l2.Equals(r2)) return l2 * (l1 + r1);
            return null;
        }

        /// <summary>
        /// **One commutative rule fires exactly where the four arms fire.** That is the claim
        /// #248 is about, and it holds.
        /// </summary>
        /// <remarks>
        /// The *value* is always the same. The *tree* is not always the same, and that is a
        /// real finding rather than a defect in either: where more than one factor is shared,
        /// the four arms and the commutative rule pull out different ones — `a*b + b*a` gives
        /// `b*(a+a)` from the arms and `a*(b+b)` from the rule, both of which are `2ab`. Which
        /// one you get is a tie-break that the `switch` fixes by the order its arms happen to be
        /// written in, and that nothing ever chose deliberately. Migrating this rule is
        /// therefore **not** purely mechanical: it needs a tie-break convention, or the printed
        /// answer moves for expressions with two shared factors.
        /// </remarks>
        [Fact]
        public void OneCommutativeRuleFiresWhereTheFourArmsDo()
        {
            var firedBoth = 0;
            var sameTree = 0;
            var disagreedOnWhether = new List<string>();
            var differentTree = new List<string>();

            foreach (var expr in ProductSums())
            {
                var byArms = TheFourArms(expr);
                var byRule = MatchedRules.SharedFactor.Rules[0].TryApply(expr);

                if ((byArms is null) != (byRule is null))
                {
                    disagreedOnWhether.Add($"{expr.Stringize()}: arms {(byArms is null ? "no" : "yes")}, "
                        + $"rule {(byRule is null ? "no" : "yes")}");
                    continue;
                }
                if (byArms is null) continue;
                firedBoth++;
                if (byArms.Equals(byRule)) sameTree++;
                else differentTree.Add($"{expr.Stringize()}: arms {byArms.Stringize()}, "
                    + $"rule {byRule!.Stringize()}");
            }

            Assert.True(firedBoth > 100, $"only {firedBoth} expressions exercised the rule");
            Assert.True(disagreedOnWhether.Count == 0,
                $"{disagreedOnWhether.Count} disagreed about *whether* to fire:\n"
                + string.Join("\n", disagreedOnWhether.Take(10)));

            // Where the trees differ, the values must not. Checked numerically rather than
            // asserted, because "both are correct" is the entire claim being made.
            foreach (var expr in ProductSums())
            {
                var byArms = TheFourArms(expr);
                var byRule = MatchedRules.SharedFactor.Rules[0].TryApply(expr);
                if (byArms is null || byRule is null || byArms.Equals(byRule)) continue;
                foreach (var at in new[] { 0.37, -1.7, 2.4 })
                {
                    static Entity Point(Entity e, double at)
                        => e.Substitute("x", at).Substitute("y", at + 1).Substitute("z", at - 0.5);
                    var one = Point(byArms, at);
                    var two = Point(byRule, at);
                    if (!one.EvaluableNumerical || !two.EvaluableNumerical) continue;
                    var left = one.EvalNumerical().RealPart.EDecimal.ToDouble();
                    var right = two.EvalNumerical().RealPart.EDecimal.ToDouble();
                    if (double.IsNaN(left) || double.IsNaN(right)) continue;
                    Assert.Equal(left, right, 8);
                }
            }
        }

        /// <summary>
        /// Backtracking, which commutativity needs and a first-match matcher does not have.
        /// `b*a + c*a` shares `a`, and finding it means abandoning the first way the left
        /// product matched — bind `k = b`, fail on the right, come back and try `k = a`.
        /// </summary>
        [Theory]
        [InlineData("b * a + c * a")]
        [InlineData("a * b + a * c")]
        [InlineData("a * b + c * a")]
        [InlineData("b * a + a * c")]
        public void CommutativeMatchingBacktracks(string expression)
            => Assert.NotNull(MatchedRules.SharedFactor.Rules[0].TryApply(expression.ToEntity()));

        /// <summary>And it does not invent a shared factor where there is none.</summary>
        [Theory]
        [InlineData("a * b + c * d")]
        [InlineData("a + b")]
        [InlineData("a * b - a * c")]
        public void CommutativeMatchingDoesNotOverreach(string expression)
            => Assert.Null(MatchedRules.SharedFactor.Rules[0].TryApply(expression.ToEntity()));

        /// <summary>
        /// Distributivity needs no condition, so this is the first rule here whose tier says
        /// something its neighbours' does not.
        /// </summary>
        [Fact]
        public void ARuleCanBeSoundWhileItsNeighboursAreNot()
        {
            Assert.Equal(Soundness.Sound, MatchedRules.SharedFactor.Soundness);
            Assert.Equal(Soundness.SoundUnderAssumptions, MatchedRules.PowerOfPower.Soundness);
        }

        /// <summary>
        /// A name used twice binds the same subexpression both times — which is the
        /// <c>when any1 == any1a</c> guard the existing rules write out by hand, made
        /// structural.
        /// </summary>
        [Fact]
        public void ARepeatedNameMustMatchTheSameSubexpression()
        {
            var pattern = MatchPattern.Node<Entity.Sumf>(MatchPattern.Any("a"), MatchPattern.Any("a"));
            Assert.NotNull(new MatchedRule("doubles", pattern,
                bound => 2 * bound["a"], Soundness.Sound).TryApply("x + x".ToEntity()));
            Assert.Null(new MatchedRule("doubles", pattern,
                bound => 2 * bound["a"], Soundness.Sound).TryApply("x + y".ToEntity()));
        }

        /// <summary>A typed hole refuses what is not of its type.</summary>
        [Fact]
        public void ATypedHoleIsTyped()
        {
            var rule = new MatchedRule("numeric-left",
                MatchPattern.Node<Entity.Mulf>(
                    MatchPattern.Any<Entity.Number>("c"), MatchPattern.Any("a")),
                bound => bound["c"] + bound["a"], Soundness.Sound);
            Assert.NotNull(rule.TryApply("2 * x".ToEntity()));
            Assert.Null(rule.TryApply("y * x".ToEntity()));
        }

        /// <summary>
        /// The set is <b>enumerable</b> and each rule is addressable by name — the property the
        /// `switch` cannot have and the reason three separate tier-2 items are blocked on this.
        /// </summary>
        [Fact]
        public void TheRulesAreEnumerableAndNamed()
        {
            var rules = MatchedRules.DivisionPreparing.Rules;
            Assert.Equal(3, rules.Count);
            Assert.All(rules, rule => Assert.False(string.IsNullOrWhiteSpace(rule.Name)));
            Assert.Equal(rules.Count, rules.Select(rule => rule.Name).Distinct().Count());
            Assert.NotNull(MatchedRules.DivisionPreparing.FirstMatching("2 / x * y".ToEntity()));
        }

        /// <summary>
        /// A set's tier is <b>derived</b> from its rules rather than declared beside them, so it
        /// cannot drift from what it is about. That is the fix for the registry's thirty sets
        /// all declaring the same value.
        /// </summary>
        [Fact]
        public void TheSetsTierIsTheWeakestOfItsRules()
        {
            Assert.Equal(Soundness.SoundUnderAssumptions, MatchedRules.DivisionPreparing.Soundness);

            var mixed = new MatchedRuleSet("mixed",
                new MatchedRule("sound", MatchPattern.Any("a"), b => b["a"], Soundness.Sound),
                new MatchedRule("heuristic", MatchPattern.Any("a"), b => b["a"], Soundness.Heuristic));
            Assert.Equal(Soundness.Heuristic, mixed.Soundness);

            var allSound = new MatchedRuleSet("sound",
                new MatchedRule("one", MatchPattern.Any("a"), b => b["a"], Soundness.Sound));
            Assert.Equal(Soundness.Sound, allSound.Soundness);
        }
    }
}
