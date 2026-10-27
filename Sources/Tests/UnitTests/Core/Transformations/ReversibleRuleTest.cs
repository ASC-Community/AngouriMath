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
using AngouriMath.Functions;
using Xunit;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Tests.Core.Transformations
{
    /// <summary>
    /// A rule read backwards: what an expression could have <b>come from</b>, rather than what it
    /// becomes. https://github.com/asc-community/AngouriMath/issues/746 tier 2 names this as its
    /// first missing piece, and nothing in the library answers it today — the 405 addressable
    /// rules in <see cref="RewriteRules"/> carry their replacement as C# source text, which is
    /// something to read and not something to match against.
    /// </summary>
    /// <remarks>
    /// Two claims are under test and they are different. That a rule <i>can</i> be reversed is
    /// structural, decided from the two sides, and asserted rule by rule below. That the reversal
    /// is <i>right</i> is mathematics, and is checked by value rather than by shape: the forward
    /// rewrite and the backward one have to name the same number at the same point.
    /// </remarks>
    [Trait("Area", "Core")]
    public sealed class ReversibleRuleTest
    {
        /// <summary>
        /// Every set in <see cref="MatchedRules"/>, read off the type rather than listed here.
        /// </summary>
        /// <remarks>
        /// This was a hand-written list of five, and it stopped covering the file the moment a
        /// sixth set was added — so <c>EveryDataRuleIsBuildableOnBothSides</c>, whose whole
        /// purpose is to catch a template naming a node type the matcher can match but not
        /// build, was silently not looking at the new sets. It missed exactly that: a rule
        /// building a <c>Cosecantf</c>, which matched, built nothing, and was indistinguishable
        /// at run time from a rule that did not apply.
        /// </remarks>
        private static IEnumerable<MatchedRuleSet> DataRuleSets() => MatchedRules.All;

        private static readonly string[] Leaves = { "x", "y", "z", "2", "-1", "1/2", "1", "0" };

        private static readonly string[] Binary =
        {
            "({0}) + ({1})", "({0}) - ({1})", "({0}) * ({1})", "({0}) / ({1})", "({0}) ^ ({1})",
        };

        private static List<Entity> Corpus()
        {
            var level1 = new List<string>(Leaves);
            var level2 = new List<string>();
            foreach (var shape in Binary)
                foreach (var left in level1)
                    foreach (var right in level1)
                        level2.Add(string.Format(shape, left, right));
            var level3 = new List<string>();
            foreach (var shape in Binary)
                foreach (var left in level2.Where((_, i) => i % 13 == 0))
                    foreach (var right in level2.Where((_, i) => i % 19 == 0))
                        level3.Add(string.Format(shape, left, right));

            var parsed = new List<Entity>();
            foreach (var source in level1.Concat(level2).Concat(level3))
            {
                try { parsed.Add(source.ToEntity()); }
                catch { /* the generator makes some strings the parser declines; not its subject */ }
            }
            return parsed;
        }

        /// <summary>
        /// Which of the rules written as data have two directions, listed one by one rather than
        /// counted — a count agrees with itself after a rule changes shape and a list does not.
        /// </summary>
        [Fact]
        public void EveryDataRuleIsClassifiedAsWritten()
        {
            // Not a dictionary keyed by name: a set parameterised by a sort level exists three
            // times over, and `Power` and `Factorization` deliberately carry the same guarded
            // `a^b * c^b` -- so one name appears several times, with the same classification each
            // time. Keying by name threw, which is the enumeration having grown a shape the
            // assertion had not.
            var actual = DataRuleSets()
                .SelectMany(set => set.Rules)
                .Select(rule => (rule.Name, rule.Reversal))
                .Distinct()
                .ToList();

            // What `Distinct` must not hide is one name standing for two classifications: that
            // would list the rule twice below rather than fail, and a name meaning two things is
            // the thing the exception used to be standing in for.
            foreach (var byName in actual.GroupBy(pair => pair.Name))
                Assert.True(byName.Count() == 1,
                    $"'{byName.Key}' is classified "
                    + string.Join(" and ", byName.Select(pair => pair.Reversal))
                    + " in different sets, so the name means two things");

            var oneWay = actual.Where(pair => pair.Reversal is not RuleReversal.Reversible)
                .Select(pair => $"{pair.Name}: {pair.Reversal}")
                .Distinct()
                .OrderBy(one => one, StringComparer.Ordinal)
                .ToArray();

            // Named one by one and with the reason, so that a rule losing or gaining a direction
            // fails rather than moving a number. Almost all of these are one-way because their
            // replacement is arithmetic on a binding rather than a tree built around it -- see
            // RuleReversal.ReplacementIsCode -- and two are one-way for a mathematical reason:
            // 1 does not say which angle it came from, and S does not say which name a set
            // builder over it would bind, which is what dropping the binder's hole means.
            //
            // The second of those was ReplacementIsCode until MatchPattern.Binder let it be
            // written as data (#1074). Its reason got *better* rather than going away: it is now
            // one-way because of what it says, rather than because of how it was written.
            Assert.Equal(
                new[]
                {
                    "a-binomial-coefficient-whose-lower-index-is-the-complement-is-the-symmetric-one: ReplacementIsCode",
                    "a-chain-of-greaters-implies-its-own-ends: ReplacementIsCode",
                    "a-chain-of-lesss-implies-its-own-ends: ReplacementIsCode",
                    "a-common-factor-is-collected-out-of-a-whole-sum: ReplacementIsCode",
                    "a-common-factor-of-two-added-products-comes-out: ReplacementIsCode",
                    "a-common-factor-of-two-subtracted-products-comes-out: ReplacementIsCode",
                    "a-conditional-set-whose-condition-is-its-own-membership-is-that-set: ReplacementDropsHoles",
                    "a-conjunction-absorbs-a-disjunction-it-shares-an-operand-with: ReplacementIsCode",
                    "a-conjunction-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "a-conjunction-drops-a-negated-copy-of-its-other-operand: ReplacementIsCode",
                    "a-conjunction-of-disjunctions-sharing-an-operand-distributes: ReplacementIsCode",
                    "a-conjunction-of-negations-is-a-negated-disjunction: ReplacementIsCode",
                    "a-conjunction-with-a-falsehood-is-false: ReplacementIsCode",
                    "a-conjunction-with-itself-is-itself: ReplacementIsCode",
                    "a-constant-raised-to-a-logarithm-of-itself-is-the-antilogarithm: ReplacementDropsHoles",
                    "a-constant-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm: ReplacementDropsHoles",
                    "a-cosecant-times-a-sine-of-one-angle-is-one: ReplacementIsCode",
                    "a-difference-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "a-difference-of-even-powers-splits: ReplacementIsCode",
                    "a-difference-of-two-negatives-turns-round: ReplacementIsCode",
                    "a-difference-over-its-own-reverse-is-minus-one: ReplacementIsCode",
                    "a-difference-that-starts-from-a-term-subtracted-from-it: ReplacementIsCode",
                    "a-difference-that-takes-a-term-away-subtracted-from-it: ReplacementIsCode",
                    "a-difference-times-a-sum-of-one-pair-is-a-difference-of-squares: ReplacementIsCode",
                    "a-disjunction-absorbs-a-conjunction-it-shares-an-operand-with: ReplacementIsCode",
                    "a-disjunction-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "a-disjunction-drops-a-negated-copy-of-its-other-operand: ReplacementIsCode",
                    "a-disjunction-of-conjunctions-sharing-an-operand-distributes: ReplacementIsCode",
                    "a-disjunction-of-negations-is-a-negated-conjunction: ReplacementIsCode",
                    "a-disjunction-with-a-truth-is-true: ReplacementIsCode",
                    "a-disjunction-with-itself-is-itself: ReplacementIsCode",
                    "a-double-negation-cancels: ReplacementIsCode",
                    "a-doubled-sine-times-a-cosecant-is-twice-the-cosine: ReplacementIsCode",
                    "a-equals-with-a-number-on-the-left-turns-round: ReplacementIsCode",
                    "a-equals-with-zero-on-the-left-turns-round: ReplacementIsCode",
                    "a-factor-repeated-across-a-product-squares: ReplacementIsCode",
                    "a-factor-shared-by-a-product-and-a-quotient-added-comes-out: ReplacementIsCode",
                    "a-factor-shared-by-a-quotient-and-a-product-added-comes-out: ReplacementIsCode",
                    "a-factor-shared-by-two-added-products-comes-out: ReplacementIsCode",
                    "a-factor-shared-by-two-subtracted-products-comes-out: ReplacementIsCode",
                    "a-factor-subtracted-from-a-product-it-is-in: ReplacementIsCode",
                    "a-factorial-is-never-zero: ReplacementIsCode",
                    "a-function-times-a-number-puts-the-number-first: ReplacementIsCode",
                    "a-greater-of-a-thing-with-itself-is-decided: ReplacementIsCode",
                    "a-greater-than-or-equal-as-written-is-at-least: ReplacementIsCode",
                    "a-greater-than-or-equal-the-other-way-round-is-at-most: ReplacementIsCode",
                    "a-greater-with-a-number-on-the-left-turns-round: ReplacementIsCode",
                    "a-greater-with-zero-on-the-left-turns-round: ReplacementIsCode",
                    "a-greaterorequal-of-a-thing-with-itself-is-decided: ReplacementIsCode",
                    "a-greaterorequal-with-a-number-on-the-left-turns-round: ReplacementIsCode",
                    "a-greaterorequal-with-zero-on-the-left-turns-round: ReplacementIsCode",
                    "a-less-of-a-thing-with-itself-is-decided: ReplacementIsCode",
                    "a-less-than-or-equal-as-written-is-at-most: ReplacementIsCode",
                    "a-less-than-or-equal-the-other-way-round-is-at-least: ReplacementIsCode",
                    "a-less-with-a-number-on-the-left-turns-round: ReplacementIsCode",
                    "a-less-with-zero-on-the-left-turns-round: ReplacementIsCode",
                    "a-lessorequal-of-a-thing-with-itself-is-decided: ReplacementIsCode",
                    "a-lessorequal-with-a-number-on-the-left-turns-round: ReplacementIsCode",
                    "a-lessorequal-with-zero-on-the-left-turns-round: ReplacementIsCode",
                    "a-logarithm-in-a-reciprocal-base-negates: ReplacementIsCode",
                    "a-logarithm-of-a-perfect-power-takes-the-exponent-out: ReplacementIsCode",
                    "a-logarithm-of-a-reciprocal-in-a-reciprocal-base-turns-round-twice: ReplacementIsCode",
                    "a-logarithm-of-a-reciprocal-negates: ReplacementIsCode",
                    "a-logarithm-of-its-own-base-is-one-where-it-is-defined: ReplacementIsCode",
                    "a-modulus-takes-a-numeric-dividend-out: ReplacementIsCode",
                    "a-modulus-takes-a-numeric-divisor-out: ReplacementIsCode",
                    "a-modulus-takes-a-numeric-factor-out: ReplacementIsCode",
                    "a-negated-conjunction-becomes-a-disjunction-of-negations: ReplacementIsCode",
                    "a-negated-disjunction-becomes-a-conjunction-of-negations: ReplacementIsCode",
                    "a-negated-reciprocal-rational-factor-is-a-negated-division: ReplacementIsCode",
                    "a-negated-term-in-a-sum-is-a-subtraction: ReplacementIsCode",
                    "a-negation-or-something-is-an-implication: ReplacementIsCode",
                    "a-negative-added-is-subtracted: ReplacementIsCode",
                    "a-negative-divisor-drops-out-of-a-equals-with-zero: ReplacementIsCode",
                    "a-negative-divisor-drops-out-of-a-greater-with-zero: ReplacementIsCode",
                    "a-negative-divisor-drops-out-of-a-greaterorequal-with-zero: ReplacementIsCode",
                    "a-negative-divisor-drops-out-of-a-less-with-zero: ReplacementIsCode",
                    "a-negative-divisor-drops-out-of-a-lessorequal-with-zero: ReplacementIsCode",
                    "a-negative-factor-first-drops-out-of-a-equals-with-zero: ReplacementIsCode",
                    "a-negative-factor-first-drops-out-of-a-greater-with-zero: ReplacementIsCode",
                    "a-negative-factor-first-drops-out-of-a-greaterorequal-with-zero: ReplacementIsCode",
                    "a-negative-factor-first-drops-out-of-a-less-with-zero: ReplacementIsCode",
                    "a-negative-factor-first-drops-out-of-a-lessorequal-with-zero: ReplacementIsCode",
                    "a-negative-factor-in-a-denominator-comes-out: ReplacementIsCode",
                    "a-negative-factor-in-a-left-product-comes-out: ReplacementIsCode",
                    "a-negative-factor-in-a-numerator-comes-out: ReplacementIsCode",
                    "a-negative-factor-in-a-right-product-comes-out: ReplacementIsCode",
                    "a-negative-factor-in-a-sum-becomes-a-difference: ReplacementIsCode",
                    "a-negative-factor-second-drops-out-of-a-equals-with-zero: ReplacementIsCode",
                    "a-negative-factor-second-drops-out-of-a-greater-with-zero: ReplacementIsCode",
                    "a-negative-factor-second-drops-out-of-a-greaterorequal-with-zero: ReplacementIsCode",
                    "a-negative-factor-second-drops-out-of-a-less-with-zero: ReplacementIsCode",
                    "a-negative-factor-second-drops-out-of-a-lessorequal-with-zero: ReplacementIsCode",
                    "a-negative-integer-power-becomes-a-reciprocal: ReplacementIsCode",
                    "a-negative-minuend-comes-out-in-front: ReplacementIsCode",
                    "a-negative-subtracted-is-added: ReplacementIsCode",
                    "a-nested-radical-is-a-sum-of-two-plain-ones: ReplacementIsCode",
                    "a-number-over-a-numeric-multiple-splits: ReplacementIsCode",
                    "a-number-plus-a-function-puts-the-function-first: ReplacementIsCode",
                    "a-number-plus-a-variable-puts-the-variable-first: ReplacementIsCode",
                    "a-number-raised-to-a-logarithm-of-itself-is-the-antilogarithm: ReplacementIsCode",
                    "a-number-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm: ReplacementIsCode",
                    "a-number-times-a-binomial-coefficient-of-the-number-less-one-is-the-chairperson-identity: ReplacementIsCode",
                    "a-numeric-coefficient-is-gathered-over-a-surd: ReplacementIsCode",
                    "a-numeric-factor-comes-out-of-a-power-of-a-product: ReplacementIsCode",
                    "a-numeric-factor-floats-out-of-a-product-of-functions: ReplacementIsCode",
                    "a-numeric-quotient-of-a-numeric-multiple-collects-its-numbers: ReplacementIsCode",
                    "a-plain-factorial-times-the-next-term: ReplacementIsCode",
                    "a-positive-divisor-drops-out-of-a-equals-with-zero: ReplacementIsCode",
                    "a-positive-divisor-drops-out-of-a-greater-with-zero: ReplacementIsCode",
                    "a-positive-divisor-drops-out-of-a-greaterorequal-with-zero: ReplacementIsCode",
                    "a-positive-divisor-drops-out-of-a-less-with-zero: ReplacementIsCode",
                    "a-positive-divisor-drops-out-of-a-lessorequal-with-zero: ReplacementIsCode",
                    "a-positive-factor-first-drops-out-of-a-equals-with-zero: ReplacementIsCode",
                    "a-positive-factor-first-drops-out-of-a-greater-with-zero: ReplacementIsCode",
                    "a-positive-factor-first-drops-out-of-a-greaterorequal-with-zero: ReplacementIsCode",
                    "a-positive-factor-first-drops-out-of-a-less-with-zero: ReplacementIsCode",
                    "a-positive-factor-first-drops-out-of-a-lessorequal-with-zero: ReplacementIsCode",
                    "a-positive-factor-second-drops-out-of-a-equals-with-zero: ReplacementIsCode",
                    "a-positive-factor-second-drops-out-of-a-greater-with-zero: ReplacementIsCode",
                    "a-positive-factor-second-drops-out-of-a-greaterorequal-with-zero: ReplacementIsCode",
                    "a-positive-factor-second-drops-out-of-a-less-with-zero: ReplacementIsCode",
                    "a-positive-factor-second-drops-out-of-a-lessorequal-with-zero: ReplacementIsCode",
                    "a-power-of-a-numeric-reciprocal-times-a-power-of-its-own-denominator-subtracts-the-exponents: ReplacementIsCode",
                    "a-power-of-a-numeric-reciprocal-times-its-own-denominator-lowers-the-exponent: ReplacementIsCode",
                    "a-power-of-a-power-multiplies-the-exponents: ReplacementIsCode",
                    "a-power-over-its-own-base-lowers-the-exponent: ReplacementIsCode",
                    "a-power-times-a-product-containing-its-own-base-raises-the-exponent: ReplacementIsCode",
                    "a-power-times-its-own-base-raises-the-exponent: ReplacementIsCode",
                    "a-power-whose-exponent-divides-by-a-logarithm-of-its-own-base-changes-base: ReplacementIsCode",
                    "a-power-with-a-real-positive-exponent-is-zero-when-its-base-is: ReplacementIsCode",
                    "a-product-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "a-product-of-two-absolute-values-is-the-absolute-value-of-the-product: ReplacementIsCode",
                    "a-product-of-two-negatives-is-positive: ReplacementIsCode",
                    "a-product-of-two-quotients-is-one-quotient: ReplacementIsCode",
                    "a-product-subtracted-from-its-own-factor: ReplacementIsCode",
                    "a-quotient-by-a-cosecant-is-a-sine: ReplacementIsCode",
                    "a-quotient-by-a-secant-is-a-cosine: ReplacementIsCode",
                    "a-quotient-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "a-quotient-of-a-plain-factorial-by-a-shifted-one: ReplacementIsCode",
                    "a-quotient-of-a-shifted-factorial-by-a-plain-one: ReplacementIsCode",
                    "a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero: ReplacementIsCode",
                    "a-quotient-of-polynomials-is-divided-out: ReplacementIsCode",
                    "a-quotient-of-polynomials-is-put-in-lowest-terms: ReplacementIsCode",
                    "a-quotient-of-powers-whose-exponents-differ-by-a-whole-factor-takes-it-into-the-dividend: ReplacementIsCode",
                    "a-quotient-of-powers-whose-exponents-differ-by-a-whole-factor-takes-it-into-the-divisor: ReplacementIsCode",
                    "a-quotient-of-shifted-factorials: ReplacementIsCode",
                    "a-quotient-of-symbolic-parts-is-grouped-pairwise: ReplacementIsCode",
                    "a-quotient-of-two-absolute-values-is-the-absolute-value-of-the-quotient: ReplacementIsCode",
                    "a-quotient-of-two-negatives-is-positive: ReplacementIsCode",
                    "a-quotient-times-a-thing-keeps-the-divisor-outermost: ReplacementIsCode",
                    "a-reciprocal-is-never-zero: ReplacementIsCode",
                    "a-reciprocal-power-is-a-quotient: ReplacementIsCode",
                    "a-reciprocal-rational-factor-is-a-division: ReplacementIsCode",
                    "a-secant-times-a-cosine-of-one-angle-is-one: ReplacementIsCode",
                    "a-set-less-itself-is-empty: ReplacementIsCode",
                    "a-shared-factor-cancels-between-two-products: ReplacementIsCode",
                    "a-shared-factor-cancels-out-of-a-quotient: ReplacementIsCode",
                    "a-shifted-factorial-times-a-bare-term: ReplacementIsCode",
                    "a-shifted-factorial-times-the-next-term: ReplacementIsCode",
                    "a-sign-times-a-thing-over-its-own-absolute-value-cancels: ReplacementIsCode",
                    "a-sign-times-an-absolute-value-of-one-thing-is-that-thing: ReplacementIsCode",
                    "a-sine-times-a-cosine-of-one-angle-is-half-the-doubled-sine: ReplacementIsCode",
                    "a-square-less-a-number-splits: ReplacementIsCode",
                    "a-squared-cosecant-less-a-squared-cotangent-is-one: ReplacementIsCode",
                    "a-squared-cosine-less-a-squared-sine-is-the-doubled-cosine: ReplacementIsCode",
                    "a-squared-secant-less-a-squared-tangent-is-one: ReplacementIsCode",
                    "a-squared-sine-and-cosine-of-one-angle-sum-to-one: ReplacementIsCode",
                    "a-squared-sine-less-a-squared-cosine-turns-round: ReplacementIsCode",
                    "a-statement-differs-from-itself-nowhere: ReplacementIsCode",
                    "a-statement-implies-itself: ReplacementIsCode",
                    "a-statement-or-its-negation-is-true-where-it-has-a-truth-value: ReplacementIsCode",
                    "a-sum-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "a-sum-containing-a-term-taken-from-that-term-leaves-the-rest-negated: ReplacementIsCode",
                    "a-sum-of-two-negatives-is-a-negated-sum: ReplacementIsCode",
                    "a-sum-or-difference-that-is-a-perfect-square: ReplacementIsCode",
                    "a-sum-over-its-own-reverse-is-one: ReplacementIsCode",
                    "a-symbol-raised-to-a-logarithm-of-itself-is-the-antilogarithm-where-defined: ReplacementIsCode",
                    "a-symbol-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm-where-defined: ReplacementIsCode",
                    "a-tangent-times-a-cotangent-of-one-angle-is-one: ReplacementIsCode",
                    "a-term-added-to-a-difference-that-starts-from-it: ReplacementIsCode",
                    "a-term-added-to-a-difference-that-takes-it-away: ReplacementIsCode",
                    "a-term-added-to-a-quotient-of-itself-comes-out: ReplacementIsCode",
                    "a-term-added-to-itself-doubles: ReplacementIsCode",
                    "a-term-repeated-across-a-sum-doubles: ReplacementIsCode",
                    "a-term-shared-with-a-product-added-to-it-comes-out: ReplacementIsCode",
                    "a-term-subtracted-from-itself-vanishes: ReplacementIsCode",
                    "a-term-taken-back-out-of-a-sum-it-is-in: ReplacementIsCode",
                    "a-term-taken-from-a-difference-that-already-took-it: ReplacementIsCode",
                    "a-term-taken-from-a-difference-that-starts-from-it: ReplacementIsCode",
                    "a-term-taken-from-a-product-of-itself-comes-out: ReplacementIsCode",
                    "a-term-with-a-product-of-itself-taken-from-it-comes-out: ReplacementIsCode",
                    "a-thing-over-a-power-of-itself-is-one-power: ReplacementIsCode",
                    "a-thing-times-a-quotient-keeps-the-divisor-outermost: ReplacementIsCode",
                    "a-thing-times-itself-is-its-square: ReplacementIsCode",
                    "a-two-term-denominator-is-multiplied-by-its-conjugate: ReplacementIsCode",
                    "a-union-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "a-variable-times-a-number-puts-the-number-first: ReplacementIsCode",
                    "a-variable-times-a-power-puts-the-power-first: ReplacementIsCode",
                    "a-whole-power-comes-out-from-under-a-radical: ReplacementIsCode",
                    "an-arccosecant-of-a-numeric-reciprocal-is-an-arcsine: ReplacementIsCode",
                    "an-arccosine-of-a-numeric-reciprocal-is-an-arcsecant: ReplacementIsCode",
                    "an-arcsecant-of-a-numeric-reciprocal-is-an-arccosine: ReplacementIsCode",
                    "an-arcsine-of-a-numeric-reciprocal-is-an-arccosecant: ReplacementIsCode",
                    "an-equality-or-a-greater-than-as-written-is-at-least: ReplacementIsCode",
                    "an-equality-or-a-greater-than-the-other-way-round-is-at-most: ReplacementIsCode",
                    "an-equality-or-a-less-than-as-written-is-at-most: ReplacementIsCode",
                    "an-equality-or-a-less-than-the-other-way-round-is-at-least: ReplacementIsCode",
                    "an-even-function-of-a-negative-multiple-drops-the-sign-abs: ReplacementIsCode",
                    "an-even-function-of-a-negative-multiple-drops-the-sign-cos: ReplacementIsCode",
                    "an-even-function-of-a-negative-multiple-drops-the-sign-secant: ReplacementIsCode",
                    "an-exclusive-disjunction-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "an-exponent-comes-out-of-a-logarithm: ReplacementIsCode",
                    "an-implication-between-negations-turns-round: ReplacementIsCode",
                    "an-intersection-chain-is-sorted-and-grouped: ReplacementIsCode",
                    "an-intersection-distributes-over-a-union-on-its-left: ReplacementIsCode",
                    "an-intersection-distributes-over-a-union-on-its-right: ReplacementIsCode",
                    "an-odd-function-of-a-negative-multiple-negates-cosecant: ReplacementIsCode",
                    "an-odd-function-of-a-negative-multiple-negates-cotan: ReplacementIsCode",
                    "an-odd-function-of-a-negative-multiple-negates-signum: ReplacementIsCode",
                    "an-odd-function-of-a-negative-multiple-negates-sin: ReplacementIsCode",
                    "an-odd-function-of-a-negative-multiple-negates-tan: ReplacementIsCode",
                    "an-unbounded-interval-is-a-whole-domain: ReplacementIsCode",
                    "anything-follows-from-a-falsehood: ReplacementIsCode",
                    "arccosine-of-a-cosine-inside-its-own-interval: ReplacementIsCode",
                    "arccotangent-of-a-cotangent-inside-its-own-range: ReplacementIsCode",
                    "arcsine-of-a-sine-inside-its-own-interval: ReplacementIsCode",
                    "arcsine-plus-arccosine-is-a-right-angle: ReplacementIsCode",
                    "arctangent-of-a-tangent-inside-its-own-interval: ReplacementIsCode",
                    "arctangent-plus-arccotangent-is-a-right-angle-with-the-sign-of-the-argument: ReplacementIsCode",
                    "cosine-of-a-whole-multiple-of-an-angle: ReplacementIsCode",
                    "dividing-by-a-power-and-then-by-its-base-raises-the-exponent: ReplacementIsCode",
                    "dividing-by-a-quotient-multiplies-by-its-reciprocal: ReplacementIsCode",
                    "dividing-by-a-thing-and-then-by-a-power-of-it-raises-the-exponent: ReplacementIsCode",
                    "dividing-by-two-powers-of-one-base-adds-the-exponents: ReplacementIsCode",
                    "dividing-twice-by-one-thing-squares-it: ReplacementIsCode",
                    "dividing-twice-divides-by-the-product: ReplacementIsCode",
                    "eulers-totient-of-a-prime-power: ReplacementIsCode",
                    "membership-of-a-singleton-is-an-equality: ReplacementIsCode",
                    "membership-of-an-interval-is-written-out: ReplacementIsCode",
                    "one-and-a-squared-cotangent-make-a-squared-cosecant: ReplacementIsCode",
                    "one-and-a-squared-tangent-make-a-squared-secant: ReplacementIsCode",
                    "one-and-not-the-other-either-way-round-is-an-exclusive-disjunction: ReplacementIsCode",
                    "one-less-a-squared-cosine-is-a-squared-sine: ReplacementIsCode",
                    "one-less-a-squared-sine-is-a-squared-cosine: ReplacementIsCode",
                    "sine-of-a-whole-multiple-of-an-angle: ReplacementIsCode",
                    "squared-sine-and-cosine-of-one-argument-sum-to-one: ReplacementDropsHoles",
                    "squared-sine-and-cosine-with-one-coefficient-sum-to-it: ReplacementDropsHoles",
                    "the-arctangent-of-one-over-root-three: ReplacementIsCode",
                    "the-arctangent-of-root-three: ReplacementIsCode",
                    "the-negation-of-a-greater-turns-it-round: ReplacementIsCode",
                    "the-negation-of-a-greaterorequal-turns-it-round: ReplacementIsCode",
                    "the-negation-of-a-less-turns-it-round: ReplacementIsCode",
                    "the-negation-of-a-lessorequal-turns-it-round: ReplacementIsCode",
                    "the-two-truth-values-are-the-boolean-domain: ReplacementIsCode",
                    "two-added-fractions-take-a-common-denominator: ReplacementIsCode",
                    "two-arctangents-of-numbers-add-by-the-tangent-formula: ReplacementIsCode",
                    "two-binomial-coefficients-of-one-upper-index-and-adjacent-lower-indices-add-by-pascals-rule: ReplacementIsCode",
                    "two-comparisons-of-one-pair-that-exclude-each-other-are-false: ReplacementIsCode",
                    "two-comparisons-of-one-pair-that-leave-no-case-are-true: ReplacementIsCode",
                    "two-functions-in-a-sum-come-together: ReplacementIsCode",
                    "two-logarithms-of-one-base-add-by-multiplying-their-antilogarithms: ReplacementIsCode",
                    "two-logarithms-of-one-base-subtract-by-dividing-their-antilogarithms: ReplacementIsCode",
                    "two-numbers-around-a-factor-collect: ReplacementIsCode",
                    "two-numeric-factors-around-a-function-collect: ReplacementIsCode",
                    "two-numeric-factors-around-a-variable-collect: ReplacementIsCode",
                    "two-numeric-multiples-of-functions-collect-their-numbers: ReplacementIsCode",
                    "two-numeric-multiples-of-one-variable-add: ReplacementIsCode",
                    "two-numeric-multiples-of-one-variable-subtract: ReplacementIsCode",
                    "two-numeric-terms-around-a-variable-collect: ReplacementIsCode",
                    "two-powers-of-one-base-divide-by-subtracting-exponents: ReplacementIsCode",
                    "two-powers-of-one-base-multiply-by-adding-exponents: ReplacementIsCode",
                    "two-powers-of-one-exponent-share-a-base: ReplacementIsCode",
                    "two-powers-of-one-exponent-share-a-quotient-of-bases: ReplacementIsCode",
                    "two-subtracted-fractions-take-a-common-denominator: ReplacementIsCode",
                },
                oneWay);
        }

        /// <summary>
        /// <b>The question no existing API answers.</b> Every entry point in the library runs
        /// forwards: <c>Simplify</c>, <c>Expand</c> and <c>Factorize</c> all take an expression to
        /// another one. This asks the other way — given an expression, which expression does one
        /// rule of this set turn into it.
        /// </summary>
        /// <remarks>
        /// <c>k*p + k*q -&gt; k*(p + q)</c> read backwards is the distributive law, and reading it
        /// backwards is the only way the library has of knowing that the two are the same fact.
        /// The forward rule is written four times in <c>Patterns.CommonRules</c> and nine times in
        /// <c>Patterns.FactorizeRules</c>, none of which knows about the expansion that undoes it.
        /// </remarks>
        [Fact]
        public void AReversedRuleSaysWhatAnExpressionCameFrom()
        {
            var factoring = MatchedRules.SharedFactor.Rules.Single();
            Assert.Equal("x * (a + b)".ToEntity(), factoring.TryApply("x * a + x * b".ToEntity()));

            var expanding = factoring.Reversed;
            Assert.NotNull(expanding);
            Assert.Equal("x * a + x * b".ToEntity(), expanding!.TryApply("x * (a + b)".ToEntity()));

            // And the set, so that the question can be asked of a whole set of rules at once.
            Assert.Equal(
                "x * a + x * b".ToEntity(),
                MatchedRules.SharedFactor.Reversed.ApplyHere("x * (a + b)".ToEntity()));
        }

        /// <summary>
        /// A rule that throws information away has no backwards reading, and the type says so
        /// rather than a comment saying so.
        /// </summary>
        [Fact]
        public void ARuleThatForgetsAHoleHasNoBackwardsReading()
        {
            // Both Pythagorean rules of the set, with a coefficient and without, drop the
            // argument. The set's other two, 1 + tan^2 = sec^2 and its cotangent twin, keep it,
            // and they are the only ones its reversal holds.
            var pythagoras = MatchedRules.PythagoreanIdentity.Rules
                .Where(rule => rule.Name.StartsWith("squared-sine-and-cosine", StringComparison.Ordinal))
                .ToList();
            Assert.Equal(2, pythagoras.Count);
            foreach (var rule in pythagoras)
            {
                Assert.Equal(RuleReversal.ReplacementDropsHoles, rule.Reversal);
                Assert.Null(rule.Reversed);
            }

            // And what the reversed set holds is the reversals of the others only, rather than
            // anything standing for the two that have none.
            Assert.Equal(
                MatchedRules.PythagoreanIdentity.Rules.Count(rule => rule.Reversed is not null),
                MatchedRules.PythagoreanIdentity.Reversed.Rules.Count);
            Assert.Equal(
                "1 + a".ToEntity(),
                MatchedRules.PythagoreanIdentity.Reversed.ApplyHere("1 + a".ToEntity()));
        }

        /// <summary>
        /// The reversal of a reversal is the rule it came from — not merely classified the same
        /// way, but rewriting every expression in the corpus to the same thing.
        /// </summary>
        [Fact]
        public void ReversingTwiceGivesTheRuleBack()
        {
            var corpus = Corpus();
            Assert.True(corpus.Count > 500, $"the corpus is only {corpus.Count} expressions");

            foreach (var rule in DataRuleSets().SelectMany(set => set.Rules))
            {
                if (rule.Reversed is not { } once) continue;
                var twice = once.Reversed;
                Assert.NotNull(twice);
                foreach (var expr in corpus)
                    Assert.Equal(rule.TryApply(expr), twice!.TryApply(expr));
            }
        }

        /// <summary>
        /// A reversed rule undoes the rule it came from, <b>by value</b>. Not by shape: reversing
        /// a rule whose pattern is commutative writes the operands in the order the rule was
        /// written in, so <c>a*x + b*x</c> comes back as <c>x*a + x*b</c>, which is the same
        /// number written differently and would fail a comparison of trees.
        /// </summary>
        [Fact]
        public void AReversedRuleUndoesTheRuleItCameFrom()
        {
            var corpus = Corpus();
            var checkedPairs = 0;
            var rewritten = 0;
            var failures = new List<string>();

            foreach (var rule in DataRuleSets().SelectMany(set => set.Rules))
            {
                if (rule.Reversed is not { } backwards) continue;
                foreach (var expr in corpus)
                {
                    if (rule.TryApply(expr) is not { } forward) continue;
                    if (backwards.TryApply(forward) is not { } back) continue;
                    checkedPairs++;
                    // The tree came back unchanged, which is the strongest answer there is and
                    // needs no arithmetic -- and asking for arithmetic anyway would fail on the
                    // corpus's expressions that have no value, such as anything over a literal 0.
                    if (expr.Equals(back)) continue;
                    rewritten++;
                    if ((expr - back).Simplify() != Integer.Zero)
                        failures.Add($"{rule.Name}: {expr.Stringize()} -> {forward.Stringize()} "
                            + $"-> {back.Stringize()}");
                }
            }

            Assert.True(checkedPairs > 100, $"only {checkedPairs} round trips were available");
            // Without one of these the value check is a check of nothing: every round trip
            // returning the same tree would pass it without ever comparing two numbers.
            Assert.True(rewritten > 0, "no round trip came back written differently");
            Assert.True(failures.Count == 0,
                $"{failures.Count} of {rewritten} rewritten round trips changed the value:\n"
                + string.Join("\n", failures.Take(10)));
        }

        /// <summary>
        /// <b>A constraint on a hole is written once and holds in both directions.</b>
        /// <c>(a/b)^c = a^c/b^c</c> needs a positive whole <c>c</c>, and the forward rule is where
        /// that is written. Read backwards the rule matches <c>a^c/b^c</c> for any <c>c</c> at
        /// all — and then refuses to build the quotient-to-a-power unless <c>c</c> passes the
        /// constraint on the side that states it.
        /// </summary>
        [Theory]
        [InlineData("x ^ 2 / y ^ 2", "(x / y) ^ 2")]
        [InlineData("x ^ 3 / y ^ 3", "(x / y) ^ 3")]
        [InlineData("x ^ n / y ^ n", null)]          // n is not known to be a positive integer
        [InlineData("x ^ (-2) / y ^ (-2)", null)]    // negative, so the forward rule refuses it
        [InlineData("x ^ (1/2) / y ^ (1/2)", null)]  // and this one is the branch-cut case
        public void AHoleConstraintSurvivesBeingReadBackwards(string from, string? expected)
        {
            var rule = MatchedRules.CollapseMultipleFractions.Rules
                .Single(one => one.Name == "positive-power-of-a-quotient-distributes");
            var backwards = rule.Reversed;
            Assert.NotNull(backwards);

            var actual = backwards!.TryApply(from.ToEntity());
            if (expected is null)
                Assert.Null(actual);
            else
                Assert.Equal(expected.ToEntity(), actual);
        }

        /// <summary>
        /// A pattern over a node this cannot construct is matchable and not writable, so a rule
        /// matching one is one-way — reported as its own reason rather than folded into the
        /// others, because it is a gap in the mechanism where the rest are facts about the
        /// mathematics.
        /// </summary>
        /// <remarks>
        /// The unbuildable node is on the <b>left</b>, which is the case this is about: the rule
        /// fires, and only its reverse is impossible. An unbuildable node on the right is a
        /// different thing entirely and is rejected outright — see
        /// <see cref="AReplacementCannotNameANodeThisCannotBuild"/>.
        /// </remarks>
        [Fact]
        public void APatternOverAnUnbuildableNodeIsMatchableAndNotReversible()
        {
            // Integralf carries an optional integration range beside its two children, so no
            // construction from children alone can rebuild it -- unbuildable for a reason that
            // will not change, where Modf, which stood here before, was merely a type Construct
            // had not been given a line for yet.
            var overIntegral = new MatchedRule(
                "integral",
                MatchPattern.Node<Integralf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                MatchPattern.Node<Mulf>(MatchPattern.Any("b"), MatchPattern.Any("a")),
                Soundness.Heuristic);

            // It still matches, which is what "matchable and not writable" means.
            var integral = MathS.Integral("x".ToEntity(), "y".ToEntity());
            Assert.True(overIntegral.Left.Matches(integral));
            Assert.Equal("y * x", overIntegral.TryApply(integral)!.Stringize());
            Assert.Equal(RuleReversal.PatternCannotBeBuilt, overIntegral.Reversal);
            Assert.Null(overIntegral.Reversed);
        }

        /// <summary>
        /// A replacement naming a node type the matcher can match but not construct is not a
        /// one-way rule — it is a rule that <b>never fires</b>, since the match succeeds and the
        /// build then returns nothing, which <c>TryApply</c> reports as "did not apply".
        /// </summary>
        /// <remarks>
        /// Indistinguishable at run time from a correct rule on an expression it declines, so
        /// nothing downstream can catch it. It cost an afternoon and a twenty-eight-row agreement
        /// failure — a set of four rules building a <c>Cosecantf</c>, which was not in
        /// <c>MatchPattern.Construct</c> — before the constructor was made to say so.
        /// </remarks>
        [Fact]
        public void AReplacementCannotNameANodeThisCannotBuild()
        {
            var thrown = Assert.Throws<ArgumentException>(() => new MatchedRule(
                "unbuildable",
                MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                MatchPattern.Node<Integralf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                Soundness.Heuristic));
            Assert.Contains("cannot build", thrown.Message);
        }

        /// <summary>
        /// A replacement naming a hole the pattern never binds is a typo, and a typo that would
        /// otherwise show as a rule that silently never fires. Only a right-hand side written as
        /// data can be checked for it at all — a builder over the bindings throws at run time on
        /// the first expression that reaches it, or not at all.
        /// </summary>
        [Fact]
        public void AReplacementCannotNameAHoleThePatternDoesNotBind()
        {
            var thrown = Assert.Throws<ArgumentException>(() => new MatchedRule(
                "typo",
                MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c")),
                Soundness.Sound));
            Assert.Contains("'c'", thrown.Message);
        }

        /// <summary>
        /// Both sides of every rule written as data can be built, so the node types the rules use
        /// are all ones <c>MatchPattern.Construct</c> knows. A rule that adds a node type to a
        /// right-hand side and not to that method fails here rather than becoming quietly one-way.
        /// </summary>
        [Fact]
        public void EveryDataRuleIsBuildableOnBothSides()
        {
            foreach (var set in DataRuleSets())
                foreach (var rule in set.Rules)
                {
                    // A rule whose replacement is code has no right-hand pattern to check, and
                    // its left need not be buildable either: it is one-way whatever the nodes.
                    if (rule.Right is null)
                    {
                        Assert.Equal(RuleReversal.ReplacementIsCode, rule.Reversal);
                        continue;
                    }
                    Assert.True(rule.Right.IsBuildable, $"{set.Name}/{rule.Name}: right");
                    // The left decides only whether it reads backwards. Stated as the two
                    // implications rather than as an equivalence: `Classify` returns the *first*
                    // reason a rule is one-way, so a rule that both drops a hole and has an
                    // unbuildable left is reported as dropping the hole, and reading
                    // PatternCannotBeBuilt as "exactly the unbuildable ones" fails on it.
                    // MatchPattern.Binder made that shape real -- { x : x in S } = S has a
                    // ConditionalSet pattern, which is not buildable, and drops the bound name.
                    if (rule.Reversal is RuleReversal.Reversible)
                        Assert.True(rule.Left.IsBuildable, $"{set.Name}/{rule.Name}: left");
                    if (rule.Reversal is RuleReversal.PatternCannotBeBuilt)
                        Assert.False(rule.Left.IsBuildable, $"{set.Name}/{rule.Name}: left");
                }
        }

        /// <summary>
        /// Reversal carries the side condition and the soundness tier over unchanged. Both follow
        /// from what a rewrite rule claims: the condition is a predicate on the bindings and both
        /// directions produce the same bindings, and an equality is symmetric.
        /// </summary>
        [Fact]
        public void ReversalCarriesTheConditionAndTheTier()
        {
            var powerOfPower = MatchedRules.PowerOfPower.Rules.Single();
            var backwards = powerOfPower.Reversed;
            Assert.NotNull(backwards);
            Assert.Equal(powerOfPower.Soundness, backwards!.Soundness);

            // (a^b)^c = a^(b*c) needs a whole c or a positive real a. Read backwards the same
            // condition decides, so a^(b*c) is only rewritten to (a^b)^c where it is true.
            Assert.Equal("(x ^ y) ^ 2".ToEntity(), backwards.TryApply("x ^ (y * 2)".ToEntity()));
            Assert.Null(backwards.TryApply("x ^ (y * z)".ToEntity()));
        }
    }
}
