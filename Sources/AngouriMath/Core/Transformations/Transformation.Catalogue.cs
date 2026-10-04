//
// Copyright (c) 2019-2026 Angouri.
// AngouriMath is licensed under MIT.
// Details: https://github.com/asc-community/AngouriMath/blob/master/LICENSE.md.
// Website: https://am.angouri.org.
//

using System;
using System.Collections.Generic;
using System.Linq;
using AngouriMath.Core.Budgets;
using AngouriMath.Functions;
using AngouriMath.Functions.Algebra;
using static AngouriMath.Entity;

namespace AngouriMath.Core.Transformations
{
    partial class Transformation
    {
        /// <summary>
        /// Applies a rewrite rule set once over every node.
        /// </summary>
        /// <param name="ruleSet">The set to apply; see <see cref="RewriteRules"/>.</param>
        public static Transformation Rewriting(RewriteRuleSet ruleSet)
            => (ruleSet ?? throw new ArgumentNullException(nameof(ruleSet))).AsTransformation();

        /// <summary>
        /// One structural tidying pass — <see cref="Entity.InnerSimplified"/>. Cheap, and
        /// no pattern search.
        /// </summary>
        public static Transformation InnerSimplification { get; } = new InnerSimplificationTransformation();

        /// <summary>
        /// The full simplification pipeline at the default level, as
        /// <see cref="Entity.Simplify(int)"/> runs it.
        /// </summary>
        public static Transformation Simplification { get; } = SimplificationAtLevel(2);

        /// <summary>
        /// The full simplification pipeline at a chosen level.
        /// </summary>
        /// <param name="level">
        /// How hard to look; the same argument <see cref="Entity.Simplify(int)"/> takes.
        /// </param>
        public static Transformation SimplificationAtLevel(int level)
            => LevelledCache.Simplification.For(level, static l => new SimplificationTransformation(l, null));

        /// <summary>
        /// The full simplification pipeline at a chosen level, rating candidates by
        /// <paramref name="costModel"/> instead of <see cref="MathS.Settings.ComplexityCriteria"/>.
        /// </summary>
        /// <param name="level">
        /// How hard to look; the same argument <see cref="Entity.Simplify(int)"/> takes.
        /// </param>
        /// <param name="costModel">
        /// Which candidate counts as cheapest. <see cref="Core.CostModel.Default"/> here behaves
        /// exactly like <see cref="SimplificationAtLevel(int)"/> — passing it is how a caller
        /// states that on purpose rather than by leaving a parameter out.
        /// </param>
        /// <remarks>
        /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2 named
        /// this remaining on its own row: "a cost model that reaches an API rather than an ambient
        /// setting". <see cref="MathS.Settings.ComplexityCriteria"/> already <i>is</i> an API in
        /// the sense that a caller can scope it explicitly and safely — it is backed by the same
        /// <see cref="Convenience.Setting{T}"/> <see cref="MathS.Settings.Budget"/> uses, async-local rather
        /// than thread-static, so one caller's override cannot leak into another's concurrent call.
        /// What it lacked was a place in <em>this</em> API, the addressable one <see cref="Transformation"/>
        /// is: composing <c>SimplificationAtLevel(2, costModel).Then(...)</c> names the choice
        /// where setting an ambient value around a call does not. This overload does not
        /// reimplement candidate search against an explicit parameter threaded through
        /// <c>Simplificator</c> -- that would be a second pipeline to keep in step with the one
        /// <see cref="Entity.Simplify(int)"/> actually runs. It scopes the existing, already-tested
        /// setting for the duration of this one call instead of introducing a second pipeline.
        /// </remarks>
        public static Transformation SimplificationAtLevel(int level, CostModel costModel)
            => new SimplificationTransformation(
                level, costModel ?? throw new ArgumentNullException(nameof(costModel)));

        /// <summary>
        /// Multiplies products over sums out, as <see cref="Entity.Expand(int)"/> does.
        /// </summary>
        public static Transformation Expansion { get; } = ExpansionAtLevel(2);

        /// <summary>
        /// Multiplies products over sums out, for a chosen number of passes.
        /// </summary>
        /// <param name="level">The number of passes; the argument <see cref="Entity.Expand(int)"/> takes.</param>
        public static Transformation ExpansionAtLevel(int level)
            => LevelledCache.Expansion.For(level, static l => new ExpansionTransformation(l));

        /// <summary>
        /// Gathers common factors back out, as <see cref="Entity.Factorize(int)"/> does.
        /// </summary>
        public static Transformation Factorization { get; } = FactorizationAtLevel(2);

        /// <summary>
        /// Gathers common factors back out, for a chosen number of passes.
        /// </summary>
        /// <param name="level">The number of passes; the argument <see cref="Entity.Factorize(int)"/> takes.</param>
        /// <remarks>
        /// Built out of the registry rather than written again: one pass is the perfect
        /// square rules, then the factorisation rules, then a tidying pass, and the level is
        /// how many times that runs.
        /// </remarks>
        public static Transformation FactorizationAtLevel(int level)
            => LevelledCache.Factorization.For(level, static l =>
                RuleBasedFactorizationAtLevel(l)
                    // And then the polynomial layer, which factors what no rule set has a rule
                    // for. Last, so that the rules keep every answer they already gave and this
                    // only ever adds one. See PolynomialFactorization.
                    .Then(PolynomialFactorization)
                    // And last of all the numeric content, which is the only step here that
                    // `Simplify` deliberately does not get: `2 * (x + 2 * a)` is a node larger
                    // than `4 * a + 2 * x`, so the cost model will not take it and only a caller
                    // who asked to factorise wants it. https://github.com/asc-community/AngouriMath/issues/195
                    .Then(NumericContentExtraction));

        /// <summary>
        /// Takes the whole number every term of a sum divides by out in front of it:
        /// <c>2x + 4a</c> becomes <c>2 * (x + 2a)</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The "forcefully" of
        /// <a href="https://github.com/asc-community/AngouriMath/issues/195">#195</a>.</b> A factor
        /// appearing <i>identically</i> in every term already comes out peacefully — <c>2x + 2a</c>
        /// is <c>2 * (a + x)</c> under plain <see cref="Entity.Simplify(int)"/> — and a common
        /// <b>divisor</b> does not, because the result is larger and
        /// <c>Entity.SimplifiedRate</c> will not choose it.
        /// </para>
        /// <para>
        /// Offered on its own as well as inside <see cref="FactorizationAtLevel"/>, because it is
        /// the one step of factorisation whose answer a caller might want without the rest.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// Held in a nested class rather than in a static field of this one, and that is not a
        /// style choice. <see cref="Factorization"/> is declared <i>above</i> this and calls
        /// <see cref="FactorizationAtLevel"/> in its own initialiser, which reads this — and a
        /// static field initialiser runs in declaration order, so it would read <see langword="null"/>
        /// and fail as a <c>TypeInitializationException</c> from somewhere else entirely. A nested
        /// type initialises on first use instead, whatever order the fields are written in.
        /// </remarks>
        public static Transformation NumericContentExtraction => Held.Instance;

