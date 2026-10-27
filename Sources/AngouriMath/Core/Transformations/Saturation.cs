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

namespace AngouriMath.Core.Transformations
{
    /// <summary>
    /// Runs rules over an e-graph until nothing more merges or the budget runs out — the shared
    /// half of every transformation built on the rewrite graph.
    /// </summary>
    /// <remarks>
    /// Shared rather than written once per caller because the loop is subtle in ways that are not
    /// visible from a second copy of it: what is charged and when, which rules can be skipped
    /// without an attempt, and which of two matching paths a rule takes. Two transformations
    /// already want it — one extracting the cheapest member of the root class and one extracting
    /// the least — and they differ only in that extraction, not in any of this.
    /// </remarks>
    internal static class Saturation
    {
        /// <summary>
        /// The rules of <see cref="MatchedRules"/> whose justification is at least
        /// <see cref="Soundness.SoundUnderAssumptions"/> and whose
        /// <see cref="MatchedRule.Growth"/> is no wider than <paramref name="widest"/>.
        /// </summary>
        /// <param name="widest">
        /// The widest growth admitted, as a ceiling in the order
        /// <see cref="RewriteRuleGrowth.Collects"/>, <see cref="RewriteRuleGrowth.Rearranges"/>,
        /// <see cref="RewriteRuleGrowth.Expands"/>, <see cref="RewriteRuleGrowth.Unknown"/> —
        /// increasing risk, and the order the values are declared in.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b><see cref="RewriteRuleGrowth.Unknown"/> is the last stop rather than an excluded
        /// one, and that is a measurement rather than a preference.</b> It reads like the value a
        /// ceiling should refuse — it means the rule's growth was not judged, because its
        /// replacement is code rather than a written pattern, so admitting it accepts a rewrite
        /// nobody measured. But it is where the rules are: of the 338 rules in
        /// <see cref="MatchedRules"/>, <b>127 collect, 49 rearrange, 36 expand and 126 are
        /// unjudged</b>. A ceiling that refuses the fourth value still refuses 37% of the library,
        /// and what is left could not do much when this was written — over six expression pairs
        /// equal only through a larger intermediate form, the
        /// <see cref="RewriteRuleGrowth.Rearranges"/> ceiling proved two, and
        /// <see cref="RewriteRuleGrowth.Expands"/> proved <i>the same two</i>. The third was proved
        /// only with the unjudged rules admitted.
        /// </para>
        /// <para>
        /// <b>Those figures moved and this paragraph did not.</b> It read "26 collect, 17
        /// rearrange, 9 expand and 270 are unjudged", and 84% — measured before the growth
        /// declarations went in, which took the unjudged count from 261 to 152. The argument
        /// survives the correction and the arithmetic in it did not, which is the reason to state
        /// counts rather than a conclusion drawn from them. <c>RuleAuthoringGuideTest</c> holds all
        /// four of these now, so the next time they move this remark fails with them.
        /// </para>
        /// <para>
        /// So the dial that matters is not how far a rule may enlarge; it is whether rules nobody
        /// classified may fire at all. Naming that as the widest setting makes it a request a
        /// caller has to make on purpose, which is the most this can honestly do about it.
        /// </para>
        /// <para>
        /// <b>And the ceiling is the scheduling policy, not a stand-in for one.</b> When both
        /// directions of an inverse pair are declared, <c>RulesUpTo(Rearranges)</c> keeps the
        /// collecting one and withholds the expanding one, mechanically, for every such pair.
        /// <c>sin(2x)</c> and <c>2 sin x cos x</c> run away at the widest ceiling and saturate at
        /// the safe one because <c>ExpandMultipleAngle</c> declares
        /// <see cref="RewriteRuleGrowth.Expands"/>. Growth is the direction marker. A pair whose
        /// expanding direction is unjudged is the one case this cannot protect, and that is what
        /// declaring a growth for a rule is for. <c>RunawayBreadthTest</c> holds this.
        /// </para>
        /// </remarks>
        internal static IReadOnlyList<MatchedRule> RulesUpTo(RewriteRuleGrowth widest)
            => MatchedRules.All
                .SelectMany(set => set.Rules)
                .Where(rule => rule.Soundness is Soundness.Sound or Soundness.SoundUnderAssumptions)
                .Where(rule => rule.Growth <= widest)
                .ToList();

