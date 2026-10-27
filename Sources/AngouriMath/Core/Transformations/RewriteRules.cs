//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using AngouriMath.Functions;

namespace AngouriMath.Core.Transformations
{
    /// <summary>
    /// The rewrite rule sets this library ships, as data: named, described, attributed with
    /// what they claim, and enumerable through <see cref="All"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Registration is explicit and static — there is no assembly scanning and no
    /// <c>Activator</c>, so the registry survives trimming and NativeAOT, and
    /// <see cref="All"/> is in a fixed order that does not depend on hashing, reflection or
    /// which type happened to be loaded first.
    /// </para>
    /// <para>
    /// Every rule set the simplifier applies is registered here, and the simplifier reaches
    /// them through this registry rather than through <c>Patterns</c> directly. That is what
    /// lets an account of what the simplifier did to an expression be a complete one: a set
    /// reachable only by its method has no name to report and nothing to attribute a step to,
    /// so a derivation built while some sets were still unregistered would quietly omit
    /// whatever they had done.
    /// </para>
    /// <para>
    /// Every entry is declared <see cref="Soundness.SoundUnderAssumptions"/>. That is a claim
    /// about what has been argued, not about what is true — nothing here checks a tier, so
    /// the registry starts conservative and promoting an entry means making the case for it.
    /// </para>
    /// </remarks>
    public static class RewriteRules
    {
        #region Arrangement

        /// <summary>
        /// Puts the operands of commutative chains into a canonical order and groups equal
        /// ones together, so that <c>x + y</c> and <c>y + x</c> stop being different trees.
        /// Looks at variables and functions only, ignoring constants and operators.
        /// </summary>
        public static RewriteRuleSet CanonicalOrder { get; } = new(
            nameof(CanonicalOrder),
            "Sorts and groups the operands of sums, products, conjunctions, disjunctions and set operations, by variables and functions alone.",
            TransformationRelation.Equivalence,
            // Regrouping reads a quotient as a product with a negative power, which is the
            // same value wherever the divisor is not zero.
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Sort(TreeAnalyzer.SortLevel.HIGH_LEVEL),
            isNormalization: true);

        /// <summary>
        /// <see cref="CanonicalOrder"/>, counting constants as well, so that terms differing
        /// only by a numeric factor are no longer grouped together.
        /// </summary>
        public static RewriteRuleSet CanonicalOrderCountingConstants { get; } = new(
            nameof(CanonicalOrderCountingConstants),
            "Sorts and groups commutative operands, distinguishing terms by their constants too.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Sort(TreeAnalyzer.SortLevel.MIDDLE_LEVEL),
            isNormalization: true);

        /// <summary>
        /// <see cref="CanonicalOrder"/> over the whole subtree, so that only structurally
        /// identical operands are grouped.
        /// </summary>
        public static RewriteRuleSet CanonicalOrderExact { get; } = new(
            nameof(CanonicalOrderExact),
            "Sorts and groups commutative operands by the whole subtree.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Sort(TreeAnalyzer.SortLevel.LOW_LEVEL),
            isNormalization: true);

        /// <summary>
        /// Turns a negative power into a quotient: <c>a * b ^ (-1)</c> becomes <c>a / b</c>.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.InvertNegativePowers"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet InvertNegativePowers { get; } = new(
            nameof(InvertNegativePowers),
            "Rewrites negative powers as quotients.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.InvertNegativePowers);

        /// <summary>
        /// Brings a negative numeric factor out in front of the term it multiplies.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.InvertNegativeMultipliers"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet InvertNegativeMultipliers { get; } = new(
            nameof(InvertNegativeMultipliers),
            "Moves a negative numeric factor out of a product into the sign of the term.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.InvertNegativeMultipliers);