        private static class Held
        {
            [ConstantField]
            internal static readonly Transformation Instance = new NumericContentTransformation();
        }

        private sealed class NumericContentTransformation : Transformation
        {
            public override string Name => "numeric-content";
            public override TransformationRelation Relation => TransformationRelation.Equivalence;
            public override Soundness Soundness => Soundness.Sound;

            // Never null. `Then` propagates a null from its second half as the whole chain having
            // no answer, so a step that says "I did not apply" by returning null does not step
            // aside -- it discards everything the steps before it produced. Returning the input
            // unchanged is how the polynomial layer above does it and for the same reason: this
            // only ever adds an answer to the ones already given.
            protected override Entity? ApplyCore(Entity input)
                => Functions.NumericContent.Extracted(input);
        }

        /// <summary>
        /// Writes an expression as a single fraction, as <see cref="Entity.AsSingleFraction"/>
        /// does: one numerator over one denominator, nothing divided inside either, and nothing
        /// cancelled or multiplied out. Where there is no division the input comes back, and where
        /// dividing by a fraction moves its denominator into the numerator the answer says that
        /// denominator is nonzero.
        /// </summary>
        /// <remarks>
        /// Held in a nested class for the reason <see cref="NumericContentExtraction"/> is.
        /// </remarks>
        public static Transformation AsSingleFraction => SingleFractionHolder.Instance;

        private static class SingleFractionHolder
        {
            [ConstantField]
            internal static readonly Transformation Instance = new SingleFractionTransformation();
        }

        private sealed class SingleFractionTransformation : Transformation
        {
            public override string Name => "single-fraction";
            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            // Sound: a sum or a product of quotients is defined exactly where each quotient is,
            // and so is the fraction it is gathered into, since nothing is cancelled. Turning a
            // quotient over is the one step that moves a denominator out of the way:
            // (a/b)/(c/d) is (a d)/(b c), which has a value where d is zero and the expression
            // has none, so the answer carries `provided not d = 0`.
            public override Soundness Soundness => Soundness.Sound;

            // The two halves are tidied each on its own -- the operands put in order and like
            // terms collected, which is Simplify's own tidying pass without its search, so
            // `t + 1 + t^2 + 1` is `2 + t + t^2` -- and the quotient is not, so that no factor of
            // the numerator meets one of the denominator and cancels.
            protected override Entity? ApplyCore(Entity input)
            {
                // A number is already what it is; there is nothing in it to gather.
                if (input is Entity.Number)
                    return input;
                // A rational number is a fraction too, as it is written: 2/3 + x/2 is (3 x + 4)/6,
                // not (x + 4/3)/2. Inside a function's argument it folds back when its half is
                // tidied, since the argument is not gathered.
                var written = input.Replace(static node => node is Entity.Number.Rational { ERational: var value } and not Entity.Number.Integer
                    ? new Entity.Divf(Entity.Number.Integer.Create(value.Numerator), Entity.Number.Integer.Create(value.Denominator))
                    : node);
                var carried = new List<Entity>();
                var (numerator, denominator) = Functions.SingleQuotient.OverLeastCommonDenominator(written, carried);
                if (denominator == Entity.Number.Integer.One && carried.Count == 0)
                    return input;
                var top = Tidied(numerator);
                var bottom = Tidied(denominator);
                var fraction = bottom == Entity.Number.Integer.One ? top : new Entity.Divf(top, bottom);

                // Only what the new denominator does not already exclude: in (1/x)/(1/x), which
                // is x/x, the x carried up is still in the denominator.
                var nonzero = new List<Entity>();
                foreach (var factor in carried)
                {
                    var tidied = Tidied(factor);
                    if (!AlreadyNonzero(tidied, bottom) && !nonzero.Contains(tidied))
                        nonzero.Add(tidied);
                }
                if (nonzero.Count == 0)
                    return fraction;
                var condition = !nonzero[0].EqualTo(0);
                for (var i = 1; i < nonzero.Count; i++)
                    condition &= !nonzero[i].EqualTo(0);
                return new Entity.Providedf(fraction, condition);
            }

            // Simplify's tidying pass, and the neatening it ends on, which writes `x^2 + -1` as
            // `x^2 - 1`.
            private static Entity Tidied(Entity half)
                => Functions.Simplificator.SimplifyChildren(half).Rewrite(RewriteRules.NumericNeat);

            // Whether `denominator` being nonzero already says `factor` is: a nonzero number
            // always is, and otherwise each factor of `factor` has to be one of the
            // denominator's, up to a whole positive power, since a product vanishes exactly where
            // one of its factors does and a power exactly where its base does.
            private static bool AlreadyNonzero(Entity factor, Entity denominator)
            {
                var excluded = Entity.Mulf.LinearChildren(denominator).Select(Base).ToList();
                return Entity.Mulf.LinearChildren(factor).All(piece =>
                    piece is Entity.Number.Complex { IsZero: false } || excluded.Contains(Base(piece)));

                static Entity Base(Entity piece)
                    => piece is Entity.Powf(var @base, Entity.Number.Integer power) && power.EInteger.Sign > 0 ? @base : piece;
            }
        }

        /// <summary>
        /// The rule-based half of <see cref="FactorizationAtLevel"/>, without the polynomial
        /// layer.
        /// </summary>
        /// <remarks>
        /// Separate because <c>Simplify</c> offers a factorisation as a <b>candidate</b> and its
        /// cost model decides. The metric prefers the expanded form — <c>x ^ 6 - 1</c> rates 12
        /// expanded against 58 factored — so a factored candidate wins only where the two are
        /// closest, and those turn out to be the places a factored answer is least wanted:
        /// <c>x ^ 3 / 3 + x ^ 2 / 2</c> becomes <c>(3 + 2 * x) * x ^ 2 / 6</c>, an antiderivative
        /// in a form nobody writes. Offering the layer to that search is
        /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2's
        /// pluggable cost model rather than
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1018">#1018</a>.
        /// </remarks>
        public static Transformation RuleBasedFactorizationAtLevel(int level)
            => LevelledCache.RuleBasedFactorization.For(level, static l =>
                Rewriting(RewriteRules.PerfectSquare)
                    .Then(Rewriting(RewriteRules.Factorization))
                    .Then(InnerSimplification)
                    // Entity.Factorize has always run at least one pass, whatever it was asked for.
                    .Repeat(Math.Max(l, 1)));

