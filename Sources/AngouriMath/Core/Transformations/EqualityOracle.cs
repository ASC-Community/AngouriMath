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
using AngouriMath.Core.Transformations.Matching;
using AngouriMath.Numerics;
using static AngouriMath.Entity;
using static AngouriMath.Entity.Number;

namespace AngouriMath.Core.Transformations
{
    /// <summary>
    /// The e-graph asked whether two expressions are equal, with a witness for the other answer.
    /// https://github.com/asc-community/AngouriMath/issues/1251
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Refute first, then prove.</b> A point where both sides are defined and their enclosures
    /// are disjoint settles the question for good and costs a dozen evaluations; a proof costs a
    /// saturation. Looking for the point first also means no proof is ever reported over a point
    /// the sampling found against it — so a rule that declares more soundness than it has
    /// surfaces as <see cref="EqualityVerdict.NotEqual"/> with a witness, never as a wrong
    /// <see cref="EqualityVerdict.Equal"/>, wherever the sampled points reach it.
    /// </para>
    /// <para>
    /// <b>Why the proof is one graph and not two canonical forms.</b> See
    /// <see cref="Saturation.ProvesEqual"/>: two sides canonicalised apart meet only where the
    /// rules are confluent; inserted into one graph, they meet wherever the rules reach.
    /// </para>
    /// </remarks>
    internal static class EqualityOracle
    {
        private const string Where = "AreEqual";

        /// <summary>The canonical-form pass's name in <see cref="EqualityAnswer.Assumptions"/>.</summary>
        internal const string CanonicalizationStep = "canonicalization";

        /// <summary>
        /// Commutativity and associativity of addition and of multiplication, as two
        /// <see cref="Soundness.Sound"/> steps: a sum chain and a product chain each put into
        /// one order, with nothing grouped, folded or rewritten.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Why these are not <c>RewriteRules.CanonicalOrderExact</c>.</b> That set sorts and
        /// also <i>groups</i> -- <c>x + x</c> becomes <c>2 x</c>, a quotient is read as a product
        /// with a negative power -- and grouping is what makes it
        /// <see cref="Soundness.SoundUnderAssumptions"/>. A reordering alone needs no condition,
        /// and the e-graph has none: it does not sort a commutative pair or flatten a nested
        /// chain. These belong to this oracle only and run nowhere else, so neither
        /// <c>Simplify</c> nor any rule set's output moves.
        /// </para>
        /// <para>
        /// <b>Why they are sound.</b> The variables range over the complex numbers, or the reals,
        /// as <see cref="MathS.AreEqual"/>'s <c>domain</c> says, and addition and multiplication
        /// there are commutative and associative with no side condition. Only <see cref="Sumf"/>
        /// and <see cref="Mulf"/> chains are read: a difference or a quotient is an operand, never
        /// flattened, so no subtraction is read as adding a negation. The order is
        /// <see cref="EntityOrder.Canonical"/>, which is total, so two writings of one chain sort
        /// to the same tree and meet by hash-consing. A chain is left alone (the step declines)
        /// where an operand may be something other than a number -- see
        /// <see cref="MayNotCommute"/> -- because a matrix product does not commute; and a
        /// question with a matrix anywhere in it does not get these steps at all
        /// (<see cref="MayHoldAMatrix"/>), since the library reads a name beside a transpose as
        /// possibly a matrix (<a href="https://github.com/asc-community/AngouriMath/issues/2147">#2147</a>)
        /// and that name may be multiplied elsewhere in the same question.
        /// </para>
        /// </remarks>
        internal static IReadOnlyList<MatchedRule> OperandOrder { get; } = new[]
        {
            new MatchedRule(
                "the-terms-of-a-sum-are-reordered",
                MatchPattern.Any<Sumf>("x"),
                (node, _) => Reordered<Sumf>(node, static (a, b) => new Sumf(a, b)),
                Soundness.Sound,
                RewriteRuleGrowth.Rearranges,
                description: "a + (b + c) = (c + a) + b, and every other order and grouping of one sum"),
            new MatchedRule(
                "the-factors-of-a-product-are-reordered",
                MatchPattern.Any<Mulf>("x"),
                (node, _) => Reordered<Mulf>(node, static (a, b) => new Mulf(a, b)),
                Soundness.Sound,
                RewriteRuleGrowth.Rearranges,
                description: "a * (b * c) = (c * a) * b, for numbers, and every other order and grouping of one product"),
        };