        /// <summary>
        /// The arithmetic housekeeping rules — collecting like terms, flattening nested
        /// quotients, moving numeric coefficients to the front.
        /// </summary>
        public static RewriteRuleSet Common { get; } = new(
            nameof(Common),
            "Collects like terms and normalises the arrangement of products and quotients.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Common);

        /// <summary>
        /// Gets a quotient into the shape the division rules expect before they run.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.DivisionPreparing"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and what still describes it.
        /// </remarks>
        public static RewriteRuleSet DivisionPreparing { get; } = new(
            nameof(DivisionPreparing),
            "Lifts numeric factors out of a quotient so that the division rules can see it.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.DivisionPreparing);

        /// <summary>
        /// Cosmetic arrangement of signs, so that adding a negative reads as a difference.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.NumericNeat"/>, where its sixteen arms are eleven
        /// rules: six of them are three rules written twice over, once for each side a
        /// negative factor can sit on, and a commutative pattern says each once.
        /// </remarks>
        public static RewriteRuleSet NumericNeat { get; } = new(
            nameof(NumericNeat),
            "Arranges signs so that adding a negative is written as subtracting a positive.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.NumericNeat);

        #endregion

        #region Powers, products and sums

        /// <summary>
        /// Rules about powers, roots and logarithms.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.Power"/>, where its thirty-five arms are thirty
        /// rules. The largest set converted, and the one whose branch-cut conditions are asked
        /// of <c>Patterns.Power.cs</c> rather than restated: five issues put a guard on one
        /// rewrite here, and a second copy of one of those guards is how the two come to
        /// disagree.
        /// </remarks>
        public static RewriteRuleSet Power { get; } = new(
            nameof(Power),
            "Gathers and splits powers, roots and logarithms.",
            TransformationRelation.Equivalence,
            // (a ^ b) ^ c is a ^ (b c) only on a branch; the rules guard for it, and the
            // guard is what the tier is stating.
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Power);

        /// <summary>
        /// Multiplies products over sums out.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.Expansion"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet Expansion { get; } = new(
            nameof(Expansion),
            "Distributes products and powers over sums.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Expansion);

        /// <summary>
        /// Takes common factors back out of a sum.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.Factorization"/>, where its twenty-two arms are
        /// eleven rules. Taking a common factor out is written four times for a sum and four
        /// for a difference, and a commutative pattern says each once.
        /// </remarks>
        public static RewriteRuleSet Factorization { get; } = new(
            nameof(Factorization),
            "Gathers common factors out of sums.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Factorization);

        /// <summary>
        /// Recognises a perfect square written out, so that factorisation has something to
        /// gather.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.PerfectSquare"/>, whose arm is the one that was
        /// recorded as needing an alternation of node types the matcher does not have. It
        /// needed a predicate on a hole, which it does.
        /// </remarks>
        public static RewriteRuleSet PerfectSquare { get; } = new(
            nameof(PerfectSquare),
            "Collapses a written-out perfect square into a squared binomial.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.PerfectSquare);

        #endregion

        #region Quotients

        /// <summary>
        /// Clears a surd out of a two-term denominator.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher, and <i>listed</i> by it</b> —
        /// <see cref="Matching.MatchedRules.RationalizeDenominator"/>. This set is an
        /// ordinary method with branches and locals, which <c>RuleRegistryGenerator</c>
        /// declines, so it was the one set in the registry with no addressable rules at all.
        /// Its rules are read from the data form instead, which is the other half of
        /// <a href="https://github.com/asc-community/AngouriMath/issues/825">#825</a>.
        /// </remarks>
        public static RewriteRuleSet RationalizeDenominator { get; } = new(
            nameof(RationalizeDenominator),
            "Multiplies a quotient by the conjugate of its denominator to clear a surd from it.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.RationalizeDenominator);

        /// <summary>
        /// Brings a quotient of quotients down to a single one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.CollapseMultipleFractions"/>. This and
        /// <see cref="DivisionPreparing"/> are the two sets whose data form is proven to agree
        /// with the <c>switch</c> it mirrors over generated expressions
        /// (<c>MatchedRulesAgreeWithTheSwitchTest</c>), which is the precondition for running one
        /// instead of the other.
        /// </para>
        /// <para>
        /// <b>What it costs.</b> Measured against <c>Simplify</c> itself, both arms in
        /// one process with a third arm that is the <c>switch</c> again as a control: the data form
        /// is −0.6% where the control differs from its own source by −1.1%, so the change is
        /// smaller than this machine's disagreement with itself, and allocation is +0.04%. That
        /// number used to be +5% for <see cref="DivisionPreparing"/> alone, and what closed it was
        /// settling a pattern's determinism once rather than on every attempt
        /// (<a href="https://github.com/asc-community/AngouriMath/pull/1050">#1050</a>).
        /// </para>
        /// <para>
        /// <b>The <c>switch</c> is still what describes them.</b> <see cref="RewriteRule"/> carries
        /// a <see cref="RewriteRule.PatternSource"/> and a <see cref="RewriteRule.SourceLine"/>,
        /// which <c>RuleRegistryGenerator</c> reads off the arms of a <c>switch</c>; it has no way
        /// yet to read a rule written as data. So the addressable rules of these two sets still come
        /// from <c>Patterns</c>, and the arms they describe are the ones the agreement test holds
        /// the matcher to rather than dead code. Teaching the generator to read
        /// <see cref="Matching.MatchedRules"/> is what would let the <c>switch</c> go, and is
        /// <a href="https://github.com/asc-community/AngouriMath/issues/825">#825</a>.
        /// </para>
        /// </remarks>
        public static RewriteRuleSet CollapseMultipleFractions { get; } = new(
            nameof(CollapseMultipleFractions),
            "Collapses nested quotients into a single numerator over a single denominator.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.CollapseMultipleFractions);

        /// <summary>
        /// Puts a sum of quotients over one denominator, grouping the terms by variables and
        /// functions alone.
        /// </summary>
        public static RewriteRuleSet CommonDenominator { get; } = new(
            nameof(CommonDenominator),
            "Adds quotients by putting them over a common denominator.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.CommonDenominator(TreeAnalyzer.SortLevel.HIGH_LEVEL));

        /// <summary>
        /// <see cref="CommonDenominator"/>, counting constants when it groups terms.
        /// </summary>
        public static RewriteRuleSet CommonDenominatorCountingConstants { get; } = new(
            nameof(CommonDenominatorCountingConstants),
            "Adds quotients over a common denominator, distinguishing terms by their constants too.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.CommonDenominator(TreeAnalyzer.SortLevel.MIDDLE_LEVEL));

        /// <summary>
        /// <see cref="CommonDenominator"/>, grouping terms by the whole subtree.
        /// </summary>
        public static RewriteRuleSet CommonDenominatorExact { get; } = new(
            nameof(CommonDenominatorExact),
            "Adds quotients over a common denominator, grouping terms by the whole subtree.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.CommonDenominator(TreeAnalyzer.SortLevel.LOW_LEVEL));

        /// <summary>
        /// Divides one polynomial by another, leaving a quotient plus a remainder.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.PolynomialLongDivision"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet PolynomialLongDivision { get; } = new(
            nameof(PolynomialLongDivision),
            "Divides a polynomial by a polynomial, giving the quotient plus the remainder.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.PolynomialLongDivision);

        /// <summary>
        /// Puts a quotient of polynomials into lowest terms.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.PolynomialGcdCancellation"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet PolynomialGcdCancellation { get; } = new(
            nameof(PolynomialGcdCancellation),
            "Cancels the greatest common divisor of a polynomial quotient's numerator and denominator.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.PolynomialGcdCancellation);

        /// <summary>
        /// The Pythagorean identity found anywhere in a sum, by the gathered matcher.
        /// </summary>
        /// <remarks>
        /// <see cref="Trigonometric"/> knows <c>sin(a)^2 + cos(a)^2</c> as a node of two
        /// operands, and the simplifier sorts a sum so that the pair lands adjacent -- which
        /// puts them in one node only when nothing sorts <i>before</i> them: <c>sqrt(2) + sin(t)^2
        /// + cos(t)^2</c> is <c>(sqrt(2) + cos(t)^2) + sin(t)^2</c>, and stayed so, while
        /// <c>a + sin(t)^2 + cos(t)^2</c>, whose <c>a</c> sorts last, became <c>1 + a</c>. This
        /// set is the n-ary reading of the same identity,
        /// <see cref="Matching.MatchedRules.PythagoreanIdentity"/>, which finds the two squares
        /// among any number of operands. Run beside <see cref="Trigonometric"/> by the simplifier
        /// rather than listed in <see cref="All"/>: it is one identity the trigonometric set
        /// already claims, in the shape the matcher can see.
        /// https://github.com/asc-community/AngouriMath/issues/725
        /// </remarks>
        internal static RewriteRuleSet PythagoreanIdentity { get; } = new(
            nameof(PythagoreanIdentity),
            "Replaces a squared sine and a squared cosine of one argument, anywhere in a sum, by one.",
            TransformationRelation.Equivalence,
            Soundness.Sound,
            Matching.MatchedRules.PythagoreanIdentity);

        #endregion

        #region Trigonometry

        /// <summary>
        /// The trigonometric identities.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.Trigonometric"/>, where its forty-three arms are
        /// thirty-three rules, and where the interval conditions of #884 and #887 are
        /// attached to the rules that need them rather than to the set.
        /// </remarks>
        public static RewriteRuleSet Trigonometric { get; } = new(
            nameof(Trigonometric),
            "Applies trigonometric identities to sines, cosines and their relatives.",
            TransformationRelation.Equivalence,
            // tan and cot bring poles with them, so an identity that introduces one holds
            // away from those points rather than everywhere.
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Trigonometric);

        /// <summary>
        /// Rewrites the derived trigonometric functions in terms of sine and cosine.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.NormalTrigonometricForm"/>, whose four rules are the
        /// first here that all read backwards. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet NormalTrigonometricForm { get; } = new(
            nameof(NormalTrigonometricForm),
            "Writes tangents, cotangents, secants and cosecants as sines and cosines.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.NormalTrigonometricForm);

        /// <summary>
        /// Gathers sines and cosines back into the derived functions where that is shorter.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.CollapseTrigonometricFunctions"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet CollapseTrigonometricFunctions { get; } = new(
            nameof(CollapseTrigonometricFunctions),
            "Recognises a quotient or reciprocal of sines and cosines as a tangent, cotangent, secant or cosecant.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.CollapseTrigonometricFunctions);

        /// <summary>
        /// Opens a trigonometric function of a sum into functions of its terms.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.ExpandTrigonometric"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet ExpandTrigonometric { get; } = new(
            nameof(ExpandTrigonometric),
            "Expands a sine or cosine of a sum into products of sines and cosines of its terms.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.ExpandTrigonometric);

        /// <summary>
        /// Opens a trigonometric function of a multiplied angle.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Written out, <c>sin(4x)</c> is far longer than it started, which is why the
        /// simplifier offers the result as a candidate rather than taking it.
        /// </para>
        /// <para>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.ExpandMultipleAngle"/>. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </para>
        /// </remarks>
        public static RewriteRuleSet ExpandMultipleAngle { get; } = new(
            nameof(ExpandMultipleAngle),
            "Expands a sine or cosine of an integer multiple of an angle.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.ExpandMultipleAngle);

        #endregion

        #region Statements, sets and number theory

        /// <summary>
        /// The rules of boolean algebra.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.Boolean"/>, where its thirty-six arms are sixteen
        /// rules. Distributivity is written eight times in the <c>switch</c> and absorption
        /// another eight, and a commutative pattern at both levels says each once — which
        /// also completes three orientations of absorption the arms never wrote out.
        /// </remarks>
        public static RewriteRuleSet Boolean { get; } = new(
            nameof(Boolean),
            "Applies the identities of boolean algebra to conjunctions, disjunctions and negations.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.Boolean);

        /// <summary>
        /// Rules about equalities and inequalities.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.InequalityEquality"/>, where its sixty-five arms are
        /// sixty-five rules: this set writes out shapes rather than orientations, so there is
        /// nothing for a commutative pattern to collapse. What it gains instead is that the two
        /// De Morgan folds and the two excluded-middle rules say what they need of the bindings,
        /// which a <c>switch</c> arm says in a guard beside the shape.
        /// </remarks>
        public static RewriteRuleSet InequalityEquality { get; } = new(
            nameof(InequalityEquality),
            "Rearranges equalities and inequalities into their usual form.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.InequalityEquality);

        /// <summary>
        /// Rules about unions, intersections and set differences.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.SetOperator"/>, and the set that found the
        /// matcher's first real limit: a pattern cannot reach inside a binder, because a
        /// <c>ConditionalSet</c> offers its predicate and not its bound variable to a
        /// traversal (<a href="https://github.com/asc-community/AngouriMath/issues/1074">#1074</a>).
        /// </remarks>
        public static RewriteRuleSet SetOperator { get; } = new(
            nameof(SetOperator),
            "Applies the identities of set algebra to unions, intersections and set differences.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.SetOperator);

        /// <summary>
        /// Cancels a quotient of factorials down to the terms that survive.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.ExpandFactorialDivisions"/>, where its eight arms are three rules:
        /// four of the eight are one rule written for every way a sum can be spelled, and a
        /// commutative pattern says that once.
        /// </remarks>
        public static RewriteRuleSet ExpandFactorialDivisions { get; } = new(
            nameof(ExpandFactorialDivisions),
            "Cancels a quotient of factorials into the product of the terms that do not cancel.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.ExpandFactorialDivisions);

        /// <summary>
        /// Recognises a product of consecutive terms as a factorial.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.FactorizeFactorialMultiplications"/>, where its eight arms are three rules:
        /// four of the eight are one rule written for every way a sum can be spelled, and a
        /// commutative pattern says that once.
        /// </remarks>
        public static RewriteRuleSet FactorizeFactorialMultiplications { get; } = new(
            nameof(FactorizeFactorialMultiplications),
            "Gathers a product of a factorial and its neighbouring terms back into one factorial.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.FactorizeFactorialMultiplications);

        /// <summary>
        /// Rules about Euler's totient function.
        /// </summary>
        /// <remarks>
        /// <b>Run by the matcher rather than by the <c>switch</c></b> —
        /// <see cref="Matching.MatchedRules.PhiFunction"/>, whose single rule carries primality
        /// as a predicate on the hole it binds. See the note on
        /// <see cref="CollapseMultipleFractions"/> for what that costs and why the <c>switch</c>
        /// stays.
        /// </remarks>
        public static RewriteRuleSet PhiFunction { get; } = new(
            nameof(PhiFunction),
            "Applies the multiplicative identities of Euler's totient function.",
            TransformationRelation.Equivalence,
            Soundness.SoundUnderAssumptions,
            Matching.MatchedRules.PhiFunction);

        #endregion

        /// <summary>
        /// Every rule set registered above, in a fixed order. Enumerable so that a property
        /// that should hold of all of them can be tested over all of them rather than over
        /// whichever ones somebody remembered.
        /// </summary>
        public static IReadOnlyList<RewriteRuleSet> All { get; } = new[]
        {
            CanonicalOrder,
            CanonicalOrderCountingConstants,
            CanonicalOrderExact,
            InvertNegativePowers,
            InvertNegativeMultipliers,
            Common,
            DivisionPreparing,
            NumericNeat,
            Power,
            Expansion,
            Factorization,
            PerfectSquare,
            RationalizeDenominator,
            CollapseMultipleFractions,
            CommonDenominator,
            CommonDenominatorCountingConstants,
            CommonDenominatorExact,
            PolynomialLongDivision,
            PolynomialGcdCancellation,
            Trigonometric,
            NormalTrigonometricForm,
            CollapseTrigonometricFunctions,
            ExpandTrigonometric,
            ExpandMultipleAngle,
            Boolean,
            InequalityEquality,
            SetOperator,
            ExpandFactorialDivisions,
            FactorizeFactorialMultiplications,
            PhiFunction,
        };

        /// <summary>
        /// The <see cref="CanonicalOrder"/> family, chosen by how finely it distinguishes
        /// operands. The simplifier picks the level from which pass it is on.
        /// </summary>
        internal static RewriteRuleSet CanonicalOrderAt(TreeAnalyzer.SortLevel level)
            => level switch
            {
                TreeAnalyzer.SortLevel.MIDDLE_LEVEL => CanonicalOrderCountingConstants,
                TreeAnalyzer.SortLevel.LOW_LEVEL => CanonicalOrderExact,
                _ => CanonicalOrder
            };

        /// <summary>
        /// The <see cref="CommonDenominator"/> family, chosen the same way.
        /// </summary>
        internal static RewriteRuleSet CommonDenominatorAt(TreeAnalyzer.SortLevel level)
            => level switch
            {
                TreeAnalyzer.SortLevel.MIDDLE_LEVEL => CommonDenominatorCountingConstants,
                TreeAnalyzer.SortLevel.LOW_LEVEL => CommonDenominatorExact,
                _ => CommonDenominator
            };
    }
}