        /// <summary>
        /// Factors a polynomial by the polynomial layer — square-free decomposition, Zassenhaus
        /// over <c>Q</c>, Kronecker's substitution and Hensel lifting — rather than by a rule.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A rule set factors what someone wrote a rule for. <c>x ^ 2 - 1</c> has one and
        /// <c>x ^ 3 - 1</c> does not, which is why <see cref="Entity.Factorize(int)"/> — the
        /// operation whose entire job is factorisation — was worse at it than the machinery that
        /// exists for it. <a href="https://github.com/asc-community/AngouriMath/issues/1018">#1018</a>
        /// </para>
        /// <para>
        /// <b>This is not the cost-model question.</b> The reason the polynomial layer is not
        /// wired into <c>Simplify</c> is that <c>SimplifiedRate</c> prefers the expanded
        /// form — <c>x ^ 6 - 1</c> rates 12 expanded against 58 factored — so a factored candidate
        /// could never win a search. There is no search here: <c>Factorize</c> is asked for the
        /// factored form and returns it.
        /// </para>
        /// <para>
        /// <b>Which variable.</b> The layer factors with respect to one, and an expression has
        /// several. Each of its variables is tried in turn and the first that yields a genuine
        /// product wins, which is deterministic because <see cref="Entity.Vars"/> is. Trying them
        /// all rather than guessing a main one is what makes <c>x ^ 4 - y ^ 4</c> come out whole
        /// however the caller wrote it.
        /// </para>
        /// <para>
        /// <b>What it declines.</b> Anything that is not a polynomial, anything the layer refuses,
        /// and anything whose answer is not a product — a refusal leaves the expression exactly as
        /// the rules left it, so nothing that factored before can stop factoring.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// Held in a nested class rather than in a static field of this one. Static field
        /// initialisers run in declaration order, and <see cref="Factorization"/> — declared
        /// above — is eager, so a field here would still be <see langword="null"/> when its
        /// pipeline is built. That surfaces as <c>ArgumentNullException(nameof(next))</c> from a
        /// combinator rather than as anything naming the real cause, and this file has had that
        /// failure before. A nested type is initialised on first touch, whatever order this
        /// one's members are written in.
        /// </remarks>
        public static Transformation PolynomialFactorization => PolynomialFactorizationHolder.Instance;

        private static class PolynomialFactorizationHolder
        {
            [ConstantField]
            internal static readonly Transformation Instance = new PolynomialFactorizationTransformation();
        }

        /// <summary>
        /// Puts commutative chains into a canonical order, so that expressions which differ
        /// only in the arrangement of their operands become the same tree.
        /// </summary>
        /// <remarks>
        /// Not a simplification: it makes an expression comparable, not shorter. It has
        /// always been available inside <c>Simplify</c> and had no name of its own until
        /// this layer gave the rule set one.
        /// </remarks>
        public static Transformation Normalization { get; }
            = Rewriting(RewriteRules.CanonicalOrder).Then(InnerSimplification);

        /// <summary>
        /// A canonical form for <b>rational functions over <c>Q</c></b>: two expressions
        /// denoting the same quotient of polynomials become the identical tree, so equality on
        /// that sublanguage is decided by comparing nodes rather than by searching.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It answers only where it can.</b> There is no canonical form for the whole
        /// language — zero-equivalence is undecidable once <c>pi</c>, the exponential, the
        /// trigonometric functions and <c>abs</c> are in play (Richardson, 1968) — so anything
        /// that is not a rational function over <c>Q</c> in its free variables gets no answer
        /// at all. That refusal is the point: a form whose value is that equal trees mean equal
        /// expressions must not quietly hand back a normalisation that merely resembles one.
        /// </para>
        /// <para>
        /// The expression is written as a single fraction, as <see cref="AsSingleFraction"/>
        /// writes it -- without which <c>1/x + 1/y</c> and <c>(x + y)/(x*y)</c> could never
        /// meet -- then reduced by the multivariate greatest common divisor and scaled so the
        /// denominator is monic in the lexicographic monomial order.
        /// </para>
        /// <para>
        /// <b>Cancelling carries its condition.</b> <c>x/x</c> is not <c>1</c>, so where a
        /// factor of positive degree comes out the answer says the factor is nonzero, as the
        /// rest of the library already does; and so does turning a quotient over, since
        /// <c>1/(1/x)</c> is not <c>x</c> either. Gathering over a common denominator widens
        /// nothing by itself: a sum is defined exactly where its terms are.
        /// <a href="https://github.com/asc-community/AngouriMath/issues/1618">#1618</a>
        /// </para>
        /// <para>
        /// Nothing runs this by default. See
        /// <c>Docs/Contributing/CanonicalForm.md</c> §5 and
        /// <a href="https://github.com/asc-community/AngouriMath/issues/934">#934</a>.
        /// <see cref="Canonicalization"/> is the companion that handles the commutative
        /// structure of expressions generally.
        /// </para>
        /// </remarks>
        public static Transformation RationalCanonicalization { get; }
            = new RationalCanonicalizationTransformation();

        /// <summary>
        /// A canonical form for the commutative structure: two expressions differing only in
        /// how their sums, products, conjunctions, disjunctions and set operations are
        /// arranged or nested come out as the identical tree.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Normalise, order, normalise</b>, and the first of those is what makes it work.
        /// The order's key depends on a node's class and the normalisation changes classes:
        /// in <c>1/2 - x</c> the constant reaches an unprepared sort as <c>1 * 2 ^ (-1)</c>,
        /// a product, and is ordered against <c>-x</c> as one, but folds to the number
        /// <c>1/2</c> immediately afterwards — so the next pass orders it the other way and
        /// the two alternate. Sorting a tree that has already settled sorts what the tree is
        /// actually going to be.
        /// </para>
        /// <para>
        /// Measured over 834 generated expressions and 2738 ordered pairs by
        /// <c>Sources/Tests/Harnesses/CanonCheck</c>: <b>idempotent, and independent of the order
        /// the operands were written in</b>, where ordering without the leading normalisation
        /// fails 18 of the first and <see cref="InnerSimplification"/> alone fails 2024 of the
        /// second. Its <c>canoncheck-baseline.tsv</c> lists every one.
        /// </para>
        /// <para>
        /// <b>What it is not.</b> It is not a canonical form for the language — no such thing
        /// exists, since zero-equivalence is undecidable here — and it is not a
        /// simplification, since it makes an expression comparable rather than shorter.
        /// Equal trees mean the expressions are equal; different trees mean nothing at all.
        /// <c>Docs/Contributing/CanonicalForm.md</c> states the boundary and what is still owed.
        /// </para>
        /// <para>
        /// <b>Nesting goes with order.</b> The sort works over commutative chains rather than
        /// over one node, so it flattens as it sorts: <c>(x + y) + a</c> and <c>x + (y + a)</c>
        /// both reach <c>a + x + y</c>, and reach it as the same tree. That is worth saying
        /// because <see cref="InnerSimplification"/> alone leaves those two as different trees
        /// which <i>print identically</i> — associativity being normalised on the way out
        /// rather than in the expression — so a comparison of printed forms cannot tell the
        /// two situations apart.
        /// </para>
        /// <para>
        /// Nothing in the library runs this: it is offered, not applied. Putting it inside
        /// <see cref="InnerSimplification"/> would move every commutative operand order in
        /// every printed answer at once, which is a decision for a release rather than for a
        /// transformation. <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
        /// tier 1.
        /// </para>
        /// </remarks>
        public static Transformation Canonicalization { get; }
            = InnerSimplification
                .Then(Rewriting(RewriteRules.CanonicalOrderExact))
                .Then(InnerSimplification);