        /// <summary>
        /// The rules whose <see cref="MatchedRule.Soundness"/> is <see cref="Soundness.Sound"/>
        /// at the safe growth ceiling, and <see cref="OperandOrder"/>, built once.
        /// </summary>
        internal static IReadOnlyList<MatchedRule> SoundRules => soundRules.Value;

        [ConstantField]
        private static readonly Lazy<IReadOnlyList<MatchedRule>> soundRules
            = new(() => Saturation.SafeRules.Where(rule => rule.Soundness == Soundness.Sound)
                .Concat(OperandOrder).ToList());

        /// <summary>
        /// <see cref="SoundRules"/> without <see cref="OperandOrder"/>, for a question in which
        /// something may be a matrix: see <see cref="MayHoldAMatrix"/>.
        /// </summary>
        private static IReadOnlyList<MatchedRule> SoundRulesWithoutOrder => soundRulesWithoutOrder.Value;

        [ConstantField]
        private static readonly Lazy<IReadOnlyList<MatchedRule>> soundRulesWithoutOrder
            = new(() => Saturation.SafeRules.Where(rule => rule.Soundness == Soundness.Sound).ToList());

        /// <summary>
        /// <see cref="Saturation.SafeRules"/> and <see cref="OperandOrder"/>, built once.
        /// </summary>
        private static IReadOnlyList<MatchedRule> AdmittedRules => admittedRules.Value;

        [ConstantField]
        private static readonly Lazy<IReadOnlyList<MatchedRule>> admittedRules
            = new(() => Saturation.SafeRules.Concat(OperandOrder).ToList());

        /// <summary>
        /// The chain rooted at <paramref name="node"/>, flattened through nested
        /// <typeparamref name="TChain"/> nodes only and rebuilt in <see cref="EntityOrder.Canonical"/>
        /// order, grouped to the left -- or <paramref name="node"/> itself, which declines, where
        /// it is already in that order or an operand may not commute.
        /// </summary>
        private static Entity Reordered<TChain>(Entity node, Func<Entity, Entity, Entity> build) where TChain : Entity
        {
            // The e-matcher hands over the class's cheapest member, which need not be the node
            // type the pattern found in the class.
            if (node is not TChain || !ReadByDefault(node)) return node;
            var operands = new List<Entity>();
            var pending = new Stack<Entity>();
            pending.Push(node);
            while (pending.Count > 0)
            {
                var next = pending.Pop();
                // A link read over a narrower codomain than its own is an operand: rebuilding it
                // would drop the reading.
                if (next is TChain && ReadByDefault(next))
                {
                    // Right child first, so the left one is popped first: the written order.
                    pending.Push(next.DirectChildren[1]);
                    pending.Push(next.DirectChildren[0]);
                }
                else
                {
                    if (MayNotCommute(next)) return node;
                    operands.Add(next);
                }
            }
            operands.Sort(EntityOrder.Canonical);
            var rebuilt = operands[0];
            for (var i = 1; i < operands.Count; i++)
                rebuilt = build(rebuilt, operands[i]);
            return rebuilt.Equals(node) ? node : rebuilt;
        }

        private static bool ReadByDefault(Entity link) => link.Codomain == link.DefaultCodomain;

        /// <summary>
        /// Whether <paramref name="operand"/> may denote something addition or multiplication
        /// does not commute over, or that is not a number at all: a matrix, a transpose (whose
        /// argument the library reads as possibly a matrix), a set, a truth value, a function,
        /// a prime factorisation, or an application of a function whose value nothing says is a number.
        /// </summary>
        /// <remarks>
        /// Any such node anywhere in the operand, not only at its root. Wider than it has to be
        /// -- a determinant is a number, and a sum of matrices commutes -- because a decline costs
        /// a proof and a wrong <see cref="Soundness.Sound"/> costs the answer.
        /// </remarks>
        private static bool MayNotCommute(Entity operand)
        {
            foreach (var node in operand.Nodes)
                if (node is Matrix or Transposef or PrimeFactorizationf or Set or Statement or Lambda or Application)
                    return true;
            return false;
        }