        /// <summary>
        /// <see cref="RulesUpTo"/> at <see cref="RewriteRuleGrowth.Rearranges"/> — the rules that
        /// never enlarge — built once, lazily: a caller inside the simplifier must not rebuild
        /// the list per call, and the registry must not be read at this type's initialisation.
        /// </summary>
        internal static IReadOnlyList<MatchedRule> SafeRules => safeRules.Value;

        [ConstantField]
        private static readonly Lazy<IReadOnlyList<MatchedRule>> safeRules
            = new(() => RulesUpTo(RewriteRuleGrowth.Rearranges));

        /// <summary>
        /// Merges into <paramref name="graph"/> every equality <paramref name="rules"/> reach from
        /// what it already holds, until a pass changes nothing or <paramref name="ledger"/> stops
        /// it. Answers whether it reached that fixed point rather than the ceiling.
        /// </summary>
        /// <param name="graph">The graph to saturate, in place.</param>
        /// <param name="rules">Which rules to fire; see <see cref="RulesUpTo"/>.</param>
        /// <param name="ledger">What bounds the work, and what records how much was done.</param>
        /// <param name="witnessCost">
        /// How a representative is chosen when one is needed — for a rule whose left-hand side
        /// cannot e-match, which has to be shown a term, and for a rule's <c>when</c> predicate,
        /// which is arbitrary code that has to be asked about something. It ranks a
        /// <i>witness</i>, never the answer: what the caller finally extracts is the caller's own
        /// business, and is where a cheapest-form and a canonical-form caller differ.
        /// </param>
        /// <param name="settled">
        /// A caller's own stop, asked after every pass that merged something; <see langword="true"/>
        /// ends the run without the graph's fixed point having been reached. A caller that
        /// extracts an answer passes "the extraction has not changed for two passes": that is
        /// the fixed point which matters to it, and it comes where the graph's own may never —
        /// on a rational coefficient beside a variable, the regrouping rules add a fresh e-node
        /// to the root's class on every pass while its cheapest member stopped changing on the
        /// third (<a href="https://github.com/asc-community/AngouriMath/issues/1200">#1200</a>).
        /// <see cref="ProvesEqual"/> passes nothing: it needs a union, not an extraction.
        /// </param>
        internal static bool Run(EGraph graph, IReadOnlyList<MatchedRule> rules,
            BudgetLedger ledger, Func<Entity, double> witnessCost, Func<bool>? settled = null)
        {
            var chargedNodes = graph.NodeCount;
            bool ChargeGrowthSinceLastCall()
            {
                var delta = graph.NodeCount - chargedNodes;
                chargedNodes = graph.NodeCount;
                return ledger.Spend(delta);
            }

            // Handed to the e-matcher so that a binding tried is a step, not only the attempt --
            // allocated once here rather than per attempt.
            Func<bool> spend = () => ledger.Spend();

            var saturated = false;
            while (!saturated && !ledger.Exhausted)
            {
                var merged = false;
                foreach (var id in graph.Classes.ToList())
                {
                    if (ledger.Exhausted) break;

                    // Which node types this class holds, gathered once for the whole sweep rather
                    // than once per rule. A pattern that requires a root type cannot match a class
                    // holding no node of it -- RequiredRootType is a necessary condition, not a
                    // sufficient one, which is the direction that makes it a filter: it licenses
                    // skipping a rule, never firing one.
                    //
                    // A union later in this same sweep can add a node type this set does not have,
                    // so a rule can be skipped in a pass where it had just become applicable. That
                    // costs nothing: a union is exactly what sets `merged`, so there is another
                    // pass, and the set is gathered again there.
                    var held = new HashSet<Type>();
                    foreach (var node in graph.NodesOf(id)) held.Add(graph.RuntimeType(node));

                    Entity? term = null;
                    var extracted = false;
                    bool TryTerm(out Entity value)
                    {
                        if (!extracted)
                        {
                            term = graph.Extract(id, witnessCost);
                            extracted = true;
                        }
                        value = term!;
                        return term is not null;
                    }

                    foreach (var rule in rules)
                    {
                        // Before the charge, because a rule this skips was never attempted: a step
                        // is a match attempt, and a type that cannot match is not one. Written as a
                        // loop rather than as `held.Any(t => ...)`: the lambda would capture
                        // `required` and so allocate a closure every time the exact-type test
                        // missed, which is most rules of most classes -- and paying an allocation
                        // to avoid a pattern match is not a pre-filter.
                        if (rule.Left.RequiredRootType is { } required && !held.Contains(required))
                        {
                            var reachable = false;
                            foreach (var type in held)
                                if (required.IsAssignableFrom(type))
                                {
                                    reachable = true;
                                    break;
                                }
                            if (!reachable) continue;
                        }

                        // One step per match attempt, charged before the attempt -- the unit
                        // Buchberger, FGLM and MatchPattern all charge, and the work this loop
                        // actually does. Charging the node-count delta alone (below) billed
                        // nothing on ordinary input, because the rules that do not expand rarely
                        // grow the graph at all: a budget of zero steps ran this sweep to
                        // saturation and then reported that it had completed.
                        if (!ledger.Spend()) break;
                        int other;
                        if (rule.Left.CanEMatch)
                        {
                            if (!rule.TryEMatchApply(graph, id, witnessCost, spend, out other)) continue;
                        }
                        else
                        {
                            if (!TryTerm(out var t)) continue;
                            var rewritten = rule.TryApply(t);
                            if (rewritten is null || rewritten.Equals(t)) continue;
                            other = graph.AddEntity(rewritten);
                        }
                        if (graph.Union(id, other)) merged = true;
                    }

                    // What the sweep grew, charged after it rather than before the next class's,
                    // so that the ceiling bounds the growth that has happened.
                    if (!ChargeGrowthSinceLastCall()) break;
                }

                // Rebuild rescans every node of every class, repeatedly until nothing more becomes
                // congruent -- a round of it is work, and it had no ledger interaction anywhere.
                if (!ledger.Spend()) break;
                graph.Rebuild();
                if (!merged) saturated = true;
                else if (settled is not null && settled()) break;
            }
            return saturated;
        }