        /// <summary>
        /// Clears a surd out of a two-term denominator: <c>1 / (sqrt(3) + 5)</c> becomes
        /// <c>(sqrt(3) - 5) / (-22)</c>.
        /// </summary>
        public static Transformation Rationalization { get; }
            = Rewriting(RewriteRules.RationalizeDenominator).Then(InnerSimplification);

        /// <summary>
        /// Explores the equalities the whole registry's rules reach from an expression at once,
        /// over an e-graph, and extracts the cheapest under <paramref name="costModel"/>.
        /// </summary>
        /// <param name="budget">
        /// What this call may spend before it settles for the best it has found so far.
        /// <see cref="WorkBudget.Steps"/> is charged once per e-node the graph actually creates;
        /// <see cref="WorkBudget.Time"/> is the wall-clock backstop. A caller who sets
        /// <see cref="MathS.Settings.Budget"/> overrides this, the same as every other bounded
        /// computation in the library.
        /// </param>
        /// <param name="costModel">Which candidate counts as cheapest once exploration stops.</param>
        /// <remarks>
        /// <para>
        /// <b>Nothing runs this by default</b> — the same standing as
        /// <see cref="RationalCanonicalization"/> and <see cref="Canonicalization"/>, and for a
        /// sharper reason: <c>Simplify</c> applies a rule set once, keeps a candidate and moves on,
        /// so an expanding rule and a collecting one never meet — the order they run in decides
        /// which wins. Equality saturation deletes that order and keeps every result, which is
        /// why it needs a budget rather than a pass count, and why only the rules a scheduler can
        /// prove will not run away are offered to it — see the next paragraph.
        /// </para>
        /// <para>
        /// <b>Only rules whose <see cref="Matching.MatchedRule.Growth"/> is exactly
        /// <see cref="RewriteRuleGrowth.Collects"/> or <see cref="RewriteRuleGrowth.Rearranges"/>,
        /// and whose <see cref="Matching.MatchedRule.Soundness"/> is at least
        /// <see cref="Soundness.SoundUnderAssumptions"/>, are used</b> — the population is
        /// <see cref="Matching.MatchedRules.All"/>, not the public registry. Growth is derived
        /// from the pattern tree for a rule whose replacement is itself a pattern, and declared
        /// explicitly by the rule's own author for a rule whose replacement is code, which is what
        /// let a further batch of code-built rules earn a place here without being able to lie
        /// about it. <see cref="RewriteRuleGrowth.Unknown"/> is withheld either way: a rule nobody
        /// has justified is not proven safe, and not proven safe is not the same as safe. This is
        /// <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a> tier 2's
        /// e-graph. As of this writing the filter passes 43 rules.
        /// </para>
        /// <para>
        /// <b>Real e-matching now runs wherever a rule's pattern supports it, and this only falls
        /// back to materialising a term where it cannot.</b> A rule's
        /// <see cref="Matching.MatchPattern.CanEMatch"/> on its <see cref="Matching.MatchedRule.Left"/>
        /// decides per rule: where it is true this asks the e-graph directly, which is what a
        /// production e-matcher over <see cref="Matching.MatchPattern"/> is supposed to do; where
        /// it is false this falls back to extracting a term and rewriting that instead — slower,
        /// and the only path the harness this is built from ever took. Of the 43 rules the filter
        /// above passes, roughly 27-28 can actually build a replacement today; the remaining ~15
        /// each build a boolean connective or a turned-around equality (<c>and</c>, <c>or</c>,
        /// <c>not</c>, <c>xor</c>, <c>implies</c>, <c>=</c>) and are correctly classified as safe,
        /// but cannot fire, because <see cref="EGraph"/>'s reconstruction whitelist has no entry
        /// for any of those node types yet and <c>EGraph.Extract</c> returns nothing for a
        /// class that needs one. That is a separate, already-known limitation of
        /// <see cref="EGraph"/> itself — see <c>Docs/Contributing/EqualitySaturationReviewFindings.md</c>
        /// — not a defect in how these 15 rules were classified. The day the whitelist widens to
        /// cover those six node types, all 15 go live at once, which is the moment to re-run
        /// <c>work/egraph</c>, not before.
        /// </para>
        /// <para>
        /// <b>What generalises and what does not.</b> The <c>work/egraph</c> harness's original
        /// measurement — a textbook corpus of 16 expressions, all of which saturated — was made
        /// under a much <i>larger</i> rule set (313 rules, off the public registry's string-length
        /// <see cref="RewriteRuleGrowth"/> proxy), before the <see cref="Soundness"/> filter existed
        /// and before any rule here could e-match at all, so it does not describe this population —
        /// a different source, a different filter, a different matcher — and should not be cited as
        /// though it still does without being re-run. Even re-run, a corpus
        /// saturating says the graph stopped growing on those inputs; it did not and could not say
        /// that of every expression <c>Simplify</c> is asked to handle. Pass a budget that reflects
        /// that this is still being found out, not one sized for how much the caller can afford to
        /// lose.
        /// </para>
        /// </remarks>
        public static Transformation EqualitySaturation(WorkBudget budget, CostModel costModel)
            => new EqualitySaturationTransformation(
                budget ?? throw new ArgumentNullException(nameof(budget)),
                costModel ?? throw new ArgumentNullException(nameof(costModel)));

        /// <summary>
        /// Test-only visibility into how many rules <see cref="EqualitySaturation"/> currently
        /// draws from -- the field itself is <see langword="private"/> on a
        /// <see langword="private"/> nested class, which a test in another assembly cannot reach
        /// any other way. Exists so a collapse back toward the old
        /// all-<see cref="RewriteRuleGrowth.Unknown"/> state — the rule population silently
        /// shrinking to nothing, the way it once did before this was measured — fails a test
        /// instead of going unmeasured again.
        /// </summary>
        internal static int EqualitySaturationSafeRuleCount => EqualitySaturationTransformation.SafeRuleCount;