        /// <summary>
        /// Whether a matrix, or a matrix operation, occurs anywhere in <paramref name="expr"/>.
        /// </summary>
        private static bool MayHoldAMatrix(Entity expr)
        {
            foreach (var node in expr.Nodes)
                if (node is Matrix or Transposef or Determinantf or Tracef or MatrixEntryf)
                    return true;
            return false;
        }

        internal static EqualityAnswer Ask(Entity left, Entity right, WorkBudget budget, Soundness weakest, Domain domain)
        {
            if (left is null) throw new ArgumentNullException(nameof(left));
            if (right is null) throw new ArgumentNullException(nameof(right));
            if (budget is null) throw new ArgumentNullException(nameof(budget));
            if (weakest is not (Soundness.Sound or Soundness.SoundUnderAssumptions))
                throw new ArgumentOutOfRangeException(nameof(weakest), weakest,
                    "A heuristic step proves nothing, so it cannot justify an equality.");
            if (domain is not (Domain.Complex or Domain.Real))
                throw new ArgumentOutOfRangeException(nameof(domain), domain,
                    "Equality is asked over the complex or the real numbers.");

            // The caller's argument, capped by a set MathS.Settings.Budget.
            var ledger = BudgetLedger.For(Where, budget, BudgetRole.Argument);
            try
            {
                if (left.Equals(right))
                    return Equal(Soundness.Sound, Array.Empty<string>(), ledger);

                if (FindWitness(left, right, domain, ledger) is { } witness)
                    return new(EqualityVerdict.NotEqual, null, Array.Empty<string>(), witness, ledger.Outcome());

                var matrices = MayHoldAMatrix(left) || MayHoldAMatrix(right);
                var sound = matrices ? SoundRulesWithoutOrder : SoundRules;
                if (weakest == Soundness.Sound)
                    return Prove(left, right, sound, canonicalize: false, ledger, out _)
                        ? Equal(Soundness.Sound, Array.Empty<string>(), ledger)
                        : Unknown(ledger);

                var admitted = matrices ? Saturation.SafeRules : AdmittedRules;
                if (!Prove(left, right, admitted, canonicalize: true, ledger, out var conditional))
                    return Unknown(ledger);
                if (conditional.Count == 0)
                    return Equal(Soundness.Sound, Array.Empty<string>(), ledger);
                // Proved, but a conditional step may have done it, or only been there when it was
                // done. The sound rules alone, on what the budget has left, tell the two apart.
                return Prove(left, right, sound, canonicalize: false, ledger, out _)
                    ? Equal(Soundness.Sound, Array.Empty<string>(), ledger)
                    : Equal(Soundness.SoundUnderAssumptions, conditional, ledger);
            }
            finally
            {
                ledger.Report();
            }
        }

        private static EqualityAnswer Equal(Soundness justification, IReadOnlyList<string> assumptions, BudgetLedger ledger)
            => new(EqualityVerdict.Equal, justification, assumptions, null, ledger.Outcome());

        private static EqualityAnswer Unknown(BudgetLedger ledger)
            => new(EqualityVerdict.Unknown, null, Array.Empty<string>(), null, ledger.Outcome());

        /// <summary>
        /// Whether <paramref name="rules"/> join <paramref name="left"/> and
        /// <paramref name="right"/> in one graph, and the conditional steps that joined any two
        /// classes on the way.
        /// </summary>
        private static bool Prove(Entity left, Entity right, IReadOnlyList<MatchedRule> rules,
            bool canonicalize, BudgetLedger ledger, out IReadOnlyList<string> conditional)
        {
            var steps = new SortedSet<string>(StringComparer.Ordinal);
            conditional = Array.Empty<string>();
            if (ledger.Exhausted) return false;

            var graph = new EGraph();
            var a = graph.AddEntity(left);
            var b = graph.AddEntity(right);
            // The canonical-form pass sorts and also groups like terms, flattens differences and
            // quotients and simplifies each side; OperandOrder only reorders. The pass joins each
            // side to its own canonical form, as one more conditional step: it declares
            // SoundUnderAssumptions.
            if (canonicalize)
                foreach (var (side, id) in new[] { (left, a), (right, b) })
                {
                    if (!ledger.Spend()) return false;
                    var canonical = Transformation.Canonicalization.ApplyOrKeep(side);
                    if (!canonical.Equals(side) && graph.Union(id, graph.AddEntity(canonical)))
                        steps.Add(CanonicalizationStep);
                }
            graph.Rebuild();

            if (graph.Find(a) != graph.Find(b))
                Saturation.Run(graph, rules, ledger, CostModel.Default.Cost,
                    settled: () => graph.Find(a) == graph.Find(b),
                    merged: rule =>
                    {
                        if (rule.Soundness != Soundness.Sound) steps.Add(rule.Name);
                    });

            conditional = steps.ToList();
            return graph.Find(a) == graph.Find(b);
        }