        /// <summary>
        /// Whether <paramref name="rules"/> prove <paramref name="left"/> and
        /// <paramref name="right"/> equal, within <paramref name="budget"/>. A
        /// <see langword="false"/> means <i>not proved</i>, never <i>unequal</i>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the reliable half, and canonicalising is not.</b> Bringing two expressions
        /// separately to a canonical form and comparing the results only decides equality where
        /// the rules are <i>confluent</i>: otherwise one side can reach a form the other cannot,
        /// and two equal expressions end in different forms having each been canonicalised
        /// correctly. Measured, on <c>(x + y) * (x - y)</c> against <c>x ^ 2 - y ^ 2</c>: the
        /// product's graph reaches the difference of squares, the difference of squares' graph
        /// does not reach the product, and the two forms are the same size — so each canonicalises
        /// to itself.
        /// </para>
        /// <para>
        /// Asking in one graph has no such gap. Both expressions are inserted, every equality the
        /// rules reach from <i>either</i> is merged, and the question is whether they ended in one
        /// class — which does not depend on the rules being confluent, only on their reaching far
        /// enough within the budget.
        /// </para>
        /// </remarks>
        internal static bool ProvesEqual(Entity left, Entity right,
            IReadOnlyList<MatchedRule> rules, WorkBudget budget)
        {
            if (left is null) throw new ArgumentNullException(nameof(left));
            if (right is null) throw new ArgumentNullException(nameof(right));
            if (left.Equals(right)) return true;

            var graph = new EGraph();
            var a = graph.AddEntity(left);
            var b = graph.AddEntity(right);
            graph.Rebuild();

            var ledger = BudgetLedger.For(nameof(ProvesEqual), budget);
            Run(graph, rules, ledger, CostModel.Default.Cost);
            ledger.Report();
            return graph.Find(a) == graph.Find(b);
        }
    }
}