        /// <summary>
        /// Brings an expression to the least of the forms the rules can reach from it, over an
        /// e-graph — <a href="https://github.com/asc-community/AngouriMath/issues/746">#746</a>
        /// tier 2's "canonicalisation framework built on" the rewrite graph.
        /// </summary>
        /// <param name="budget">What bounds the search. See <see cref="EqualitySaturation"/>.</param>
        /// <param name="widest">
        /// The widest <see cref="RewriteRuleGrowth"/> admitted, as a ceiling over
        /// <see cref="RewriteRuleGrowth.Collects"/>, <see cref="RewriteRuleGrowth.Rearranges"/>,
        /// <see cref="RewriteRuleGrowth.Expands"/>. This is the knob that matters — see the
        /// measurement below.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>How this differs from <see cref="Canonicalization"/>, which is a rule pass.</b> A
        /// pass rewrites and commits: having applied a rule it is standing on the result, and if
        /// recognising an equality needed a <i>larger</i> intermediate form, the pass cannot get
        /// there and back. A graph does not commit — it holds every form at once and chooses at
        /// the end — so it can pass through a bigger writing to reach a smaller one.
        /// </para>
        /// <para>
        /// <b>That difference is measured, and the measurement says the ceiling matters less than
        /// it looks.</b> Two rewrite chains run from each of 1458 generated expressions produced
        /// <b>no divergent pair at all</b> under <see cref="RewriteRuleGrowth.Collects"/> ∪
        /// <see cref="RewriteRuleGrowth.Rearranges"/>: those rules are confluent on that corpus,
        /// so a pass already reaches the same form a graph would, and over that ceiling this
        /// earns nothing at all. Widening it barely helps — over six expression pairs equal only
        /// through a larger intermediate form, <see cref="RewriteRuleGrowth.Rearranges"/> proved
        /// two and <see cref="RewriteRuleGrowth.Expands"/>, all nine expanding rules added, proved
        /// <i>the same two</i>. Only <see cref="RewriteRuleGrowth.Unknown"/> — which admits the
        /// 270 rules whose growth nobody judged, against 52 that were — proved a third.
        /// </para>
        /// <para>
        /// <b>Why the default is nonetheless the narrow ceiling.</b> Because the widest one is
        /// where the risk is, not merely where the rules are: the <c>work/egraph</c> harness
        /// measured a 7,147× blow-up over the undirected rule set — a different mechanism (term
        /// enumeration, no budget, no pre-filter) on the same question. Six pairs completing
        /// inside their budget is not evidence against that. So each step up is offered and none
        /// is assumed, and the caller who takes one is the caller who sets the budget.
        /// </para>
        /// <para>
        /// <b>What this is not.</b> Not a canonical form <i>for the language</i> — no such thing
        /// exists here, since zero-equivalence is undecidable, and
        /// <c>Docs/Contributing/CanonicalForm.md</c> states that boundary. It is a canonical form
        /// <i>modulo these rules, this budget, and the answer having settled</i>: equal trees mean
        /// the rules proved the two expressions equal, different trees mean they did not, and a
        /// budget that ran out is reported rather than hidden. The run stops once the least
        /// member of the input's class has survived two passes unchanged, which is the fixed
        /// point that matters to an extraction and comes where the graph's own may never — on
        /// a rational coefficient beside a variable the regrouping rules add a member on every
        /// pass for as long as they are allowed (<a href="https://github.com/asc-community/AngouriMath/issues/1200">#1200</a>),
        /// and the answer was settled on the third. Measured on the corpus and on those inputs,
        /// no answer moved and the nine of them went from two seconds to milliseconds. Nothing
        /// in the library calls this — like <see cref="Canonicalization"/> it is offered, not
        /// applied.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// <para>
        /// <b>Built on <see cref="Canonicalization"/> rather than beside it.</b> The graph does
        /// not sort a commutative operand pair: the rules that do build their replacement in code,
        /// so their <see cref="RewriteRuleGrowth"/> is <see cref="RewriteRuleGrowth.Unknown"/> and
        /// no ceiling admits them — measured, by <c>x + y</c> and <c>y + x</c> canonicalising to
        /// themselves. Nor does it flatten <c>(x + y) + a</c> against <c>x + (y + a)</c>, which
        /// are different trees that print alike. The rule pass does both, is already measured
        /// idempotent and order-independent, and is not improved by being written again — so it
        /// runs on each side of the graph step: once so that equal inputs enter the graph as one
        /// tree, and once so that what extraction rebuilt leaves as one.
        /// </para>
        /// </remarks>
        public static Transformation CanonicalizationOverGraph(WorkBudget budget, RewriteRuleGrowth widest)
            => Canonicalization
                .Then(new GraphCanonicalizationTransformation(
                    budget ?? throw new ArgumentNullException(nameof(budget)), widest))
                .Then(Canonicalization);

        /// <summary>
        /// <see cref="CanonicalizationOverGraph(WorkBudget, RewriteRuleGrowth)"/> over the rules
        /// that do not expand, which is the ceiling
        /// <see cref="EqualitySaturation"/> also draws from.
        /// </summary>
        /// <param name="budget">What bounds the search.</param>
        public static Transformation CanonicalizationOverGraph(WorkBudget budget)
            => CanonicalizationOverGraph(budget, RewriteRuleGrowth.Rearranges);

        /// <summary>
        /// Replaces every occurrence of <paramref name="what"/> with
        /// <paramref name="with"/>, as <see cref="Entity.Substitute(Entity, Entity)"/> does.
        /// </summary>
        /// <param name="what">The subexpression to replace.</param>
        /// <param name="with">What to put in its place.</param>
        public static Transformation Substitution(Entity what, Entity with)
            => new SubstitutionTransformation(
                what ?? throw new ArgumentNullException(nameof(what)),
                with ?? throw new ArgumentNullException(nameof(with)));

        /// <summary>
        /// The symbolic derivative over <paramref name="variable"/>, as
        /// <see cref="Entity.Differentiate(Variable)"/> computes it.
        /// </summary>
        /// <param name="variable">The variable to differentiate over.</param>
        public static Transformation Differentiation(Variable variable)
            => new DifferentiationTransformation(variable ?? throw new ArgumentNullException(nameof(variable)));