        /// <summary>
        /// Values the variables are given in turn: irregular, so that no point sits on a pole or a
        /// cut by the coincidence of a small integer, of both signs, and off the real line.
        /// </summary>
        [ConstantField]
        private static readonly Complex[] RealValues =
        {
            Rational.Create(7, 5), Rational.Create(-3, 7), Rational.Create(13, 4),
            Rational.Create(-11, 5), Rational.Create(2, 9), Rational.Create(-5, 3),
        };

        [ConstantField]
        private static readonly Complex[] ComplexValues = RealValues.Concat(new[]
        {
            Complex.Create(Rational.Create(2, 3), Rational.Create(5, 4)),
            Complex.Create(Rational.Create(-7, 6), Rational.Create(1, 3)),
            Complex.Create(Rational.Create(1, 5), Rational.Create(-9, 7)),
            // Past pi off the real line, where a logarithm's principal branch stops undoing
            // an exponential: ln(e^x) and x part only once |Im x| > pi.
            Complex.Create(Rational.Create(-2, 7), Rational.Create(19, 5)),
        }).ToArray();

        /// <summary>How many points are tried, each charged one step.</summary>
        internal const int Points = 12;

        /// <summary>
        /// A point at which both sides are defined and provably differ, or <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// "Defined" is asked three ways, and all three must say yes, because a witness is a claim
        /// that two values exist: the library's own evaluation gives a finite number, and an
        /// enclosure exists for every subexpression, not only for the whole. The second matters
        /// because the enclosures take a product with an exact zero to zero whatever the other
        /// factor is, so <c>0 * (1/x)</c> at <c>x = 0</c> has an enclosure and no value. "Differ"
        /// is an enclosure of the difference that excludes zero -- doubles first, fifty digits
        /// where the doubles cannot tell -- never a difference that merely looks large.
        /// </remarks>
        private static IReadOnlyDictionary<Variable, Entity>? FindWitness(Entity left, Entity right, Domain domain, BudgetLedger ledger)
        {
            var variables = left.Vars.Concat(right.Vars).Distinct().OrderBy(v => v.Name, StringComparer.Ordinal).ToList();
            var pool = domain == Domain.Real ? RealValues : ComplexValues;
            var points = variables.Count == 0 ? 1 : Points;
            for (var i = 0; i < points; i++)
            {
                if (!ledger.Spend()) return null;
                var point = new Dictionary<Variable, Entity>();
                for (var j = 0; j < variables.Count; j++)
                    point[variables[j]] = pool[(i + 5 * j) % pool.Length];
                var l = variables.Count == 0 ? left : left.Substitute(point);
                var r = variables.Count == 0 ? right : right.Substitute(point);
                if (Differ(l, r, domain)) return point;
            }
            return null;
        }

        private static bool Differ(Entity left, Entity right, Domain domain)
        {
            if (Enclosure(left, domain) is not { } l || Enclosure(right, domain) is not { } r)
                return false;
            if (IntervalEvaluation.Agree(l, r, 0) is { } agree)
                return !agree;
            const int digits = 50;
            return PreciseEvaluation.Of(left, digits) is { } pl && PreciseEvaluation.Of(right, digits) is { } pr
                && PreciseEvaluation.Agree(pl, pr, 0) == false;
        }

        /// <summary>The enclosure of a variable-free expression that is defined, or null.</summary>
        private static ComplexInterval? Enclosure(Entity expr, Domain domain)
        {
            if (expr.Evaled is not Complex value || value.IsNaN || !value.IsFinite)
                return null;
            if (domain == Domain.Real && value is not Real)
                return null;
            foreach (var node in expr.Nodes)
            {
                if (node is Complex) continue;
                if (IntervalEvaluation.Of(node) is not { } enclosure)
                    return null;
                if (domain == Domain.Real && !enclosure.IsReal)
                    return null;
            }
            return IntervalEvaluation.Of(expr);
        }
    }
}
