//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Core.Transformations.Matching
{
    /// <summary>
    /// Rule sets written as data, a few at a time and deliberately so: the value of this file is
    /// that a set expressed here can be checked against the <c>switch</c> that already expresses
    /// it, so the migration is proven one set at a time rather than asserted wholesale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>MatchedRulesAgreeWithTheSwitchTest</c> is that check. It runs both forms over
    /// generated expressions and requires them to agree on every one, which is what makes
    /// replacing the <c>switch</c> a mechanical step rather than a leap.
    /// </para>
    /// <para>
    /// <see cref="PythagoreanIdentity"/> is the exception, and is here for the opposite reason:
    /// there is nothing for it to agree with. It uses n-ary matching to say something the
    /// <c>switch</c> has no way of saying, so it is checked against the mathematics rather than
    /// against the code it would replace.
    /// </para>
    /// <para>
    /// <b>Both sides of every rule here are patterns</b>, so thirteen of the fourteen can be read
    /// backwards — <see cref="MatchedRule.Reversed"/>, and
    /// <c>Docs/Contributing/ReversibleRules.md</c> for what that requires and what it does not
    /// claim. The fourteenth is the Pythagorean identity, which cannot, for a reason that is about
    /// the mathematics rather than about the encoding.
    /// </para>
    /// <para>
    /// <b>What it costs to use one of these has been measured end to end, and the first answer
    /// was wrong about the reason.</b> The first exchange cost about <b>5% of
    /// <c>Simplify</c>'s time</b> for one set, which was recorded here as the price of the idea
    /// and as the argument against doing it wholesale. It was not the idea: <c>NodePattern</c>
    /// recomputed <see cref="MatchPattern.IsDeterministic"/> on <i>every attempt</i>, walking the
    /// whole pattern tree behind a delegate before any matching began, so the case that does the
    /// least work — a rule that does not fire — paid the most for it. Settled once instead, a
    /// miss goes 29.99 ns → 13.48 ns and a whole pass 1182.9 ns → 659.9 ns at identical
    /// allocation.
    /// </para>
    /// <para>
    /// Re-measured against <c>Simplify</c> itself, three arms in one process with the third a
    /// second copy of the first, the exchange is <b>inside the noise floor on time</b> — six
    /// samples each, medians +0.12% apart where the same binary spreads 1.90% — and
    /// <b>+0.14% on allocation</b>, which is the one figure that reproduces: two copies of the
    /// same assembly agree to 5 bytes in 45.6 MB. So a set is exchanged when its rules want to
    /// be data, and the cost is no longer the reason not to.
    /// </para>
    /// </remarks>
    internal static class MatchedRules
    {
        /// <summary>
        /// <see cref="Functions.Patterns.DivisionPreparingRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// Chosen first because it is three rules with no side conditions, so it exercises node
        /// matching, a literal, a typed hole and a repeated-free binding without also needing
        /// commutativity — which the matcher does not have and which #248 is about.
        /// </remarks>
        internal static MatchedRuleSet DivisionPreparing { get; } = new(
            nameof(DivisionPreparing),

            // a * (1 / b) -> a / b
            new MatchedRule(
                "reciprocal-factor-becomes-a-quotient",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Divf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Any("b"))),
                MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                // a * (1/b) and a/b are undefined at exactly the same points, but the quotient
                // is a quotient either way, so this inherits division's own condition rather
                // than adding one. Left at the conservative tier until the audit reaches it.
                Soundness.SoundUnderAssumptions,
                description: "a * (1 / b) = a / b"),

            // (c * a) / b -> c * (a / b), for a numeric c
            new MatchedRule(
                "numeric-factor-out-of-a-quotient",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any("a")),
                    MatchPattern.Any("b")),
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("c"),
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                Soundness.SoundUnderAssumptions,
                description: "(c * a) / b = c * (a / b), for a numeric c"),

            // (c / a) * b -> c * (b / a), for a numeric c
            new MatchedRule(
                "numeric-numerator-out-of-a-product",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any<Number>("c"), MatchPattern.Any("a")),
                    MatchPattern.Any("b")),
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("c"),
                    MatchPattern.Node<Divf>(MatchPattern.Any("b"), MatchPattern.Any("a"))),
                Soundness.SoundUnderAssumptions,
                description: "(c / a) * b = c * (b / a), for a numeric c"));

        /// <summary>
        /// <see cref="Functions.Patterns.CollapseMultipleFractions"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The second set expressed here, and chosen because it is harder in three ways that
        /// test whether the shape generalises rather than whether it works once. It has eight
        /// rules instead of three; it is <b>order-dependent</b>, since
        /// <c>Mulf(Divf, Divf)</c> has to be tried before <c>Mulf(a, Divf)</c> or the more
        /// general rule would swallow the special one; and it needs a <b>predicate on a
        /// hole</b> — <c>Integer { IsPositive: true }</c> — which the matcher did not have.
        /// </para>
        /// <para>
        /// One feature was added for it and nothing else changed, which is the answer to the
        /// question this set was picked to ask.
        /// </para>
        /// <para>
        /// <b>The order-dependence above is no longer maintained by hand.</b>
        /// <see cref="MatchPattern.Subsumes"/> computes it — <c>Mulf(a, Divf(b, c))</c> matches
        /// everything <c>Mulf(Divf(a, b), Divf(c, d))</c> matches and more — and
        /// <c>MatchedRuleSet.RulesByPriority</c> puts the specific rule first because of that
        /// rather than because of where it sits in this file. Four of this set's rule pairs are
        /// ordered that way, and they are four of only six such conflicts in the whole registry;
        /// <c>RulePriorityTest</c> lists them.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet CollapseMultipleFractions { get; } = new(
            nameof(CollapseMultipleFractions),

            // (a / b) ^ c -> a^c / b^c, for a positive whole c
            new MatchedRule(
                "positive-power-of-a-quotient-distributes",
                MatchPattern.Node<Powf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Any<Integer>("c", whole => whole.IsPositive)),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("c")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                Soundness.SoundUnderAssumptions,
                description: "(a / b) ^ c = a^c / b^c, for a positive whole c"),

            // (a * b) ^ c -> a^c * b^c, for a positive whole c
            new MatchedRule(
                "positive-power-of-a-product-distributes",
                MatchPattern.Node<Powf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Any<Integer>("c", whole => whole.IsPositive)),
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("c")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                Soundness.SoundUnderAssumptions,
                description: "(a * b) ^ c = a^c * b^c, for a positive whole c"),

            // (a/b) * (c/d) -> (a*c) / (b*d). Before the two below it, which are more general.
            new MatchedRule(
                "product-of-two-quotients",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Node<Divf>(MatchPattern.Any("c"), MatchPattern.Any("d"))),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c")),
                    MatchPattern.Node<Mulf>(MatchPattern.Any("b"), MatchPattern.Any("d"))),
                Soundness.SoundUnderAssumptions,
                description: "(a / b) * (c / d) = (a * c) / (b * d)"),

            new MatchedRule(
                "product-with-a-quotient-on-the-right",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Divf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Any("c")),
                Soundness.SoundUnderAssumptions,
                description: "a * (b / c) = (a * b) / c"),

            new MatchedRule(
                "product-with-a-quotient-on-the-left",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Any("c")),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c")),
                    MatchPattern.Any("b")),
                Soundness.SoundUnderAssumptions,
                description: "(a / b) * c = (a * c) / b"),

            // (a/b) / (c/d) -> (a*d) / (b*c). Likewise before the two below it.
            new MatchedRule(
                "quotient-of-two-quotients",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Node<Divf>(MatchPattern.Any("c"), MatchPattern.Any("d"))),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("d")),
                    MatchPattern.Node<Mulf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                Soundness.SoundUnderAssumptions,
                description: "(a / b) / (c / d) = (a * d) / (b * c)"),

            new MatchedRule(
                "quotient-whose-numerator-is-a-quotient",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Any("c")),
                MatchPattern.Node<Divf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Mulf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                Soundness.SoundUnderAssumptions,
                description: "(a / b) / c = a / (b * c)"),

            new MatchedRule(
                "quotient-whose-denominator-is-a-quotient",
                MatchPattern.Node<Divf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Divf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c")),
                    MatchPattern.Any("b")),
                Soundness.SoundUnderAssumptions,
                description: "a / (b / c) = (a * c) / b"));

        /// <summary>
        /// The one rule from <see cref="Functions.Patterns.PowerRules"/> that carries a real
        /// side condition, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The third set expressed here, and the one that exercises the last piece of the
        /// design: a <b>condition about the match as a whole</b> rather than about one hole.
        /// <c>(a^b)^c = a^(b*c)</c> is true for a positive base whatever the exponents, and for
        /// any base when the outer exponent is whole — and false outside those two, which is
        /// <a href="https://github.com/asc-community/AngouriMath/issues/752">#752</a>: applied
        /// unconditionally it turned <c>sqrt(x^2)</c> into <c>x</c>, which at -0.63 is -0.63
        /// where the expression is 0.63.
        /// </para>
        /// <para>
        /// It is also the first rule here whose <see cref="Soundness"/> carries information
        /// rather than repeating its neighbours'. The condition is what makes it
        /// <see cref="Soundness.SoundUnderAssumptions"/>, and a reader can see the condition
        /// and the tier in one place — which is the whole argument for rules being data, since
        /// in the <c>switch</c> the tier lives on the set and the condition lives forty lines
        /// away from it.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet PowerOfPower { get; } = new(
            nameof(PowerOfPower),

            new MatchedRule(
                "power-of-a-power-multiplies-its-exponents",
                MatchPattern.Node<Powf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Any("c")),
                MatchPattern.Node<Powf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Mulf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                Soundness.SoundUnderAssumptions,
                // Two bindings at once, which no predicate on a single hole can express.
                when: bound => bound["c"] is Integer
                    || bound["a"].Evaled is Real { IsPositive: true }));

        /// <summary>
        /// <c>k*p + k*q = k*(p + q)</c>, written once, where the <c>switch</c> writes it four
        /// times.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is what <a href="https://github.com/asc-community/AngouriMath/issues/248">#248</a>
        /// is for. <c>Patterns.CommonRules</c> spells the same identity out in four arms —
        /// <c>(k*p) + (k*q)</c>, <c>(p*k) + (k*q)</c>, <c>(k*p) + (q*k)</c>, <c>(p*k) + (q*k)</c>
        /// — because a C# pattern cannot say "either way round". One commutative pattern says
        /// it, and the four arms become one rule.
        /// </para>
        /// <para>
        /// It is also the <b>first rule here that is <see cref="Soundness.Sound"/></b>.
        /// Distributivity holds for every complex <c>k</c>, <c>p</c> and <c>q</c> with no side
        /// condition and no branch to choose, so the tier says something its neighbours' does
        /// not — which is the whole reason a tier belongs on a rule rather than on a set.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet SharedFactor { get; } = new(
            nameof(SharedFactor),

            new MatchedRule(
                "a-shared-factor-comes-out-of-a-sum",
                MatchPattern.Commutative<Sumf>(
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("p")),
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("q"))),
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("k"),
                    MatchPattern.Node<Sumf>(MatchPattern.Any("p"), MatchPattern.Any("q"))),
                Soundness.Sound));

        /// <summary>
        /// The Pythagorean identity, written once and firing wherever the two terms sit.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This set exists to show what <see cref="MatchPattern.Gathered{T}"/> buys, because the
        /// <c>switch</c> cannot express it. <c>Patterns.TrigonometricRules</c> spends <b>two</b>
        /// arms on this identity — <c>Sumf(Powf(Sinf, 2), Powf(Cosf, 2))</c> and the same with the
        /// operands the other way round — because it has no commutative matching, and both match
        /// only two children of one <c>Sumf</c>. So the pair is found in
        /// <c>sin(x)^2 + cos(x)^2</c> and missed in <c>a + sin(x)^2 + b + cos(x)^2</c>, where the
        /// two terms are not siblings. The library's answer is to sort the operands with
        /// <c>CanonicalOrder</c> before the rules run, so the pair becomes adjacent. That works,
        /// and it is the matcher's limitation showing through as a pipeline stage.
        /// </para>
        /// <para>
        /// Here the rule says what it means: <i>among the terms of this sum, find one squared
        /// sine and one squared cosine of the same argument</i>. The rest of the sum comes back
        /// bound, and is <c>0</c> when there is none, so the same rule covers both shapes.
        /// </para>
        /// <para>
        /// <see cref="Soundness.Sound"/>: <c>sin²+cos² = 1</c> holds for every complex argument,
        /// with no branch to choose and no point excluded.
        /// </para>
        /// <para>
        /// <b>The one rule here with no backwards reading</b>, and the reason is the identity
        /// rather than the way it is written: <c>1</c> does not say which angle it came from, so
        /// <c>x</c> is bound on the left and mentioned nowhere on the right.
        /// <see cref="MatchedRule.Reversal"/> is <see cref="RuleReversal.ReplacementDropsHoles"/>
        /// and <see cref="MatchedRule.Reversed"/> is <see langword="null"/>.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet PythagoreanIdentity { get; } = new(
            nameof(PythagoreanIdentity),

            // ... + sin(x)^2 + ... + cos(x)^2 + ...  ->  1 + (everything else)
            new MatchedRule(
                "squared-sine-and-cosine-of-one-argument-sum-to-one",
                MatchPattern.Gathered<Sumf>(
                    "rest",
                    MatchPattern.Node<Powf>(
                        MatchPattern.Node<Sinf>(MatchPattern.Any("x")),
                        MatchPattern.Exact(Integer.Create(2))),
                    MatchPattern.Node<Powf>(
                        MatchPattern.Node<Cosf>(MatchPattern.Any("x")),
                        MatchPattern.Exact(Integer.Create(2)))),
                // "x" is bound by the first part and re-matched by the second, so the two terms
                // are required to be about the same argument rather than merely both squared.
                MatchPattern.Node<Sumf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Any("rest")),
                Soundness.Sound));

        /// <summary>
        /// <see cref="Functions.Patterns.InvertNegativePowers"/>, as data.
        /// </summary>
        /// <remarks>
        /// One rule, and the first here whose replacement is <b>code rather than a pattern</b>:
        /// <c>-1 * n</c> is arithmetic on the bound integer and not a tree built around it, so
        /// writing it as a pattern would produce <c>a ^ (-1 * -3)</c> where the <c>switch</c>
        /// produces <c>a ^ 3</c>. That costs the reversal —
        /// <see cref="RuleReversal.ReplacementIsCode"/> — which is the honest trade and is
        /// recorded by the type rather than by a comment.
        /// </remarks>
        internal static MatchedRuleSet InvertNegativePowers { get; } = new(
            nameof(InvertNegativePowers),

            // a ^ n -> 1 / a ^ (-n), for a negative integer n
            new MatchedRule(
                "a-negative-integer-power-becomes-a-reciprocal",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Any<Integer>("n", n => n.IsNegative)),
                bound => 1 / MathS.Pow(bound["a"], -1 * (Integer)bound["n"]),
                // Both sides are undefined at exactly one place, a = 0, and equal everywhere
                // else in the complex plane: this is the definition of a negative power rather
                // than an identity that needs one. Measured at 0 as well as away from it before
                // the tier was written down.
                Soundness.Sound,
                description: "a ^ n = 1 / a ^ (-n), for a negative integer n",
                // Three nodes become five, for every input: the power stays a power with its
                // exponent replaced one for one, and the quotient and the numerator 1 the
                // replacement wraps it in are the two. A different mechanism from the negation
                // rules that share this figure, and the same arithmetic. Measured at +2 on all
                // 27 firings over the corpus.
                growth: RewriteRuleGrowth.Expands));

        /// <summary>
        /// <see cref="Functions.Patterns.InvertNegativeMultipliers"/>, as data.
        /// </summary>
        /// <remarks>
        /// Two rules that differ in the way that matters here. The first negates a bound real,
        /// so its replacement is code and it has no reversal. The second only rearranges what it
        /// bound — <c>-(a - b) = b - a</c> — so both of its sides are patterns and it reads
        /// backwards, which is the distinction <see cref="MatchedRule.Reversal"/> exists to make
        /// visible within one set.
        /// </remarks>
        internal static MatchedRuleSet InvertNegativeMultipliers { get; } = new(
            nameof(InvertNegativeMultipliers),

            // a + (c * b) -> a - (-c) * b, for a negative real c
            new MatchedRule(
                "a-negative-factor-in-a-sum-becomes-a-difference",
                MatchPattern.Node<Sumf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Any<Real>("c", c => c.IsNegative),
                        MatchPattern.Any("b"))),
                bound => bound["a"] - (-1 * (Real)bound["c"]) * bound["b"],
                Soundness.Sound,
                description: "a + (c * b) = a - (-c) * b, for a negative real c",
                // Sum for difference and product for product, one operator each, and the literal
                // replaced by its magnitude one node for one: the same size whatever it matches.
                growth: RewriteRuleGrowth.Rearranges),

            // (-1) * (a - b) -> b - a
            new MatchedRule(
                "a-negated-difference-is-the-difference-the-other-way",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Exact(Integer.Create(-1)),
                    MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                MatchPattern.Node<Minusf>(MatchPattern.Any("b"), MatchPattern.Any("a")),
                Soundness.Sound,
                description: "(-1) * (a - b) = b - a"));

        /// <summary>
        /// <see cref="Functions.Patterns.NormalTrigonometricForm"/>, as data.
        /// </summary>
        /// <remarks>
        /// Four rules, and <b>the first set here where every rule reverses</b>: each writes one
        /// of the four derived trigonometric functions in terms of sine and cosine, and both
        /// sides are patterns, so reading a rule backwards recognises the quotient and puts the
        /// name back. That direction is not run by anything today — it is the e-graph's
        /// inverse-pair table that would — but it is now a property of the rules rather than a
        /// second set someone has to write.
        /// </remarks>
        internal static MatchedRuleSet NormalTrigonometricForm { get; } = new(
            nameof(NormalTrigonometricForm),

            // tan(a) -> sin(a) / cos(a)
            new MatchedRule(
                "tangent-is-sine-over-cosine",
                MatchPattern.Node<Tanf>(MatchPattern.Any("a")),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Sinf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Cosf>(MatchPattern.Any("a"))),
                // The quotient is undefined exactly where the tangent is -- at a zero of the
                // cosine -- so the domain neither widens nor narrows. Left at the conservative
                // tier with its three neighbours until the audit reaches them together.
                Soundness.SoundUnderAssumptions,
                description: "tan(a) = sin(a) / cos(a)"),

            // cotan(a) -> cos(a) / sin(a)
            new MatchedRule(
                "cotangent-is-cosine-over-sine",
                MatchPattern.Node<Cotanf>(MatchPattern.Any("a")),
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Cosf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Sinf>(MatchPattern.Any("a"))),
                Soundness.SoundUnderAssumptions,
                description: "cotan(a) = cos(a) / sin(a)"),

            // sec(a) -> 1 / cos(a)
            new MatchedRule(
                "secant-is-one-over-cosine",
                MatchPattern.Node<Secantf>(MatchPattern.Any("a")),
                MatchPattern.Node<Divf>(
                    MatchPattern.Exact(Integer.Create(1)),
                    MatchPattern.Node<Cosf>(MatchPattern.Any("a"))),
                Soundness.SoundUnderAssumptions,
                description: "sec(a) = 1 / cos(a)"),

            // cosec(a) -> 1 / sin(a)
            new MatchedRule(
                "cosecant-is-one-over-sine",
                MatchPattern.Node<Cosecantf>(MatchPattern.Any("a")),
                MatchPattern.Node<Divf>(
                    MatchPattern.Exact(Integer.Create(1)),
                    MatchPattern.Node<Sinf>(MatchPattern.Any("a"))),
                Soundness.SoundUnderAssumptions,
                description: "cosec(a) = 1 / sin(a)"));

        /// <summary>
        /// <see cref="Functions.Patterns.PhiFunctionRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// One rule, and the first whose <b>predicate on a hole is a mathematical property</b>
        /// rather than a sign or a type: <c>phi(p ^ k) = p ^ (k - 1) * (p - 1)</c> holds for a
        /// prime <c>p</c> and for no other integer, so primality is the condition and the hole
        /// carries it. The replacement is arithmetic on the bound prime, so it is code and the
        /// rule does not reverse.
        /// </remarks>
        internal static MatchedRuleSet PhiFunction { get; } = new(
            nameof(PhiFunction),

            // phi(p ^ k) -> p ^ (k - 1) * (p - 1), for a prime p
            new MatchedRule(
                "eulers-totient-of-a-prime-power",
                MatchPattern.Node<Phif>(
                    MatchPattern.Node<Powf>(
                        MatchPattern.Any<Integer>("p", p => p.IsPrime),
                        MatchPattern.Any("k"))),
                // (Integer) on the prime and not on the exponent, and the difference is the
                // point: p - 1 is arithmetic that folds to a number, k - 1 is a tree because k
                // is whatever was bound. Written without the cast the rule builds 2 ^ (5 - 1) *
                // (2 - 1) where the switch builds 2 ^ (5 - 1) * 1, which is what the agreement
                // test caught.
                bound => new Powf(bound["p"], bound["k"] - 1) * ((Integer)bound["p"] - 1),
                // Euler's product formula, which is a theorem and not an assumption: for a prime
                // p the integers below p^k sharing a factor with it are exactly the multiples of
                // p, of which there are p^(k-1).
                Soundness.Sound,
                description: "phi(p ^ k) = p ^ (k - 1) * (p - 1), for a prime p",
                // Each of the two holes is used once on both sides, and (p - 1) folds to a single
                // integer as the replacement is built -- the cast noted above is what makes it
                // fold. So what the replacement adds is the product and the decremented exponent,
                // three nodes, for every input.
                growth: RewriteRuleGrowth.Expands));

        /// <summary>
        /// <see cref="Functions.Patterns.CollapseTrigonometricFunctions"/>, as data.
        /// </summary>
        /// <remarks>
        /// The reverse of <see cref="NormalTrigonometricForm"/>, and the pair is worth having as
        /// data together: they are each other read backwards, and the type says so instead of a
        /// comment. Order is load-bearing — the two named quotients must be tried before the
        /// general reciprocal rules, or <c>sin(x) / cos(x)</c> becomes <c>sin(x) * sec(x)</c>
        /// rather than <c>tan(x)</c>.
        /// </remarks>
        internal static MatchedRuleSet CollapseTrigonometricFunctions { get; } = new(
            nameof(CollapseTrigonometricFunctions),

            // sin(a) / cos(a) -> tan(a). "a" is bound by the numerator and re-matched by the
            // denominator, which is how the pattern says "of the same argument".
            new MatchedRule(
                "sine-over-cosine-is-the-tangent",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Sinf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Cosf>(MatchPattern.Any("a"))),
                MatchPattern.Node<Tanf>(MatchPattern.Any("a")),
                Soundness.SoundUnderAssumptions,
                description: "sin(a) / cos(a) = tan(a)"),

            // cos(a) / sin(a) -> cotan(a)
            new MatchedRule(
                "cosine-over-sine-is-the-cotangent",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Cosf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Sinf>(MatchPattern.Any("a"))),
                MatchPattern.Node<Cotanf>(MatchPattern.Any("a")),
                Soundness.SoundUnderAssumptions,
                description: "cos(a) / sin(a) = cotan(a)"),

            // a / sin(b) -> a * cosec(b)
            new MatchedRule(
                "a-quotient-by-a-sine-is-a-cosecant",
                MatchPattern.Node<Divf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Sinf>(MatchPattern.Any("b"))),
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Cosecantf>(MatchPattern.Any("b"))),
                Soundness.SoundUnderAssumptions,
                description: "a / sin(b) = a * cosec(b)"),

            // a / cos(b) -> a * sec(b)
            new MatchedRule(
                "a-quotient-by-a-cosine-is-a-secant",
                MatchPattern.Node<Divf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Cosf>(MatchPattern.Any("b"))),
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Secantf>(MatchPattern.Any("b"))),
                Soundness.SoundUnderAssumptions,
                description: "a / cos(b) = a * sec(b)"));

        /// <summary>
        /// <see cref="Functions.Patterns.ExpandRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// The angle-sum identities, and both sides of each are patterns — so read backwards they
        /// are the angle-<i>gathering</i> identities, which the <c>switch</c> would need a second
        /// set to say.
        /// </remarks>
        internal static MatchedRuleSet Expansion { get; } = new(
            nameof(Expansion),

            // sin(a + b) -> sin(a)cos(b) + sin(b)cos(a)
            new MatchedRule(
                "sine-of-a-sum",
                MatchPattern.Node<Sinf>(
                    MatchPattern.Node<Sumf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                MatchPattern.Node<Sumf>(
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Node<Sinf>(MatchPattern.Any("a")),
                        MatchPattern.Node<Cosf>(MatchPattern.Any("b"))),
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Node<Sinf>(MatchPattern.Any("b")),
                        MatchPattern.Node<Cosf>(MatchPattern.Any("a")))),
                // Holds for every complex a and b: the identity is the addition theorem, which
                // has no branch to fall off.
                Soundness.Sound,
                description: "sin(a + b) = sin(a)cos(b) + sin(b)cos(a)"),

            // sin(a - b) -> sin(a)cos(b) - sin(b)cos(a)
            new MatchedRule(
                "sine-of-a-difference",
                MatchPattern.Node<Sinf>(
                    MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                MatchPattern.Node<Minusf>(
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Node<Sinf>(MatchPattern.Any("a")),
                        MatchPattern.Node<Cosf>(MatchPattern.Any("b"))),
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Node<Sinf>(MatchPattern.Any("b")),
                        MatchPattern.Node<Cosf>(MatchPattern.Any("a")))),
                Soundness.Sound,
                description: "sin(a - b) = sin(a)cos(b) - sin(b)cos(a)"));

        /// <summary>
        /// <see cref="Functions.Patterns.ExpandTrigonometricRules"/>, as data.
        /// </summary>
        internal static MatchedRuleSet ExpandTrigonometric { get; } = new(
            nameof(ExpandTrigonometric),

            // (1/2) * sin(2a) -> sin(a) * cos(a)
            new MatchedRule(
                "half-the-sine-of-a-doubled-angle",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Exact(Rational.Create(1, 2)),
                    MatchPattern.Node<Sinf>(
                        MatchPattern.Node<Mulf>(
                            MatchPattern.Exact(Integer.Create(2)),
                            MatchPattern.Any("a")))),
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Sinf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Cosf>(MatchPattern.Any("a"))),
                Soundness.Sound,
                description: "(1/2) * sin(2a) = sin(a) * cos(a)"),

            // cos(2a) -> cos(a)^2 - sin(a)^2
            new MatchedRule(
                "cosine-of-a-doubled-angle",
                MatchPattern.Node<Cosf>(
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Exact(Integer.Create(2)),
                        MatchPattern.Any("a"))),
                MatchPattern.Node<Minusf>(
                    MatchPattern.Node<Powf>(
                        MatchPattern.Node<Cosf>(MatchPattern.Any("a")),
                        MatchPattern.Exact(Integer.Create(2))),
                    MatchPattern.Node<Powf>(
                        MatchPattern.Node<Sinf>(MatchPattern.Any("a")),
                        MatchPattern.Exact(Integer.Create(2)))),
                Soundness.Sound,
                description: "cos(2a) = cos(a)^2 - sin(a)^2"));

        /// <summary>
        /// <see cref="Functions.Patterns.ExpandMultipleAngleRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// The replacement is a Chebyshev-style expansion computed from the multiplier, so it is
        /// code and the rule does not reverse. The predicate on the hole asks
        /// <see cref="Functions.Patterns.IsWorthExpanding"/> rather than repeating its bound,
        /// which is how two copies of a constant start disagreeing.
        /// </remarks>
        internal static MatchedRuleSet ExpandMultipleAngle { get; } = new(
            nameof(ExpandMultipleAngle),

            // sin(n * a) -> the expansion, for a whole n worth expanding
            new MatchedRule(
                "sine-of-a-whole-multiple-of-an-angle",
                MatchPattern.Node<Sinf>(
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Any<Integer>("n", Functions.Patterns.IsWorthExpanding),
                        MatchPattern.Any("a"))),
                bound => TrigonometricAngleExpansion.ExpandSineArgumentMultiplied(
                    new Sinf(bound["a"]), new Cosf(bound["a"]),
                    ((Integer)bound["n"]).EInteger.ToInt32Checked()),
                Soundness.Sound,
                // Declared, because the replacement is code and so nothing counts it. The
                // Chebyshev expansion of sin(n * a) is a sum of n terms where the pattern is one
                // node, for every n this fires on -- IsWorthExpanding is what bounds n, not what
                // makes the direction uncertain.
                RewriteRuleGrowth.Expands,
                description: "sin(n * a) = its expansion, for a whole n worth expanding"),

            // cos(n * a) -> the expansion, for a whole n worth expanding
            new MatchedRule(
                "cosine-of-a-whole-multiple-of-an-angle",
                MatchPattern.Node<Cosf>(
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Any<Integer>("n", Functions.Patterns.IsWorthExpanding),
                        MatchPattern.Any("a"))),
                bound => TrigonometricAngleExpansion.ExpandCosineArgumentMultiplied(
                    new Sinf(bound["a"]), new Cosf(bound["a"]),
                    ((Integer)bound["n"]).EInteger.ToInt32Checked()),
                Soundness.Sound,
                // Declared, because the replacement is code and so nothing counts it. The
                // Chebyshev expansion of sin(n * a) is a sum of n terms where the pattern is one
                // node, for every n this fires on -- IsWorthExpanding is what bounds n, not what
                // makes the direction uncertain.
                RewriteRuleGrowth.Expands,
                description: "cos(n * a) = its expansion, for a whole n worth expanding"));

        /// <summary>
        /// <see cref="Functions.Patterns.PolynomialLongDivision"/>, as data.
        /// </summary>
        /// <remarks>
        /// One rule with <b>no side condition</b>, deliberately. The work that decides whether it
        /// applies is the division itself, and asking it in a guard would run it twice on a path
        /// the simplifier takes for every quotient; the helper hands back the expression
        /// unchanged where it declines, which <see cref="MatchedRuleSet.ApplyHere"/> reads as no
        /// rewrite exactly as the <c>switch</c>'s own fallthrough does.
        /// </remarks>
        internal static MatchedRuleSet PolynomialLongDivision { get; } = new(
            nameof(PolynomialLongDivision),

            new MatchedRule(
                "a-quotient-of-polynomials-is-divided-out",
                MatchPattern.Node<Divf>(MatchPattern.Any("n"), MatchPattern.Any("d")),
                // The division is only the quotient where the divisor has a value. `ln(x) / ln(x)`
                // divides out to `1 + 0 / ln(x)`, which is 1 at x = 0 while the quotient it came
                // from is undefined there -- ln(0) is a pole, and the remainder term carries only
                // `ln(x) != 0`. The condition folds away for any divisor that cannot be undefined.
                // https://github.com/asc-community/AngouriMath/issues/1174
                (node, bound) => TreeAnalyzer.PolynomialLongDivision(bound["n"], bound["d"])
                    is var (divided, remainder)
                    ? (divided + remainder).Provided(bound["d"].DomainCondition)
                    : node,
                Soundness.SoundUnderAssumptions,
                // Left Unknown: a quotient and a remainder can be smaller than the fraction or
                // much larger, and the divisor's domain condition is attached besides.
                description: "n / d = quotient + remainder, by polynomial long division, "
                    + "provided d is defined"));

        /// <summary>
        /// <see cref="Functions.Patterns.PolynomialGcdCancellation"/>, as data.
        /// </summary>
        /// <remarks>
        /// No side condition, for the reason <see cref="PolynomialLongDivision"/> gives: the
        /// cancellation is the test.
        /// </remarks>
        internal static MatchedRuleSet PolynomialGcdCancellation { get; } = new(
            nameof(PolynomialGcdCancellation),

            new MatchedRule(
                "a-quotient-of-polynomials-is-put-in-lowest-terms",
                MatchPattern.Node<Divf>(MatchPattern.Any("n"), MatchPattern.Any("d")),
                (node, bound) => PolynomialGcd.TryCancel(bound["n"], bound["d"], out var cancelled)
                    ? cancelled
                    : node,
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the factor cancelled is of any size, and the two quotients are
                // rebuilt by the helper rather than counted from the pattern.
                description: "n / d = (n / g) / (d / g), for g the gcd of n and d"));

        /// <summary>
        /// <see cref="Functions.Patterns.ExpandFactorialDivisions"/>, as data.
        /// </summary>
        /// <remarks>
        /// <b>Eight arms become three.</b> Four of the eight are one rule written for every way a
        /// sum can be spelled — <c>x + c</c> or <c>c + x</c>, on each side of the quotient — which
        /// is what <see cref="MatchPattern.Commutative{T}"/> says once. The other four are the
        /// cases where one factorial has no constant at all, and they stay separate because the
        /// constant they pass is <c>0</c> rather than a binding.
        /// </remarks>
        internal static MatchedRuleSet ExpandFactorialDivisions { get; } = new(
            nameof(ExpandFactorialDivisions),

            // (x + a)! / (y + b)!
            new MatchedRule(
                "a-quotient-of-shifted-factorials",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Factorialf>(MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("x"), MatchPattern.Any<Number>("a"))),
                    MatchPattern.Node<Factorialf>(MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("y"), MatchPattern.Any<Number>("b")))),
                (node, bound) => Functions.Patterns.CancelFactorials(
                    node, bound["x"], bound["y"], (Number)bound["a"], (Number)bound["b"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the answer is the product of the terms between the two offsets,
                // which collects when they are one apart and expands when they are five.
                description: "(x + a)! / (y + b)! = the product between them, where the offsets are close"),

            // x! / (y + b)!
            new MatchedRule(
                "a-quotient-of-a-plain-factorial-by-a-shifted-one",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Factorialf>(MatchPattern.Any("x")),
                    MatchPattern.Node<Factorialf>(MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("y"), MatchPattern.Any<Number>("b")))),
                (node, bound) => Functions.Patterns.CancelFactorials(
                    node, bound["x"], bound["y"], Integer.Create(0), (Number)bound["b"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: as above, the answer's size is the offset's.
                description: "x! / (y + b)! = the product between them, where the offsets are close"),

            // (x + a)! / y!
            new MatchedRule(
                "a-quotient-of-a-shifted-factorial-by-a-plain-one",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Factorialf>(MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("x"), MatchPattern.Any<Number>("a"))),
                    MatchPattern.Node<Factorialf>(MatchPattern.Any("y"))),
                (node, bound) => Functions.Patterns.CancelFactorials(
                    node, bound["x"], bound["y"], (Number)bound["a"], Integer.Create(0)),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: as above, the answer's size is the offset's.
                description: "(x + a)! / y! = the product between them, where the offsets are close"));

        /// <summary>
        /// <see cref="Functions.Patterns.FactorizeFactorialMultiplications"/>, as data.
        /// </summary>
        /// <remarks>
        /// The same eight-into-three collapse as <see cref="ExpandFactorialDivisions"/>, and for
        /// the same reason.
        /// </remarks>
        internal static MatchedRuleSet FactorizeFactorialMultiplications { get; } = new(
            nameof(FactorizeFactorialMultiplications),

            // (x + a)! * (y + b)
            new MatchedRule(
                "a-shifted-factorial-times-the-next-term",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Factorialf>(MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("x"), MatchPattern.Any<Number>("a"))),
                    MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("y"), MatchPattern.Any<Number>("b"))),
                (node, bound) => Functions.Patterns.GatherFactorial(
                    node, bound["x"], bound["y"], (Number)bound["a"], (Number)bound["b"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the helper folds the offsets it gathers, so the answer is one
                // factorial of a sum whose size is theirs -- smaller for a bare next term and
                // not for one written with a larger offset.
                description: "(x + a)! * (y + b) = (x + a + 1)!, where (y + b) is the next term"),

            // x! * (y + b)
            new MatchedRule(
                "a-plain-factorial-times-the-next-term",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Factorialf>(MatchPattern.Any("x")),
                    MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("y"), MatchPattern.Any<Number>("b"))),
                (node, bound) => Functions.Patterns.GatherFactorial(
                    node, bound["x"], bound["y"], Integer.Create(0), (Number)bound["b"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: as above, the answer's size is the offset's.
                description: "x! * (y + b) = (x + 1)!, where (y + b) is the next term"),

            // (x + a)! * y
            new MatchedRule(
                "a-shifted-factorial-times-a-bare-term",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Factorialf>(MatchPattern.Commutative<Sumf>(
                        MatchPattern.Any("x"), MatchPattern.Any<Number>("a"))),
                    MatchPattern.Any("y")),
                (node, bound) => Functions.Patterns.GatherFactorial(
                    node, bound["x"], bound["y"], (Number)bound["a"], Integer.Create(0)),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: as above, the answer's size is the offset's.
                description: "(x + a)! * y = (x + a + 1)!, where y is the next term"),

            // The binomial coefficient's three identities, each in the direction that collects:
            // Pascal's rule, the chairperson identity and the symmetry. Sullivan and Mackey's
            // Props 8.4.1-8.4.3, which it proves by counting in two ways;
            // here they are identities of the falling factorial for a whole lower index and of
            // the gamma function elsewhere. https://github.com/asc-community/AngouriMath/issues/1409
            new MatchedRule(
                "two-binomial-coefficients-of-one-upper-index-and-adjacent-lower-indices-add-by-pascals-rule",
                MatchPattern.Node<Sumf>(
                    MatchPattern.Node<Binomialf>(MatchPattern.Any("n"), MatchPattern.Any("k")),
                    MatchPattern.Node<Binomialf>(MatchPattern.Any("n"), MatchPattern.Any("j"))),
                (node, bound) => Functions.Patterns.PascalsRule(node, bound["n"], bound["k"], bound["j"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: which of the two lower indices is the larger is decided by the
                // helper, and the answer is one coefficient where there were two.
                description: "binomial(n, k) + binomial(n, k - 1) = binomial(n + 1, k)"),

            new MatchedRule(
                "a-number-times-a-binomial-coefficient-of-the-number-less-one-is-the-chairperson-identity",
                MatchPattern.Commutative<Mulf>(
                    MatchPattern.Any("n"),
                    MatchPattern.Node<Binomialf>(MatchPattern.Any("m"), MatchPattern.Any("j"))),
                (node, bound) => Functions.Patterns.ChairpersonsRule(node, bound["n"], bound["m"], bound["j"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: fires only where the multiplier is one more than the upper
                // index, and the answer is written with the lower index one more.
                description: "n * binomial(n - 1, k - 1) = k * binomial(n, k)"),

            new MatchedRule(
                "a-binomial-coefficient-whose-lower-index-is-the-complement-is-the-symmetric-one",
                MatchPattern.Node<Binomialf>(MatchPattern.Any("n"), MatchPattern.Any("d")),
                (node, bound) => Functions.Patterns.SymmetricBinomial(node, bound["n"], bound["d"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: fires only where the complement of the lower index is the
                // smaller expression, and the answer is the coefficient with that.
                description: "binomial(n, n - k) = binomial(n, k)"));

        /// <summary>
        /// <see cref="Functions.Patterns.PerfectSquareRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One rule, and it is here to settle a question rather than for its own sake. The
        /// <c>switch</c> arm is <c>x is Sumf or Minusf</c> — an <b>alternation of node types</b>,
        /// which <see cref="MatchPattern.Node{T}"/> cannot say and which was recorded as needing
        /// an addition to the matcher.
        /// </para>
        /// <para>
        /// <b>It does not.</b> A typed hole with a predicate says it:
        /// <c>Any&lt;Entity&gt;(name, e =&gt; e is Sumf or Minusf)</c> matches either and binds the
        /// whole node, which is exactly what the arm does. The same shape covers every other
        /// construct on that list — <c>var x and not Integer(1)</c> is a predicate,
        /// <c>Rational and not Integer</c> is a predicate on a typed hole, and
        /// <c>not Set and not Matrix</c> is a predicate. So the matcher was never the thing
        /// standing in the way of those sets.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet PerfectSquare { get; } = new(
            nameof(PerfectSquare),

            new MatchedRule(
                "a-sum-or-difference-that-is-a-perfect-square",
                MatchPattern.Any<Entity>("x", node => node is Sumf or Minusf),
                (node, _) => Functions.Patterns.CollapseToPerfectSquare(node) ?? node,
                // sqrt(u)^2 is u for every complex u, so the identity itself is unconditional.
                // What is not is the test for whether the cross term matches, which needs
                // Simplify -- see the remark on Patterns.CollapseToPerfectSquare.
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the square built from the two roots is 5 - |cross term| against
                // the sum, and the cross term is written in as many sizes as there are ways to
                // write it.
                description: "a +- 2*sqrt(a)*sqrt(b) + b = (sqrt(a) +- sqrt(b))^2"));

        /// <summary>
        /// <see cref="Functions.Patterns.RationalizeDenominator"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This set is why <see cref="MatchedRuleSet.AsAddressable"/> exists. It is an ordinary
        /// method with branches and locals rather than a <c>switch</c>, so
        /// <c>RuleRegistryGenerator</c> declines it — and it was the one set in the registry with
        /// <b>no addressable rules at all</b>. Nothing about it could be listed, named in a
        /// report or checked by the tooling that reads arms.
        /// </para>
        /// <para>
        /// Neither rule carries a side condition, for the reason
        /// <see cref="PolynomialLongDivision"/> gives: the work that decides whether the rewrite
        /// applies <i>is</i> the rewrite, and each helper hands the expression back where it
        /// declines.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet RationalizeDenominator { get; } = new(
            nameof(RationalizeDenominator),

            // k * (value / d) -> (k * value) / d, where the numerator carries a surd
            new MatchedRule(
                "a-numeric-coefficient-is-gathered-over-a-surd",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any<Rational>("k"),
                    MatchPattern.Node<Divf>(MatchPattern.Any("value"), MatchPattern.Any<Rational>("d"))),
                (node, _) => Functions.Patterns.GatherNumericCoefficientOverASurd(node),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the helper folds the two rationals to one and scales the surd by
                // it, and what that scaling does with the surd's own coefficient is its own.
                description: "k * (value / d) = (k * value) / d"),

            // num / (a + b) -> num * (a - b) / (a^2 - b^2)
            new MatchedRule(
                "a-two-term-denominator-is-multiplied-by-its-conjugate",
                MatchPattern.Node<Divf>(MatchPattern.Any("num"), MatchPattern.Any("den")),
                (node, _) => Functions.Patterns.MultiplyByTheConjugate(node),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: as written the conjugate is 7 + |a| + |b| nodes more, and the
                // helper folds the numeric denominator it builds, so the corpus measured -2
                // and +2 for the same rule.
                description: "num / (a + b) = num * (a - b) / (a^2 - b^2)"));

        /// <summary>
        /// <see cref="Functions.Patterns.NumericNeatRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Sixteen arms become eleven.</b> Six of the sixteen are three rules written twice, once
        /// for each side a negative factor can sit on inside a product — which is
        /// <see cref="MatchPattern.Commutative{T}"/> — and two more are the one-sided sum and
        /// difference rules, whose two spellings are one rule about "the other operand".
        /// </para>
        /// <para>
        /// <b>Order is load-bearing and is kept exactly.</b> The both-negative rules come first,
        /// because a commutative one-sided rule matches a both-negative sum too and would take
        /// it. Every replacement negates a bound number, so all eleven are one-way.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet NumericNeat { get; } = new(
            nameof(NumericNeat),

            // (-a) + (-b) -> -(a + b), where a and b are the magnitudes
            new MatchedRule(
                "a-sum-of-two-negatives-is-a-negated-sum",
                MatchPattern.Node<Sumf>(
                    MatchPattern.Any<Real>("l", value => value.IsNegative),
                    MatchPattern.Any<Real>("r", value => value.IsNegative)),
                bound => -((-(Real)bound["l"]) + (Entity)(-(Real)bound["r"])),
                Soundness.Sound,
                description: "(-a) + (-b) = -(a + b)",
                // Three nodes become five, for every input: the negation the replacement puts
                // round the whole sum is two nodes, and each literal is replaced by its
                // magnitude one for one. Measured at +2 on all 9 firings over the corpus.
                growth: RewriteRuleGrowth.Expands),

            // x + (-a) -> x - a, either way round
            new MatchedRule(
                "a-negative-added-is-subtracted",
                MatchPattern.Commutative<Sumf>(
                    MatchPattern.Any("x"),
                    MatchPattern.Any<Real>("n", value => value.IsNegative)),
                bound => bound["x"] - -(Real)bound["n"],
                Soundness.Sound,
                description: "x + (-a) = x - a",
                // One binary operator becomes another and the literal is replaced by its
                // magnitude, one node for one, so three nodes stay three.
                growth: RewriteRuleGrowth.Rearranges),

            // (-a) - (-b) -> b - a
            new MatchedRule(
                "a-difference-of-two-negatives-turns-round",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Any<Real>("l", value => value.IsNegative),
                    MatchPattern.Any<Real>("r", value => value.IsNegative)),
                bound => (-(Real)bound["r"]) - (Entity)(-(Real)bound["l"]),
                Soundness.Sound,
                description: "(-a) - (-b) = b - a",
                // The difference stays a difference with its operands swapped, and each literal
                // is replaced by its magnitude, one node for one.
                growth: RewriteRuleGrowth.Rearranges),

            // x - (-a) -> x + a
            new MatchedRule(
                "a-negative-subtracted-is-added",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Any("x"),
                    MatchPattern.Any<Real>("n", value => value.IsNegative)),
                bound => bound["x"] + -(Real)bound["n"],
                Soundness.Sound,
                description: "x - (-a) = x + a",
                // One binary operator becomes another and the literal is replaced by its
                // magnitude, one node for one, so three nodes stay three.
                growth: RewriteRuleGrowth.Rearranges),

            // (-a) - x -> -(x + a)
            new MatchedRule(
                "a-negative-minuend-comes-out-in-front",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Any<Real>("n", value => value.IsNegative),
                    MatchPattern.Any("x")),
                bound => -(bound["x"] + -(Real)bound["n"]),
                Soundness.Sound,
                description: "(-a) - x = -(x + a)",
                // Three nodes become five, for every input: the difference becomes a sum, one
                // operator for one, and the negation round the whole of it is the two.
                // Measured at +2 on all 27 firings over the corpus.
                growth: RewriteRuleGrowth.Expands),

            // (-a) * (-b) -> a * b
            new MatchedRule(
                "a-product-of-two-negatives-is-positive",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any<Real>("l", value => value.IsNegative),
                    MatchPattern.Any<Real>("r", value => value.IsNegative)),
                bound => (-(Real)bound["l"]) * (Entity)(-(Real)bound["r"]),
                Soundness.Sound,
                description: "(-a) * (-b) = a * b",
                // The product stays a product and each literal is replaced by its magnitude,
                // one node for one.
                growth: RewriteRuleGrowth.Rearranges),

            // (-a) / (-b) -> a / b
            new MatchedRule(
                "a-quotient-of-two-negatives-is-positive",
                MatchPattern.Node<Divf>(
                    MatchPattern.Any<Real>("l", value => value.IsNegative),
                    MatchPattern.Any<Real>("r", value => value.IsNegative)),
                bound => (-(Real)bound["l"]) / (Entity)(-(Real)bound["r"]),
                Soundness.Sound,
                description: "(-a) / (-b) = a / b",
                // The quotient stays a quotient and each literal is replaced by its magnitude,
                // one node for one.
                growth: RewriteRuleGrowth.Rearranges),

            // (-a * x) * y -> -(a * (x * y)), the negative factor either way round.
            // A factor of -1 is declined here and in the three rules below it: its magnitude is 1,
            // so "taking the factor out" leaves a literal `1 *` behind that says nothing, and the
            // numerator rule and the denominator rule then undo each other around it -- `-x / (-y)`
            // grew by four nodes a pass for ever. -1 is not a factor to take out, it is the sign,
            // and the two rules above this one already answer it.
            // https://github.com/asc-community/AngouriMath/issues/1167
            new MatchedRule(
                "a-negative-factor-in-a-left-product-comes-out",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Commutative<Mulf>(
                        MatchPattern.Any<Real>("n", value => value.IsNegative && value != -1),
                        MatchPattern.Any("x")),
                    MatchPattern.Any("y")),
                bound => -((-(Real)bound["n"]) * (bound["x"] * bound["y"])),
                Soundness.Sound,
                description: "(-a * x) * y = -(a * (x * y))",
                // Five nodes become seven, for every input: the two products are regrouped, two
                // operators for two, the literal is replaced by its magnitude one for one, and
                // the negation round the whole of it is the difference. Measured at +2 on all
                // 63 firings over the corpus.
                growth: RewriteRuleGrowth.Expands),

            // (-a * x) / y -> -(a * (x / y)). Declines -1; see the left-product rule above, and
            // note that this rule and the denominator one below are the pair that cycled.
            new MatchedRule(
                "a-negative-factor-in-a-numerator-comes-out",
                MatchPattern.Node<Divf>(
                    MatchPattern.Commutative<Mulf>(
                        MatchPattern.Any<Real>("n", value => value.IsNegative && value != -1),
                        MatchPattern.Any("x")),
                    MatchPattern.Any("y")),
                bound => -((-(Real)bound["n"]) * (bound["x"] / bound["y"])),
                Soundness.Sound,
                description: "(-a * x) / y = -(a * (x / y))",
                // Five nodes become seven, by the same count as its two neighbours: operators
                // for operators, magnitude for literal, and the negation is the two. Measured
                // at +2 on all 63 firings over the corpus.
                growth: RewriteRuleGrowth.Expands),

            // y * (-a * x) -> -(a * (x * y)). Declines -1; see the left-product rule above.
            new MatchedRule(
                "a-negative-factor-in-a-right-product-comes-out",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any("y"),
                    MatchPattern.Commutative<Mulf>(
                        MatchPattern.Any<Real>("n", value => value.IsNegative && value != -1),
                        MatchPattern.Any("x"))),
                bound => -((-(Real)bound["n"]) * (bound["x"] * bound["y"])),
                Soundness.Sound,
                description: "y * (-a * x) = -(a * (x * y))",
                // Five nodes become seven, the mirror of the left-product rule above and by the
                // same count. Measured at +2 on all 84 firings over the corpus.
                growth: RewriteRuleGrowth.Expands),

            // y / (-a * x) -> -(y / (a * x)). What is left stays under the line: written the
            // other way, as the numerator rules above are, the quotient came back inverted --
            // https://github.com/asc-community/AngouriMath/issues/936 and the note on the switch.
            // Declines -1; see the left-product rule above, and note that this rule and the
            // numerator one are the pair that cycled.
            new MatchedRule(
                "a-negative-factor-in-a-denominator-comes-out",
                MatchPattern.Node<Divf>(
                    MatchPattern.Any("y"),
                    MatchPattern.Commutative<Mulf>(
                        MatchPattern.Any<Real>("n", value => value.IsNegative && value != -1),
                        MatchPattern.Any("x"))),
                bound => -(bound["y"] / ((-(Real)bound["n"]) * bound["x"])),
                Soundness.Sound,
                description: "y / (-a * x) = -(y / (a * x))",
                // Five nodes become seven, for every input: the quotient and the product stay
                // as they are, the literal is replaced by its magnitude, and the negation round
                // the whole of it is the two. Measured at +2 on all 84 firings over the corpus.
                growth: RewriteRuleGrowth.Expands));

        /// <summary>
        /// <see cref="Functions.Patterns.BooleanRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Thirty-six arms become sixteen</b>, and this set is why
        /// <a href="https://github.com/asc-community/AngouriMath/issues/248">#248</a> is worth
        /// having. Distributivity is written <b>eight times</b> in the <c>switch</c> — both
        /// distributive laws, each for the four ways the shared operand can sit inside two
        /// commutative pairs — and absorption another eight. A commutative pattern at both levels
        /// says each of those once.
        /// </para>
        /// <para>
        /// <b>Order is load-bearing throughout.</b> Excluded middle is tried before the general
        /// <c>¬a ∨ b = a → b</c>, which would otherwise swallow it; and that general rule comes
        /// before the constant-folding rules, so <c>¬x ∨ True</c> is <c>x → True</c> and not
        /// <c>True</c>. Both orderings are the <c>switch</c>'s, kept.
        /// </para>
        /// <para>
        /// Every rule asks <see cref="Functions.Patterns.IsLogic(Entity)"/> of what it binds, because
        /// these laws are about statements and a bare variable under the default reading is not
        /// one. That is a guard over several bindings, which no predicate on a single hole can
        /// express.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet Boolean { get; } = new(
            nameof(Boolean),

            // False -> b  is  True
            new MatchedRule(
                "anything-follows-from-a-falsehood",
                MatchPattern.Node<Impliesf>(
                    MatchPattern.Exact(Entity.Boolean.False), MatchPattern.Any("b")),
                bound => Entity.Boolean.True.Provided(bound["b"].DomainCondition),
                Soundness.Sound,
                when: bound => Functions.Patterns.IsLogic(bound["b"]),
                // Left Unknown: the truth value attached carries the operand's domain condition,
                // whose size nothing here bounds.
                description: "(False implies b) = True"),

            // De Morgan, both ways round
            new MatchedRule(
                "a-conjunction-of-negations-is-a-negated-disjunction",
                MatchPattern.Node<Andf>(
                    MatchPattern.Node<Notf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Notf>(MatchPattern.Any("b"))),
                bound => !(bound["a"] | bound["b"]),
                Soundness.Sound,
                // Two negations become one, and nothing else moves: three nodes around `a` and
                // `b` become two.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"], bound["b"]),
                description: "(not a) and (not b) = not (a or b)"),

            new MatchedRule(
                "a-disjunction-of-negations-is-a-negated-conjunction",
                MatchPattern.Node<Orf>(
                    MatchPattern.Node<Notf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Notf>(MatchPattern.Any("b"))),
                bound => !(bound["a"] & bound["b"]),
                Soundness.Sound,
                // Two negations become one, and nothing else moves: three nodes around `a` and
                // `b` become two.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"], bound["b"]),
                description: "(not a) or (not b) = not (a and b)"),

            // Excluded middle, either way round. Before the implication rule below, which would
            // otherwise take it -- and conditional, because a proposition without a truth value
            // has no excluded middle: `i < 0` is NaN.
            // https://github.com/asc-community/AngouriMath/issues/876
            new MatchedRule(
                "a-statement-or-its-negation-is-true-where-it-has-a-truth-value",
                MatchPattern.Commutative<Orf>(
                    MatchPattern.Node<Notf>(MatchPattern.Any("a")), MatchPattern.Any("a")),
                bound => Entity.Boolean.True.Provided(Functions.Patterns.TruthCondition(bound["a"])),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                // Left Unknown: the truth value attached carries the operand's truth condition,
                // whose size nothing here bounds.
                description: "((not a) or a) = True, where a has a truth value"),

            // Not commutative: `a or not b` is not an implication of anything.
            new MatchedRule(
                "a-negation-or-something-is-an-implication",
                MatchPattern.Node<Orf>(
                    MatchPattern.Node<Notf>(MatchPattern.Any("a")), MatchPattern.Any("b")),
                bound => bound["a"].Implies(bound["b"]),
                Soundness.Sound,
                // The `not` disappears and the `or` becomes an `->`: the same `a` and `b` under
                // one node instead of two.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"], bound["b"]),
                description: "((not a) or b) = (a implies b)"),

            // Idempotence
            new MatchedRule(
                "a-conjunction-with-itself-is-itself",
                MatchPattern.Node<Andf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => bound["a"],
                Soundness.Sound,
                // The replacement is the matched node's own child, unconditionally.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "(a and a) = a"),

            new MatchedRule(
                "a-disjunction-with-itself-is-itself",
                MatchPattern.Node<Orf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => bound["a"],
                Soundness.Sound,
                // The replacement is the matched node's own child, unconditionally.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "(a or a) = a"),

            new MatchedRule(
                "a-statement-implies-itself",
                MatchPattern.Node<Impliesf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Entity.Boolean.True,
                Soundness.Sound,
                // One leaf, from a node that had two copies of `a` under it.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "(a implies a) = True"),

            new MatchedRule(
                "a-statement-differs-from-itself-nowhere",
                MatchPattern.Node<Xorf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Entity.Boolean.False.Provided(bound["a"].DomainCondition),
                Soundness.Sound,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                // Left Unknown: the truth value attached carries the operand's domain condition,
                // whose size nothing here bounds.
                description: "(a xor a) = False"),

            new MatchedRule(
                "a-double-negation-cancels",
                MatchPattern.Node<Notf>(MatchPattern.Node<Notf>(MatchPattern.Any("a"))),
                bound => bound["a"],
                Soundness.Sound,
                // The replacement is the matched node's own grandchild, unconditionally.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "not (not a) = a"),

            // Constants, after the implication rule above: `not x or True` is `x -> True`.
            new MatchedRule(
                "a-disjunction-with-a-truth-is-true",
                MatchPattern.Node<Orf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                bound => Entity.Boolean.True
                    .Provided(bound["a"].DomainCondition).Provided(bound["b"].DomainCondition),
                Soundness.Sound,
                when: bound => (bound["a"] == Entity.Boolean.True || bound["b"] == Entity.Boolean.True)
                               && Functions.Patterns.IsLogic(bound["a"], bound["b"]),
                // Left Unknown: the truth value attached carries both operands' domain
                // conditions, whose size nothing here bounds.
                description: "(a or True) = True"),

            new MatchedRule(
                "a-conjunction-with-a-falsehood-is-false",
                MatchPattern.Node<Andf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                bound => Entity.Boolean.False
                    .Provided(bound["a"].DomainCondition).Provided(bound["b"].DomainCondition),
                Soundness.Sound,
                when: bound => (bound["a"] == Entity.Boolean.False || bound["b"] == Entity.Boolean.False)
                               && Functions.Patterns.IsLogic(bound["a"], bound["b"]),
                // Left Unknown: the truth value attached carries both operands' domain
                // conditions, whose size nothing here bounds.
                description: "(a and False) = False"),

            // Distributivity. Eight arms of the switch, two rules here: commutative at both
            // levels finds the shared operand wherever it sits.
            new MatchedRule(
                "a-disjunction-of-conjunctions-sharing-an-operand-distributes",
                MatchPattern.Commutative<Orf>(
                    MatchPattern.Commutative<Andf>(MatchPattern.Any("k"), MatchPattern.Any("p")),
                    MatchPattern.Commutative<Andf>(MatchPattern.Any("k"), MatchPattern.Any("q"))),
                bound => bound["k"] & (bound["p"] | bound["q"]),
                Soundness.Sound,
                when: bound => Functions.Patterns.IsLogic(bound["k"], bound["p"], bound["q"]),
                description: "((k and p) or (k and q)) = (k and (p or q))",
                // The shared operand is matched twice and written once, and the three connectives
                // stay three, so the delta is -(1 + |k|).
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-conjunction-of-disjunctions-sharing-an-operand-distributes",
                MatchPattern.Commutative<Andf>(
                    MatchPattern.Commutative<Orf>(MatchPattern.Any("k"), MatchPattern.Any("p")),
                    MatchPattern.Commutative<Orf>(MatchPattern.Any("k"), MatchPattern.Any("q"))),
                bound => bound["k"] | (bound["p"] & bound["q"]),
                Soundness.Sound,
                when: bound => Functions.Patterns.IsLogic(bound["k"], bound["p"], bound["q"]),
                description: "((k or p) and (k or q)) = (k or (p and q))",
                // The same count as its dual above: -(1 + |k|).
                growth: RewriteRuleGrowth.Collects),

            // Absorption, and the absorption that leaves something behind. Four arms each in the
            // switch, and the negated form four more.
            new MatchedRule(
                "a-disjunction-absorbs-a-conjunction-it-shares-an-operand-with",
                MatchPattern.Commutative<Orf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Commutative<Andf>(MatchPattern.Any("a"), MatchPattern.Any("_rest"))),
                bound => bound["a"],
                Soundness.Sound,
                // The replacement is one of the matched node's own operands, unconditionally.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "(a or (a and b)) = a"),

            new MatchedRule(
                "a-conjunction-absorbs-a-disjunction-it-shares-an-operand-with",
                MatchPattern.Commutative<Andf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Commutative<Orf>(MatchPattern.Any("a"), MatchPattern.Any("_rest"))),
                bound => bound["a"],
                Soundness.Sound,
                // The replacement is one of the matched node's own operands, unconditionally.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "(a and (a or b)) = a"),

            new MatchedRule(
                "a-disjunction-drops-a-negated-copy-of-its-other-operand",
                MatchPattern.Commutative<Orf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Commutative<Andf>(
                        MatchPattern.Node<Notf>(MatchPattern.Any("a")), MatchPattern.Any("b"))),
                bound => bound["a"] | bound["b"],
                Soundness.Sound,
                // The second copy of `a` and its `not` and the inner `and` all go; `a` and `b`
                // are left under the one `or`.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "(a or ((not a) and b)) = (a or b)"),

            new MatchedRule(
                "a-conjunction-drops-a-negated-copy-of-its-other-operand",
                MatchPattern.Commutative<Andf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Commutative<Orf>(
                        MatchPattern.Node<Notf>(MatchPattern.Any("a")), MatchPattern.Any("b"))),
                bound => bound["a"] & bound["b"],
                Soundness.Sound,
                // The second copy of `a` and its `not` and the inner `or` all go; `a` and `b`
                // are left under the one `and`.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"]),
                description: "(a and ((not a) or b)) = (a and b)"),

            // Exclusive disjunction, written four ways in the switch.
            new MatchedRule(
                "one-and-not-the-other-either-way-round-is-an-exclusive-disjunction",
                MatchPattern.Commutative<Orf>(
                    MatchPattern.Commutative<Andf>(
                        MatchPattern.Any("a"), MatchPattern.Node<Notf>(MatchPattern.Any("b"))),
                    MatchPattern.Commutative<Andf>(
                        MatchPattern.Any("b"), MatchPattern.Node<Notf>(MatchPattern.Any("a")))),
                bound => bound["a"] ^ bound["b"],
                Soundness.Sound,
                // Two `and`s, two `not`s and the duplicate `a` and `b` collapse into one `xor`
                // over one copy of each.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"], bound["b"]),
                description: "((a and not b) or (b and not a)) = (a xor b)"),

            // Contraposition
            new MatchedRule(
                "an-implication-between-negations-turns-round",
                MatchPattern.Node<Impliesf>(
                    MatchPattern.Node<Notf>(MatchPattern.Any("a")),
                    MatchPattern.Node<Notf>(MatchPattern.Any("b"))),
                bound => bound["b"].Implies(bound["a"]),
                Soundness.Sound,
                // The same `a` and `b` under the same `->`, swapped, with both `not`s dropped.
                growth: RewriteRuleGrowth.Collects,
                when: bound => Functions.Patterns.IsLogic(bound["a"], bound["b"]),
                description: "((not a) implies (not b)) = (b implies a)"));

        /// <summary>
        /// <see cref="Functions.Patterns.FactorizeRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Twenty-two arms become eleven.</b> Taking a common factor out of a sum is written
        /// four times — <c>k*p + k*q</c> for each of the four ways the shared factor can sit —
        /// and the difference four more, and the two "one of the terms <i>is</i> the factor"
        /// cases twice each. Commutative patterns say each once, at both levels where both
        /// levels are commutative.
        /// </para>
        /// <para>
        /// A difference is <b>not</b> commutative, so the outer pattern of every subtractive rule
        /// stays a plain node while its operands' products are matched either way round. That
        /// distinction is invisible in a <c>switch</c>, where both are just arms.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet Factorization { get; } = new(
            nameof(Factorization),

            // a^2n - b^2m -> (a^n - b^m)(a^n + b^m), both exponents even
            new MatchedRule(
                "a-difference-of-even-powers-splits",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any<Integer>("n")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any<Integer>("m"))),
                bound =>
                {
                    var halfN = Integer.Create(((Integer)bound["n"]).EInteger / 2);
                    var halfM = Integer.Create(((Integer)bound["m"]).EInteger / 2);
                    return (new Powf(bound["a"], halfN) - new Powf(bound["b"], halfM))
                         * (new Powf(bound["a"], halfN) + new Powf(bound["b"], halfM));
                },
                Soundness.Sound,
                // Both exponents even, or halving them introduces radicals and the rule fires
                // again on what it just produced.
                when: bound => ((Integer)bound["n"]).EInteger.IsEven
                               && ((Integer)bound["m"]).EInteger.IsEven,
                description: "a^2n - b^2m = (a^n - b^m)(a^n + b^m)",
                // Declared, because the replacement is code and nothing counts it: one difference becomes a product of two.
                growth: RewriteRuleGrowth.Expands),

            // a^2 - c -> (a - sqrt(c))(a + sqrt(c)), for a numeric c
            new MatchedRule(
                "a-square-less-a-number-splits",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Exact(Integer.Create(2))),
                    MatchPattern.Any<Number>("c")),
                bound => (bound["a"] - new Powf(bound["c"], Rational.Create(1, 2)))
                       * (bound["a"] + new Powf(bound["c"], Rational.Create(1, 2))),
                Soundness.SoundUnderAssumptions,
                description: "a^2 - c = (a - sqrt(c))(a + sqrt(c)), for a numeric c",
                // Declared, because the replacement is code and nothing counts it: one difference becomes a product of two, with a root introduced.
                growth: RewriteRuleGrowth.Expands),

            // k*p + k*q -> k*(p + q), the shared factor anywhere in either product
            new MatchedRule(
                "a-factor-shared-by-two-added-products-comes-out",
                MatchPattern.Commutative<Sumf>(
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("p")),
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("q"))),
                bound => bound["k"] * (bound["p"] + bound["q"]),
                Soundness.Sound,
                description: "k*p + k*q = k*(p + q)",
                // Declared, because the replacement is code and nothing counts it: k*p + k*q is seven nodes and k*(p + q) is five.
                growth: RewriteRuleGrowth.Collects),

            // k + k*q -> k*(1 + q), either way round at both levels
            new MatchedRule(
                "a-term-shared-with-a-product-added-to-it-comes-out",
                MatchPattern.Commutative<Sumf>(
                    MatchPattern.Any("k"),
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("q"))),
                bound => bound["k"] * (1 + bound["q"]),
                Soundness.Sound,
                description: "k + k*q = k*(1 + q)",
                // The shared term is matched twice and written once, and the 1 is one node more:
                // the delta is 1 - |k|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            // k + k -> 2k
            new MatchedRule(
                "a-term-added-to-itself-doubles",
                MatchPattern.Node<Sumf>(MatchPattern.Any("k"), MatchPattern.Any("k")),
                bound => 2 * bound["k"],
                Soundness.Sound,
                description: "k + k = 2k",
                // The term is matched twice and written once, and the 2 is one node more: the
                // delta is 1 - |k|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            // k*p - k*q -> k*(p - q). The outer node is a difference and stays one.
            new MatchedRule(
                "a-factor-shared-by-two-subtracted-products-comes-out",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("p")),
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("q"))),
                bound => bound["k"] * (bound["p"] - bound["q"]),
                Soundness.Sound,
                description: "k*p - k*q = k*(p - q)",
                // Declared, because the replacement is code and nothing counts it: as its added twin: seven nodes become five.
                growth: RewriteRuleGrowth.Collects),

            // k - k*q -> k*(1 - q)
            new MatchedRule(
                "a-product-subtracted-from-its-own-factor",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Any("k"),
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("q"))),
                bound => bound["k"] * (1 - bound["q"]),
                Soundness.Sound,
                description: "k - k*q = k*(1 - q)",
                // The shared term is matched twice and written once, and the 1 is one node more:
                // the delta is 1 - |k|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            // k*q - k -> k*(q - 1)
            new MatchedRule(
                "a-factor-subtracted-from-a-product-it-is-in",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("k"), MatchPattern.Any("q")),
                    MatchPattern.Any("k")),
                bound => bound["k"] * (bound["q"] - 1),
                Soundness.Sound,
                description: "k*q - k = k*(q - 1)",
                // The shared term is matched twice and written once, and the 1 is one node more:
                // the delta is 1 - |k|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            // k - k -> 0
            new MatchedRule(
                "a-term-subtracted-from-itself-vanishes",
                MatchPattern.Node<Minusf>(MatchPattern.Any("k"), MatchPattern.Any("k")),
                bound => Integer.Create(0).Provided(bound["k"].DomainCondition),
                Soundness.Sound,
                description: "k - k = 0",
                // Declared, because the replacement is code and nothing counts it: three nodes become one.
                growth: RewriteRuleGrowth.Collects),

            // a^b * c^b -> (a*c)^b, guarded as its twin in PowerRules is
            new MatchedRule(
                "two-powers-of-one-exponent-share-a-base",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("c"), MatchPattern.Any("b"))),
                bound => new Powf(bound["a"] * bound["c"], bound["b"]),
                // True for a whole exponent whatever the signs, and for positive real bases
                // whatever the exponent, and false outside those two: sqrt(x) * sqrt(y) became
                // sqrt(x * y), which at x = y = -1 is 1 where the product is -1.
                // https://github.com/asc-community/AngouriMath/issues/801
                Soundness.SoundUnderAssumptions,
                when: bound => bound["b"] is Integer
                               || (bound["a"].Evaled is Real { IsPositive: true }
                                   && bound["c"].Evaled is Real { IsPositive: true }),
                description: "a^b * c^b = (a*c)^b",
                // Declared, because the replacement is code and nothing counts it: a^b * c^b is seven nodes and (a*c)^b is five.
                growth: RewriteRuleGrowth.Collects),

            // Anything left, over a whole sum or difference rather than two of its terms. Last,
            // because it is the general case of the rules above it.
            new MatchedRule(
                "a-common-factor-is-collected-out-of-a-whole-sum",
                MatchPattern.Any<Entity>("x", node => node is Sumf or Minusf),
                (node, _) => Functions.Patterns.CollectCommonFactors(node) ?? node,
                Soundness.Sound,
                description: "k*p + k*q + ... = k*(p + q + ...), over a whole sum rather than two of its terms"));
                // Undeclared, and it used to say `Collects` on the grounds that the whole point of
                // it is to be smaller. Measured against the corpus it never shrinks and sometimes
                // grows by eight nodes: `-x + x + -1/2` becomes `x * (-1 + 1) + -1/2`, seven nodes
                // for seven. Collects wants every firing smaller, Rearranges wants every one the
                // same size, Expands wants every one bigger, and each of those has a
                // counterexample here -- so Unknown is the only one of the four that is true.
                // Making it collect for real means declining where it does not shrink, which is a
                // change to what the rule does rather than to what it says about itself, and the
                // folding that turns `x * (-1 + 1)` into zero would have to be checked first.

        /// <summary>
        /// <see cref="Functions.Patterns.TrigonometricRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <b>Forty-three arms become thirty-three.</b> Ten of the arms are five rules written
        /// twice, once for each order of a commutative pair — a sine times a cosine, an arcsine
        /// plus an arccosine, a tangent times a cotangent, and so on — and a commutative pattern
        /// says each once. The rest are genuinely distinct identities, several of them carrying
        /// the interval conditions that
        /// <a href="https://github.com/asc-community/AngouriMath/issues/884">#884</a> and
        /// <a href="https://github.com/asc-community/AngouriMath/issues/887">#887</a> are about,
        /// and those conditions are now attached to the rule they belong to rather than to the
        /// set.
        /// </remarks>
        internal static MatchedRuleSet Trigonometric { get; } = new(
            nameof(Trigonometric),

            new MatchedRule(
                "a-sine-times-a-cosine-of-one-angle-is-half-the-doubled-sine",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Sinf>(MatchPattern.Any("a")), MatchPattern.Node<Cosf>(MatchPattern.Any("a"))),
                bound => Rational.Create(1, 2) * new Sinf(2 * bound["a"]),
                Soundness.Sound,
                // Left Unknown: the angle is matched twice and written once, and the half and
                // the 2 are two nodes more, so the delta is 2 - |a| -- one larger at a leaf.
                // This is the collecting half of the inverse pair SaturationAblationTest names,
                // and it was never "never larger".
                description: "sin(a) * cos(a) = (1/2) * sin(2a)"),

            // arccos(x) is pi/2 - arcsin(x) by definition, over the whole plane, so this needs
            // no assumption.
            new MatchedRule(
                "arcsine-plus-arccosine-is-a-right-angle",
                MatchPattern.Commutative<Sumf>(MatchPattern.Node<Arcsinf>(MatchPattern.Any("a")), MatchPattern.Node<Arccosf>(MatchPattern.Any("a"))),
                bound => MathS.pi / 2,
                Soundness.Sound,
                description: "arcsin(a) + arccos(a) = pi/2",
                // Three nodes around two copies of the angle become the three of pi/2, so the
                // delta is -2|a| -- at most -2, and further the larger the angle is. The
                // replacement holds no operand, so nothing it is filled with can make it grow.
                growth: RewriteRuleGrowth.Collects),

            // This library's arccotan is arctan(1/x) with range (-pi/2, pi/2], so the sum is
            // pi/2 for non-negative x and -pi/2 for negative x -- not pi/2 unconditionally,
            // which was a wrong answer at every negative real.
            // https://github.com/asc-community/AngouriMath/issues/887
            new MatchedRule(
                "arctangent-plus-arccotangent-is-a-right-angle-with-the-sign-of-the-argument",
                MatchPattern.Commutative<Sumf>(MatchPattern.Node<Arctanf>(MatchPattern.Any("a")), MatchPattern.Node<Arccotanf>(MatchPattern.Any("a"))),
                bound => Functions.Patterns.ArctanPlusArccotan(bound["a"])!,
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.ArctanPlusArccotan(bound["a"]) is not null,
                // Left Unknown: the helper answers a constant where the sign of a is decided and
                // a piecewise sized by a where it is not.
                description: "arctan(a) + arccotan(a) = pi/2 for a >= 0, and -pi/2 for a < 0"),

            // Holds as written only while ab < 1: past that the sum leaves the range arctan
            // answers in and the identity is off by a whole pi. Both arguments have to be
            // numbers so that ab < 1 is a question with an answer.
            new MatchedRule(
                "two-arctangents-of-numbers-add-by-the-tangent-formula",
                MatchPattern.Node<Sumf>(MatchPattern.Node<Arctanf>(MatchPattern.Any<Real>("a")), MatchPattern.Node<Arctanf>(MatchPattern.Any<Real>("b"))),
                bound => MathS.Arctan((((Real)bound["a"] + (Real)bound["b"])
                    / (1 - (Real)bound["a"] * (Real)bound["b"])).InnerSimplified),
                Soundness.SoundUnderAssumptions,
                when: bound => ((Real)bound["a"] * (Real)bound["b"]).Evaled is Real product && product < 1,
                description: "arctan(a) + arctan(b) = arctan((a + b) / (1 - a*b)), while a*b < 1",
                // Both operands are matched as Real and so are leaves, and the tangent formula is
                // folded to a single one by the InnerSimplified on it, so five nodes become two:
                // exactly -3, for every pair the `when` admits.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "the-arctangent-of-root-three",
                MatchPattern.Node<Arctanf>(MatchPattern.Node<Powf>(MatchPattern.Exact(Integer.Create(3)), MatchPattern.Exact(Rational.Create(1, 2)))),
                bound => MathS.pi / 3,
                Soundness.Sound,
                description: "arctan(sqrt(3)) = pi/3",
                // The pattern binds nothing, so both sides are a fixed size and the delta is a
                // number rather than an expression in the operands.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "the-arctangent-of-one-over-root-three",
                MatchPattern.Node<Arctanf>(MatchPattern.Node<Divf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Node<Powf>(MatchPattern.Exact(Integer.Create(3)), MatchPattern.Exact(Rational.Create(1, 2))))),
                bound => MathS.pi / 6,
                Soundness.Sound,
                description: "arctan(1 / sqrt(3)) = pi/6",
                // The pattern binds nothing, so both sides are a fixed size and the delta is a
                // number rather than an expression in the operands.
                growth: RewriteRuleGrowth.Collects),

            // The cosecant's own condition has to be carried: 2cos(u) is a number where
            // sin(u) is zero and sin(2u) csc(u) is not.
            // https://github.com/asc-community/AngouriMath/issues/557
            new MatchedRule(
                "a-doubled-sine-times-a-cosecant-is-twice-the-cosine",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Sinf>(MatchPattern.Node<Mulf>(MatchPattern.Exact(Integer.Create(2)), MatchPattern.Any("a"))), MatchPattern.Node<Cosecantf>(MatchPattern.Any("a"))),
                bound => (2 * new Cosf(bound["a"])).Provided(new Cosecantf(bound["a"]).DomainCondition),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached is the cosecant's domain condition, whose
                // size nothing here bounds.
                description: "sin(2a) * cosec(a) = 2 * cos(a)"),

            new MatchedRule(
                "a-tangent-times-a-cotangent-of-one-angle-is-one",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Tanf>(MatchPattern.Any("a")), MatchPattern.Node<Cotanf>(MatchPattern.Any("a"))),
                bound => Integer.Create(1),
                Soundness.SoundUnderAssumptions,
                description: "tan(a) * cotan(a) = 1",
                // The replacement is the literal 1 and the pattern is three nodes around two
                // copies of the angle, so the delta is -(2 + 2|a|).
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "arcsine-of-a-sine-inside-its-own-interval",
                MatchPattern.Node<Arcsinf>(MatchPattern.Node<Sinf>(MatchPattern.Any("a"))),
                bound => bound["a"],
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.WithinHalfPi(bound["a"], closed: true),
                description: "arcsin(sin(a)) = a, for a in [-pi/2; pi/2]",
                // The function and its inverse both go and the angle is handed back untouched, so
                // two nodes disappear whatever the angle is: exactly -2.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "arccosine-of-a-cosine-inside-its-own-interval",
                MatchPattern.Node<Arccosf>(MatchPattern.Node<Cosf>(MatchPattern.Any("a"))),
                bound => bound["a"],
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.WithinZeroAndPi(bound["a"], closed: true),
                description: "arccos(cos(a)) = a, for a in [0; pi]",
                // The function and its inverse both go and the angle is handed back untouched, so
                // two nodes disappear whatever the angle is: exactly -2.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "arctangent-of-a-tangent-inside-its-own-interval",
                MatchPattern.Node<Arctanf>(MatchPattern.Node<Tanf>(MatchPattern.Any("a"))),
                bound => bound["a"],
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.WithinHalfPi(bound["a"], closed: false),
                description: "arctan(tan(a)) = a, for a in (-pi/2; pi/2)",
                // The function and its inverse both go and the angle is handed back untouched, so
                // two nodes disappear whatever the angle is: exactly -2.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "arccotangent-of-a-cotangent-inside-its-own-range",
                MatchPattern.Node<Arccotanf>(MatchPattern.Node<Cotanf>(MatchPattern.Any("a"))),
                bound => bound["a"],
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.WithinArccotanRange(bound["a"]),
                description: "arccotan(cotan(a)) = a, for a in arccotan's own range",
                // The function and its inverse both go and the angle is handed back untouched, so
                // two nodes disappear whatever the angle is: exactly -2.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-sine-of-an-arcsine",
                MatchPattern.Node<Sinf>(MatchPattern.Node<Arcsinf>(MatchPattern.Any("a"))),
                MatchPattern.Any("a"),
                Soundness.Sound,
                description: "sin(arcsin(a)) = a"),

            new MatchedRule(
                "a-cosine-of-an-arccosine",
                MatchPattern.Node<Cosf>(MatchPattern.Node<Arccosf>(MatchPattern.Any("a"))),
                MatchPattern.Any("a"),
                Soundness.Sound,
                description: "cos(arccos(a)) = a"),

            new MatchedRule(
                "a-tangent-of-an-arctangent",
                MatchPattern.Node<Tanf>(MatchPattern.Node<Arctanf>(MatchPattern.Any("a"))),
                MatchPattern.Any("a"),
                Soundness.Sound,
                description: "tan(arctan(a)) = a"),

            new MatchedRule(
                "a-cotangent-of-an-arccotangent",
                MatchPattern.Node<Cotanf>(MatchPattern.Node<Arccotanf>(MatchPattern.Any("a"))),
                MatchPattern.Any("a"),
                Soundness.Sound,
                description: "cotan(arccotan(a)) = a"),

            new MatchedRule(
                "a-squared-sine-and-cosine-of-one-angle-sum-to-one",
                MatchPattern.Commutative<Sumf>(MatchPattern.Node<Powf>(MatchPattern.Node<Sinf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2))), MatchPattern.Node<Powf>(MatchPattern.Node<Cosf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => Integer.Create(1),
                Soundness.Sound,
                description: "sin(a)^2 + cos(a)^2 = 1",
                // The replacement is the literal 1 and the pattern is seven nodes around two
                // copies of the angle, so the delta is -(6 + 2|a|): at most -8.
                growth: RewriteRuleGrowth.Collects),

            // Only this direction: rewriting cos^2 back as 1 - sin^2 would undo it as fast
            // as it fired.
            new MatchedRule(
                "one-less-a-squared-sine-is-a-squared-cosine",
                MatchPattern.Node<Minusf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Node<Powf>(MatchPattern.Node<Sinf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => new Powf(new Cosf(bound["a"]), 2),
                Soundness.Sound,
                description: "1 - sin(a)^2 = cos(a)^2",
                // Five nodes around the angle become three, exactly -2 for every input.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "one-less-a-squared-cosine-is-a-squared-sine",
                MatchPattern.Node<Minusf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Node<Powf>(MatchPattern.Node<Cosf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => new Powf(new Sinf(bound["a"]), 2),
                Soundness.Sound,
                description: "1 - cos(a)^2 = sin(a)^2",
                // Five nodes around the angle become three: the difference and the literal 1 go,
                // and the squared function stays a squared function. Exactly -2, for every input.
                growth: RewriteRuleGrowth.Collects),

            // The identity divided through by cos^2. Knowing the plain one and not these made
            // the answer depend on which of the three ways an expression happened to be
            // written -- https://github.com/asc-community/AngouriMath/issues/725
            new MatchedRule(
                "one-and-a-squared-tangent-make-a-squared-secant",
                MatchPattern.Commutative<Sumf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Node<Powf>(MatchPattern.Node<Tanf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => new Powf(new Secantf(bound["a"]), 2),
                Soundness.SoundUnderAssumptions,
                description: "1 + tan(a)^2 = sec(a)^2",
                // Five nodes around the angle become three, exactly -2 for every input.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "one-and-a-squared-cotangent-make-a-squared-cosecant",
                MatchPattern.Commutative<Sumf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Node<Powf>(MatchPattern.Node<Cotanf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => new Powf(new Cosecantf(bound["a"]), 2),
                Soundness.SoundUnderAssumptions,
                description: "1 + cotan(a)^2 = cosec(a)^2",
                // Five nodes around the angle become three, exactly -2 for every input.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-squared-secant-less-a-squared-tangent-is-one",
                MatchPattern.Node<Minusf>(MatchPattern.Node<Powf>(MatchPattern.Node<Secantf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2))), MatchPattern.Node<Powf>(MatchPattern.Node<Tanf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => Integer.Create(1),
                Soundness.SoundUnderAssumptions,
                description: "sec(a)^2 - tan(a)^2 = 1",
                // The replacement is the literal 1 and the pattern is seven nodes around two
                // copies of the angle, so the delta is -(6 + 2|a|): at most -8.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-squared-cosecant-less-a-squared-cotangent-is-one",
                MatchPattern.Node<Minusf>(MatchPattern.Node<Powf>(MatchPattern.Node<Cosecantf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2))), MatchPattern.Node<Powf>(MatchPattern.Node<Cotanf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => Integer.Create(1),
                Soundness.SoundUnderAssumptions,
                description: "cosec(a)^2 - cotan(a)^2 = 1",
                // The replacement is the literal 1 and the pattern is seven nodes around two
                // copies of the angle, so the delta is -(6 + 2|a|): at most -8.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-squared-sine-less-a-squared-cosine-turns-round",
                MatchPattern.Node<Minusf>(MatchPattern.Node<Powf>(MatchPattern.Node<Sinf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2))), MatchPattern.Node<Powf>(MatchPattern.Node<Cosf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => -1 * (new Powf(new Cosf(bound["a"]), 2) - new Powf(new Sinf(bound["a"]), 2)),
                Soundness.Sound,
                description: "sin(a)^2 - cos(a)^2 = -(cos(a)^2 - sin(a)^2)",
                // The difference is written the other way round, which is the same seven nodes, and
                // the negation round the whole of it is two more: exactly +2, for every input.
                growth: RewriteRuleGrowth.Expands),

            new MatchedRule(
                "a-squared-cosine-less-a-squared-sine-is-the-doubled-cosine",
                MatchPattern.Node<Minusf>(MatchPattern.Node<Powf>(MatchPattern.Node<Cosf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2))), MatchPattern.Node<Powf>(MatchPattern.Node<Sinf>(MatchPattern.Any("a")), MatchPattern.Exact(Integer.Create(2)))),
                bound => new Cosf(2 * bound["a"]),
                Soundness.Sound,
                description: "cos(a)^2 - sin(a)^2 = cos(2a)",
                // Seven nodes around two copies of the angle become three around one, so the delta
                // is -(4 + |a|): at most -5, and further the larger the angle is.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-quotient-by-a-secant-is-a-cosine",
                MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Node<Secantf>(MatchPattern.Any("b"))),
                bound => bound["a"] * bound["b"].Cos(),
                Soundness.SoundUnderAssumptions,
                description: "x / sec(a) = x * cos(a)",
                // A quotient becomes a product and a secant a cosine, one node for one.
                growth: RewriteRuleGrowth.Rearranges),

            new MatchedRule(
                "a-quotient-by-a-cosecant-is-a-sine",
                MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Node<Cosecantf>(MatchPattern.Any("b"))),
                bound => bound["a"] * bound["b"].Sin(),
                Soundness.SoundUnderAssumptions,
                description: "x / cosec(a) = x * sin(a)",
                // A quotient becomes a product and a cosecant a sine, one node for one, so the
                // two are the same size whatever they are filled with.
                growth: RewriteRuleGrowth.Rearranges),

            new MatchedRule(
                "a-secant-times-a-cosine-of-one-angle-is-one",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Secantf>(MatchPattern.Any("a")), MatchPattern.Node<Cosf>(MatchPattern.Any("a"))),
                bound => Integer.Create(1),
                Soundness.SoundUnderAssumptions,
                description: "sec(a) * cos(a) = 1",
                // The replacement is the literal 1 and the pattern is three nodes around two
                // copies of the angle, so the delta is -(2 + 2|a|): at most -4.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-cosecant-times-a-sine-of-one-angle-is-one",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Cosecantf>(MatchPattern.Any("a")), MatchPattern.Node<Sinf>(MatchPattern.Any("a"))),
                bound => Integer.Create(1),
                Soundness.SoundUnderAssumptions,
                description: "cosec(a) * sin(a) = 1",
                // The replacement is the literal 1 and the pattern is three nodes around two
                // copies of the angle, so the delta is -(2 + 2|a|): at most -4, and further the
                // larger the angle is.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "an-arcsine-of-a-numeric-reciprocal-is-an-arccosecant",
                MatchPattern.Node<Arcsinf>(MatchPattern.Node<Divf>(MatchPattern.Any("n"), MatchPattern.Any("d"))),
                bound => new Arccosecantf(bound["d"] / bound["n"]),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["n"] is Number && bound["d"] is not Number,
                description: "arcsin(1 / c) = arccosec(c), for a numeric c",
                // One inverse function becomes another and the quotient stays a quotient with its
                // operands the other way round, so the two are the same size whatever they are.
                growth: RewriteRuleGrowth.Rearranges),

            new MatchedRule(
                "an-arccosine-of-a-numeric-reciprocal-is-an-arcsecant",
                MatchPattern.Node<Arccosf>(MatchPattern.Node<Divf>(MatchPattern.Any("n"), MatchPattern.Any("d"))),
                bound => new Arcsecantf(bound["d"] / bound["n"]),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["n"] is Number && bound["d"] is not Number,
                description: "arccos(1 / c) = arcsec(c), for a numeric c",
                // One inverse function becomes another and the quotient stays a quotient with its
                // operands the other way round, so the two are the same size whatever they are.
                growth: RewriteRuleGrowth.Rearranges),

            new MatchedRule(
                "an-arccosecant-of-a-numeric-reciprocal-is-an-arcsine",
                MatchPattern.Node<Arccosecantf>(MatchPattern.Node<Divf>(MatchPattern.Any("n"), MatchPattern.Any("d"))),
                bound => new Arcsinf(bound["d"] / bound["n"]),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["n"] is Number && bound["d"] is not Number,
                description: "arccosec(1 / c) = arcsin(c), for a numeric c",
                // One inverse function becomes another and the quotient stays a quotient with its
                // operands the other way round, so the two are the same size whatever they are.
                growth: RewriteRuleGrowth.Rearranges),

            new MatchedRule(
                "an-arcsecant-of-a-numeric-reciprocal-is-an-arccosine",
                MatchPattern.Node<Arcsecantf>(MatchPattern.Node<Divf>(MatchPattern.Any("n"), MatchPattern.Any("d"))),
                bound => new Arccosf(bound["d"] / bound["n"]),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["n"] is Number && bound["d"] is not Number,
                description: "arcsec(1 / c) = arccos(c), for a numeric c",
                // One inverse function becomes another and the quotient stays a quotient with its
                // operands the other way round, so the two are the same size whatever they are.
                growth: RewriteRuleGrowth.Rearranges));

        /// <summary>
        /// <see cref="Functions.Patterns.SetOperatorRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The set that was expected to need something the matcher does not have, and did not.
        /// Two things were in doubt. An <see cref="Set.Interval"/> has <b>four</b> children where
        /// every other node pattern here has one or two — and it turns out a node pattern is not
        /// limited to two, so the arity was never the question; the rule binds the interval whole
        /// anyway, because its replacement wants the node rather than its parts.
        /// </para>
        /// <para>
        /// The other <b>was real, and was the first limitation of the matcher this file had to
        /// work around.</b> <c>{ x : x in S }</c> is <c>S</c>, and the <c>switch</c> says that by
        /// deconstructing <c>ConditionalSet(var v, Inf(var v, var s))</c> — a record
        /// deconstruction, which reads the <i>stored</i> <c>Var</c> and <c>Predicate</c>. The
        /// matcher walked <c>DirectChildren</c>, and a <see cref="Set.ConditionalSet"/> has
        /// <b>one</b> child there, its predicate, with the bound variable already renamed to a
        /// placeholder: <c>{ x : x in [0; 1] }</c> offers <c>%1 in [0; 1]</c> and nothing else.
        /// So the rule bound the whole set and took it apart in its replacement — honest rather
        /// than clever, but a rule whose shape was code again.
        /// </para>
        /// <para>
        /// It is data now. <c>MatchPattern.Binder</c> reads a binder's declared parts, and the
        /// repeated hole is what the <c>switch</c> wrote as <c>when v1 == v1a</c>. Only this node
        /// type needed it: every other binder in the language publishes the name it binds as an
        /// ordinary child.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1074">#1074</a>
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet SetOperator { get; } = new(
            nameof(SetOperator),

            // A /\ A = A
            new MatchedRule(
                "an-intersection-with-itself-is-itself",
                MatchPattern.Node<Set.Intersectionf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                MatchPattern.Any("a"),
                Soundness.Sound,
                description: "A /\\ A = A"),

            // A /\ (B \/ C) = (A /\ B) \/ (A /\ C), and the same with the union on the left.
            // Two rules rather than one commutative pattern, because each builds its answer with
            // the operands in the order it found them.
            new MatchedRule(
                "an-intersection-distributes-over-a-union-on-its-right",
                MatchPattern.Node<Set.Intersectionf>(
                    MatchPattern.Any<Set>("a"),
                    MatchPattern.Node<Set.Unionf>(
                        MatchPattern.Any<Set>("b"), MatchPattern.Any<Set>("c"))),
                bound => ((Set)bound["a"]).Intersect((Set)bound["b"])
                    .Unite(((Set)bound["a"]).Intersect((Set)bound["c"])),
                Soundness.Sound,
                description: "A /\\ (B \\/ C) = (A /\\ B) \\/ (A /\\ C)",
                // The set that is distributed over the union is matched once and written twice, so
                // the delta is +(1 + |A|): at least +2, and further the larger that set is.
                growth: RewriteRuleGrowth.Expands),

            new MatchedRule(
                "an-intersection-distributes-over-a-union-on-its-left",
                MatchPattern.Node<Set.Intersectionf>(
                    MatchPattern.Node<Set.Unionf>(
                        MatchPattern.Any<Set>("b"), MatchPattern.Any<Set>("c")),
                    MatchPattern.Any<Set>("a")),
                bound => ((Set)bound["b"]).Intersect((Set)bound["a"])
                    .Unite(((Set)bound["c"]).Intersect((Set)bound["a"])),
                Soundness.Sound,
                description: "(B \\/ C) /\\ A = (B /\\ A) \\/ (C /\\ A)",
                // The set that is distributed over the union is matched once and written twice,
                // so the delta is +(1 + |A|): at least +2, the mirror of the rule above.
                growth: RewriteRuleGrowth.Expands),

            // A \/ A = A
            new MatchedRule(
                "a-union-with-itself-is-itself",
                MatchPattern.Node<Set.Unionf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                MatchPattern.Any("a"),
                Soundness.Sound,
                description: "A \\/ A = A"),

            // A \ A = {}
            new MatchedRule(
                "a-set-less-itself-is-empty",
                MatchPattern.Node<Set.SetMinusf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Set.Empty,
                Soundness.Sound,
                description: "A \\ A = {}",
                // Everything the pattern matches goes and one node arrives, so the delta is -2|A|:
                // at most -2, and further the larger the set is.
                growth: RewriteRuleGrowth.Collects),

            // { x : x in S } = S. The repeated "v" is what the switch wrote as `when v1 == v1a`.
            new MatchedRule(
                "a-conditional-set-whose-condition-is-its-own-membership-is-that-set",
                MatchPattern.Binder<Set.ConditionalSet>(
                    "v", MatchPattern.Node<Set.Inf>(MatchPattern.Any("v"), MatchPattern.Any("s"))),
                MatchPattern.Any("s"),
                Soundness.Sound,
                description: "{ x : x in S } = S"),

            // x in {a} = (x = a)
            new MatchedRule(
                "membership-of-a-singleton-is-an-equality",
                MatchPattern.Node<Set.Inf>(
                    MatchPattern.Any("x"),
                    MatchPattern.Any<Set.FiniteSet>("s", finite => finite.Count == 1)),
                bound => bound["x"].EqualTo(((Set.FiniteSet)bound["s"]).First()),
                Soundness.Sound,
                description: "x in {a} = (x = a)",
                // The singleton set goes and the membership becomes an equality, one node for one,
                // so exactly -1. `EqualTo` is a bare `Equalsf` and does not chain.
                growth: RewriteRuleGrowth.Collects),

            // x in (a; b) is written out as the inequalities it stands for
            new MatchedRule(
                "membership-of-an-interval-is-written-out",
                MatchPattern.Node<Set.Inf>(
                    MatchPattern.Any<Entity>("x", node => node is not Set and not Matrix),
                    MatchPattern.Any<Set.Interval>("i")),
                bound =>
                {
                    var interval = (Set.Interval)bound["i"];
                    return Simplificator.ParaphraseInterval(
                        bound["x"], interval.Left, interval.LeftClosed,
                        interval.Right, interval.RightClosed);
                },
                Soundness.Sound,
                // Left Unknown: a bounded interval becomes two comparisons joined, 1 + |x| more
                // than the membership; a half-line becomes one comparison, two fewer.
                description: "x in (a; b) = the inequalities it stands for"),

            // { True, False } is the boolean domain
            new MatchedRule(
                "the-two-truth-values-are-the-boolean-domain",
                MatchPattern.Any<Set.FiniteSet>(
                    "s", finite => finite == Functions.Patterns.FullBooleanSet),
                bound => Set.SpecialSet.Create(Domain.Boolean),
                Soundness.Sound,
                description: "{ True, False } = the boolean domain",
                // The pattern binds nothing, so both sides are a fixed size and the delta is a
                // number rather than an expression in the operands.
                growth: RewriteRuleGrowth.Collects),

            // (-oo; +oo) is the domain it is an interval of, where that domain names a set.
            // The Any case is refused in the condition rather than left to the replacement:
            // MatchedRule.Build swallows what the right-hand side throws, so without this the
            // rule declined by accident on an interval widened to "no constraint" -- which is
            // the right answer arrived at by an exception. There is no set of Domain.Any.
            // https://github.com/asc-community/AngouriMath/issues/996
            new MatchedRule(
                "an-unbounded-interval-is-a-whole-domain",
                MatchPattern.Any<Set.Interval>(
                    "i", interval => interval.Left == Real.NegativeInfinity
                                     && interval.Right == Real.PositiveInfinity
                                     && interval.Codomain is not Domain.Any),
                bound => Set.SpecialSet.Create(((Set.Interval)bound["i"]).Codomain),
                Soundness.Sound,
                description: "(-oo; +oo) = the domain it is an interval of",
                // The interval and its two bounds become the one node of a special set, and the
                // bounds are matched exactly rather than bound, so the delta is a number.
                growth: RewriteRuleGrowth.Collects));

        /// <summary>
        /// <see cref="Functions.Patterns.SortRules"/>, as data — one set per sort level.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The first set here that is parameterised by something other than the expression.</b>
        /// A <c>switch</c> takes that as a second argument and closes over it; a set of rules
        /// closes over it too, but the set itself then has to be built per value rather than
        /// declared once.
        /// </para>
        /// <para>
        /// <b>And it is not wired, because the exchange is not free here — measured, and
        /// re-measured.</b> The three canonical orders are the <i>normalisation</i>: they run on
        /// every node of every simplification pass, where every other set fires on a shape.
        /// </para>
        /// <para>
        /// The figure first recorded here was <b>+48% of <c>SimplifyEasy</c></b>, which the
        /// kernel gate reported as 4.14x on a shared runner. <b>That number has since stopped
        /// being true</b>, and it was only ever a statement about the matcher of the day: with
        /// bounded matching (<a href="https://github.com/asc-community/AngouriMath/issues/1079">#1079</a>)
        /// and rules indexed by node type (#1085) the same wiring measures <b>+13.3%</b> —
        /// 97,048 ns to 109,968 ns on the repository's own benchmark, with allocation +0.28% and
        /// inside the gate's band. The decision is unchanged and the reason for it is a third of
        /// what it was.
        /// </para>
        /// <para>
        /// What is left is not dispatch across the rules. Every rule here is typed —
        /// <c>Any&lt;Sumf&gt;</c>, <c>Any&lt;Mulf&gt;</c> — so the index tries one or two of them
        /// at a node, not seven. It is the layer itself: a rule is a match that binds a name and
        /// a delegate that reads it back, where a <c>switch</c> arm is a type test and a call.
        /// Everywhere else that layer buys something — a pattern that says what the rewrite is,
        /// reversible, addressable. Here every rule is <i>a type test and a call already</i>, so
        /// there is nothing for it to buy. <b>That is the boundary, and it is about what a rule
        /// is rather than about where it runs.</b>
        /// </para>
        /// <para>
        /// Giving each rule a concrete root type instead of a predicate over
        /// <c>Any&lt;Entity&gt;</c> was tried when the figure was 48%, on the reading that a hole
        /// typed <c>Entity</c> matches every node before its predicate is consulted. It moved
        /// +52% to +48%, so that was not the cost either.
        /// </para>
        /// <para>
        /// So this stays here, proven to agree with the <c>switch</c> at every level, and the
        /// <c>switch</c> keeps running. It is the one set where the answer to "should this be
        /// data?" is no, and the reason is a number rather than a preference — a number that has
        /// to be re-measured when the matcher changes, since it already has been once.
        /// </para>
        /// <para>
        /// Every rule is a bare type test with the whole node bound, which is a typed hole — and
        /// two of the seven test <i>two</i> types, a sum being either <c>Sumf</c> or
        /// <c>Minusf</c> and a product either <c>Mulf</c> or <c>Divf</c>, which is the predicate
        /// on a hole again. All seven replacements are code: sorting a chain is not a tree
        /// written down.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet Sort(TreeAnalyzer.SortLevel level) => new(
            nameof(Sort) + "." + level,

            // A sum chain is either spelling, and each is its own rule rather than one rule over
            // `Sumf or Minusf`. A predicate on a hole of type Entity matches every node in the
            // tree before the predicate is consulted, which defeats the root-type dispatch that
            // makes a miss cheap -- and this set runs on every node of every pass, so that is
            // the difference between an exchange that is free and one that is not.
            new MatchedRule(
                "a-sum-chain-is-sorted-and-grouped",
                MatchPattern.Any<Sumf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Sumf.LinearChildren(node), level, (a, b) => a + b),
                Soundness.SoundUnderAssumptions,
                description: "the terms of a sum are put in canonical order and like terms are grouped"),

            new MatchedRule(
                "a-difference-chain-is-sorted-and-grouped",
                MatchPattern.Any<Minusf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Sumf.LinearChildren(node), level, (a, b) => a + b),
                Soundness.SoundUnderAssumptions,
                description: "a difference is read as a sum, whose terms are put in canonical order and grouped"),

            new MatchedRule(
                "a-product-chain-is-sorted-and-grouped",
                MatchPattern.Any<Mulf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Mulf.LinearChildren(node), level, (a, b) => a * b),
                // Regrouping reads a quotient as a product with a negative power, which is the
                // same value wherever the divisor is not zero.
                Soundness.SoundUnderAssumptions,
                description: "the factors of a product are put in canonical order and like factors are grouped"),

            new MatchedRule(
                "a-quotient-chain-is-sorted-and-grouped",
                MatchPattern.Any<Divf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Mulf.LinearChildren(node), level, (a, b) => a * b),
                Soundness.SoundUnderAssumptions,
                description: "a quotient is read as a product, whose factors are put in canonical order and grouped"),

            new MatchedRule(
                "a-conjunction-chain-is-sorted-and-grouped",
                MatchPattern.Any<Andf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Andf.LinearChildren(node), level, (a, b) => a & b),
                Soundness.Sound,
                description: "the operands of a conjunction are put in canonical order and repeats are grouped"),

            new MatchedRule(
                "a-disjunction-chain-is-sorted-and-grouped",
                MatchPattern.Any<Orf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Orf.LinearChildren(node), level, (a, b) => a | b),
                Soundness.Sound,
                description: "the operands of a disjunction are put in canonical order and repeats are grouped"),

            new MatchedRule(
                "a-union-chain-is-sorted-and-grouped",
                MatchPattern.Any<Set.Unionf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Set.Unionf.LinearChildren(node), level, (a, b) => a.Unite(b)),
                Soundness.Sound,
                description: "the operands of a union are put in canonical order and repeats are grouped"),

            new MatchedRule(
                "an-intersection-chain-is-sorted-and-grouped",
                MatchPattern.Any<Set.Intersectionf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Set.Intersectionf.LinearChildren(node), level, (a, b) => a.Intersect(b)),
                Soundness.Sound,
                description: "the operands of an intersection are put in canonical order and repeats are grouped"),

            new MatchedRule(
                "an-exclusive-disjunction-chain-is-sorted-and-grouped",
                MatchPattern.Any<Xorf>("x"),
                (node, _) => Functions.Patterns.SortAndGroup(
                    node, Xorf.LinearChildren(node), level, (a, b) => a ^ b),
                Soundness.Sound,
                description: "the operands of an exclusive disjunction are put in canonical order and repeats are grouped"));

        /// <summary>
        /// <see cref="Functions.Patterns.FractionCommonDenominatorRules"/>, as data — again one
        /// set per sort level.
        /// </summary>
        internal static MatchedRuleSet CommonDenominator(TreeAnalyzer.SortLevel level) => new(
            nameof(CommonDenominator) + "." + level,

            new MatchedRule(
                "two-added-fractions-take-a-common-denominator",
                MatchPattern.Node<Sumf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("ln"), MatchPattern.Any("ld")),
                    MatchPattern.Node<Divf>(MatchPattern.Any("rn"), MatchPattern.Any("rd"))),
                (node, bound) => Functions.Patterns.SumOfFractions(
                    node, bound["ln"], bound["ld"], bound["rn"], bound["rd"]),
                Soundness.SoundUnderAssumptions,
                description: "ln / ld + rn / rd = one quotient over a common denominator"),

            new MatchedRule(
                "two-subtracted-fractions-take-a-common-denominator",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("ln"), MatchPattern.Any("ld")),
                    MatchPattern.Node<Divf>(MatchPattern.Any("rn"), MatchPattern.Any("rd"))),
                (node, bound) => Functions.Patterns.SumOfFractions(
                    node, bound["ln"], bound["ld"], -bound["rn"], bound["rd"]),
                Soundness.SoundUnderAssumptions,
                description: "ln / ld - rn / rd = one quotient over a common denominator"),

            new MatchedRule(
                "a-quotient-of-symbolic-parts-is-grouped-pairwise",
                MatchPattern.Node<Divf>(MatchPattern.Any("num"), MatchPattern.Any("den")),
                (node, bound) => Functions.Patterns.PairwiseGroupedQuotient(
                    node, bound["num"], bound["den"], level),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["num"].Vars.Any() && bound["den"].Vars.Any(),
                description: "num / den = the quotient with shared factors paired off"));

        /// <summary>
        /// <see cref="Functions.Patterns.InequalityEqualityRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Sixty-five arms, and the set where transcription found a <b>wrong answer</b>: four of
        /// the eight <c>or</c>-with-equality arms carried their neighbour's comparison, so
        /// <c>(y &lt; x) or (x = y)</c> simplified to <c>x &lt;= y</c> — the negation of itself
        /// off the diagonal. Fixed on its own before this
        /// (<a href="https://github.com/asc-community/AngouriMath/issues/1077">#1077</a>), so what
        /// is here agrees with a <c>switch</c> that is right.
        /// </para>
        /// <para>
        /// Three things this set needs that a pattern alone does not say. The two De Morgan arms
        /// are a <b>fold over a chain of any length</b> rather than a shape, so the rule matches
        /// the chain and the fold stays in <c>Patterns.EqualityInequality.cs</c> where both forms
        /// ask it. The excluded-middle pair reads <i>two</i> bound comparisons against each other
        /// — same operands, opposite or exhaustive signs — which is a <c>when</c> over the
        /// bindings rather than a shape. And the conditions those two attach are about where the
        /// ordering is defined at all, since <c>i &lt; 0</c> is <c>NaN</c> rather than false.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet InequalityEquality { get; } = new(
            nameof(InequalityEquality),

            new MatchedRule(
                "a-less-than-or-equal-as-written-is-at-most",
                MatchPattern.Node<Orf>(MatchPattern.Node<Lessf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] <= bound["b"],
                Soundness.Sound,
                description: "((a < b) or (a = b)) = (a <= b)"),
            new MatchedRule(
                "a-less-than-or-equal-the-other-way-round-is-at-least",
                MatchPattern.Node<Orf>(MatchPattern.Node<Lessf>(MatchPattern.Any("b"), MatchPattern.Any("a")), MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] >= bound["b"],
                Soundness.Sound,
                description: "((b < a) or (a = b)) = (a >= b)"),
            new MatchedRule(
                "a-greater-than-or-equal-as-written-is-at-least",
                MatchPattern.Node<Orf>(MatchPattern.Node<Greaterf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] >= bound["b"],
                Soundness.Sound,
                description: "((a > b) or (a = b)) = (a >= b)"),
            new MatchedRule(
                "a-greater-than-or-equal-the-other-way-round-is-at-most",
                MatchPattern.Node<Orf>(MatchPattern.Node<Greaterf>(MatchPattern.Any("b"), MatchPattern.Any("a")), MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] <= bound["b"],
                Soundness.Sound,
                description: "((b > a) or (a = b)) = (a <= b)"),
            new MatchedRule(
                "an-equality-or-a-less-than-as-written-is-at-most",
                MatchPattern.Node<Orf>(MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Lessf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] <= bound["b"],
                Soundness.Sound,
                description: "((a = b) or (a < b)) = (a <= b)"),
            new MatchedRule(
                "an-equality-or-a-less-than-the-other-way-round-is-at-least",
                MatchPattern.Node<Orf>(MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Lessf>(MatchPattern.Any("b"), MatchPattern.Any("a"))),
                bound => bound["a"] >= bound["b"],
                Soundness.Sound,
                description: "((a = b) or (b < a)) = (a >= b)"),
            new MatchedRule(
                "an-equality-or-a-greater-than-as-written-is-at-least",
                MatchPattern.Node<Orf>(MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Greaterf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] >= bound["b"],
                Soundness.Sound,
                description: "((a = b) or (a > b)) = (a >= b)"),
            new MatchedRule(
                "an-equality-or-a-greater-than-the-other-way-round-is-at-most",
                MatchPattern.Node<Orf>(MatchPattern.Node<Equalsf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Greaterf>(MatchPattern.Any("b"), MatchPattern.Any("a"))),
                bound => bound["a"] <= bound["b"],
                Soundness.Sound,
                description: "((a = b) or (b > a)) = (a <= b)"),
            new MatchedRule(
                "the-negation-of-a-greater-turns-it-round",
                MatchPattern.Node<Notf>(MatchPattern.Node<Greaterf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] <= bound["b"],
                Soundness.Sound,
                description: "not (a > b) = (a <= b)"),
            new MatchedRule(
                "the-negation-of-a-less-turns-it-round",
                MatchPattern.Node<Notf>(MatchPattern.Node<Lessf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] >= bound["b"],
                Soundness.Sound,
                description: "not (a < b) = (a >= b)"),
            new MatchedRule(
                "the-negation-of-a-greaterorequal-turns-it-round",
                MatchPattern.Node<Notf>(MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] < bound["b"],
                Soundness.Sound,
                description: "not (a >= b) = (a < b)"),
            new MatchedRule(
                "the-negation-of-a-lessorequal-turns-it-round",
                MatchPattern.Node<Notf>(MatchPattern.Node<LessOrEqualf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] > bound["b"],
                Soundness.Sound,
                description: "not (a <= b) = (a > b)"),
            // De Morgan over a chain of any length, which is a fold rather than a shape -- so the rule
            // matches the chain and the fold stays in `Patterns.EqualityInequality.cs`, asked by both
            // forms.
            new MatchedRule(
                "a-negated-conjunction-becomes-a-disjunction-of-negations",
                MatchPattern.Node<Notf>(MatchPattern.Any<Andf>("chain", chain => Functions.Patterns.MayPushNotInside(Andf.LinearChildren(chain), insideConjunction: true))),
                bound => Functions.Patterns.PushNotInside(Andf.LinearChildren((Andf)bound["chain"]), disjoin: true),
                Soundness.Sound,
                description: "not (a and b and ...) = (not a) or (not b) or ..."),
            new MatchedRule(
                "a-negated-disjunction-becomes-a-conjunction-of-negations",
                MatchPattern.Node<Notf>(MatchPattern.Any<Orf>("chain", chain => Functions.Patterns.MayPushNotInside(Orf.LinearChildren(chain), insideConjunction: false))),
                bound => Functions.Patterns.PushNotInside(Orf.LinearChildren((Orf)bound["chain"]), disjoin: false),
                Soundness.Sound,
                description: "not (a or b or ...) = (not a) and (not b) and ..."),
            new MatchedRule(
                "a-chain-of-greaters-implies-its-own-ends",
                MatchPattern.Node<Impliesf>(MatchPattern.Node<Andf>(MatchPattern.Node<Greaterf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Greaterf>(MatchPattern.Any("b"), MatchPattern.Any("c"))), MatchPattern.Node<Greaterf>(MatchPattern.Any("a"), MatchPattern.Any("c"))),
                bound => Entity.Boolean.True.Provided(bound["a"].DomainCondition).Provided(bound["b"].DomainCondition).Provided(bound["c"].DomainCondition),
                Soundness.SoundUnderAssumptions,
                description: "(((a > b) and (b > c)) implies (a > c)) = True"),
            new MatchedRule(
                "a-chain-of-lesss-implies-its-own-ends",
                MatchPattern.Node<Impliesf>(MatchPattern.Node<Andf>(MatchPattern.Node<Lessf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Lessf>(MatchPattern.Any("b"), MatchPattern.Any("c"))), MatchPattern.Node<Lessf>(MatchPattern.Any("a"), MatchPattern.Any("c"))),
                bound => Entity.Boolean.True.Provided(bound["a"].DomainCondition).Provided(bound["b"].DomainCondition).Provided(bound["c"].DomainCondition),
                Soundness.SoundUnderAssumptions,
                description: "(((a < b) and (b < c)) implies (a < c)) = True"),
            new MatchedRule(
                "a-equals-with-zero-on-the-left-turns-round",
                MatchPattern.Node<Equalsf>(MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one)), MatchPattern.Any<Entity>("other", one => !Functions.Patterns.IsZeroReal(one))),
                bound => bound["other"].EqualTo(bound["zero"]),
                Soundness.Sound,
                // `EqualTo` is a bare `Equalsf` -- it never chains the way `Equalizes` and the
                // comparison operators do -- so this is the same two operands under the same
                // node, swapped.
                growth: RewriteRuleGrowth.Rearranges,
                description: "(0 = a) = (a = 0)"),
            new MatchedRule(
                "a-greater-with-zero-on-the-left-turns-round",
                MatchPattern.Node<Greaterf>(MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one)), MatchPattern.Any<Entity>("other", one => !Functions.Patterns.IsZeroReal(one))),
                bound => bound["other"] < bound["zero"],
                Soundness.Sound,
                description: "(0 > a) = (a < 0)"),
            new MatchedRule(
                "a-less-with-zero-on-the-left-turns-round",
                MatchPattern.Node<Lessf>(MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one)), MatchPattern.Any<Entity>("other", one => !Functions.Patterns.IsZeroReal(one))),
                bound => bound["other"] > bound["zero"],
                Soundness.Sound,
                description: "(0 < a) = (a > 0)"),
            new MatchedRule(
                "a-greaterorequal-with-zero-on-the-left-turns-round",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one)), MatchPattern.Any<Entity>("other", one => !Functions.Patterns.IsZeroReal(one))),
                bound => bound["other"] <= bound["zero"],
                Soundness.Sound,
                description: "(0 >= a) = (a <= 0)"),
            new MatchedRule(
                "a-lessorequal-with-zero-on-the-left-turns-round",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one)), MatchPattern.Any<Entity>("other", one => !Functions.Patterns.IsZeroReal(one))),
                bound => bound["other"] >= bound["zero"],
                Soundness.Sound,
                description: "(0 <= a) = (a >= 0)"),
            new MatchedRule(
                "a-equals-with-a-number-on-the-left-turns-round",
                MatchPattern.Node<Equalsf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Entity>("other", one => one is not Number)),
                bound => bound["other"].EqualTo(bound["c"]),
                Soundness.Sound,
                // `EqualTo` is a bare `Equalsf` -- it never chains the way `Equalizes` and the
                // comparison operators do -- so this is the same two operands under the same
                // node, swapped.
                growth: RewriteRuleGrowth.Rearranges,
                description: "(c = a) = (a = c), for a numeric c"),
            new MatchedRule(
                "a-greater-with-a-number-on-the-left-turns-round",
                MatchPattern.Node<Greaterf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Entity>("other", one => one is not Number)),
                bound => bound["other"] < bound["c"],
                Soundness.Sound,
                description: "(c > a) = (a < c), for a numeric c"),
            new MatchedRule(
                "a-less-with-a-number-on-the-left-turns-round",
                MatchPattern.Node<Lessf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Entity>("other", one => one is not Number)),
                bound => bound["other"] > bound["c"],
                Soundness.Sound,
                description: "(c < a) = (a > c), for a numeric c"),
            new MatchedRule(
                "a-greaterorequal-with-a-number-on-the-left-turns-round",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Entity>("other", one => one is not Number)),
                bound => bound["other"] <= bound["c"],
                Soundness.Sound,
                description: "(c >= a) = (a <= c), for a numeric c"),
            new MatchedRule(
                "a-lessorequal-with-a-number-on-the-left-turns-round",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Entity>("other", one => one is not Number)),
                bound => bound["other"] >= bound["c"],
                Soundness.Sound,
                description: "(c <= a) = (a >= c), for a numeric c"),
            new MatchedRule(
                "two-comparisons-of-one-pair-that-exclude-each-other-are-false",
                MatchPattern.Node<Andf>(MatchPattern.Any<ComparisonSign>("left"), MatchPattern.Any<ComparisonSign>("right")),
                bound => Entity.Boolean.False.Provided(Functions.Patterns.OrderedConditionOf(bound["left"], 0)).Provided(Functions.Patterns.OrderedConditionOf(bound["left"], 1)),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.SameOperands(bound["left"], bound["right"]) && Functions.Patterns.HaveOppositeSigns(bound["left"], bound["right"]),
                description: "(left and right) = False, where the two comparisons leave no case"),
            // The other half of the same law. The unsatisfiable conjunction above was decided and the
            // valid disjunction was not -- half of excluded middle. https://github.com/asc-
            // community/AngouriMath/issues/876
            new MatchedRule(
                "two-comparisons-of-one-pair-that-leave-no-case-are-true",
                MatchPattern.Node<Orf>(MatchPattern.Any<ComparisonSign>("left"), MatchPattern.Any<ComparisonSign>("right")),
                bound => Entity.Boolean.True.Provided(Functions.Patterns.OrderedConditionOf(bound["left"], 0)).Provided(Functions.Patterns.OrderedConditionOf(bound["left"], 1)),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.SameOperands(bound["left"], bound["right"]) && Functions.Patterns.HaveExhaustiveSigns(bound["left"], bound["right"]),
                description: "(left or right) = True, where the two comparisons cover every case"),
            new MatchedRule(
                "a-power-with-a-real-positive-exponent-is-zero-when-its-base-is",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("p", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"].EqualTo(bound["zero"]),
                Soundness.SoundUnderAssumptions,
                description: "(a ^ p = 0) = (a = 0), for a real p above zero",
                // The power goes and its exponent with it, leaving the base against the same zero:
                // the delta is -(1 + |p|), at most -2. `EqualTo` is a bare `Equalsf` and does not
                // chain the way the comparison operators do.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-reciprocal-is-never-zero",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Divf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Any("e")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => new Providedf(false, !bound["e"].EqualTo(0)),
                Soundness.SoundUnderAssumptions,
                description: "(1 / e = 0) = False, provided e is not zero"),
            new MatchedRule(
                "a-positive-factor-first-drops-out-of-a-equals-with-zero",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"].EqualTo(Integer.Zero),
                Soundness.Sound,
                description: "(k * a = 0) = (a = 0), for a positive real k",
                // The factor and the zero on the right both go and nothing arrives, so the delta
                // is -(|k| + |zero|): at most -2, and further the larger either is. The
                // replacement is built with `EqualTo`, which is a bare `Equalsf` and does not
                // chain the way the comparison operators do -- which is why the equals rule of
                // this family can say its growth and its five comparison siblings cannot.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-positive-factor-first-drops-out-of-a-greater-with-zero",
                MatchPattern.Node<Greaterf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] > Integer.Zero,
                Soundness.Sound,
                description: "(k * a > 0) = (a > 0), for a positive real k"),
            new MatchedRule(
                "a-positive-factor-first-drops-out-of-a-greaterorequal-with-zero",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] >= Integer.Zero,
                Soundness.Sound,
                description: "(k * a >= 0) = (a >= 0), for a positive real k"),
            new MatchedRule(
                "a-positive-factor-first-drops-out-of-a-less-with-zero",
                MatchPattern.Node<Lessf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] < Integer.Zero,
                Soundness.Sound,
                description: "(k * a < 0) = (a < 0), for a positive real k"),
            new MatchedRule(
                "a-positive-factor-first-drops-out-of-a-lessorequal-with-zero",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] <= Integer.Zero,
                Soundness.Sound,
                description: "(k * a <= 0) = (a <= 0), for a positive real k"),
            new MatchedRule(
                "a-positive-factor-second-drops-out-of-a-equals-with-zero",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"].EqualTo(Integer.Zero),
                Soundness.Sound,
                description: "(a * k = 0) = (a = 0), for a positive real k",
                // The factor and the zero on the right both go, so the delta is -(|k| + |zero|).
                // `EqualTo` does not chain, so nothing larger can arrive in their place.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-positive-factor-second-drops-out-of-a-greater-with-zero",
                MatchPattern.Node<Greaterf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] > Integer.Zero,
                Soundness.Sound,
                description: "(a * k > 0) = (a > 0), for a positive real k"),
            new MatchedRule(
                "a-positive-factor-second-drops-out-of-a-greaterorequal-with-zero",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] >= Integer.Zero,
                Soundness.Sound,
                description: "(a * k >= 0) = (a >= 0), for a positive real k"),
            new MatchedRule(
                "a-positive-factor-second-drops-out-of-a-less-with-zero",
                MatchPattern.Node<Lessf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] < Integer.Zero,
                Soundness.Sound,
                description: "(a * k < 0) = (a < 0), for a positive real k"),
            new MatchedRule(
                "a-positive-factor-second-drops-out-of-a-lessorequal-with-zero",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] <= Integer.Zero,
                Soundness.Sound,
                description: "(a * k <= 0) = (a <= 0), for a positive real k"),
            new MatchedRule(
                "a-negative-factor-first-drops-out-of-a-equals-with-zero",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"].EqualTo(Integer.Zero),
                Soundness.Sound,
                description: "(k * a = 0) = (a = 0), for a negative real k",
                // The factor and the zero on the right both go, so the delta is -(|k| + |zero|).
                // `EqualTo` does not chain, so nothing larger can arrive in their place.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-negative-factor-first-drops-out-of-a-greater-with-zero",
                MatchPattern.Node<Greaterf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] < Integer.Zero,
                Soundness.Sound,
                description: "(k * a > 0) = (a < 0), for a negative real k"),
            new MatchedRule(
                "a-negative-factor-first-drops-out-of-a-greaterorequal-with-zero",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] <= Integer.Zero,
                Soundness.Sound,
                description: "(k * a >= 0) = (a <= 0), for a negative real k"),
            new MatchedRule(
                "a-negative-factor-first-drops-out-of-a-less-with-zero",
                MatchPattern.Node<Lessf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] > Integer.Zero,
                Soundness.Sound,
                description: "(k * a < 0) = (a > 0), for a negative real k"),
            new MatchedRule(
                "a-negative-factor-first-drops-out-of-a-lessorequal-with-zero",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0)), MatchPattern.Any("a")), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] >= Integer.Zero,
                Soundness.Sound,
                description: "(k * a <= 0) = (a >= 0), for a negative real k"),
            new MatchedRule(
                "a-negative-factor-second-drops-out-of-a-equals-with-zero",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"].EqualTo(Integer.Zero),
                Soundness.Sound,
                description: "(a * k = 0) = (a = 0), for a negative real k",
                // The factor and the zero on the right both go, so the delta is -(|k| + |zero|).
                // `EqualTo` does not chain, so nothing larger can arrive in their place.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-negative-factor-second-drops-out-of-a-greater-with-zero",
                MatchPattern.Node<Greaterf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] < Integer.Zero,
                Soundness.Sound,
                description: "(a * k > 0) = (a < 0), for a negative real k"),
            new MatchedRule(
                "a-negative-factor-second-drops-out-of-a-greaterorequal-with-zero",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] <= Integer.Zero,
                Soundness.Sound,
                description: "(a * k >= 0) = (a <= 0), for a negative real k"),
            new MatchedRule(
                "a-negative-factor-second-drops-out-of-a-less-with-zero",
                MatchPattern.Node<Lessf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] > Integer.Zero,
                Soundness.Sound,
                description: "(a * k < 0) = (a > 0), for a negative real k"),
            new MatchedRule(
                "a-negative-factor-second-drops-out-of-a-lessorequal-with-zero",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] >= Integer.Zero,
                Soundness.Sound,
                description: "(a * k <= 0) = (a >= 0), for a negative real k"),
            new MatchedRule(
                "a-positive-divisor-drops-out-of-a-equals-with-zero",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"].EqualTo(Integer.Zero),
                Soundness.Sound,
                description: "(a / k = 0) = (a = 0), for a positive real k",
                // The divisor and the zero on the right both go, so the delta is -(|k| + |zero|).
                // `EqualTo` does not chain, so nothing larger can arrive in their place.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-positive-divisor-drops-out-of-a-greater-with-zero",
                MatchPattern.Node<Greaterf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] > Integer.Zero,
                Soundness.Sound,
                description: "(a / k > 0) = (a > 0), for a positive real k"),
            new MatchedRule(
                "a-positive-divisor-drops-out-of-a-greaterorequal-with-zero",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] >= Integer.Zero,
                Soundness.Sound,
                description: "(a / k >= 0) = (a >= 0), for a positive real k"),
            new MatchedRule(
                "a-positive-divisor-drops-out-of-a-less-with-zero",
                MatchPattern.Node<Lessf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] < Integer.Zero,
                Soundness.Sound,
                description: "(a / k < 0) = (a < 0), for a positive real k"),
            new MatchedRule(
                "a-positive-divisor-drops-out-of-a-lessorequal-with-zero",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] <= Integer.Zero,
                Soundness.Sound,
                description: "(a / k <= 0) = (a <= 0), for a positive real k"),
            new MatchedRule(
                "a-negative-divisor-drops-out-of-a-equals-with-zero",
                MatchPattern.Node<Equalsf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"].EqualTo(Integer.Zero),
                Soundness.Sound,
                description: "(a / k = 0) = (a = 0), for a negative real k",
                // The divisor and the zero on the right both go, so the delta is -(|k| + |zero|).
                // `EqualTo` does not chain, so nothing larger can arrive in their place.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-negative-divisor-drops-out-of-a-greater-with-zero",
                MatchPattern.Node<Greaterf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] < Integer.Zero,
                Soundness.Sound,
                description: "(a / k > 0) = (a < 0), for a negative real k"),
            new MatchedRule(
                "a-negative-divisor-drops-out-of-a-greaterorequal-with-zero",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] <= Integer.Zero,
                Soundness.Sound,
                description: "(a / k >= 0) = (a <= 0), for a negative real k"),
            new MatchedRule(
                "a-negative-divisor-drops-out-of-a-less-with-zero",
                MatchPattern.Node<Lessf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] > Integer.Zero,
                Soundness.Sound,
                description: "(a / k < 0) = (a > 0), for a negative real k"),
            new MatchedRule(
                "a-negative-divisor-drops-out-of-a-lessorequal-with-zero",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any<Entity>("k", one => Functions.Patterns.IsRealAbove(one, 0) is false && Functions.Patterns.IsRealBelow(one, 0))), MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => bound["a"] >= Integer.Zero,
                Soundness.Sound,
                description: "(a / k <= 0) = (a >= 0), for a negative real k"),
            // The factorial's own condition, not its argument's: `(-1)!` is a pole, so
            // `(-1)! = 0` is NaN rather than False.
            // https://github.com/asc-community/AngouriMath/issues/1081
            new MatchedRule(
                "a-factorial-is-never-zero",
                MatchPattern.Node<Equalsf>(
                    MatchPattern.Any<Factorialf>("f"),
                    MatchPattern.Any<Entity>("zero", one => Functions.Patterns.IsZeroReal(one))),
                bound => Entity.Boolean.False.Provided(((Factorialf)bound["f"]).DomainCondition),
                Soundness.SoundUnderAssumptions,
                description: "(a! = 0) = False, where a! is defined"),
            // The `DomainCondition` is about singularities and says nothing about where the ordering is
            // defined, so both are needed: `x < x` is False on the real line and NaN at x = i.
            new MatchedRule(
                "a-greater-of-a-thing-with-itself-is-decided",
                MatchPattern.Node<Greaterf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Entity.Boolean.False.Provided(bound["a"].DomainCondition).Provided(Functions.Patterns.OrderedConditionFor(bound["a"])),
                Soundness.SoundUnderAssumptions,
                description: "(a > a) = False, where a is ordered"),
            new MatchedRule(
                "a-less-of-a-thing-with-itself-is-decided",
                MatchPattern.Node<Lessf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Entity.Boolean.False.Provided(bound["a"].DomainCondition).Provided(Functions.Patterns.OrderedConditionFor(bound["a"])),
                Soundness.SoundUnderAssumptions,
                description: "(a < a) = False, where a is ordered"),
            new MatchedRule(
                "a-greaterorequal-of-a-thing-with-itself-is-decided",
                MatchPattern.Node<GreaterOrEqualf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Entity.Boolean.True.Provided(bound["a"].DomainCondition).Provided(Functions.Patterns.OrderedConditionFor(bound["a"])),
                Soundness.SoundUnderAssumptions,
                description: "(a >= a) = True, where a is ordered"),
            new MatchedRule(
                "a-lessorequal-of-a-thing-with-itself-is-decided",
                MatchPattern.Node<LessOrEqualf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Entity.Boolean.True.Provided(bound["a"].DomainCondition).Provided(Functions.Patterns.OrderedConditionFor(bound["a"])),
                Soundness.SoundUnderAssumptions,
                description: "(a <= a) = True, where a is ordered"));

        /// <summary>
        /// <see cref="Functions.Patterns.PowerRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The largest set converted so far, and the one whose rules are least alike.</b> It
        /// carries the branch-cut guards that <a
        /// href="https://github.com/asc-community/AngouriMath/issues/752">#752</a>, <a
        /// href="https://github.com/asc-community/AngouriMath/issues/801">#801</a>, <a
        /// href="https://github.com/asc-community/AngouriMath/issues/802">#802</a>, <a
        /// href="https://github.com/asc-community/AngouriMath/issues/902">#902</a> and <a
        /// href="https://github.com/asc-community/AngouriMath/issues/721">#721</a> each put on one
        /// rewrite, and those guards are the reason nearly every rule here is
        /// <see cref="Soundness.SoundUnderAssumptions"/> rather than
        /// <see cref="Soundness.Sound"/>: the identity is true, and it is true on a region.
        /// </para>
        /// <para>
        /// The conditions stay where they were — <c>Patterns.Power.cs</c> holds
        /// <c>MayTakeLogOfPower</c>, <c>MayGatherLogarithms</c> and <c>ReduceRadical</c>, and the
        /// rules here ask them. Copying a branch-cut condition into a second file is how the two
        /// copies come to disagree, and one of them has already been wrong in both directions at
        /// once.
        /// </para>
        /// <para>
        /// <para>
        /// <b>Two commutative rules stand for six arms here, and that is affordable only because
        /// of <a href="https://github.com/asc-community/AngouriMath/issues/1079">#1079</a>.</b>
        /// A commutative pattern is not <see cref="MatchPattern.IsDeterministic"/>, and before
        /// bounded matching that meant enumerating its two candidates through an iterator state
        /// machine per pattern node, at every node of every pass: 165.05 MB to 171.37 MB on
        /// <c>SolveMediumHard</c>, +3.8%, past the kernel gate's 3% allocation band. Walked by
        /// index instead, the same two rules cost +0.94% and the six arms need not be written out.
        /// </para>
        /// Two rules read something a <see cref="Bindings"/> cannot carry. <c>log_b(b) = 1</c>
        /// needs the <i>node's</i> own <c>DomainCondition</c> rather than a condition written out
        /// here, and takes the overload that is handed the matched node; and the radical reduction
        /// asks <c>ReduceRadical</c> in both its <c>when</c> and its replacement, because whether
        /// the rule applies and what it produces are the same computation.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet Power { get; } = new(
            nameof(Power),

            new MatchedRule(
                "a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero",
                MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                // Two assumptions, not one: that a is not zero, and that a has a value at all.
                // https://github.com/asc-community/AngouriMath/issues/1174
                bound => new Providedf(1, !bound["a"].EqualTo(0) & bound["a"].DomainCondition),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached carries a copy of a and a's own domain
                // condition, whose size nothing here bounds.
                description: "a / a = 1, provided a is not zero and is defined"),

            new MatchedRule(
                "a-power-whose-exponent-divides-by-a-logarithm-of-its-own-base-changes-base",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Divf>(
                        MatchPattern.Any("n"),
                        MatchPattern.Node<Logf>(MatchPattern.Any("c"), MatchPattern.Any("a")))),
                bound => new Powf(bound["c"], bound["n"]),
                Soundness.SoundUnderAssumptions,
                description: "a ^ (n / log(c, a)) = c ^ n",
                // The base is matched twice -- once as the base and once inside the logarithm -- and
                // written not at all, and the quotient and the logarithm go with it: the delta is
                // -(3 + 2|a|), at most -5.
                growth: RewriteRuleGrowth.Collects),

            // Both orientations are written out in the `switch`, so one commutative rule fires
            // exactly where the pair did.
            new MatchedRule(
                "a-power-times-its-own-base-raises-the-exponent",
                MatchPattern.Commutative<Mulf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Any("a")),
                bound => new Powf(bound["a"], bound["n"] + 1),
                Soundness.SoundUnderAssumptions,
                description: "a ^ n * a = a ^ (n + 1)",
                // The base is matched twice and written once, and the 1 added to the exponent is
                // one node more: the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "two-powers-of-one-base-multiply-by-adding-exponents",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("m"))),
                bound => new Powf(bound["a"], bound["n"] + bound["m"]),
                Soundness.SoundUnderAssumptions,
                description: "a ^ n * a ^ m = a ^ (n + m)",
                // The base is matched twice and written once, so the replacement is shorter by
                // a whole copy of it and by one operator besides: the delta is -(1 + |a|), which
                // is -2 for a base of one node and more for a larger one. It shrinks for every
                // input rather than on average.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "two-powers-of-one-base-divide-by-subtracting-exponents",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("m"))),
                bound => new Powf(bound["a"], bound["n"] - bound["m"]),
                Soundness.SoundUnderAssumptions,
                description: "a ^ n / a ^ m = a ^ (n - m)",
                // The base is matched twice and written once, so the delta is -(1 + |a|) --
                // -2 for a base of one node and more for a larger one.
                growth: RewriteRuleGrowth.Collects),

            // True for a positive base whatever the exponents, and for any base when the outer
            // exponent is whole. Outside those two it moves the branch, which is what #752
            // measured: sqrt(x^2) came back as x, and at -0.63 that is the negation.
            // https://github.com/asc-community/AngouriMath/issues/752
            new MatchedRule(
                "a-power-of-a-power-multiplies-the-exponents",
                MatchPattern.Node<Powf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Any("m")),
                bound => new Powf(bound["a"], bound["n"] * bound["m"]),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["m"] is Integer
                               || bound["a"].Evaled is Real { IsPositive: true },
                description: "(a ^ n) ^ m = a ^ (n * m)",
                // Two binary operators and three holes on each side, every hole used once: the
                // nesting moves from the base to the exponent and nothing is added or dropped.
                growth: RewriteRuleGrowth.Rearranges),

            // https://github.com/asc-community/AngouriMath/issues/801
            new MatchedRule(
                "two-powers-of-one-exponent-share-a-base",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("n"))),
                bound => new Powf(bound["a"] * bound["b"], bound["n"]),
                Soundness.SoundUnderAssumptions,
                when: bound => (bound["n"] is Integer
                                || (bound["a"].Evaled is Real { IsPositive: true }
                                    && bound["b"].Evaled is Real { IsPositive: true }))
                               // And declines the one shape its neighbour undoes.
                               // `a-numeric-factor-comes-out-of-a-power-of-a-product` takes a
                               // numeric factor back out of `(c * a) ^ d` when d is numeric too,
                               // so `1 ^ (-2) * x ^ (-2)` collected and expanded for ever. The
                               // narrower rule is the one with the opinion worth keeping there:
                               // it exists so `c ^ d` can be evaluated, which is why it wants
                               // both numeric. Where *both* bases are numeric there is nothing to
                               // take out afterwards and this one still fires, so `2 ^ 2 * 3 ^ 2`
                               // still collects and `3 ^ (-1/2) * 2 ^ (-1/2)` is still
                               // `sqrt(6) / 6` rather than `sqrt(3) * sqrt(2) / 6`.
                               // https://github.com/asc-community/AngouriMath/issues/1171
                               && !(bound["n"] is Number
                                    && bound["a"] is Number != bound["b"] is Number),
                description: "a ^ n * b ^ n = (a * b) ^ n",
                // The exponent is matched twice and written once, so the delta is -(1 + |n|) --
                // -2 for an exponent of one node and more for a larger one. The same identity
                // is in `Power` with the same figure counted from its own operands.
                growth: RewriteRuleGrowth.Collects),

            // https://github.com/asc-community/AngouriMath/issues/802
            new MatchedRule(
                "two-powers-of-one-exponent-share-a-quotient-of-bases",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("n"))),
                bound => new Powf(bound["a"] / bound["b"], bound["n"]),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["n"] is Integer
                               || (bound["a"].Evaled is Real { IsPositive: true }
                                   && bound["b"].Evaled is Real { IsPositive: true }),
                description: "a ^ n / b ^ n = (a / b) ^ n",
                // The exponent is matched twice and written once, so the delta is -(1 + |n|) --
                // -2 for an exponent of one node and more for a larger one.
                growth: RewriteRuleGrowth.Collects),

            // The pair the rule above loses whenever only one of the two bases is itself a power,
            // read back. https://github.com/asc-community/AngouriMath/issues/740
            new MatchedRule(
                "a-quotient-of-powers-whose-exponents-differ-by-a-whole-factor-takes-it-into-the-divisor",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Node<Powf>(
                        MatchPattern.Any("b"),
                        MatchPattern.Node<Mulf>(
                            MatchPattern.Any<Integer>("c", whole => whole.IsPositive),
                            MatchPattern.Any("n")))),
                bound => new Powf(bound["a"] / new Powf(bound["b"], bound["c"]), bound["n"]),
                Soundness.SoundUnderAssumptions,
                description: "a ^ n / b ^ (c * n) = (a / b ^ c) ^ n, for a positive whole c",
                // The exponent is matched twice and written once, and one power node goes: the delta
                // is -(1 + |n|), at most -2 and further the larger the exponent is.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-quotient-of-powers-whose-exponents-differ-by-a-whole-factor-takes-it-into-the-dividend",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Powf>(
                        MatchPattern.Any("a"),
                        MatchPattern.Node<Mulf>(
                            MatchPattern.Any<Integer>("c", whole => whole.IsPositive),
                            MatchPattern.Any("n"))),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("n"))),
                bound => new Powf(new Powf(bound["a"], bound["c"]) / bound["b"], bound["n"]),
                Soundness.SoundUnderAssumptions,
                description: "a ^ (c * n) / b ^ n = (a ^ c / b) ^ n, for a positive whole c",
                // The exponent is matched twice and written once, and one power node goes with it:
                // the delta is -(1 + |n|), the mirror of the rule that takes it into the divisor.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-thing-over-a-power-of-itself-is-one-power",
                MatchPattern.Node<Divf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n"))),
                bound => new Powf(bound["a"], 1 - bound["n"]),
                Soundness.SoundUnderAssumptions,
                description: "a / a ^ n = a ^ (1 - n)",
                // The base is matched twice and written once, and the 1 the exponent is taken
                // from is one node more: the delta is 1 - |a|, nothing at a leaf and a shrink
                // beyond it.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-power-over-its-own-base-lowers-the-exponent",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Any("a")),
                bound => new Powf(bound["a"], bound["n"] - 1),
                Soundness.SoundUnderAssumptions,
                description: "a ^ n / a = a ^ (n - 1)",
                // The base is matched twice and written once, and the 1 taken from the exponent
                // is one node more: the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            // b ^ log_b(a) = a, in three arms by what can be said of the base. A number other
            // than 0 and 1, and a constant -- e, pi -- are decidably neither 0 nor 1, which is all
            // the identity needs: log_b(a) is defined only where b is neither, and where it is
            // defined the power undoes it. It holds off the positive reals too -- at a = -3,
            // ln(-3) is ln(3) + i*pi and e^(ln(3) + i*pi) is -3 -- and at a = 0, where ln(0) is
            // -oo here and e^(-oo) is 0, so both sides are 0 and no definedness moves. Labelled
            // under assumptions rather than Sound because it is the principal branch of the
            // logarithm that makes it true away from the reals, which is a branch convention.
            // A base of 0 or 1 is refused rather than answered: 1 ^ log(1, a) is 1, not a.
            new MatchedRule(
                "a-number-raised-to-a-logarithm-of-itself-is-the-antilogarithm",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any<Number>("c", static c => c != Integer.Zero && c != Integer.One),
                    MatchPattern.Node<Logf>(MatchPattern.Any<Number>("c"), MatchPattern.Any("a"))),
                bound => bound["a"],
                Soundness.SoundUnderAssumptions,
                description: "a ^ log(a, b) = b",
                // The base is a Number and so a leaf, and the power and the logarithm both go, so
                // four nodes around the remaining hole disappear: exactly -4, for every input.
                growth: RewriteRuleGrowth.Collects),

            // The same identity through a constant: `ln(b)` is stored as log(e, b) and `e` is a
            // Constant, not a Number, so `Any<Number>` never binds it however the logarithm is
            // written, and pi ^ log(pi, b) was left standing for the same reason. Written with a
            // pattern on the right rather than the `bound => bound["a"]` its numeric sibling uses,
            // so it is data in both directions: the constructor can then check the replacement
            // only builds names the pattern binds, and the rule classifies as Reversible instead
            // of ReplacementIsCode. https://github.com/asc-community/AngouriMath/issues/994
            new MatchedRule(
                "a-constant-raised-to-a-logarithm-of-itself-is-the-antilogarithm",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any<Constant>("c"),
                    MatchPattern.Node<Logf>(MatchPattern.Any<Constant>("c"), MatchPattern.Any("a"))),
                MatchPattern.Any("a"),
                Soundness.SoundUnderAssumptions,
                description: "e ^ ln(b) = b"),

            // And through a symbolic base, where b != 0 and b != 1 cannot be decided and so
            // travel with the answer as its condition -- the domain condition of the logarithm
            // that the rewrite would otherwise lose, which SimplificationContract.md says a rule
            // either attaches or does not fire on. a ^ log(a, b) at a = 1 is undefined and b is
            // not, so the difference is exactly that condition.
            new MatchedRule(
                "a-symbol-raised-to-a-logarithm-of-itself-is-the-antilogarithm-where-defined",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any("c"),
                    MatchPattern.Node<Logf>(MatchPattern.Any("c"), MatchPattern.Any("a"))),
                bound => bound["a"].Provided(!bound["c"].EqualTo(0) & !bound["c"].EqualTo(1)),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["c"] is not Number and not Constant,
                description: "a ^ log(a, b) = b provided not a = 0 and not a = 1",
                // The base is written twice again inside the condition, beside six new nodes
                // for it: more than the power and the logarithm that go.
                growth: RewriteRuleGrowth.Expands),

            // The same identity with a multiplier on the logarithm: c ^ (k log_c(b)) is b ^ k,
            // since c ^ z is exp(z ln c) and log_c(b) is ln(b) / ln(c) on the principal branch,
            // so the two ln(c) cancel and what is left is exp(k ln b), which is the definition
            // of b ^ k. Exact for every complex b other than 0, any k, and a base that is
            // decidably neither 0 nor 1 -- a number or a constant; a symbolic base is left to
            // the rule below, which attaches the condition. This is what e ^ (-3/2 ln(u)) was
            // waiting for: the exponential substitution in the integrator writes its
            // antiderivatives in that shape and nothing folded them.
            // https://github.com/asc-community/AngouriMath/issues/994
            // https://github.com/asc-community/AngouriMath/issues/718
            new MatchedRule(
                "a-number-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any<Number>("c", static c => c != Integer.Zero && c != Integer.One),
                    MatchPattern.Commutative<Mulf>(
                        MatchPattern.Any("k"),
                        MatchPattern.Node<Logf>(MatchPattern.Any<Number>("c"), MatchPattern.Any("a")))),
                bound => new Powf(bound["a"], bound["k"]),
                Soundness.SoundUnderAssumptions,
                description: "a ^ (k * log(a, b)) = b ^ k",
                // The base, matched twice, and the product and the logarithm go; the power is
                // rebuilt over the two holes: -(2|c| + 2).
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-constant-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any<Constant>("c"),
                    MatchPattern.Commutative<Mulf>(
                        MatchPattern.Any("k"),
                        MatchPattern.Node<Logf>(MatchPattern.Any<Constant>("c"), MatchPattern.Any("a")))),
                // A pattern on the right, as for its k = 1 sibling above, so the rule is data in
                // both directions; like that sibling it is one-way all the same, because the
                // answer forgets which constant the base was.
                MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("k")),
                Soundness.SoundUnderAssumptions,
                description: "e ^ (k * ln(b)) = b ^ k"),

            new MatchedRule(
                "a-symbol-raised-to-a-multiple-of-a-logarithm-of-itself-is-a-power-of-the-antilogarithm-where-defined",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any("c"),
                    MatchPattern.Commutative<Mulf>(
                        MatchPattern.Any("k"),
                        MatchPattern.Node<Logf>(MatchPattern.Any("c"), MatchPattern.Any("a")))),
                bound => new Powf(bound["a"], bound["k"]).Provided(!bound["c"].EqualTo(0) & !bound["c"].EqualTo(1)),
                Soundness.SoundUnderAssumptions,
                when: bound => bound["c"] is not Number and not Constant,
                description: "a ^ (k * log(a, b)) = b ^ k provided not a = 0 and not a = 1",
                // The condition writes the base twice more, beside six new nodes: more than the
                // product and the logarithm that go.
                growth: RewriteRuleGrowth.Expands),

            // Four `switch` arms: the power on either side of a product, and the shared base in
            // either position inside it. A commutative pattern says both halves at once.
            new MatchedRule(
                "a-power-times-a-product-containing-its-own-base-raises-the-exponent",
                MatchPattern.Commutative<Mulf>(
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n")),
                    MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("rest"))),
                bound => new Powf(bound["a"], bound["n"] + 1) * bound["rest"],
                Soundness.SoundUnderAssumptions,
                description: "a ^ n * (a * rest) = a ^ (n + 1) * rest",
                // The base is matched twice and written once, and the 1 added to the exponent is
                // one node more: the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            // Taking a factor out from under a root needs that factor positive, or the root to be
            // a whole power. https://github.com/asc-community/AngouriMath/issues/752
            new MatchedRule(
                "a-numeric-factor-comes-out-of-a-power-of-a-product",
                MatchPattern.Node<Powf>(
                    MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any("a")),
                    MatchPattern.Any<Number>("d")),
                bound => new Powf(bound["c"], bound["d"]) * new Powf(bound["a"], bound["d"]),
                Soundness.SoundUnderAssumptions,
                // And declines when the other factor is numeric as well, because then there is
                // nothing to take the factor out *of*: `two-powers-of-one-exponent-share-a-base`
                // collects those back together, and the two looped. `(2 * 3) ^ 2` folds to 36 on
                // its own. https://github.com/asc-community/AngouriMath/issues/1171
                when: bound => (bound["d"] is Integer || bound["c"] is Real { IsPositive: true })
                               && bound["a"] is not Number,
                description: "(c * a) ^ d = c ^ d * a ^ d, for a numeric c and d and a symbolic a",
                // Both c and d are Numbers, which are leaves, so the pattern is four nodes around
                // the remaining hole and the replacement is six: exactly +2, for every input.
                growth: RewriteRuleGrowth.Expands),

            new MatchedRule(
                "a-reciprocal-power-is-a-quotient",
                MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Exact(Integer.Create(-1))),
                bound => 1 / bound["a"],
                Soundness.Sound,
                description: "a ^ (-n) = 1 / a ^ n",
                // The exponent this matches is the literal -1, so the power becomes a quotient
                // and the -1 becomes the numerator 1: three nodes for three, one hole in each.
                growth: RewriteRuleGrowth.Rearranges),

            new MatchedRule(
                "a-power-of-a-numeric-reciprocal-times-its-own-denominator-lowers-the-exponent",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Powf>(
                        MatchPattern.Node<Divf>(MatchPattern.Any<Number>("c"), MatchPattern.Any("a")),
                        MatchPattern.Any<Number>("d")),
                    MatchPattern.Any("a")),
                // `Number` arithmetic rather than `Entity` arithmetic: the `switch` binds these
                // as numbers, so `1 - c` folds to a literal there and would build a `Minusf` here.
                bound => new Powf(bound["c"], bound["d"])
                    * new Powf(bound["a"], 1 - (Number)bound["d"]),
                Soundness.SoundUnderAssumptions,
                // Splitting the power of the quotient moves the branch for a negative a unless d
                // is whole, or an odd root over a positive c: see AReciprocalPowerSplits.
                when: bound => Functions.Patterns.AReciprocalPowerSplits((Number)bound["c"], (Number)bound["d"]),
                description: "(c / a) ^ d * a = c ^ d * a ^ (1 - d), for numeric c and d, d whole or an odd root over a positive c",
                // The two numbers are leaves and 1 - d folds to one; the base is matched twice
                // and written once, and the second power is one node more than the quotient it
                // replaces: the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-power-of-a-numeric-reciprocal-times-a-power-of-its-own-denominator-subtracts-the-exponents",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Node<Powf>(
                        MatchPattern.Node<Divf>(MatchPattern.Any<Number>("c"), MatchPattern.Any("a")),
                        MatchPattern.Any<Number>("d")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any<Number>("e"))),
                bound => new Powf(bound["c"], bound["d"])
                    * new Powf(bound["a"], (Number)bound["e"] - (Number)bound["d"]),
                Soundness.SoundUnderAssumptions,
                // As above, and the two powers of a combine only where both read a negative a the
                // same way: an odd root beside a principal one does not.
                when: bound => Functions.Patterns.AReciprocalPowerSplits((Number)bound["c"], (Number)bound["d"])
                               && (bound["d"] is Integer || Functions.Patterns.IsWholeOrAnOddRoot((Number)bound["e"])),
                description: "(c / a) ^ d * a ^ e = c ^ d * a ^ (e - d), for numeric c, d and e, d whole or an odd root over a positive c",
                // The three numbers are leaves and e - d folds to one; the base is matched twice
                // and written once, and the quotient goes: the delta is -1 - |a|, at most -2.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "dividing-twice-by-one-thing-squares-it",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Any("b")),
                bound => bound["a"] / new Powf(bound["b"], 2),
                Soundness.SoundUnderAssumptions,
                description: "a / b / b = a / b ^ 2",
                // The divisor is matched twice and written once, and the exponent is one node
                // more: the delta is 1 - |b|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "dividing-by-a-power-and-then-by-its-base-raises-the-exponent",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Divf>(
                        MatchPattern.Any("a"),
                        MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("n"))),
                    MatchPattern.Any("b")),
                bound => bound["a"] / new Powf(bound["b"], bound["n"] + 1),
                Soundness.SoundUnderAssumptions,
                description: "a / b ^ n / b = a / b ^ (n + 1)",
                // The divisor is matched twice and written once, and the 1 added to the exponent
                // is one node more: the delta is 1 - |b|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "dividing-by-a-thing-and-then-by-a-power-of-it-raises-the-exponent",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("n"))),
                bound => bound["a"] / new Powf(bound["b"], bound["n"] + 1),
                Soundness.SoundUnderAssumptions,
                description: "a / b / b ^ n = a / b ^ (n + 1)",
                // The divisor is matched twice and written once, and the 1 added to the exponent
                // is one node more: the delta is 1 - |b|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "dividing-by-two-powers-of-one-base-adds-the-exponents",
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Divf>(
                        MatchPattern.Any("a"),
                        MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("m"))),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("n"))),
                bound => bound["a"] / new Powf(bound["b"], bound["n"] + bound["m"]),
                Soundness.SoundUnderAssumptions,
                description: "a / b ^ n / b ^ m = a / b ^ (n + m)",
                // The base is matched twice and written once, and one quotient and one power go:
                // the delta is -(2 + |b|), at most -3.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-variable-times-a-power-puts-the-power-first",
                MatchPattern.Node<Mulf>(
                    MatchPattern.Any<Variable>("v"),
                    MatchPattern.Node<Powf>(MatchPattern.Any("a"), MatchPattern.Any("n"))),
                bound => new Powf(bound["a"], bound["n"]) * bound["v"],
                Soundness.Sound,
                description: "v * a ^ n = a ^ n * v, for a variable v",
                // The two operands of the product are written the other way round and nothing else
                // moves, so the two sides are the same size whatever they are filled with.
                growth: RewriteRuleGrowth.Rearranges),

            // https://github.com/asc-community/AngouriMath/issues/902
            new MatchedRule(
                "an-exponent-comes-out-of-a-logarithm",
                MatchPattern.Node<Logf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Powf>(MatchPattern.Any("b"), MatchPattern.Any("n"))),
                bound => bound["n"] * MathS.Log(bound["a"], bound["b"]),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.MayTakeLogOfPower(bound["b"], bound["n"]),
                description: "log(a, b ^ n) = n * log(a, b)",
                // A logarithm and a power become a product and a logarithm, one node for one, and
                // each of the three holes is used once on both sides.
                growth: RewriteRuleGrowth.Rearranges),
            // The same identity for a rational literal that is a power without saying so, with a
            // positive root only, so that the principal branch has nothing to discard.
            // https://github.com/asc-community/AngouriMath/issues/1212
            new MatchedRule(
                "a-logarithm-of-a-perfect-power-takes-the-exponent-out",
                MatchPattern.Node<Logf>(MatchPattern.Any("a"),
                    MatchPattern.Any<Rational>("r", r => Functions.TreeAnalyzer.TryPerfectPower(r, out _, out _))),
                bound =>
                {
                    Functions.TreeAnalyzer.TryPerfectPower((Rational)bound["r"], out var root, out var exponent);
                    return Integer.Create(exponent) * MathS.Log(bound["a"], root);
                },
                Soundness.Sound,
                description: "log(a, r) = n * log(a, root) where r = root ^ n",
                // One literal becomes a product of a literal and a logarithm of a literal: two
                // nodes more, whatever the base is.
                growth: RewriteRuleGrowth.Expands),

            // The condition to carry is the node's own: log(-3, -3) is 1 where a written-out
            // `a > 0` calls it undefined, and log(1, 1) is NaN where that guard calls it 1.
            // https://github.com/asc-community/AngouriMath/issues/721
            new MatchedRule(
                "a-logarithm-of-its-own-base-is-one-where-it-is-defined",
                MatchPattern.Node<Logf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                (node, bound) => new Providedf(1, ((Logf)node).DomainCondition),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached is the node's own domain condition, whose
                // size nothing here bounds.
                description: "log(a, a) = 1, where log(a, a) is defined"),

            // ln(1/b) = -ln(b) is false on the negative reals: at b = -0.63 the two differ by the
            // full turn of the argument the principal branch discards.
            // https://github.com/asc-community/AngouriMath/issues/721
            new MatchedRule(
                "a-logarithm-of-a-reciprocal-in-a-reciprocal-base-turns-round-twice",
                MatchPattern.Node<Logf>(
                    MatchPattern.Node<Divf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Any("a")),
                    MatchPattern.Node<Divf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Any("b"))),
                bound => MathS.Log(bound["a"], bound["b"]),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.MayGatherLogarithms(Integer.One, bound["a"], isDifference: true)
                               && Functions.Patterns.MayGatherLogarithms(Integer.One, bound["b"], isDifference: true),
                description: "log(1 / a, 1 / b) = log(a, b)",
                // Both reciprocals go and nothing arrives: five nodes around the two holes become
                // one, exactly -4 for every input.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "a-logarithm-of-a-reciprocal-negates",
                MatchPattern.Node<Logf>(
                    MatchPattern.Any("a"),
                    MatchPattern.Node<Divf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Any("b"))),
                bound => -MathS.Log(bound["a"], bound["b"]),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.MayGatherLogarithms(Integer.One, bound["b"], isDifference: true),
                description: "log(a, 1 / b) = -log(a, b)",
                // The quotient by one that spells the reciprocal becomes the product by minus one
                // that spells the negation, two nodes for two.
                growth: RewriteRuleGrowth.Rearranges),

            new MatchedRule(
                "a-logarithm-in-a-reciprocal-base-negates",
                MatchPattern.Node<Logf>(
                    MatchPattern.Node<Divf>(MatchPattern.Exact(Integer.Create(1)), MatchPattern.Any("a")),
                    MatchPattern.Any("b")),
                bound => -MathS.Log(bound["a"], bound["b"]),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.MayGatherLogarithms(Integer.One, bound["a"], isDifference: true),
                description: "log(1 / a, b) = -log(a, b)",
                // The reciprocal in the base becomes the negation outside, two nodes for two.
                growth: RewriteRuleGrowth.Rearranges),

            // https://github.com/asc-community/AngouriMath/issues/721
            new MatchedRule(
                "two-logarithms-of-one-base-add-by-multiplying-their-antilogarithms",
                MatchPattern.Node<Sumf>(
                    MatchPattern.Node<Logf>(MatchPattern.Any("c"), MatchPattern.Any("a")),
                    MatchPattern.Node<Logf>(MatchPattern.Any("c"), MatchPattern.Any("b"))),
                bound => bound["c"].Log(bound["a"] * bound["b"]),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.MayGatherLogarithms(bound["a"], bound["b"], isDifference: false),
                description: "log(a, b) + log(a, c) = log(a, b * c)",
                // The base is matched twice and written once, so the delta is -(1 + |a|): -2 for a
                // base of one node and more for a larger one.
                growth: RewriteRuleGrowth.Collects),

            new MatchedRule(
                "two-logarithms-of-one-base-subtract-by-dividing-their-antilogarithms",
                MatchPattern.Node<Minusf>(
                    MatchPattern.Node<Logf>(MatchPattern.Any("c"), MatchPattern.Any("a")),
                    MatchPattern.Node<Logf>(MatchPattern.Any("c"), MatchPattern.Any("b"))),
                bound => bound["c"].Log(bound["a"] / bound["b"]),
                Soundness.SoundUnderAssumptions,
                when: bound => Functions.Patterns.MayGatherLogarithms(bound["a"], bound["b"], isDifference: true),
                description: "log(a, b) - log(a, c) = log(a, b / c)",
                // The base is matched twice and written once, so the delta is -(1 + |a|).
                growth: RewriteRuleGrowth.Collects),

            // sqrt(8) = 2 * sqrt(2). Whether it applies and what it produces are one computation,
            // so the `when` asks the same helper the replacement does.
            new MatchedRule(
                "a-whole-power-comes-out-from-under-a-radical",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any<Integer>("radicand", whole => whole.IsPositive),
                    MatchPattern.Any<Rational>("power", ratio => ratio is not Integer)),
                bound => Functions.Patterns.ReduceRadical(
                    (Integer)bound["radicand"], (Rational)bound["power"])!,
                Soundness.Sound,
                when: bound => Functions.Patterns.ReduceRadical(
                    (Integer)bound["radicand"], (Rational)bound["power"]) is not null,
                // Left Unknown: the helper answers a whole times a root, two nodes more than the
                // pattern, or a whole alone, two fewer.
                description: "sqrt(8) = 2 * sqrt(2), and its like for a positive whole radicand"),

            // The rule above takes a whole power out from under one root; this takes a root out
            // from under another, which is the nesting rather than the size. Sound rather than
            // conditional: the helper refuses every radicand it cannot square its answer back
            // to, so what fires is an identity between two non-negative reals with no branch
            // chosen -- see the remarks on DenestRadical for why a non-negative `a` and a square
            // discriminant are the whole of what it needs.
            new MatchedRule(
                "a-nested-radical-is-a-sum-of-two-plain-ones",
                MatchPattern.Node<Powf>(
                    MatchPattern.Any("radicand"),
                    MatchPattern.Any<Rational>("power", ratio => ratio.ERational.Equals(
                        PeterO.Numbers.ERational.Create(
                            PeterO.Numbers.EInteger.One, PeterO.Numbers.EInteger.FromInt32(2))))),
                bound => Functions.Patterns.DenestRadical(bound["radicand"])!,
                Soundness.Sound,
                when: bound => Functions.Patterns.DenestRadical(bound["radicand"]) is not null,
                // Left Unknown: the helper computes the answer, and the radicands it denests take
                // more shapes than one count covers.
                description: "sqrt(5 + 2*sqrt(6)) = sqrt(2) + sqrt(3)"));

        /// <summary>
        /// <see cref="Functions.Patterns.CommonRules"/>, as data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The last set, and the one the exchange was always going to be judged on:</b> a
        /// hundred arms, more than any other, and the great majority of them one shape written
        /// out in every orientation its operands can take. A commutative pattern says all of
        /// them at once, so the hundred arms are 62 rules -- and none of that is a guess about
        /// what the `switch` covers, because every orientation collapsed here is one the
        /// `switch` writes out. Where it writes only one, the pattern is a node and stays one.
        /// </para>
        /// <para>
        /// Order is load-bearing throughout, more than in any set so far: <c>a * a</c> becoming
        /// <c>a ^ 2</c> sits in the middle of the file and would swallow half of what is above it
        /// if it were moved up. The rules are in the arms' order and the agreement test is what
        /// says that is enough.
        /// </para>
        /// </remarks>
        internal static MatchedRuleSet Common { get; } = new(
            nameof(Common),

            new MatchedRule(
                "a-numeric-factor-floats-out-of-a-product-of-functions",
                MatchPattern.Node<Mulf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Function>("f")), MatchPattern.Any<Function>("g")),
                bound => bound["f"] * bound["g"] * bound["c"],
                Soundness.SoundUnderAssumptions,
                description: "(c * f) * g = c * (f * g), for a numeric c",
                // The numeric factor moves to the outside of the product and the two functions come
                // together, which is two products either way round: the same size for every input.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-product-of-two-quotients-is-one-quotient",
                MatchPattern.Node<Mulf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Divf>(MatchPattern.Any("c"), MatchPattern.Any("d"))),
                bound => bound["a"] * bound["c"] / (bound["b"] * bound["d"]),
                Soundness.SoundUnderAssumptions,
                description: "(a / b) * (c / d) = (a * c) / (b * d)",
                // Three binary operators and four holes on each side, every hole used once, so
                // the two are the same size whatever they are filled with.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "dividing-by-a-quotient-multiplies-by-its-reciprocal",
                MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Node<Divf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                bound => bound["a"] * bound["c"] / bound["b"],
                Soundness.SoundUnderAssumptions,
                description: "a / (b / c) = a * c / b",
                // Two binary operators and three holes on each side, every hole used once.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-quotient-times-a-thing-keeps-the-divisor-outermost",
                MatchPattern.Node<Mulf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Any("c")),
                bound => bound["a"] * bound["c"] / bound["b"],
                Soundness.SoundUnderAssumptions,
                description: "(a / b) * c = (a * c) / b",
                // Two binary operators and three holes on each side, every hole used once.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-numeric-quotient-of-a-numeric-multiple-collects-its-numbers",
                // Not for c = -1. That is not a numeric factor to collect but the sign, which the
                // language spells as a product -- and collecting it is exactly undone by
                // `a-negated-reciprocal-rational-factor-is-a-negated-division`, which is Sound
                // where this is SoundUnderAssumptions. The two were the cycle in #1056.
                MatchPattern.Node<Divf>(
                    MatchPattern.Node<Mulf>(
                        MatchPattern.Any<Number>("c", number => number != -1), MatchPattern.Any("a")),
                    MatchPattern.Any<Number>("d")),
                bound => (Number)bound["c"] / (Number)bound["d"] * bound["a"],
                Soundness.SoundUnderAssumptions,
                description: "(c * a) / d = (c / d) * a, for numeric c and d other than -1",
                // The two numbers are folded into one as the replacement is built, and every hole
                // the pattern binds is a leaf or is used once, so two nodes go: exactly -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "dividing-twice-divides-by-the-product",
                MatchPattern.Node<Divf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Any("c")),
                bound => bound["a"] / (bound["b"] * bound["c"]),
                Soundness.SoundUnderAssumptions,
                description: "a / b / c = a / (b * c)",
                // Two binary operators and three holes on each side, every hole used once.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-thing-times-a-quotient-keeps-the-divisor-outermost",
                MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Node<Divf>(MatchPattern.Any("b"), MatchPattern.Any("c"))),
                bound => bound["a"] * bound["b"] / bound["c"],
                Soundness.SoundUnderAssumptions,
                description: "a * (b / c) = (a * b) / c",
                // Two binary operators and three holes on each side, every hole used once.
                growth: RewriteRuleGrowth.Rearranges),
            // Both orientations of the outer product are written out in the `switch`.
            new MatchedRule(
                "two-numeric-factors-around-a-function-collect",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Function>("f")), MatchPattern.Any<Number>("d")),
                bound => (Number)bound["c"] * (Number)bound["d"] * bound["f"],
                Soundness.SoundUnderAssumptions,
                description: "(c * f) * d = (c * d) * f, for numeric c and d",
                // The two numbers are folded into one as the replacement is built, so four nodes
                // around the function become two: exactly -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "two-numeric-multiples-of-functions-collect-their-numbers",
                MatchPattern.Node<Mulf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Function>("f")), MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("d"), MatchPattern.Any<Function>("g"))),
                bound => bound["f"] * bound["g"] * ((Number)bound["c"] * (Number)bound["d"]),
                Soundness.SoundUnderAssumptions,
                description: "(c * f) * (d * g) = f * g * (c * d), for numeric c and d",
                // The two numbers are folded into one as the replacement is built, so five nodes
                // around the two functions become three: exactly -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "two-functions-in-a-sum-come-together",
                MatchPattern.Commutative<Sumf>(MatchPattern.Node<Sumf>(MatchPattern.Any<Function>("f"), MatchPattern.Any("a")), MatchPattern.Any<Function>("g")),
                bound => bound["f"] + bound["g"] + bound["a"],
                Soundness.SoundUnderAssumptions,
                description: "(f + a) + g = f + g + a, for functions f and g",
                // The three operands are regrouped and nothing else moves: two sums stay two sums
                // and each hole is used once on both sides.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-variable-times-a-number-puts-the-number-first",
                MatchPattern.Node<Mulf>(MatchPattern.Any<Variable>("v"), MatchPattern.Any<Number>("c")),
                bound => bound["c"] * bound["v"],
                Soundness.Sound,
                // The same two bound operands under a new `Mulf`, swapped: one node in, one out.
                growth: RewriteRuleGrowth.Rearranges,
                description: "v * c = c * v, for a variable v and a numeric c"),
            new MatchedRule(
                "a-number-plus-a-variable-puts-the-variable-first",
                MatchPattern.Node<Sumf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Variable>("v")),
                bound => bound["v"] + bound["c"],
                Soundness.Sound,
                // The same two bound operands under a new `Sumf`, swapped: one node in, one out.
                growth: RewriteRuleGrowth.Rearranges,
                description: "c + v = v + c, for a variable v and a numeric c"),
            new MatchedRule(
                "a-function-times-a-number-puts-the-number-first",
                MatchPattern.Node<Mulf>(MatchPattern.Any<Function>("f"), MatchPattern.Any<Number>("c")),
                bound => bound["c"] * bound["f"],
                Soundness.Sound,
                // The same two bound operands under a new `Mulf`, swapped: one node in, one out.
                growth: RewriteRuleGrowth.Rearranges,
                description: "f * c = c * f, for a function f and a numeric c"),
            new MatchedRule(
                "a-number-plus-a-function-puts-the-function-first",
                MatchPattern.Node<Sumf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Function>("f")),
                bound => bound["f"] + bound["c"],
                Soundness.Sound,
                // The same two bound operands under a new `Sumf`, swapped: one node in, one out.
                growth: RewriteRuleGrowth.Rearranges,
                description: "c + f = f + c, for a function f and a numeric c"),
            new MatchedRule(
                "two-numeric-multiples-of-one-variable-add",
                MatchPattern.Node<Sumf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Variable>("v")), MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("d"), MatchPattern.Any<Variable>("v"))),
                bound => ((Number)bound["c"] + (Number)bound["d"]) * bound["v"],
                Soundness.Sound,
                description: "c * v + d * v = (c + d) * v, for numeric c and d",
                // Seven nodes become three, exactly, for every input: the pattern's holes are a
                // Number, a Number and a Variable, each of which is a leaf, and the sum of the
                // two numbers is folded to one leaf as the replacement is built. Nothing here
                // can be filled with something larger.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "two-numeric-multiples-of-one-variable-subtract",
                MatchPattern.Node<Minusf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Variable>("v")), MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("d"), MatchPattern.Any<Variable>("v"))),
                bound => ((Number)bound["c"] - (Number)bound["d"]) * bound["v"],
                Soundness.Sound,
                description: "c * v - d * v = (c - d) * v, for numeric c and d",
                // Seven nodes become three, exactly, by the same count as its addition twin:
                // three leaves in the pattern and the difference of the two numbers folded.
                growth: RewriteRuleGrowth.Collects),
            // All four orientations are written out in the `switch`, which is what makes one commutative pattern on each side of the sum exact rather than wider.
            new MatchedRule(
                "a-common-factor-of-two-added-products-comes-out",
                MatchPattern.Node<Sumf>(MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c"))),
                bound => bound["a"] * (bound["b"] + bound["c"]),
                Soundness.SoundUnderAssumptions,
                description: "k * p + k * q = k * (p + q)",
                // The shared factor is matched twice and written once, so the delta is
                // -(1 + |k|): -2 where the factor is one node and more where it is larger.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-shared-with-a-product-added-to-it-comes-out",
                MatchPattern.Commutative<Sumf>(MatchPattern.Any("a"), MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] * (1 + bound["b"]),
                Soundness.SoundUnderAssumptions,
                description: "k + k * q = k * (1 + q)",
                // The shared term is matched twice and written once, and the 1 is one node more:
                // the delta is 1 - |k|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-added-to-itself-doubles",
                MatchPattern.Node<Sumf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => 2 * bound["a"],
                Soundness.Sound,
                description: "k + k = 2 * k",
                // The term is matched twice and written once, and the 2 is one node more: the
                // delta is 1 - |k|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-common-factor-of-two-subtracted-products-comes-out",
                MatchPattern.Node<Minusf>(MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c"))),
                bound => bound["a"] * (bound["b"] - bound["c"]),
                Soundness.SoundUnderAssumptions,
                description: "k * p - k * q = k * (p - q)",
                // The shared factor is matched twice and written once, so the delta is
                // -(1 + |k|), the same count as its addition twin above.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-with-a-product-of-itself-taken-from-it-comes-out",
                MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] * (1 - bound["b"]),
                Soundness.SoundUnderAssumptions,
                description: "a - a * b = a * (1 - b)",
                // The shared term is matched twice and written once, and the 1 is one node more:
                // the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-taken-from-a-product-of-itself-comes-out",
                MatchPattern.Node<Minusf>(MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Any("a")),
                bound => bound["a"] * (bound["b"] - 1),
                Soundness.SoundUnderAssumptions,
                description: "a * b - a = a * (b - 1)",
                // The shared term is matched twice and written once, and the 1 is one node more:
                // the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-subtracted-from-itself-vanishes",
                MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => Integer.Zero.Provided(bound["a"].DomainCondition),
                Soundness.Sound,
                description: "k - k = 0",
                // Everything the pattern matched goes and one node arrives, and the operand was
                // matched twice: the delta is -2|k|, at most -2 and further the larger it is.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-factor-shared-by-a-quotient-and-a-product-added-comes-out",
                MatchPattern.Node<Sumf>(MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c"))),
                bound => bound["a"] * (1 / bound["b"] + bound["c"]),
                Soundness.SoundUnderAssumptions,
                description: "a / b + a * c = a * (1 / b + c)",
                // The shared factor is matched twice and written once, and the 1 over b is one
                // node more than the quotient it replaces: the delta is 1 - |a|.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-factor-shared-by-a-product-and-a-quotient-added-comes-out",
                MatchPattern.Node<Sumf>(MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("c")), MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] * (bound["c"] + 1 / bound["b"]),
                Soundness.SoundUnderAssumptions,
                description: "a * c + a / b = a * (c + 1 / b)",
                // The shared factor is matched twice and written once, and the 1 over b is one
                // node more than the quotient it replaces: the delta is 1 - |a|.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-added-to-a-quotient-of-itself-comes-out",
                MatchPattern.Commutative<Sumf>(MatchPattern.Any<Entity>("a", one => one is not Integer(1)), MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["a"] * (1 + 1 / bound["b"]),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the 1 and the 1 over b are three nodes more against the one copy
                // of a that goes, so the delta is 3 - |a| -- larger at a leaf, smaller only from
                // four nodes up.
                description: "a + a / b = a * (1 + 1 / b)"),
            new MatchedRule(
                "a-thing-times-itself-is-its-square",
                MatchPattern.Node<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                bound => new Powf(bound["a"], 2),
                Soundness.Sound,
                description: "a * a = a ^ 2",
                // The factor is matched twice and written once, and the exponent is one node
                // more: the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "two-numeric-factors-around-a-variable-collect",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Any<Variable>("v")), MatchPattern.Any<Number>("d")),
                bound => (Number)bound["c"] * (Number)bound["d"] * bound["v"],
                Soundness.Sound,
                description: "(c * v) * d = (c * d) * v, for numeric c and d",
                // The two numbers are folded into one as the replacement is built, and every hole
                // the pattern binds is a leaf or is used once, so two nodes go: exactly -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "two-numeric-terms-around-a-variable-collect",
                MatchPattern.Commutative<Sumf>(MatchPattern.Node<Sumf>(MatchPattern.Any<Variable>("v"), MatchPattern.Any<Number>("c")), MatchPattern.Any<Number>("d")),
                bound => bound["v"] + ((Number)bound["c"] + (Number)bound["d"]),
                Soundness.Sound,
                description: "(v + c) + d = v + (c + d), for numeric c and d",
                // The two numbers are folded into one as the replacement is built, and every hole
                // the pattern binds is a leaf or is used once, so two nodes go: exactly -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-factor-repeated-across-a-product-squares",
                MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Commutative<Mulf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => new Powf(bound["a"], 2) * bound["b"],
                Soundness.Sound,
                description: "a * (a * b) = a ^ 2 * b",
                // The factor is matched twice and written once, and the exponent is one node
                // more: the delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-negated-term-in-a-sum-is-a-subtraction",
                MatchPattern.Commutative<Sumf>(MatchPattern.Node<Mulf>(MatchPattern.Exact(Integer.Create(-1)), MatchPattern.Any("neg")), MatchPattern.Any("rest")),
                bound => bound["rest"] - bound["neg"],
                Soundness.Sound,
                description: "a + (-b) = a - b",
                // Five nodes become three, for every input: the sum becomes a difference, one
                // operator for one, and the product by the literal -1 that spelled the negation
                // goes entirely.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-difference-times-a-sum-of-one-pair-is-a-difference-of-squares",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Minusf>(MatchPattern.Any<Variable>("v"), MatchPattern.Any("a")), MatchPattern.Node<Sumf>(MatchPattern.Any<Variable>("v"), MatchPattern.Any("a"))),
                bound => new Powf(bound["v"], 2) - new Powf(bound["a"], 2),
                Soundness.Sound,
                description: "(a - b) * (a + b) = a ^ 2 - b ^ 2",
                // The variable is a leaf either way; the other operand is matched twice and
                // written once, and the two exponents are two nodes more against the operator
                // that goes: the delta is 1 - |b|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-quotient-of-a-thing-by-itself-is-one-unless-it-is-zero",
                MatchPattern.Node<Divf>(MatchPattern.Any("a"), MatchPattern.Any("a")),
                // Two assumptions, not one: that a is not zero, and that a has a value at all.
                // https://github.com/asc-community/AngouriMath/issues/1174
                bound => Integer.One.Provided(
                    !bound["a"].EqualTo(Integer.Zero) & bound["a"].DomainCondition),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached carries a copy of a and a's own domain
                // condition, whose size nothing here bounds.
                description: "a / a = 1, provided a is not zero and is defined"),
            new MatchedRule(
                "a-shared-factor-cancels-out-of-a-quotient",
                MatchPattern.Node<Divf>(MatchPattern.Commutative<Mulf>(MatchPattern.Any("keep"), MatchPattern.Any("c")), MatchPattern.Any("c")),
                // Two assumptions, not one: that c is not zero, and that c has a value at all.
                // https://github.com/asc-community/AngouriMath/issues/1174
                bound => bound["keep"].Provided(
                    !bound["c"].EqualTo(Integer.Zero) & bound["c"].DomainCondition),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached carries a copy of c and c's own domain
                // condition, whose size nothing here bounds.
                description: "(keep * c) / c = keep, provided c is not zero and is defined"),
            new MatchedRule(
                "a-shared-factor-cancels-between-two-products",
                MatchPattern.Node<Divf>(MatchPattern.Commutative<Mulf>(MatchPattern.Any("num"), MatchPattern.Any("c")), MatchPattern.Commutative<Mulf>(MatchPattern.Any("c"), MatchPattern.Any("den"))),
                bound => (bound["num"] / bound["den"]).Provided(!bound["c"].EqualTo(Integer.Zero)),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached carries a copy of c, so the delta is
                // 2 - |c| -- larger at a leaf, smaller from three nodes up.
                description: "(num * c) / (c * den) = num / den, provided c is not zero"),
            new MatchedRule(
                "a-difference-over-its-own-reverse-is-minus-one",
                MatchPattern.Node<Divf>(MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Minusf>(MatchPattern.Any("b"), MatchPattern.Any("a"))),
                (node, bound) => new Providedf(-1, !node.DirectChildren[1].EqualTo(0)),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached carries a copy of the divisor, so the
                // delta is 3 - |a| - |b| -- larger at two leaves, smaller from four nodes up.
                description: "(a - b) / (b - a) = -1, provided a is not b"),
            new MatchedRule(
                "a-sum-over-its-own-reverse-is-one",
                MatchPattern.Node<Divf>(MatchPattern.Node<Sumf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Node<Sumf>(MatchPattern.Any("b"), MatchPattern.Any("a"))),
                (node, bound) => new Providedf(1, !node.DirectChildren[1].EqualTo(0)),
                Soundness.SoundUnderAssumptions,
                // Left Unknown: the condition attached carries a copy of the divisor, so the
                // delta is 3 - |a| - |b| -- larger at two leaves, smaller from four nodes up.
                description: "(a + b) / (b + a) = 1, provided the sum is not zero"),
            new MatchedRule(
                "a-number-over-a-numeric-multiple-splits",
                MatchPattern.Node<Divf>(MatchPattern.Any<Number>("c"), MatchPattern.Commutative<Mulf>(MatchPattern.Any<Number>("d"), MatchPattern.Any("a"))),
                bound => (Number)bound["c"] / (Number)bound["d"] / bound["a"],
                Soundness.SoundUnderAssumptions,
                description: "c / (d * a) = (c / d) / a, for numeric c and d",
                // The two numbers are folded into one as the replacement is built, so four nodes
                // around the remaining hole become two: exactly -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "two-numbers-around-a-factor-collect",
                MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("c"), MatchPattern.Node<Mulf>(MatchPattern.Any<Number>("d"), MatchPattern.Any("a"))),
                bound => (Number)bound["c"] * (Number)bound["d"] * bound["a"],
                Soundness.Sound,
                description: "c * (d * a) = (c * d) * a, for numeric c and d",
                // The two numbers are folded into one as the replacement is built, and every hole
                // the pattern binds is a leaf or is used once, so two nodes go: exactly -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-repeated-across-a-sum-doubles",
                MatchPattern.Commutative<Sumf>(MatchPattern.Any("a"), MatchPattern.Commutative<Sumf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => 2 * bound["a"] + bound["b"],
                Soundness.Sound,
                description: "a + (a + b) = 2 * a + b",
                // The term is matched twice and written once, and the 2 is one node more: the
                // delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-taken-back-out-of-a-sum-it-is-in",
                MatchPattern.Node<Minusf>(MatchPattern.Commutative<Sumf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Any("a")),
                bound => bound["b"],
                Soundness.Sound,
                description: "(a + b) - a = b",
                // All of the pattern but one hole disappears, and the repeated operand goes twice
                // over: the delta is -(2 + 2|a|), at most -4.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-sum-containing-a-term-taken-from-that-term-leaves-the-rest-negated",
                MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Commutative<Sumf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => -bound["b"],
                Soundness.Sound,
                description: "a - (a + b) = -b",
                // The repeated operand goes twice over and a negation of two nodes arrives in place
                // of the difference and the sum, so the delta is -2|a|: at most -2.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-added-to-a-difference-that-takes-it-away",
                MatchPattern.Commutative<Sumf>(MatchPattern.Any("a"), MatchPattern.Node<Minusf>(MatchPattern.Any("b"), MatchPattern.Any("a"))),
                bound => bound["b"],
                Soundness.Sound,
                description: "a + (b - a) = b",
                // All of the pattern but one hole disappears, and the repeated operand goes twice
                // over: the delta is -(2 + 2|a|), at most -4.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-added-to-a-difference-that-starts-from-it",
                MatchPattern.Commutative<Sumf>(MatchPattern.Any("a"), MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => 2 * bound["a"] - bound["b"],
                Soundness.Sound,
                description: "a + (a - b) = 2 * a - b",
                // The term is matched twice and written once, and the 2 is one node more: the
                // delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-difference-that-takes-a-term-away-subtracted-from-it",
                MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Node<Minusf>(MatchPattern.Any("b"), MatchPattern.Any("a"))),
                bound => 2 * bound["a"] - bound["b"],
                Soundness.Sound,
                description: "a - (b - a) = 2 * a - b",
                // The term is matched twice and written once, and the 2 is one node more: the
                // delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-difference-that-starts-from-a-term-subtracted-from-it",
                MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Any("b"))),
                bound => bound["b"],
                Soundness.Sound,
                description: "a - (a - b) = b",
                // The whole of the pattern but one hole disappears: the delta is -(2 + 2|a|),
                // at most -4 and further the larger the repeated operand is.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-taken-from-a-difference-that-already-took-it",
                MatchPattern.Node<Minusf>(MatchPattern.Node<Minusf>(MatchPattern.Any("b"), MatchPattern.Any("a")), MatchPattern.Any("a")),
                bound => bound["b"] - 2 * bound["a"],
                Soundness.Sound,
                description: "(b - a) - a = b - 2 * a",
                // The term is matched twice and written once, and the 2 is one node more: the
                // delta is 1 - |a|, nothing at a leaf and a shrink beyond it.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-term-taken-from-a-difference-that-starts-from-it",
                MatchPattern.Node<Minusf>(MatchPattern.Node<Minusf>(MatchPattern.Any("a"), MatchPattern.Any("b")), MatchPattern.Any("a")),
                bound => -bound["b"],
                Soundness.Sound,
                description: "(a - b) - a = -b",
                // The repeated operand goes twice over and a negation of two nodes arrives, so
                // the delta is -2|a|: at most -2, and further the larger that operand is.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-product-of-two-absolute-values-is-the-absolute-value-of-the-product",
                MatchPattern.Node<Mulf>(MatchPattern.Node<Absf>(MatchPattern.Any("a")), MatchPattern.Node<Absf>(MatchPattern.Any("b"))),
                bound => new Absf(bound["a"] * bound["b"]),
                Soundness.Sound,
                description: "abs(a) * abs(b) = abs(a * b)",
                // Two absolute values become one and the product stays a product, so three nodes
                // around the operands become two: exactly -1, for every input.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-quotient-of-two-absolute-values-is-the-absolute-value-of-the-quotient",
                MatchPattern.Node<Divf>(MatchPattern.Node<Absf>(MatchPattern.Any("a")), MatchPattern.Node<Absf>(MatchPattern.Any("b"))),
                bound => new Absf(bound["a"] / bound["b"]),
                Soundness.SoundUnderAssumptions,
                description: "abs(a) / abs(b) = abs(a / b)",
                // Two absolute values become one: exactly -1, for every input.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-sign-times-a-thing-over-its-own-absolute-value-cancels",
                MatchPattern.Node<Divf>(MatchPattern.Node<Mulf>(MatchPattern.Node<Signumf>(MatchPattern.Any("a")), MatchPattern.Node<Mulf>(MatchPattern.Any("b"), MatchPattern.Any("a"))), MatchPattern.Node<Absf>(MatchPattern.Any("a"))),
                bound => bound["b"].Provided(!bound["a"].EqualTo(Integer.Zero)),
                Soundness.SoundUnderAssumptions,
                description: "(sgn(a) * (b * a)) / abs(a) = b, provided a is not zero",
                // Three copies of a go and one comes back inside the condition; the sign, the
                // absolute value, the quotient and two products go against the three nodes the
                // condition and its attachment add: the delta is -1 - 2|a|, at most -3.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-sign-times-an-absolute-value-of-one-thing-is-that-thing",
                MatchPattern.Commutative<Mulf>(MatchPattern.Node<Signumf>(MatchPattern.Any("a")), MatchPattern.Node<Absf>(MatchPattern.Any("a"))),
                bound => bound["a"],
                Soundness.SoundUnderAssumptions,
                description: "sgn(a) * abs(a) = a",
                // Three nodes around two copies of the operand become the one copy that is left, so
                // the delta is -(3 + |a|): at most -4.
                growth: RewriteRuleGrowth.Collects),
            new MatchedRule(
                "a-reciprocal-rational-factor-is-a-division",
                MatchPattern.Commutative<Mulf>(MatchPattern.Any<Entity>("r", one => Functions.Patterns.IsWholeReciprocal(one, 1)), MatchPattern.Any("a")),
                bound => bound["a"] / Functions.Patterns.DenominatorOf(bound["r"]),
                Soundness.Sound,
                description: "a * (1 / c) = a / c, for a rational c",
                // The reciprocal is a single rational leaf, and its denominator is a single leaf
                // under a quotient in place of the product: one node for one, whatever a is.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-negated-reciprocal-rational-factor-is-a-negated-division",
                MatchPattern.Commutative<Mulf>(MatchPattern.Any<Entity>("r", one => Functions.Patterns.IsWholeReciprocal(one, -1)), MatchPattern.Any("a")),
                bound => -(bound["a"] / Functions.Patterns.DenominatorOf(bound["r"])),
                Soundness.Sound,
                description: "a * (-1 / c) = -(a / c), for a rational c",
                // As above, and the negation put round the quotient is two nodes more, whatever
                // a is.
                growth: RewriteRuleGrowth.Expands),
            // Parity, over the whole complex plane. The poles of the odd ones sit symmetrically
            // about zero -- tan(-z) is undefined exactly where tan(z) is -- so the domain neither
            // widens nor narrows and no condition is owed.
            // https://github.com/asc-community/AngouriMath/issues/929
            new MatchedRule(
                "an-even-function-of-a-negative-multiple-drops-the-sign-cos",
                MatchPattern.Node<Cosf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => new Cosf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "cos(-a) = cos(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // nothing else moves: the same size whatever the argument is.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "an-even-function-of-a-negative-multiple-drops-the-sign-secant",
                MatchPattern.Node<Secantf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => new Secantf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "sec(-a) = sec(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // nothing else moves: the same size whatever the argument is.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "an-even-function-of-a-negative-multiple-drops-the-sign-abs",
                MatchPattern.Node<Absf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => new Absf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "abs(-a) = abs(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // nothing else moves: the same size whatever the argument is.
                growth: RewriteRuleGrowth.Rearranges),
            // A numeric factor that carries a phase comes out of a modulus as its own modulus,
            // |i * x| = |x|. The modulus is multiplicative on the whole plane, so nothing is
            // assumed; the factor is taken only where its modulus is exact, and never a real one,
            // which carries no phase. https://github.com/asc-community/AngouriMath/issues/1186
            new MatchedRule(
                "a-modulus-takes-a-numeric-factor-out",
                MatchPattern.Node<Absf>(MatchPattern.Commutative<Mulf>(MatchPattern.Any<Entity>("c", c => AngouriMath.Functions.TreeAnalyzer.ExactModulus(c) is not null), MatchPattern.Any("rest"))),
                bound => AngouriMath.Functions.TreeAnalyzer.ExactModulus(bound["c"])! * new Absf(bound["rest"]),
                Soundness.Sound,
                description: "abs(c * a) = |c| * abs(a)",
                // The factor moves from inside the modulus to outside it as one number, and the
                // modulus stays over the rest: the same size whatever the rest is.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-modulus-takes-a-numeric-dividend-out",
                MatchPattern.Node<Absf>(MatchPattern.Node<Divf>(MatchPattern.Any<Entity>("c", c => AngouriMath.Functions.TreeAnalyzer.ExactModulus(c) is not null), MatchPattern.Any("rest"))),
                bound => AngouriMath.Functions.TreeAnalyzer.ExactModulus(bound["c"])! / new Absf(bound["rest"]),
                Soundness.Sound,
                description: "abs(c / a) = |c| / abs(a)",
                // As above: one number moves out and the modulus stays over the rest.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "a-modulus-takes-a-numeric-divisor-out",
                MatchPattern.Node<Absf>(MatchPattern.Node<Divf>(MatchPattern.Any("rest"), MatchPattern.Any<Entity>("c", c => AngouriMath.Functions.TreeAnalyzer.ExactModulus(c) is not null))),
                bound => new Absf(bound["rest"]) / AngouriMath.Functions.TreeAnalyzer.ExactModulus(bound["c"])!,
                Soundness.Sound,
                description: "abs(a / c) = abs(a) / |c|",
                // As above: one number moves out and the modulus stays over the rest.
                growth: RewriteRuleGrowth.Rearranges),
            new MatchedRule(
                "an-odd-function-of-a-negative-multiple-negates-sin",
                MatchPattern.Node<Sinf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => -new Sinf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "sin(-a) = -sin(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // the negation the replacement puts round the whole of it is two nodes more.
                growth: RewriteRuleGrowth.Expands),
            new MatchedRule(
                "an-odd-function-of-a-negative-multiple-negates-tan",
                MatchPattern.Node<Tanf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => -new Tanf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "tan(-a) = -tan(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // the negation the replacement puts round the whole of it is two nodes more.
                growth: RewriteRuleGrowth.Expands),
            new MatchedRule(
                "an-odd-function-of-a-negative-multiple-negates-cotan",
                MatchPattern.Node<Cotanf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => -new Cotanf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "cotan(-a) = -cotan(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // the negation the replacement puts round the whole of it is two nodes more.
                growth: RewriteRuleGrowth.Expands),
            new MatchedRule(
                "an-odd-function-of-a-negative-multiple-negates-cosecant",
                MatchPattern.Node<Cosecantf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => -new Cosecantf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "cosec(-a) = -cosec(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // the negation the replacement puts round the whole of it is two nodes more.
                growth: RewriteRuleGrowth.Expands),
            new MatchedRule(
                "an-odd-function-of-a-negative-multiple-negates-signum",
                MatchPattern.Node<Signumf>(MatchPattern.Node<Mulf>(MatchPattern.Any<Real>("neg", real => real.IsNegative), MatchPattern.Any("rest"))),
                bound => -new Signumf((-(Real)bound["neg"]) * bound["rest"]),
                Soundness.Sound,
                description: "sgn(-a) = -sgn(a)",
                // The negative literal inside is replaced by its magnitude, one node for one, and
                // the negation the replacement puts round the whole of it is two nodes more.
                growth: RewriteRuleGrowth.Expands));

        /// <summary>
        /// Every <see cref="MatchedRuleSet"/> this class declares — the parameterless ones as
        /// properties, and <see cref="Sort"/>/<see cref="CommonDenominator"/> at every
        /// <see cref="TreeAnalyzer.SortLevel"/>, since a set parameterised by a sort level is a
        /// <b>method</b>, not a property, and enumerating properties alone would silently miss it.
        /// </summary>
        /// <remarks>
        /// Declared last in this file on purpose: it reflects over every member declared above it,
        /// and a static field initialiser runs in declaration order, so it must run after all of
        /// them have their backing fields set. Moving it earlier in the file would have it read
        /// some of those sets as their default (null).
        /// </remarks>
        [ConstantField]
        internal static readonly IReadOnlyList<MatchedRuleSet> All = BuildAll();

        private static IReadOnlyList<MatchedRuleSet> BuildAll()
        {
            const System.Reflection.BindingFlags Any =
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static;

            var sets = typeof(MatchedRules)
                .GetProperties(Any)
                .Where(property => property.PropertyType == typeof(MatchedRuleSet))
                .Select(property => (MatchedRuleSet)property.GetValue(null)!)
                .ToList();

            var factories = typeof(MatchedRules)
                .GetMethods(Any)
                .Where(method => method.ReturnType == typeof(MatchedRuleSet)
                                 && method.GetParameters() is { Length: 1 } only
                                 && only[0].ParameterType == typeof(TreeAnalyzer.SortLevel));
            foreach (var factory in factories)
#pragma warning disable IL3050
                foreach (var level in System.Enum.GetValues(typeof(TreeAnalyzer.SortLevel)))
                    sets.Add((MatchedRuleSet)factory.Invoke(null, new[] { level })!);
#pragma warning restore IL3050

            return sets.OrderBy(set => set.Name, System.StringComparer.Ordinal).ToList();
        }
    }
}