        /// <summary>
        /// The antiderivative over <paramref name="variable"/>, <b>without</b> the constant
        /// of integration — <see cref="Entity.Integrate(Variable)"/> is what adds it.
        /// </summary>
        /// <param name="variable">The variable to integrate over.</param>
        /// <remarks>
        /// Where no antiderivative is found this says so, by having no answer.
        /// <see cref="Entity.Integrate(Variable)"/> hands back an unevaluated
        /// <see cref="Entity.Integralf"/> instead, which is the same claim in the shape 1.x
        /// callers expect.
        /// </remarks>
        public static Transformation Integration(Variable variable)
            => new IntegrationTransformation(variable ?? throw new ArgumentNullException(nameof(variable)));

        /// <summary>
        /// The limit over <paramref name="variable"/> as it approaches
        /// <paramref name="destination"/> from <paramref name="side"/>.
        /// </summary>
        /// <param name="variable">The variable that approaches.</param>
        /// <param name="destination">Where it approaches.</param>
        /// <param name="side">From which side.</param>
        /// <remarks>
        /// The limit as computed, not further simplified — <see cref="Entity.Limit(Variable, Entity, ApproachFrom)"/>
        /// is what tidies it. Where the limit cannot be settled this has no answer, rather
        /// than the unevaluated <see cref="Entity.Limitf"/> node the 1.x method returns;
        /// neither is a claim that the limit does not exist.
        /// </remarks>
        public static Transformation LimitAt(Variable variable, Entity destination, ApproachFrom side)
            => new LimitTransformation(
                variable ?? throw new ArgumentNullException(nameof(variable)),
                destination ?? throw new ArgumentNullException(nameof(destination)),
                side);

        /// <summary>
        /// The transformations that differ only by a level, each built at most once, so that
        /// an ordinary <c>Simplify()</c> allocates nothing to reach its transformation.
        /// </summary>
        /// <remarks>
        /// Filled on demand rather than in the static initialiser, and it has to be. Building
        /// the factorisation entry reads <see cref="RewriteRules"/>, whose sets in turn build
        /// transformations of their own; doing that while this type is still initialising
        /// makes the two types depend on each other's initialisation, and one of them then
        /// reads the other's fields before they are set. Two threads arriving together may
        /// each build the same entry; the entries are equivalent and immutable, so whichever
        /// reference lands is the one everyone then uses.
        /// </remarks>
        private sealed class LevelledCache
        {
            // The levels Simplify, Expand and Factorize are actually asked for: the default
            // 2, the negated -level Simplify passes itself, and a little room either side.
            private const int Lowest = -4;
            private const int Highest = 4;

            [ConcurrentField]
            internal static readonly LevelledCache Simplification = new();
            [ConcurrentField]
            internal static readonly LevelledCache Expansion = new();
            [ConcurrentField]
            internal static readonly LevelledCache Factorization = new();

            [ConstantField]
            internal static readonly LevelledCache RuleBasedFactorization = new();

            private readonly Transformation?[] cached = new Transformation?[Highest - Lowest + 1];

            internal Transformation For(int level, Func<int, Transformation> make)
                => level < Lowest || level > Highest
                    ? make(level)
                    : cached[level - Lowest] ??= make(level);
        }

        private sealed class EqualitySaturationTransformation : Transformation
        {
            /// <summary>
            /// Every rule in <see cref="Matching.MatchedRules.All"/> whose
            /// <see cref="Matching.MatchedRule.Growth"/> is known not to expand and whose
            /// <see cref="Matching.MatchedRule.Soundness"/> is at least <see cref="Soundness.SoundUnderAssumptions"/>
            /// -- the real pattern-tree classification (Task 4), not the public registry's
            /// string-length proxy, and a per-rule <see cref="Soundness"/> check the previous,
            /// public-surface-sourced version of this field had no way to make (it filtered by
            /// Growth alone). Computed once: the registry does not change while the process runs.
            /// </summary>
            [ConstantField]
            private static readonly IReadOnlyList<Matching.MatchedRule> SafeRules
                = Saturation.SafeRules;

            /// <summary>
            /// <see cref="SafeRules"/>.Count, for <see cref="Transformation.EqualitySaturationSafeRuleCount"/>
            /// to forward -- this class is <see langword="private"/>, so even an
            /// <see langword="internal"/> member here is only visible within
            /// <see cref="Transformation"/>'s own body, never from another assembly.
            /// </summary>
            internal static int SafeRuleCount => SafeRules.Count;

            private readonly WorkBudget budget;
            private readonly CostModel costModel;

            internal EqualitySaturationTransformation(WorkBudget budget, CostModel costModel)
                => (this.budget, this.costModel) = (budget, costModel);

            public override string Name => $"equality-saturation[{costModel.Name}]";

            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            // The rules this draws from are a mix of Sound and SoundUnderAssumptions, and a
            // rule set's own tier is already the minimum over what it contains -- so the
            // weakest tier represented is the honest claim for the whole of what this used,
            // the same convention every rule set in the registry already follows.
            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            /// <remarks>
            /// <para>
            /// <b>What this reports to a <see cref="RewriteRecording"/>, and what it does not.</b>
            /// The pass as a whole, and nothing finer. A rule set records each firing because a
            /// firing there <i>is</i> the rewrite -- the node it matched leaves and the
            /// replacement takes its place. A firing here is not: it adds another member to an
            /// e-class, every member of which is already believed equal, and the answer is then
            /// chosen by <c>EGraph.Extract</c> from all of them at once. Most firings
            /// contribute nothing to what extraction picked, and none of them is a step on a route
            /// from the input to the output, because there is no route -- that is the whole point
            /// of saturating rather than rewriting. Reporting them as
            /// <see cref="RewriteStep"/>s would name rewrites that are not in the answer.
            /// </para>
            /// <para>
            /// So one edge, input to output, under this transformation's own
            /// <see cref="Name"/>. That is a true statement at the grain a derivation is read at,
            /// and it is what was missing: a caller who opened a recording round this used to get
            /// a real rewrite with an empty derivation, and nothing to distinguish
            /// "introspection cannot see this" from "there was nothing to see".
            /// </para>
            /// </remarks>
            protected override Entity? ApplyCore(Entity input)
            {
                // Read once, before the work: nothing here should pay for a recording nobody
                // opened, which is the same reason RewriteRuleSet.ApplyOnce reads it once.
                var recording = RewriteRecording.Current;
                var mark = recording?.Mark() ?? 0;
                var output = Saturate(input);
                if (recording is not null && output is not null && !output.Equals(input))
                    recording.Note(input, output, null, Name, mark);
                return output;
            }

            private Entity? Saturate(Entity input)
            {
                var graph = new EGraph();
                var root = graph.AddEntity(input);
                graph.Rebuild();

                var ledger = BudgetLedger.For(Name, budget);
                // Stop once the answer has: after two passes in which the cheapest member of the
                // root's class did not change, further merging can only add members this
                // extraction would not choose. That is the fixed point a caller who extracts
                // cares about, and on #1200's inputs it comes on the third pass where the
                // graph's own never does. Measured on the corpus, no answer moves.
                Entity? last = null;
                var unchanged = 0;
                bool Settled()
                {
                    var now = graph.Extract(root, costModel.Cost);
                    unchanged = now is not null && now.Equals(last) ? unchanged + 1 : 0;
                    last = now;
                    return unchanged >= SettledPasses;
                }
                Saturation.Run(graph, SafeRules, ledger, costModel.Cost, Settled);
                ledger.Report();
                return graph.Extract(root, costModel.Cost) ?? input;
            }

            /// <summary>
            /// How many consecutive passes the extracted answer must survive unchanged before
            /// the run stops on the answer rather than on the graph. Two, not one: a pass can
            /// leave the cheapest member alone while adding what the next pass will improve on.
            /// </summary>
            private const int SettledPasses = 2;
        }

        /// <summary>
        /// <see cref="Transformation.CanonicalizationOverGraph(WorkBudget, RewriteRuleGrowth)"/>.
        /// </summary>
        private sealed class GraphCanonicalizationTransformation : Transformation
        {
            private readonly WorkBudget budget;
            private readonly RewriteRuleGrowth widest;
            private readonly IReadOnlyList<Matching.MatchedRule> rules;

            internal GraphCanonicalizationTransformation(WorkBudget budget, RewriteRuleGrowth widest)
                => (this.budget, this.widest, rules)
                    = (budget, widest, Saturation.RulesUpTo(widest));

            public override string Name => $"canonical-over-graph[{widest}]";

            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            // The weakest tier represented in what it may use, which is the convention every rule
            // set in the registry already follows.
            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            /// <inheritdoc cref="EqualitySaturationTransformation.ApplyCore"/>
            protected override Entity? ApplyCore(Entity input)
            {
                var recording = RewriteRecording.Current;
                var mark = recording?.Mark() ?? 0;

                var graph = new EGraph();
                var root = graph.AddEntity(input);
                graph.Rebuild();

                var ledger = BudgetLedger.For(Name, budget);
                // The same stop as EqualitySaturationTransformation's, on the least member
                // rather than the cheapest, since that is what this extracts.
                Entity? last = null;
                var unchanged = 0;
                bool Settled()
                {
                    var now = graph.ExtractLeast(root, EntityOrder.Canonical);
                    unchanged = now is not null && now.Equals(last) ? unchanged + 1 : 0;
                    last = now;
                    return unchanged >= 2;
                }
                Saturation.Run(graph, rules, ledger, CostModel.Default.Cost, Settled);
                ledger.Report();

                // The least member, not the cheapest: a cost model ties, and a tie would make the
                // answer depend on which member was reached first, which is exactly the thing a
                // canonical form may not depend on. See EntityOrder.
                var output = graph.ExtractLeast(root, EntityOrder.Canonical) ?? input;

                if (recording is not null && !output.Equals(input))
                    recording.Note(input, output, null, Name, mark);
                return output;
            }
        }

        private sealed class RationalCanonicalizationTransformation : Transformation
        {
            public override string Name => "rational-canonical-form";

            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            protected override Entity? ApplyCore(Entity input)
                => RationalFunction.TryCanonicalize(input, out var canonical) ? canonical : null;
        }

        private sealed class PolynomialFactorizationTransformation : Transformation
        {
            public override string Name => "polynomial-factorization";

            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            public override Soundness Soundness => Soundness.Sound;

            // Total rather than declining, because a step that returns null makes the whole
            // chain decline -- `Then` has no notion of an optional part, and `Factorization` is
            // a chain. `InnerSimplification` is total for the same reason. So "nothing to
            // factor" is the input handed back, not a refusal.
            protected override Entity? ApplyCore(Entity input)
            {
                // A product is taken apart and each factor asked separately, rather than being
                // handed over whole. Replacing the rules' product would change answers that were
                // never the complaint -- the order two factors come out in is arbitrary and
                // theirs is the one on record -- but *declining* it leaves a real gap: the rules
                // take a numeric content out and hand back `2 * (x ^ 3 - 1)`, and the remainder
                // is exactly the shape #1018 is about. Asking each factor keeps every factor the
                // rules found and splits the ones they could not.
                if (input is Entity.Mulf)
                {
                    Entity? rebuilt = null;
                    var moved = false;
                    foreach (var factor in Entity.Mulf.LinearChildren(input))
                    {
                        var piece = Factored(factor) ?? factor;
                        moved |= !ReferenceEquals(piece, factor);
                        rebuilt = rebuilt is null ? piece : rebuilt * piece;
                    }
                    return moved && rebuilt is not null ? rebuilt : input;
                }
                return Factored(input) ?? input;
            }

            /// <summary>
            /// <paramref name="input"/> factored by the polynomial layer, or <see langword="null"/>
            /// where it does not factor.
            /// </summary>
            /// <remarks>
            /// Each of its variables is tried in turn and the first that yields a genuine product
            /// wins, which is deterministic because <see cref="Entity.Vars"/> is. Trying them all
            /// rather than guessing a main one is what makes <c>x ^ 4 - y ^ 4</c> come out whole
            /// however the caller wrote it.
            /// </remarks>
            private static Entity? Factored(Entity input)
            {
                if (input is Entity.Mulf or Entity.Powf)
                    return null;
                foreach (var variable in input.Vars)
                    if (MathS.Polynomials.Factor(input, variable) is { } factored
                        && factored is Entity.Mulf or Entity.Powf)
                        return factored;
                foreach (var variable in input.Vars)
                    if (FactoredInASubtree(input, variable) is { } factored)
                        return factored;
                return null;
            }

            /// <summary>
            /// <paramref name="input"/> factored as a polynomial in the smallest subtree that
            /// holds every occurrence of <paramref name="variable"/>: <c>sin(x)^2 + 2 sin(x) + 1</c>
            /// is <c>t^2 + 2 t + 1</c> at <c>t = sin(x)</c>, which is <c>(t + 1)^2</c>. The
            /// replacement the solver uses for the same purpose, applied to factoring.
            /// https://github.com/asc-community/AngouriMath/issues/203
            /// </summary>
            private static Entity? FactoredInASubtree(Entity input, Variable variable)
            {
                var subtree = TreeAnalyzer.GetMinimumSubtree(input, variable);
                if (subtree == variable || subtree == input)
                    return null;
                var placeholder = Variable.CreateTemp(input.Vars);
                var replaced = input.Substitute(subtree, placeholder);
                if (replaced.ContainsNode(variable))
                    return null;
                return MathS.Polynomials.Factor(replaced, placeholder) is { } factored
                    && factored is Entity.Mulf or Entity.Powf
                        ? factored.Substitute(placeholder, subtree)
                        : null;
            }
        }

        private sealed class InnerSimplificationTransformation : Transformation
        {
            public override string Name => "inner-simplify";

            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            protected override Entity? ApplyCore(Entity input) => input.InnerSimplified;
        }

        private sealed class SimplificationTransformation : Transformation
        {
            private readonly int level;
            private readonly CostModel? costModel;

            internal SimplificationTransformation(int level, CostModel? costModel)
                => (this.level, this.costModel) = (level, costModel);

            public override string Name
                => costModel is null ? $"simplify[{level}]" : $"simplify[{level}, {costModel.Name}]";

            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            protected override Entity? ApplyCore(Entity input)
            {
                if (costModel is null)
                    return Simplificator.Simplify(input, level);
                using var _ = MathS.Settings.ComplexityCriteria.Set(costModel.Cost);
                return Simplificator.Simplify(input, level);
            }
        }

        private sealed class ExpansionTransformation : Transformation
        {
            private readonly int level;

            internal ExpansionTransformation(int level) => this.level = level;

            public override string Name => $"expand[{level}]";

            public override TransformationRelation Relation => TransformationRelation.Equivalence;

            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            protected override Entity? ApplyCore(Entity input) => input.ExpandOverSum(level);
        }

        private sealed class SubstitutionTransformation : Transformation
        {
            private readonly Entity what, with;

            internal SubstitutionTransformation(Entity what, Entity with) => (this.what, this.with) = (what, with);

            public override string Name => $"substitute[{what.Stringize()} := {with.Stringize()}]";

            // The output is a different expression from the input, not another way of
            // writing it, so subtracting the two means nothing.
            public override TransformationRelation Relation => TransformationRelation.Derivation;

            // Replacing every occurrence of a subexpression by another is valid whatever
            // the two are; nothing about it is conditional.
            public override Soundness Soundness => Soundness.Sound;

            protected override Entity? ApplyCore(Entity input) => input.Substitute(what, with);
        }

        private sealed class DifferentiationTransformation : Transformation
        {
            private readonly Variable variable;

            internal DifferentiationTransformation(Variable variable) => this.variable = variable;

            public override string Name => $"differentiate[{variable.Name}]";

            public override TransformationRelation Relation => TransformationRelation.Derivation;

            // The derivative is the derivative where the expression is differentiable, and
            // an unevaluated Derivativef node where this library does not know a rule.
            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            protected override Entity? ApplyCore(Entity input) => input.DifferentiateOnce(variable);
        }

        private sealed class IntegrationTransformation : Transformation
        {
            private readonly Variable variable;

            internal IntegrationTransformation(Variable variable) => this.variable = variable;

            public override string Name => $"integrate[{variable.Name}]";

            public override TransformationRelation Relation => TransformationRelation.Derivation;

            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            protected override Entity? ApplyCore(Entity input)
                => MathS.Settings.DowncastingEnabled.Value ? Integrate(input) : IntegrateExactly(input);

            private Entity? Integrate(Entity input)
                => Functions.Algebra.Integration.ComputeIndefiniteIntegral(input.InnerSimplified, variable)?.InnerSimplified;

            /// <summary>See <see cref="ReadExactly"/>.</summary>
            private Entity? IntegrateExactly(Entity input)
            {
                using var _ = MathS.Settings.DowncastingEnabled.Set(true);
                return Integrate(ReadExactly(input));
            }
        }

        private sealed class LimitTransformation : Transformation
        {
            private readonly Variable variable;
            private readonly Entity destination;
            private readonly ApproachFrom side;

            internal LimitTransformation(Variable variable, Entity destination, ApproachFrom side)
                => (this.variable, this.destination, this.side) = (variable, destination, side);

            public override string Name => $"limit[{variable.Name} -> {destination.Stringize()}, {side}]";

            public override TransformationRelation Relation => TransformationRelation.Derivation;

            public override Soundness Soundness => Soundness.SoundUnderAssumptions;

            // An indeterminate form the machinery could not read -- 0 / abs(0) for
            // (i / x) / abs(i / x) -- evaluates to NaN, and NaN is the claim that there is no
            // limit. Inside the machinery that NaN is what stops the search, and the machinery
            // reaches this entry point for the limits of its own parts, so it is kept for
            // those; at the outermost call, where the answer is made, it is withheld, and the
            // limit stays a question rather than a wrong answer to one.
            // https://github.com/asc-community/AngouriMath/issues/1186
            protected override Entity? ApplyCore(Entity input)
                => MathS.Settings.DowncastingEnabled.Value ? Limit(input, destination) : LimitExactly(input);

            private Entity? Limit(Entity input, Entity at)
                => LimitFunctional.ComputeLimit(input, variable, at, side) is { } limit
                   && (LimitFunctional.ReadingAnApproach || !LimitFunctional.IsAnIndeterminateForm(limit))
                    ? limit
                    : null;

            /// <summary>See <see cref="ReadExactly"/>.</summary>
            private Entity? LimitExactly(Entity input)
            {
                using var _ = MathS.Settings.DowncastingEnabled.Set(true);
                return Limit(ReadExactly(input), ReadExactly(destination));
            }
        }

        /// <summary>
        /// <paramref name="expr"/> with every decimal number read as the exact rational it is, for
        /// a computation that runs with the downcasting on whatever the caller's setting.
        /// </summary>
        /// <remarks>
        /// With the downcasting off, a whole number parses as a decimal, so <c>x^2</c> is not a
        /// whole power to the integrator, and the zero test reads no literal zero as zero:
        /// integration gave <c>NaN</c> for <c>(a + b arcsin(c x))/sqrt(d - c^2 d x^2)</c> and
        /// limits gave it for <c>sin(c x)/x</c> at 0, where the default setting answers. Both are
        /// computed as with the downcasting on, on the exact values the decimals hold, so that
        /// the answer does not depend on the setting. The cost is that decimals come back as
        /// rationals: the integral of <c>0.1 x</c> is <c>x^2/20</c>.
        /// https://github.com/asc-community/AngouriMath/issues/1490
        /// </remarks>
        private static Entity ReadExactly(Entity expr)
            => expr.Replace(static node => node switch
            {
                Number.Rational => node,
                Number.Real real => Exactly(real),
                Number.Complex { RealPart: var re, ImaginaryPart: var im } => Number.Complex.Create(Exactly(re), Exactly(im)),
                _ => node
            });

        private static Number.Real Exactly(Number.Real value)
            => value is Number.Rational || !value.EDecimal.IsFinite
                ? value
                : Number.Rational.Create(PeterO.Numbers.ERational.FromEDecimal(value.EDecimal));
    }
}
